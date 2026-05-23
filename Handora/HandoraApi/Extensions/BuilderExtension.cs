using HandoraApi.Middleware;

namespace HandoraApi.Extensions;

public static class BuilderExtension
{
    public static void UseCustomMiddlewares(this WebApplication app)
    {
        app.UseCors("development");
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
