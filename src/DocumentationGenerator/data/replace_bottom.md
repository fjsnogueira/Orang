﻿## Use Redirected Input

Piped output from a previous command will be treated as a list of paths separated by newlines.

## Samples

### Sample

Update version in csproj and vbproj files in a current directory.

Current directory contains file **pattern.txt** with a following content:

```
(?mx)
(?<=
  ^\ *\<Version\>
)
\d+\.\d+\.\d+\.\d+
(?=
  \</Version\>
)
```

#### Syntax

```
orang replace ^
 --extension csproj,vbproj ^
 --content "pattern.txt" from-file ^
 --replacement "1.2.3.0" ^
 --highlight match replacement
```

#### Short Syntax

```
orang replace ^
 -e csproj,vbproj ^
 -c "pattern.txt" f ^
 -r "1.2.3.0" ^
 -t m r
```

### Sample

Remove duplicate words in C# comments from source files in a current directory.

Current directory contains file **pattern.txt** with a following content:

```
(?mx)
(?<=
  ^
  (\ |\t)*
  //[^\r\n]*\b(?<g>\w+)\b
)
\ +
\b\k<g>\b
```

#### Syntax

```
orang replace ^
 --extension cs ^
 --content "pattern.txt" ^
 --include-directory ".git" whole-input negative ^
 --highlight match
```

#### Short Syntax

```
orang replace ^
 -e cs ^
 -c "pattern.txt" ^
 -i ".git" wi e ^
 -t m

```

### Sample

Normalize newline to CR+LF for all files in a current directory.

#### Syntax

```
orang replace ^
 --content "(?<!\r)\n" ^
 --replacement "\r\n" multiline ^
 --verbosity minimal
```

#### Short Syntax

```
orang replace ^
 -c "(?<!\r)\n" ^
 -r "\r\n" m ^
 -v m
```
