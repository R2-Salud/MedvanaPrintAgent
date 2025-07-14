using MedvanaPrintAgentMonitor;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "MedvanaPrintAgentMonitor_Log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services.AddSerilog(); // Use Serilog for logging

builder.Services.AddWindowsService(); // Enable running as a Windows Service

builder.Services.AddSingleton<MonitoringService>(); // Register MonitoringService
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
