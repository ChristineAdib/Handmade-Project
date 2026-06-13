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
}
