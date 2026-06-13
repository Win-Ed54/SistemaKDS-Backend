using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 40 caracteres.")]
    public string Username { get; set; } = "";

    [StringLength(80, ErrorMessage = "El nombre no puede exceder 80 caracteres.")]
    public string? FullName { get; set; }

    [StringLength(120, ErrorMessage = "El correo no puede exceder 120 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no es valido.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [StringLength(20, ErrorMessage = "El rol no es valido.")]
    public string Role { get; set; } = "";

    [StringLength(20, ErrorMessage = "El alcance no es valido.")]
    public string? ServiceScope { get; set; } = "hybrid";

    public bool IsDemoAccount { get; set; }
}
