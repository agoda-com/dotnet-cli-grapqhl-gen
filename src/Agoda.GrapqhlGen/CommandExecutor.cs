using Microsoft.Extensions.Logging;

namespace Agoda.GrapqhlGen;

public interface ICommandExecutor
{
    Task ExecuteAsync(string command, string arguments);
}

public class CommandExecutor : ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;

    public CommandExecutor(ILogger<CommandExecutor>? logger = null)
    {
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<CommandExecutor>();
    }

    public async Task ExecuteAsync(string command, string arguments)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _logger.LogDebug("Executing command: {Command} {Arguments}", command, arguments);

        using var process = System.Diagnostics.Process.Start(processStartInfo);
        if (process == null)
        {
            _logger.LogError("Failed to start process: {Command}", command);
            throw new Exception($"Failed to start process: {command}");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Command failed with exit code {ExitCode}. Error: {Error}",
                process.ExitCode, error);
            throw new Exception($"Command failed with exit code {process.ExitCode}. Error: {error}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            _logger.LogDebug("Command output: {Output}", output);
        }
    }
}