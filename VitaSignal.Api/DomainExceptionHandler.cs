using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VitaSignal.Application.Common.Exceptions;
using VitaSignal.Domain.Common;

namespace VitaSignal.Api;

public sealed partial class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<DomainExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
            DomainValidationException or InvalidRequestException => (StatusCodes.Status400BadRequest, "Invalid request.", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? "/";

        if (statusCode >= StatusCodes.Status500InternalServerError)
            LogUnexpectedError(_logger, exception, method, path);
        else
            LogClientError(_logger, statusCode, method, path, detail);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error while handling {HttpMethod} {HttpPath}")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception, string httpMethod, string httpPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returning {StatusCode} for {HttpMethod} {HttpPath}: {ErrorDetail}")]
    private static partial void LogClientError(ILogger logger, int statusCode, string httpMethod, string httpPath, string? errorDetail);
}
