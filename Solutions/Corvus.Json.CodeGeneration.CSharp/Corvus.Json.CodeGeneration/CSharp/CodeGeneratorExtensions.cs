﻿// <copyright file="CodeGeneratorExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Web;
using Microsoft.CodeAnalysis.CSharp;

namespace Corvus.Json.CodeGeneration.CSharp;

/// <summary>
/// A delegate for a callback to assign a backing field with a value.
/// </summary>
/// <param name="generator">The code generator to which to append the assigment code.</param>
/// <param name="typeDeclaration">The type declaration.</param>
/// <param name="backingFieldName">The backing field name.</param>
public delegate void AppendConstructorBackingFieldAssignmentCallback(CodeGenerator generator, TypeDeclaration typeDeclaration, string backingFieldName);

/// <summary>
/// Extension methods for the <see cref="CodeGenerator"/>.
/// </summary>
internal static partial class CodeGeneratorExtensions
{
    /// <summary>
    /// Append using statements for the given namespaces.
    /// </summary>
    /// <param name="generator">The generator to which to append usings.</param>
    /// <param name="namespaces">The namespace to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendUsings(this CodeGenerator generator, params ConditionalCodeSpecification[] namespaces)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        ConditionalCodeSpecification.AppendConditionalsInOrder(
            generator,
            namespaces,
            static (g, a, _) => Append(g, a));

        return generator;

        static void Append(CodeGenerator generator, Action<CodeGenerator> action)
        {
            generator.Append("using ");
            action(generator);
            generator.AppendLine(";");
        }
    }

    /// <summary>
    /// Append a namespace statement.
    /// </summary>
    /// <param name="generator">The generator to which to append usings.</param>
    /// <param name="ns">The namespace to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginNamespace(this CodeGenerator generator, string ns)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .Append("namespace ")
            .Append(ns)
            .AppendLine(";")
            .PushMemberScope(ns, ScopeType.TypeContainer);
    }

    /// <summary>
    /// Append a namespace statement.
    /// </summary>
    /// <param name="generator">The generator to which to append usings.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator EndNamespace(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .PopMemberScope();
    }

    /// <summary>
    /// Append the start of a public readonly property declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the property.</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="nullable">If true, make the property type nullable.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginPublicReadOnlyPropertyDeclaration(this CodeGenerator generator, string propertyType, string propertyName, bool nullable = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendLineIndent("public ", propertyType, nullable ? "? " : " ", propertyName)
            .AppendLineIndent("{")
            .PushIndent()
            .AppendLineIndent("get")
            .AppendLineIndent("{")
            .PushIndent();
    }

    /// <summary>
    /// Append the start of a public readonly property declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator EndReadOnlyPropertyDeclaration(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .PopIndent()
            .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Begin a local method declaration for an explicit name which will be reserved in the scope.
    /// </summary>
    /// <param name="generator">The generator to which to append the local method.</param>
    /// <param name="visibilityAndModifiers">The visibility and modifiers for the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <param name="methodName">The method name, which will have been reserved in the scope.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginLocalMethodDeclaration(
        this CodeGenerator generator,
        string visibilityAndModifiers,
        string returnType,
        string methodName,
        params MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendIndent(visibilityAndModifiers)
            .Append(' ')
            .Append(returnType)
            .Append(' ')
            .Append(methodName)
            .PushMemberScope(methodName, ScopeType.Method) // Then move to the method scope before appending parameters
            .AppendParameterList(parameters)
            .AppendLineIndent("{")
            .PushIndent();
    }

    /// <summary>
    /// Begin a method declaration for an explicit name which will be reserved in the scope.
    /// </summary>
    /// <param name="generator">The generator to which to append the method.</param>
    /// <param name="visibilityAndModifiers">The visibility and modifiers for the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <param name="methodName">The method name, which will be reserved in the scope.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginReservedMethodDeclaration(
        this CodeGenerator generator,
        string visibilityAndModifiers,
        string returnType,
        string methodName,
        params MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendIndent(visibilityAndModifiers)
            .Append(' ')
            .Append(returnType)
            .Append(' ')
            .Append(methodName)
            .ReserveNameIfNotReserved(methodName) // Reserve the method name in the parent scope
            .PushMemberScope(methodName, ScopeType.Method) // Then move to the method scope before appending parameters
            .AppendParameterList(parameters)
            .AppendLineIndent("{")
            .PushIndent();
    }

    /// <summary>
    /// Begin a method declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the method.</param>
    /// <param name="visibilityAndModifiers">The visibility and modifiers for the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <param name="methodName">The method name, which will be reserved in the scope.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginMethodDeclaration(
        this CodeGenerator generator,
        string visibilityAndModifiers,
        string returnType,
        MemberName methodName,
        params MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string realisedMethodName = generator.GetOrAddMemberName(methodName);

        return generator
            .AppendIndent(visibilityAndModifiers)
            .Append(' ')
            .Append(returnType)
            .Append(' ')
            .Append(realisedMethodName)
            .PushMemberScope(realisedMethodName, ScopeType.Method)
            .AppendParameterList(parameters)
            .AppendLineIndent("{")
            .PushIndent();
    }

    /// <summary>
    /// Append the backing fields for the implied core types.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="impliedCoreTypes">The implied core types.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendBackingFields(this CodeGenerator generator, CoreTypes impliedCoreTypes)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendBackingField("Backing", "backing")
            .AppendBackingField("JsonElement", "jsonElementBacking")
            .AppendBackingField("string", "stringBacking", impliedCoreTypes, CoreTypes.String)
            .AppendBackingField("bool", "boolBacking", impliedCoreTypes, CoreTypes.Boolean)
            .AppendBackingField("BinaryJsonNumber", "numberBacking", impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
            .AppendBackingField("ImmutableList<JsonAny>", "arrayBacking", impliedCoreTypes, CoreTypes.Array)
            .AppendBackingField("ImmutableList<JsonObjectProperty>", "objectBacking", impliedCoreTypes, CoreTypes.Object);
    }

    /// <summary>
    /// Append the schema location static property for the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendSchemaLocationStaticProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("SchemaLocation")
            .AppendSeparatorLine()
            .AppendBlockIndent(
            """
            /// <summary>
            /// Gets the schema location from which this type was generated.
            /// </summary>
            """)
            .AppendIndent("public static string SchemaLocation { get; } = ")
            .Append(SymbolDisplay.FormatLiteral(typeDeclaration.RelativeSchemaLocation, true))
            .AppendLine(";");
    }

    /// <summary>
    /// Append the static property which provides a null instance of the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendNullInstanceStaticProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("Null")
            .AppendSeparatorLine()
            .AppendBlockIndent(
            """
            /// <summary>
            /// Gets a Null instance.
            /// </summary>
            """)
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" Null { get; } = new(JsonValueHelpers.NullElement);");
    }

    /// <summary>
    /// Append the static property which provides a null instance of the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendUndefinedInstanceStaticProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("Undefined")
            .AppendSeparatorLine()
            .AppendBlockIndent(
            """
            /// <summary>
            /// Gets an Undefined instance.
            /// </summary>
            """)
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" Undefined { get; }");
    }

    /// <summary>
    /// Append the static property which provides a default instance of the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendDefaultInstanceStaticProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("DefaultInstance")
            .AppendSeparatorLine()
            .AppendBlockIndent(
            """
            /// <summary>
            /// Gets the default instance.
            /// </summary>
            """)
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append(" DefaultInstance { get; }");

        return typeDeclaration.DefaultValue().ValueKind switch
        {
            JsonValueKind.Undefined => generator.AppendLine(),
            JsonValueKind.Null => generator
                                    .Append(" = ")
                                    .Append(typeDeclaration.DotnetTypeName())
                                    .AppendLine(".ParseValue(\"null\"u8);"),
            _ => generator
                    .Append(" = ")
                    .Append(typeDeclaration.DotnetTypeName())
                    .Append(".ParseValue(")
                    .Append(SymbolDisplay.FormatLiteral(typeDeclaration.DefaultValue().GetRawText(), true))
                    .AppendLine("u8);"),
        };
    }

    /// <summary>
    /// Append the static property which provides a const instance of the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendConstInstanceStaticProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.Keywords().OfType<ISingleConstantValidationKeyword>().FirstOrDefault()
            is ISingleConstantValidationKeyword keyword)
        {
            string validationClassName = generator.ValidationClassName();
            string constantFieldName =
                generator.GetStaticReadOnlyFieldNameInScope(
                    keyword.Keyword,
                    rootScope: generator.ValidationClassScope());

            generator
                .ReserveName("ConstInstance")
                .AppendSeparatorLine()
                .AppendBlockIndent(
                """
                /// <summary>
                /// Gets the const instance.
                /// </summary>
                """)
                .AppendIndent("public static ")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(" ConstInstance => ");

            if (keyword.TryGetConstantValue(typeDeclaration, out JsonElement constantValue) &&
                constantValue.ValueKind == JsonValueKind.Number)
            {
                generator
                    .AppendLine("new(", validationClassName, ".", constantFieldName, ");");
            }
            else
            {
                generator
                    .AppendLine(validationClassName, ".", constantFieldName, ".As<", typeDeclaration.DotnetTypeName(), ">();");
            }
        }

        return generator;
    }

    /// <summary>
    /// Append the classes which provide sets of constant values.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the enum values class.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAnyOfConstantValuesClasses(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.AnyOfConstantValues() is IReadOnlyDictionary<IAnyOfConstantValidationKeyword, JsonElement[]> constantValues)
        {
            foreach (IAnyOfConstantValidationKeyword keyword in constantValues.Keys)
            {
                if (generator.IsCancellationRequested)
                {
                    return generator;
                }

                JsonElement[] constants = constantValues[keyword];
                string className = generator.GetUniqueClassNameInScope(keyword.Keyword, suffix: "Values");

                generator
                    .PushMemberScope(className, ScopeType.Type)
                    .AppendSeparatorLine()
                    .AppendLineIndent("/// <summary>")
                    .AppendLineIndent("/// Constant values for the ", keyword.Keyword, " keyword.")
                    .AppendLineIndent("/// </summary>")
                    .AppendLineIndent("public static class ", className)
                    .AppendLineIndent("{")
                    .PushIndent();

                for (int i = 0; i < constants.Length; i++)
                {
                    AppendAnyOfConstantValue(generator, typeDeclaration, keyword, constants, i);
                }

                generator
                    .PopIndent()
                    .AppendLineIndent("}")
                    .PopMemberScope();
            }
        }

        return generator;

        static void AppendAnyOfConstantValue(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, JsonElement[] constants, int i)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            bool requiresIndex = constants.Length > 1;
            JsonElement constantValue = constants[i];

            switch (constantValue.ValueKind)
            {
                case JsonValueKind.String:
                    AppendStringProperties(generator, typeDeclaration, keyword, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
                case JsonValueKind.Number:
                    AppendNumberProperties(generator, typeDeclaration, keyword, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    AppendBooleanProperties(generator, typeDeclaration, keyword, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
                case JsonValueKind.Object:
                    AppendObjectProperties(generator, typeDeclaration, keyword, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
                case JsonValueKind.Array:
                    AppendArrayProperties(generator, typeDeclaration, keyword, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
                case JsonValueKind.Null:
                    AppendNullProperties(generator, typeDeclaration, requiresIndex ? (i + 1).ToString() : null, constantValue);
                    break;
            }
        }

        static void AppendStringProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            if (constantValue.GetString() is string stringValue)
            {
                string constField =
                    generator.GetPropertyNameInScope(
                        keyword.Keyword,
                        rootScope: generator.ValidationClassScope(),
                        suffix: index);

                string propertyName = generator.GetUniquePropertyNameInScope(stringValue);

                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("/// <summary>")
                    .AppendLineIndent("/// Gets the string '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(stringValue, false)), "'")
                    .AppendLineIndent("/// as a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                    .AppendLineIndent("/// </summary>")
                    .AppendLineIndent(
                        "public static ",
                        typeDeclaration.DotnetTypeName(),
                        " ",
                        propertyName,
                        " { get; } = ",
                        generator.ValidationClassName(),
                        ".",
                        constField,
                        ".As<",
                        typeDeclaration.DotnetTypeName(),
                        ">();")
                    .AppendSeparatorLine()
                    .AppendLineIndent("/// <summary>")
                    .AppendLineIndent("/// Gets the string '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(stringValue, false)), "'")
                    .AppendLineIndent("/// as a UTF8 byte array.")
                    .AppendLineIndent("/// </summary>")
                    .ReserveName(propertyName, suffix: "Utf8")
                    .AppendLineIndent(
                        "public static ReadOnlySpan<byte> ",
                        propertyName,
                        "Utf8 => ",
                        generator.ValidationClassName(),
                        ".",
                        constField,
                        "Utf8;");
            }
        }

        static void AppendNumberProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string numberValue = constantValue.GetRawText();
            string constField =
                generator.GetPropertyNameInScope(
                    keyword.Keyword,
                    rootScope: generator.ValidationClassScope(),
                    suffix: index);

            string propertyName = index is string i ? $"Item{index}" : "Item1";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the number '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(numberValue, false)), "'")
                .AppendLineIndent("/// as a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent(
                    "public static ",
                    typeDeclaration.DotnetTypeName(),
                    " ",
                    propertyName,
                    " { get; } = new(",
                    generator.ValidationClassName(),
                    ".",
                    constField,
                    ");");
        }

        static void AppendBooleanProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string booleanValue = constantValue.GetRawText();
            string constField =
                generator.GetPropertyNameInScope(
                    keyword.Keyword,
                    rootScope: generator.ValidationClassScope(),
                    suffix: index);

            string propertyName = index is string i ? $"Item{index}" : "Item1";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the boolean value '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(booleanValue, false)), "'")
                .AppendLineIndent("/// as a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent(
                    "public static ",
                    typeDeclaration.DotnetTypeName(),
                    " ",
                    propertyName,
                    " { get; } = ",
                    generator.ValidationClassName(),
                    ".",
                    constField,
                    ".As<",
                    typeDeclaration.DotnetTypeName(),
                    ">();");
        }

        static void AppendNullProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string booleanValue = constantValue.GetRawText();
            string propertyName = index is string i ? $"Item{index}" : "Item1";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the null value for a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent(
                    "public static ",
                    typeDeclaration.DotnetTypeName(),
                    " ",
                    propertyName,
                    " => ",
                    typeDeclaration.DotnetTypeName(),
                    ".Null;");
        }

        static void AppendObjectProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string objectValue = constantValue.GetRawText();
            string constField =
                generator.GetPropertyNameInScope(
                    keyword.Keyword,
                    rootScope: generator.ValidationClassScope(),
                    suffix: index);

            string propertyName = index is string i ? $"Item{index}" : "Item1";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the object value '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(objectValue, false)), "'")
                .AppendLineIndent("/// as a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent(
                    "public static ",
                    typeDeclaration.DotnetTypeName(),
                    " ",
                    propertyName,
                    " { get; } = ",
                    generator.ValidationClassName(),
                    ".",
                    constField,
                    ".As<",
                    typeDeclaration.DotnetTypeName(),
                    ">();");
        }

        static void AppendArrayProperties(CodeGenerator generator, TypeDeclaration typeDeclaration, IKeyword keyword, string? index, JsonElement constantValue)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string arrayValue = constantValue.GetRawText();
            string constField =
                generator.GetPropertyNameInScope(
                    keyword.Keyword,
                    rootScope: generator.ValidationClassScope(),
                    suffix: index);

            string propertyName = index is string i ? $"Item{index}" : "Item1";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the array value '", HttpUtility.HtmlEncode(SymbolDisplay.FormatLiteral(arrayValue, false)), "'")
                .AppendLineIndent("/// as a <see cref=\"", typeDeclaration.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent(
                    "public static ",
                    typeDeclaration.DotnetTypeName(),
                    " ",
                    propertyName,
                    " { get; } = ",
                    generator.ValidationClassName(),
                    ".",
                    constField,
                    ".As<",
                    typeDeclaration.DotnetTypeName(),
                    ">();");
        }
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonAny instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsAnyProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsAny")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendIndent("public JsonAny AsAny");

        if (typeDeclaration.IsCorvusJsonExtendedJsonAny())
        {
            generator
                .AppendLine(" => this;");
        }
        else
        {
            generator
                .AppendLine()
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("get")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendConditionalConstructFromBacking(
                            "Backing.JsonElement",
                            "jsonElementBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Any)
                        .AppendConditionalConstructFromBacking(
                            "Backing.String",
                            "stringBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.String)
                        .AppendConditionalConstructFromBacking(
                            "Backing.Bool",
                            "boolBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Boolean)
                        .AppendConditionalConstructFromBacking(
                            "Backing.Number",
                            "numberBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Number | CoreTypes.Integer)
                        .AppendConditionalConstructFromBacking(
                            "Backing.Array",
                            "arrayBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Array)
                        .AppendConditionalConstructFromBacking(
                            "Backing.Object",
                            "objectBacking",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Object)
                        .AppendReturnNullInstanceIfNull()
                        .AppendSeparatorLine()
                        .AppendLineIndent("return JsonAny.Undefined;")
                    .PopIndent()
                    .AppendLineIndent("}")
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonElement instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsJsonElementProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("AsJsonElement")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent("public JsonElement AsJsonElement")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return ",
                        "jsonElementBacking",
                        ";",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Any)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.String",
                        "return JsonValueHelpers.StringToJsonElement(",
                        "stringBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.String)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Bool",
                        "return JsonValueHelpers.BoolToJsonElement(",
                        "boolBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Boolean)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Number",
                        "return JsonValueHelpers.NumberToJsonElement(",
                        "numberBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Number | CoreTypes.Integer)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Array",
                        "return JsonValueHelpers.ArrayToJsonElement(",
                        "arrayBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Array)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Object",
                        "return JsonValueHelpers.ObjectToJsonElement(",
                        "objectBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Object)
                    .AppendSeparatorLine()
                    .AppendReturnNullJsonElementIfNull()
                    .AppendSeparatorLine()
                    .AppendLineIndent("return default;")
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonString instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsStringProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsString")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent(
                (typeDeclaration.ImpliedCoreTypesOrAny() & CoreTypes.String) != 0
                    ? "public JsonString AsString"
                    : "JsonString IJsonValue.AsString")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent();

        if (typeDeclaration.DotnetTypeName() == "JsonString" && typeDeclaration.DotnetNamespace() == "Corvus.Json")
        {
            generator
                .AppendLineIndent("return this;");
        }
        else
        {
            generator
                   .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return new(",
                        "jsonElementBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Any)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.String",
                        "return new(",
                        "stringBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.String)
                    .AppendSeparatorLine()
                    .AppendLineIndent("throw new InvalidOperationException();");
        }

        return generator
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonBoolean instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsBooleanProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsBoolean")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent(
                (typeDeclaration.ImpliedCoreTypesOrAny() & CoreTypes.Boolean) != 0
                    ? "public JsonBoolean AsBoolean"
                    : "JsonBoolean IJsonValue.AsBoolean")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent();

        if (typeDeclaration.DotnetTypeName() == "JsonBoolean" && typeDeclaration.DotnetNamespace() == "Corvus.Json")
        {
            generator
                .AppendLineIndent("return this;");
        }
        else
        {
            generator
                .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return new(",
                        "jsonElementBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Any)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Bool",
                        "return new(",
                        "boolBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Boolean)
                    .AppendSeparatorLine()
                    .AppendLineIndent("throw new InvalidOperationException();");
        }

        return generator
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonNumber instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsNumberProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsNumber")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent(
                (typeDeclaration.ImpliedCoreTypesOrAny() & (CoreTypes.Number | CoreTypes.Integer)) != 0
                    ? "public JsonNumber AsNumber"
                    : "JsonNumber IJsonValue.AsNumber")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent();

        if (typeDeclaration.DotnetTypeName() == "JsonNumber" && typeDeclaration.DotnetNamespace() == "Corvus.Json")
        {
            generator
                .AppendLineIndent("return this;");
        }
        else
        {
            generator
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return new(",
                        "jsonElementBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Any)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Number",
                        "return new(",
                        "numberBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Number | CoreTypes.Integer)
                    .AppendSeparatorLine()
                    .AppendLineIndent("throw new InvalidOperationException();");
        }

        return generator
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonObject instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsObjectProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsObject")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent(
                (typeDeclaration.ImpliedCoreTypesOrAny() & CoreTypes.Object) != 0
                    ? "public JsonObject AsObject"
                    : "JsonObject IJsonValue.AsObject")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent();

        if (typeDeclaration.DotnetTypeName() == "JsonObject" && typeDeclaration.DotnetNamespace() == "Corvus.Json")
        {
            generator
                .AppendLineIndent("return this;");
        }
        else
        {
            generator
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return new(",
                        "jsonElementBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Any)
                    .AppendSeparatorLine()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Object",
                        "return new(",
                        "objectBacking",
                        ");",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Object)
                    .AppendSeparatorLine()
                    .AppendLineIndent("throw new InvalidOperationException();");
        }

        return generator
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the property which converts this instance to a JsonArray instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsArrayProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("AsArray")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent(
                (typeDeclaration.ImpliedCoreTypesOrAny() & CoreTypes.Array) != 0
                    ? "public JsonArray AsArray"
                    : "JsonArray IJsonValue.AsArray")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent();

        if (typeDeclaration.DotnetTypeName() == "JsonArray" && typeDeclaration.DotnetNamespace() == "Corvus.Json")
        {
            generator
                .AppendLineIndent("return this;");
        }
        else
        {
            generator
                        .AppendConditionalWrappedBackingValueLineIndent(
                            "Backing.JsonElement",
                            "return new(",
                            "jsonElementBacking",
                            ");",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Any)
                        .AppendSeparatorLine()
                        .AppendConditionalWrappedBackingValueLineIndent(
                            "Backing.Array",
                            "return new(",
                            "arrayBacking",
                            ");",
                            impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                            forCoreTypes: CoreTypes.Array)
                        .AppendSeparatorLine()
                        .AppendLineIndent("throw new InvalidOperationException();");
        }

        return generator
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append a property which gets a value indicating if the instance has a JsonElement backing.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendHasJsonElementBackingProperty(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("HasJsonElementBacking")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent("public bool HasJsonElementBacking")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendIndent("return ")
                    .AppendTestBacking("Backing.JsonElement")
                    .AppendLine(";")
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append a property which gets a value indicating if the instance has a .NET core type backing.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendHasDotnetBackingProperty(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("HasDotnetBacking")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent("public bool HasDotnetBacking")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendIndent("return ")
                    .AppendTestBacking("Backing.Dotnet")
                    .AppendLine(";")
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append a property which gets the <see cref="JsonValueKind"/> for the instance.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the property.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendValueKindProperty(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("ValueKind")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent("public JsonValueKind ValueKind")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("get")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.JsonElement",
                        "return ",
                        "jsonElementBacking",
                        ".ValueKind;")
                    .AppendConditionalBackingValueLineIndent(
                        "Backing.String",
                        "return JsonValueKind.String;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.String)
                    .AppendConditionalWrappedBackingValueLineIndent(
                        "Backing.Bool",
                        "return ",
                        "boolBacking",
                        " ? JsonValueKind.True : JsonValueKind.False;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Boolean)
                    .AppendConditionalBackingValueLineIndent(
                        "Backing.Number",
                        "return JsonValueKind.Number;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Number | CoreTypes.Integer)
                    .AppendConditionalBackingValueLineIndent(
                        "Backing.Array",
                        "return JsonValueKind.Array;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Array)
                    .AppendConditionalBackingValueLineIndent(
                        "Backing.Object",
                        "return JsonValueKind.Object;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Object)
                    .AppendConditionalBackingValueLineIndent(
                        "Backing.Null",
                        "return JsonValueKind.Null;",
                        impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                        forCoreTypes: CoreTypes.Null)
                    .AppendSeparatorLine()
                    .AppendLineIndent("return JsonValueKind.Undefined;")
                .PopIndent()
                .AppendLineIndent("}")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the default constructor for the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicDefaultConstructor(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.SingleConstantValue().ValueKind != JsonValueKind.Undefined)
        {
            // Don't emit this for a type that has a single constant value.
            return generator;
        }

        CoreTypes impliedCoreTypes = typeDeclaration.ImpliedCoreTypesOrAny();

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Initializes a new instance of the ")
            .AppendTypeAsSeeCref(typeDeclaration.DotnetTypeName())
            .AppendLine(" struct.")
            .AppendLineIndent("/// </summary>")
            .AppendIndent("public ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine("()")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBackingFieldAssignment("jsonElementBacking", "default")
                .AppendBackingFieldAssignment("backing", "Backing.JsonElement")
                .AppendBackingFieldAssignment("stringBacking", "string.Empty", impliedCoreTypes, CoreTypes.String)
                .AppendBackingFieldAssignment("boolBacking", "default", impliedCoreTypes, CoreTypes.Boolean)
                .AppendBackingFieldAssignment("numberBacking", "default", impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
                .AppendBackingFieldAssignment("arrayBacking", "ImmutableList<JsonAny>.Empty", impliedCoreTypes, CoreTypes.Array)
                .AppendBackingFieldAssignment("objectBacking", "ImmutableList<JsonObjectProperty>.Empty", impliedCoreTypes, CoreTypes.Object)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the default constructor for the type declaration.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicJsonElementConstructor(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        CoreTypes impliedCoreTypes = typeDeclaration.ImpliedCoreTypesOrAny();

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Initializes a new instance of the ")
            .AppendTypeAsSeeCref(typeDeclaration.DotnetTypeName())
            .AppendLine(" struct.")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to construct the instance.</param>")
            .AppendIndent("public ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine("(in JsonElement value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBackingFieldAssignment("jsonElementBacking", "value")
                .AppendBackingFieldAssignment("backing", "Backing.JsonElement")
                .AppendBackingFieldAssignment("stringBacking", "string.Empty", impliedCoreTypes, CoreTypes.String)
                .AppendBackingFieldAssignment("boolBacking", "default", impliedCoreTypes, CoreTypes.Boolean)
                .AppendBackingFieldAssignment("numberBacking", "default", impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
                .AppendBackingFieldAssignment("arrayBacking", "ImmutableList<JsonAny>.Empty", impliedCoreTypes, CoreTypes.Array)
                .AppendBackingFieldAssignment("objectBacking", "ImmutableList<JsonObjectProperty>.Empty", impliedCoreTypes, CoreTypes.Object)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion from <paramref name="sourceType"/> to the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="sourceType">The name of the source type from which to convert.</param>
    /// <param name="useInForSourceType">Determines whether the source type should have an in modifier (defaults to false).</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionFromTypeUsingConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType,
        bool useInForSourceType = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string targetType = typeDeclaration.DotnetTypeName();

        if (targetType == sourceType)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion from ")
            .AppendTypeAsSeeCref(sourceType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(useInForSourceType ? "in " : string.Empty)
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return new(value);")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion from <paramref name="sourceType"/> to the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="sourceType">The name of the source type from which to convert.</param>
    /// <param name="sourceValueKind">The expected <see cref="JsonValueKind"/> for the conversion.</param>
    /// <param name="dotnetTypeConversion">The code that converts the "value" to a dotnet value suitable
    /// for a constructor.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionFromJsonValueTypeUsingConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType,
        JsonValueKind sourceValueKind,
        string dotnetTypeConversion)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return AppendImplicitConversionFromJsonValueTypeUsingConstructor(
            generator,
            typeDeclaration,
            sourceType,
            [sourceValueKind],
            dotnetTypeConversion);
    }

    /// <summary>
    /// Appends an implicit conversion to bool for a boolean-backed type.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionToBoolean(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <summary>
                /// Conversion to <see langword="bool"/>.
                /// </summary>
                /// <param name="value">The value from which to convert.</param>
                /// <exception cref="InvalidOperationException">The value was not a boolean.</exception>
                """)
            .AppendIndent("public static implicit operator bool(")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" value)")
            .AppendBlockIndent(
                """
                {
                    return value.GetBoolean() ?? throw new InvalidOperationException();
                }
                """);
    }

    /// <summary>
    /// Appends an implicit conversion from <paramref name="sourceType"/> to the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="sourceType">The name of the source type from which to convert.</param>
    /// <param name="sourceValueKinds">The expected <see cref="JsonValueKind"/> or kinds for the conversion.</param>
    /// <param name="dotnetTypeConversion">The code that converts the "value" to a dotnet value suitable
    /// for a constructor.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionFromJsonValueTypeUsingConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType,
        JsonValueKind[] sourceValueKinds,
        string dotnetTypeConversion)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string targetType = typeDeclaration.DotnetTypeName();

        if (targetType == sourceType)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion from ")
            .Append(sourceType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendIndent("if (value.HasDotnetBacking && ")
                .AppendShortcircuitingOr(sourceValueKinds, static (g, v) => g.AppendJsonValueKindEquals("value", v), includeParensIfMultiple: true)
                .AppendLine(")")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("return new(")
                    .PushIndent()
                        .AppendBlockIndent(dotnetTypeConversion, omitLastLineEnd: true)
                    .PopIndent()
                    .AppendLine(");")
                .PopIndent()
                .AppendLineIndent("}")
                .AppendSeparatorLine()
                .AppendLineIndent("return new(value.AsJsonElement);")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion to <paramref name="targetType"/> from the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration from which to convert.</param>
    /// <param name="targetType">The name of the target type to which to convert.</param>
    /// <param name="forCoreTypes">The core types for which the conversion applies.</param>
    /// <param name="dotnetTypeConversion">The code that converts the value to the target type.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionToJsonValueType(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string targetType,
        CoreTypes forCoreTypes,
        string dotnetTypeConversion)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((typeDeclaration.ImpliedCoreTypesOrAny() & forCoreTypes) == 0)
        {
            return generator;
        }

        string sourceType = typeDeclaration.DotnetTypeName();

        if (targetType == sourceType)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion to ")
            .Append(targetType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return")
                .PushIndent()
                    .AppendBlockIndent(dotnetTypeConversion, omitLastLineEnd: true)
                .PopIndent()
                .AppendLine(";")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append <c>&lt;see cref="[typeName]"/&gt;</c>.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="typeName">The type name to which to append the reference.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendTypeAsSeeCref(
        this CodeGenerator generator,
        string typeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .Append("<see cref=\"");

        foreach (char c in typeName)
        {
            if (c == '<')
            {
                generator.Append('{');
            }
            else if (c == '>')
            {
                generator.Append('}');
            }
            else
            {
                generator.Append(c);
            }
        }

        return generator
            .Append("\"/>");
    }

    /// <summary>
    /// Appends an implicit conversion from the
    /// dotnet type of the <paramref name="typeDeclaration"/> to the <paramref name="targetType"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="targetType">The name of the target type towhich to convert.</param>
    /// <param name="dotnetTypeConversion">The code that converts the "value" to a dotnet value suitable
    /// for a constructor.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionToType(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string targetType,
        string dotnetTypeConversion)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion to ")
            .AppendTypeAsSeeCref(targetType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return")
                .PushIndent()
                    .AppendBlockIndent(dotnetTypeConversion, omitLastLineEnd: true)
                .PopIndent()
                .AppendLine(";")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion from <paramref name="sourceType"/> to the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="sourceType">The name of the source type from which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionFromJsonValueTypeUsingAs(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string targetType = typeDeclaration.DotnetTypeName();

        if (targetType == sourceType)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion from ")
            .Append(sourceType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendIndent("return value.As<")
                .Append(targetType)
                .AppendLine(">();")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion to <paramref name="targetType"/> from the
    /// dotnet type of the <paramref name="typeDeclaration"/>.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="targetType">The name of the target type to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionToJsonValueTypeUsingAs(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string targetType)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string sourceType = typeDeclaration.DotnetTypeName();
        if (sourceType == targetType)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Conversion to ")
            .Append(targetType)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator ")
            .Append(targetType)
            .Append('(')
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendIndent("return value.As<")
                .Append(targetType)
                .AppendLine(">();")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends an implicit conversion from dotnet type of the <paramref name="typeDeclaration"/>
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendImplicitConversionToJsonAny(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string sourceType = typeDeclaration.DotnetTypeName();
        if (sourceType == "JsonAny")
        {
            return generator
                .AppendNumericConversions(typeDeclaration, allImplicit: true, fromOnly: true)
                .AppendImplicitConversionFromTypeUsingConstructor(typeDeclaration, "bool")
                .AppendImplicitConversionFromTypeUsingConstructor(typeDeclaration, "string");
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendLineIndent("/// Conversion to JsonAny.")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
            .AppendIndent("public static implicit operator JsonAny(")
            .Append(sourceType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return value.AsAny;")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends <c>Is[Type]</c> and <c>As[Type]</c> methods for composition types.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="rootDeclaration">The type declaration which is the basis of the composition types.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsProperties(
        this CodeGenerator generator,
        TypeDeclaration rootDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        HashSet<string> visitedTypes = [];

        int index = 0;

        HashSet<string> usedPropertyNames = new();

        foreach (TypeDeclaration t in rootDeclaration.CompositionTypeDeclarations())
        {
            ++index;
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (!visitedTypes.Add(t.FullyQualifiedDotnetTypeName()))
            {
                continue;
            }

            string propertyNameAs = generator.GetPropertyNameInScope("As", suffix: t.DotnetTypeName());
            string currentPropertyName = propertyNameAs;

            int pnIndex = 0;
            while (usedPropertyNames.Contains(currentPropertyName))
            {
                if (pnIndex == 0)
                {
                    currentPropertyName = generator.GetPropertyNameInScope("As", suffix: t.DotnetTypeNameWithoutNamespace());

                    // Create the new base name
                    propertyNameAs = currentPropertyName;
                }
                else
                {
                    currentPropertyName = generator.GetPropertyNameInScope(propertyNameAs, suffix: pnIndex.ToString());
                }

                pnIndex++;
            }

            propertyNameAs = currentPropertyName;
            usedPropertyNames.Add(propertyNameAs);

            string propertyNameIs = generator.GetPropertyNameInScope("Is", suffix: t.DotnetTypeName());
            currentPropertyName = propertyNameIs;

            pnIndex = 0;
            while (usedPropertyNames.Contains(currentPropertyName))
            {
                if (pnIndex == 0)
                {
                    currentPropertyName = generator.GetPropertyNameInScope("Is", suffix: t.DotnetTypeNameWithoutNamespace());

                    // Create the new base name
                    propertyNameIs = currentPropertyName;
                }
                else
                {
                    currentPropertyName = generator.GetPropertyNameInScope(propertyNameIs, suffix: pnIndex.ToString());
                }

                pnIndex++;
            }

            propertyNameIs = currentPropertyName;
            usedPropertyNames.Add(propertyNameIs);

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the instance as a <see cref=\"", t.FullyQualifiedDotnetTypeName(), "\" />.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent("public ", t.FullyQualifiedDotnetTypeName(), " ", propertyNameAs)
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("get")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return this.As<", t.FullyQualifiedDotnetTypeName(), ">();")
                    .PopIndent()
                    .AppendLineIndent("}")
                .PopIndent()
                .AppendLineIndent("}")
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets a value indicating whether the instance is a <see cref=\"", t.FullyQualifiedDotnetTypeName(), "\" />.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent("public bool ", propertyNameIs)
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("get")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return this.As<", t.FullyQualifiedDotnetTypeName(), ">().IsValid();")
                    .PopIndent()
                    .AppendLineIndent("}")
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    /// <summary>
    /// Appends <c>TryGet()</c> methods composition types.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="rootDeclaration">The type declaration which is the basis of the composition types.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendTryGetMethods(
        this CodeGenerator generator,
        TypeDeclaration rootDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        HashSet<string> visitedTypes = [];

        foreach (TypeDeclaration t in rootDeclaration.CompositionTypeDeclarations())
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (!visitedTypes.Add(t.FullyQualifiedDotnetTypeName()))
            {
                continue;
            }

            string methodName = generator.GetMethodNameInScope("TryGetAs", suffix: t.DotnetTypeName());
            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Gets the value as a <see cref=\"", t.FullyQualifiedDotnetTypeName(), "\" />.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent("/// <param name=\"result\">The result of the conversions.</param>")
                .AppendLineIndent("/// <returns><see langword=\"true\" /> if the conversion was valid.</returns>")
                .AppendLineIndent("public bool ", methodName, "(out ", t.FullyQualifiedDotnetTypeName(), " result)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("result = this.As<", t.FullyQualifiedDotnetTypeName(), ">();")
                    .AppendLineIndent("return result.IsValid();")
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    /// <summary>
    /// Appends conversions from dotnet type of the <paramref name="rootDeclaration"/>
    /// to the composition types.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="rootDeclaration">The type declaration which is the basis of the conversions.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendConversionToCompositionTypes(
    this CodeGenerator generator,
    TypeDeclaration rootDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        HashSet<TypeDeclaration> appliedConversions = [];
        Queue<(TypeDeclaration Target, bool AllowsImplicitFrom, bool AllowsImplicitTo)> typesToProcess = [];

        typesToProcess.Enqueue((rootDeclaration, true, true));

        while (typesToProcess.Count > 0)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            (TypeDeclaration subschema, bool allowsImplicitFrom, bool allowsImplicitTo) = typesToProcess.Dequeue();
            AppendConversions(generator, appliedConversions, rootDeclaration, subschema, allowsImplicitFrom, allowsImplicitTo);
            AppendCompositionConversions(generator, appliedConversions, typesToProcess, rootDeclaration, subschema, allowsImplicitFrom: allowsImplicitFrom, allowsImplicitTo: allowsImplicitTo);
        }

        return generator;

        static void AppendCompositionConversions(
            CodeGenerator generator,
            HashSet<TypeDeclaration> appliedConversions,
            Queue<(TypeDeclaration Target, bool AllowsImplicitFrom, bool AllowsImplicitTo)> typesToProcess,
            TypeDeclaration rootType,
            TypeDeclaration sourceType,
            bool allowsImplicitFrom,
            bool allowsImplicitTo)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            if (sourceType.AllOfCompositionTypes() is IReadOnlyDictionary<IAllOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> allOf)
            {
                AppendSubschemaConversions(generator, appliedConversions, typesToProcess, rootType, allOf.SelectMany(k => k.Value).ToList(), isImplicitFrom: false, isImplicitTo: allowsImplicitTo);
            }

            if (sourceType.AnyOfCompositionTypes() is IReadOnlyDictionary<IAnyOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> anyOf)
            {
                // Defer any of until all the AllOf have been processed so we prefer an implicit to the allOf types
                foreach (TypeDeclaration subschema in anyOf.SelectMany(k => k.Value))
                {
                    if (generator.IsCancellationRequested)
                    {
                        return;
                    }

                    typesToProcess.Enqueue((subschema.ReducedTypeDeclaration().ReducedType, allowsImplicitFrom, false));
                }
            }

            if (sourceType.OneOfCompositionTypes() is IReadOnlyDictionary<IOneOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> oneOf)
            {
                // Defer any of until all the AllOf have been processed so we prefer an implicit to the allOf types
                foreach (TypeDeclaration subschema in oneOf.SelectMany(k => k.Value))
                {
                    if (generator.IsCancellationRequested)
                    {
                        return;
                    }

                    typesToProcess.Enqueue((subschema.ReducedTypeDeclaration().ReducedType, allowsImplicitFrom, false));
                }
            }
        }

        static void AppendSubschemaConversions(
            CodeGenerator generator,
            HashSet<TypeDeclaration> appliedConversions,
            Queue<(TypeDeclaration Target, bool AllowsImplicitFrom, bool AllowsImplicitTo)> typesToProcess,
            TypeDeclaration rootDeclaration,
            IReadOnlyCollection<TypeDeclaration> subschemas,
            bool isImplicitFrom,
            bool isImplicitTo)
        {
            foreach (TypeDeclaration candidate in subschemas)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                TypeDeclaration subschema = candidate.ReducedTypeDeclaration().ReducedType;
                if (!AppendConversions(generator, appliedConversions, rootDeclaration, subschema, isImplicitFrom, isImplicitTo))
                {
                    continue;
                }

                // Recurse, which will add more allOfs, and queue up the anyOfs and oneOfs.
                AppendCompositionConversions(generator, appliedConversions, typesToProcess, rootDeclaration, subschema, isImplicitFrom, isImplicitTo);
            }
        }

        static bool AppendConversions(
            CodeGenerator generator,
            HashSet<TypeDeclaration> appliedConversions,
            TypeDeclaration rootDeclaration,
            TypeDeclaration subschema,
            bool isImplicitFrom,
            bool isImplicitTo)
        {
            if (generator.IsCancellationRequested)
            {
                return false;
            }

            if (rootDeclaration == subschema)
            {
                return false;
            }

            if (!appliedConversions.Add(subschema) || subschema.DoNotGenerate())
            {
                // We've already seen it.
                return false;
            }

            string implictOrExplicitFrom = isImplicitFrom ? "implicit" : "explicit";
            string implictOrExplicitTo = isImplicitTo ? "implicit" : "explicit";

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Conversion to <see cref=\"", subschema.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
                .AppendIndent("public static ", implictOrExplicitTo, " operator ", subschema.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName(), "(")
                .Append(rootDeclaration.DotnetTypeName())
                .AppendLine(" value)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("return value.As<", subschema.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName(), ">();")
                .PopIndent()
                .AppendLineIndent("}");

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent("/// Conversion from <see cref=\"", subschema.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName(), "\"/>.")
                .AppendLineIndent("/// </summary>")
                .AppendLineIndent("/// <param name=\"value\">The value from which to convert.</param>")
                .AppendIndent("public static ", implictOrExplicitFrom, " operator ", rootDeclaration.DotnetTypeName(), "(")
                .Append(subschema.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName())
                .AppendLine(" value)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("return value.As<", rootDeclaration.DotnetTypeName(), ">();")
                .PopIndent()
                .AppendLineIndent("}");

            return true;
        }
    }

    /// <summary>
    /// Appends the CreateFromSerializedInstance static factory method.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendCreateFromSerializedInstanceFactoryMethod(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.IsCorvusJsonExtendedJsonAny())
        {
            return generator
                .ReserveName("CreateFromSerializedInstance")
                .AppendSeparatorLine()
                .AppendBlockIndent(
                    """
                /// <summary>
                /// Create a <see cref="JsonAny"/> instance from an arbitrary object.
                /// </summary>
                /// <typeparam name="T">The type of the object from which to create the instance.</typeparam>
                /// <param name="instance">The object from which to create the instance.</param>
                /// <param name="options">The (optional) <see cref="JsonWriterOptions"/>.</param>
                /// <returns>A <see cref="JsonAny"/> derived from serializing the object.</returns>
                public static JsonAny CreateFromSerializedInstance<T>(T instance, JsonWriterOptions options = default)
                {
                    var abw = new ArrayBufferWriter<byte>();
                    using var writer = new Utf8JsonWriter(abw, options);
                    JsonSerializer.Serialize(writer, instance, typeof(T));
                    writer.Flush();
                    return Parse(abw.WrittenMemory);
                }
                """);
        }

        return generator;
    }

    /// <summary>
    /// Appends the FromJson static factory method.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendFromJsonFactoryMethod(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("FromJson")
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <summary>
                /// Gets an instance of the JSON value from a <see cref="JsonElement"/> value.
                /// </summary>
                /// <param name="value">The <see cref="JsonElement"/> value from which to instantiate the instance.</param>
                /// <returns>An instance of this type, initialized from the <see cref="JsonElement"/>.</returns>
                /// <remarks>The returned value will have a <see cref = "IJsonValue.ValueKind"/> of <see cref = "JsonValueKind.Undefined"/> if the
                /// value cannot be constructed from the given instance (e.g. because they have an incompatible .NET backing type).
                /// </remarks>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                """)
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" FromJson(in JsonElement value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return new(value);")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends the FromAny static factory method.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendFromAnyFactoryMethod(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveName("FromAny")
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <summary>
                /// Gets an instance of the JSON value from a <see cref="JsonAny"/> value.
                /// </summary>
                /// <param name="value">The <see cref="JsonAny"/> value from which to instantiate the instance.</param>
                /// <returns>An instance of this type, initialized from the <see cref="JsonAny"/> value.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                """)
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append(" FromAny(in JsonAny value)");

        if (typeDeclaration.IsCorvusJsonExtendedJsonAny())
        {
            generator
                .AppendLine(" => value;");
        }
        else
        {
            generator
                .AppendLine()
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendConversionFromValue("value", typeDeclaration.ImpliedCoreTypesOrAny(), requiresAs: true)
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    /// <summary>
    /// Appends static factory method of the form FromXXX{TValue}.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to convert.</param>
    /// <param name="forCoreTypes">The core types for which to append conversions.</param>
    /// <param name="jsonValueTypeBaseName">The base name for the JSON value type (e.g. Boolean, String).</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendFromTValueFactoryMethod(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        CoreTypes forCoreTypes,
        string jsonValueTypeBaseName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((typeDeclaration.ImpliedCoreTypesOrAny() & forCoreTypes) != 0)
        {
            return generator
                .ReserveName($"From{jsonValueTypeBaseName}")
                .AppendSeparatorLine()
                .AppendBlockIndent(
                    """
                    /// <summary>
                    /// Gets an instance of the JSON value from the provided value.
                    /// </summary>
                    /// <typeparam name="TValue">The type of the value.</typeparam>
                    /// <param name="value">The value from which to instantiate the instance.</param>
                    /// <returns>An instance of this type, initialized from the provided value.</returns>
                    """)
                .AppendLineIndent("[MethodImpl(MethodImplOptions.AggressiveInlining)]")
                .AppendIndent("public static ")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(" From")
                .Append(jsonValueTypeBaseName)
                .AppendLine("<TValue>(in TValue value)")
                .PushIndent()
                .AppendIndent("where TValue : struct, IJson")
                .Append(jsonValueTypeBaseName)
                .AppendLine("<TValue>")
                .PopIndent()
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendConversionFromValue("value", forCoreTypes)
                .PopIndent()
                .AppendLineIndent("}");
        }
        else
        {
            return generator
                .ReserveName($"From{jsonValueTypeBaseName}")
                .AppendSeparatorLine()
                .AppendLine("#if NET8_0_OR_GREATER")
                .AppendBlockIndent(
                    """
                    /// <summary>
                    /// Gets an instance of the JSON value from the provided value.
                    /// </summary>
                    /// <typeparam name="TValue">The type of the value.</typeparam>
                    /// <param name="value">The value from which to instantiate the instance.</param>
                    /// <returns>An instance of this type, initialized from the provided value.</returns>
                    """)
                .AppendLineIndent("[MethodImpl(MethodImplOptions.AggressiveInlining)]")
                .AppendIndent("static ")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(" IJsonValue<")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(">.")
                .Append("From")
                .Append(jsonValueTypeBaseName)
                .AppendLine("<TValue>(in TValue value)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendIndent("if (")
                    .AppendLine("value.HasJsonElementBacking)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendIndent("return new(")
                        .AppendLine("value.AsJsonElement);")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("return Undefined;")
                .PopIndent()
                .AppendLineIndent("}")
                .AppendLine("#endif");
        }
    }

    /// <summary>
    /// Appends a static Parse() method to parse an instance of the <paramref name="sourceType"/> to an instance of the
    /// <paramref name="typeDeclaration"/> type.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the method.</param>
    /// <param name="sourceType">The type of the source from which to parse.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParseMethod(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveNameIfNotReserved("Parse")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Parses the ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"source\">The source of the JSON string to parse.</param>")
            .AppendLineIndent("/// <param name=\"options\">The (optional) JsonDocumentOptions.</param>")
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append(" Parse(")
            .Append(sourceType)
            .AppendLine(" source, JsonDocumentOptions options = default)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("using var jsonDocument = JsonDocument.Parse(source, options);")
                .AppendLineIndent("return new(jsonDocument.RootElement.Clone());")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the As{T} conversion method.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the method.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAsTMethod(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveNameIfNotReserved("As")
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <summary>
                /// Gets the value as an instance of the target value.
                /// </summary>
                /// <typeparam name="TTarget">The type of the target.</typeparam>
                /// <returns>An instance of the target type.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public TTarget As<TTarget>()
                    where TTarget : struct, IJsonValue<TTarget>
                {
                """)
            .PushIndent()
            .AppendLine("#if NET8_0_OR_GREATER")
            .AppendConditionalBackingValueLineIndent(
                "Backing.JsonElement",
                "return TTarget.FromJson(this.jsonElementBacking);")
            .AppendConditionalBackingValueLineIndent(
                "Backing.String",
                (typeDeclaration.LocallyImpliedCoreTypes() & CoreTypes.String) != 0 ? "return TTarget.FromString(this);" : "return TTarget.FromString(this.AsString);",
                typeDeclaration.ImpliedCoreTypesOrAny(),
                CoreTypes.String)
            .AppendConditionalBackingValueLineIndent(
                "Backing.Bool",
                (typeDeclaration.LocallyImpliedCoreTypes() & CoreTypes.Boolean) != 0 ? "return TTarget.FromBoolean(this);" : "return TTarget.FromBoolean(this.AsBoolean);",
                typeDeclaration.ImpliedCoreTypesOrAny(),
                CoreTypes.Boolean)
            .AppendConditionalBackingValueLineIndent(
                "Backing.Number",
                (typeDeclaration.LocallyImpliedCoreTypes() & (CoreTypes.Number | CoreTypes.Integer)) != 0 ? "return TTarget.FromNumber(this);" : "return TTarget.FromNumber(this.AsNumber);",
                typeDeclaration.ImpliedCoreTypesOrAny(),
                CoreTypes.Number | CoreTypes.Integer)
            .AppendConditionalBackingValueLineIndent(
                "Backing.Array",
                (typeDeclaration.LocallyImpliedCoreTypes() & CoreTypes.Array) != 0 ? "return TTarget.FromArray(this);" : "return TTarget.FromArray(this.AsArray);",
                typeDeclaration.ImpliedCoreTypesOrAny(),
                CoreTypes.Array)
            .AppendConditionalBackingValueLineIndent(
                "Backing.Object",
                (typeDeclaration.LocallyImpliedCoreTypes() & CoreTypes.Object) != 0 ? "return TTarget.FromObject(this);" : "return TTarget.FromObject(this.AsObject);",
                typeDeclaration.ImpliedCoreTypesOrAny(),
                CoreTypes.Object)
            .AppendConditionalBackingValueLineIndent("Backing.Null", "return TTarget.Null;")
            .AppendSeparatorLine()
            .AppendLineIndent("return TTarget.Undefined;")
            .AppendLine("#else")
            .AppendIndent("return this.As<")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(", TTarget>();")
            .AppendLine("#endif")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the WriteTo() method.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the method.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendWriteToMethod(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .ReserveName("WriteTo")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <inheritdoc/>")
            .AppendLineIndent("public void WriteTo(Utf8JsonWriter writer)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendConditionalBackingValueCallbackIndent(
                    "Backing.JsonElement",
                    "jsonElementBacking",
                    static (g, name) => g.AppendWriteJsonElementBacking(name),
                    returnFromClause: true)
                .AppendConditionalWrappedBackingValueLineIndent(
                    "Backing.Array",
                    "JsonValueHelpers.WriteItems(",
                    "arrayBacking",
                    ", writer);",
                    impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                    forCoreTypes: CoreTypes.Array,
                    returnFromClause: true)
                .AppendConditionalWrappedBackingValueLineIndent(
                    "Backing.Bool",
                    "writer.WriteBooleanValue(",
                    "boolBacking",
                    ");",
                    impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                    forCoreTypes: CoreTypes.Boolean,
                    returnFromClause: true)
                .AppendConditionalWrappedBackingValueLineIndent(
                    "Backing.Number",
                    string.Empty,
                    "numberBacking",
                    ".WriteTo(writer);",
                    impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                    forCoreTypes: CoreTypes.Number | CoreTypes.Integer,
                    returnFromClause: true)
                .AppendConditionalWrappedBackingValueLineIndent(
                    "Backing.Object",
                    "JsonValueHelpers.WriteProperties(",
                    "objectBacking",
                    ", writer);",
                    impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                    forCoreTypes: CoreTypes.Object,
                    returnFromClause: true)
                .AppendConditionalWrappedBackingValueLineIndent(
                    "Backing.String",
                    "writer.WriteStringValue(",
                    "stringBacking",
                    ");",
                    impliedCoreTypes: typeDeclaration.ImpliedCoreTypesOrAny(),
                    forCoreTypes: CoreTypes.String,
                    returnFromClause: true)
                .AppendConditionalBackingValueLineIndent(
                    "Backing.Null",
                    "writer.WriteNullValue();",
                    returnFromClause: true)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the Equals() method overloads.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the methods.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendEqualsOverloads(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveNameIfNotReserved("Equals")
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <inheritdoc/>
                public override bool Equals(object? obj)
                {
                    return
                """)
            .AppendLineIndent("        (obj is IJsonValue jv && this.Equals(jv.As<", typeDeclaration.DotnetTypeName(), ">())) ||")
            .AppendBlockIndent(
                """
                        (obj is null && this.IsNull());
                }

                /// <inheritdoc/>
                public bool Equals<T>(in T other)
                    where T : struct, IJsonValue<T>
                {
                """)
            .PushIndent()
            .AppendLineIndent("return this.Equals(other.As<", typeDeclaration.DotnetTypeName(), ">());")
            .PopIndent()
            .AppendLineIndent("}")
            .AppendSeparatorLine()
            .AppendBlockIndent(
                """
                /// <summary>
                /// Equality comparison.
                /// </summary>
                /// <param name="other">The other item with which to compare.</param>
                /// <returns><see langword="true"/> if the values were equal.</returns>
                """)
            .AppendIndent("public bool Equals(in ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" other)")
            .AppendLineIndent("{")
            .PushIndent();

        bool appendDefault = true;
        if (typeDeclaration.Format() is string format)
        {
            appendDefault = !FormatHandlerRegistry.Instance.FormatHandlers.AppendFormatEqualsTBody(generator, typeDeclaration, format);
        }

        if (appendDefault)
        {
            generator
                .AppendComparer(typeDeclaration);
        }

        return generator
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends standard comparer logic based on the core types.
    /// <paramref name="typeDeclaration"/> type.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the core types.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendComparer(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .AppendLineIndent("JsonValueKind thisKind = this.ValueKind;")
            .AppendLineIndent("JsonValueKind otherKind = other.ValueKind;")
            .AppendLineIndent("if (thisKind != otherKind)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLineIndent("return false;")
            .PopIndent()
            .AppendLineIndent("}");

        AppendNullOrUndefinedComparer(generator);

        CoreTypes coreTypes = typeDeclaration.ImpliedCoreTypesOrAny();
        if ((coreTypes & CoreTypes.Array) != 0)
        {
            AppendArrayComparer(generator, typeDeclaration);
        }

        if ((coreTypes & CoreTypes.Boolean) != 0)
        {
            AppendBooleanComparer(generator);
        }

        if ((coreTypes & (CoreTypes.Integer | CoreTypes.Number)) != 0)
        {
            AppendNumberComparer(generator);
        }

        if ((coreTypes & CoreTypes.Object) != 0)
        {
            AppendObjectComparer(generator, typeDeclaration);
        }

        if ((coreTypes & CoreTypes.String) != 0)
        {
            AppendStringComparer(generator);
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("return false;");

        static void AppendArrayComparer(CodeGenerator generator, TypeDeclaration typeDeclaration)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string? arrayItemsType = typeDeclaration.ArrayItemsType()?.ReducedType.FullyQualifiedDotnetTypeName();
            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.Array)")
                .AppendLineIndent("{")
                .PushIndent();

            string thisAccessor = "this";
            string otherAccessor = "other";

            if ((typeDeclaration.AllowedCoreTypes() & CoreTypes.Array) == 0)
            {
                thisAccessor = "thisArray";
                otherAccessor = "otherArray";
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("JsonArray thisArray = this.AsArray;")
                    .AppendLineIndent("JsonArray otherArray = other.AsArray;");
                arrayItemsType = null;
            }

            generator
                    .AppendIndent("JsonArrayEnumerator")
                    .AppendGenericParameterIfRequired(arrayItemsType)
                    .AppendLine(" lhs = ", thisAccessor, ".EnumerateArray();")
                    .AppendIndent("JsonArrayEnumerator")
                    .AppendGenericParameterIfRequired(arrayItemsType)
                    .AppendLine(" rhs = ", otherAccessor, ".EnumerateArray();")
                    .AppendBlockIndent(
                        """
                        while (lhs.MoveNext())
                        {
                            if (!rhs.MoveNext())
                            {
                                return false;
                            }

                            if (!lhs.Current.Equals(rhs.Current))
                            {
                                return false;
                            }
                        }

                        return !rhs.MoveNext();
                        """)
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendBooleanComparer(CodeGenerator generator)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.True || thisKind == JsonValueKind.False)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("return true;")
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendNumberComparer(CodeGenerator generator)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string backing = generator.GetFieldNameInScope("backing");
            string numberBacking = generator.GetFieldNameInScope("numberBacking");
            string jsonElementBacking = generator.GetFieldNameInScope("jsonElementBacking");

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.Number)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("if (this.", backing, " == Backing.Number && other.", backing, " == Backing.Number)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return BinaryJsonNumber.Equals(this.", numberBacking, ", other.", numberBacking, ");")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (this.", backing, " == Backing.Number && other.", backing, " == Backing.JsonElement)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return BinaryJsonNumber.Equals(this.", numberBacking, ", other.", jsonElementBacking, ");")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (this.", backing, " == Backing.JsonElement && other.", backing, " == Backing.Number)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return BinaryJsonNumber.Equals(this.", jsonElementBacking, ", other.", numberBacking, ");")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (this.", jsonElementBacking, ".TryGetDouble(out double lDouble))")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("if (other.", jsonElementBacking, ".TryGetDouble(out double rDouble))")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return lDouble.Equals(rDouble);")
                        .PopIndent()
                        .AppendLineIndent("}")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (this.", jsonElementBacking, ".TryGetDecimal(out decimal lDecimal))")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("if (other.", jsonElementBacking, ".TryGetDecimal(out decimal rDecimal))")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return lDecimal.Equals(rDecimal);")
                        .PopIndent()
                        .AppendLineIndent("}")
                    .PopIndent()
                    .AppendLineIndent("}")
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendNullOrUndefinedComparer(CodeGenerator generator)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.Null || thisKind == JsonValueKind.Undefined)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("return true;")
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendObjectComparer(CodeGenerator generator, TypeDeclaration typeDeclaration)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string? propertyTypeName = null;
            if (typeDeclaration.FallbackObjectPropertyType() is FallbackObjectPropertyType propertyType && !propertyType.ReducedType.IsBuiltInJsonAnyType())
            {
                propertyTypeName = propertyType.ReducedType.FullyQualifiedDotnetTypeName();
            }

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.Object)")
                .AppendLineIndent("{")
                .PushIndent();

            string thisAccessor = "this";
            string otherAccessor = "other";

            if ((typeDeclaration.AllowedCoreTypes() & CoreTypes.Object) == 0)
            {
                thisAccessor = "thisObject";
                otherAccessor = "otherObject";
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("JsonObject thisObject = this.AsObject;")
                    .AppendLineIndent("JsonObject otherObject = other.AsObject;");
                propertyTypeName = null;
            }

            generator
                    .AppendLineIndent("int count = 0;")
                    .AppendIndent("foreach (JsonObjectProperty")
                    .AppendGenericParameterIfRequired(propertyTypeName)
                    .AppendLine(" property in ", thisAccessor, ".EnumerateObject())")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("if (!", otherAccessor, ".TryGetProperty(property.Name, out ", propertyTypeName ?? "JsonAny", " value) || !property.Value.Equals(value))")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return false;")
                        .PopIndent()
                        .AppendLineIndent("}")
                        .AppendSeparatorLine()
                        .AppendLineIndent("count++;")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("int otherCount = 0;")
                    .AppendIndent("foreach (JsonObjectProperty")
                    .AppendGenericParameterIfRequired(propertyTypeName)
                    .AppendLine(" otherProperty in ", otherAccessor, ".EnumerateObject())")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("otherCount++;")
                        .AppendLineIndent("if (otherCount > count)")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return false;")
                        .PopIndent()
                        .AppendLineIndent("}")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("return count == otherCount;")
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendStringComparer(CodeGenerator generator)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string backing = generator.GetFieldNameInScope("backing");
            string stringBacking = generator.GetFieldNameInScope("stringBacking");
            string jsonElementBacking = generator.GetFieldNameInScope("jsonElementBacking");

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("if (thisKind == JsonValueKind.String)")
                .AppendLineIndent("{")
                .PushIndent()
                    .AppendLineIndent("if (this.", backing, " == Backing.JsonElement)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("if (other.", backing, " == Backing.String)")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("return this.", jsonElementBacking, ".ValueEquals(other.", stringBacking, ");")
                        .PopIndent()
                        .AppendLineIndent("}")
                        .AppendLineIndent("else")
                        .AppendLineIndent("{")
                        .PushIndent()
                            .AppendLineIndent("other.", jsonElementBacking, ".TryGetValue(CompareValues, this.", jsonElementBacking, ", out bool areEqual);")
                            .AppendLineIndent("return areEqual;")
                        .PopIndent()
                        .AppendLineIndent("}")
                        .AppendSeparatorLine()
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (other.", backing, " == Backing.JsonElement)")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return other.", jsonElementBacking, ".ValueEquals(this.", stringBacking, ");")
                    .PopIndent()
                    .AppendLineIndent("}")
                    .AppendSeparatorLine()
                    .AppendLineIndent("return this.", stringBacking, ".Equals(other.", stringBacking, ");")
                    .AppendSeparatorLine()
                    .AppendBlockIndent(
                        """
                        static bool CompareValues(ReadOnlySpan<byte> span, in JsonElement firstItem, out bool value)
                        {
                            value = firstItem.ValueEquals(span);
                            return true;
                        }
                        """)
                .PopIndent()
                .AppendLineIndent("}");
        }
    }

    /// <summary>
    /// Appends a static ParseValue() method to parse an instance of the <paramref name="sourceType"/> to an instance of the
    /// <paramref name="typeDeclaration"/> type.
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to produce the method.</param>
    /// <param name="sourceType">The type of the source from which to parse.</param>
    /// <param name="byRef">Whether the parameter is a by-ref value.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParseValueMethod(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string sourceType,
        bool byRef = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .ReserveNameIfNotReserved("ParseValue")
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Parses the ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"source\">The source of the JSON string to parse.</param>")
            .AppendIndent("public static ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append(" ParseValue(");

        if (byRef)
        {
            generator.Append("ref ");
        }

        generator
            .Append(sourceType)
            .AppendLine(" source)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendLine("#if NET8_0_OR_GREATER")
                .AppendIndent("return IJsonValue<")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(">.ParseValue(");

        if (byRef)
        {
            generator.Append("ref ");
        }

        generator
                .AppendLine("source);")
                .AppendLine("#else")
                .AppendIndent("return JsonValueHelpers.ParseValue<")
                .Append(typeDeclaration.DotnetTypeName())
                .Append(">(");

        if (byRef)
        {
            generator.Append("ref ");
        }

        return generator
            .AppendLine("source", sourceType == "string" ? ".AsSpan()" : string.Empty, ");")
            .AppendLine("#endif")
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Appends a binary operator for the <paramref name="typeDeclaration"/>
    /// to JsonAny.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to add the operator.</param>
    /// <param name="returnType">The return type of the operator.</param>
    /// <param name="operatorSymbol">The symbol to inject for the operator.</param>
    /// <param name="operatorBody">The body to inject for the operator.</param>
    /// <param name="returnValueDocumentation">The return value documentation.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendBinaryOperator(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string returnType,
        string operatorSymbol,
        string operatorBody,
        string returnValueDocumentation)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Operator ")
            .Append(operatorSymbol)
            .AppendLine(".")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"left\">The lhs of the operator.</param>")
            .AppendLineIndent("/// <param name=\"right\">The rhs of the operator.</param>")
            .AppendLineIndent("/// <returns>")
            .AppendBlockIndentWithPrefix(returnValueDocumentation, "/// ")
            .AppendLineIndent("/// </returns>")
            .AppendIndent("public static ")
            .Append(returnType)
            .Append(" operator ")
            .Append(operatorSymbol)
            .Append("(in ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append(" left, in ")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(" right)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBlockIndent(operatorBody)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append an ordinal name for a number.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="number">The number for which to generate the ordinal.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendOrdinalName(this CodeGenerator generator, int number)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
                .Append(number);

        if (number >= 11 && number <= 13)
        {
            return generator
                .Append("th");
        }

        return (number % 10) switch
        {
            1 => generator.Append("st"),
            2 => generator.Append("nd"),
            3 => generator.Append("rd"),
            _ => generator.Append("th"),
        };
    }

    /// <summary>
    /// Append a short-circuiting set of OR (||) operations.
    /// </summary>
    /// <typeparam name="T">The type of the entity to be passed to the <paramref name="appendCallback"/>.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="values">The values to append.</param>
    /// <param name="appendCallback">The callback which appends the value.</param>
    /// <param name="includeParensIfMultiple">Indicates whether to wrap the clause in round brackets if there
    /// are multiple values.
    /// </param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendShortcircuitingOr<T>(this CodeGenerator generator, T[] values, Action<CodeGenerator, T> appendCallback, bool includeParensIfMultiple)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        bool includeParens = values.Length > 1 && includeParensIfMultiple;

        if (includeParens)
        {
            generator.Append('(');
        }

        for (int i = 0; i < values.Length; ++i)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (i > 0)
            {
                generator.Append(" || ");
            }

            appendCallback(generator, values[i]);
        }

        if (includeParens)
        {
            generator.Append(')');
        }

        return generator;
    }

    /// <summary>
    /// Append an equality comparison for a JsonValueKind.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="lhs">The left hand side of the comparison.</param>
    /// <param name="jsonValueKind">The value kind to compare.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendJsonValueKindEquals(
        this CodeGenerator generator,
        string lhs,
        JsonValueKind jsonValueKind)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .Append(lhs)
            .Append(".ValueKind == ")
            .AppendJsonValueKind(jsonValueKind);
    }

    /// <summary>
    /// Append a JSON value kind.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="valueKind">The value kind to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendJsonValueKind(this CodeGenerator generator, JsonValueKind valueKind)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .Append("JsonValueKind.")
            .Append(valueKind.ToString());
    }

    /// <summary>
    /// Append the constructor for the type declaration from two parameters.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <param name="valueType1">The type of the first value.</param>
    /// <param name="valueType2">The type of the second value.</param>
    /// <param name="valueCoreType">The core type of the value type.</param>
    /// <param name="valueConverter">The conversion expression. It may make use of the <c>value1</c> and <c>value2</c> parameters.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicConvertedValueConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string valueType1,
        string valueType2,
        CoreTypes valueCoreType,
        string valueConverter)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        CoreTypes impliedCoreTypes = typeDeclaration.ImpliedCoreTypesOrAny();

        if ((impliedCoreTypes & valueCoreType) == 0)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Initializes a new instance of the ")
            .AppendTypeAsSeeCref(typeDeclaration.DotnetTypeName())
            .AppendLine(" struct.")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value1\">The <see cref=\"", valueType1, "\"/> from which to construct the instance.</param>")
            .AppendLineIndent("/// <param name=\"value2\">The <see cref=\"", valueType2, "\"/> from which to construct the instance.</param>")
            .AppendIndent("public ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append("(")
            .Append(valueType1)
            .Append(" value1, ")
            .Append(valueType2)
            .AppendLine(" value2)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBackingFieldAssignment("backing", GetBacking(valueCoreType))
                .AppendBackingFieldAssignment("jsonElementBacking", "default")
                .AppendBackingFieldAssignment("stringBacking", GetValue(valueCoreType, CoreTypes.String, "string.Empty", valueConverter), impliedCoreTypes, CoreTypes.String)
                .AppendBackingFieldAssignment("boolBacking", GetValue(valueCoreType, CoreTypes.Boolean, "default", valueConverter), impliedCoreTypes, CoreTypes.Boolean)
                .AppendBackingFieldAssignment("numberBacking", GetValue(valueCoreType, CoreTypes.Number | CoreTypes.Integer, "default", valueConverter), impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
                .AppendBackingFieldAssignment("arrayBacking", GetValue(valueCoreType, CoreTypes.Array, "ImmutableList<JsonAny>.Empty", valueConverter), impliedCoreTypes, CoreTypes.Array)
                .AppendBackingFieldAssignment("objectBacking", GetValue(valueCoreType, CoreTypes.Object, "ImmutableList<JsonObjectProperty>.Empty", valueConverter), impliedCoreTypes, CoreTypes.Object)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the constructor for the type declaration from a given value, where the value
    /// must be converted.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <param name="valueType">The type of the value.</param>
    /// <param name="valueCoreType">The core type of the value type.</param>
    /// <param name="valueConverter">The conversion expression. It may make use of the <c>value</c> parameter.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicConvertedValueConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string valueType,
        CoreTypes valueCoreType,
        string valueConverter)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        CoreTypes impliedCoreTypes = typeDeclaration.ImpliedCoreTypesOrAny();

        if ((impliedCoreTypes & valueCoreType) == 0)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Initializes a new instance of the ")
            .AppendTypeAsSeeCref(typeDeclaration.DotnetTypeName())
            .AppendLine(" struct.")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to construct the instance.</param>")
            .AppendIndent("public ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append("(")
            .Append(valueType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBackingFieldAssignment("backing", GetBacking(valueCoreType))
                .AppendBackingFieldAssignment("jsonElementBacking", "default")
                .AppendBackingFieldAssignment("stringBacking", GetValue(valueCoreType, CoreTypes.String, "string.Empty", valueConverter), impliedCoreTypes, CoreTypes.String)
                .AppendBackingFieldAssignment("boolBacking", GetValue(valueCoreType, CoreTypes.Boolean, "default", valueConverter), impliedCoreTypes, CoreTypes.Boolean)
                .AppendBackingFieldAssignment("numberBacking", GetValue(valueCoreType, CoreTypes.Number | CoreTypes.Integer, "default", valueConverter), impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
                .AppendBackingFieldAssignment("arrayBacking", GetValue(valueCoreType, CoreTypes.Array, "ImmutableList<JsonAny>.Empty", valueConverter), impliedCoreTypes, CoreTypes.Array)
                .AppendBackingFieldAssignment("objectBacking", GetValue(valueCoreType, CoreTypes.Object, "ImmutableList<JsonObjectProperty>.Empty", valueConverter), impliedCoreTypes, CoreTypes.Object)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the constructor for the type declaration from a given value, where the value
    /// must be converted.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <param name="valueType">The type of the value.</param>
    /// <param name="valueCoreType">The core type of the value type.</param>
    /// <param name="appendAssignment">The code that appends the assignment.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicConvertedValueWithBodyConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string valueType,
        CoreTypes valueCoreType,
        AppendConstructorBackingFieldAssignmentCallback appendAssignment)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        CoreTypes impliedCoreTypes = typeDeclaration.ImpliedCoreTypesOrAny();

        if ((impliedCoreTypes & valueCoreType) == 0)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendLineIndent("/// <summary>")
            .AppendIndent("/// Initializes a new instance of the ")
            .AppendTypeAsSeeCref(typeDeclaration.DotnetTypeName())
            .AppendLine(" struct.")
            .AppendLineIndent("/// </summary>")
            .AppendLineIndent("/// <param name=\"value\">The value from which to construct the instance.</param>")
            .AppendIndent("public ")
            .Append(typeDeclaration.DotnetTypeName())
            .Append("(")
            .Append(valueType)
            .AppendLine(" value)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendBackingFieldAssignment("backing", GetBacking(valueCoreType))
                .AppendBackingFieldAssignment("jsonElementBacking", "default")
                .AppendBackingFieldAssignment(typeDeclaration, "stringBacking", GetValue(valueCoreType, CoreTypes.String, "string.Empty", appendAssignment), impliedCoreTypes, CoreTypes.String)
                .AppendBackingFieldAssignment(typeDeclaration, "boolBacking", GetValue(valueCoreType, CoreTypes.Boolean, "default", appendAssignment), impliedCoreTypes, CoreTypes.Boolean)
                .AppendBackingFieldAssignment(typeDeclaration, "numberBacking", GetValue(valueCoreType, CoreTypes.Number | CoreTypes.Integer, "default", appendAssignment), impliedCoreTypes, CoreTypes.Number | CoreTypes.Integer)
                .AppendBackingFieldAssignment(typeDeclaration, "arrayBacking", GetValue(valueCoreType, CoreTypes.Array, "ImmutableList<JsonAny>.Empty", appendAssignment), impliedCoreTypes, CoreTypes.Array)
                .AppendBackingFieldAssignment(typeDeclaration, "objectBacking", GetValue(valueCoreType, CoreTypes.Object, "ImmutableList<JsonObjectProperty>.Empty", appendAssignment), impliedCoreTypes, CoreTypes.Object)
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append the constructor for the type declaration from a given value, where the value
    /// can be directly assigned to a backing field.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration for which to emit the constructor.</param>
    /// <param name="valueType">The type of the value.</param>
    /// <param name="valueCoreType">The core type of the value type.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicValueConstructor(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration,
        string valueType,
        CoreTypes valueCoreType)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendPublicConvertedValueConstructor(typeDeclaration, valueType, valueCoreType, "value");
    }

    /// <summary>
    /// End a method declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the method.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator EndMethodDeclaration(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .PopMemberScope()
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append a parameter list. This will produce the parameters on a single line
    /// for 0, 1, or 2 parameters, and an indented multi-line list for 3 or more parameters.
    /// </summary>
    /// <param name="generator">The generator to which to append the parameter list.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParameterList(
        this CodeGenerator generator,
        params MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (parameters.Length < 3)
        {
            return AppendParameterListSingleLine(generator, parameters);
        }

        return AppendParameterListIndent(generator, parameters);
    }

    /// <summary>
    /// Append a parameter list on a single line.
    /// </summary>
    /// <param name="generator">The generator to which to append the parameter list.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParameterListSingleLine(CodeGenerator generator, MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (parameters.Length == 0)
        {
            // If we have no parameters, just emit the brackets.
            return generator.AppendLine("()");
        }

        generator.Append("(");
        bool first = true;

        foreach (MethodParameter parameter in parameters)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (first)
            {
                first = false;
            }
            else
            {
                generator.Append(", ");
            }

            generator.AppendParameter(parameter);
        }

        return generator
            .AppendLine(")");
    }

    /// <summary>
    /// Append a parameter list on multiple lines, indented.
    /// </summary>
    /// <param name="generator">The generator to which to append the parameter list.</param>
    /// <param name="parameters">The parameter list.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParameterListIndent(CodeGenerator generator, MethodParameter[] parameters)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (parameters.Length == 0)
        {
            // If we have no parameters, just emit the brackets.
            return generator.AppendLine("()");
        }

        generator.AppendLine("(");
        generator.PushIndent();
        bool first = true;

        foreach (MethodParameter parameter in parameters)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (first)
            {
                first = false;
            }
            else
            {
                generator.AppendLine(",");
            }

            generator.AppendParameterIndent(parameter);
        }

        return generator
            .PopIndent()
            .AppendLine(")");
    }

    /// <summary>
    /// Append a parameter in a parameter list.
    /// </summary>
    /// <param name="generator">The generator to which to append the parameter.</param>
    /// <param name="parameter">The parameter to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParameterIndent(
        this CodeGenerator generator,
        MethodParameter parameter)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string name = parameter.GetName(generator, isDeclaration: true);

        if (!string.IsNullOrEmpty(parameter.Modifiers))
        {
            generator
                .AppendIndent(parameter.Modifiers)
                .Append(' ')
                .Append(parameter.Type);
        }
        else
        {
            generator
                .AppendIndent(parameter.Type);
        }

        if (parameter.TypeIsNullable)
        {
            generator
                .Append('?');
        }

        generator
            .Append(' ')
            .Append(name);

        if (!string.IsNullOrEmpty(parameter.DefaultValue))
        {
            generator
                .Append(" = ")
                .Append(parameter.DefaultValue);
        }

        return generator;
    }

    /// <summary>
    /// Append a parameter in a parameter list.
    /// </summary>
    /// <param name="generator">The generator to which to append the parameter.</param>
    /// <param name="parameter">The parameter to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParameter(
        this CodeGenerator generator,
        MethodParameter parameter)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string name = parameter.GetName(generator, isDeclaration: true);

        if (!string.IsNullOrEmpty(parameter.Modifiers))
        {
            generator
                .Append(parameter.Modifiers)
                .Append(' ');
        }

        generator
            .Append(parameter.Type);

        if (parameter.TypeIsNullable)
        {
            generator
                .Append('?');
        }

        generator
            .Append(' ')
            .Append(name);

        if (!string.IsNullOrEmpty(parameter.DefaultValue))
        {
            generator
                .Append(" = ")
                .Append(parameter.DefaultValue);
        }

        return generator;
    }

    /// <summary>
    /// Emits the parent/child nesting.
    /// </summary>
    /// <param name="generator">The generator to which to append the parent/child declaration nesting.</param>
    /// <param name="typeDeclaration">The type declaration being emitted.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginTypeDeclarationNesting(
        this CodeGenerator generator,
        TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Stack<TypeDeclaration> parentTypes = new();

        TypeDeclaration? current = typeDeclaration.Parent();

        // We need to reverse the order, so we push them onto a stack...
        while (current is not null)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            parentTypes.Push(current);
            current = current.Parent();
        }

        bool isFirst = true;

        // Just begin our namespace if we have no parents.
        if (parentTypes.Count == 0)
        {
            return generator
                .BeginNamespace(typeDeclaration.DotnetNamespace());
        }

        // ...and then pop them off again.
        while (parentTypes.Count > 0)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            TypeDeclaration parent = parentTypes.Pop();

            if (isFirst)
            {
                generator
                    .BeginNamespace(parent.DotnetNamespace());
                isFirst = false;
            }

            generator
                .AppendSeparatorLine()
                .AppendDocumentation(parent)
                .BeginReadonlyPartialStructDeclaration(
                    parent.DotnetAccessibility(),
                    parent.DotnetTypeName());
        }

        return generator;
    }

    /// <summary>
    /// Closes off the parent/child nesting.
    /// </summary>
    /// <param name="generator">The generator to which to append the nested-type closing.</param>
    /// <param name="typeDeclaration">The type declaration being emitted.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator EndTypeDeclarationNesting(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        TypeDeclaration? current = typeDeclaration.Parent();
        while (current is not null)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            generator.EndClassOrStructDeclaration();
            current = current.Parent();
        }

        return generator;
    }

    /// <summary>
    /// Emits the end of a class or struct declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the end of the struct declaration.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator EndClassOrStructDeclaration(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .PopMemberScope()
            .PopIndent()
            .AppendLineIndent("}");
    }

    /// <summary>
    /// Append a numeric string.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendNumericLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.Number, "The value must be a number.");

        generator.Append(value.GetRawText());

        if (!value.TryGetDouble(out double _))
        {
            // Fall back to a decimal if we can't process the value with a double.
            generator.Append("M");
        }

        return generator;
    }

    /// <summary>
    /// Append an integer string.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendIntegerLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.Number, "The value must be a number.");

        if (value.TryGetInt64(out long result))
        {
            generator.Append(result);
        }
        else if (value.TryGetDouble(out double resultD))
        {
            double roundedResult = Math.Round(resultD);
            if (roundedResult == resultD)
            {
                generator.Append((long)roundedResult);
            }
        }

        return generator;
    }

    /// <summary>
    /// Append a quoted string value.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendQuotedStringLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.String, "The value must be a string.");

        generator.Append(SymbolDisplay.FormatLiteral(value.GetRawText(), true));

        return generator;
    }

    /// <summary>
    /// Append a quoted string value.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendSerializedBooleanLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False, "The value must be a boolean.");

        generator.Append(SymbolDisplay.FormatLiteral(value.GetRawText(), true));

        return generator;
    }

    /// <summary>
    /// Append a quoted string value.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendQuotedStringLiteral(this CodeGenerator generator, string value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .Append(SymbolDisplay.FormatLiteral(value, true));
    }

    /// <summary>
    /// Append an object serialized as a string literal.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendSerializedObjectStringLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.Object, "The value must be an object.");

        return generator
            .Append(SymbolDisplay.FormatLiteral(value.GetRawText(), true));
    }

    /// <summary>
    /// Append an array serialized as a string literal.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="value">The numeric value to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendSerializedArrayStringLiteral(this CodeGenerator generator, in JsonElement value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        Debug.Assert(value.ValueKind == JsonValueKind.Array, "The value must be an array.");

        return generator
            .Append(SymbolDisplay.FormatLiteral(value.GetRawText(), true));
    }

    /// <summary>
    /// Format a type name of the form <c>{genericTypeName}&lt;{typeDeclaration.DotnetTypeName()}&gt;</c>.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="genericTypeName">The name of the genertic type.</param>
    /// <param name="typeDeclaration">The type declaration for which to form the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator GenericTypeOf(
        this CodeGenerator generator,
        string genericTypeName,
        TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
                .Append(genericTypeName)
                .Append('<')
                .Append(typeDeclaration.FullyQualifiedDotnetTypeName())
                .Append('>');
    }

    /// <summary>
    /// Format a type name of the form
    /// <c>{genericTypeName}&lt;{typeDeclaration1.FullyQualifiedDotnetTypeName()}, {typeDeclaration2.FullyQualifiedDotnetTypeName()}}&gt;</c>.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="genericTypeName">The name of the genertic type.</param>
    /// <param name="typeDeclaration1">The first type declaration from which to form the name.</param>
    /// <param name="typeDeclaration2">The second type declaration from which to form the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator GenericTypeOf(
        this CodeGenerator generator,
        string genericTypeName,
        TypeDeclaration typeDeclaration1,
        TypeDeclaration typeDeclaration2)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
                .Append(genericTypeName)
                .Append('<')
                .Append(typeDeclaration1.FullyQualifiedDotnetTypeName())
                .Append(", ")
                .Append(typeDeclaration2.FullyQualifiedDotnetTypeName())
                .Append('>');
    }

    /// <summary>
    /// Format a type name of the form
    /// <c>{genericTypeName}&lt;{typeDeclaration1.FullyQualifiedDotnetTypeName()}, {typeDeclaration2.FullyQualifiedDotnetTypeName()}, {typeDeclaration3.FullyQualifiedDotnetTypeName()}&gt;</c>.
    /// </summary>
    /// <param name="generator">The generator to which to append the numeric string.</param>
    /// <param name="genericTypeName">The name of the genertic type.</param>
    /// <param name="typeDeclaration1">The 1st type declaration from which to form the name.</param>
    /// <param name="typeDeclaration2">The 2nd type declaration from which to form the name.</param>
    /// <param name="typeDeclaration3">The 3rd type declaration from which to form the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator GenericTypeOf(
        this CodeGenerator generator,
        string genericTypeName,
        TypeDeclaration typeDeclaration1,
        TypeDeclaration typeDeclaration2,
        TypeDeclaration typeDeclaration3)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
                .Append(genericTypeName)
                .Append('<')
                .Append(typeDeclaration1.FullyQualifiedDotnetTypeName())
                .Append(", ")
                .Append(typeDeclaration2.FullyQualifiedDotnetTypeName())
                .Append(", ")
                .Append(typeDeclaration3.FullyQualifiedDotnetTypeName())
                .Append('>');
    }

    /// <summary>
    /// Emits the start of a partial struct declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="accessibility">The accessibility for the generated type.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <param name="interfaces">Interfaces to implement.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginReadonlyPartialStructDeclaration(
        this CodeGenerator generator,
        GeneratedTypeAccessibility accessibility,
        string dotnetTypeName,
        ConditionalCodeSpecification[]? interfaces = null)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string accessibilityString = accessibility switch
        {
            GeneratedTypeAccessibility.Public => "public",
            GeneratedTypeAccessibility.Internal => "internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility)),
        };

        generator.ReserveNameIfNotReserved(dotnetTypeName);
        generator
            .AppendIndent(accessibilityString, " readonly partial struct ")
            .AppendLine(dotnetTypeName);

        if (interfaces is ConditionalCodeSpecification[] conditionalSpecifications)
        {
            generator.PushIndent();
            ConditionalCodeSpecification.AppendConditionalsGroupingBlocks(generator, conditionalSpecifications, AppendInterface);
            generator.PopIndent();
        }

        return generator
            .AppendLineIndent("{")
            .PushMemberScope(dotnetTypeName, ScopeType.Type)
            .ReserveNameIfNotReserved(dotnetTypeName) // Reserve the name of the containing scope in its own scope
            .PushIndent();

        static void AppendInterface(CodeGenerator generator, Action<CodeGenerator> appendFunction, int elementIndexInConditionalBlock)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            if (elementIndexInConditionalBlock == 0)
            {
                generator.AppendIndent(": ");
                appendFunction(generator);
            }
            else
            {
                generator
                    .AppendLine(",")
                    .AppendIndent("  "); // Align with the ": "
                appendFunction(generator);
            }
        }
    }

    /// <summary>
    /// Emits the start of a private static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginPrivateStaticClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string name = generator.GetTypeNameInScope(dotnetTypeName);
        return generator
            .AppendIndent("private static class ")
            .AppendLine(name)
            .AppendLineIndent("{")
            .PushMemberScope(name, ScopeType.Type)
            .ReserveNameIfNotReserved(name) // Reserve the name of the containing scope in its own scope
            .PushIndent();
    }

    /// <summary>
    /// Emits the start of a private static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginPrivateStaticPartialClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .BeginReservedPrivateStaticPartialClassDeclaration(dotnetTypeName);
    }

    /// <summary>
    /// Emits the start of a private static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginReservedPrivateStaticPartialClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendIndent("private static partial class ")
            .AppendLine(dotnetTypeName)
            .AppendLineIndent("{")
            .PushMemberScope(dotnetTypeName, ScopeType.Type)
            .ReserveNameIfNotReserved(dotnetTypeName) // Reserve the name of the containing scope in its own scope
            .PushIndent();
    }

    /// <summary>
    /// Emits the start of a public static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginPublicStaticPartialClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .BeginReservedPublicStaticPartialClassDeclaration(dotnetTypeName);
    }

    /// <summary>
    /// Emits the start of a public static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginReservedPublicStaticPartialClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendIndent("public static partial class ")
            .AppendLine(dotnetTypeName)
            .AppendLineIndent("{")
            .PushMemberScope(dotnetTypeName, ScopeType.Type)
            .ReserveNameIfNotReserved(dotnetTypeName) // Reserve the name of the containing scope in its own scope
            .PushIndent();
    }

    /// <summary>
    /// Emits the start of a public static class declaration.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <param name="dotnetTypeName">The .NET type name for the partial struct.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator BeginPublicStaticClassDeclaration(this CodeGenerator generator, string dotnetTypeName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string name = generator.GetTypeNameInScope(dotnetTypeName);
        return generator
            .AppendIndent("public static class ")
            .AppendLine(name)
            .AppendLineIndent("{")
            .PushMemberScope(name, ScopeType.Type)
            .ReserveNameIfNotReserved(name) // Reserve the name of the containing scope in its own scope
            .PushIndent();
    }

    /// <summary>
    /// Emits the auto-generated header.
    /// </summary>
    /// <param name="generator">The generator to which to append the beginning of the struct declaration.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendAutoGeneratedHeader(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendBlockIndent(
            """
            //------------------------------------------------------------------------------
            // <auto-generated>
            //     This code was generated by a tool.
            //
            //     Changes to this file may cause incorrect behavior and will be lost if
            //     the code is regenerated.
            // </auto-generated>
            //------------------------------------------------------------------------------
            """);
    }

    /// <summary>
    /// Append the JsonConverter attribute.
    /// </summary>
    /// <param name="generator">The generator to which to append the JsonConverter attribute.</param>
    /// <param name="typeDeclaration">The type declaration.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendJsonConverterAttribute(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendIndent("[System.Text.Json.Serialization.JsonConverter(typeof(Corvus.Json.Internal.JsonValueConverter<")
            .Append(typeDeclaration.DotnetTypeName())
            .AppendLine(">))]");
    }

    /// <summary>
    /// Append the text as paragraphs, splitting on newline and/or carriage return.
    /// </summary>
    /// <param name="generator">The generator to which to append the paragraphs.</param>
    /// <param name="paragraphs">The text containing the paragraphs to append.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendParagraphs(this CodeGenerator generator, string paragraphs)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string[] lines = NormalizeAndSplitBlockIntoLines(paragraphs, removeBlankLines: true);
        foreach (string line in lines)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            generator
                .AppendLineIndent("/// <para>")
                .AppendIndent("/// ")
                .AppendLine(SymbolDisplay.FormatLiteral(HttpUtility.HtmlEncode(line), false))
                .AppendLineIndent("/// </para>");
        }

        return generator;
    }

    /// <summary>
    /// Append a multi-line block of text at the given indent.
    /// </summary>
    /// <param name="generator">The generator to which to append the block.</param>
    /// <param name="block">The block to append.</param>
    /// <param name="trimWhitespaceOnlyLines">Whether to trim lines that are whitespace only.</param>
    /// <param name="omitLastLineEnd">If <see langword="true"/> then the last line is appended without an additional line-end, leaving
    /// the generator at the end of the block.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendBlockIndent(this CodeGenerator generator, string block, bool trimWhitespaceOnlyLines = true, bool omitLastLineEnd = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string[] lines = NormalizeAndSplitBlockIntoLines(block);

        for (int i = 0; i < lines.Length; i++)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            string line = lines[i];
            if (omitLastLineEnd && i == lines.Length - 1)
            {
                generator
                    .AppendIndent(line);
            }
            else
            {
                generator
                    .AppendLineIndent(line, trimWhitespaceOnlyLines);
            }
        }

        return generator;
    }

    /// <summary>
    /// Append a multi-line block of text at the given indent.
    /// </summary>
    /// <param name="generator">The generator to which to append the block.</param>
    /// <param name="block">The block to append.</param>
    /// <param name="trimWhitespaceOnlyLines">Whether to trim lines that are whitespace only.</param>
    /// <param name="omitLastLineEnd">If <see langword="true"/> then the last line is appended without an additional line-end, leaving
    /// the generator at the end of the block.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendBlockIndentWithHashOutdent(this CodeGenerator generator, string block, bool trimWhitespaceOnlyLines = true, bool omitLastLineEnd = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string[] lines = NormalizeAndSplitBlockIntoLines(block);

        for (int i = 0; i < lines.Length; i++)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            string line = lines[i];
            if (omitLastLineEnd && i == lines.Length - 1)
            {
                if (line[0] == '#')
                {
                    generator.Append(line);
                }
                else
                {
                    generator
                        .AppendIndent(line);
                }
            }
            else
            {
                if (line.Length > 0 && line[0] == '#')
                {
                    generator.AppendLine(line);
                }
                else
                {
                    generator
                        .AppendLineIndent(line, trimWhitespaceOnlyLines);
                }
            }
        }

        return generator;
    }

    /// <summary>
    /// Append a multi-line block of text at the given indent, with a given line prefix.
    /// </summary>
    /// <param name="generator">The generator to which to append the block.</param>
    /// <param name="block">The block to append.</param>
    /// <param name="linePrefix">The prefix for each line.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendBlockIndentWithPrefix(this CodeGenerator generator, string block, string linePrefix)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string[] lines = NormalizeAndSplitBlockIntoLines(block);
        foreach (string line in lines)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            generator
                .AppendIndent(linePrefix)
                .AppendLine(line);
        }

        return generator;
    }

    /// <summary>
    /// Appends a public static readonly field.
    /// </summary>
    /// <param name="generator">The generator to which to append the field.</param>
    /// <param name="type">The field type name.</param>
    /// <param name="name">The name of the field.</param>
    /// <param name="value">An (optional) initializer value for the field.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendPublicStaticReadonlyField(
        this CodeGenerator generator,
        string type,
        string name,
        string? value)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .AppendIndent("public static readonly ")
            .Append(type)
            .Append(' ')
            .Append(generator.GetStaticReadOnlyFieldNameInScope(name));

        if (value is string intializerValue)
        {
            generator
                .Append(" = ")
                .Append(intializerValue);
        }

        return generator
            .AppendLine(";");
    }

    /// <summary>
    /// Gets the name for a parameter.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetParameterNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the unique name for a parameter.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueParameterNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a field.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetFieldNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets unique name for a field.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueFieldNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a static readonly field.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetStaticReadOnlyFieldNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets a unique name for a static readonly field.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueStaticReadOnlyFieldNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a property.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetPropertyNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a property.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniquePropertyNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a method.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetMethodNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a method.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueMethodNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a class.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueClassNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Reserves a specific name in a scope.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator ReserveName(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator.ReserveName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.Unmodified,
                prefix,
                suffix));
    }

    /// <summary>
    /// Reserves a specific name in a scope.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator ReserveNameIfNotReserved(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator.ReserveNameIfNotReserved(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.Unmodified,
                prefix,
                suffix));
    }

    /// <summary>
    /// Tries to reserves a specific name in a scope.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static bool TryReserveName(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return false;
        }

        return generator.TryReserveName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.Unmodified,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a variable.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetVariableNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets a unique name for a variable.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetUniqueVariableNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetUniqueMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.CamelCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Gets the name for a type.
    /// </summary>
    /// <param name="generator">The generator from which to get the name.</param>
    /// <param name="baseName">The base name.</param>
    /// <param name="childScope">The (optional) child scope from the root scope.</param>
    /// <param name="rootScope">The (optional) root scope overriding the current scope.</param>
    /// <param name="prefix">The (optional) prefix for the name.</param>
    /// <param name="suffix">The (optional) suffix for the name.</param>
    /// <returns>A unique name in the scope.</returns>
    public static string GetTypeNameInScope(
        this CodeGenerator generator,
        string baseName,
        string? childScope = null,
        string? rootScope = null,
        string? prefix = null,
        string? suffix = null)
    {
        if (generator.IsCancellationRequested)
        {
            return string.Empty;
        }

        return generator.GetOrAddMemberName(
            new CSharpMemberName(
                generator.GetChildScope(childScope, rootScope),
                baseName,
                Casing.PascalCase,
                prefix,
                suffix));
    }

    /// <summary>
    /// Appends the Validate() method if there are no validation keywords present.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to append the method.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendValidateMethodForNoValidation(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if (typeDeclaration.ValidationKeywords().Count == 0)
        {
            generator
                .ReserveNameIfNotReserved("Validate")
                .AppendSeparatorLine();

            if (typeDeclaration.IsCorvusJsonExtendedJsonNotAny())
            {
                generator
                    .AppendBlockIndent(
                    """
                    /// <inheritdoc/>
                    public ValidationContext Validate(in ValidationContext context, ValidationLevel validationLevel = ValidationLevel.Flag) => context.WithResult(false);
                    """);
            }
            else
            {
                generator
                    .AppendBlockIndent(
                    """
                    /// <inheritdoc/>
                    public ValidationContext Validate(in ValidationContext context, ValidationLevel validationLevel = ValidationLevel.Flag) => context;
                    """);
            }
        }

        return generator;
    }

    /// <summary>
    /// Appends the pattern-matching methods.
    /// </summary>
    /// <param name="generator">The code generator.</param>
    /// <param name="typeDeclaration">The type declaration to which to append the method.</param>
    /// <returns>A reference to the generator having completed the operation.</returns>
    public static CodeGenerator AppendMatchMethods(this CodeGenerator generator, TypeDeclaration typeDeclaration)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        int matchOverloadIndex = 0;
        if (typeDeclaration.AllOfCompositionTypes() is IReadOnlyDictionary<IAllOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> allOf)
        {
            foreach (IAllOfSubschemaValidationKeyword keyword in allOf.Keys)
            {
                if (generator.IsCancellationRequested)
                {
                    return generator;
                }

                var subschema = allOf[keyword].Distinct().ToList();
                if (subschema.Count > 1)
                {
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: true, matchOverloadIndex++);
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: false, matchOverloadIndex++);
                }
            }
        }

        if (typeDeclaration.AnyOfCompositionTypes() is IReadOnlyDictionary<IAnyOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> anyOf)
        {
            foreach (IAnyOfSubschemaValidationKeyword keyword in anyOf.Keys)
            {
                if (generator.IsCancellationRequested)
                {
                    return generator;
                }

                var subschema = anyOf[keyword].Distinct().ToList();
                if (subschema.Count > 1)
                {
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: true, matchOverloadIndex++);
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: false, matchOverloadIndex++);
                }
            }
        }

        if (typeDeclaration.OneOfCompositionTypes() is IReadOnlyDictionary<IOneOfSubschemaValidationKeyword, IReadOnlyCollection<TypeDeclaration>> oneOf)
        {
            foreach (IOneOfSubschemaValidationKeyword keyword in oneOf.Keys)
            {
                if (generator.IsCancellationRequested)
                {
                    return generator;
                }

                var subschema = oneOf[keyword].Distinct().ToList();
                if (subschema.Count > 1)
                {
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: true, matchOverloadIndex++);
                    AppendMatchCompositionMethod(generator, typeDeclaration, subschema, includeContext: false, matchOverloadIndex++);
                }
            }
        }

        if (typeDeclaration.AnyOfConstantValues() is IReadOnlyDictionary<IAnyOfConstantValidationKeyword, JsonElement[]> anyOfConstant)
        {
            foreach (IAnyOfConstantValidationKeyword keyword in anyOfConstant.Keys)
            {
                if (generator.IsCancellationRequested)
                {
                    return generator;
                }

                JsonElement[] constantValues = anyOfConstant[keyword].Distinct().ToArray();
                if (constantValues.Length > 1)
                {
                    AppendMatchConstantMethod(generator, keyword, constantValues, includeContext: true, matchOverloadIndex: matchOverloadIndex++);
                    AppendMatchConstantMethod(generator, keyword, constantValues, includeContext: false, matchOverloadIndex: matchOverloadIndex++);
                }
            }
        }

        if (typeDeclaration.IfSubschemaType() is SingleSubschemaKeywordTypeDeclaration ifSubschema)
        {
            AppendMatchIfMethod(generator, typeDeclaration, ifSubschema, includeContext: true, matchOverloadIndex++);
            AppendMatchIfMethod(generator, typeDeclaration, ifSubschema, includeContext: false, matchOverloadIndex++);
        }

        return generator;

        static void AppendMatchCompositionMethod(CodeGenerator generator, TypeDeclaration typeDeclaration, IReadOnlyCollection<TypeDeclaration> subschema, bool includeContext, int matchOverloadIndex)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string scopeName = $"Match{matchOverloadIndex}";

            generator
                .ReserveNameIfNotReserved("Match")
                .AppendSeparatorLine()
                .AppendBlockIndent(
                """
                /// <summary>
                /// Matches the value against the composed values, and returns the result of calling the provided match function for the first match found.
                /// </summary>
                """);

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <typeparam name=\"TIn\">The immutable context to pass in to the match function.</typeparam>");
            }

            generator
                .AppendLineIndent("/// <typeparam name=\"TOut\">The result of calling the match function.</typeparam>");

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <param name=\"context\">The context to pass to the match function.</param>")
                    .ReserveNameIfNotReserved("context", childScope: scopeName);
            }

            // Reserve the parameter names we are going to require
            generator
                .ReserveNameIfNotReserved("defaultMatch", childScope: scopeName);

            string[] parameterNames = new string[subschema.Count];

            int i = 0;
            foreach (TypeDeclaration match in subschema)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                // This is the parameter name for the match match method.
                string matchTypeName = match.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName();
                string matchParamName = generator.GetUniqueParameterNameInScope(match.ReducedTypeDeclaration().ReducedType.DotnetTypeName(), childScope: scopeName, prefix: "match");

                parameterNames[i++] = matchParamName;

                generator
                    .AppendLineIndent("/// <param name=\"", matchParamName, "\">Match a <see cref=\"", matchTypeName, "\"/>.</param>");
            }

            generator
                .AppendLineIndent("/// <param name=\"defaultMatch\">Match any other value.</param>")
                .AppendLineIndent("/// <returns>An instance of the value returned by the match function.</returns>")
                .AppendLineIndent("public TOut Match<", includeContext ? "TIn, " : string.Empty, "TOut>(")
                .PushMemberScope(scopeName, ScopeType.Method)
                .PushIndent();

            if (includeContext)
            {
                generator
                    .AppendIndent("in TIn context");
            }

            i = 0;
            foreach (TypeDeclaration match in subschema)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                if (i > 0 || includeContext)
                {
                    generator
                        .AppendLine(",");
                }

                generator
                    .AppendIndent(
                        "Matcher<",
                        match.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName(),
                        includeContext ? ", TIn" : string.Empty,
                        ", TOut> ",
                        parameterNames[i++]);
            }

            generator
                .AppendLine(",")
                .AppendLineIndent(
                    "Matcher<",
                    typeDeclaration.FullyQualifiedDotnetTypeName(),
                    includeContext ? ", TIn" : string.Empty,
                    ", TOut> defaultMatch)")
                .PopIndent()
                .AppendLineIndent("{")
                .PushIndent();

            i = 0;
            foreach (TypeDeclaration match in subschema)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                string matchTypeName = match.ReducedTypeDeclaration().ReducedType.FullyQualifiedDotnetTypeName();
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent(
                        matchTypeName,
                        " ",
                        parameterNames[i],
                        "Value = this.As<",
                        matchTypeName,
                        ">();")
                    .AppendLineIndent("if (", parameterNames[i], "Value.IsValid())")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return ", parameterNames[i], "(", parameterNames[i], "Value", includeContext ? ", context);" : ");")
                    .PopIndent()
                    .AppendLineIndent("}");
                i++;
            }

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("return defaultMatch(this", includeContext ? ", context" : string.Empty, ");")
                .PopMemberScope()
                .PopIndent()
                .AppendLineIndent("}");
        }

        static void AppendMatchConstantMethod(CodeGenerator generator, IAnyOfConstantValidationKeyword keyword, JsonElement[] constValues, bool includeContext, int matchOverloadIndex)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            string scopeName = $"Match{matchOverloadIndex}";

            generator
                .ReserveNameIfNotReserved("Match")
                .AppendSeparatorLine()
                .AppendBlockIndent(
                """
                /// <summary>
                /// Matches the value against the constant values, and returns the result of calling the provided match function for the first match found.
                /// </summary>
                """);

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <typeparam name=\"TIn\">The immutable context to pass in to the match function.</typeparam>");
            }

            generator
                .AppendLineIndent("/// <typeparam name=\"TOut\">The result of calling the match function.</typeparam>");

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <param name=\"context\">The context to pass to the match function.</param>")
                    .ReserveNameIfNotReserved("context", childScope: scopeName);
            }

            // Reserve the parameter names we are going to require
            generator
                .ReserveNameIfNotReserved("defaultMatch", childScope: scopeName);

            int count = constValues.Length;
            string[] parameterNames = new string[count];
            string[] constFields = new string[count];

            for (int i = 1; i <= count; ++i)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                JsonElement constValue = constValues[i - 1];

                string matchParamName = GetUniqueParameterName(generator, scopeName, constValue, i);
                string constField =
                    generator.GetPropertyNameInScope(
                        keyword.Keyword,
                        rootScope: generator.ValidationClassScope(),
                        suffix: count > 1 ? i.ToString() : null);

                parameterNames[i - 1] = matchParamName;
                constFields[i - 1] = constField;

                generator
                    .AppendIndent("/// <param name=\"", matchParamName, "\">Match ")
                    .AppendOrdinalName(i)
                    .AppendLine(" item.</param>");
            }

            generator
                .AppendLineIndent("/// <param name=\"defaultMatch\">Match any other value.</param>")
                .AppendLineIndent("/// <returns>An instance of the value returned by the match function.</returns>")
                .AppendLineIndent("public TOut Match<", includeContext ? "TIn, " : string.Empty, "TOut>(")
                .PushMemberScope(scopeName, ScopeType.Method)
                .PushIndent();

            if (includeContext)
            {
                generator
                    .AppendIndent("in TIn context");
            }

            for (int i = 0; i < count; ++i)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                if (i > 0 || includeContext)
                {
                    generator
                        .AppendLine(",");
                }

                generator
                    .AppendIndent(
                        "Func<",
                        includeContext ? "TIn, " : string.Empty,
                        "TOut> ",
                        parameterNames[i]);
            }

            generator
                .AppendLine(",")
                .AppendLineIndent(
                    "Func<",
                    includeContext ? "TIn, " : string.Empty,
                    "TOut> defaultMatch)")
                .PopIndent()
                .AppendLineIndent("{")
                .PushIndent();

            for (int i = 0; i < count; ++i)
            {
                if (generator.IsCancellationRequested)
                {
                    return;
                }

                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("if (this.Equals(", generator.ValidationClassName(), ".", constFields[i], "))")
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return ", parameterNames[i], "(", includeContext ? "context);" : ");")
                    .PopIndent()
                    .AppendLineIndent("}");
            }

            generator
                .AppendSeparatorLine()
                .AppendLineIndent("return defaultMatch(", includeContext ? "context" : string.Empty, ");")
                .PopMemberScope()
                .PopIndent()
                .AppendLineIndent("}");
        }

        static string GetUniqueParameterName(CodeGenerator generator, string scopeName, JsonElement constValue, int index)
        {
            if (generator.IsCancellationRequested)
            {
                return string.Empty;
            }

            return constValue.ValueKind switch
            {
                JsonValueKind.Object => generator.GetUniqueParameterNameInScope("matchObjectValue", childScope: scopeName, suffix: index.ToString()),
                JsonValueKind.Array => generator.GetUniqueParameterNameInScope("matchArrayValue", childScope: scopeName, suffix: index.ToString()),
                JsonValueKind.String => generator.GetUniqueParameterNameInScope(constValue.GetString()!, childScope: scopeName, prefix: "match"),
                JsonValueKind.Number => generator.GetUniqueParameterNameInScope(constValue.GetRawText().Replace(".", "point"), childScope: scopeName, prefix: "matchNumber"),
                JsonValueKind.True => generator.GetUniqueParameterNameInScope("matchTrue", childScope: scopeName),
                JsonValueKind.False => generator.GetUniqueParameterNameInScope("matchFalse", childScope: scopeName),
                JsonValueKind.Null => generator.GetUniqueParameterNameInScope("matchNull", childScope: scopeName),
                _ => throw new InvalidOperationException($"Unsupport JsonValueKind: {constValue.ValueKind}"),
            };
        }

        static void AppendMatchIfMethod(CodeGenerator generator, TypeDeclaration typeDeclaration, SingleSubschemaKeywordTypeDeclaration ifSubschema, bool includeContext, int matchOverloadIndex)
        {
            if (generator.IsCancellationRequested)
            {
                return;
            }

            SingleSubschemaKeywordTypeDeclaration? thenDeclaration = typeDeclaration.ThenSubschemaType();
            SingleSubschemaKeywordTypeDeclaration? elseDeclaration = typeDeclaration.ElseSubschemaType();

            if (thenDeclaration is null && elseDeclaration is null)
            {
                return;
            }

            string scopeName = $"Match{matchOverloadIndex}";

            generator
                .ReserveNameIfNotReserved("Match")
                .AppendSeparatorLine()
                .AppendLineIndent("/// <summary>")
                .AppendLineIndent(
                    "/// Matches the value against the 'if' type, and returns the result of calling the provided match function for");
            if (thenDeclaration is not null)
            {
                generator
                    .AppendLineIndent("/// the 'then' type if the match is successful", elseDeclaration is not null ? " or" : ".");
            }

            if (elseDeclaration is not null)
            {
                generator
                    .AppendLineIndent("/// the 'else' type if the match is not successful.");
            }

            generator
                .AppendLineIndent("/// </summary>");

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <typeparam name=\"TIn\">The immutable context to pass in to the match function.</typeparam>");
            }

            generator
                .AppendLineIndent("/// <typeparam name=\"TOut\">The result of calling the match function.</typeparam>");

            if (includeContext)
            {
                generator
                    .AppendLineIndent("/// <param name=\"context\">The context to pass to the match function.</param>")
                    .ReserveNameIfNotReserved("context", childScope: scopeName);
            }

            string? thenMatchParamName = null;

            if (thenDeclaration is SingleSubschemaKeywordTypeDeclaration thenSubschema)
            {
                // This is the parameter name for the if match method.
                string? thenMatchTypeName = thenSubschema.ReducedType.FullyQualifiedDotnetTypeName();
                thenMatchParamName = generator.GetUniqueParameterNameInScope(thenMatchTypeName, childScope: scopeName, prefix: "match");

                generator
                    .AppendLineIndent("/// <param name=\"", thenMatchParamName, "\">Match a <see cref=\"", thenMatchTypeName, "\"/>.</param>");
            }

            string? elseMatchParamName = null;
            if (elseDeclaration is SingleSubschemaKeywordTypeDeclaration elseSubschema)
            {
                // This is the parameter name for the if match method.
                string? elseMatchTypeName = elseSubschema.ReducedType.FullyQualifiedDotnetTypeName();
                elseMatchParamName = generator.GetUniqueParameterNameInScope(elseMatchTypeName, childScope: scopeName, prefix: "match");

                generator
                    .AppendLineIndent("/// <param name=\"", elseMatchParamName, "\">Match a <see cref=\"", elseMatchTypeName, "\"/>.</param>");
            }

            if (elseMatchParamName is null)
            {
                generator
                    .AppendLineIndent("/// <param name=\"defaultMatch\">Default match if the 'if' schema did not match.</param>");
            }

            if (thenMatchParamName is null)
            {
                generator
                    .AppendLineIndent("/// <param name=\"defaultMatch\">Default match if the 'if' schema matched.</param>");
            }

            generator
                .AppendLineIndent("/// <returns>An instance of the value returned by the match function.</returns>")
                .AppendLineIndent("public TOut Match<", includeContext ? "TIn, " : string.Empty, "TOut>(")
                .PushMemberScope(scopeName, ScopeType.Method)
                .PushIndent();

            if (includeContext)
            {
                generator
                    .AppendIndent("in TIn context");
            }

            if (thenDeclaration is SingleSubschemaKeywordTypeDeclaration thenSubschema2 &&
                thenMatchParamName is string thenMatchParamName2)
            {
                if (includeContext)
                {
                    generator
                        .AppendLine(",");
                }

                generator
                    .AppendIndent(
                        "Matcher<",
                        thenSubschema2.ReducedType.FullyQualifiedDotnetTypeName(),
                        includeContext ? ", TIn" : string.Empty,
                        ", TOut> ",
                        thenMatchParamName2);
            }

            if (elseDeclaration is SingleSubschemaKeywordTypeDeclaration elseSubschema2 &&
                elseMatchParamName is string elseMatchParamName2)
            {
                if (thenDeclaration is not null || includeContext)
                {
                    generator
                        .AppendLine(",");
                }

                generator
                    .AppendIndent(
                        "Matcher<",
                        elseSubschema2.ReducedType.FullyQualifiedDotnetTypeName(),
                        includeContext ? ", TIn" : string.Empty,
                        ", TOut> ",
                        elseMatchParamName2);
            }

            if (thenDeclaration is null || elseDeclaration is null)
            {
                generator
                    .AppendLine(",")
                    .AppendIndent(
                        "Matcher<",
                        typeDeclaration.DotnetTypeName(),
                        includeContext ? ", TIn" : string.Empty,
                        ", TOut> defaultMatch");
            }

            generator
                .AppendLine(")")
                .PopIndent()
                .AppendLineIndent("{")
                .PushIndent();

            string matchTypeName = ifSubschema.ReducedType.FullyQualifiedDotnetTypeName();

            generator
                .AppendSeparatorLine()
                .AppendLineIndent(
                    matchTypeName,
                    " ifValue = this.As<",
                    matchTypeName,
                    ">();");

            if (thenDeclaration is not null)
            {
                generator
                    .AppendLineIndent("if (ifValue.IsValid())");
            }
            else
            {
                generator
                    .AppendLineIndent("if (!ifValue.IsValid())");
            }

            if (thenDeclaration is SingleSubschemaKeywordTypeDeclaration thenDeclaration3 &&
                thenMatchParamName is string thenMatchParam3)
            {
                generator
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return ", thenMatchParam3, "(this.As<", thenDeclaration3.ReducedType.FullyQualifiedDotnetTypeName(), ">()", includeContext ? ", context" : string.Empty, ");")
                    .PopIndent()
                    .AppendLineIndent("}");
            }

            if (elseDeclaration is SingleSubschemaKeywordTypeDeclaration elseDeclaration3 &&
                elseMatchParamName is string elseMatchParam3)
            {
                if (thenDeclaration is not null)
                {
                    generator
                        .AppendLineIndent("else");
                }

                generator
                    .AppendLineIndent("{")
                    .PushIndent()
                        .AppendLineIndent("return ", elseMatchParam3, "(this.As<", elseDeclaration3.ReducedType.FullyQualifiedDotnetTypeName(), ">()", includeContext ? ", context" : string.Empty, ");")
                    .PopIndent()
                    .AppendLineIndent("}");
            }

            if (thenDeclaration is null || elseDeclaration is null)
            {
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("return defaultMatch(this", includeContext ? ", context" : string.Empty, ");");
            }

            generator
                .PopMemberScope()
                .PopIndent()
                .AppendLineIndent("}");
        }
    }

    private static CodeGenerator AppendConversionFromValue(
        this CodeGenerator generator,
        string identifierName,
        CoreTypes forTypes,
        bool requiresAs = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        generator
            .AppendIndent("if (")
            .Append(identifierName)
            .AppendLine(".HasJsonElementBacking)")
            .AppendLineIndent("{")
            .PushIndent()
                .AppendIndent("return new(")
                .Append(identifierName)
                .AppendLine(".AsJsonElement);")
            .PopIndent()
            .AppendLineIndent("}")
            .AppendLine()
            .AppendIndent("return ")
            .Append(identifierName)
            .AppendLine(".ValueKind switch")
            .AppendLineIndent("{")
            .PushIndent();

        if ((forTypes & CoreTypes.String) != 0)
        {
            generator
                .AppendIndent("JsonValueKind.String => new(")
                .Append(identifierName)
                .AppendLine(requiresAs ? ".AsString" : string.Empty, ".GetString()!),");
        }

        if ((forTypes & CoreTypes.Boolean) != 0)
        {
            generator
                .AppendLineIndent("JsonValueKind.True => new(true),")
                .AppendLineIndent("JsonValueKind.False => new(false),");
        }

        if ((forTypes & CoreTypes.Number) != 0 ||
            (forTypes & CoreTypes.Integer) != 0)
        {
            generator
                .AppendIndent("JsonValueKind.Number => new(")
                .Append(identifierName)
                .AppendLine(requiresAs ? ".AsNumber" : string.Empty, ".AsBinaryJsonNumber),");
        }

        if ((forTypes & CoreTypes.Array) != 0)
        {
            generator
                .AppendIndent("JsonValueKind.Array => new(")
                .Append(identifierName)
                .AppendLine(requiresAs ? ".AsArray" : string.Empty, ".AsImmutableList()),");
        }

        if ((forTypes & CoreTypes.Object) != 0)
        {
            generator
                .AppendIndent("JsonValueKind.Object => new(")
                .Append(identifierName)
                .AppendLine(requiresAs ? ".AsObject" : string.Empty, ".AsPropertyBacking()),");
        }

        return generator
            .AppendLineIndent("JsonValueKind.Null => Null,")
            .AppendLineIndent("_ => Undefined,")
            .PopIndent()
            .AppendLineIndent("};");
    }

    private static CodeGenerator AppendBackingField(
        this CodeGenerator generator,
        string fieldType,
        string fieldName,
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string localBackingFieldName = generator.GetFieldNameInScope(fieldName);
            generator
                .AppendIndent("private readonly ")
                .Append(fieldType)
                .Append(' ')
                .Append(localBackingFieldName)
                .AppendLine(";");
        }

        return generator;
    }

    private static CodeGenerator AppendBackingFieldAssignment(
        this CodeGenerator generator,
        string fieldName,
        string fieldValue,
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string localBackingFieldName = generator.GetFieldNameInScope(fieldName);
            generator
                .AppendIndent("this.")
                .Append(localBackingFieldName)
                .Append(" = ")
                .Append(fieldValue)
                .AppendLine(";");
        }

        return generator;
    }

    private static CodeGenerator AppendBackingFieldAssignment(
    this CodeGenerator generator,
    TypeDeclaration typeDeclaration,
    string fieldName,
    AppendConstructorBackingFieldAssignmentCallback appendFieldValue,
    CoreTypes impliedCoreTypes = CoreTypes.Any,
    CoreTypes forCoreTypes = CoreTypes.Any)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            generator
                .AppendSeparatorLine();
            appendFieldValue(generator, typeDeclaration, fieldName);
        }

        return generator;
    }

    private static string GetBacking(CoreTypes valueCoreTypes)
    {
        return valueCoreTypes switch
        {
            CoreTypes.String => "Backing.String",
            CoreTypes.Boolean => "Backing.Bool",
            CoreTypes.Number => "Backing.Number",
            CoreTypes.Integer => "Backing.Number",
            CoreTypes.Number | CoreTypes.Integer => "Backing.Number",
            CoreTypes.Array => "Backing.Array",
            CoreTypes.Object => "Backing.Object",
            _ => throw new InvalidOperationException($"Unsupported backing type {valueCoreTypes}"),
        };
    }

    private static string GetValue(CoreTypes typeDeclarationImpliedCoreTypes, CoreTypes valueCoreTypes, string defaultValue, string valueConverter)
    {
        return (typeDeclarationImpliedCoreTypes & valueCoreTypes) != 0
            ? valueConverter
            : defaultValue;
    }

    private static AppendConstructorBackingFieldAssignmentCallback GetValue(CoreTypes typeDeclarationImpliedCoreTypes, CoreTypes valueCoreTypes, string defaultValue, AppendConstructorBackingFieldAssignmentCallback fieldAssignmentCallback)
    {
        return (typeDeclarationImpliedCoreTypes & valueCoreTypes) != 0
            ? fieldAssignmentCallback
            : (g, _, f) => g.AppendLineIndent("this.", f, " = ", defaultValue, ";");
    }

    private static CodeGenerator AppendConditionalConstructFromBacking(
        this CodeGenerator generator,
        string backingType,
        string fieldName,
        string identifier = "this",
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any)
    {
        return AppendConditionalWrappedBackingValueLineIndent(
            generator,
            backingType,
            "return new(",
            fieldName,
            ");",
            identifier,
            impliedCoreTypes,
            forCoreTypes);
    }

    private static CodeGenerator AppendConditionalWrappedJsonElementBackingValueKindLineIndent(
    this CodeGenerator generator,
    JsonValueKind jsonElementValueKind,
    string prefix,
    string fieldName,
    string suffix,
    string identifier = "this",
    CoreTypes impliedCoreTypes = CoreTypes.Any,
    CoreTypes forCoreTypes = CoreTypes.Any,
    bool returnFromClause = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string localBackingFieldName = generator.GetFieldNameInScope(fieldName);
            generator
                .AppendSeparatorLine()
                .AppendIndent("if (")
                .Append(identifier)
                .Append('.')
                .Append(localBackingFieldName)
                .Append(".ValueKind == JsonValueKind.")
                .Append(jsonElementValueKind.ToString())
                .AppendLine(")")
                .AppendLineIndent("{")
                .PushIndent()
                .AppendIndent(prefix)
                .Append(identifier)
                .Append('.')
                .Append(localBackingFieldName)
                .AppendLine(suffix);

            if (returnFromClause)
            {
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("return;");
            }

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    private static CodeGenerator AppendConditionalWrappedBackingValueLineIndent(
        this CodeGenerator generator,
        string backingType,
        string prefix,
        string fieldName,
        string suffix,
        string identifier = "this",
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any,
        bool returnFromClause = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string backingName = generator.GetFieldNameInScope("backing");
            string localBackingFieldName = generator.GetFieldNameInScope(fieldName);
            generator
                .AppendSeparatorLine()
                .AppendIndent("if ((")
                .Append(identifier)
                .Append('.')
                .Append(backingName)
                .Append(" & ")
                .Append(backingType)
                .AppendLine(") != 0)")
                .AppendLineIndent("{")
                .PushIndent()
                .AppendIndent(prefix)
                .Append(identifier)
                .Append('.')
                .Append(localBackingFieldName)
                .AppendLine(suffix);

            if (returnFromClause)
            {
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("return;");
            }

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    private static CodeGenerator AppendConditionalBackingValueLineIndent(
        this CodeGenerator generator,
        string backingType,
        string content,
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any,
        bool returnFromClause = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string backingName = generator.GetFieldNameInScope("backing");
            generator
                .AppendSeparatorLine()
                .AppendIndent("if ((this.")
                .Append(backingName)
                .Append(" & ")
                .Append(backingType)
                .AppendLine(") != 0)")
                .AppendLineIndent("{")
                .PushIndent()
                .AppendLineIndent(content);

            if (returnFromClause)
            {
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("return;");
            }

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    private static CodeGenerator AppendWriteJsonElementBacking(this CodeGenerator generator, string fieldName)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendIndent("if (this.")
            .Append(fieldName)
            .AppendLine(".ValueKind != JsonValueKind.Undefined)")
            .AppendLineIndent("{")
            .PushIndent()
            .AppendIndent("this.")
            .Append(fieldName)
            .AppendLine(".WriteTo(writer);")
            .PopIndent()
            .AppendLineIndent("}");
    }

    private static CodeGenerator AppendConditionalBackingValueCallbackIndent(
        this CodeGenerator generator,
        string backingType,
        string fieldName,
        Action<CodeGenerator, string> callback,
        string identifier = "this",
        CoreTypes impliedCoreTypes = CoreTypes.Any,
        CoreTypes forCoreTypes = CoreTypes.Any,
        bool returnFromClause = false)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        if ((impliedCoreTypes & forCoreTypes) != 0)
        {
            string backingName = generator.GetFieldNameInScope("backing");
            string localFieldName = generator.GetFieldNameInScope(fieldName);
            generator
                .AppendSeparatorLine()
                .AppendIndent("if ((")
                .Append(identifier)
                .Append('.')
                .Append(backingName)
                .Append(" & ")
                .Append(backingType)
                .AppendLine(") != 0)")
                .AppendLineIndent("{")
                .PushIndent();

            callback(generator, localFieldName);

            if (returnFromClause)
            {
                generator
                    .AppendSeparatorLine()
                    .AppendLineIndent("return;");
            }

            generator
                .PopIndent()
                .AppendLineIndent("}");
        }

        return generator;
    }

    private static CodeGenerator AppendReturnNullInstanceIfNull(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendSeparatorLine()
            .AppendIndent("if (")
            .AppendTestBacking("Backing.Null")
            .AppendLine(")")
            .AppendLineIndent("{")
            .PushIndent()
            .AppendLineIndent("return JsonAny.Null;")
            .PopIndent()
            .AppendLineIndent("}");
    }

    private static CodeGenerator AppendReturnNullJsonElementIfNull(this CodeGenerator generator)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        return generator
            .AppendIndent("if (")
            .AppendTestBacking("Backing.Null")
            .AppendLine(")")
            .AppendLineIndent("{")
            .PushIndent()
            .AppendLineIndent("return JsonValueHelpers.NullElement;")
            .PopIndent()
            .AppendLineIndent("}");
    }

    private static CodeGenerator AppendTestBacking(
        this CodeGenerator generator,
        string backingType)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string backingName = generator.GetFieldNameInScope("backing");
        return generator
            .Append("(this.")
            .Append(backingName)
            .Append(" & ")
            .Append(backingType)
            .Append(") != 0");
    }

    private static CodeGenerator AppendBackingFieldAssignment(
        this CodeGenerator generator,
        string fieldName,
        string fieldValue)
    {
        if (generator.IsCancellationRequested)
        {
            return generator;
        }

        string localBackingFieldName = generator.GetFieldNameInScope(fieldName);
        return generator
            .AppendIndent("this.")
            .Append(localBackingFieldName)
            .Append(" = ")
            .Append(fieldValue)
            .AppendLine(";");
    }

    private static CodeGenerator AppendCommaSeparatedNumericSuffixItems(this CodeGenerator generator, string baseName, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (i > 0)
            {
                generator
                    .Append(", ");
            }

            generator
                .Append(baseName)
                .Append(i + 1);
        }

        return generator;
    }

    private static CodeGenerator AppendCommaSeparatedNumericSuffixItems(this CodeGenerator generator, string baseNameFirst, string baseNameSecond, int count, string separator = " ")
    {
        for (int i = 0; i < count; ++i)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (i > 0)
            {
                generator
                    .Append(", ");
            }

            generator
                .Append(baseNameFirst)
                .Append(i + 1)
                .Append(separator)
                .Append(baseNameSecond)
                .Append(i + 1);
        }

        return generator;
    }

    private static CodeGenerator AppendCommaSeparatedInParameterAndNumericSuffixItems(this CodeGenerator generator, string baseNameType, string baseNameForNumericSuffix, int count, string separator = " ")
    {
        for (int i = 0; i < count; ++i)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (i > 0)
            {
                generator
                    .Append(", ");
            }

            generator
                .Append("in ")
                .Append(baseNameType)
                .Append(separator)
                .Append(baseNameForNumericSuffix)
                .Append(i + 1);
        }

        return generator;
    }

    private static CodeGenerator AppendCommaSeparatedValueItems(this CodeGenerator generator, TupleTypeDeclaration tupleType)
    {
        return generator.AppendCommaSeparatedNumericSuffixItems("value.Item", tupleType.ItemsTypes.Length);
    }

    private static CodeGenerator AppendCommaSeparatedTupleTypes(this CodeGenerator generator, TupleTypeDeclaration tupleType)
    {
        for (int i = 0; i < tupleType.ItemsTypes.Length; ++i)
        {
            if (generator.IsCancellationRequested)
            {
                return generator;
            }

            if (i > 0)
            {
                generator
                    .Append(", ");
            }

            generator
                .Append(tupleType.ItemsTypes[i].ReducedType.FullyQualifiedDotnetTypeName());
        }

        return generator;
    }

    private static string[] NormalizeAndSplitBlockIntoLines(string block, bool removeBlankLines = false)
    {
        string normalizedBlock = block.Replace("\r\n", "\n");
        string[] lines = normalizedBlock.Split(['\n'], removeBlankLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        return lines;
    }
}