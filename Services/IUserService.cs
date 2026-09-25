using SecurityProject.Dtos;
using SecurityProject.Models;

namespace SecurityProject.Services;

public interface IUserService
{
    public Task<List<User>> FindAll();
    public Task<User?> FindByEmail(string email);
    public Task<int> CreateUser(UserDTO data);
    public Task<User?> FindById(int id);
}