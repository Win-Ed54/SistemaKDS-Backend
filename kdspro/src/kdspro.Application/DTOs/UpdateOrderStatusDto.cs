using System;
using System.Collections.Generic;
using kdspro.Domain.Enums;


namespace kdspro.Application.DTOs;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}
