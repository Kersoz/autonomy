
using System.Text;
using System.Text.Json.Serialization;
using Autonomy.Api.BackgroundJobs;
using Autonomy.Api.Authorization;
using Autonomy.Api.Middleware;
using Autonomy.Api.Services;
using Autonomy.Api.Validation;
using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.Features.Couriers;
using Autonomy.Application.Features.Assignments;
using Autonomy.Application.Features.Deliveries;
using Autonomy.Application.Features.JobOffers;
using Autonomy.Application.Interfaces;
using Autonomy.Application.Mappings;
using FluentValidation;
using Autonomy.Persistence.Context;
using Autonomy.Persistence.Identity;
using Autonomy.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddMvcOptions(options =>
    {
        options.Filters.AddService<FluentValidationActionFilter>();
        options.Filters.AddService<CorrelationProblemDetailsFilter>();
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.AddScoped<CorrelationProblemDetailsFilter>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
            ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };
        details.Extensions["correlationId"] =
            context.HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
        return new BadRequestObjectResult(details);
    };
});

// PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured. Set ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<AutonomyDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICourierRepository, CourierRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IJobOfferRepository, JobOfferRepository>();
builder.Services.AddScoped<ICourierService, CourierService>();
builder.Services.AddScoped<IAssignmentService, AssignmentApplicationService>();
builder.Services.AddScoped<IDeliveryService, DeliveryApplicationService>();
builder.Services.AddScoped<IJobOfferService, JobOfferApplicationService>();
builder.Services.AddHostedService<JobOfferExpirationService>();
builder.Services.AddAutoMapper(
    _ => { },
    typeof(CourierMappingProfile).Assembly);
builder.Services.AddScoped<AuthService>();

// ASP.NET Core Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AutonomyDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT configuration
var jwtKey = builder.Configuration["Authentication:Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy(AuthorizationPolicies.PlatformAdmin, policy =>
        policy.RequireRole(Autonomy.Persistence.Identity.AuthorizationRoles.PlatformAdmin));

    options.AddPolicy(AuthorizationPolicies.UserProvisioning, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || AuthorizationPolicies.IsFleetAdmin(context.User)));

    options.AddPolicy(AuthorizationPolicies.FleetAdmin, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.FleetAdmin)
                && AuthorizationPolicies.HasTenant(
                    context.User))));

    options.AddPolicy(AuthorizationPolicies.FleetOperations, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (AuthorizationPolicies.HasTenant(
                    context.User)
                && (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.FleetAdmin)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Dispatcher)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Courier)))));

    options.AddPolicy(AuthorizationPolicies.MerchantOperations, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.MerchantUser)
                && AuthorizationPolicies.HasMerchant(context.User))));

    options.AddPolicy(AuthorizationPolicies.CourierOperations, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Courier)
                && AuthorizationPolicies.HasTenant(context.User))));

    options.AddPolicy(AuthorizationPolicies.DeliveryOperations, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (AuthorizationPolicies.HasTenant(context.User)
                && (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.FleetAdmin)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Dispatcher)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Courier)
                    || (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.MerchantUser)
                        && AuthorizationPolicies.HasMerchant(context.User))))));

    options.AddPolicy(AuthorizationPolicies.DeliveryManagement, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (AuthorizationPolicies.HasTenant(context.User)
                && (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.FleetAdmin)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Dispatcher)
                    || (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.MerchantUser)
                        && AuthorizationPolicies.HasMerchant(context.User))))));

    options.AddPolicy(AuthorizationPolicies.DeliveryDispatch, policy =>
        policy.RequireAssertion(context =>
            AuthorizationPolicies.IsPlatformAdmin(context.User)
            || (AuthorizationPolicies.HasTenant(context.User)
                && (context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.FleetAdmin)
                    || context.User.IsInRole(Autonomy.Persistence.Identity.AuthorizationRoles.Dispatcher)))));
});

var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"];
if (!string.IsNullOrWhiteSpace(frontendOrigin))
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(frontendOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<RequestTrackingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Autonomy API v1");
    });
}

app.UseHttpsRedirection();

if (!string.IsNullOrWhiteSpace(frontendOrigin))
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
