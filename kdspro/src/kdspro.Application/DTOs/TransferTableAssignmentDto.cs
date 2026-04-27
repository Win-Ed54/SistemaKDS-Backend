using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class TransferTableAssignmentDto
{
    [Range(1, 99, ErrorMessage = "Número de mesa destino debe estar entre 1 y 99")]
    public int TargetTableNumber { get; set; }
}
