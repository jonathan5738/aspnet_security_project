using System.ComponentModel.DataAnnotations.Schema;

namespace SecurityProject.Models;

public class User
{
    [Column("id")]
    public int Id {get; set;}
    [Column("first_name")]
    public string FirstName  {get; set;} = default!;
    [Column("last_name")]
    public string LastName {get; set;} = default!;

    [Column("email")]
    public string Email  {get; set;} = default!;

    [Column("password")]
    public string Password {get; set;} = default!;
}