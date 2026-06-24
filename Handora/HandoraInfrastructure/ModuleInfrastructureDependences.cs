namespace HandoraInfrastructure;

using Microsoft.Extensions.DependencyInjection;
using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries;
using HandoraInfrastructure.Repositries_UOW;
using HandoraInfrastructure.Settings;
using HandoraInfrastructure.Services;
using Microsoft.Extensions.Configuration;
using HandoraApplication.AI.Interfaces;
using HandoraApplication.IServices;
using HandoraInfrastructure.AI.Options;
using System;

public static class ModuleInfrastructureDependences
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service , IConfiguration configuration)
    {
        service.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        service.AddScoped<IProductRepository, ProductRepository>();
        service.AddScoped<IOrderRepository, OrderRepository>();
        service.AddScoped<ICouponRepository, CouponRepository>();
        service.AddScoped<IShopRepository, ShopRepository>();
        service.AddScoped<IUnitOfWork, UnitOfWork>();
        service.Configure<PaymobSettings>(
        configuration.GetSection("Paymob"));
        service.AddScoped<IOtpRepository, OtpRepository>();
        service.AddScoped<IUserStatsRepository, UserStatsRepository>();
        service.AddScoped<IAiReviewService, GeminiAiService>();

        // Custom Studio Repositories
        service.AddScoped<ICustomRequestRepository, CustomRequestRepository>();
        service.AddScoped<IGeneratedDesignRepository, GeneratedDesignRepository>();
        service.AddScoped<ICustomOfferRepository, CustomOfferRepository>();
        service.AddScoped<IProjectWorkspaceRepository, ProjectWorkspaceRepository>();

        // Custom Studio Services & Settings
        service.Configure<AiImageGeneratorSettings>(configuration.GetSection(AiImageGeneratorSettings.SectionName));
        service.AddScoped<IImageValidationService, ImageValidationService>();

        // Caching
        service.AddMemoryCache();

        // Prompt Builder
        service.AddScoped<IAIPromptBuilder, GoogleCrochetPromptBuilder>();

        // Dynamic AI Image Generator registration based on ActiveProvider
        var activeProvider = configuration["AIProvider"] ?? configuration["AiImageGenerator:ActiveProvider"] ?? "Google";
        if (string.Equals(activeProvider, "Mock", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(activeProvider, "MockProvider", StringComparison.OrdinalIgnoreCase))
        {
            // Register IAIImageGenerationService wrapped with cache decorator
            service.AddScoped<IAIImageGenerationService>(sp =>
            {
                var mockService = new MockAIImageGenerationService(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiImageGeneratorSettings>>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MockAIImageGenerationService>>()
                );
                return new AIImageGenerationCacheDecorator(
                    mockService,
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AIImageGenerationCacheDecorator>>()
                );
            });
        }
        else
        {
            // Register IAIImageGenerationService wrapped with cache decorator
            service.AddHttpClient<GoogleAIImageGenerationService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            service.AddScoped<IAIImageGenerationService>(sp =>
            {
                var googleService = sp.GetRequiredService<GoogleAIImageGenerationService>();
                return new AIImageGenerationCacheDecorator(
                    googleService,
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AIImageGenerationCacheDecorator>>()
                );
            });
        }

        return service;
    }
}
