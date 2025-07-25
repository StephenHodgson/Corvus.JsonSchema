﻿// <copyright file="PropertiesValidationHandler.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Microsoft.CodeAnalysis.CSharp;

namespace Corvus.Json.CodeGeneration.CSharp;

/// <summary>
/// A properties property validation handler.
/// </summary>
public class PropertiesValidationHandler : IChildObjectPropertyValidationHandler
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="PropertiesValidationHandler"/>.
    /// </summary>
    public static PropertiesValidationHandler Instance { get; } = new();

    /// <inheritdoc/>
    public uint ValidationHandlerPriority { get; } = ValidationPriorities.Last;

    /// <inheritdoc/>
    public CodeGenerator AppendValidationCode(CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        return generator;
    }

    /// <inheritdoc/>
    public CodeGenerator AppendValidateMethodSetup(CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        return generator;
    }

    /// <inheritdoc/>
    public CodeGenerator AppendObjectPropertyValidationCode(CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.ExplicitProperties() is IReadOnlyCollection<PropertyDeclaration> properties)
        {
            generator
                .AppendSeparatorLine();

            bool first = true;
            foreach (PropertyDeclaration property in properties)
            {
                if (first)
                {
                    first = false;
                    generator.AppendIndent(string.Empty);
                }
                else
                {
                    generator.AppendIndent("else ");
                }

                AppendPropertyValidation(generator, property);
            }

            if (typeDeclaration.LocalEvaluatedPropertyType() is FallbackObjectPropertyType localEvaluatedProperty)
            {
                bool enumeratorIsCorrectType = localEvaluatedProperty.ReducedType.LocatedSchema.Location == typeDeclaration.FallbackObjectPropertyType()?.ReducedType.LocatedSchema.Location;
                AppendLocalEvaluatedProperty(generator, localEvaluatedProperty, enumeratorIsCorrectType);
            }

            if (typeDeclaration.LocalAndAppliedEvaluatedPropertyType() is FallbackObjectPropertyType localAndAppliedEvaluatedProperty)
            {
                bool enumeratorIsCorrectType = localAndAppliedEvaluatedProperty.ReducedType.LocatedSchema.Location == typeDeclaration.FallbackObjectPropertyType()?.ReducedType.LocatedSchema.Location;
                AppendLocalAndAppliedEvaluatedProperty(generator, localAndAppliedEvaluatedProperty, enumeratorIsCorrectType);
            }
        }

        return generator;

        static void AppendPropertyValidation(CodeGenerator generator, PropertyDeclaration property)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                .AppendLine(
                    "if (property.NameEquals(",
                    generator.JsonPropertyNamesClassName(),
                    ".",
                    property.DotnetPropertyName(),
                    "Utf8, ",
                    generator.JsonPropertyNamesClassName(),
                    ".",
                    property.DotnetPropertyName(),
                    "))")
                .AppendLineIndent("{")
                .PushIndent();

            if (property.RequiredOrOptional == RequiredOrOptional.Required &&
                !property.ReducedPropertyType.HasDefaultValue())
            {
                string hasSeenField = RequiredValidationHandler.GetHasSeenVariableName(generator, property);
                generator
                    .AppendLineIndent(hasSeenField, " = true;");
            }

            if (property.LocalOrComposed == LocalOrComposed.Local)
            {
                generator
                        .AppendLineIndent("result = result.WithLocalProperty(propertyCount);")
                        .AppendLineIndent("if (level > ValidationLevel.Basic)")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent(
                                "result = result.PushValidationLocationReducedPathModifierAndProperty(new(",
                                SymbolDisplay.FormatLiteral(property.KeywordPathModifier, true),
                                "), ",
                                generator.JsonPropertyNamesClassName(),
                                ".",
                                property.DotnetPropertyName(),
                                ");")
                        .PopIndent()
                        .AppendLineIndent("}")
                        .AppendSeparatorLine()
                        .AppendLineIndent("ValidationContext propertyResult = property.Value.As<", property.ReducedPropertyType.FullyQualifiedDotnetTypeName(), ">().Validate(result.CreateChildContext(), level);")
                        .AppendLineIndent("if (level == ValidationLevel.Flag && !propertyResult.IsValid)")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return propertyResult;")
                        .PopIndent()
                        .AppendLineIndent("}")
                        .AppendSeparatorLine()
                        .AppendLineIndent("result = result.MergeResults(propertyResult.IsValid, level, propertyResult);")
                        .AppendSeparatorLine()
                        .AppendLineIndent("if (level > ValidationLevel.Basic)")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("result = result.PopLocation();")
                        .PopIndent()
                        .AppendLineIndent("}");
            }

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendLocalEvaluatedProperty(CodeGenerator generator, FallbackObjectPropertyType fallbackProperty, bool enumeratorIsCorrectType)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                .AppendLineIndent("if (!result.HasEvaluatedLocalProperty(propertyCount))")
                .AppendLineIndent("{")
                .PushIndent();

            AppendFallbackPropertyValidation(generator, fallbackProperty, enumeratorIsCorrectType);

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendLocalAndAppliedEvaluatedProperty(CodeGenerator generator, FallbackObjectPropertyType fallbackProperty, bool enumeratorIsCorrectType)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                .AppendLineIndent("if (!result.HasEvaluatedLocalOrAppliedProperty(propertyCount))")
                .AppendLineIndent("{")
                .PushIndent();

            AppendFallbackPropertyValidation(generator, fallbackProperty, enumeratorIsCorrectType);

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendFallbackPropertyValidation(CodeGenerator generator, FallbackObjectPropertyType fallbackPropertyType, bool enumeratorIsCorrectType)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                    .AppendLineIndent("if (level > ValidationLevel.Basic)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("string localEvaluatedPropertyName = (propertyNameAsString ??= property.Name.GetString());");

            if (fallbackPropertyType.ReducedPathModifier.Fragment.Length > 1)
            {
                generator
                            .AppendLineIndent(
                                "result = result.PushValidationLocationReducedPathModifierAndProperty(new JsonReference(",
                                SymbolDisplay.FormatLiteral(fallbackPropertyType.KeywordPathModifier, true),
                                ").AppendUnencodedPropertyNameToFragment(localEvaluatedPropertyName).AppendFragment(new(",
                                SymbolDisplay.FormatLiteral(fallbackPropertyType.ReducedPathModifier, true),
                                ")), localEvaluatedPropertyName);");
            }
            else
            {
                generator
                            .AppendLineIndent(
                                "result = result.PushValidationLocationReducedPathModifierAndProperty(new JsonReference(",
                                SymbolDisplay.FormatLiteral(fallbackPropertyType.KeywordPathModifier, true),
                                ").AppendUnencodedPropertyNameToFragment(localEvaluatedPropertyName), localEvaluatedPropertyName);");
            }

            generator
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine();

            if (!fallbackPropertyType.ReducedType.IsBuiltInJsonAnyType())
            {
                if (enumeratorIsCorrectType)
                {
                    generator
                        .AppendLineIndent("ValidationContext propertyResult = property.Value.Validate(result.CreateChildContext(), level);");
                }
                else
                {
                    generator
                        .AppendLineIndent("ValidationContext propertyResult = property.Value.As<", fallbackPropertyType.ReducedType.FullyQualifiedDotnetTypeName(), ">().Validate(result.CreateChildContext(), level);");
                }

                generator
                    .AppendLineIndent("if (level == ValidationLevel.Flag && !propertyResult.IsValid)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return propertyResult;")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("result = result.MergeResults(propertyResult.IsValid, level, propertyResult);");
            }

            generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (level > ValidationLevel.Basic)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("result = result.PopLocation();")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("result = result.WithLocalProperty(propertyCount);");
        }
    }

    /// <inheritdoc/>
    public CodeGenerator AppendValidationSetup(CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        return generator;
    }

    /// <inheritdoc/>
    public bool RequiresPropertyNameAsString(TypeDeclaration typeDeclaration) => typeDeclaration.LocalEvaluatedPropertyType() is not null || typeDeclaration.LocalAndAppliedEvaluatedPropertyType() is not null;
}