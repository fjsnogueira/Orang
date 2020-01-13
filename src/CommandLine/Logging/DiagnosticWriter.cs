// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Orang.FileSystem;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Orang.CommandLine
{
    internal static class DiagnosticWriter
    {
        public static Verbosity Verbosity { get; } = Verbosity.Diagnostic;

        private static ConsoleColors ValueColors { get; } = new ConsoleColors(ConsoleColor.Cyan);

        private static ConsoleColors UnspecifiedColors { get; } = new ConsoleColors(ConsoleColor.DarkGray);

        internal static void WriteCommand(DeleteCommandOptions options)
        {
            WriteOption("ask", options.Ask);
            WriteOption("attributes", options.Attributes);
            WriteOption("attributes to skip", options.AttributesToSkip);
            WriteFilter("content filter", options.ContentFilter);
            WriteOption("content only", options.ContentOnly);
            WriteEncoding("default encoding", options.DefaultEncoding);
            WriteOption("directories only", options.DirectoriesOnly);
            WriteFilter("directory filter", options.DirectoryFilter);
            WriteDisplayFormat("display", options.Format);
            WriteOption("dry run", options.DryRun);
            WriteOption("empty", options.Empty);
            WriteFilter("extension filter", options.ExtensionFilter);
            WriteFilePropertyFilter("file property filter", options.FilePropertyFilter);
            WriteOption("files only", options.FilesOnly);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("including bom", options.IncludingBom);
            WriteOption("max matching files", options.MaxMatchingFiles);
            WriteFilter("name filter", options.NameFilter);
            WritePaths("paths", options.Paths);
            WriteOption("progress", options.Progress);
            WriteOption("recurse subdirectories", options.RecurseSubdirectories);
            WriteOption("search target", options.SearchTarget);
            WriteSortOptions("sort", options.SortOptions);
        }

        internal static void WriteCommand(EscapeCommandOptions options)
        {
            WriteOption("char group", options.InCharGroup);
            WriteOption("input", options.Input);
            WriteOption("replacement", options.Replacement);
        }

        internal static void WriteCommand(FindCommandOptions options)
        {
            WriteOption("ask", options.AskMode);
            WriteOption("attributes", options.Attributes);
            WriteOption("attributes to skip", options.AttributesToSkip);
            WriteFilter("content filter", options.ContentFilter);
            WriteEncoding("default encoding", options.DefaultEncoding);
            WriteFilter("directory filter", options.DirectoryFilter);
            WriteDisplayFormat("display", options.Format);
            WriteOption("empty", options.Empty);
            WriteFilter("extension filter", options.ExtensionFilter);
            WriteFilePropertyFilter("file property filter", options.FilePropertyFilter);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("max matches", options.MaxMatches);
            WriteOption("max matches in file", options.MaxMatchesInFile);
            WriteOption("max matching files", options.MaxMatchingFiles);
            WriteFilter("name filter", options.NameFilter);
            WritePaths("paths", options.Paths);
            WriteOption("progress", options.Progress);
            WriteOption("recurse subdirectories", options.RecurseSubdirectories);
            WriteOption("search target", options.SearchTarget);
            WriteSortOptions("sort", options.SortOptions);
        }

        internal static void WriteCommand(MatchCommandOptions options)
        {
            WriteDisplayFormat("display", options.Format);
            WriteFilter("filter", options.Filter);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("input", options.Input);
            WriteOption("max count", options.MaxCount);
        }

        internal static void WriteCommand(ListSyntaxCommandOptions options)
        {
            WriteOption("filter", options.Filter);
            WriteOption("sections", options.Sections);
        }

        internal static void WriteCommand(RenameCommandOptions options)
        {
            WriteOption("ask", options.Ask);
            WriteOption("attributes", options.Attributes);
            WriteOption("attributes to skip", options.AttributesToSkip);
            WriteFilter("content filter", options.ContentFilter);
            WriteEncoding("default encoding", options.DefaultEncoding);
            WriteFilter("directory filter", options.DirectoryFilter);
            WriteDisplayFormat("display", options.Format);
            WriteOption("dry run", options.DryRun);
            WriteOption("empty", options.Empty);
            WriteEvaluator("evaluator", options.ReplaceOptions.MatchEvaluator);
            WriteFilter("extension filter", options.ExtensionFilter);
            WriteFilePropertyFilter("file property filter", options.FilePropertyFilter);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("max matching files", options.MaxMatchingFiles);
            WriteModify("modify", options.ReplaceOptions);
            WriteFilter("name filter", options.NameFilter);
            WritePaths("paths", options.Paths);
            WriteOption("progress", options.Progress);
            WriteOption("recurse subdirectories", options.RecurseSubdirectories);
            WriteOption("replacement", options.ReplaceOptions.Replacement);
            WriteOption("search target", options.SearchTarget);
            WriteSortOptions("sort", options.SortOptions);
        }

        internal static void WriteCommand(ReplaceCommandOptions options)
        {
            WriteOption("ask", options.AskMode);
            WriteOption("attributes", options.Attributes);
            WriteOption("attributes to skip", options.AttributesToSkip);
            WriteFilter("content filter", options.ContentFilter);
            WriteEncoding("default encoding", options.DefaultEncoding);
            WriteFilter("directory filter", options.DirectoryFilter);
            WriteDisplayFormat("display", options.Format);
            WriteOption("dry run", options.DryRun);
            WriteOption("empty", options.Empty);
            WriteEvaluator("evaluator", options.ReplaceOptions.MatchEvaluator);
            WriteFilter("extension filter", options.ExtensionFilter);
            WriteFilePropertyFilter("file property filter", options.FilePropertyFilter);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("input", options.Input);
            WriteOption("max matches", options.MaxMatches);
            WriteOption("max matches in file", options.MaxMatchesInFile);
            WriteOption("max matching files", options.MaxMatchingFiles);
            WriteModify("modify", options.ReplaceOptions);
            WriteFilter("name filter", options.NameFilter);
            WritePaths("paths", options.Paths);
            WriteOption("progress", options.Progress);
            WriteOption("recurse subdirectories", options.RecurseSubdirectories);
            WriteOption("replacement", options.ReplaceOptions.Replacement);
            WriteOption("search target", options.SearchTarget);
            WriteSortOptions("sort", options.SortOptions);
        }

        internal static void WriteCommand(SplitCommandOptions options)
        {
            WriteDisplayFormat("display", options.Format);
            WriteFilter("filter", options.Filter);
            WriteOption("highlight options", options.HighlightOptions);
            WriteOption("input", options.Input);
            WriteOption("max count", options.MaxCount);
            WriteOption("omit groups", options.OmitGroups);
        }

        private static void WriteOption(string name, int value)
        {
            WriteName(name);
            WriteValue(value.ToString());
            WriteLine();
        }

        private static void WriteOption(string name, bool value)
        {
            WriteName(name);
            WriteValue(value);
            WriteLine();
        }

        private static void WriteOption(string name, bool? value)
        {
            WriteName(name);

            if (value == null)
            {
                WriteUnspecifiedValue();
            }
            else
            {
                WriteValue(value.Value);
            }

            WriteLine();
        }

        private static void WriteOption(string name, string value)
        {
            WriteName(name);
            WriteValue(value);
            WriteLine();
        }

        private static void WriteOption<TEnum>(string name, TEnum value) where TEnum : Enum
        {
            WriteName(name);
            WriteValue(value.ToString());
            WriteLine();
        }

        private static void WriteOption<TEnum>(string name, ImmutableArray<TEnum> values) where TEnum : Enum
        {
            WriteName(name);
            WriteLine();
            WriteValues(values);
        }

        private static void WriteEncoding(string name, Encoding encoding)
        {
            WriteName(name);
            WriteValue(encoding.EncodingName);
            WriteLine();
        }

        private static void WriteFilter(string name, Filter filter)
        {
            WriteName(name);

            if (filter == null)
            {
                WriteUnspecifiedValue();
                WriteLine();
                return;
            }

            WriteLine();
            WriteIndent();
            WriteName("pattern");
            WriteLine($"  {filter.Regex}");
            WriteIndent();
            WriteName("options");
            WriteLine($"  {filter.Regex.Options.ToString()}");
            WriteIndent();
            WriteName("negative");
            WriteLine($" {filter.IsNegative.ToString().ToLowerInvariant()}");
            WriteIndent();
            WriteName("group");
            Write("   ");

            if (string.IsNullOrEmpty(filter.GroupName))
            {
                WriteUnspecifiedValue();
                WriteLine();
            }
            else
            {
                WriteLine(filter.GroupName);
            }

            WriteIndent();
            WriteName("part");
            WriteLine($"     {filter.NamePart}");
        }

        private static void WriteFilePropertyFilter(string name, FilePropertyFilter filter)
        {
            WriteName(name);

            if (filter == null)
            {
                WriteUnspecifiedValue();
                WriteLine();
                return;
            }

            WriteLine();

            WriteIndent();
            WriteFilterPredicate("creation time", filter.CreationTimePredicate);
            WriteFilterPredicate("modified time", filter.ModifiedTimePredicate);
            WriteFilterPredicate("size", filter.SizePredicate);

            static void WriteFilterPredicate<T>(string name, FilterPredicate<T> filterPredicate)
            {
                if (filterPredicate == null)
                    return;

                WriteIndent();
                WriteName(name);
                WriteValue(filterPredicate.Expression.Kind.ToString());
                WriteIndent();
                WriteValue(filterPredicate.Expression.ExpressionText);
                WriteLine();
            }
        }

        private static void WriteDisplayFormat(string name, OutputDisplayFormat format)
        {
            WriteName(name);

            if (format == null)
            {
                WriteUnspecifiedValue();
                WriteLine();
                return;
            }

            WriteLine();
            WriteIndent();
            WriteOption("content display", format.ContentDisplayStyle);
            WriteIndent();
            WriteOption("display parts", format.DisplayParts);
            WriteIndent();
            WriteOption("file properties", format.FileProperties);
            WriteIndent();
            WriteOption("include base directory", format.IncludeBaseDirectory);
            WriteIndent();
            WriteOption("indent", format.Indent);
            WriteIndent();
            WriteOption("line options", format.LineOptions);
            WriteIndent();
            WriteOption("path display", format.PathDisplayStyle);
            WriteIndent();
            WriteOption("separator", format.Separator);
        }

        private static void WriteSortOptions(string name, SortOptions options)
        {
            WriteName(name);

            if (options == null)
            {
                WriteUnspecifiedValue();
                WriteLine();
                return;
            }

            WriteLine();
            WriteIndent();
            WriteOption("max count", options.MaxCount);

            foreach (SortDescriptor descriptor in options.Descriptors)
            {
                WriteIndent();
                WriteName("property");
                WriteValue(descriptor.Property.ToString());
                WriteIndent();
                WriteName("direction");
                WriteValue(descriptor.Direction.ToString());
                WriteLine();
            }
        }

        private static void WritePaths(string name, ImmutableArray<PathInfo> paths)
        {
            WriteName(name);
            WriteLine();

            foreach (PathInfo path in paths)
            {
                WriteIndent();
                WriteValue($"{path.Origin} {path.Path}");
                WriteLine();
            }
        }

        private static void WriteEvaluator(string name, MatchEvaluator matchEvaluator)
        {
            WriteName(name);

            if (matchEvaluator != null
                && !object.ReferenceEquals(matchEvaluator.Method.DeclaringType.Assembly, typeof(Program).Assembly))
            {
                WriteLine();
                WriteIndent();
                WriteOption("type", matchEvaluator.Method.DeclaringType.AssemblyQualifiedName);
                WriteIndent();
                WriteOption("method", matchEvaluator.Method.Name);
            }
            else
            {
                WriteUnspecifiedValue();
                WriteLine();
            }
        }

        private static void WriteModify(string name, ReplaceOptions options)
        {
            WriteName(name);

            if (options.Functions != ReplaceFunctions.None)
            {
                if (options.CultureInvariant)
                {
                    WriteValue(nameof(options.CultureInvariant) + ", " + options.Functions.ToString());
                }
                else
                {
                    WriteValue(options.Functions.ToString());
                }
            }
            else if (options.CultureInvariant)
            {
                WriteValue(nameof(options.CultureInvariant));
            }
            else
            {
                WriteUnspecifiedValue();
            }

            WriteLine();
        }

        private static void WriteValue(bool value)
        {
            WriteIndent();
            Write((value) ? "true" : "false");
        }

        private static void WriteValue(string value)
        {
            WriteIndent();
            Write(value);
        }

        private static void WriteValues<T>(ImmutableArray<T> values)
        {
            foreach (T value in values)
            {
                WriteValue(value.ToString());
                WriteLine();
            }
        }

        private static void WriteIndent()
        {
            Write(" ");
        }

        private static void WriteName(string name)
        {
            Logger.Write(name, Verbosity);
            Logger.Write(":", Verbosity);
        }

        private static void WriteUnspecifiedValue()
        {
            WriteIndent();
            Logger.Write("<unspecified>", UnspecifiedColors, Verbosity);
        }

        private static void Write(string value)
        {
            Logger.Write(value, ValueColors, Verbosity);
        }

        private static void WriteLine()
        {
            Logger.WriteLine(Verbosity);
        }

        private static void WriteLine(string value)
        {
            Logger.WriteLine(value, ValueColors, Verbosity);
        }
    }
}
