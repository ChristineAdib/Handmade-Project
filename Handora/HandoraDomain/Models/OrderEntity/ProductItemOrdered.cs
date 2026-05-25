namespace HandoraDomain.Models.OrderEntity
{
    public class ProductItemOrdered
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;

        public ProductItemOrdered() { }

        public ProductItemOrdered(Guid productId, string productName, string pictureUrl)
        {
            ProductId = productId;
            ProductName = productName;
            PictureUrl = pictureUrl;
        }
    }
}