using BlueDeep.Server;
using BlueDeep.Server.Broker;
using BlueDeep.Server.Processors;
using BlueDeep.Server.Services;
using BlueDeep.Server.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
builder.Services.AddSingleton<MessageBroker>();
builder.Services.AddSingleton<TopicSubscribersBag>();
builder.Services.AddSingleton<SubscribeProcessor>();
builder.Services.AddSingleton<PublishProcessor>();
builder.Services.AddHostedService<ServerService>();

//Start server
var host = builder.Build();
host.RunAsync();

//Stop server on Enter
Console.WriteLine("Press Enter to stop server...");
Console.ReadLine();
await host.StopAsync();