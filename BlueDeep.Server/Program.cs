using BlueDeep.Server;
using BlueDeep.Server.Models;
using BlueDeep.Server.Processors;
using BlueDeep.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(args);

//Settings
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables(prefix: "BDS_");

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

builder.Services.AddSingleton<TopicService>();
builder.Services.AddScoped<MessageBrokerService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<MessageSenderService>();
builder.Services.AddScoped<SubscribeMessageProcessor>();
builder.Services.AddScoped<PublishMessageProcessor>();
builder.Services.AddHostedService<ServerService>();


//Start server
var host = builder.Build();
host.RunAsync();

//Stop server on Enter
Console.WriteLine("Press Enter to stop server...");
Console.ReadLine();
await host.StopAsync();