using BlueDeep.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlueDeep.Server.WebHost;

public class WebStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MessageBrokerService>();
        services.AddScoped<TopicService>();
        services.AddSingleton<WebStatService>();
        services.AddRouting();
    }   
    
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, WebStatService webStatService)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.MapWhen((HttpContext context) => context.Request.Method == "GET" && context.Request.Path == "/",
            (IApplicationBuilder builder) => {
                builder.Run(async (context) => {
                    await context.Response.WriteAsync(webStatService.GetWebStatistics());
                });
            });
        
        app.MapWhen((HttpContext context) => context.Request.Method == "GET" && context.Request.Path.Value.ToLower() == "/health",
            (IApplicationBuilder builder) => {
                builder.Run(async (context) => {
                    await context.Response.WriteAsync("OK");
                });
            });
    }  
}