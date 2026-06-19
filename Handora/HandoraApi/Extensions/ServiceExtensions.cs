using HandoraApi.Hubs;
using HandoraApi.Services;
using HandoraApplication.AI.Embeddings;
using HandoraApplication.AI.Interfaces;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.Hubs;
using HandoraApplication.IServices;
using HandoraApplication.Services;
using HandoraApplication.Settings;
using HandoraDomain.Interfaces;
using HandoraInfrastructure.AI.OpenAI;
using HandoraInfrastructure.AI.Options;
using HandoraInfrastructure.Repositries_UOW;
using HandoraInfrastructure.AI.Qdrant;
using HandoraInfrastructure.AI.Documents;
using HandoraApplication.AI.Services;

namespace HandoraApi.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));

            services.AddScoped<IChatHubContext, ChatHubContext>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<INotificationHubContext, NotificationHubContext>();
            services.AddSignalR();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddSingleton<JwtHelper>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ICartService, CartService>();
           services.AddScoped<IWishListService, WishListService>();
            // ✅ OpenAI config
            services.Configure<OpenAIOptions>(
                configuration.GetSection(OpenAIOptions.SectionName));

            // ✅ Qdrant config
            services.Configure<QdrantOptions>(
                configuration.GetSection(QdrantOptions.SectionName));

            // ✅ Gemini config
            services.Configure<GeminiOptions>(
                configuration.GetSection(GeminiOptions.SectionName));
            services.AddHttpClient<IGeminiService, GeminiService>();

            services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();
            services.AddSingleton<IVectorStoreService, QdrantService>();
            services.AddSingleton<IChunkService, ChunkService>();
            services.AddScoped<IRagService, RagService>();

            services.AddScoped<IGiftConversationManager, GiftConversationManager>();
            services.AddScoped<IGiftAssistantService, GiftAssistantService>();
            services.AddScoped<IProductIndexerService, ProductIndexerService>();

            return services;
        }
    }
}
