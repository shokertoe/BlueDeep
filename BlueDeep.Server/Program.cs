using BlueDeep.Server.Models;
using BlueDeep.Server.ServerHost;
using BlueDeep.Server.WebHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var serverHost = ServerHost.Create(args);
_ = serverHost.RunAsync();

//BlueDeep web host
var serverConfig = serverHost.Services.GetService<IOptions<ServerConfig>>()?.Value ??
                   new ServerConfig() { Port = 9090, UseWebServer = false };
if (serverConfig.UseWebServer)
{
    var webHost = WebHost.Create();
    await webHost.RunAsync();
}
else
{
    Console.ReadLine();
}


await serverHost.StopAsync();
Console.WriteLine("BlueDeep broker server stopped");