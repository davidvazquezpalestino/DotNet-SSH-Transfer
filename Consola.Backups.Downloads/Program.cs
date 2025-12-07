IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();

LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext();

if (configuration.GetSection("Serilog").Exists())
{
    loggerConfiguration = loggerConfiguration.ReadFrom.Configuration(configuration);
}
else
{
    loggerConfiguration = loggerConfiguration
        .MinimumLevel.Information()
        .WriteTo.Console();
}

Log.Logger = loggerConfiguration.CreateLogger();

try
{
    DownloadOptions options = DownloadOptions.Parse(args, configuration);

    Log.Information("Starting backup download process targeting {RemotePath}", options.RemotePath);

    BackupDownloader downloader = new(options);
    downloader.Run();

    Log.Information("Backup download process finished successfully.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal error while executing backup download process.");
    Environment.ExitCode = -1;
}
finally
{
    Log.CloseAndFlush();
}