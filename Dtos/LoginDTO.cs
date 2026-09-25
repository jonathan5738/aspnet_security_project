using System.ComponentModel.DataAnnotations;

namespace SecurityProject.Dtos;

public class LoginDTO
{
    [Required]
    public string Email {get; set; } = default!;
    [Required]
    public string Password {get; set;} = default!;

    public void Deconstruct(out string email, out string password)
    {
        email = Email;
        password = Password;
    }
}