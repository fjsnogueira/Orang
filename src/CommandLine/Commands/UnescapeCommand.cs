// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class UnescapeCommand : AbstractCommand
    {
        public UnescapeCommand(UnescapeCommandOptions options)
        {
            Options = options;
        }

        public UnescapeCommandOptions Options { get; }

        protected override CommandResult ExecuteCore(CancellationToken cancellationToken = default)
        {
            string unescaped = Regex.Unescape(Options.Input);
            WriteLine(unescaped);

            return CommandResult.Success;
        }
    }
}
