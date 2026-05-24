namespace HandoraInfrastructure.Settings
{
    public class PaymobSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Hmac { get; set; } = string.Empty;
        public int IntegrationId { get; set; }
    }
}