// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Orang.CommandLine;
using static Orang.Logger;

namespace Orang.FileSystem
{
    internal class DiagnosticProgressReporter : ProgressReporter
    {
        public DiagnosticProgressReporter(
            ProgressReportMode consoleReportMode,
            ProgressReportMode fileReportMode,
            CommonFindCommandOptions options,
            string indent)
        {
            ConsoleReportMode = consoleReportMode;
            FileReportMode = fileReportMode;
            Options = options;
            Indent = indent;
        }

        public ProgressReportMode ConsoleReportMode { get; }

        public ProgressReportMode FileReportMode { get; }

        public CommonFindCommandOptions Options { get; }

        public string Indent { get; }

        public override void Report(FileSystemFinderProgress value)
        {
            if (value.Error != null)
            {
                WriteWarning(value.Error, verbosity: Verbosity.Diagnostic);
                return;
            }

            switch (value.Kind)
            {
                case ProgressKind.SearchedDirectory:
                    {
                        SearchedDirectoryCount++;

                        if (ConsoleReportMode == ProgressReportMode.Path)
                        {
                            WritePath(value.Path, value.Kind, Indent);
                        }
                        else if (FileReportMode == ProgressReportMode.Path)
                        {
                            WritePathToFile(value.Path, value.Kind);
                        }

                        break;
                    }
                case ProgressKind.Directory:
                    {
                        DirectoryCount++;
                        WritePath(value.Path, value.Kind);
                        break;
                    }
                case ProgressKind.File:
                    {
                        FileCount++;
                        WritePath(value.Path, value.Kind);
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{value.Kind}'.");
                    }
            }
        }

        private void WritePath(string path, ProgressKind kind)
        {
            if (ConsoleReportMode == ProgressReportMode.Dot)
            {
                if ((FileCount + DirectoryCount) % 100 == 0)
                {
                    ConsoleOut.Write(".", Colors.Path_Progress);
                    ProgressReported = true;
                }

                if (FileReportMode == ProgressReportMode.Path)
                    WritePathToFile(path, kind, Indent);
            }
            else if (ConsoleReportMode == ProgressReportMode.Path)
            {
                WritePath(path, kind, Indent);
            }
            else if (FileReportMode == ProgressReportMode.Path)
            {
                WritePathToFile(path, kind, Indent);
            }
        }

        private void WritePathToFile(string path, ProgressKind kind, string indent = null)
        {
            if (Out.ShouldWrite(Verbosity.Diagnostic))
            {
                Out.Write(indent);
                Out.Write(GetPrefix(kind));
                Out.WriteLine(GetPath(path));
            }
        }

        private void WritePath(string path, ProgressKind kind, string indent = null)
        {
            ReadOnlySpan<char> p = default;

            if (ConsoleOut.ShouldWrite(Verbosity.Diagnostic))
            {
                p = GetPath(path);

                ConsoleOut.Write(indent, Colors.Path_Progress);
                ConsoleOut.Write(GetPrefix(kind), Colors.Path_Progress);
                ConsoleOut.WriteLine(p, Colors.Path_Progress);
            }

            if (Out?.ShouldWrite(Verbosity.Diagnostic) == true)
            {
                Out.Write(indent);
                Out.Write(GetPrefix(kind));
                Out.WriteLine((p.IsEmpty) ? GetPath(path) : p);
            }
        }

        private ReadOnlySpan<char> GetPath(string path)
        {
            return GetPath(path, BaseDirectoryPath, Options.DisplayRelativePath);
        }

        private static ReadOnlySpan<char> GetPath(
            string path,
            string basePath,
            bool relativePath)
        {
            if (string.Equals(path, basePath, FileSystemHelpers.Comparison))
                return (relativePath) ? "." : path;

            if (relativePath
                && basePath != null
                && path.Length > basePath.Length
                && path.StartsWith(basePath, FileSystemHelpers.Comparison))
            {
                int startIndex = basePath.Length;

                if (FileSystemHelpers.IsDirectorySeparator(path[startIndex]))
                    startIndex++;

                return path.AsSpan(startIndex);
            }

            return path;
        }

        private static string GetPrefix(ProgressKind kind)
        {
            return kind switch
            {
                ProgressKind.SearchedDirectory => "[SCAN] ",
                ProgressKind.Directory => "[DIR]  ",
                ProgressKind.File => "[FILE] ",
                _ => throw new InvalidOperationException($"Unknown enum value '{kind}'."),
            };
        }
    }
}
