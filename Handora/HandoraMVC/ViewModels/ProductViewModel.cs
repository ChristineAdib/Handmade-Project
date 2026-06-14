using HandoraDomain.Models.ProductEntities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace HandoraMVC.ViewModels
{
    public class ProductItemViewModel
    {
        public Guid Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price;
        public string? MainImageUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string CategoryNameEn { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();
        public Guid? SelectedCategoryId { get; set; }
        public ProductStatus? SelectedStatus { get; set; }
        public string? SearchQuery { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
