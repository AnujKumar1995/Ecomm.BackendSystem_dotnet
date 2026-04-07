using OrderOrchestratorService.Data;
using OrderOrchestratorService.Orchestration;
using Serilog;
using Shared;
using Shared.Messaging;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddJwtAuthentication();

// HTTP clients for synchronous communication
var cartServiceUrl = builder.Configuration["ServiceUrls:CartService"] ?? "http://cart.service:5003";
var productDetailServiceUrl = builder.Configuration["ServiceUrls:ProductDetailService"] ?? "http://product-detail_service:5002";

builder.Services.AddHttpClient("CartService", c => c.BaseAddress = new Uri(cartServiceUrl));
builder.Services.AddHttpClient("ProductDetailService", c => c.BaseAddress = new Uri(productDetailServiceUrl));

// RabbitMQ publisher for async communication
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    return RabbitMqPublisher.CreateAsync(rabbitHost, logger).GetAwaiter().GetResult();
});

builder.Services.AddScoped<CheckoutOrchestrator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("Order Orchestrator Service starting on port 5004");
app.Run();
