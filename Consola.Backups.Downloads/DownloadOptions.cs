namespace Consola.Backups.Downloads;

sealed record DownloadOptions(
    string Host,
    int Port,
    string Username,
    string Password,
    string RemotePath,
    string LocalPath,
    string HostKeyFingerprint,
    string PlinkExecutable,
    string PscpExecutable)
{
    private static readonly string DefaultLocalPath = Path.Combine(AppContext.BaseDirectory, "Backups");
    private static string DefaultPlinkExecutable => OperatingSystem.IsWindows() ? "plink.exe" : "plink";
    private static string DefaultPscpExecutable => OperatingSystem.IsWindows() ? "pscp.exe" : "pscp";

    public static DownloadOptions Parse(string[] args, IConfiguration configuration)
    {
        IConfigurationSection configSection = configuration.GetSection("Downloads");
        Dictionary<string, string> map = args
            .Where(arg => arg.StartsWith("--"))
            .Select(arg => arg[2..])
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        string host = Coalesce(Get(map, "host"),
            Environment.GetEnvironmentVariable("DOWNLOADS_SSH_HOST"),
            configSection["Host"]) 
            ?? throw new InvalidOperationException("Host is required in configuration");

        int port = ParseInt(Get(map, "port"))
                   ?? ParseInt(Environment.GetEnvironmentVariable("DOWNLOADS_SSH_PORT"))
                   ?? ParseInt(configSection["Port"])
                   ?? 22;

        string username = Coalesce(Get(map, "username"),
            Environment.GetEnvironmentVariable("DOWNLOADS_SSH_USERNAME"),
            configSection["Username"]) 
            ?? throw new InvalidOperationException("Username is required in configuration");

        string password = Coalesce(Get(map, "password"),
                              Environment.GetEnvironmentVariable("DOWNLOADS_SSH_PASSWORD"),
                              configSection["Password"],
                              null)
                          ?? PromptForPassword();

        string? remotePathValue = Coalesce(
            Get(map, "remotePath"),
            Environment.GetEnvironmentVariable("DOWNLOADS_SSH_REMOTEPATH"),
            configSection["RemotePath"]);
            
        if (string.IsNullOrWhiteSpace(remotePathValue))
        {
            throw new InvalidOperationException("RemotePath is required in configuration");
        }
        string remotePath = NormalizeRemotePath(remotePathValue);

        string localPath = Path.GetFullPath(Coalesce(
            Get(map, "localPath"),
            Environment.GetEnvironmentVariable("DOWNLOADS_LOCAL_PATH"),
            configSection["LocalPath"],
            DefaultLocalPath)!);

        string? hostKey = Coalesce(
            Get(map, "hostKey"),
            Environment.GetEnvironmentVariable("DOWNLOADS_SSH_HOSTKEY"),
            configSection["HostKey"]);
            
        if (string.IsNullOrWhiteSpace(hostKey))
        {
            Log.Warning("No HostKey provided. This is not recommended for production use.");
        }

        string plinkExecutable = Coalesce(
            Get(map, "plink"),
            Environment.GetEnvironmentVariable("DOWNLOADS_PLINK_PATH"),
            configSection["Plink"],
            DefaultPlinkExecutable)!;

        string pscpExecutable = Coalesce(
            Get(map, "pscp"),
            Environment.GetEnvironmentVariable("DOWNLOADS_PSCP_PATH"),
            configSection["Pscp"],
            DefaultPscpExecutable)!;

        if (Path.IsPathRooted(plinkExecutable))
        {
            plinkExecutable = Path.GetFullPath(plinkExecutable);
        }

        if (Path.IsPathRooted(pscpExecutable))
        {
            pscpExecutable = Path.GetFullPath(pscpExecutable);
        }

        return new DownloadOptions(host, port, username, password, remotePath, localPath, hostKey, plinkExecutable, pscpExecutable);
    }

    private static string? Get(IReadOnlyDictionary<string, string> map, string key) => map.GetValueOrDefault(key);
    private static string? Coalesce(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static int? ParseInt(string? value) => int.TryParse(value, out int parsed) ? parsed : null;

    private static string NormalizeRemotePath(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            throw new ArgumentException("Remote path cannot be empty.");
        }

        return remotePath.TrimEnd('/') + '/';
    }

    private static string PromptForPassword()
    {
        Console.Write("SSH password: ");
        StringBuilder builder = new StringBuilder();

        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            builder.Append(keyInfo.KeyChar);
            Console.Write('*');
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException("Password is required.");
        }

        return builder.ToString();
    }
}