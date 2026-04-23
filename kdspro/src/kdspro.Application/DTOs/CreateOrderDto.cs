using System.Collections.Generic;

namespace kdspro.Application.DTOs;
public class CreateOrderDto
{
    public int TableNumber { get; set; }

    public string CustomerName { get; set; } = "";

    public string TakeoutDestination { get; set; } = "";

    public string WaiterName { get; set; } = "";

    public List<CreateOrderItemDto> Items { get; set; } =new ();
}
