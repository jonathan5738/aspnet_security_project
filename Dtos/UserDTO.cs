using System.ComponentModel.DataAnnotations;

namespace SecurityProject.Dtos;

public class UserDTO
{
    [Required]
    [StringLength(40)]
    public string FirstName {get; set;} = default!;
    [Required]
    [StringLength(40)]
    public string LastName {get; set;} = default!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email {get; set;} = default!;

    [Required]
    [StringLength(50)] 
    public string Password {get; set;} = default!;
    public void Deconstruct(out string firstName, out string lastName, 
    out string email, out string password)
    {
        firstName = FirstName;
        lastName = LastName;
        email = Email;
        password = Password;
    }
}