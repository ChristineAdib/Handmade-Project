namespace HandoraMVC.ViewModels;

public class SellerIndexViewModel
{
    public List<SellerCardViewModel> Sellers { get; set; } = [];
}

public class SellerCardViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImage { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime MemberSince { get; set; }
}

public class ShopDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Logo { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal TotalSales { get; set; }
    public bool IsVerified { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}