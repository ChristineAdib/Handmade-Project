using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HandoraMVC.ViewModels;

public class CategoryViewModel
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentNameEn { get; set; }
    public List<SubCategoryViewModel> SubCategories { get; set; } = [];
}

public class SubCategoryViewModel
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
}

public class CreateCategoryViewModel
{
    [Required]
    public string NameEn { get; set; } = string.Empty;
    [Required]
    public string NameAr { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public List<SelectListItem> ParentCategories { get; set; } = [];
}

public class EditCategoryViewModel
{
    public Guid Id { get; set; }
    [Required]
    public string NameEn { get; set; } = string.Empty;
    [Required]
    public string NameAr { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public List<SelectListItem> ParentCategories { get; set; } = [];
}
