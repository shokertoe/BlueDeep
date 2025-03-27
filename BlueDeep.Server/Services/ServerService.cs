using System.Net;
using System.Net.Sockets;
using BlueDeep.Server.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueDeep.Server.Services;

public class ServerService : BackgroundService
{
    private readonly ILogger<ServerService> _logger;
    private readonly ServerConfig _serverConfig;
    private readonly ClientService _clientService;
    private readonly MessageSenderService _messageSenderService;

    public ServerService(ILogger<ServerService> logger,
        IOptions<ServerConfig> serverConfig,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serverConfig = serverConfig.Value;
        using var scope = serviceScopeFactory.CreateScope();
        _clientService = scope.ServiceProvider.GetRequiredService<ClientService>();
        _messageSenderService = scope.ServiceProvider.GetRequiredService<MessageSenderService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _serverConfig.Port);
        listener.Start();

        _logger.LogInformation("BlueDeep broker server started at {Address}", listener.LocalEndpoint);

        // Start Message processor (sending messages to subscribers)
        _ = Task.Run(async () => await _messageSenderService.MessageSenderStartAsync(), stoppingToken);

        _logger.LogInformation("Ready for incoming connections");

        await Task.Run(async () =>
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = _clientService
                    .StartReceiveDataAsync(client); // Client connection is processing in a separate thread
            }
        }, stoppingToken);
    }
}