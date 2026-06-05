using HandoraApplication.DTOs.OrderDTOs;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.OrderEntity;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[Route("api/delivery-methods")]
[ApiController]
public class DeliveryMethodsController(IUnitOfWork unitOfWork) : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var repo = _unitOfWork.Repository<DeliveryMethod, Guid>();
        var methods = await repo.GetAllAsync();

        var dtos = methods.Select(m => new DeliveryMethodResponseDto(
            m.Id,
            m.ShortName,
            m.DescriptionEn,
            m.DescriptionAr,
            m.DeliveryTimeEn,
            m.DeliveryTimeAr,
            m.Cost,
            m.IsActive
        ));

        return Ok(dtos);
    }
}
