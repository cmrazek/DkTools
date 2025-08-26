using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Schema
{
    internal class InterfaceMapLoader
    {
        DkAppSettings _appSettings;
        CodeParser _code;
        DataType.ParseArgs _dtParse;

        public InterfaceMapLoader(DkAppSettings appSettings, string source)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _code = new CodeParser(source);
        }

        public void Load()
        {
            try
            {
                _dtParse = new DataType.ParseArgs(_code, _appSettings)
                {
                    InterfaceCallback = (name) =>
                    {
                        var intf = _appSettings.Dict.GetInterface(name);
                        if (intf == null) intf = _appSettings.Dict.GetInterfaceByPlatformName(name);
                        return intf;
                    }
                };

                while (!_code.EndOfFile)
                {
                    if (_code.ReadExact("#interface"))
                    {
                        ReadInterface();
                    }
                }
            }
            catch (Exception ex)
            {
                _appSettings.Log.Error(ex, "Exception when parsing interface map.");
            }
        }

        private void ReadInterface()
        {
            var intfName = _code.ReadWordR();
            if (string.IsNullOrEmpty(intfName)) throw new InterfaceMapException(_code, "No interface name found.");

            var intf = _appSettings.Dict.GetInterface(intfName);
            if (intf == null) throw new InterfaceMapException(_code, $"Interface '{intfName}' does not exist.");

            if (!_code.ReadExact('{')) throw new InterfaceMapException(_code, "Expected '{'.");

            while (!_code.EndOfFile && _code.ReadExact('}') == false)
            {
                try
                {
                    var dataType = DataType.TryParse(_dtParse);
                    if (dataType == null) throw new InterfaceMapException(_code, "Expected data type.");

                    var name = _code.ReadWordR();
                    if (string.IsNullOrEmpty(name)) throw new InterfaceMapException(_code, "Expected method or property name.");

                    if (_code.ReadExact('('))
                    {
                        // Method

                        var first = true;
                        var args = new List<ArgumentDescriptor>();

                        while (!_code.EndOfFile && !_code.ReadExact(')'))
                        {
                            if (first) first = false;
                            else
                            {
                                if (!_code.ReadExact(',')) throw new InterfaceMapException(_code, "Expected ','.");
                            }

                            var argDataType = DataType.TryParse(_dtParse);
                            if (argDataType == null) throw new InterfaceMapException(_code, "Expected argument data type.");

                            string argName = null;
                            if (_code.ReadWord()) argName = _code.Text;

                            args.Add(new ArgumentDescriptor(argName, argDataType, PassByMethod.Value));
                        }

                        if (!_code.ReadExact(';')) throw new InterfaceMapException(_code, "Expected ';'.");

                        var sig = new FunctionSignature(
                            isExtern: false,
                            privacy: FunctionPrivacy.Public,
                            returnDataType: dataType,
                            className: intfName,
                            funcName: name,
                            devDesc: null,
                            args: args,
                            serverContext: ServerContext.Neutral,
                            flags: 0);

                        var methodDef = new InterfaceMethodDefinition(intf.Definition, name, sig, dataType);

                        intf.AddMethod(methodDef);
                    }
                    else
                    {
                        // Property

                        var readOnly = _code.ReadExactWholeWord("readonly");
                        if (!_code.ReadExact(';')) throw new InterfaceMapException(_code, "Expected ';'.");

                        var propDef = new InterfacePropertyDefinition(intf.Definition, name, dataType, readOnly);

                        intf.AddProperty(propDef);
                    }
                }
                catch (InterfaceMapException ex)
                {
                    _appSettings.Log.Warning(ex, "Interface map exception.");

                    // Skip forward until the next definition ';' or the end of the interface '}'.
                    while (_code.Read())
                    {
                        if (_code.Type == CodeType.Operator)
                        {
                            if (_code.Text == ";") break;
                            if (_code.Text == "}") return;
                        }
                    }
                }
            }
        }
    }

    internal class InterfaceMapException : Exception
    {
        public InterfaceMapException(CodeParser code, string message) : base(FormatMessage(code, message)) { }

        private static string FormatMessage(CodeParser code, string message)
        {
            code.CalcLineAndPosFromOffset(code.Position, out var lineNumOut, out var linePosOut);
            return $"Line {lineNumOut + 1} Pos {linePosOut + 1}: {message}";
        }
    }
}
