using BlueDeep.Server;
using BlueDeep.Server.Models;
using BlueDeep.Server.Processors;
using BlueDeep.Server.Services;
using BlueDeep.Server.WebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

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
builder.Services.AddHostedService<ServerService>();

var host = builder.Build();
host.RunAsync();

//BlueDeep web host
IWebHost? webHostBuilder = null;
if (serverConfig.UseWebServer is true)
{
    webHostBuilder = new WebHostBuilder()
        .UseStartup<WebStartup>()
        .UseKestrel() //tiny web server. It can be replaced with any web server  
        .Build();
    await webHostBuilder.RunAsync(); 
}

await host.StopAsync();
Console.WriteLine("BlueDeep broker server stopped");