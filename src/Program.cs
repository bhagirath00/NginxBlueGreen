using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var appColor = Environment.GetEnvironmentVariable("APP_COLOR") ?? "Green";
var appPort = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:2001";
var isPort2001 = appPort.Contains("2001");

// ─── ROBUST FILE PATH RESOLUTION ───────────────────────────────────────────
// Use the ASP.NET content root to anchor paths (stable when launched via scripts).
var rootDir = app.Environment.ContentRootPath;
var publishWatchPath = Path.Combine(rootDir, "publish");
var greenDir = Path.Combine(rootDir, "www", "green");

DateTime GetLatestWriteUtc(string dir)
{
    if (!Directory.Exists(dir)) return DateTime.MinValue;

    var latest = DateTime.MinValue;
    foreach (var file in Directory.GetFiles(dir))
    {
        var writeTime = File.GetLastWriteTimeUtc(file);
        if (writeTime > latest) latest = writeTime;
    }

    return latest;
}

// Helper to start Port 2001 and kill this process
async Task ReturnToPort2001()
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[PORT 2002] Copying new files to ./www/green/ ...");
    Directory.CreateDirectory(greenDir);
    foreach (var file in Directory.GetFiles(publishWatchPath))
    {
        try { File.Copy(file, Path.Combine(greenDir, Path.GetFileName(file)), overwrite: true); }
        catch { }
    }

    Console.WriteLine("[PORT 2002] Starting Port 2001...");
    Console.ResetColor();

    Process.Start(new ProcessStartInfo("dotnet")
    {
        Arguments = $"\"{Path.Combine(greenDir, "BlueGreenApp.dll")}\"",
        UseShellExecute = false,
        EnvironmentVariables =
        {
            ["APP_COLOR"] = "Green",
            ["ASPNETCORE_URLS"] = "http://localhost:2001"
        }
    });

    await Task.Delay(2000);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[PORT 2002] Port 2001 is BACK ONLINE. Shutting down Port 2002.");
    Console.ResetColor();
    Environment.Exit(0);
}

// Helper to start Port 2002 and kill this process
async Task SwitchToPort2002()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[PORT 2001] File change detected! Starting Port 2002...");
    Console.ResetColor();

    // Timestamped blue folder to avoid file lock conflicts
    var blueDir = Path.GetFullPath($"./www/blue-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
    Directory.CreateDirectory(blueDir);
    foreach (var file in Directory.GetFiles(publishWatchPath))
    {
        try { File.Copy(file, Path.Combine(blueDir, Path.GetFileName(file)), overwrite: true); }
        catch { }
    }

    Process.Start(new ProcessStartInfo("dotnet")
    {
        Arguments = $"\"{Path.Combine(blueDir, "BlueGreenApp.dll")}\"",
        UseShellExecute = false,
        EnvironmentVariables =
        {
            ["APP_COLOR"] = "Blue",
            ["ASPNETCORE_URLS"] = "http://localhost:2002"
        }
    });

    await Task.Delay(2000);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[PORT 2001] STOPPED. DLL files in ./publish/ are now FREE.");
    Console.ResetColor();
    Environment.Exit(0);
}

// ─── GREEN MODE (Port 2001): Watch for DLL changes ───────────────────────────
if (isPort2001)
{
    var lastWrite = GetLatestWriteUtc(publishWatchPath);
    Console.WriteLine($"[PORT 2001] Watching for changes in {publishWatchPath}... (Baseline: {lastWrite})");

    var triggered = false;

    _ = Task.Run(async () =>
    {
        await Task.Delay(3000); // Wait for startup to settle
        while (!triggered)
        {
            await Task.Delay(1000);
            var currentWrite = GetLatestWriteUtc(publishWatchPath);
            if (currentWrite > lastWrite)
            {
                await Task.Delay(500); // Settling time for build
                triggered = true;
                Console.WriteLine($"\n[PORT 2001] UPDATE DETECTED! (New: {currentWrite} > Old: {lastWrite})");
                await SwitchToPort2002();
            }
        }
    });
}
// ─── BLUE MODE (Port 2002): Wait 30s then return ─────────────────────────────
else
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("[PORT 2002] Blue server LIVE. Port 2001 DLLs are UNLOCKED for 30 Seconds.");
    Console.ResetColor();

    _ = Task.Run(async () =>
    {
        Console.WriteLine("[PORT 2002] Waiting 30 seconds before auto-returning to 2001...");
        await Task.Delay(30000);
        Console.WriteLine("[PORT 2002] 30 seconds elapsed. Returning to Port 2001 Automatically...");
        await ReturnToPort2001();
    });
}

// ─── UI ──────────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Content(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta http-equiv='refresh' content='2'>
    <title>Blue-Green Dashboard</title>
    <script>
        let port = window.location.port;
        // Aggressive polling so the browser switches ports instantly when one dies
        setInterval(async () => {
            try { 
                const response = await fetch('/'); 
                if (!response.ok) throw new Error();
            }
            catch { 
                window.location.href = port === '2001' ? 'http://localhost:2002' : 'http://localhost:2001'; 
            }
        }, 500);
    </script>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Segoe UI', sans-serif; display: flex; align-items: center; justify-content: center; min-height: 100vh; background: " + (isPort2001 ? "#e8f5e9" : "#e3f2fd") + @"; }
        .card { background: white; padding: 40px 50px; border-radius: 16px; box-shadow: 0 8px 24px rgba(0,0,0,0.12); text-align: center; max-width: 480px; width: 100%; }
        .badge { display: inline-block; padding: 6px 18px; border-radius: 20px; font-size: 13px; font-weight: 600; letter-spacing: 0.5px; margin-bottom: 20px; background: " + (isPort2001 ? "#c8e6c9" : "#bbdefb") + @"; color: " + (isPort2001 ? "#1b5e20" : "#0d47a1") + @"; }
        h1 { font-size: 30px; margin-bottom: 8px; color: #111; }
        .port { font-size: 16px; font-weight: 600; color: " + (isPort2001 ? "#2e7d32" : "#1565c0") + @"; margin-bottom: 24px; }
        hr { border: none; border-top: 1px solid #eee; margin: 20px 0; }
        p { color: #555; margin-bottom: 20px; line-height: 1.6; font-size: 15px; }
        .alert { background: #fff8e1; border-left: 4px solid #f9a825; border-radius: 6px; padding: 12px 16px; color: #795548; margin-bottom: 20px; font-size: 14px; text-align: left; }
    </style>
</head>
<body>
    <div class='card'>
        <div class='badge'>" + (isPort2001 ? "PRIMARY | PORT 2001" : "TEMPORARY | PORT 2002") + @"</div>
        <h1>" + (isPort2001 ? "Green Server" : "Blue Server") + @"</h1>
        <div class='port'>" + appPort + @"</div>
        <hr/>
        " + (isPort2001 ? "" : "") + @"
    </div>
</body>
</html>", "text/html"));

app.Run();
