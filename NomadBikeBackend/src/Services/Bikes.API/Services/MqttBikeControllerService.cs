using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace Bikes.API.Services;

public class MqttBikeControllerService : IHostedService
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;

    public MqttBikeControllerService(IConfiguration configuration)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var host = configuration["Mqtt:Host"] ?? "localhost";
        var port = int.Parse(configuration["Mqtt:Port"] ?? "1883");

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("BikesApi_Backend")
            .WithCleanSession()
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient.DisconnectedAsync += async e =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            try { await _mqttClient.ConnectAsync(_options, cancellationToken); }
            catch { /* Retry on next delay */ }
        };

        await _mqttClient.ConnectAsync(_options, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
    }

    public async Task SendLockCommandAsync(string serialNumber, bool isLocked)
    {
        if (!_mqttClient.IsConnected) return;

        var payload = JsonSerializer.Serialize(new { command = isLocked ? "LOCK" : "UNLOCK" });
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"bikes/{serialNumber}/commands")
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.PublishAsync(message);
    }
}
