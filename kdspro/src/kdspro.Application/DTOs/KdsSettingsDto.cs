namespace kdspro.Application.DTOs;

public class KdsSettingsDto
{
    public string ServiceMode { get; set; } = "quick-service";
    public int MaxDistinctItems { get; set; }
    public int MaxTotalUnits { get; set; }
    public int MaxQuantityPerProduct { get; set; }
    public int LargeOrderUnitsWarning { get; set; }
}
