namespace HandoraApi.Extensions
{
    public static class RedisExtension
    {
        public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis")
                                     ?? configuration["Redis:ConnectionString"];
                options.InstanceName = "Handora_";
            });

            return services;
        }
    }
}
