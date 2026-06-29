namespace HandoraApi.Extensions
{
    public static class RedisExtension
    {
        public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                               ?? configuration["Redis:ConnectionString"]
                               ?? configuration["Redis"];

            bool isValidRedis = !string.IsNullOrEmpty(redisConnection) 
                                && !redisConnection.Contains("YOUR_") 
                                && !redisConnection.Contains("placeholder");

            if (isValidRedis)
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
