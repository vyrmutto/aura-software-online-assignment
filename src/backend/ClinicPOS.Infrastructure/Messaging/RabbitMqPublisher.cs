using System.Text;
using System.Text.Json;
using ClinicPOS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ClinicPOS.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private const string ExchangeName = "clinic-pos.events";
    private readonly bool _isConnected;

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                UserName = config["RabbitMQ:Username"] ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest"
            };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
            _isConnected = true;
            _logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ connection failed. Events will not be published.");
            _isConnected = false;
        }
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        if (!_isConnected || _channel == null)
        {
            _logger.LogWarning("RabbitMQ not connected. Skipping publish of {RoutingKey}", routingKey);
            return;
        }

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(ExchangeName, routingKey, body, ct);
        _logger.LogInformation("Published event {RoutingKey}", routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
    }
}
