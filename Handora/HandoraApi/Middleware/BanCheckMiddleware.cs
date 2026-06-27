using HandoraDomain.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace HandoraApi.Middleware
{
    public class BanCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public BanCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthRepository authRepo)
        {
            // خلي الـ check-status يعدي عادي عشان الـ polling يشتغل
            var path = context.Request.Path.Value ?? "";
            var isCheckStatus = path.Contains("check-status");

            if (!isCheckStatus && context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await authRepo.GetByIdAsync(userId);

                    if (user != null && user.IsBanned)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var response = new
                        {
                            success = false,
                            message = "Your account has been suspended. Please contact support.",
                            data = (object?)null
                        };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}