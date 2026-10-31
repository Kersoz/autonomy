namespace Autonomy.Domain.Exceptions;

public sealed class AssignmentConflictException(string message) : Exception(message);
