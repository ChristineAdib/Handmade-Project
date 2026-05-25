using HandoraDomain.Models.OrderEntity;
using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.OrderDTOs;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
