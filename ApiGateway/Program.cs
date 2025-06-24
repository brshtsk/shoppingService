using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy;
using Microsoft.OpenApi.Models;
using ApiGateway.Configs;

// namespace ApiGateway.Configs
// {
//     public class SwaggerEndpointConfig
//     {
//         public string Key { get; set; }
//         public string Name { get; set; }
//         public string GatewayPathPrefix { get; set; }
//         public string ServicePathPrefixToReplace { get; set; }
//         // Не храним абсолютный Url, а будем строить относительный: GatewayPathPrefix + "/swagger/v1/swagger.json"
//     }
// }

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем YARP Reverse Proxy, конфигурация подгрузится из appsettings.json: "ReverseProxy" секции
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Swagger: регистрируем API explorer и SwaggerGen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Для самого gateway создаём документ (название может быть любым).
    c.SwaggerDoc("gateway", new OpenApiInfo
    {
        Title = "API Gateway",
        Version = "gateway"
    });
});

var app = builder.Build();

// 3. В Development (или если всегда хотим Swagger UI), включаем Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Добавляем собственный endpoint для gateway SwaggerDoc
        c.SwaggerEndpoint("/swagger/gateway/swagger.json", "API Gateway");

        // Читаем конфигурацию downstream SwaggerEndpoints из appsettings
        var swaggerEndpoints = builder.Configuration
            .GetSection("SwaggerEndpoints")
            .Get<List<SwaggerEndpointConfig>>();

        if (swaggerEndpoints != null)
        {
            foreach (var ep in swaggerEndpoints)
            {
                // Здесь указываем относительный URL через Gateway. 
                // Предполагаем, что ReverseProxy настроен так, 
                // что запрос на /{GatewayPathPrefix}/swagger/v1/swagger.json
                // проксируется к downstream /swagger/v1/swagger.json.
                var url = $"{ep.GatewayPathPrefix}/swagger/v1/swagger.json";
                c.SwaggerEndpoint(url, ep.Name);
            }
        }
    });
}

// 4. Проксируем все запросы по конфигурации YARP
app.MapReverseProxy();

app.Run();
