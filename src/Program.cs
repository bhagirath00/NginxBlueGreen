using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var appColor = Environment.GetEnvironmentVariable("APP_COLOR") ?? "Green";
var appPort = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:2001";
var isPort2001 = appPort.Contains("2001");
var publishWatchPath = Path.GetFullPath("./publish");
var greenDir = Path.GetFullPath("./www/green");

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
        Arguments = $"{greenDir}/BlueGreenApp.dll",
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

// ─── BLUE MODE (Port 2002): Poll + Manual trigger ────────────────────────────
if (!isPort2001)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("[PORT 2002] Blue server LIVE. Port 2001 DLLs are UNLOCKED.");
    Console.WriteLine("[PORT 2002] Replace your DLLs, then click the button at http://localhost:2002");
    Console.ResetColor();

    var startedAt = DateTime.UtcNow;
    var triggered = false;

    // Auto-detect if DLL actually changes (real deployment scenario)
    _ = Task.Run(async () =>
    {
        while (!triggered)
        {
            await Task.Delay(1000);
            var dll = Path.Combine(publishWatchPath, "BlueGreenApp.dll");
            if (File.Exists(dll) && File.GetLastWriteTimeUtc(dll) > startedAt)
            {
                if (triggered) return;
                triggered = true;
                Console.WriteLine("[PORT 2002] Auto-detected new DLL. Returning to Port 2001...");
                await ReturnToPort2001();
            }
        }
    });
}

// ─── UI ──────────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Content(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>Blue-Green Dashboard</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Segoe UI', sans-serif; display: flex; align-items: center; justify-content: center; min-height: 100vh; background: " + (isPort2001 ? "#e8f5e9" : "#e3f2fd") + @"; }
        .card { background: white; padding: 40px 50px; border-radius: 16px; box-shadow: 0 8px 24px rgba(0,0,0,0.12); text-align: center; max-width: 480px; width: 100%; }
        .badge { display: inline-block; padding: 6px 18px; border-radius: 20px; font-size: 13px; font-weight: 600; letter-spacing: 0.5px; margin-bottom: 20px; background: " + (isPort2001 ? "#c8e6c9" : "#bbdefb") + @"; color: " + (isPort2001 ? "#1b5e20" : "#0d47a1") + @"; }
        h1 { font-size: 30px; margin-bottom: 8px; color: #111; }
        .port { font-size: 16px; font-weight: 600; color: " + (isPort2001 ? "#2e7d32" : "#1565c0") + @"; margin-bottom: 24px; }
        hr { border: none; border-top: 1px solid #eee; margin: 20px 0; }
        p { color: #555; margin-bottom: 20px; line-height: 1.6; font-size: 15px; }
        button { color: white; border: none; padding: 14px 32px; border-radius: 8px; cursor: pointer; font-size: 15px; font-weight: 600; width: 100%; background: " + (isPort2001 ? "#1565c0" : "#2e7d32") + @"; }
        button:hover { opacity: 0.88; }
        .alert { background: #fff8e1; border-left: 4px solid #f9a825; border-radius: 6px; padding: 12px 16px; color: #795548; margin-bottom: 20px; font-size: 14px; text-align: left; }
    </style>
</head>
<body>
    <div class='card'>
        <div class='badge'>" + (isPort2001 ? "PRIMARY | PORT 2001" : "TEMPORARY | PORT 2002") + @"</div>
        <h1>" + (isPort2001 ? "Green Server" : "Blue Server") + @"</h1>
        <div class='port'>" + appPort + @"</div>
        <hr/>
        " + (isPort2001
            ? @"<p>Click below to shift traffic to Port 2002 and unlock your DLL files for replacement.</p>
        <form action='/switch' method='POST'>
            <button type='submit'>Switch to Port 2002</button>
        </form>"
            : @"<div class='alert'>Port 2001 is stopped. DLL files are now free to replace.</div>
        <p>Once you have replaced your DLL files, click below to return to Port 2001.</p>
        <form action='/return' method='POST'>
            <button type='submit'>Return to Port 2001</button>
        </form>") + @"
    </div>
</body>
</html>", "text/html"));

// ─── MANUAL RETURN: Port 2002 → Port 2001 ────────────────────────────────────
app.MapPost("/return", async (HttpContext ctx) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[PORT 2002] Manual return triggered from browser.");
    Console.ResetColor();

    await ctx.Response.WriteAsync(@"
<html><head><style>
body{font-family:'Segoe UI',sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;background:#e8f5e9}
.card{background:white;padding:40px;border-radius:16px;box-shadow:0 8px 24px rgba(0,0,0,0.1);text-align:center;max-width:420px}
h2{color:#2e7d32;margin-bottom:12px}p{color:#555;margin-bottom:8px}
</style></head><body>
<div class='card'>
<h2>Returning to Port 2001...</h2>
<p>Copying new DLL files to Green server.</p>
<p>Port 2001 is starting up.</p>
<p>Redirecting automatically...</p>
<script>setTimeout(()=>window.location.href='http://localhost:2001',4000)</script>
</div></body></html>");

    _ = Task.Run(ReturnToPort2001);
});

// ─── SWITCH: Port 2001 → Port 2002 ───────────────────────────────────────────
app.MapPost("/switch", async (HttpContext ctx) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[PORT 2001] Switch triggered. Starting Port 2002...");
    Console.ResetColor();

    // Timestamped blue folder to avoid file lock conflicts
    var blueDir = Path.GetFullPath($"./www/blue-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
    var publishDir = Path.GetFullPath("./publish");
    Directory.CreateDirectory(blueDir);
    foreach (var file in Directory.GetFiles(publishDir))
        File.Copy(file, Path.Combine(blueDir, Path.GetFileName(file)), overwrite: true);

    Process.Start(new ProcessStartInfo("dotnet")
    {
        Arguments = $"{blueDir}/BlueGreenApp.dll",
        UseShellExecute = false,
        EnvironmentVariables =
        {
            ["APP_COLOR"] = "Blue",
            ["ASPNETCORE_URLS"] = "http://localhost:2002"
        }
    });

    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[PORT 2001] STOPPED. DLL files in ./publish/ are now FREE.");
        Console.ResetColor();
        Environment.Exit(0);
    });

    await ctx.Response.WriteAsync(@"
<html><head><style>
body{font-family:'Segoe UI',sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;background:#e3f2fd}
.card{background:white;padding:40px;border-radius:16px;box-shadow:0 8px 24px rgba(0,0,0,0.1);text-align:center;max-width:420px}
h2{color:#1565c0;margin-bottom:12px}p{color:#555;margin-bottom:8px}
</style></head><body>
<div class='card'>
<h2>Switching to Port 2002...</h2>
<p>Port 2001 is shutting down.</p>
<p>DLL files will be unlocked in 2 seconds.</p>
<script>setTimeout(()=>window.location.href='http://localhost:2002',3500)</script>
</div></body></html>");
});

app.Run();
