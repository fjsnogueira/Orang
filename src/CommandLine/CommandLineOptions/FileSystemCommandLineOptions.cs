﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [OptionValueProvider(nameof(AttributesToSkip), OptionValueProviderNames.FileSystemAttributesToSkip)]
    internal abstract class FileSystemCommandLineOptions : CommonRegexCommandLineOptions
    {
        private FileSystemAttributes FileSystemAttributes { get; set; }

        [Value(index: 0,
            HelpText = "Path to one or more files and/or directories that should be searched.",
            MetaName = ArgumentMetaNames.Path)]
        public IEnumerable<string> Path { get; set; }

        [Option(shortName: OptionShortNames.Attributes, longName: OptionNames.Attributes,
        HelpText = "File attributes that are required.",
        MetaValue = MetaValues.Attributes)]
        public IEnumerable<string> Attributes { get; set; }

        [Option(longName: OptionNames.AttributesToSkip,
            HelpText = "File attributes that should be skipped.",
            MetaValue = MetaValues.Attributes)]
        public IEnumerable<string> AttributesToSkip { get; set; }

        [Option(longName: OptionNames.Encoding,
            HelpText = "Encoding to use when a file does not contain byte order mark. Default encoding is UTF-8.",
            MetaValue = MetaValues.Encoding)]
        public string Encoding { get; set; }

        [Option(shortName: OptionShortNames.Extension, longName: OptionNames.Extension,
            HelpText = "A filter for file extensions (case-insensitive by default). Syntax is EXT1[,EXT2,...] [<EXTENSION_OPTIONS>].",
            MetaValue = MetaValues.ExtensionFilter)]
        public IEnumerable<string> Extension { get; set; }

        [Option(shortName: OptionShortNames.Properties, longName: OptionNames.Properties,
            HelpText = "A filter for file properties.",
            MetaValue = MetaValues.FileProperties)]
        public IEnumerable<string> FileProperties { get; set; }

        [Option(shortName: OptionShortNames.IncludeDirectory, longName: OptionNames.IncludeDirectory,
            HelpText = "Regular expression for a directory name. Syntax is <PATTERN> [<PATTERN_OPTIONS>].",
            MetaValue = MetaValues.Regex)]
        public IEnumerable<string> IncludeDirectory { get; set; }

        [Option(longName: OptionNames.NoRecurse,
            HelpText = "Do not search subdirectories.")]
        public bool NoRecurse { get; set; }

        [Option(longName: OptionNames.PathsFrom,
            HelpText = "Read the list of paths to search from a file. Paths should be separated by newlines.",
            MetaValue = MetaValues.FilePath)]
        public string PathsFrom { get; set; }

        [Option(longName: OptionNames.Progress,
            HelpText = "Display dot (.) for every hundredth searched file or directory.")]
        public bool Progress { get; set; }

        [Option(shortName: OptionShortNames.Sort, longName: OptionNames.Sort,
            HelpText = "Sort matched files and directories.",
            MetaValue = MetaValues.SortOptions)]
        public IEnumerable<string> Sort { get; set; }

        public bool TryParse(FileSystemCommandOptions options)
        {
            var baseOptions = (CommonRegexCommandOptions)options;

            if (!TryParse(baseOptions))
                return false;

            options = (FileSystemCommandOptions)baseOptions;

            if (!TryParsePaths(out ImmutableArray<PathInfo> paths))
                return false;

            if (!TryParseAsEnumFlags(Attributes, OptionNames.Attributes, out FileSystemAttributes attributes, provider: OptionValueProviders.FileSystemAttributesProvider))
                return false;

            if (!TryParseAsEnumFlags(AttributesToSkip, OptionNames.AttributesToSkip, out FileSystemAttributes attributesToSkip, provider: OptionValueProviders.FileSystemAttributesToSkipProvider))
                return false;

            if (!TryParseEncoding(Encoding, out Encoding defaultEncoding, EncodingHelpers.UTF8NoBom))
                return false;

            if (!TryParseSortOptions(Sort, OptionNames.Sort, out SortOptions sortOptions))
                return false;

            if (!FilterParser.TryParse(IncludeDirectory, OptionNames.IncludeDirectory, OptionValueProviders.PatternOptionsProvider, out Filter directoryFilter, allowNull: true))
                return false;

            if (!FilterParser.TryParse(
                Extension,
                OptionNames.Extension,
                OptionValueProviders.ExtensionOptionsProvider,
                out Filter extensionFilter,
                allowNull: true,
                defaultNamePart: NamePartKind.Extension,
                includedPatternOptions: PatternOptions.List | PatternOptions.Equals | PatternOptions.IgnoreCase))
            {
                return false;
            }

            if (!TryParseFileProperties(
                FileProperties,
                OptionNames.Properties,
                out FilePropertyFilter filePropertyFilter))
            {
                return false;
            }

            if ((attributes & FileSystemAttributes.Empty) != 0)
            {
                if ((attributesToSkip & FileSystemAttributes.Empty) != 0)
                {
                    Logger.WriteError($"Value '{OptionValueProviders.FileSystemAttributesProvider.GetValue(nameof(FileSystemAttributes.Empty)).HelpValue}' cannot be specified both for '{OptionNames.GetHelpText(OptionNames.Attributes)}' and '{OptionNames.GetHelpText(OptionNames.AttributesToSkip)}'.");
                    return false;
                }

                options.Empty = true;
            }
            else if ((attributesToSkip & FileSystemAttributes.Empty) != 0)
            {
                options.Empty = false;
            }

            options.Paths = paths;
            options.DirectoryFilter = directoryFilter;
            options.ExtensionFilter = extensionFilter;
            options.Attributes = GetFileAttributes(attributes);
            options.AttributesToSkip = GetFileAttributes(attributesToSkip);
            options.RecurseSubdirectories = !NoRecurse;
            options.Progress = Progress;
            options.DefaultEncoding = defaultEncoding;
            options.SortOptions = sortOptions;
            options.FilePropertyFilter = filePropertyFilter;

            FileSystemAttributes = attributes;

            return true;
        }

        protected virtual bool TryParsePaths(out ImmutableArray<PathInfo> paths)
        {
            paths = ImmutableArray<PathInfo>.Empty;

            if (Path.Any()
                && !TryEnsureFullPath(Path, PathOrigin.Argument, out paths))
            {
                return false;
            }

            ImmutableArray<PathInfo> pathsFromFile = ImmutableArray<PathInfo>.Empty;

            if (PathsFrom != null)
            {
                if (!FileSystemHelpers.TryReadAllText(PathsFrom, out string content))
                    return false;

                IEnumerable<string> lines = TextHelpers.ReadLines(content).Where(f => !string.IsNullOrWhiteSpace(f));

                if (!TryEnsureFullPath(lines, PathOrigin.File, out pathsFromFile))
                    return false;

                paths = paths.AddRange(pathsFromFile);
            }

            if (Console.IsInputRedirected)
            {
                ImmutableArray<PathInfo> pathsFromInput = ConsoleHelpers.ReadRedirectedInputAsLines()
                   .Where(f => !string.IsNullOrEmpty(f))
                   .Select(f => new PathInfo(f, PathOrigin.RedirectedInput))
                   .ToImmutableArray();

                paths = paths.AddRange(pathsFromInput);
            }

            if (paths.IsEmpty)
                paths = ImmutableArray.Create(new PathInfo(Environment.CurrentDirectory, PathOrigin.CurrentDirectory));

            return true;
        }

        private static bool TryEnsureFullPath(IEnumerable<string> paths, PathOrigin kind, out ImmutableArray<PathInfo> fullPaths)
        {
            ImmutableArray<PathInfo>.Builder builder = ImmutableArray.CreateBuilder<PathInfo>();

            foreach (string path in paths)
            {
                if (!ParseHelpers.TryEnsureFullPath(path, out string fullPath))
                    return false;

                builder.Add(new PathInfo(fullPath, kind));
            }

            fullPaths = builder.ToImmutableArray();
            return true;
        }

        private FileAttributes GetFileAttributes(FileSystemAttributes attributes)
        {
            FileAttributes fileAttributes = 0;

            if ((attributes & FileSystemAttributes.ReadOnly) != 0)
                fileAttributes |= FileAttributes.ReadOnly;

            if ((attributes & FileSystemAttributes.Hidden) != 0)
                fileAttributes |= FileAttributes.Hidden;

            if ((attributes & FileSystemAttributes.System) != 0)
                fileAttributes |= FileAttributes.System;

            if ((attributes & FileSystemAttributes.Archive) != 0)
                fileAttributes |= FileAttributes.Archive;

            if ((attributes & FileSystemAttributes.Device) != 0)
                fileAttributes |= FileAttributes.Device;

            if ((attributes & FileSystemAttributes.Normal) != 0)
                fileAttributes |= FileAttributes.Normal;

            if ((attributes & FileSystemAttributes.Temporary) != 0)
                fileAttributes |= FileAttributes.Temporary;

            if ((attributes & FileSystemAttributes.SparseFile) != 0)
                fileAttributes |= FileAttributes.SparseFile;

            if ((attributes & FileSystemAttributes.ReparsePoint) != 0)
                fileAttributes |= FileAttributes.ReparsePoint;

            if ((attributes & FileSystemAttributes.Compressed) != 0)
                fileAttributes |= FileAttributes.Compressed;

            if ((attributes & FileSystemAttributes.Offline) != 0)
                fileAttributes |= FileAttributes.Offline;

            if ((attributes & FileSystemAttributes.NotContentIndexed) != 0)
                fileAttributes |= FileAttributes.NotContentIndexed;

            if ((attributes & FileSystemAttributes.Encrypted) != 0)
                fileAttributes |= FileAttributes.Encrypted;

            if ((attributes & FileSystemAttributes.IntegrityStream) != 0)
                fileAttributes |= FileAttributes.IntegrityStream;

            if ((attributes & FileSystemAttributes.NoScrubData) != 0)
                fileAttributes |= FileAttributes.NoScrubData;

            return fileAttributes;
        }

        protected SearchTarget GetSearchTarget()
        {
            if ((FileSystemAttributes & FileSystemAttributes.File) != 0)
            {
                return ((FileSystemAttributes & FileSystemAttributes.Directory) != 0)
                    ? SearchTarget.All
                    : SearchTarget.Files;
            }

            if ((FileSystemAttributes & FileSystemAttributes.Directory) != 0)
                return SearchTarget.Directories;

            return SearchTarget.Files;
        }
    }
}
