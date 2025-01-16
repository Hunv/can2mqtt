using can2mqtt;
using Microsoft.Extensions.Logging.Console;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddConsoleFormatter<Can2MqttLogFormatter, ConsoleFormatterOptions>();
        });

        services.AddHostedService<Can2Mqtt>();
    })
    .Build();

await host.RunAsync();