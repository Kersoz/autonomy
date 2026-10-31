using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autonomy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentEngineConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_DeliveryId",
                table: "Assignments");

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "DeliveryId"
                               ORDER BY "AssignedAt" DESC, "Id" DESC) AS position
                    FROM "Assignments"
                    WHERE "Status" = 1
                )
                UPDATE "Assignments" AS assignment
                SET "Status" = 2,
                    "EndedAt" = COALESCE(assignment."EndedAt", CURRENT_TIMESTAMP),
                    "ReleaseReason" = COALESCE(
                        assignment."ReleaseReason",
                        'Duplicate active assignment closed during migration')
                FROM ranked
                WHERE assignment."Id" = ranked."Id"
                  AND ranked.position > 1;

                UPDATE "Deliveries" AS delivery
                SET "ActiveAssignmentId" = assignment."Id",
                    "AssignedCourierId" = assignment."CourierId"
                FROM "Assignments" AS assignment
                WHERE assignment."DeliveryId" = delivery."Id"
                  AND assignment."Status" = 1;

                UPDATE "Deliveries" AS delivery
                SET "ActiveAssignmentId" = NULL
                WHERE delivery."ActiveAssignmentId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Assignments" AS assignment
                      WHERE assignment."Id" = delivery."ActiveAssignmentId"
                        AND assignment."Status" = 1);

                UPDATE "Couriers" AS courier
                SET "ActiveDeliveryCount" = (
                        SELECT COUNT(*)::integer
                        FROM "Assignments" AS assignment
                        WHERE assignment."CourierId" = courier."Id"
                          AND assignment."Status" = 1),
                    "CurrentAssignmentId" = (
                        SELECT assignment."Id"
                        FROM "Assignments" AS assignment
                        WHERE assignment."CourierId" = courier."Id"
                          AND assignment."Status" = 1
                        ORDER BY assignment."AssignedAt" DESC, assignment."Id" DESC
                        LIMIT 1);

                DO $block$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Assignments" AS assignment
                        JOIN "Couriers" AS courier ON courier."Id" = assignment."CourierId"
                        WHERE assignment."Status" = 1
                        GROUP BY courier."Id", courier."MaxSimultaneousDeliveries"
                        HAVING COUNT(*) > courier."MaxSimultaneousDeliveries") THEN
                        RAISE EXCEPTION
                            'Active assignments exceed courier capacity; resolve these records before applying the assignment engine migration.';
                    END IF;
                END;
                $block$;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_delivery_active_assignment",
                table: "Assignments",
                column: "DeliveryId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_courier_max_simultaneous_deliveries",
                table: "Couriers",
                sql: "\"MaxSimultaneousDeliveries\" > 0");

            migrationBuilder.Sql("""
                CREATE FUNCTION enforce_courier_assignment_capacity()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    courier_limit integer;
                    active_count integer;
                BEGIN
                    SELECT "MaxSimultaneousDeliveries"
                    INTO courier_limit
                    FROM "Couriers"
                    WHERE "Id" = NEW."CourierId"
                    FOR UPDATE;

                    IF NOT FOUND THEN
                        RETURN NEW;
                    END IF;

                    SELECT COUNT(*)::integer
                    INTO active_count
                    FROM "Assignments"
                    WHERE "CourierId" = NEW."CourierId"
                      AND "Status" = 1
                      AND "Id" <> NEW."Id";

                    IF NEW."Status" = 1 AND active_count >= courier_limit THEN
                        RAISE EXCEPTION 'Courier has reached concurrent assignment capacity.'
                            USING ERRCODE = '23514',
                                  CONSTRAINT = 'ck_courier_assignment_capacity';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER enforce_courier_assignment_capacity
                BEFORE INSERT OR UPDATE OF "CourierId", "Status"
                ON "Assignments"
                FOR EACH ROW
                EXECUTE FUNCTION enforce_courier_assignment_capacity();

                CREATE FUNCTION guard_courier_assignment_capacity_update()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    active_count integer;
                BEGIN
                    SELECT COUNT(*)::integer
                    INTO active_count
                    FROM "Assignments"
                    WHERE "CourierId" = NEW."Id"
                      AND "Status" = 1;

                    IF NEW."MaxSimultaneousDeliveries" <= 0
                       OR active_count > NEW."MaxSimultaneousDeliveries" THEN
                        RAISE EXCEPTION 'Courier capacity cannot be lower than active assignments.'
                            USING ERRCODE = '23514',
                                  CONSTRAINT = 'ck_courier_max_simultaneous_deliveries';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER guard_courier_assignment_capacity_update
                BEFORE INSERT OR UPDATE OF "MaxSimultaneousDeliveries"
                ON "Couriers"
                FOR EACH ROW
                EXECUTE FUNCTION guard_courier_assignment_capacity_update();

                CREATE FUNCTION sync_courier_active_assignments()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF NEW."Status" = 1 THEN
                            PERFORM sync_courier_active_assignment_count(NEW."CourierId");
                        END IF;
                        RETURN NEW;
                    ELSIF TG_OP = 'DELETE' THEN
                        IF OLD."Status" = 1 THEN
                            PERFORM sync_courier_active_assignment_count(OLD."CourierId");
                        END IF;
                        RETURN OLD;
                    END IF;

                    IF OLD."Status" = 1 THEN
                        PERFORM sync_courier_active_assignment_count(OLD."CourierId");
                    END IF;
                    IF NEW."Status" = 1
                       AND (OLD."Status" <> 1 OR OLD."CourierId" <> NEW."CourierId") THEN
                        PERFORM sync_courier_active_assignment_count(NEW."CourierId");
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION sync_courier_active_assignment_count(courier_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    UPDATE "Couriers" AS courier
                    SET "ActiveDeliveryCount" = (
                            SELECT COUNT(*)::integer
                            FROM "Assignments" AS assignment
                            WHERE assignment."CourierId" = courier_id
                              AND assignment."Status" = 1),
                        "CurrentAssignmentId" = (
                            SELECT assignment."Id"
                            FROM "Assignments" AS assignment
                            WHERE assignment."CourierId" = courier_id
                              AND assignment."Status" = 1
                            ORDER BY assignment."AssignedAt" DESC, assignment."Id" DESC
                            LIMIT 1)
                    WHERE courier."Id" = courier_id;
                END;
                $function$;

                CREATE TRIGGER sync_courier_active_assignments
                AFTER INSERT OR DELETE OR UPDATE OF "CourierId", "Status"
                ON "Assignments"
                FOR EACH ROW
                EXECUTE FUNCTION sync_courier_active_assignments();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_delivery_active_assignment",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DeliveryId",
                table: "Assignments",
                column: "DeliveryId");

            migrationBuilder.DropCheckConstraint(
                name: "ck_courier_max_simultaneous_deliveries",
                table: "Couriers");

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS sync_courier_active_assignments ON "Assignments";
                DROP FUNCTION IF EXISTS sync_courier_active_assignments();
                DROP FUNCTION IF EXISTS sync_courier_active_assignment_count(uuid);
                DROP TRIGGER IF EXISTS guard_courier_assignment_capacity_update ON "Couriers";
                DROP FUNCTION IF EXISTS guard_courier_assignment_capacity_update();
                DROP TRIGGER IF EXISTS enforce_courier_assignment_capacity ON "Assignments";
                DROP FUNCTION IF EXISTS enforce_courier_assignment_capacity();
                """);
        }
    }
}
