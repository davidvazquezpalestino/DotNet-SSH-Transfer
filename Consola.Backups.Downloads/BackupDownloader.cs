namespace Consola.Backups.Downloads;

sealed class BackupDownloader(DownloadOptions options)
{
    private bool ConnectionAnnounced;

    public void Run()
    {
        EnsureToolsExist();

        Log.Information("Listing files at {RemotePath}...", options.RemotePath);
        IReadOnlyList<RemoteFileInfo> files = ListRemoteFiles();

        if (files.Count == 0)
        {
            Log.Information("No files to download.");
            return;
        }

        //List<RemoteFileInfo> downloaded = new();

        foreach (RemoteFileInfo remoteFile in files)
        {
            string targetFolder = Path.Combine(options.LocalPath, remoteFile.DateFolder);
            Directory.CreateDirectory(targetFolder);

            string safeName = SanitizeFileName(remoteFile.Name);
            string localTarget = Path.Combine(targetFolder, safeName);
            string remoteFullPath = remoteFile.FullPath;

            Log.Information("Downloading '{RemoteFile}' -> '{LocalTarget}'...", remoteFile.Name, localTarget);
            DownloadRemoteFile(remoteFullPath, localTarget);

            //downloaded.Add(remoteFile);
        }

        // Delete remote files from the previous day
        DateTime yesterday = DateTime.Now.AddDays(-1);
        string yesterdayStr = yesterday.ToString("yyyy-MM-dd");
        IEnumerable<RemoteFileInfo> oldFiles = files.Where(f => f.DateFolder == yesterdayStr);

        foreach (RemoteFileInfo oldFile in oldFiles)
        {
            Log.Information("Deleting old remote file '{RemoteFile}'...", oldFile.Name);
            DeleteRemoteFile(oldFile.FullPath);
        }

        Log.Information("All files processed successfully.");
    }

    private void EnsureToolsExist()
    {
        ValidateExecutable(options.PlinkExecutable, "Plink");
        ValidateExecutable(options.PscpExecutable, "Pscp");
    }

    private static void ValidateExecutable(string pathOrName, string label)
    {
        bool includesPath = Path.IsPathRooted(pathOrName)
                            || pathOrName.Contains(Path.DirectorySeparatorChar)
                            || pathOrName.Contains(Path.AltDirectorySeparatorChar);

        if (includesPath && !File.Exists(pathOrName))
        {
            throw new FileNotFoundException($"{label} executable not found at '{pathOrName}'.");
        }
    }

    private IReadOnlyList<RemoteFileInfo> ListRemoteFiles()
    {
        string command = $"find {ShellQuote(options.RemotePath)} -maxdepth 1 -type f -printf '%p|%f|%TY-%Tm-%Td\\n'";
        string output = RunPlink(command);

        List<RemoteFileInfo> files = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseRemoteFileLine)
            .Where(info => info is not null)
            .Select(info => info!)
            .ToList();

        return files;
    }

    private RemoteFileInfo? ParseRemoteFileLine(string line)
    {
        string[] parts = line.Split('|');

        if (parts.Length < 3)
        {
            return null;
        }

        string fullPath = parts[0].Trim();
        string name = parts[1].Trim();
        string dateFolder = parts[2].Trim();

        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dateFolder))
        {
            return null;
        }

        return new RemoteFileInfo(fullPath, name, dateFolder);
    }

    private void DownloadRemoteFile(string remoteFullPath, string localFilePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(localFilePath)!);
        string remoteSpec = $"{options.Username}@{options.Host}:{remoteFullPath}";

        List<string> args = new List<string>
        {
            "-batch",
            "-P", options.Port.ToString(),
            "-pw", options.Password,
        };

        if (!string.IsNullOrWhiteSpace(options.HostKeyFingerprint))
        {
            args.Add("-hostkey");
            args.Add(options.HostKeyFingerprint);
        }

        args.Add(remoteSpec);
        args.Add(localFilePath);

        RunProcess(options.PscpExecutable, args, "download");
    }

    private void DeleteRemoteFile(string remoteFullPath)
    {
        string command = $"rm {ShellQuote(remoteFullPath)}";
        RunPlink(command);
    }

    private string RunPlink(string remoteCommand)
    {
        List<string> args = new List<string>
        {
            "-batch",
            "-P", options.Port.ToString(),
            "-pw", options.Password,
        };

        if (!string.IsNullOrWhiteSpace(options.HostKeyFingerprint))
        {
            args.Add("-hostkey");
            args.Add(options.HostKeyFingerprint);
        }

        args.Add($"{options.Username}@{options.Host}");
        args.Add(remoteCommand);

        string output = RunProcess(options.PlinkExecutable, args, "plink");
        AnnounceConnectionIfNeeded();
        return output;
    }

    private void AnnounceConnectionIfNeeded()
    {
        if (ConnectionAnnounced)
        {
            return;
        }

        Log.Information("Connected to {Host}:{Port} as {Username}.", options.Host, options.Port, options.Username);
        ConnectionAnnounced = true;
    }

    private static string RunProcess(string fileName, IReadOnlyList<string> arguments, string operation)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException($"Unable to start {operation} process.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Log.Error("{Operation} command failed with exit code {ExitCode}. Error: {Error}", operation, process.ExitCode, error);
            throw new InvalidOperationException($"{operation} command failed with exit code {process.ExitCode}. Details: {error}");
        }

        return output;
    }

    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\"'\"'")}'";

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }
}

sealed record RemoteFileInfo(string FullPath, string Name, string DateFolder)
{
    public override string ToString() => Name;
}