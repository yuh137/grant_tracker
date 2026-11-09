using GrantTracker.Domain.Enums;

namespace GrantTracker.Domain;

public class BusinessRuleException(BusinessRuleErrorCode errorCode, string message) : Exception(message)
{
    public BusinessRuleErrorCode ErrorCode { get; } = errorCode;
}

public class NotFoundException(string entity, Guid id) : Exception($"{entity} '{id}' was not found.");
