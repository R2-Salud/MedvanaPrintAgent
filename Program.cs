using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Drawing.Printing;

var builder = WebApplication.CreateBuilder(args);

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

app.UseCors("AllowAll");

app.MapPost("/print", async context =>
{
    // Read the ZPL command from the request body.
    var printRequest = await context.Request.ReadFromJsonAsync<PrintRequest>();
    if (printRequest == null || string.IsNullOrWhiteSpace(printRequest.zpl))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid request: ZPL is required.");
        return;
    }

    // Use the printer name provided in the request, if any
    string printerName = printRequest.printerName ?? "";

    // 2. If not provided, try reading from the configuration file.
    if (string.IsNullOrEmpty(printerName))
    {
        string propertiesFile = Path.Combine(Directory.GetCurrentDirectory(), "printer_agent.properties");
        if (File.Exists(propertiesFile))
        {
            var lines = File.ReadAllLines(propertiesFile);
            foreach (var line in lines)
            {
                // Look for a line starting with "printer.name="
                if (line.Trim().StartsWith("printer.name=", System.StringComparison.OrdinalIgnoreCase))
                {
                    printerName = line.Trim().Substring("printer.name=".Length).Trim();
                    break;
                }
            }
        }
    }

    // 3. If still not found, use the system's default printer.
    if (string.IsNullOrEmpty(printerName))
    {
        // Create a PrinterSettings instance to determine the default printer name.
        PrinterSettings ps = new PrinterSettings();
        printerName = ps.PrinterName;
    }

    // Log the resolved printer name to a file
    string logPath = Path.Combine(AppContext.BaseDirectory, "MedvanaPrintAgent_Log.txt");
    File.AppendAllText(logPath, $"{DateTime.Now}: Resolved printer name: {printerName}{Environment.NewLine}");

    printerName = "ZDesigner ZD421-203dpi ZPL";

    string zpl = printRequest.zpl;

    //zpl = "^XA ^MMT ^MTD ^PON ^FWN ^LH0,0 ^XZ ^XA ^PW406 ^LL203 ^FO20,10^A0N,15,26^FDMARIA GUADALUPE MOTA MEDINA^FS ^FO20,25^A0N,16,16^FDSEXO: F^FS ^FO100,28^A0N,10,18^FD25/03/2025 06:55:14 A.M.^FS ^FO80,50^BY2,2,70^BCN,70,N,N,N,D^FD250325004501^FS ^FO80,130^A0N,16,16^FD2503250045^FS ^FO20,150^A0N,16,16^FD/815^FS ^FO20,180^A0N,16,16^FD/EUROINMUN IZQ /ELISAS^FS ^FO380,30^A0R,25,25^FDSUERO^FS ^XZ ^XA ^PW406 ^LL203 ^FO20,10^A0N,15,26^FDMARIA GUADALUPE MOTA MEDINA^FS ^FO20,25^A0N,16,16^FDSEXO: F^FS ^FO100,28^A0N,10,18^FD25/03/2025^FS ^FO220,25^A0N,16,16^FDEDAD: 47^FS ^FO20,50^A0N,50,50^FD2503250045^FS ^FO20,150^A0N,16,16^FD/815^FS ^FO20,180^A0N,16,16^FDS: 6423-SALUD DIGNA LEON OBELISCO^FS ^FO380,30^A0R,25,25^FDLANS^FS ^XZ";

    // Use the determined printer name to send the ZPL to the printer.
    bool success = RawPrinter.SendStringToPrinter(printerName, zpl);

    if (!success)
    {
        File.AppendAllText(logPath, $"{DateTime.Now}: Printing failed on printer: {printerName}{Environment.NewLine}");
    }
    else
    {
        File.AppendAllText(logPath, $"{DateTime.Now}: Printed successfully on printer: {printerName}{Environment.NewLine}");
    }

    context.Response.StatusCode = success ? 200 : 500;
    await context.Response.WriteAsync(success ? "Printed successfully" : "Print failed");
});

await app.RunAsync();

// Define a record type to represent the JSON request payload.
public record PrintRequest(string zpl, string? printerName);