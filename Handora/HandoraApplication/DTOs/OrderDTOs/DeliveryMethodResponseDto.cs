namespace HandoraApplication.DTOs.OrderDTOs;

public record DeliveryMethodResponseDto(
    Guid Id,
    string ShortName,
    string DescriptionEn,
    string DescriptionAr,
    string DeliveryTimeEn,
    string DeliveryTimeAr,
    decimal Cost,
    bool IsActive
);
