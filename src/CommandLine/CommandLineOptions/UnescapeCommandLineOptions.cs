// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;

namespace Orang.CommandLine
{
    [Verb("unescape", HelpText = "Converts any escaped characters in the input string.")]
    internal class UnescapeCommandLineOptions
    {
        [Option(shortName: OptionShortNames.Input, longName: OptionNames.Input,
            Required = true,
            HelpText = "Text to be unescaped.",
            MetaValue = MetaValues.Input)]
        public string Input { get; set; }

        public bool TryParse(ref UnescapeCommandOptions options)
        {
            options.Input = Input;

            return true;
        }
    }
}
