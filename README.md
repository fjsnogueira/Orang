# Orang <img align="left" src="images/icon48.png">

Orang is a cross-platform command-line tool for:

* [searching](docs/cli/find-command.md) files, directories and files' content,
* [replacing](docs/cli/replace-command.md) files' content,
* [renaming](docs/cli/rename-command.md) files and directories,
* [deleting](docs/cli/delete-command.md) files, directories or its content,
* executing [Regex](https://docs.microsoft.com/cs-cz/dotnet/api/system.text.regularexpressions.regex?view=netcore-2.2) functions such as [match](docs/cli/match-command.md) or [split](docs/cli/split-command.md)

All these commands are powered with [.NET regular expression engine](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions).

## How to install

Orang is distributed as a [.NET Core global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). To install Orang run:

```
dotnet tool install -g orang.dotnet.cli
```

To install non-alpha version run:

```
dotnet tool install -g orang.dotnet.cli --version 0.1.0-beta
```

To update Orang run:

```
dotnet tool update -g orang.dotnet.cli
```

Note: Orang requires [.NET Core Runtime](https://dotnet.microsoft.com/download) 2.1 or higher.

## How to Use

```
orang [command] [parameters]
```

## Basic Commands

* [delete](docs/cli/delete-command.md)
* [find](docs/cli/find-command.md)
* [match](docs/cli/match-command.md)
* [rename](docs/cli/rename-command.md)
* [replace](docs/cli/replace-command.md)
* [split](docs/cli/split-command.md)

For a list of all commands please see [Orang Command-Line Reference](docs/cli/README.md)

## How to Learn

For a full list of commands, parameters and parameter values run:

```
orang help [command] [-v]
```

For a full manual run:

```
orang help -m [-v]
```

For a full list of .NET regular expressions syntax run:

```
orang list-syntax
```

## Features

### Dry Run

The option `-d, --dry-run` gives you opportunity to see the results before you actually replace, rename or delete anything.

### Match and Replacement Side-by-Side

The option `-t, --highlight` with values `m[atch] r[eplacement]` gives you opportunity to see the match and the replacement side-by-side in the output.

### Evaluator

The option '--evaluator' gives you opportunity to precisely define how the replacement string is computed.
Evaluator is a path to a library (dll file) which contains method that has signature compatible with [MatchEvaluator](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.matchevaluator) i.e. `string M(Match match)`.
Expected format is 'MyLib.dll,MyNamespace.MyClass.MyMethod'.

### Load Pattern From a File

The more complicated a pattern is, the less readable it becomes when written in one line.

```
orang find --content "(?x)(?<=(\A|\.)\s*)\p{Ll}\w+\b"
```

The option `f[rom-file]` gives you opportunity to store pattern in a file where it can be formatted.

```
orang find --content "pattern.txt" from-file
```
or
```
orang find -c "pattern.txt" f
```

Note: Replacement string can be store in a file as well.

### Sample Command

Goal: Capitalize first character of a word at the beginning of the text or at the beginning of a sentence.

Folder `C:/Lib` contains library `Evaluator.dll` which contains method `N.C.M`.

```csharp
using System.Text.RegularExpressions;

namespace N
{
    public static class C
    {
        public static string M(Match match)
        {
            return char.ToUpper(match.Value[0]) + match.Value.Substring(1);
        }
    }
}
```

File `pattern.txt` has following content:

```
(?x)      # set multiline option
(?<=      # is preceded with
  (\A|\.) # beginning of text or a dot
  \s*     # zero or more white-space characters
)
\p{Ll}    # lowercase letter
\w+       # one or more word characters
\b        # word boundary (between word and non-word character)
```

```
orang replace ^
 --extension txt ^
 --content "pattern.txt" from-file ^
 --evaluator "C:/Lib/Evaluator.dll,N.C.M" ^
 --highlight match replacement ^
 --display path=omit summary ^
 --dry-run
```
or
```
orang replace -e txt -c "pattern.txt" f --evaluator "C:/Lib/Evaluator.dll,N.C.M" -t m r -y p=o su -d
```

![Capitalize first character in a sentence](/images/CapitalizeFirstCharInSentence.png)

## Links

* [Regular Expression Language - Quick Reference](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference)
* [.NET Regular Expressions](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions)
* [Regular Expression Options](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options)
