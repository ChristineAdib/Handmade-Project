using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.IServices;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HandoraMVC.Controllers;

public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IFileService _fileService;

    public CategoriesController(ICategoryService categoryService, IFileService fileService)
    {
        _categoryService = categoryService;
        _fileService = fileService;
    }

    // GET: /Categories
    public async Task<IActionResult> Index()
    {
        var result = await _categoryService.GetAllCategories();
        if (!result.IsSuccess)
            return View("Error");

        var viewModels = result.Data!.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            NameEn = c.NameEn,
            NameAr = c.NameAr,
            ImageUrl = c.ImageUrl,
            ParentId = c.ParentId,
            SubCategories = c.SubCategories.Select(s => new SubCategoryViewModel
            {
                Id = s.Id,
                NameEn = s.NameEn,
                NameAr = s.NameAr
            }).ToList()
        }).ToList();

        return View(viewModels);
    }

    // GET: /Categories/Create
    public async Task<IActionResult> Create(Guid? parentId)
    {
        var vm = new CreateCategoryViewModel
        {
            ParentId = parentId,
            ParentCategories = await GetParentCategoriesSelectList()
        };
        return View(vm);
    }

    // POST: /Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        string? imageUrl = null;
        if (vm.ImageFile != null && vm.ImageFile.Length > 0)
        {
            try
            {
                imageUrl = await _fileService.UploadFileAsync(vm.ImageFile, "categories");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to upload image: {ex.Message}");
                vm.ParentCategories = await GetParentCategoriesSelectList();
                return View(vm);
            }
        }

        var dto = new CreateCategoryDto
        {
            NameEn = vm.NameEn,
            NameAr = vm.NameAr,
            ImageUrl = imageUrl,
            ParentId = vm.ParentId
        };

        var result = await _categoryService.CreateCategory(dto);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                try { await _fileService.DeleteFileAsync(imageUrl); } catch { /* ignore */ }
            }
            ModelState.AddModelError("", string.Join(", ", result.Errors ?? System.Array.Empty<string>()));
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Edit/id
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _categoryService.GetCategoryById(id);
        if (!result.IsSuccess)
            return NotFound();

        var vm = new EditCategoryViewModel
        {
            Id = result.Data!.Id,
            NameEn = result.Data!.NameEn,
            NameAr = result.Data!.NameAr,
            ImageUrl = result.Data!.ImageUrl,
            ParentId = result.Data!.ParentId,
            ParentCategories = await GetParentCategoriesSelectList()
        };

        return View(vm);
    }

    // POST: /Categories/Edit/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditCategoryViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        string? imageUrl = vm.ImageUrl;
        string? oldImageUrl = vm.ImageUrl;

        if (vm.ImageFile != null && vm.ImageFile.Length > 0)
        {
            try
            {
                imageUrl = await _fileService.UploadFileAsync(vm.ImageFile, "categories");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to upload image: {ex.Message}");
                vm.ParentCategories = await GetParentCategoriesSelectList();
                return View(vm);
            }
        }

        var dto = new UpdateCategoryDto
        {
            NameEn = vm.NameEn,
            NameAr = vm.NameAr,
            ImageUrl = imageUrl,
            ParentId = vm.ParentId
        };

        var result = await _categoryService.UpdateCategory(id, dto);
        if (!result.IsSuccess)
        {
            if (imageUrl != oldImageUrl && !string.IsNullOrEmpty(imageUrl))
            {
                try { await _fileService.DeleteFileAsync(imageUrl); } catch { /* ignore */ }
            }
            ModelState.AddModelError("", string.Join(", ", result.Errors ?? System.Array.Empty<string>()));
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        if (imageUrl != oldImageUrl && !string.IsNullOrEmpty(oldImageUrl))
        {
            try { await _fileService.DeleteFileAsync(oldImageUrl); } catch { /* ignore */ }
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Delete/id
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _categoryService.GetCategoryById(id);
        if (!result.IsSuccess)
            return NotFound();

        var vm = new CategoryViewModel
        {
            Id = result.Data!.Id,
            NameEn = result.Data!.NameEn,
            NameAr = result.Data!.NameAr,
            ImageUrl = result.Data!.ImageUrl
        };

        return View(vm);
    }

    // POST: /Categories/Delete/id
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _categoryService.DeleteCategory(id);
        if (!result.IsSuccess)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    // Helper
    private async Task<List<SelectListItem>> GetParentCategoriesSelectList()
    {
        var result = await _categoryService.GetAllCategories();
        if (!result.IsSuccess) return new List<SelectListItem>();

        return result.Data!.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.NameEn
        }).ToList();
    }
}