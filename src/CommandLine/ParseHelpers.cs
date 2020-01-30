﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orang.Expressions;
using Orang.FileSystem;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal static class ParseHelpers
    {
        public static bool TryParseFileProperties(
            IEnumerable<string> values,
            string optionName,
            out FilePropertyFilter filter)
        {
            filter = null;
            FilterPredicate<long> sizePredicate = null;
            FilterPredicate<DateTime> creationTimePredicate = null;
            FilterPredicate<DateTime> modifiedTimePredicate = null;

            foreach (string value in values)
            {
                try
                {
                    Expression expression = Expression.Parse(value);

                    if (OptionValues.FileProperty_Size.IsKeyOrShortKey(expression.Identifier))
                    {
                        if (expression.Kind == ExpressionKind.DecrementExpression)
                        {
                            WriteError($"Option '{OptionNames.GetHelpText(optionName)}' has invalid expression '{value}'.");
                            return false;
                        }

                        sizePredicate = new FilterPredicate<long>(expression, PredicateHelpers.GetLongPredicate(expression));
                    }
                    else if (OptionValues.FileProperty_CreationTime.IsKeyOrShortKey(expression.Identifier))
                    {
                        creationTimePredicate = new FilterPredicate<DateTime>(expression, PredicateHelpers.GetDateTimePredicate(expression));
                    }
                    else if (OptionValues.FileProperty_ModifiedTime.IsKeyOrShortKey(expression.Identifier))
                    {
                        modifiedTimePredicate = new FilterPredicate<DateTime>(expression, PredicateHelpers.GetDateTimePredicate(expression));
                    }
                    else
                    {
                        WriteParseError(value, optionName, OptionValueProviders.FilePropertiesProvider);
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    WriteError($"Option '{OptionNames.GetHelpText(optionName)}' has invalid expression '{value}'.");
                    return false;
                }
            }

            filter = new FilePropertyFilter(
                sizePredicate: sizePredicate,
                creationTimePredicate: creationTimePredicate,
                modifiedTimePredicate: modifiedTimePredicate);

            return true;
        }

        public static bool TryParseSortOptions(
            IEnumerable<string> values,
            string optionName,
            out SortOptions sortOptions)
        {
            sortOptions = null;
            int maxCount = 0;

            List<string> options = null;

            if (!values.Any())
                return true;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.MaxCount.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, out maxCount, value))
                            return false;
                    }
                    else
                    {
                        WriteParseError(value, optionName, OptionValueProviders.SortFlagsProvider);
                        return false;
                    }
                }
                else
                {
                    (options ?? (options = new List<string>())).Add(value);
                }
            }

            if (!TryParseAsEnumValues(options, optionName, out ImmutableArray<SortFlags> flags, provider: OptionValueProviders.SortFlagsProvider))
                return false;

            SortDirection direction = (flags.Contains(SortFlags.Descending))
                ? SortDirection.Descending
                : SortDirection.Ascending;

            List<SortDescriptor> descriptors = null;

            foreach (SortFlags flag in flags)
            {
                switch (flag)
                {
                    case SortFlags.Name:
                        {
                            AddDescriptor(SortProperty.Name, direction);
                            break;
                        }
                    case SortFlags.CreationTime:
                        {
                            AddDescriptor(SortProperty.CreationTime, direction);
                            break;
                        }
                    case SortFlags.ModifiedTime:
                        {
                            AddDescriptor(SortProperty.ModifiedTime, direction);
                            break;
                        }
                    case SortFlags.Size:
                        {
                            AddDescriptor(SortProperty.Size, direction);
                            break;
                        }
                    case SortFlags.None:
                    case SortFlags.Ascending:
                    case SortFlags.Descending:
                        {
                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException($"Unknown enum value '{flag}'.");
                        }
                }
            }

            if (descriptors != null)
            {
                sortOptions = new SortOptions(descriptors.ToImmutableArray(), maxCount: maxCount);
            }
            else
            {
                sortOptions = new SortOptions(ImmutableArray.Create(new SortDescriptor(SortProperty.Name, SortDirection.Ascending)), maxCount: maxCount);
            }

            return true;

            void AddDescriptor(SortProperty p, SortDirection d)
            {
                (descriptors ?? (descriptors = new List<SortDescriptor>())).Add(new SortDescriptor(p, d));
            }
        }

        public static bool TryParseModifyOptions(
            IEnumerable<string> values,
            string optionName,
            out ModifyOptions modifyOptions,
            out bool aggregateOnly)
        {
            modifyOptions = null;
            aggregateOnly = false;

            var sortProperty = ValueSortProperty.None;
            List<string> options = null;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.SortBy.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(value2, optionName, out sortProperty, provider: OptionValueProviders.ValueSortPropertyProvider))
                            return false;
                    }
                    else
                    {
                        string helpText = OptionValueProviders.ModifyFlagsProvider.GetHelpText();
                        WriteError($"Option '{OptionNames.GetHelpText(optionName)}' has invalid value '{value}'. Allowed values: {helpText}.");
                        return false;
                    }
                }
                else
                {
                    (options ?? (options = new List<string>())).Add(value);
                }
            }

            var modifyFlags = ModifyFlags.None;

            if (options != null
                && !TryParseAsEnumFlags(options, optionName, out modifyFlags, provider: OptionValueProviders.ModifyFlagsProvider))
            {
                return false;
            }

            if ((modifyFlags & ModifyFlags.ExceptIntersect) == ModifyFlags.ExceptIntersect)
            {
                WriteError($"Values '{OptionValues.ModifyFlags_Except.HelpValue}' and '{OptionValues.ModifyFlags_Intersect.HelpValue}' cannot be use both at the same time.");
                return false;
            }

            var functions = ModifyFunctions.None;

            if ((modifyFlags & ModifyFlags.Distinct) != 0)
                functions |= ModifyFunctions.Distinct;

            if ((modifyFlags & ModifyFlags.Ascending) != 0)
                functions |= ModifyFunctions.Sort;

            if ((modifyFlags & ModifyFlags.Descending) != 0)
                functions |= ModifyFunctions.SortDescending;

            if ((modifyFlags & ModifyFlags.Except) != 0)
                functions |= ModifyFunctions.Except;

            if ((modifyFlags & ModifyFlags.Intersect) != 0)
                functions |= ModifyFunctions.Intersect;

            if ((modifyFlags & ModifyFlags.RemoveEmpty) != 0)
                functions |= ModifyFunctions.RemoveEmpty;

            if ((modifyFlags & ModifyFlags.RemoveWhiteSpace) != 0)
                functions |= ModifyFunctions.RemoveWhiteSpace;

            if ((modifyFlags & ModifyFlags.TrimStart) != 0)
                functions |= ModifyFunctions.TrimStart;

            if ((modifyFlags & ModifyFlags.TrimEnd) != 0)
                functions |= ModifyFunctions.TrimEnd;

            if ((modifyFlags & ModifyFlags.ToLower) != 0)
                functions |= ModifyFunctions.ToLower;

            if ((modifyFlags & ModifyFlags.ToUpper) != 0)
                functions |= ModifyFunctions.ToUpper;

            aggregateOnly = (modifyFlags & ModifyFlags.AggregateOnly) != 0;

            if (modifyFlags != ModifyFlags.None)
            {
                modifyOptions = new ModifyOptions(
                    functions: functions,
                    aggregate: (modifyFlags & ModifyFlags.Aggregate) != 0 || aggregateOnly,
                    ignoreCase: (modifyFlags & ModifyFlags.IgnoreCase) != 0,
                    cultureInvariant: (modifyFlags & ModifyFlags.CultureInvariant) != 0,
                    sortProperty: sortProperty);
            }
            else
            {
                modifyOptions = ModifyOptions.Default;
            }

            return true;
        }

        public static bool TryParseReplaceOptions(
            IEnumerable<string> values,
            string optionName,
            string replacement,
            MatchEvaluator matchEvaluator,
            out ReplaceOptions replaceOptions)
        {
            replaceOptions = null;
            var replaceFlags = ReplaceFlags.None;

            if (values != null
                && !TryParseAsEnumFlags(values, optionName, out replaceFlags, provider: OptionValueProviders.ReplaceFlagsProvider))
            {
                return false;
            }

            var functions = ReplaceFunctions.None;

            if ((replaceFlags & ReplaceFlags.TrimStart) != 0)
                functions |= ReplaceFunctions.TrimStart;

            if ((replaceFlags & ReplaceFlags.TrimEnd) != 0)
                functions |= ReplaceFunctions.TrimEnd;

            if ((replaceFlags & ReplaceFlags.ToLower) != 0)
                functions |= ReplaceFunctions.ToLower;

            if ((replaceFlags & ReplaceFlags.ToUpper) != 0)
                functions |= ReplaceFunctions.ToUpper;

            replaceOptions = new ReplaceOptions(
                replacement: replacement,
                matchEvaluator: matchEvaluator,
                functions: functions,
                cultureInvariant: (replaceFlags & ReplaceFlags.CultureInvariant) != 0);

            return true;
        }

        public static bool TryParseDisplay(
            IEnumerable<string> values,
            string optionName,
            out ContentDisplayStyle? contentDisplayStyle,
            out PathDisplayStyle? pathDisplayStyle,
            out LineDisplayOptions lineDisplayOptions,
            out DisplayParts displayParts,
            out ImmutableArray<FileProperty> fileProperties,
            out string indent,
            out string separator,
            OptionValueProvider contentDisplayStyleProvider = null,
            OptionValueProvider pathDisplayStyleProvider = null)
        {
            contentDisplayStyle = null;
            pathDisplayStyle = null;
            lineDisplayOptions = LineDisplayOptions.None;
            displayParts = DisplayParts.None;
            fileProperties = ImmutableArray<FileProperty>.Empty;
            indent = null;
            separator = null;

            ImmutableArray<FileProperty>.Builder builder = null;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.Display_Content.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(value2, optionName, out ContentDisplayStyle contentDisplayStyle2, provider: contentDisplayStyleProvider))
                            return false;

                        contentDisplayStyle = contentDisplayStyle2;
                    }
                    else if (OptionValues.Display_Path.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(value2, optionName, out PathDisplayStyle pathDisplayStyle2, provider: pathDisplayStyleProvider))
                            return false;

                        pathDisplayStyle = pathDisplayStyle2;
                    }
                    else if (OptionValues.Display_Indent.IsKeyOrShortKey(key))
                    {
                        indent = value2;
                    }
                    else if (OptionValues.Display_Separator.IsKeyOrShortKey(key))
                    {
                        separator = value2;
                    }
                    else
                    {
                        ThrowException(value);
                    }
                }
                else if (OptionValues.Display_Summary.IsValueOrShortValue(value))
                {
                    displayParts |= DisplayParts.Summary;
                }
                else if (OptionValues.Display_Count.IsValueOrShortValue(value))
                {
                    displayParts |= DisplayParts.Count;
                }
                else if (OptionValues.Display_CreationTime.IsValueOrShortValue(value))
                {
                    (builder ?? (builder = ImmutableArray.CreateBuilder<FileProperty>())).Add(FileProperty.CreationTime);
                }
                else if (OptionValues.Display_ModifiedTime.IsValueOrShortValue(value))
                {
                    (builder ?? (builder = ImmutableArray.CreateBuilder<FileProperty>())).Add(FileProperty.ModifiedTime);
                }
                else if (OptionValues.Display_Size.IsValueOrShortValue(value))
                {
                    (builder ?? (builder = ImmutableArray.CreateBuilder<FileProperty>())).Add(FileProperty.Size);
                }
                else if (OptionValues.Display_LineNumber.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.IncludeLineNumber;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else
                {
                    ThrowException(value);
                }
            }

            if (builder != null)
                fileProperties = builder.ToImmutableArray();

            return true;

            void ThrowException(string value)
            {
                string helpText = OptionValueProviders.DisplayProvider.GetHelpText();

                throw new ArgumentException($"Option '{OptionNames.GetHelpText(optionName)}' has invalid value '{value}'. Allowed values: {helpText}.", nameof(values));
            }
        }

        public static bool TryParseOutputOptions(
            IEnumerable<string> values,
            string optionName,
            out string path,
            out Verbosity verbosity,
            out Encoding encoding,
            out bool append)
        {
            path = null;
            verbosity = Verbosity.Normal;
            encoding = Encoding.UTF8;
            append = false;

            if (!values.Any())
                return true;

            if (!TryEnsureFullPath(values.First(), out path))
                return false;

            foreach (string value in values.Skip(1))
            {
                string option = value;

                int index = option.IndexOf('=');

                if (index >= 0)
                {
                    string key = option.Substring(0, index);
                    string value2 = option.Substring(index + 1);

                    if (OptionValues.Verbosity.IsKeyOrShortKey(key))
                    {
                        if (!TryParseVerbosity(value2, out verbosity))
                            return false;
                    }
                    else if (OptionValues.Encoding.IsKeyOrShortKey(key))
                    {
                        if (!TryParseEncoding(value2, out encoding))
                            return false;
                    }
                    else
                    {
                        WriteParseError(value, optionName, OptionValueProviders.OutputFlagsProvider);
                        return false;
                    }
                }
                else if (OptionValues.Output_Append.IsValueOrShortValue(value))
                {
                    append = true;
                }
                else
                {
                    WriteParseError(value, optionName, OptionValueProviders.OutputFlagsProvider);
                    return false;
                }
            }

            return true;
        }

        public static bool TryParseReplacement(
            IEnumerable<string> values,
            out string replacement)
        {
            if (!values.Any())
            {
                replacement = null;
                return true;
            }

            replacement = values.First();

            if (!TryParseAsEnumFlags(values.Skip(1), OptionNames.Replacement, out ReplacementOptions options, ReplacementOptions.None, OptionValueProviders.ReplacementOptionsProvider))
                return false;

            if ((options & ReplacementOptions.FromFile) != 0
                && !FileSystemHelpers.TryReadAllText(replacement, out replacement))
            {
                return false;
            }

            if ((options & ReplacementOptions.Literal) != 0)
                replacement = RegexEscape.EscapeSubstitution(replacement);

            if ((options & ReplacementOptions.CharacterEscapes) != 0)
                replacement = RegexEscape.ConvertCharacterEscapes(replacement);

            return true;
        }

        public static bool TryParseAsEnumFlags<TEnum>(
            IEnumerable<string> values,
            string optionName,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider provider = null) where TEnum : struct
        {
            result = (TEnum)(object)0;

            if (values?.Any() != true)
            {
                if (defaultValue != null)
                {
                    result = (TEnum)(object)defaultValue;
                }

                return true;
            }

            int flags = 0;

            foreach (string value in values)
            {
                if (!TryParseAsEnum(value, optionName, out TEnum result2, provider: provider))
                    return false;

                flags |= (int)(object)result2;
            }

            result = (TEnum)(object)flags;

            return true;
        }

        public static bool TryParseAsEnumValues<TEnum>(
            IEnumerable<string> values,
            string optionName,
            out ImmutableArray<TEnum> result,
            ImmutableArray<TEnum> defaultValue = default,
            OptionValueProvider provider = null) where TEnum : struct
        {
            if (values?.Any() != true)
            {
                result = (defaultValue.IsDefault) ? ImmutableArray<TEnum>.Empty : defaultValue;

                return true;
            }

            ImmutableArray<TEnum>.Builder builder = ImmutableArray.CreateBuilder<TEnum>();

            foreach (string value in values)
            {
                if (!TryParseAsEnum(value, optionName, out TEnum result2, provider: provider))
                    return false;

                builder.Add(result2);
            }

            result = builder.ToImmutableArray();

            return true;
        }

        public static bool TryParseAsEnum<TEnum>(
            string value,
            string optionName,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider provider = null) where TEnum : struct
        {
            if (!TryParseAsEnum(value, out result, defaultValue, provider))
            {
                WriteParseError(value, optionName, provider?.GetHelpText() ?? OptionValue.GetDefaultHelpText<TEnum>());
                return false;
            }

            return true;
        }

        public static bool TryParseAsEnum<TEnum>(
            string value,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider provider = default) where TEnum : struct
        {
            if (value == null
                && defaultValue != null)
            {
                result = defaultValue.Value;
                return true;
            }

            if (provider != null)
            {
                return provider.TryParseEnum(value, out result);
            }
            else
            {
                return Enum.TryParse(value?.Replace("-", ""), ignoreCase: true, out result);
            }
        }

        public static bool TryParseVerbosity(string value, out Verbosity verbosity)
        {
            return TryParseAsEnum(value, OptionNames.Verbosity, out verbosity, provider: OptionValueProviders.VerbosityProvider);
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding#remarks
        public static bool TryParseEncoding(string name, out Encoding encoding)
        {
            if (name == "utf-8-no-bom")
            {
                encoding = EncodingHelpers.UTF8NoBom;
                return true;
            }

            try
            {
                encoding = Encoding.GetEncoding(name);
                return true;
            }
            catch (ArgumentException ex)
            {
                WriteError(ex);

                encoding = null;
                return false;
            }
        }

        public static bool TryParseEncoding(string name, out Encoding encoding, Encoding defaultEncoding)
        {
            if (name == null)
            {
                encoding = defaultEncoding;
                return true;
            }

            return TryParseEncoding(name, out encoding, defaultEncoding);
        }

        public static bool TryParseMaxCount(IEnumerable<string> values, out int maxCount, out int maxMatches, out int maxMatchingFiles)
        {
            maxCount = 0;
            maxMatches = 0;
            maxMatchingFiles = 0;

            if (!values.Any())
                return true;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.MaxMatches.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, out maxMatches, value))
                            return false;
                    }
                    else if (OptionValues.MaxMatchingFiles.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, out maxMatchingFiles, value))
                            return false;
                    }
                    else
                    {
                        WriteParseError(value, OptionNames.MaxCount, OptionValueProviders.MaxOptionsProvider);
                        return false;
                    }
                }
                else if (!TryParseCount(value, out maxCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseCount(string value, out int count, string value2 = null)
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count))
                return true;

            WriteError($"Option '{OptionNames.GetHelpText(OptionNames.MaxCount)}' has invalid value '{value2 ?? value}'.");
            return false;
        }

        public static bool TryParseChar(string value, out char result)
        {
            if (value.Length == 2
                && value[0] == '\\'
                && value[1] >= 48
                && value[1] <= 57)
            {
                result = value[1];
                return true;
            }

            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int charCode))
            {
                if (charCode < 0 || charCode > 0xFFFF)
                {
                    WriteError("Value must be in range from 0 to 65535.");
                    result = default;
                    return false;
                }

                result = (char)charCode;
            }
            else if (!char.TryParse(value, out result))
            {
                WriteError($"Could not parse '{value}' as character value.");
                return false;
            }

            return true;
        }

        public static bool TryEnsureFullPath(IEnumerable<string> paths, out ImmutableArray<string> fullPaths)
        {
            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

            foreach (string path in paths)
            {
                if (!TryEnsureFullPath(path, out string fullPath))
                    return false;

                builder.Add(fullPath);
            }

            fullPaths = builder.ToImmutableArray();
            return true;
        }

        public static bool TryEnsureFullPath(string path, out string result)
        {
            try
            {
                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(path);

                result = path;
                return true;
            }
            catch (ArgumentException ex)
            {
                WriteError($"Path '{path}' is invalid: {ex.Message}.");
                result = null;
                return false;
            }
        }

        private static void WriteParseError(string value, string optionName, OptionValueProvider provider)
        {
            string helpText = provider.GetHelpText();

            WriteParseError(value, optionName, helpText);
        }

        private static void WriteParseError(string value, string optionName, string helpText)
        {
            WriteError($"Option '{OptionNames.GetHelpText(optionName)}' has invalid value '{value}'. Allowed values: {helpText}.");
        }

        internal static bool TryParseProperties(string ask, IEnumerable<string> name, CommonFindCommandOptions options)
        {
            if (!TryParseAsEnum(ask, OptionNames.Ask, out AskMode askMode, defaultValue: AskMode.None, OptionValueProviders.AskModeProvider))
                return false;

            if (askMode == AskMode.Value
                && ConsoleOut.Verbosity < Verbosity.Normal)
            {
                WriteError($"Option '{OptionNames.GetHelpText(OptionNames.Ask)}' cannot have value '{OptionValueProviders.AskModeProvider.GetValue(nameof(AskMode.Value)).HelpValue}' when '{OptionNames.GetHelpText(OptionNames.Verbosity)}' is set to '{OptionValueProviders.VerbosityProvider.GetValue(ConsoleOut.Verbosity.ToString()).HelpValue}'.");
                return false;
            }

            if (!FilterParser.TryParse(name, OptionNames.Name, OptionValueProviders.PatternOptionsProvider, out Filter nameFilter, allowNull: true))
                return false;

            options.AskMode = askMode;
            options.NameFilter = nameFilter;

            return true;
        }
    }
}
