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

**Version 1.5.15 Changes**
- FEC and FEC to Visual C commands will no longer error out when the file is not within a source path.
- Renamed 'app data' folder to DkTools (finally).
- Code Analysis will now only report "Unreachable code" once per code path.

**Version 1.5.14 Changes:**
- Removed dependencies on WBDK DLLs.
- Fixed automatic brace completion when pressing enter after '{', and enabled for single-quotes.

**Version 1.5.13 Changes:**
- Added automatic brace completion (can be turned off in options).
- Fixed Peek Definitions not showing in later version of Visual Studio.
- Brace highlighting will now select only one set of braces at a time.
- Background scanning has been completely rewritten, improving speed and reliability.
- Added code analysis for enum option usage.
