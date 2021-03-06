﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.CommandLine
{
    internal abstract class DeleteOrRenameCommandLineOptions : FileSystemCommandLineOptions
    {
        public bool TryParse(DeleteOrRenameCommandOptions options)
        {
            var baseOptions = (FileSystemCommandOptions)options;

            if (!TryParse(baseOptions))
                return false;

            options = (DeleteOrRenameCommandOptions)baseOptions;

            return true;
        }
    }
}
