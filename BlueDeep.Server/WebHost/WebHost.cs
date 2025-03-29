using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BlueDeep.Server.WebHost;

public static class WebHost
{
    public static IWebHost Create()
    {
        return new WebHostBuilder()
            .UseStartup<WebStartup>()
            .UseKestrel() //tiny web server. It can be replaced with any web server  
            .Build();
    }
}