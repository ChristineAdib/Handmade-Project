using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface IProductService
{
    Task<Result<ProductResponseDto>> GetProduct(Guid id);
    Task<Result<PagedResultDto<ProductSummaryDto>>> GetProducts(ProductQueryDto query);
    Task<Result<ProductResponseDto>> CreateProduct(CreateProductDto dto);
    Task<Result<ProductResponseDto>> UpdateProduct(Guid id, UpdateProductDto dto);
    Task<Result> DeleteProduct(Guid id);
    Task<Result> ApproveProductAsync(Guid productId);
    Task<Result> RejectProductAsync(Guid productId);

    // Draft approval workflow (for edits on already-live products)
    Task<Result> ApproveDraftAsync(Guid productId);
    Task<Result> RejectDraftAsync(Guid productId);
}
