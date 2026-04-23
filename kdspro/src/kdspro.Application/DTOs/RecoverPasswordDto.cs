using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class RecoverPasswordDto
{
    [Required]
    [MaxLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_]*$")]
    public string Username { get; set; } = "";

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = "";

    [Required]
    [MinLength(32)]
    [MaxLength(200)]
    public string RecoveryKey { get; set; } = "";
}
