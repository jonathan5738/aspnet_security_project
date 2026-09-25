using System.ComponentModel.DataAnnotations;

namespace SecurityProject.Dtos;

public class PostDTO
{
    [Required]
    public string Title {get; set;} = default!;
    [Required]
    public string Excerpt {get; set;} = default!;
    [Required]
    public string Body {get; set;} = default!;
}