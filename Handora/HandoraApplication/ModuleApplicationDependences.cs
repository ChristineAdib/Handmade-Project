namespace HandoraApplication;

using HandoraApplication.DTOs.AdminDashboardDTOs;
using HandoraApplication.DTOs.CustomStudioDTOs;
using HandoraApplication.IServices;
using HandoraApplication.Mappers;
using HandoraApplication.Services;
using HandoraDomain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleApplicationDependences
{
    public static IServiceCollection AddReposetoriesServices(this IServiceCollection services)
    {
        MapsterSettings.Configure();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<IEscrowService, EscrowService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISellerService, SellerService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IShopReviewService, ShopReviewService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminCustomStudioService, AdminCustomStudioService>();

        services.AddScoped<ISellerAnalyticsService, SellerAnalyticsService>();
        services.AddHttpClient();

        services.AddScoped<IProductAgentService, ProductAgentService>();

        // Handora Custom Studio Services & Validators
        services.AddScoped<ICustomStudioService, CustomStudioService>();
        services.AddScoped<IValidator<CreateCustomRequestCommand>, CreateCustomRequestCommandValidator>();
        services.AddScoped<IValidator<SaveConfigurationCommand>, SaveConfigurationCommandValidator>();
        services.AddScoped<IValidator<CreateSellerOfferCommand>, CreateSellerOfferCommandValidator>();

        return services;
    }
}