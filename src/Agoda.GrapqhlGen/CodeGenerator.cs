using System.Text;
using Microsoft.Extensions.Logging;

namespace Agoda.GrapqhlGen;

public class CodeGenerator
{
    private readonly string _schemaUrl;
    private readonly string _inputPath;
    private readonly string _outputPath;
    private readonly string _baseNamespace;
    private readonly Dictionary<string, string>? _headers;
    private readonly string _template;
    private readonly string _modelFile;
    private readonly ICommandExecutor _commandExecutor;
    private readonly ILogger<CodeGenerator> _logger;

    public CodeGenerator(
        string schemaUrl,
        string inputPath,
        string outputPath,
        string baseNamespace,
        Dictionary<string, string>? headers,
        string template,
        string modelFile,
        ICommandExecutor? commandExecutor = null,
        ILogger<CodeGenerator>? logger = null)
    {
        _schemaUrl = schemaUrl;
        _inputPath = inputPath;
        _outputPath = outputPath;
        _baseNamespace = baseNamespace;
        _headers = headers;
        _template = template;
        _modelFile = modelFile;
        _commandExecutor = commandExecutor ?? new CommandExecutor();
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole())
                                       .CreateLogger<CodeGenerator>();
    }

    public async Task GenerateAsync()
    {
        _logger.LogInformation("Starting code generation...");

        EnsureDirectoriesExist();
        CleanWorkingDirectory();
        CopyGraphQLFiles();
        await GenerateCode();
        ProcessRegions();
        CleanWorkingDirectory();

        _logger.LogInformation("Code generation completed successfully.");
    }

    private void EnsureDirectoriesExist()
    {
        _logger.LogDebug("Ensuring directory exists: {OutputPath}", _outputPath);
        Directory.CreateDirectory(_outputPath);
    }

    private void CleanWorkingDirectory()
    {
        _logger.LogDebug("Cleaning working directory...");
        foreach (var file in Directory.GetFiles(_outputPath, "*.graphql"))
        {
            _logger.LogDebug("Deleting GraphQL file: {File}", file);
            File.Delete(file);
        }
        var classesFile = Path.Combine(_outputPath, "Classes.cs");
        if (File.Exists(classesFile))
        {
            _logger.LogDebug("Deleting existing Classes.cs: {ClassesFile}", classesFile);
            File.Delete(classesFile);
        }
    }

    private void CopyGraphQLFiles()
    {
        _logger.LogDebug("Copying GraphQL files from {InputPath} to {OutputPath}", _inputPath, _outputPath);
        foreach (var file in Directory.GetFiles(_inputPath, "*.graphql", SearchOption.AllDirectories))
        {
            var destFile = Path.Combine(_outputPath, Path.GetFileName(file));
            _logger.LogDebug("Copying {SourceFile} to {DestFile}", file, destFile);
            File.Copy(file, destFile, true);
        }
    }

    private async Task GenerateCode()
    {
        _logger.LogInformation("Starting code generation process...");

        // Ensure pnpm is installed
        _logger.LogInformation("Installing pnpm...");
        await _commandExecutor.ExecuteAsync("npm", "install -g pnpm");

        // Install dependencies using pnpm
        _logger.LogInformation("Installing GraphQL CodeGen dependencies...");
        await _commandExecutor.ExecuteAsync("pnpm", "install @graphql-codegen/cli @graphql-codegen/typescript");

        // Build pnpm command for code generation
        var headerArgs = _headers?.Select(h => $"--header \"{h.Key}: {h.Value}\"") ?? Array.Empty<string>();
        var args = $"graphql-codegen " +
                  $"--schema {_schemaUrl} " +
                  $"--template {_template} " +
                  $"--out {_outputPath} " +
                  string.Join(" ", headerArgs) +
                  $" {Path.Combine(_outputPath, "*.graphql")}";

        _logger.LogDebug("Executing GraphQL CodeGen with args: {Arguments}", args);
        await _commandExecutor.ExecuteAsync("pnpm", args);

        var classesFile = Path.Combine(_outputPath, "Classes.cs");
        _logger.LogDebug("Checking for Classes.cs at {ClassesFile}. Exists: {Exists}",
            classesFile, File.Exists(classesFile));
    }

    private void ProcessRegions()
    {
        var classesFile = Path.Combine(_outputPath, "Classes.cs");
        _logger.LogDebug("Processing regions from {ClassesFile}", classesFile);

        if (!File.Exists(classesFile))
        {
            _logger.LogError("Generated Classes.cs file not found at {ClassesFile}", classesFile);
            throw new Exception($"Generated Classes.cs file not found at {classesFile}");
        }

        var regions = RegionParser.Parse(classesFile);
        _logger.LogDebug("Found {RegionCount} regions", regions.Count());

        // Process models
        var models = regions.FirstOrDefault(r => r.Name == "input types");
        if (models != null)
        {
            var modelFile = Path.Combine(_outputPath, _modelFile);
            _logger.LogInformation("Generating models file at {ModelFile}", modelFile);
            GenerateFile(_modelFile, models);
        }
        else
        {
            _logger.LogWarning("No input types region found");
        }

        // Process other regions
        foreach (var region in regions.Where(r => !new[] { "fragments", "input types", "Query" }.Contains(r.Name)))
        {
            var fileName = $"{region.Name}.generated.cs";
            _logger.LogInformation("Generating file for region {RegionName} at {FileName}", region.Name, fileName);
            GenerateFile(fileName, region);
        }
    }

    private void GenerateFile(string fileName, Region region)
    {
        var filePath = Path.Combine(_outputPath, fileName);
        _logger.LogDebug("Generating file at {FilePath}", filePath);

        var content = region.GetSourceCode()
            .Replace("namespace Generated", $"\nnamespace {_baseNamespace}")
            .Replace("using Agoda.CodeGen.GraphQL", "using Agoda.Graphql.Client")
            .Replace("</sumary>", "</summary>");

        File.WriteAllText(filePath, content, Encoding.UTF8);
        _logger.LogDebug("File generated successfully at {FilePath}", filePath);
    }
}