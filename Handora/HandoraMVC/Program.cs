using HandoraApplication;
using HandoraApplication.IServices;
using HandoraApplication.Mappers;
using HandoraApplication.Services;
using HandoraApplication.Settings;
using HandoraApplication.Helpers.AuthHelper;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraInfrastructure;
using HandoraInfrastructure.Data;
using HandoraInfrastructure.Repositries;
using HandoraInfrastructure.Repositries_UOW;
using HandoraMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie Authentication للـ MVC (مش JWT)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<IFileService, FileService>();
// Infrastructure Services

// Infrastructure
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Application
MapsterSettings.Configure();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IPayoutService, PayoutService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminCustomStudioService, AdminCustomStudioService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<INotificationHubContext, HttpNotificationHubContext>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5216/";
    if (!apiBaseUrl.EndsWith("/"))
    {
        apiBaseUrl += "/";
    }
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<HandoraApplication.AI.Interfaces.IProductIndexerService, HandoraMVC.Services.HttpProductIndexerService>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5216/";
    if (!apiBaseUrl.EndsWith("/"))
    {
        apiBaseUrl += "/";
    }
    client.BaseAddress = new Uri(apiBaseUrl);
});


// Redis (نفس pattern الـ API)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Handora_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Analytics}/{id?}");

app.Run();