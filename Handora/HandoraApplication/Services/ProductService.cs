using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;

namespace HandoraApplication.Services;

public class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<ProductResponseDto>> GetProduct(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<PagedResultDto<ProductSummaryDto>>> GetProducts(ProductQueryDto query)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ProductResponseDto>> CreateProduct(CreateProductDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ProductResponseDto>> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteProduct(Guid id)
    {
        throw new NotImplementedException();
    }
}
