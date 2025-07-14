namespace MedvanaPrintAgentMonitor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MonitoringService _monitoringService;

    public Worker(ILogger<Worker> logger, MonitoringService monitoringService)
    {
        _logger = logger;
        _monitoringService = monitoringService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            bool webServerStatus = await _monitoringService.CheckWebServerStatus();
            bool printEndpointStatus = await _monitoringService.CheckPrintEndpointAvailability();

            _logger.LogInformation("Web Server Status: {WebServerStatus}, Print Endpoint Status: {PrintEndpointStatus}", webServerStatus, printEndpointStatus);

            if (!webServerStatus || !printEndpointStatus)
            {
                _logger.LogWarning("MedvanaPrintAgent is not responding adequately. Attempting to restart the service.");
                _monitoringService.RestartPrintAgentService();
            }

            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }
    }
}
