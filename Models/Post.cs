using System.ComponentModel.DataAnnotations.Schema;
namespace SecurityProject.Models;

public class Post
{
    [Column("id")]
    public int Id {get; set;}

    [Column("title")]
    public string Title {get; set;} = default!;
    
    [Column("excerpt")]
    public string Excerpt {get; set;} = default!;

    [Column("body")]
    public string Body {get; set;} = default!;

    [Column("author_id")]
    public int AuthorId {get; set;}
}