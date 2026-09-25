using System.Security.Claims;
using SecurityProject.Services;

namespace SecurityProject.Middlewares;

public class OwnershipMiddleware
{
    private readonly RequestDelegate _next;
    public OwnershipMiddleware(RequestDelegate next) => 
        this._next = next;
    
    public async Task InvokeAsync(HttpContext context)
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
        await this._next.Invoke(context);
    }
}

public static class OwnershipMiddlewareExtensions
{
    public static IApplicationBuilder UseCheckAuthorOwnership(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OwnershipMiddleware>();
    }
}