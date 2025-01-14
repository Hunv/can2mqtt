using can2mqtt;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Can2Mqtt>();
    })
    .Build();

await host.RunAsync();