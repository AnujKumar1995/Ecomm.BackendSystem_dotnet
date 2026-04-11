using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Shared.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// UseRouting must be explicit so endpoint execution below is placed correctly
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ===== AUTH ENDPOINTS (Local) =====

// User Login
app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    if (!ValidateUserCredentials(request.Email, request.Password))
        return Results.Unauthorized();

    var token = GenerateToken(request.Email, "User", app.Configuration);
    return Results.Ok(new LoginResponse
    {
        Token = token,
        Role = "User",
        UserId = Guid.NewGuid().ToString(),
        Email = request.Email
    });
});

// Admin Login
app.MapPost("/api/auth/admin/login", (AdminLoginRequest request) =>
{
    const string AdminSecretKey = "SuperSecureAdminKey123!";
    
    if (request.AdminSecretKey != AdminSecretKey)
        return Results.Unauthorized();

    if (!ValidateAdminCredentials(request.Email, request.Password))
        return Results.Unauthorized();

    var token = GenerateToken(request.Email, "Admin", app.Configuration);
    return Results.Ok(new LoginResponse
    {
        Token = token,
        Role = "Admin",
        UserId = Guid.NewGuid().ToString(),
        Email = request.Email
    });
});

// Logout
app.MapPost("/api/auth/logout", () =>
{
    return Results.Ok(new { message = "Logged out successfully" });
});

// Local controllers (e.g. AuthController) execute HERE, before Ocelot
app.UseEndpoints(endpoints => endpoints.MapControllers());

// Ocelot proxies all remaining (unmatched) requests
await app.UseOcelot();

Log.Information("API Gateway starting on port 5000");
app.Run();

// ===== HELPER METHODS =====
static bool ValidateUserCredentials(string email, string password)
{
    return !string.IsNullOrEmpty(email) && password?.Length > 5;
}

static bool ValidateAdminCredentials(string email, string password)
{
    var validAdmins = new[] { "admin@ecommerce.com" };
    return validAdmins.Contains(email) && password?.Length > 5;
}

static string GenerateToken(string email, string role, IConfiguration configuration)
{
    var jwtSecret = configuration["Jwt:Secret"] ?? "your-super-secret-key-min-32-characters-long!";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role),
        new Claim("UserId", Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: "EcommerceAPI",
        audience: "EcommerceClient",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

