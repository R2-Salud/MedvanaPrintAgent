using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Drawing.Printing;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace MedvanaPrintAgent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(
                    Path.Combine(AppContext.BaseDirectory, "logs", "MedvanaPrintAgent_Log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            builder.WebHost.UseUrls("http://*:3110");
            // This extension enables your app to run as a Windows Service.
            builder.Host.UseWindowsService();

            var app = builder.Build();
            Log.Information("MedvanaPrintAgent application built.");

            app.UseCors("AllowAll");
            Log.Information("CORS policy 'AllowAll' applied.");

            app.MapPost("/print", async context =>
            {
                Log.Information("Print request received.");
                try
                {
                    Log.Information("Attempting to read print request from body.");
                    // Read the ZPL command from the request body.
                    var printRequest = await context.Request.ReadFromJsonAsync<PrintRequest>();
                    if (printRequest == null || string.IsNullOrWhiteSpace(printRequest.zpl))
                    {
                        Log.Warning("Invalid print request: ZPL is required.");
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid request: ZPL is required.");
                        return;
                    }

                    Log.Information("Print request details: ZPL length = {ZplLength}, PrinterName = {RequestedPrinterName}", printRequest.zpl.Length, printRequest.printerName ?? "Not provided");

                    // Use the printer name provided in the request, if any
                    string printerName = printRequest.printerName ?? "";

                    // 2. If not provided, try reading from the configuration file.
                    if (string.IsNullOrEmpty(printerName))
                    {
                        string propertiesFile = Path.Combine(Directory.GetCurrentDirectory(), "printer_agent.properties");
                        Log.Information("Checking for printer_agent.properties at: {PropertiesFile}", propertiesFile);
                        if (File.Exists(propertiesFile))
                        {
                            Log.Information("printer_agent.properties found. Reading lines.");
                            var lines = File.ReadAllLines(propertiesFile);
                            foreach (var line in lines)
                            {
                                // Look for a line starting with "printer.name="
                                if (line.Trim().StartsWith("printer.name=", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    printerName = line.Trim().Substring("printer.name=".Length).Trim();
                                    Log.Information("Printer name resolved from config file: {PrinterName}", printerName);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Log.Warning("printer_agent.properties not found at: {PropertiesFile}", propertiesFile);
                        }
                    }

                    // 3. If still not found, use the system's default printer.
                    if (string.IsNullOrEmpty(printerName))
                    {
                        Log.Information("Printer name not found in request or config. Attempting to use system default printer.");
                        // Create a PrinterSettings instance to determine the default printer name.
                        PrinterSettings ps = new PrinterSettings();
                        printerName = ps.PrinterName;
                        Log.Information("Printer name resolved to system default: {PrinterName}", printerName);
                    }

                    // Log the resolved printer name
                    Log.Information("Final resolved printer name for printing: {PrinterName}", printerName);

                    string zpl = printRequest.zpl;

                    // Use the determined printer name to send the ZPL to the printer.
                    Log.Information("Sending ZPL to printer: {PrinterName}", printerName);
                    bool success = RawPrinter.SendStringToPrinter(printerName, zpl);

                    if (!success)
                    {
                        Log.Error("Printing failed on printer: {PrinterName}", printerName);
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Print failed");
                    }
                    else
                    {
                        Log.Information("Printed successfully on printer: {PrinterName}", printerName);
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Printed successfully");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "EXCEPTION in /print endpoint - {ErrorMessage}", ex.Message);

                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Server error: {ex.Message}");
                }
            });

            Log.Information("Starting MedvanaPrintAgent application.");
            await app.RunAsync();
            Log.Information("MedvanaPrintAgent application stopped.");
        }
    }

    // Define a record type to represent the JSON request payload.
    public record PrintRequest(string zpl, string? printerName);
}