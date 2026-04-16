namespace kdspro.Application.DTOs;

public class MarkOrderPaidDto
{
    public string PaymentMethod { get; set; } = "efectivo";
    public string ReceiptNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "ticket";
    public bool InvoiceRequested { get; set; } = false;
}
