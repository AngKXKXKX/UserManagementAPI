using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

var users = new List<User>();

app.MapGet("/", () => "User Management API is running");

// GET all users
app.MapGet("/users", () => users);

// GET user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// POST create user
app.MapPost("/users", (User newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.Email))
    {
        return Results.BadRequest("Name and Email are required");
    }

    newUser.Id = users.Count + 1;
    users.Add(newUser);

    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT update user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);

    if (user is null)
        return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;

    return Results.Ok(user);
});

// DELETE user
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);

    if (user is null)
        return Results.NotFound();

    users.Remove(user);

    return Results.NoContent();
});

app.Run();

// Middleware classes

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Internal server error.\"}");
        }
    }
}

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ") || authHeader != "Bearer valid-token")
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
            return;
        }
        await _next(context);
    }
}

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation($"Incoming request: {context.Request.Method} {context.Request.Path}");
        await _next(context);
        _logger.LogInformation($"Outgoing response: {context.Response.StatusCode}");
    }
}

// Model
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}