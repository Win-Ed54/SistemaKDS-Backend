using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class StockUpdateDto 
{ 
    [Range(0, 999999, ErrorMessage = "Stock debe estar entre 0 y 999999")]
    public int NewStock { get; set; } 
}
