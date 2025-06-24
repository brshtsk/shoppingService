namespace ApiGateway.Configs;

public class SwaggerEndpointConfig
{
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string GatewayPathPrefix { get; set; } = null!;
    public string ServicePathPrefixToReplace { get; set; } = null!;
}
