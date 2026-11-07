namespace GrantTracker.Domain;

/// <summary>
/// A request was well-formed but violates a business rule (BR-1 .. BR-5).
/// <see cref="Code"/> is the rule id from the specification, surfaced to clients.
/// </summary>
public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string entity, Guid id)
    : Exception($"{entity} '{id}' was not found.");
