using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El nombre de usurio es obligatorio")]
    [MaxLength(30, ErrorMessage = "El usuario no puede exceder los 30 caracteres ")]
    [RegularExpression(@"^[a-zA-Z0-9_]*$", ErrorMessage = "Caracteres no permitidos en el usuario")]
    public string Username {get; set;} = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres")]
    [MaxLength(100, ErrorMessage ="La contraseña es demasiada larga")]
    public string Password {get; set;} = "";
}