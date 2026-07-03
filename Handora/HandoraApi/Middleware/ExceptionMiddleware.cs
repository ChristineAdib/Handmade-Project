using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.AI.Exceptions;

namespace HandoraApi.Middleware
{
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AuthException ex)
            {
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (AIQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "AI Provider quota exceeded: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status429TooManyRequests, ex.Message);
            }
            catch (AIRateLimitException ex)
            {
                _logger.LogWarning(ex, "AI Provider rate limit reached: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status429TooManyRequests, ex.Message);
            }
            catch (AIInvalidPromptException ex)
            {
                _logger.LogWarning(ex, "Invalid AI request prompt: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (AIInvalidImageException ex)
            {
                _logger.LogWarning(ex, "Invalid AI input image: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (AITimeoutException ex)
            {
                _logger.LogError(ex, "AI request timed out: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status504GatewayTimeout, ex.Message);
            }
            catch (AIProviderUnavailableException ex)
            {
                _logger.LogError(ex, "AI Provider is unavailable: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
            catch (AINetworkException ex)
            {
                _logger.LogError(ex, "AI Network connection failure: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
            catch (AIException ex)
            {
                _logger.LogError(ex, "General AI Provider exception: {Message}", ex.Message);
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation("Request was canceled by the client: {Message}", ex.Message);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 499; // Client Closed Request
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Request was canceled by the client."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception.");
                await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,
                    ex.ToString());
            }
        }

        private static Task WriteResponseAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(message);
            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
