using BlueDeep.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BlueDeep.Server.WebApp;

public class WebStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MessageBrokerService>();
        services.AddScoped<TopicService>();
        services.AddScoped<WebStatService>();
    }  
    
    public void Configure(IApplicationBuilder app, WebStatService webStatService)
    {
        app.UseHttpsRedirection();
        app.Run(async (context) =>
        {
            var msg = webStatService.GetWebStatistics();
            await context.Response.WriteAsync(msg);
        });
    }  
}