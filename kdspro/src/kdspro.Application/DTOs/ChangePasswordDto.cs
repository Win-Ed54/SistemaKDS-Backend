using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contrasena actual es obligatoria.")]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "La contrasena actual es obligatoria.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "La nueva contrasena es obligatoria.")]
    [StringLength(120, MinimumLength = 8, ErrorMessage = "La nueva contrasena debe tener al menos 8 caracteres.")]
    public string NewPassword { get; set; } = "";
}
