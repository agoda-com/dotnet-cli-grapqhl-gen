# dotnet-cli-graphql-gen: GraphQL Code Generation Made Easy!

## Overview

Welcome to dotnet-cli-graphql-gen, an open-source CLI tool that wraps npm package graphql-codegen to add some missing parts.

## Requirements

- .NET 8.0 or higher (Because we believe in staying current!)
- pnpm (Don't worry, we'll install it if you don't have it)

## Installation

### Global Tool Installation

Install it globally (because great tools should be available everywhere):
```bash
dotnet tool install --global Agoda.GrapqhlGen
```

### Local Project Installation

Or keep it project-specific:
```bash
dotnet new tool-manifest # if you haven't already
dotnet tool install Agoda.GrapqhlGen
```

## Quick Start

Generate your GraphQL client code with a single command:

```bash
grapqhlgen \
  --schema-url "https://your.graphql.api" \
  --input-path "./graphql" \
  --output-path "./Generated" \
  --namespace "YourCompany.YourProject" \
  --headers "API-Key: your-key"
```

## Command Options

- `--schema-url` (Required): URL of your GraphQL schema
- `--input-path` (Required): Directory containing your .graphql files
- `--output-path` (Required): Where to save generated files
- `--namespace` (Optional): Base namespace for generated code (Default: "Generated")
- `--headers` (Optional): Headers for schema request (Format: "Key: Value")
- `--template` (Optional): Code generation template (Default: "typescript")
- `--model-file` (Optional): Name of the generated models file (Default: "Models.cs")
- `--log-level` (Optional): Set logging verbosity (Default: "Information")

## Contributing

We love contributions! Whether you're fixing bugs, improving documentation, or adding new features, check out our [Contributing Guide](CONTRIBUTING.md) for details on getting started.

## Best Practices

- Keep your GraphQL queries in separate .graphql files
- Use meaningful names for your queries and mutations
- Organize your GraphQL files by feature or domain
- Version control your GraphQL files alongside your code

## And Finally...

Remember, in the world of GraphQL, there are two types of developers: those who use code generation tools, and those who wish they had started using them sooner! With dotnet-cli-graphql-gen, you'll never want to write GraphQL client code by hand again.

Happy coding, and may your queries always resolve! 🚀

## License

Apache 2.0 - feel free to use this tool in your projects, whether personal or commercial. Just don't blame us if your GraphQL queries start writing themselves! 😉