using HandoraApi.Hubs;
using HandoraApi.Middleware;
using Microsoft.Extensions.Hosting;

namespace HandoraApi.Extensions;

public static class BuilderExtension
{
    public static void UseCustomMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("development");
        }
        else
        {
            app.UseCors("production");
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseMiddleware<BanCheckMiddleware>(); 
        app.MapControllers();

        // /admin  →  AdminController.Analytics (MVC admin panel)
        app.MapControllerRoute(
            name: "admin",
            pattern: "admin/{action=Analytics}/{id?}",
            defaults: new { controller = "Admin" });

        // All other MVC routes
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<ChatHub>("/hubs/chat");
    }
}
