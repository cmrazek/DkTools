# DkTools
Visual Studio Extension for WBDK

**Features:**
- Syntax highlighting
- Go to / peek references
- Quick-info on mouse hover
- Find all references
- Statement completion
- Function signature help
- Code snippets
- Automatic brace completion
- Code folding
- Brace matching
- WBDK compiler
- WBDK file / dictionary explorer
- Static code analysis

## Change Log

**Version 1.5.18 Changes:**
- Replaced the old Run form with a new tab in DK Explorer.
- Code analysis will now limit the number of warnings it reports, to cope with scenarios where there are thousands of warnings.
- Fixed a bug where files created via DK Explorer could not be found via the file filter until the next refresh.
- Using Go To Definition on a class method that exists in both cc and sc will now take you to the correct server context, when known.

**Version 1.5.17 Changes:**
- The code modeler/analysis processes will now cancel themselves if you start typing again. This should improve responsiveness.
- Class methods now only shown in statement completion after typing the class name.

**Version 1.5.16 Changes:**
- Class names are no longer forced to lowercase.
- Background scanning database is 'shrunk' less often, improving performance when saving a file.
- Fixed statement completion on class method arguments.

**Version 1.5.15 Changes:**
- FEC and FEC to Visual C commands will no longer error out when the file is not within a source path.
- Renamed 'app data' folder to DkTools (finally).
- Code Analysis will now only report "Unreachable code" once per code path.
- Now recognizes localization for .t files. (merges .t+, .t& files)

**Version 1.5.14 Changes:**
- Removed dependencies on WBDK DLLs.
- Fixed automatic brace completion when pressing enter after '{', and enabled for single-quotes.

**Version 1.5.13 Changes:**
- Added automatic brace completion (can be turned off in options).
- Fixed Peek Definitions not showing in later version of Visual Studio.
- Brace highlighting will now select only one set of braces at a time.
- Background scanning has been completely rewritten, improving speed and reliability.
- Added code analysis for enum option usage.
