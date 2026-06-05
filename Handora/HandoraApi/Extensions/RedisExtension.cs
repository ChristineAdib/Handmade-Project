namespace HandoraApi.Extensions
{
    public static class RedisExtension
    {
        public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                               ?? configuration["Redis:ConnectionString"];

            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "Handora_";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            return services;
        }
    }
}
