using SecurityProject.Dtos;
using SecurityProject.Models;

namespace SecurityProject.Services;
public interface IPostService
{
    public Task<List<Post>> FindPosts();
    public Task<Post?> FindPostById(int id);
    public Task<int> CreatePost(PostDTO data, long authorId);
    public Task<int> UpdatePost(Post post, PostDTO data);
    public Task DeletePost(int id);
}