namespace kdspro.Application.Services;

public static class OrderValidationRules
{
    public const string QuickServiceMode = "quick-service";
    public const string RestaurantMode = "restaurant";
    public const int MaxKitchenNoteLength = 160;
    public const int MaxReceiptNumberLength = 40;

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

    public static string NormalizeKitchenNote(string? note)
    {
        var normalized = string.Join(
            " ",
            (note ?? string.Empty)
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        );

        return normalized;
    }

    public static bool IsKitchenNoteAllowed(string? note)
    {
        var normalized = NormalizeKitchenNote(note);

        return normalized.All(character =>
            char.IsLetter(character) ||
            char.IsWhiteSpace(character) ||
            character is ',' or '.' or ';' or ':' or '(' or ')' or '/' or '-' or '!' or '?' or '+');
    }

    public static string NormalizeReceiptNumber(string? value)
    {
        var normalized = string.Join(
            " ",
            (value ?? string.Empty)
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        );

        if (normalized.Length > MaxReceiptNumberLength)
            throw new InvalidOperationException("El numero de comprobante no puede exceder 40 caracteres.");

        if (!normalized.All(character =>
            char.IsLetterOrDigit(character) ||
            char.IsWhiteSpace(character) ||
            character is '.' or '/' or '-' or '#'))
        {
            throw new InvalidOperationException("El numero de comprobante contiene caracteres no permitidos.");
        }

        return normalized;
    }
}

public record OrderValidationDefaults(
    int MaxDistinctItems,
    int MaxTotalUnits,
    int MaxQuantityPerProduct,
    int LargeOrderUnitsWarning
);
