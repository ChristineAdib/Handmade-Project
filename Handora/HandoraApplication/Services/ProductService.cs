using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services;

public class ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork, IFileService fileService) : IProductService
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFileService _fileService = fileService;

    public async Task<Result<ProductResponseDto>> GetProduct(Guid id)
    {
        var product = await _productRepository.GetProductByIDWithDetailsAsync(id);

        if (product is null)
            return Result<ProductResponseDto>.Failure("Product not found");

        var response = product.Adapt<ProductResponseDto>();
        return Result<ProductResponseDto>.Success(response);
    }

    public async Task<Result<PagedResultDto<ProductSummaryDto>>> GetProducts(ProductQueryDto query)
    {
        var productsQuery = await _productRepository.GetAllProductsQueryAsync();

        // Filtering
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            productsQuery = productsQuery.Where(p =>
                p.TitleEn.ToLower().Contains(search) ||
                p.TitleAr.Contains(search) ||
                (p.DescriptionEn != null && p.DescriptionEn.ToLower().Contains(search)));
        }

        if (query.CategoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.ShopId.HasValue)
            productsQuery = productsQuery.Where(p => p.ShopId == query.ShopId.Value);

        if (query.MinPrice.HasValue)
            productsQuery = productsQuery.Where(p => (p.DiscountPrice ?? p.Price) >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            productsQuery = productsQuery.Where(p => (p.DiscountPrice ?? p.Price) <= query.MaxPrice.Value);

        if (query.MinRating.HasValue)
            productsQuery = productsQuery.Where(p => p.AverageRating >= query.MinRating.Value);

        if (query.IsAdmin == true)
        {
            if (query.Status.HasValue)
                productsQuery = productsQuery.Where(p => p.Status == query.Status.Value);
        }
        else if (query.ShopId.HasValue)
        {
            // الـ buyer بيشوف منتجات شوب معين — يظهرله حتى OutOfStock
            if (query.Status.HasValue)
                productsQuery = productsQuery.Where(p => p.Status == query.Status.Value);
        }
        else
        {
            // الصفحة العادية — مش يظهرله OutOfStock
            productsQuery = productsQuery.Where(p =>
                p.Status == ProductStatus.Active && p.Quantity > 0);
        }

        if (query.Tags != null && query.Tags.Any())
            productsQuery = productsQuery.Where(p => p.Tags.Any(t => query.Tags.Contains(t.Name)));

        // Sorting
        productsQuery = query.SortBy?.ToLower() switch
        {
            "price" => query.SortDescending
                ? productsQuery.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                : productsQuery.OrderBy(p => p.DiscountPrice ?? p.Price),
            "rating" => query.SortDescending
                ? productsQuery.OrderByDescending(p => p.AverageRating)
                : productsQuery.OrderBy(p => p.AverageRating),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.CreatedAt)
        };

        // Pagination
        var totalCount = await productsQuery.CountAsync();
        var items = await productsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResultDto<ProductSummaryDto>
        {
            Items = items.Adapt<List<ProductSummaryDto>>(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Result<PagedResultDto<ProductSummaryDto>>.Success(result);
    }

    public async Task<Result<ProductResponseDto>> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TitleEn = dto.TitleEn,
            TitleAr = dto.TitleAr,
            DescriptionEn = dto.DescriptionEn,
            DescriptionAr = dto.DescriptionAr,
            Price = dto.Price,
            Quantity = dto.Quantity,
            CategoryId = dto.CategoryId,
            ShopId = dto.ShopId,
            Status = ProductStatus.Active
        };

        // Handle tags
        if (dto.Tags != null && dto.Tags.Any())
        {
            var tagRepo = _unitOfWork.Repository<Tag, Guid>();
            var existingTags = await tagRepo.GetAllAsync();
            var existingTagList = await existingTags.Where(t => dto.Tags.Contains(t.Name)).ToListAsync();

            foreach (var tagName in dto.Tags)
            {
                var tag = existingTagList.FirstOrDefault(t => t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { Id = Guid.NewGuid(), Name = tagName };
                    await tagRepo.AddAsync(tag);
                }
                product.Tags.Add(tag);
            }
        }

        // Handle images
        if (dto.Images != null && dto.Images.Any())
        {
            for (int i = 0; i < dto.Images.Count; i++)
            {
                var imageUrl = await _fileService.UploadFileAsync(dto.Images[i], "products");
                product.Images.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ImageUrl = imageUrl,
                    IsMain = i == 0,
                    ProductId = product.Id
                });
            }
        }

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return await GetProduct(product.Id);
    }

    public async Task<Result<ProductResponseDto>> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetProductByIDWithDetailsAsync(id);

        if (product is null)
            return Result<ProductResponseDto>.Failure("Product not found");

        // Update basic properties
        if (dto.TitleEn != null) product.TitleEn = dto.TitleEn;
        if (dto.TitleAr != null) product.TitleAr = dto.TitleAr;
        if (dto.DescriptionEn != null) product.DescriptionEn = dto.DescriptionEn;
        if (dto.DescriptionAr != null) product.DescriptionAr = dto.DescriptionAr;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.DiscountPrice.HasValue) product.DiscountPrice = dto.DiscountPrice.Value;
        if (dto.Quantity.HasValue) product.Quantity = dto.Quantity.Value;
        if (dto.Status.HasValue) product.Status = dto.Status.Value;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;

        product.UpdatedAt = DateTime.UtcNow;

        // Handle tags
        if (dto.Tags != null)
        {
            // Remove tags that are not in the new list
            var tagsToRemove = product.Tags.Where(t => !dto.Tags.Contains(t.Name)).ToList();
            foreach (var tag in tagsToRemove)
            {
                product.Tags.Remove(tag);
            }

            // Add tags that are in the new list but not in the product's tags
            var currentTagNames = product.Tags.Select(t => t.Name).ToList();
            var tagsToAdd = dto.Tags.Where(t => !currentTagNames.Contains(t)).ToList();

            if (tagsToAdd.Any())
            {
                var tagRepo = _unitOfWork.Repository<Tag, Guid>();
                var existingTags = await tagRepo.GetAllAsync();
                var existingTagList = await existingTags.Where(t => tagsToAdd.Contains(t.Name)).ToListAsync();

                foreach (var tagName in tagsToAdd)
                {
                    var tag = existingTagList.FirstOrDefault(t => t.Name == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Id = Guid.NewGuid(), Name = tagName };
                        await tagRepo.AddAsync(tag);
                    }
                    product.Tags.Add(tag);
                }
            }
        }

        // ── Image handling ────────────────────────────────────────────
        // We avoid touching product.Images.Remove() or .Add() because
        // modifying the navigation collection triggers EF Core's
        // relationship fixup, which corrupts change-tracker states.
        // Instead, we operate directly on the DbContext entries.

        // Track IDs of images that existed in the DB before this update
        var originalImageIds = product.Images.Select(i => i.Id).ToHashSet();

        // Track IDs of newly added images so we DON'T reset them to Unchanged
        var newImageIds = new HashSet<Guid>();

        // 1. Delete images that should be removed
        var removedImageIds = new HashSet<Guid>();
        if (dto.RemoveImageIds != null && dto.RemoveImageIds.Any())
        {
            var imagesToRemove = product.Images
                .Where(i => dto.RemoveImageIds.Contains(i.Id))
                .ToList();

            foreach (var image in imagesToRemove)
            {
                await _fileService.DeleteFileAsync(image.ImageUrl);
                _productRepository.RemoveProductImage(image);
                removedImageIds.Add(image.Id);
            }
        }

        // 2. Add new images directly to the context (not the collection)
        if (dto.NewImages != null && dto.NewImages.Any())
        {
            bool hasRemainingImages = product.Images
                .Any(i => !removedImageIds.Contains(i.Id));

            foreach (var file in dto.NewImages)
            {
                var imageUrl = await _fileService.UploadFileAsync(file, "products");
                var newId = Guid.NewGuid();
                var newImage = new ProductImage
                {
                    Id = newId,
                    ImageUrl = imageUrl,
                    IsMain = !hasRemainingImages,
                    ProductId = product.Id
                };
                _productRepository.AddProductImage(newImage);
                newImageIds.Add(newId);
                hasRemainingImages = true;
            }
        }

        // 3. Trigger DetectChanges so Product/Tag property changes are
        //    picked up by the change tracker. NOTE: This also causes
        //    relationship fixup which adds new images into product.Images.
        _productRepository.ForceDetectChanges();

        // 4. Force ONLY pre-existing (non-removed) images to Unchanged.
        //    CRITICAL: Skip new images — they must keep their Added state
        //    so that SaveChanges generates INSERT statements for them.
        foreach (var image in product.Images)
        {
            if (originalImageIds.Contains(image.Id) && !removedImageIds.Contains(image.Id))
            {
                _productRepository.SetImageUnchanged(image);
            }
        }

        // 5. Disable auto-detect so SaveChangesAsync won't re-evaluate
        //    and override our explicit state assignments above.
        _productRepository.DisableAutoDetectChanges();
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        finally
        {
            _productRepository.EnableAutoDetectChanges();
        }

        return await GetProduct(product.Id);
    }

    public async Task<Result> DeleteProduct(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
            return Result.Failure("Product not found");

        await _productRepository.SoftDeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}
