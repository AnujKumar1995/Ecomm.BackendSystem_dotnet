using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Shared.Middleware;

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

var app = builder.Build();

// UseRouting must be explicit so endpoint execution below is placed correctly
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Local controllers (e.g. AuthController) execute HERE, before Ocelot
app.UseEndpoints(endpoints => endpoints.MapControllers());

// Ocelot proxies all remaining (unmatched) requests
await app.UseOcelot();

Log.Information("API Gateway starting on port 5000");
app.Run();
