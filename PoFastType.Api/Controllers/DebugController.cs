using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;

namespace PoFastType.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;
    private static readonly string DebugPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DEBUG");

    public DebugController(ILogger<DebugController> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(DebugPath);
    }

    [HttpPost("log")]
    public async Task<IActionResult> LogClientMessage([FromBody] ClientLogEntry logEntry)
    {
        try
        {
            // Log to Serilog
            Log.Information("Client Log [{Level}]: {Message} from {Url} at {Timestamp}", 
                logEntry.Level, 
                logEntry.Message, 
                logEntry.Url, 
                logEntry.Timestamp);

            // Also append to a separate client logs file
            var clientLogPath = Path.Combine(DebugPath, "client-logs.txt");
            var logLine = $"[{logEntry.Timestamp}] [{logEntry.Level.ToUpperInvariant()}] {logEntry.Message} (URL: {logEntry.Url})\n";
            
            await System.IO.File.AppendAllTextAsync(clientLogPath, logLine);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log entry");
            return StatusCode(500);
        }
    }

    [HttpPost("network")]
    public async Task<IActionResult> LogNetworkActivity([FromBody] NetworkActivityEntry activity)
    {
        try
        {
            // Log to Serilog
            Log.Information("Network Activity [{Type}] {Direction}: {Url} at {Timestamp} - {Details}", 
                activity.Type, 
                activity.Direction, 
                activity.Url, 
                activity.Timestamp,
                JsonSerializer.Serialize(activity.Details));

            // Also append to a separate network activity file
            var networkLogPath = Path.Combine(DebugPath, "network-activity.txt");
            var logLine = $"[{activity.Timestamp}] [{activity.Type.ToUpperInvariant()}] {activity.Direction}: {activity.Url} - {JsonSerializer.Serialize(activity.Details)}\n";
            
            await System.IO.File.AppendAllTextAsync(networkLogPath, logLine);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process network activity entry");
            return StatusCode(500);
        }
    }

    [HttpGet("status")]
    public IActionResult GetDebugStatus()
    {
        var debugFiles = Directory.GetFiles(DebugPath)
            .Select(f => new 
            { 
                name = Path.GetFileName(f), 
                size = new FileInfo(f).Length,
                lastModified = new FileInfo(f).LastWriteTime 
            })
            .ToList();

        return Ok(new 
        { 
            debugPath = DebugPath,
            files = debugFiles,
            timestamp = DateTime.UtcNow
        });
    }
}

public class ClientLogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class NetworkActivityEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public JsonElement Details { get; set; }
}
