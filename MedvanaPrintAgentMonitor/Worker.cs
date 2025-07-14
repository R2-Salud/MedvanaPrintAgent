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
        _logger.LogInformation("MedvanaPrintAgentMonitor Worker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MedvanaPrintAgentMonitor Worker running at: {time}", DateTimeOffset.Now);

            bool webServerStatus = false;
            try
            {
                webServerStatus = await _monitoringService.CheckWebServerStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CheckWebServerStatus: {ErrorMessage}", ex.Message);
            }

            bool printEndpointStatus = false;
            try
            {
                printEndpointStatus = await _monitoringService.CheckPrintEndpointAvailability();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CheckPrintEndpointAvailability: {ErrorMessage}", ex.Message);
            }

            _logger.LogInformation("Web Server Status: {WebServerStatus}, Print Endpoint Status: {PrintEndpointStatus}", webServerStatus, printEndpointStatus);

            if (!webServerStatus || !printEndpointStatus)
            {
                _logger.LogWarning("MedvanaPrintAgent is not responding adequately. Attempting to restart the service.");
                _monitoringService.RestartPrintAgentService();
            }

            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }

        _logger.LogInformation("MedvanaPrintAgentMonitor Worker stopping.");
    }
}
