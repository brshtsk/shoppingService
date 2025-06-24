using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Contracts.Messaging.Configuration;      // RabbitMqSettings
using OrdersService.Persistence;             // OrdersDbContext
using OrdersService.Application.Services;    // IOrderService, OrderService
using OrdersService.Application.Messaging;   // HostedService’ы
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Читаем строку подключения
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
                       ?? throw new InvalidOperationException("Connection string 'OrdersDb' not found.");

// 2. Регистрируем DbContext с PostgreSQL
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Конфигурация RabbitMQ
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value);

// 4. Регистрируем Application Services
builder.Services.AddScoped<IOrderService, OrderService>();

// 5. HostedService’ы для очередей
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHostedService<PaymentCompletedConsumerService>();

// 6. Контроллеры и SwaggerGen
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrdersService API",
        Version = "v1"
    });
});

var app = builder.Build();

// 7. Swagger UI и применение миграций
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrdersService API v1");
        // при желании c.RoutePrefix = "";
    });

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Applied OrdersDb migrations.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while applying OrdersDb migrations.");
    }
// }

// 8. (Опционально) HTTPS Redirection
// app.UseHttpsRedirection();

// 9. Авторизация, CORS и т.п., если нужно
// app.UseAuthorization();

app.MapControllers();

app.Run();
