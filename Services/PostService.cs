using Npgsql;
using SecurityProject.Dtos;
using SecurityProject.Models;
using SecurityProject.Utils;

namespace SecurityProject.Services;

public class PostService : IPostService
{
    private readonly string connectionString;
    public PostService(IConfiguration config)
    {
        this.connectionString = config["ConnectionString:DbConnect"]!;
    }
    public async Task<int> CreatePost(PostDTO data, long authorId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        var query = @"INSERT INTO posts(title,excerpt,body,author_id)
         VALUES (@title, @excerpt, @body, @authorId) RETURNING id";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("title", InputSanitizer.Sanitize(data.Title));
        cmd.Parameters.AddWithValue("excerpt", InputSanitizer.Sanitize(data.Excerpt));
        cmd.Parameters.AddWithValue("body", InputSanitizer.Sanitize(data.Body));
        cmd.Parameters.AddWithValue("authorId", authorId);

        using var reader = await cmd.ExecuteReaderAsync();
        
        await reader.ReadAsync();
        return (int) reader.GetInt64(0);
    }

    public async Task DeletePost(int id)
    {
        using var conn = new NpgsqlConnection(this.connectionString);
        await conn.OpenAsync();
        string query = "DELETE FROM posts WHERE id=@id";
        using var cmd = new NpgsqlCommand(query, conn);

        cmd.Parameters.AddWithValue("id", id);     
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Post?> FindPostById(int id)
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = "SELECT * FROM posts WHERE id=@id";
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if(!await reader.ReadAsync())
            throw new Exception("unable to proceed.");
        
        return new Post
        {
            Id = (int) reader.GetInt64(0),
            Title = reader.GetString(1),
            Excerpt = reader.GetString(2),
            Body = reader.GetString(3),
            AuthorId = (int) reader.GetInt64(4)
        };
    }

    public async Task<List<Post>> FindPosts()
    {
        using var conn = new NpgsqlConnection(this.connectionString);
        await conn.OpenAsync();
        string query = "SELECT * FROM posts";
        using var cmd = new NpgsqlCommand(query, conn);

        using var reader = await cmd.ExecuteReaderAsync();
        var posts = new List<Post>();
        while (await reader.ReadAsync())
        {
            posts.Add(new Post
            {
                Id = (int) reader.GetInt64(0),
                Title = reader.GetString(1),
                Excerpt = reader.GetString(2),
                Body = reader.GetString(3),
                AuthorId = (int) reader.GetInt64(4)
            });
        }
        return posts;
    }

    public async Task<int> UpdatePost(Post post, PostDTO data)
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"UPDATE posts SET title=@title,excerpt=@excerpt,body=@body 
        WHERE id=@id RETURNING id";

        var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", post.Id);
        cmd.Parameters.AddWithValue("title", data.Title);
        cmd.Parameters.AddWithValue("excerpt", data.Excerpt);
        cmd.Parameters.AddWithValue("body", data.Body);

        using var reader = await cmd.ExecuteReaderAsync();
        
        await reader.ReadAsync();
        return (int) reader.GetInt64(0);
    }
}