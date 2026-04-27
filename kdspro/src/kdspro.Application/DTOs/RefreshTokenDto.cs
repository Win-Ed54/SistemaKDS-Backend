using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token es obligatorio")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Refresh token inválido")]
    public string RefreshToken { get; set; } = "";
}