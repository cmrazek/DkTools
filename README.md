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

**Version 1.6.7 Changes:**
- Fixed problem with code analysis warnings when calling widthof() with an uninitialized variable.
- Added support for using constants with #warndel and #warnadd.
- Fixed infinite loop in code analysis 'in' operator parsing.

**Version 1.6.6 Changes:**
- Added support for the new 'in' operator.
- Added #SQLWhereClauseCompatibleAttribute and #SQLResultsFilteringAttribute for statement completion.
- Fixed a bug in select statement where calling a function could break parsing of the rest of the statement.

**Version 1.6.5 Changes:**
- Added filterby keyword for select statements.
- Refactored the code that gathers errors/warnings from FEC and code analysis. This should work better with WBDK 10.
- Fixed a bug where commas between columns in a create relationship statement would not parse correctly.
- Using NOINPUT in dictionary will no longer break the table parsing.

**Version 1.6.4 Changes:**
- Fixed a bug in modeling of 'create time relationship' statements which would cause them to consume more of the source than necessary.

**Version 1.6.3 Changes:**
- Function calls to deprecated functions now trigger a code analysis error (CA0120 - suppress with #warndel 990120)<br>
  A function is marked deprecated if the description text contains the word "deprecated".
- Conditional (aka ternary) statements now trigger a code analysis error if they aren't wrapped in brackets.
- Fixed bug where table/function names would sometimes be confused for enum values.

**Version 1.6.2 Changes:**
- Optimizations for quicker smart indenting and signature help.
- Typing a closing '}' will now fix up indenting on the statements inside the scope.

**Version 1.6.1 Changes:**
- Reduced the number of operations that can cause a code model rebuild, improving typing performance.
- No longer report code analysis errors when a valid enum value is used as a string.
- Create File dialog now shows the length of your file name.
- Statement completion for #include will no longer commit when a '.' is typed.
- Insert Tag can now format a file header comment if it can detect it from the line below.

**Version 1.5.20 Changes:**
- Fixed a bug where extern functions referenced in a class would be associated with that class, causing a discrepancy in Find All References.

**Version 1.5.19 Changes:**
- Fixed bug where changing the ACM app in DK Explorer wouldn't update the registry.
- Fixed FEC errors/warnings not always being refreshed when a file is saved.

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
