using System.CommandLine;
using System.Text;
using Agoda.GrapqhlGen;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace GraphqlCodeGen;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GraphQL Code Generation Tool");

        // Required options
        var schemaUrlOption = new Option<string>(
            name: "--schema-url",
            description: "URL of the GraphQL schema")
        { IsRequired = true };

        var inputPathOption = new Option<string>(
            name: "--input-path",
            description: "Path to the directory containing .graphql files")
        { IsRequired = true };

        var outputPathOption = new Option<string>(
            name: "--output-path",
            description: "Path where generated files will be saved")
        { IsRequired = true };

        // Optional options
        var namespaceOption = new Option<string>(
            name: "--namespace",
            description: "Base namespace for generated code",
            getDefaultValue: () => "Generated");

        var headersOption = new Option<string[]>(
            name: "--headers",
            description: "Headers to include in the schema request (format: 'Key: Value')")
        { AllowMultipleArgumentsPerToken = true };

        var templateOption = new Option<string>(
            name: "--template",
            description: "Template to use for code generation",
            getDefaultValue: () => "typescript");

        var modelFileOption = new Option<string>(
            name: "--model-file",
            description: "Name of the generated models file",
            getDefaultValue: () => "Models.cs");

        var logLevelOption = new Option<LogLevel>(
            name: "--log-level",
            description: "Set the logging level (Debug, Information, Warning, Error, Critical)",
            getDefaultValue: () => LogLevel.Information);

        // Add options to command
        rootCommand.AddOption(schemaUrlOption);
        rootCommand.AddOption(inputPathOption);
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(namespaceOption);
        rootCommand.AddOption(headersOption);
        rootCommand.AddOption(templateOption);
        rootCommand.AddOption(modelFileOption);
        rootCommand.AddOption(logLevelOption);

        rootCommand.SetHandler(async (
            string schemaUrl,
            string inputPath,
            string outputPath,
            string namespaceName,
            string[] headers,
            string template,
            string modelFile,
            LogLevel logLevel) =>
        {
            try
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .SetMinimumLevel(logLevel)
                        .AddConsole(options =>
                        {
                            options.FormatterName = ConsoleFormatterNames.Simple;
                        });
                });

                var logger = loggerFactory.CreateLogger<CodeGenerator>();

                var generator = new CodeGenerator(
                    schemaUrl,
                    inputPath,
                    outputPath,
                    namespaceName,
                    headers?.ToDictionary(h => h.Split(':')[0].Trim(), h => h.Split(':')[1].Trim()),
                    template,
                    modelFile,
                    commandExecutor: null,
                    logger: logger);

                await generator.GenerateAsync();
            }
            catch (Exception ex)
            {
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .SetMinimumLevel(LogLevel.Error)
                        .AddConsole(options =>
                        {
                            options.FormatterName = ConsoleFormatterNames.Simple;
                        });
                });
                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "Error during code generation");
                Environment.Exit(1);
            }
        },
        schemaUrlOption,
        inputPathOption,
        outputPathOption,
        namespaceOption,
        headersOption,
        templateOption,
        modelFileOption,
        logLevelOption);

        return await rootCommand.InvokeAsync(args);
    }
}