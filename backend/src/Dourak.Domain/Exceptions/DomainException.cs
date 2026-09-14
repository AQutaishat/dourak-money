namespace Dourak.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would violate a Dourak business rule
/// (e.g. changing a locked payout order, deleting a member with history).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
