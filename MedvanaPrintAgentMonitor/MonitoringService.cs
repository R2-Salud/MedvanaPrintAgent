using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

public class MonitoringService
{
    private readonly ILogger<MonitoringService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public MonitoringService(ILogger<MonitoringService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:3110"; // The main service runs on port 3110
    }

    public async Task<bool> CheckWebServerStatus()
    {
        try
        {
            _logger.LogInformation("Checking web server status at {BaseUrl}...", _baseUrl);
            HttpResponseMessage response = await _httpClient.GetAsync(_baseUrl);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Web server is responding. Status Code: {StatusCode}", response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogWarning("Web server is not responding as expected. Status Code: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error checking web server status: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking web server status: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<bool> CheckPrintEndpointAvailability()
    {
        try
        {
            string printEndpointUrl = $"{_baseUrl}/print";
            _logger.LogInformation("Checking print endpoint availability at {PrintEndpointUrl}...", printEndpointUrl);

            // For checking availability, we can send a dummy request or a GET request if the endpoint supports it.
            // Since it's a POST endpoint, a GET request will likely return 405 Method Not Allowed, which still indicates availability.
            // A more robust check would be to send a valid POST request and check the response.
            // For now, a simple GET to see if the endpoint exists and responds.
            HttpResponseMessage response = await _httpClient.GetAsync(printEndpointUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed || response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Print endpoint is available. Status Code: {StatusCode}", response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogWarning("Print endpoint is not responding as expected. Status Code: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error checking print endpoint availability: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking print endpoint availability: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public void RestartPrintAgentService()
    {
        _logger.LogInformation("Attempting to restart MedvanaPrintAgentService...");
        try
        {
            // Use Process.Start to execute sc.exe command
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "sc.exe";
            process.StartInfo.Arguments = "stop MedvanaPrintAgentService";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            _logger.LogInformation("Stop command output: {Output}", process.StandardOutput.ReadToEnd());

            // Give some time for the service to stop
            System.Threading.Thread.Sleep(5000);

            process.StartInfo.Arguments = "start MedvanaPrintAgentService";
            process.Start();
            process.WaitForExit();
            _logger.LogInformation("Start command output: {Output}", process.StandardOutput.ReadToEnd());

            _logger.LogInformation("MedvanaPrintAgentService restart command issued.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting MedvanaPrintAgentService: {ErrorMessage}", ex.Message);
        }
    }
}