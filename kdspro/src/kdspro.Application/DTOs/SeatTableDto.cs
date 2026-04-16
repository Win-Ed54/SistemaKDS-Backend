namespace kdspro.Application.DTOs;

public class SeatTableDto
{
    public int PartySize { get; set; }
    public int EstimatedDiningMinutes { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AssignedWaiterId { get; set; } = string.Empty;
    public string AssignedWaiterName { get; set; } = string.Empty;
}
