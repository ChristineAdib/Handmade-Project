using HandoraApplication.AI.Interfaces;
using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.FollowEntities;
using HandoraDomain.Models.ShopEntities;

namespace HandoraApplication.Services;

public class ProductService(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IFileService fileService,
    IAuthRepository authRepository,
    INotificationService notificationService,
    IProductIndexerService productIndexerService) : IProductService
    INotificationService notificationService) : IProductService
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFileService _fileService = fileService;
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IProductIndexerService _productIndexerService = productIndexerService;

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

        if (query.OnlyOnePiece == true)
            productsQuery = productsQuery.Where(p => p.IsOnePiece);

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
        var categoryRepo = _unitOfWork.Repository<Category, Guid>();
        var parentCategory = await categoryRepo.GetByIdAsync(dto.CategoryId);

        if (parentCategory == null || parentCategory.ParentId != null || parentCategory.IsDeleted)
        {
            return Result<ProductResponseDto>.Failure("Please select a valid parent category.");
        }

        var parentHasSubcategories = await (await categoryRepo.GetAllAsync())
            .AnyAsync(c => c.ParentId == dto.CategoryId && !c.IsDeleted);

        Guid targetCategoryId;
        if (parentHasSubcategories)
        {
            if (dto.SubCategoryId == null)
            {
                return Result<ProductResponseDto>.Failure("Please select a valid subcategory under the chosen category.");
            }
            var subCategory = await categoryRepo.GetByIdAsync(dto.SubCategoryId.Value);
            if (subCategory == null || subCategory.ParentId != dto.CategoryId || subCategory.IsDeleted)
            {
                return Result<ProductResponseDto>.Failure("Please select a valid subcategory under the chosen category.");
            }
            targetCategoryId = dto.SubCategoryId.Value;
        }
        else
        {
            targetCategoryId = dto.CategoryId;
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TitleEn = dto.TitleEn,
            TitleAr = dto.TitleAr,
            DescriptionEn = dto.DescriptionEn,
            DescriptionAr = dto.DescriptionAr,
            Price = dto.Price,
            Quantity = dto.Quantity,
            IsOnePiece = dto.Quantity == 1,
            CategoryId = targetCategoryId,
            ShopId = dto.ShopId,
            Status = ProductStatus.Inactive,
            IsActive = false
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

        // Handle AR / 3D model
        if (dto.ArModel != null)
        {
            var modelUrl = await _fileService.UploadRawFileAsync(dto.ArModel, "products/models");
            product.ArModelUrl = modelUrl;
        }

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Notify admins (Scenario 3)
        try
        {
            var admins = await _authRepository.GetUsersInRoleAsync(AppRoles.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.SendAsync(new SendNotificationDto
                {
                    UserId = admin.Id,
                    TitleEn = "New Product Submitted",
                    TitleAr = "تم تقديم منتج جديد",
                    MessageEn = $"Product '{product.TitleEn}' has been submitted and is pending review.",
                    MessageAr = $"تم تقديم المنتج '{product.TitleAr}' وهو قيد المراجعة.",
                    Type = NotificationType.ProductSubmitted,
                    ReferenceId = product.Id,
                    ReferenceType = "Product"
                });
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        return await GetProduct(product.Id);
    }

    public async Task<Result<ProductResponseDto>> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetProductByIDWithDetailsAsync(id);

        if (product is null)
            return Result<ProductResponseDto>.Failure("Product not found");

        Result<ProductResponseDto> updateResult;

        // ── BRANCHING: Live products get a draft, non-live products get direct edits ──
        if (product.IsActive)
        {
            updateResult = await CreateOrUpdateDraft(product, dto);
        }
        else
        {
            // Product is NOT yet live — apply changes directly (existing behavior)
            updateResult = await ApplyDirectUpdate(product, dto);
        }

        if (updateResult.IsSuccess)
        {
            // Notify admins of the update (Scenario 11)
            try
            {
                var admins = await _authRepository.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "Product Updated",
                        TitleAr = "تم تحديث المنتج",
                        MessageEn = $"Product '{product.TitleEn}' has been updated and is pending review.",
                        MessageAr = $"تم تحديث المنتج '{product.TitleAr}' وهو قيد المراجعة.",
                        Type = NotificationType.ProductUpdated,
                        ReferenceId = product.Id,
                        ReferenceType = "Product"
                    });
                }
            }
            catch (System.Exception)
            {
                // Ignore
            }
        }

        return updateResult;
    }

    /// <summary>
    /// Applies changes directly to a non-live product (existing behavior for first-time edits).
    /// </summary>
    private async Task<Result<ProductResponseDto>> ApplyDirectUpdate(Product product, UpdateProductDto dto)
    {
        // Map properties from DTO to the existing product entity
        dto.Adapt(product);

        // 1. Category Logic from main branch
        if (dto.CategoryId.HasValue || dto.SubCategoryId.HasValue)
        {
            var categoryId = dto.CategoryId ?? product.Category?.ParentId ?? product.CategoryId;
            var subCategoryId = dto.SubCategoryId;

            var categoryRepo = _unitOfWork.Repository<Category, Guid>();
            var parentCategory = await categoryRepo.GetByIdAsync(categoryId);

            if (parentCategory == null || parentCategory.ParentId != null || parentCategory.IsDeleted)
            {
                return Result<ProductResponseDto>.Failure("Please select a valid parent category.");
            }

            var parentHasSubcategories = await (await categoryRepo.GetAllAsync())
                .AnyAsync(c => c.ParentId == categoryId && !c.IsDeleted);

            Guid targetCategoryId;
            if (parentHasSubcategories)
            {
                var subId = subCategoryId ?? (product.Category?.ParentId != null ? product.CategoryId : Guid.Empty);
                var subCategory = await categoryRepo.GetByIdAsync(subId);
                if (subCategory == null || subCategory.ParentId != categoryId || subCategory.IsDeleted)
                {
                    return Result<ProductResponseDto>.Failure("Please select a valid subcategory under the chosen category.");
                }
                targetCategoryId = subId;
            }
            else
            {
                targetCategoryId = categoryId;
            }

            product.CategoryId = targetCategoryId;
        }

        // 2. Map properties manually from main (Instead of dto.Adapt(product) so we don't overwrite CategoryId)
        // Update basic properties
        if (dto.TitleEn != null) product.TitleEn = dto.TitleEn;
        if (dto.TitleAr != null) product.TitleAr = dto.TitleAr;
        if (dto.DescriptionEn != null) product.DescriptionEn = dto.DescriptionEn;
        if (dto.DescriptionAr != null) product.DescriptionAr = dto.DescriptionAr;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.DiscountPrice.HasValue) product.DiscountPrice = dto.DiscountPrice.Value;
        if (dto.Quantity.HasValue)
        {
            product.Quantity = dto.Quantity.Value;
            product.IsOnePiece = dto.Quantity.Value == 1;
        }
        if (dto.Status.HasValue) product.Status = dto.Status.Value;

        // 3. Your Logic from Feature
        if (dto.RemoveArModel == true)
        {
            if (!string.IsNullOrEmpty(product.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(product.ArModelUrl);
                product.ArModelUrl = null;
            }
        }
        if (dto.ArModel != null)
        {
            if (!string.IsNullOrEmpty(product.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(product.ArModelUrl);
            }
            var modelUrl = await _fileService.UploadRawFileAsync(dto.ArModel, "products/models");
            product.ArModelUrl = modelUrl;
        }
     

        product.UpdatedAt = DateTime.UtcNow;
        product.IsActive = false; // Ensure product is pending review after update

        // Handle tags
        if (dto.Tags != null)
        {
            var tagsToRemove = product.Tags.Where(t => !dto.Tags.Contains(t.Name)).ToList();
            foreach (var tag in tagsToRemove)
            {
                product.Tags.Remove(tag);
            }

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
        var originalImageIds = product.Images.Select(i => i.Id).ToHashSet();
        var newImageIds = new HashSet<Guid>();

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

        _productRepository.ForceDetectChanges();

        foreach (var image in product.Images)
        {
            if (originalImageIds.Contains(image.Id) && !removedImageIds.Contains(image.Id))
            {
                _productRepository.SetImageUnchanged(image);
            }
        }

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


    /// <summary>
    /// Creates or updates a ProductDraft for a live product. The live product remains unchanged.
    /// </summary>
    private async Task<Result<ProductResponseDto>> CreateOrUpdateDraft(Product product, UpdateProductDto dto)
    {
        // Find existing pending draft or create new one
        var draft = await _productRepository.GetPendingDraftByProductIdAsync(product.Id);
        bool isNewDraft = draft == null;

        if (isNewDraft)
        {
            draft = new ProductDraft
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Status = DraftStatus.PendingReview
            };
        }

        // Populate draft fields from DTO
        if (dto.TitleEn != null) draft!.TitleEn = dto.TitleEn;
        if (dto.TitleAr != null) draft!.TitleAr = dto.TitleAr;
        if (dto.DescriptionEn != null) draft!.DescriptionEn = dto.DescriptionEn;
        if (dto.DescriptionAr != null) draft!.DescriptionAr = dto.DescriptionAr;
        if (dto.Price.HasValue) draft!.Price = dto.Price.Value;
        if (dto.DiscountPrice.HasValue) draft!.DiscountPrice = dto.DiscountPrice.Value;
        if (dto.Quantity.HasValue) draft!.Quantity = dto.Quantity.Value;
        if (dto.CategoryId.HasValue) draft!.CategoryId = dto.CategoryId.Value;

        // Serialize tags
        if (dto.Tags != null)
            draft!.ProposedTagsJson = JsonSerializer.Serialize(dto.Tags);

        // Handle image uploads — upload immediately, store URLs in draft
        if (dto.NewImages != null && dto.NewImages.Any())
        {
            var newUrls = new List<string>();
            // Preserve any previously uploaded draft images
            if (!string.IsNullOrEmpty(draft!.NewImageUrlsJson))
            {
                var existing = JsonSerializer.Deserialize<List<string>>(draft.NewImageUrlsJson);
                if (existing != null) newUrls.AddRange(existing);
            }

            foreach (var file in dto.NewImages)
            {
                var imageUrl = await _fileService.UploadFileAsync(file, "products");
                newUrls.Add(imageUrl);
            }
            draft.NewImageUrlsJson = JsonSerializer.Serialize(newUrls);
        }

        // Serialize image removal IDs
        if (dto.RemoveImageIds != null && dto.RemoveImageIds.Any())
        {
            var removeIds = new List<Guid>();
            if (!string.IsNullOrEmpty(draft!.RemoveImageIdsJson))
            {
                var existing = JsonSerializer.Deserialize<List<Guid>>(draft.RemoveImageIdsJson);
                if (existing != null) removeIds.AddRange(existing);
            }
            removeIds.AddRange(dto.RemoveImageIds);
            draft.RemoveImageIdsJson = JsonSerializer.Serialize(removeIds.Distinct().ToList());
        }

        // Handle AR model changes in draft
        if (dto.RemoveArModel == true)
        {
            draft.RemoveArModel = true;
            // Delete any draft uploaded URL if it existed
            if (!string.IsNullOrEmpty(draft.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(draft.ArModelUrl);
                draft.ArModelUrl = null;
            }
        }
        if (dto.ArModel != null)
        {
            // If we already uploaded a new model in this draft, delete it from Cloudinary first
            if (!string.IsNullOrEmpty(draft.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(draft.ArModelUrl);
            }
            var modelUrl = await _fileService.UploadRawFileAsync(dto.ArModel, "products/models");
            draft.ArModelUrl = modelUrl;
            draft.RemoveArModel = false;
        }

        draft!.UpdatedAt = DateTime.UtcNow;

        if (isNewDraft)
            await _productRepository.AddDraftAsync(draft);

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

        _ = Task.Run(() => _productIndexerService.IndexAllProductsAsync());

        return Result.Success();
    }

    public async Task<Result> ApproveProductAsync(Guid productId)
    {
        var product = await _productRepository.GetProductByIDWithDetailsAsync(productId);
        if (product == null)
            return Result.Failure("Product not found");

        product.IsActive = true;
        // Optionally update Status to Active
        product.Status = ProductStatus.Active;

        await _unitOfWork.SaveChangesAsync();

        // Notify seller and followers (Scenario 4 & 6)
        try
        {
            var shop = product.Shop;
            if (shop == null)
            {
                shop = await _unitOfWork.Repository<Shop, Guid>().GetByIdAsync(product.ShopId);
            }
            if (shop != null)
            {
                await _notificationService.SendAsync(new SendNotificationDto
                {
                    UserId = shop.OwnerId,
                    TitleEn = "Product Approved",
                    TitleAr = "تمت الموافقة على المنتج",
                    MessageEn = $"Your product '{product.TitleEn}' has been approved and is now active.",
                    MessageAr = $"تمت الموافقة على منتجك '{product.TitleAr}' وهو الآن نشط.",
                    Type = NotificationType.ProductApproved,
                    ReferenceId = product.Id,
                    ReferenceType = "Product"
                });

                // Notify followers (Scenario 6)
                var followRepo = _unitOfWork.Repository<Follow, Guid>();
                var followersQuery = await followRepo.GetAllAsNoTracking();
                var followers = await followersQuery
                    .Where(f => f.ShopId == product.ShopId)
                    .ToListAsync();

                foreach (var f in followers)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = f.UserId,
                        TitleEn = $"New Product from {shop.Name}",
                        TitleAr = $"منتج جديد من {shop.Name}",
                        MessageEn = $"A new product '{product.TitleEn}' has been added to {shop.Name}.",
                        MessageAr = $"تمت إضافة منتج جديد '{product.TitleAr}' إلى {shop.Name}.",
                        Type = NotificationType.NewProductFromFollowedShop,
                        ReferenceId = product.Id,
                        ReferenceType = "Product"
                    });
                }
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        _ = Task.Run(() => _productIndexerService.IndexAllProductsAsync());

        return Result.Success();
    }

    public async Task<Result> RejectProductAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            return Result.Failure("Product not found");

        product.IsActive = false;
        product.Status = ProductStatus.Inactive;

        await _unitOfWork.SaveChangesAsync();

        // Notify seller (Scenario 5)
        try
        {
            var shop = await _unitOfWork.Repository<Shop, Guid>().GetByIdAsync(product.ShopId);
            if (shop != null)
            {
                await _notificationService.SendAsync(new SendNotificationDto
                {
                    UserId = shop.OwnerId,
                    TitleEn = "Product Rejected",
                    TitleAr = "تم رفض المنتج",
                    MessageEn = $"Your product '{product.TitleEn}' has been rejected by the administrator.",
                    MessageAr = $"تم رفض منتجك '{product.TitleAr}' من قبل المسؤول.",
                    Type = NotificationType.ProductRejected,
                    ReferenceId = product.Id,
                    ReferenceType = "Product"
                });
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        _ = Task.Run(() => _productIndexerService.IndexAllProductsAsync());

        return Result.Success();
    }

    public async Task<Result> ApproveDraftAsync(Guid productId)
    {
        var product = await _productRepository.GetProductByIDWithDetailsAsync(productId);
        if (product == null)
            return Result.Failure("Product not found");

        var draft = await _productRepository.GetPendingDraftByProductIdAsync(productId);
        if (draft == null)
            return Result.Failure("No pending draft found for this product");

        // Copy non-null draft fields onto the live product
        if (draft.TitleEn != null) product.TitleEn = draft.TitleEn;
        if (draft.TitleAr != null) product.TitleAr = draft.TitleAr;
        if (draft.DescriptionEn != null) product.DescriptionEn = draft.DescriptionEn;
        if (draft.DescriptionAr != null) product.DescriptionAr = draft.DescriptionAr;
        if (draft.Price.HasValue) product.Price = draft.Price.Value;
        if (draft.DiscountPrice.HasValue) product.DiscountPrice = draft.DiscountPrice.Value;
        if (draft.Quantity.HasValue)
        {
            product.Quantity = draft.Quantity.Value;
            product.IsOnePiece = draft.Quantity.Value == 1;
        }
        if (draft.CategoryId.HasValue) product.CategoryId = draft.CategoryId.Value;

        product.UpdatedAt = DateTime.UtcNow;

        // Apply tag changes
        if (!string.IsNullOrEmpty(draft.ProposedTagsJson))
        {
            var proposedTags = JsonSerializer.Deserialize<List<string>>(draft.ProposedTagsJson) ?? new List<string>();

            // Remove tags not in proposed list
            var tagsToRemove = product.Tags.Where(t => !proposedTags.Contains(t.Name)).ToList();
            foreach (var tag in tagsToRemove)
                product.Tags.Remove(tag);

            // Add new tags
            var currentTagNames = product.Tags.Select(t => t.Name).ToList();
            var tagsToAdd = proposedTags.Where(t => !currentTagNames.Contains(t)).ToList();
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

        // Apply image removals
        if (!string.IsNullOrEmpty(draft.RemoveImageIdsJson))
        {
            var removeIds = JsonSerializer.Deserialize<List<Guid>>(draft.RemoveImageIdsJson) ?? new List<Guid>();
            var imagesToRemove = product.Images.Where(i => removeIds.Contains(i.Id)).ToList();
            foreach (var image in imagesToRemove)
            {
                await _fileService.DeleteFileAsync(image.ImageUrl);
                _productRepository.RemoveProductImage(image);
            }
        }

        // Apply new images
        if (!string.IsNullOrEmpty(draft.NewImageUrlsJson))
        {
            var newUrls = JsonSerializer.Deserialize<List<string>>(draft.NewImageUrlsJson) ?? new List<string>();
            bool hasRemainingImages = product.Images.Any();

            foreach (var url in newUrls)
            {
                var newImage = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ImageUrl = url,
                    IsMain = !hasRemainingImages,
                    ProductId = product.Id
                };
                _productRepository.AddProductImage(newImage);
                hasRemainingImages = true;
            }
        }

        // Apply AR model changes from draft
        if (draft.RemoveArModel == true)
        {
            if (!string.IsNullOrEmpty(product.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(product.ArModelUrl);
                product.ArModelUrl = null;
            }
        }
        if (draft.ArModelUrl != null)
        {
            if (!string.IsNullOrEmpty(product.ArModelUrl))
            {
                await _fileService.DeleteFileAsync(product.ArModelUrl);
            }
            product.ArModelUrl = draft.ArModelUrl;
        }

        // Delete the draft
        _productRepository.RemoveDraft(draft);

        await _unitOfWork.SaveChangesAsync();

        // Notify seller (Scenario 12)
        try
        {
            var shop = product.Shop;
            if (shop == null)
            {
                shop = await _unitOfWork.Repository<Shop, Guid>().GetByIdAsync(product.ShopId);
            }
            if (shop != null)
            {
                await _notificationService.SendAsync(new SendNotificationDto
                {
                    UserId = shop.OwnerId,
                    TitleEn = "Product Update Approved",
                    TitleAr = "تمت الموافقة على تحديث المنتج",
                    MessageEn = $"The update for your product '{product.TitleEn}' has been approved.",
                    MessageAr = $"تمت الموافقة على تحديث منتجك '{product.TitleAr}'.",
                    Type = NotificationType.ProductUpdateApproved,
                    ReferenceId = product.Id,
                    ReferenceType = "Product"
                });
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        _ = Task.Run(() => _productIndexerService.IndexAllProductsAsync());

        return Result.Success();
    }

    public async Task<Result> RejectDraftAsync(Guid productId)
    {
        var draft = await _productRepository.GetPendingDraftByProductIdAsync(productId);
        if (draft == null)
            return Result.Failure("No pending draft found for this product");

        // Clean up any images that were uploaded for this draft
        if (!string.IsNullOrEmpty(draft.NewImageUrlsJson))
        {
            var newUrls = JsonSerializer.Deserialize<List<string>>(draft.NewImageUrlsJson) ?? new List<string>();
            foreach (var url in newUrls)
            {
                await _fileService.DeleteFileAsync(url);
            }
        }

        // Delete the draft
        _productRepository.RemoveDraft(draft);

        await _unitOfWork.SaveChangesAsync();

        // Notify seller (Scenario 13)
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                var shop = await _unitOfWork.Repository<Shop, Guid>().GetByIdAsync(product.ShopId);
                if (shop != null)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = shop.OwnerId,
                        TitleEn = "Product Update Rejected",
                        TitleAr = "تم رفض تحديث المنتج",
                        MessageEn = $"The update for your product '{product.TitleEn}' has been rejected.",
                        MessageAr = $"تم رفض تحديث منتجك '{product.TitleAr}'.",
                        Type = NotificationType.ProductUpdateRejected,
                        ReferenceId = product.Id,
                        ReferenceType = "Product"
                    });
                }
            }
        }
        catch (System.Exception)
        {
            // Ignore
        }

        return Result.Success();
    }
}
