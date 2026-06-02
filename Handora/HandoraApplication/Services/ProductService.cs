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

        if (query.Status.HasValue)
            productsQuery = productsQuery.Where(p => p.Status == query.Status.Value);

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
            product.Tags.Clear();
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

        // Remove images
        if (dto.RemoveImageIds != null && dto.RemoveImageIds.Any())
        {
            var imagesToRemove = product.Images.Where(i => dto.RemoveImageIds.Contains(i.Id)).ToList();
            foreach (var image in imagesToRemove)
            {
                await _fileService.DeleteFileAsync(image.ImageUrl);
                product.Images.Remove(image);
            }
        }

        // Add new images
        if (dto.NewImages != null && dto.NewImages.Any())
        {
            foreach (var file in dto.NewImages)
            {
                var imageUrl = await _fileService.UploadFileAsync(file, "products");
                product.Images.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ImageUrl = imageUrl,
                    IsMain = !product.Images.Any(),
                    ProductId = product.Id
                });
            }
        }

        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

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
