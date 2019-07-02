using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace can2mqtt_core
{
    public class DaemonService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IOptions<DaemonConfig> config;
        private readonly Can2Mqtt c2mConverter = new Can2Mqtt();

        public DaemonService(ILogger<DaemonService> logger, IOptions<DaemonConfig> config)
        {
            this.logger = logger;
            this.config = config;
        }
        
        public void Dispose()
        {
            logger.LogInformation("Disposing " + config.Value.DaemonName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting: " + config.Value.DaemonName);
            _ = c2mConverter.Start(config.Value);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping: " + config.Value.DaemonName);
            return Task.CompletedTask;
        }
    }
}
