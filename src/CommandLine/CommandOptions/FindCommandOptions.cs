﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.CommandLine
{
    internal class FindCommandOptions : CommonFindCommandOptions
    {
        internal FindCommandOptions()
        {
        }

        public ModifyOptions ModifyOptions { get; internal set; }

        internal override void WriteDiagnostic()
        {
            DiagnosticWriter.WriteCommand(this);
        }
    }
}
