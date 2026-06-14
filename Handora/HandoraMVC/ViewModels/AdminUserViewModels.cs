using System;
using System.Collections.Generic;

namespace HandoraMVC.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImage { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool HasShop { get; set; }
        public Guid? ShopId { get; set; }
    }

    public class UserManagementViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public string? SelectedRole { get; set; }
        public string? SelectedStatus { get; set; }
    }

    public class ShopDetailsViewModel
    {
        public Guid ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public decimal TotalSales { get; set; }
        public bool IsVerified { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int ActiveProductCount { get; set; }
    }

    public class UserDetailsViewModel
    {
        // Basic Info
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public bool IsSeller { get; set; }
        public bool HasShop { get; set; }
        public Guid? ShopId { get; set; }

        // Buyer Details
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public List<UserOrderDetailViewModel> RecentOrders { get; set; } = new();
        public List<UserAddressViewModel> Addresses { get; set; } = new();
        public List<UserReviewViewModel> Reviews { get; set; } = new();

        // Seller Details
        public string? ShopName { get; set; }
        public decimal ShopRating { get; set; }
        public int ShopReviewCount { get; set; }
        public int ShopProductCount { get; set; }
        public decimal ShopTotalSales { get; set; }
        public int ShopOrderCount { get; set; }
        public bool ShopIsVerified { get; set; }
        public List<UserProductViewModel> RecentProducts { get; set; } = new();
    }

    public class UserOrderDetailViewModel
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserAddressViewModel
    {
        public Guid Id { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
    }

    public class UserReviewViewModel
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserProductViewModel
    {
        public Guid Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
