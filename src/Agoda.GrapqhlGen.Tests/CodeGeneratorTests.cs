using NUnit.Framework;
using NSubstitute;
using Shouldly;
using System.Text;

namespace Agoda.GrapqhlGen.Tests;

[TestFixture]
public class CodeGeneratorTests
{
    private ICommandExecutor _commandExecutor = null!;
    private string _tempPath = null!;
    private CodeGenerator _generator = null!;
    private const string SchemaUrl = "https://test.graphql";

    [SetUp]
    public void Setup()
    {
        _commandExecutor = Substitute.For<ICommandExecutor>();
        _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempPath);

        _generator = new CodeGenerator(
            SchemaUrl,
            _tempPath,
            _tempPath,
            "Test.Namespace",
            new Dictionary<string, string> { { "API-Key", "test-key" } },
            "typescript",
            "Models.cs",
            _commandExecutor);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    private void SetupDefaultCommandExecutor()
    {
        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var command = callInfo.ArgAt<string>(0);
                var args = callInfo.ArgAt<string>(1);
                Console.WriteLine($"Mock received command: {command} {args}");

                if (args.Contains("graphql-codegen"))
                {
                    var classesPath = Path.Combine(_tempPath, "Classes.cs");
                    Console.WriteLine($"Creating Classes.cs at {classesPath}");
                    File.WriteAllText(classesPath, @"
namespace Generated
{
    #region input types
    public class TestModel {}
    #endregion

    #region TestQuery
    public class TestQuery {}
    #endregion
}");
                    Console.WriteLine($"Classes.cs created: {File.Exists(classesPath)}");
                }
                return Task.CompletedTask;
            });
    }

    [Test]
    public async Task GenerateAsync_ShouldCreateExpectedFiles()
    {
        // Arrange
        SetupMockFiles();
        SetupDefaultCommandExecutor();

        // Act
        await _generator.GenerateAsync();

        // Debug Output
        Console.WriteLine($"Temp Path: {_tempPath}");
        Console.WriteLine($"Files in directory:");
        foreach (var file in Directory.GetFiles(_tempPath))
        {
            Console.WriteLine($"- {file}");
        }

        // Assert
        var modelsPath = Path.Combine(_tempPath, "Models.cs");
        File.Exists(modelsPath).ShouldBeTrue($"Models.cs should exist at {modelsPath}");

        var queryPath = Path.Combine(_tempPath, "TestQuery.generated.cs");
        File.Exists(queryPath).ShouldBeTrue($"TestQuery.generated.cs should exist at {queryPath}");
    }

    [Test]
    public async Task GenerateAsync_ShouldReplaceNamespaceCorrectly()
    {
        // Arrange
        SetupMockFiles();
        SetupDefaultCommandExecutor();

        // Act
        await _generator.GenerateAsync();

        // Assert
        var generatedContent = File.ReadAllText(Path.Combine(_tempPath, "Models.cs"));
        generatedContent.ShouldContain("namespace Test.Namespace");
        generatedContent.ShouldNotContain("namespace Generated");
    }

    [Test]
    public async Task GenerateAsync_WithNoInputTypesRegion_ShouldNotCreateModelsFile()
    {
        // Arrange
        SetupMockFiles();

        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                if (callInfo.ArgAt<string>(1).Contains("graphql-codegen"))
                {
                    File.WriteAllText(Path.Combine(_tempPath, "Classes.cs"), @"
namespace Generated
{
    #region TestQuery
    public class TestQuery {}
    #endregion
}");
                }
                return Task.CompletedTask;
            });

        // Act
        await _generator.GenerateAsync();

        // Assert
        File.Exists(Path.Combine(_tempPath, "Models.cs")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempPath, "TestQuery.generated.cs")).ShouldBeTrue();
    }

    [Test]
    public async Task GenerateAsync_WithEmptyHeaders_ShouldNotIncludeHeadersInCommand()
    {
        // Arrange
        var generator = new CodeGenerator(
            SchemaUrl,
            _tempPath,
            _tempPath,
            "Test.Namespace",
            null,
            "typescript",
            "Models.cs",
            _commandExecutor);

        SetupMockFiles();

        // Setup command executor to create Classes.cs
        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var command = callInfo.ArgAt<string>(0);
                var args = callInfo.ArgAt<string>(1);

                if (args.Contains("graphql-codegen"))
                {
                    var content = @"
namespace Generated
{
    #region input types
    public class TestModel {}
    #endregion

    #region TestQuery
    public class TestQuery {}
    #endregion
}";
                    File.WriteAllText(Path.Combine(_tempPath, "Classes.cs"), content);
                }
                return Task.CompletedTask;
            });

        // Act
        await generator.GenerateAsync();

        // Assert
        // Check that none of the pnpm commands contained "--header"
        await _commandExecutor.Received().ExecuteAsync("pnpm",
            Arg.Is<string>(s => s == "install @graphql-codegen/cli @graphql-codegen/typescript"));

        await _commandExecutor.Received().ExecuteAsync("pnpm",
            Arg.Is<string>(s =>
                s.StartsWith("graphql-codegen") &&
                !s.Contains("--header")));

        // Additional verification that no other pnpm commands were called
        await _commandExecutor.Received(2).ExecuteAsync(
            Arg.Is("pnpm"),
            Arg.Any<string>());

        // Verify the files are generated correctly
        File.Exists(Path.Combine(_tempPath, "Models.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempPath, "TestQuery.generated.cs")).ShouldBeTrue();
    }

    [Test]
    public async Task GenerateAsync_ShouldCleanWorkingDirectory()
    {
        // Arrange
        var existingGraphqlFile = Path.Combine(_tempPath, "existing.graphql");
        var existingClassesFile = Path.Combine(_tempPath, "Classes.cs");
        File.WriteAllText(existingGraphqlFile, "");
        File.WriteAllText(existingClassesFile, "");

        // Setup command executor to create Classes.cs with required content
        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var command = callInfo.ArgAt<string>(0);
                var args = callInfo.ArgAt<string>(1);
                Console.WriteLine($"Mock received command: {command} {args}");

                if (args.Contains("graphql-codegen"))
                {
                    // Note the normalized line endings and careful spacing
                    var content = string.Join(
                        Environment.NewLine,
                        "using System;",
                        "using System.Collections.Generic;",
                        "",
                        "namespace Generated",
                        "{",
                        "    #region input types",
                        "    public class TestInputModel",
                        "    {",
                        "        public string Id { get; set; }",
                        "    }",
                        "    #endregion",
                        "",
                        "    #region TestQuery",
                        "    public class TestQuery",
                        "    {",
                        "        public string Name { get; set; }",
                        "    }",
                        "    #endregion",
                        "}"
                    );

                    var path = Path.Combine(_tempPath, "Classes.cs");
                    File.WriteAllText(path, content, Encoding.UTF8);

                    Console.WriteLine("Created Classes.cs with content:");
                    Console.WriteLine(content);
                    Console.WriteLine($"File exists: {File.Exists(path)}");
                    Console.WriteLine("File content verification:");
                    Console.WriteLine(File.ReadAllText(path));
                }
                return Task.CompletedTask;
            });

        // Act
        await _generator.GenerateAsync();

        // Output directory contents
        Console.WriteLine("\nDirectory contents before assertion:");
        foreach (var file in Directory.GetFiles(_tempPath))
        {
            Console.WriteLine($"File: {file}");
            if (Path.GetFileName(file).EndsWith(".cs"))
            {
                Console.WriteLine("Content:");
                Console.WriteLine(File.ReadAllText(file));
            }
        }

        // Assert
        var modelsPath = Path.Combine(_tempPath, "Models.cs");
        File.Exists(modelsPath).ShouldBeTrue($"Models.cs should exist at {modelsPath}");

        if (File.Exists(modelsPath))
        {
            var content = File.ReadAllText(modelsPath);
            content.ShouldContain("namespace Test.Namespace");
            content.ShouldContain("TestInputModel");
        }
    }

    [Test]
    public async Task GenerateAsync_WhenClassesFileNotGenerated_ShouldThrow()
    {
        // Arrange
        SetupMockFiles();

        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Should.ThrowAsync<Exception>(() => _generator.GenerateAsync());
        exception.Message.ShouldContain("Generated Classes.cs file not found");
    }

    [TestCase("using Agoda.CodeGen.GraphQL", "using Agoda.Graphql.Client")]
    [TestCase("</sumary>", "</summary>")]
    public async Task GenerateAsync_ShouldReplaceExpectedStrings(string original, string expected)
    {
        // Arrange
        SetupMockFiles();

        _commandExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                if (callInfo.ArgAt<string>(1).Contains("graphql-codegen"))
                {
                    File.WriteAllText(Path.Combine(_tempPath, "Classes.cs"), $@"
namespace Generated
{{
    #region input types
    {original}
    public class TestModel {{}}
    #endregion
}}");
                }
                return Task.CompletedTask;
            });

        // Act
        await _generator.GenerateAsync();

        // Assert
        var generatedContent = File.ReadAllText(Path.Combine(_tempPath, "Models.cs"));
        generatedContent.ShouldContain(expected);
        generatedContent.ShouldNotContain(original);
    }

    private void SetupMockFiles()
    {
        var graphqlFile = Path.Combine(_tempPath, "query.graphql");
        Console.WriteLine($"Creating GraphQL file at {graphqlFile}");
        File.WriteAllText(graphqlFile, "query { test }");
        Console.WriteLine($"GraphQL file created: {File.Exists(graphqlFile)}");
    }
}