namespace ReconFlow.Application.Exceptions;

public class AppException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string code, string message) : AppException(code, message, 404);

public sealed class ConflictException(string code, string message) : AppException(code, message, 409);

public sealed class BusinessRuleException(string code, string message) : AppException(code, message, 422);
