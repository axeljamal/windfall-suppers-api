namespace WindfallSuppers.Api.Domain;

public enum DomainError
{
    Invalid,
    NotFound,
    Conflict,
}

/// <summary>
/// A broken business rule. The API maps the error kind to an HTTP status in one
/// place (ProblemDetailsExceptionHandler), so the domain never knows about HTTP.
/// </summary>
public class DomainException(DomainError error, string message) : Exception(message)
{
    public DomainError Error { get; } = error;
}
