using Npgsql;
using SecurityProject.Dtos;
using SecurityProject.Models;
using SecurityProject.Utils;
using BC = BCrypt.Net.BCrypt;

namespace SecurityProject.Services;

public class UserService : IUserService
{
    private readonly IConfiguration config;
    private readonly IEncryptionService encryptionService;
    private readonly string connectionString;
    public UserService(IConfiguration configuration,
     IEncryptionService encryptionService)
    {
        this.encryptionService = encryptionService;
        this.config = configuration;
        this.connectionString = configuration["ConnectionString:DbConnect"]!;
    }

    public async Task<int> CreateUser(UserDTO data)
    {
        var (firstName, lastName, email) = EncryptUserData(SanitizeUserData(data));
    
        var hashedPassword = BC.HashPassword(data.Password);
        using var conn = new NpgsqlConnection(connectionString);

        await conn.OpenAsync();
        var query = @"INSERT INTO users (first_name,last_name,email,password)
        VALUES (@firstName,@lastName,@email,@password) RETURNING id";
   
        await using var cmd = new NpgsqlCommand(query, conn);
        
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password", hashedPassword);

        await using var reader = await cmd.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
             throw new Exception("unable to proceed.");

        return (int) reader.GetInt64(0);
    }

    public async Task<List<User>> FindAll()
    {
        var users = new List<User>();
        using var conn = new NpgsqlConnection(this.connectionString);
        conn.Open();
        var query = "SELECT * FROM users";
        using var cmd = new NpgsqlCommand(query, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new User
            {
                Id = (int)reader.GetInt64(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Password = reader.GetString(4)
            });
        }
        return users;
    }

    public async Task<User?> FindByEmail(string email)
    {
        using var conn = new NpgsqlConnection(connectionString);
        var query = @"SELECT * FROM users WHERE email=@email";
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("email", email);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new Exception("unable to proceed.");

        var user = new User
        {
            Id = (int) reader.GetInt64(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Email = reader.GetString(3),
            Password = reader.GetString(4)
        };
        return user;
    }
    private Tuple<string,string,string> EncryptUserData(Tuple<string,string, string> sanitizedData)
    {
        var (firstName, lastName, email) = sanitizedData;
        var fname = this.encryptionService.Encrypt(firstName);
        var lname = this.encryptionService.Encrypt(lastName);
        var encryptedEmail = this.encryptionService.Encrypt(email);

        return new Tuple<string,string,string>(fname, lname, encryptedEmail);
    }

    public Tuple<string,string,string> SanitizeUserData(UserDTO data)
    {
       /* 
        email is not sanitized here, 
        because i already applied the [EmailAddress] 
        attribute in UserDTO
       */ 
        var (firstName, lastName, email, _) = data;
        
        firstName = InputSanitizer.Sanitize(firstName);
        lastName = InputSanitizer.Sanitize(lastName);
        return new Tuple<string, string, string>(firstName, lastName, email);
    }

    public async Task<User?> FindById(int id)
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        var query = "SELECT * FROM users WHERE id=@id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", id);
        var reader = await cmd.ExecuteReaderAsync();

        if(!await reader.ReadAsync())
            throw new Exception("unable to proceed.");

       return new User
        {
            Id = (int) reader.GetInt64(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Email = reader.GetString(3),
            Password = reader.GetString(4)
        }; 
    }
}