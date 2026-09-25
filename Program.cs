using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SecurityProject.Dtos;
using SecurityProject.Services;
using SecurityProject.Utils;
using BC = BCrypt.Net.BCrypt;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration["ConnectionString:DbConnect"];
builder.Services.AddValidation();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var issuer = builder.Configuration.GetValue<string>("JwtIssuer")!;
    var validAudience = builder.Configuration.GetValue<string>("JwtAudience");
    var secret = Convert.FromBase64String(builder.Configuration.GetValue<string>("JwtSecret")!);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,

        ValidIssuer = issuer,
        ValidAudience = validAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secret)
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserPolicy", p => p.RequireRole("User"));
var app = builder.Build();

/*
    *** NOTE FOR THE REVIEWER ***
    to communicate with the database i would have prefer to use Ef core
    which natively support prepared statements (parameterized queries)
    and is less clumsy
*/
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseWhen(context => 
context.Request.Path.StartsWithSegments("/posts") && 
context.Request.Method == "PUT" ||
context.Request.Method == "DELETE", app =>
{
    app.Use(async (context, next) =>
    {
        var postService = context.RequestServices.GetRequiredService<IPostService>();
        var routeValue = context.GetRouteValue("id");
        if (routeValue == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("invalid request");
            return;
        }
        if(!int.TryParse(routeValue.ToString(), out int id))
        {
            
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("invalid request");
            return;
        }
        var foundPost = await postService.FindPostById(id);
        if(foundPost == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("invalid request");
            return;
        }
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("unauthorized");
            return;
        } 
        if(!int.TryParse(sub, out int userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("unauthorized");
            return;  
        }

        if(foundPost.AuthorId != userId)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
        await next.Invoke(context);
    });
});

app.MapPost("/users/register", async (IConfiguration config,
 IUserService userService, [FromBody] UserDTO data) =>
{
    try {
        var userId = await userService.CreateUser(data);
        var token = JwtUtils.GenerateToken(config, "User", userId);
        return Results.Ok(new {tokenType = "bearer", token});
    } catch(Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.BadRequest();
    } 
});

app.MapGet("/users/{id}", async (int id, 
IUserService userService, IEncryptionService encryptionService) =>
{
    try
    {
        var foundUser = await userService.FindById(id);
        if(foundUser == null) return Results.NotFound();
        return Results.Ok(new
        {
            FirstName = encryptionService.Decrypt(foundUser.FirstName),
            LastName = encryptionService.Decrypt(foundUser.LastName),
            Email = encryptionService.Decrypt(foundUser.Email)
        });
    } catch(Exception ex)
    {
        System.Console.WriteLine(ex);
        return Results.BadRequest();
    }
});
app.MapPost("/users/login", async (
    IConfiguration config, 
    IUserService userService, [FromBody] LoginDTO data) =>
{
   var (Email, Password) = data;
   try
    {
        var user = await userService.FindByEmail(Email);
        if(user == null)
            return Results.BadRequest();

        if(!BC.Verify(Password, user.Password))
            return Results.BadRequest(new {error = "authentication failed"});
            
        var token = JwtUtils.GenerateToken(config, "User", user.Id);
        return Results.Ok(new {tokenType = "bearer", token});
    } catch(Exception ex)
    {
        Console.WriteLine(ex);
        return Results.BadRequest(new {error = "something went wrong."});
    }
});

app.MapGet("/posts", async(IPostService postService) =>
{
    var posts = await postService.FindPosts();
    return Results.Ok(posts);
});
app.MapGet("/posts/{id}", async (IPostService postService, int id) =>
{
    try
    {
       var foundPost = await postService.FindPostById(id);
       if(foundPost == null) return Results.NotFound();
       return Results.Ok(foundPost);

    } catch(Exception ex)
    {
        System.Console.WriteLine(ex);
        return Results.BadRequest();
    }
});

app.MapPost("/posts", async(HttpContext context,
 PostDTO data, IPostService postService) =>
{
    var user = context.User;
    if(user == null)
    {
        return Results.BadRequest();
    }
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if(string.IsNullOrEmpty(userId)) 
        return Results.BadRequest(new {error = "unable to proceed"});
    try
    { 
        long.TryParse(userId, out long authorId);
        var result = await postService.CreatePost(data, authorId);
        return Results.Ok(new {link = $"http://localhost:5137/posts/{result}"});
    } catch(Exception ex)
    {
        System.Console.WriteLine(ex);
        return Results.BadRequest(new {error = "unable to proceed."});
    }
}).RequireAuthorization("UserPolicy");

app.MapPut("/posts/{id}", async (
    HttpContext context, 
    IPostService postService, 
    int id, PostDTO data) =>
{
    try
    {
        var foundPost = await postService.FindPostById(id);
        if(foundPost == null)
            return Results.NotFound();
        var result = await postService.UpdatePost(foundPost, data);
        return Results.Ok(new {
            message = "post successfully updated",
            link = $"http://localhost:5137/posts/{result}"
        });
    } catch(Exception ex)
    {
        System.Console.WriteLine(ex);
        return Results.BadRequest();
    }
}).RequireAuthorization("UserPolicy");

app.MapDelete("/posts/{id}", async(int id, IPostService postService) =>
{
    await postService.DeletePost(id);
    return Results.Ok(new {message = "post successfully deleted."});
});

app.MapGet("/users", async (IUserService userService) =>
{
    var users = await userService.FindAll();
    return Results.Ok(users);
});
app.Run();

