@echo off
xsd FunctionFileDatabase.xsd /c /n:ProbeTools.FunctionFileScanning.FunctionFileDatabase
if errorlevel 1 (
	echo Error: XSD failed
	goto :eof
)

REM move FunctionFileDatabase.cs FunctionFileScanning\
REM if errorlevel 1 (
	REM echo Error: Failed to move 'FunctionFileDatabase.cs' into the 'FunctionFileScanning' directory.
	REM goto :eof
REM )
