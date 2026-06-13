using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [MaxLength(30, ErrorMessage = "El usuario no puede exceder los 30 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]*$", ErrorMessage = "Caracteres no permitidos en el usuario")]
    public string Username { get; set; } = "";

    [MaxLength(100, ErrorMessage = "La contrasena es demasiado larga")]
    public string Password { get; set; } = "";
}
