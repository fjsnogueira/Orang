// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal static class DiagnosticWriter
    {
        public static void WriteHelpCommand(HelpCommandOptions options)
        {
            WriteString("command", options.Command);
            WriteBool("values", options.IncludeValues);
            WriteBool("manual", options.Manual);
        }

        private static void WriteString(string name, string value)
        {
            WriteName(name);
            Write(value);
        }

        private static void WriteBool(string name, bool value)
        {
            WriteName(name);
            Write((value) ? "true" : "false");
        }

        private static void WriteName(string name)
        {
            Write(name);
            Write(": ");
        }
    }
}
