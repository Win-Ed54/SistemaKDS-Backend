using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class KdsSettingsDto
{
    [RegularExpression(@"^(quick-service|restaurant)$", ErrorMessage = "Modo debe ser 'quick-service' o 'restaurant'")]
    public string ServiceMode { get; set; } = "quick-service";

    [Range(5, 100, ErrorMessage = "MaxDistinctItems debe estar entre 5 y 100")]
    public int MaxDistinctItems { get; set; }

    [Range(10, 500, ErrorMessage = "MaxTotalUnits debe estar entre 10 y 500")]
    public int MaxTotalUnits { get; set; }

    [Range(1, 200, ErrorMessage = "MaxQuantityPerProduct debe estar entre 1 y 200")]
    public int MaxQuantityPerProduct { get; set; }

    [Range(1, 500, ErrorMessage = "LargeOrderUnitsWarning debe estar entre 1 y 500")]
    public int LargeOrderUnitsWarning { get; set; }

    public bool TakeoutRequirePrepayment { get; set; }
    public bool RequireCustomerNameForTakeout { get; set; }

    [Range(5, 120, ErrorMessage = "DefaultCleaningMinutes debe estar entre 5 y 120")]
    public int DefaultCleaningMinutes { get; set; }

    [Range(1, 100, ErrorMessage = "MaxPartySize debe estar entre 1 y 100")]
    public int MaxPartySize { get; set; }
}
