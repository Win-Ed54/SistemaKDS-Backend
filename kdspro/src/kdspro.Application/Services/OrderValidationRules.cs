namespace kdspro.Application.Services;

public static class OrderValidationRules
{
    public const string QuickServiceMode = "quick-service";
    public const string RestaurantMode = "restaurant";

    public static string NormalizeMode(string? mode) =>
        string.Equals(mode, RestaurantMode, StringComparison.OrdinalIgnoreCase)
            ? RestaurantMode
            : QuickServiceMode;

    public static OrderValidationDefaults GetDefaults(string? mode)
    {
        var normalizedMode = NormalizeMode(mode);

        return normalizedMode == RestaurantMode
            ? new OrderValidationDefaults(45, 120, 30, 25)
            : new OrderValidationDefaults(30, 80, 20, 15);
    }
}

public record OrderValidationDefaults(
    int MaxDistinctItems,
    int MaxTotalUnits,
    int MaxQuantityPerProduct,
    int LargeOrderUnitsWarning
);
