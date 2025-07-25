﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Corvus.Json.CodeGenerator;

/// <summary>
/// Spectre.Console.Cli command for code generation.
/// </summary>
internal class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    /// <summary>
    /// Settings for the generate command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--rootNamespace")]
        [Description("The default root namespace for generated types.")]
        public string? RootNamespace { get; init; }

        [CommandOption("--rootPath")]
        [Description("The path in the document for the root type.")]
        public string? RootPath { get; init; }

        [CommandOption("--useSchema")]
        [Description("Override the fallback schema variant to use. If NotSpecified, and it cannot be inferred from the schema itself, it will use Draft2020-12.")]
        [DefaultValue(SchemaVariant.NotSpecified)]
        public SchemaVariant UseSchema { get; init; }

        [CommandOption("--outputMapFile")]
        [Description("The name to use for a map file which includes details of the files that were written.")]
        public string? OutputMapFile { get; init; }

        [CommandOption("--outputPath")]
        [Description("The path to which to write the generated code.")]
        public string? OutputPath { get; init; }

        [CommandOption("--outputRootTypeName")]
        [Description("The .NET type name for the root type.")]
        [DefaultValue(null)]
        public string? OutputRootTypeName { get; init; }

        [CommandOption("--rebaseToRootPath")]
        [Description("If a --rootPath is specified, rebase the document as if it was rooted on the specified element.")]
        [DefaultValue(false)]
        public bool RebaseToRootPath { get; init; }

        [CommandOption("--assertFormat")]
        [Description("If --assertFormat is specified, assert format specifications.")]
        [DefaultValue(true)]
        public bool AssertFormat { get; init; }

        [Description("The path to the schema file to process.")]
        [CommandArgument(0, "<schemaFile>")]
        [NotNull] // <> => NotNull
        public string? SchemaFile { get; init; }

        [CommandOption("--disableOptionalNamingHeuristics")]
        [Description("Disables all optional naming heuristics.")]
        [DefaultValue(false)]
        public bool DisableOptionalNamingHeuristics { get; init; }

        [CommandOption("--disableNamingHeuristic")]
        [Description("Disables the specific naming heuristic.")]
        public string[]? DisableNamingHeuristic { get; init; }

        [CommandOption("--optionalAsNullable")]
        [Description("If NullOrUndefined, optional properties are emitted as .NET nullable values.")]
        [DefaultValue(OptionalAsNullable.None)]
        public OptionalAsNullable OptionalAsNullable { get; init; }

        [CommandOption("--useImplicitOperatorString")]
        [Description("If true, conversion operators to string are implicit, rather than explicit.")]
        [DefaultValue(false)]
        public bool UseImplicitOperatorString { get; init; }

        [CommandOption("--useUnixLineEndings")]
        [Description("If true, line endings emitted using the Unix style ('\\n'). Otherwise, it uses the Windows style ('\\r\\n').")]
        [DefaultValue(false)]
        public bool UseUnixLineEndings { get; init; }

        [CommandOption("--yaml")]
        [Description("If true, YAML support is enabled. You may use YAML, JSON or a mixture of such documents.")]
        [DefaultValue(false)]
        public bool SupportYaml { get; init; }

        [CommandOption("--addExplicitUsings")]
        [Description("If true, the generate will include using statements for the standard implicit usings.")]
        [DefaultValue(false)]
        public bool AddExplicitUsings { get; init; }
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(settings.SchemaFile); // We will never see this exception if the framework is doing its job; it should have blown up inside the CLI command handling
        ArgumentNullException.ThrowIfNullOrEmpty(settings.RootNamespace); // We will never see this exception if the framework is doing its job; it should have blown up inside the CLI command handling

        var config = GeneratorConfig.Create(
            settings.RootNamespace,
            [GeneratorConfig.GenerationSpecification.Create(
                schemaFile: settings.SchemaFile,
                outputRootTypeName: settings.OutputRootTypeName.AsNullableJsonString(),
                rebaseToRootPath: settings.RebaseToRootPath,
                rootPath: settings.RootPath.AsNullableJsonString())],
            additionalFiles: null,
            assertFormat: settings.AssertFormat,
            disabledNamingHeuristics: settings.DisableNamingHeuristic is string[] disabledItems ? JsonArray.FromRange(disabledItems) : default(GeneratorConfig.JsonStringArray?),
            disableOptionalNameHeuristics: settings.DisableOptionalNamingHeuristics,
            optionalAsNullable: settings.OptionalAsNullable.ToString(),
            outputMapFile: settings.OutputMapFile.AsNullableJsonString(),
            outputPath: settings.OutputPath.AsNullableJsonString(),
            useSchema: settings.UseSchema != SchemaVariant.NotSpecified ? (GeneratorConfig.UseSchema)settings.UseSchema.ToString() : default(GeneratorConfig.UseSchema?),
            useImplicitOperatorString: settings.UseImplicitOperatorString,
            useUnixLineEndings: settings.UseUnixLineEndings,
            supportYaml: settings.SupportYaml,
            addExplicitUsings: settings.AddExplicitUsings);

        return GenerationDriver.GenerateTypes(config);
    }
}
