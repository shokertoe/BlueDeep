using BlueDeep.Server.Models;
using BlueDeep.Server.Processors;
using BlueDeep.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BlueDeep.Server.ServerHost;

public static class ServerHost
{
    public static IHost Create(string[] args)
    {
        // BlueDeep Broker Server
        var builder = Host.CreateApplicationBuilder(args);

        //Configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
        builder.Configuration.AddEnvironmentVariables(prefix: "BDS_");

        var serverConfig = new ServerConfig();
        builder.Configuration.GetSection("Server").Bind(serverConfig);

//Logging (Serilog)
        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();
        builder.Logging.AddSerilog();

        //Services
        builder.Services.AddOptions<ServerConfig>()
            .Bind(builder.Configuration.GetSection("Server"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddScoped<TopicService>();
        builder.Services.AddScoped<MessageBrokerService>();
        builder.Services.AddScoped<ClientService>();
        builder.Services.AddScoped<MessageSenderService>();
        builder.Services.AddScoped<SubscribeMessageProcessor>();
        builder.Services.AddScoped<PublishMessageProcessor>();
        builder.Services.AddScoped<AckMessageProcessor>();
        builder.Services.AddHostedService<ServerService>();

        var host = builder.Build();
        return host;
    }
}