using System.ComponentModel.DataAnnotations;

namespace SecurityProject.Dtos;

public class PostDTO
{
    [Required]
    [StringLength(100)]
    public string Title {get; set;} = default!;

    [Required]
    [StringLength(255)]
    public string Excerpt {get; set;} = default!;
    
    [Required]
    public string Body {get; set;} = default!;
}