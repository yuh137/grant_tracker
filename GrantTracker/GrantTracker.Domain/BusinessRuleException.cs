namespace GrantTracker.Domain;

public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string entity, Guid id)
    : Exception($"{entity} '{id}' was not found.");
