using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Contracts.Messaging.Configuration;      // RabbitMqSettings
using PaymentsService.Persistence;             // PaymentsDbContext
using PaymentsService.Application.Services;    // IPaymentsService, PaymentsService
using PaymentsService.Application.Messaging;   // HostedService’ы
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Читаем строку подключения
var connectionString = builder.Configuration.GetConnectionString("PaymentsDb")
                       ?? throw new InvalidOperationException("Connection string 'PaymentsDb' not found.");

// 2. Регистрируем DbContext с PostgreSQL
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Конфигурация RabbitMQ
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value);

// 4. Регистрируем Application Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();

// 5. HostedService’ы для очередей
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHostedService<OrderCreatedConsumerService>();

// 6. Контроллеры и SwaggerGen
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PaymentsService API",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentsService API v1");
        // при желании c.RoutePrefix = "";
    });

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Applied PaymentsDb migrations.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while applying PaymentsDb migrations.");
    }
// }

// 8. (Опционально) HTTPS Redirection
// app.UseHttpsRedirection();

// 9. Авторизация, CORS и т.п., если нужно
// app.UseAuthorization();

app.MapControllers();

app.Run();
