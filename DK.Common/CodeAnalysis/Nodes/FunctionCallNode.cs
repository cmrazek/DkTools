using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Definitions;
using DK.Modeling;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
    class FunctionCallNode : Node
    {
        private string _name;
        private CodeSpan _funcNameSpan;
        private List<GroupNode> _args = new List<GroupNode>();
        private CodeSpan _argumentSpan;
        private Definition _def;

        public FunctionCallNode(Statement stmt, CodeSpan funcNameSpan, string funcName, Definition funcDef)
            : base(stmt, funcDef != null ? funcDef.DataType : DataType.Void, funcNameSpan)
        {
            _name = funcName;
            _funcNameSpan = funcNameSpan;
            _def = funcDef;
        }

        public override string ToString() => new string[] { _name, "(", _args.Select(a => a.ToString()).Combine(", "), ")" }.Combine();

        private static FunctionCallNode ParseArguments(ReadParams p, CodeSpan funcNameSpan, string funcName,
            IEnumerable<Definition> funcDefs, int argsStartPos)
        {
            switch (funcDefs.Count())
            {
                case 0: return null;
                case 1: return ParseArguments(p, funcNameSpan, funcName, funcDefs.First(), argsStartPos);
            }

            FunctionCallNode bestNode = null;
            float bestScore = 0.0f;
            FunctionCallNode firstNode = null;

            foreach (var funcDef in funcDefs)
            {
                var resetPos = p.Code.Position;
                var funcNode = ParseArguments(p, funcNameSpan, funcName, funcDef, argsStartPos);
                p.Code.Position = resetPos;
                if (funcNode == null) continue;
                var score = funcNode.CalcArgumentMatchScore();
                if (score > bestScore)
                {
                    bestNode = funcNode;
                    bestScore = score;
                }
                if (firstNode == null) firstNode = funcNode;
            }

            if (bestNode != null)
            {
                p.Code.Position = bestNode.Span.End;
                return bestNode;
            }

            p.Code.Position = firstNode.Span.End;
            return firstNode;
        }

        private static FunctionCallNode ParseArguments(ReadParams p, CodeSpan funcNameSpan, string funcName,
            Definition funcDef, int argsStartPos)
        {
            var funcCallNode = new FunctionCallNode(p.Statement, funcNameSpan, funcName, funcDef);
            var code = p.Code;
            var resetPos = code.Position;
            var commaExpected = false;
            var closed = false;
            var argIndex = 0;
            var args = new List<GroupNode>();
            var argDefs = funcDef.Arguments.ToArray();
            var closePos = -1;

            if (code.ReadExact(')'))
            {
                closed = true;
                closePos = code.Span.End;
            }
            else
            {
                while (!code.EndOfFile)
                {
                    if (commaExpected)
                    {
                        if (code.ReadExact(')'))
                        {
                            closed = true;
                            closePos = code.Span.End;
                            break;
                        }
                        if (!code.ReadExact(','))
                        {
                            code.Position = resetPos;
                            return null;
                        }
                        commaExpected = false;
                    }
                    else
                    {
                        var argDef = argDefs != null && argIndex < argDefs.Length ? argDefs[argIndex] : null;

                        var arg = ExpressionNode.Read(p, argDef != null ? argDef.DataType : null, ",", ")");
                        if (arg != null) funcCallNode.AddArgument(arg);
                        commaExpected = true;
                        argIndex++;
                    }
                }
            }

            if (!closed)
            {
                code.Position = resetPos;
                return null;
            }

            funcCallNode._argumentSpan = new CodeSpan(argsStartPos, closePos);

            funcCallNode.Span = new CodeSpan(funcNameSpan.Start, closePos);
            return funcCallNode;
        }

        public static FunctionCallNode Read(ReadParams p, CodeSpan funcNameSpan, string funcName,
            IEnumerable<Definition> funcDefs, int argsStartPos)
        {
            if (funcDefs != null)
            {
                var node = ParseArguments(p, funcNameSpan, funcName, funcDefs, argsStartPos);
                if (node != null)
                {
                    var numArgumentsRequired = node.Definition.Arguments.Count();
                    if (node.NumArguments != numArgumentsRequired)
                    {
                        node.ReportError(node.ArgumentSpan, CAError.CA0121,
                            numArgumentsRequired, node.NumArguments);  // Function requires {0} arguments. ({1} passed)
                    }

                    return node;
                }
            }

            funcDefs = (from d in p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(funcNameSpan.Start, funcName)
                            where d.ArgumentsRequired && !d.RequiresParent(p.CodeAnalyzer.CodeModel.ClassName)
                            select d).ToList();
            foreach (var def in funcDefs)
            {
                var fd = def as FunctionDefinition;
                if (fd == null) continue;

                var node = ParseArguments(p, funcNameSpan, funcName, fd, argsStartPos);
                if (node != null) return node;
            }

            var funcCallNode = new FunctionCallNode(p.Statement, funcNameSpan, funcName, funcDef: null);
            funcCallNode.ReportError(funcNameSpan, CAError.CA0003, funcName);	// Function '{0}' not found.
            return funcCallNode;
        }

        public override bool IsReportable => _def != null && _def.DataType != null && _def.DataType.IsReportable;

        public void AddArgument(GroupNode node)
        {
            _args.Add(node);
        }

        public int NumArguments
        {
            get { return _args.Count; }
        }

        public CodeSpan ArgumentSpan => _argumentSpan;

        public Definition Definition
        {
            get { return _def; }
            set { _def = value; }
        }

        public override DataType DataType
        {
            get
            {
                switch (_name)
                {
                    case "abs":
                    case "max":
                    case "min":
                    case "oldvalue":
                    case "sum":
                        return _args.Count > 0 ? _args[0].DataType : DataType.Void;
                    case "count":
                        return DataType.Int;
                    default:
                        return base.DataType;
                }
            }
        }

        public override void Execute(CAScope scope)
        {
            // Running has the same effect as reading, since DK function cannot return references
            ReadValue(scope);
        }

        public override Value ReadValue(CAScope scope)
        {
            switch (_name)
            {
                case "abs":
                    return Read_abs(scope);
                case "count":
                    return Read_count(scope);
                case "max":
                    return Read_max(scope);
                case "min":
                    return Read_min(scope);
                case "oldvalue":
                    return Read_oldvalue(scope);
                case "sum":
                    return Read_sum(scope);
                case "widthof":
                    return Read_widthof(scope);
            }

            if (_def is FunctionDefinition funcDef)
            {
                if (funcDef.Deprecated)
                {
                    ReportError(_funcNameSpan, CAError.CA0120, funcDef.Signature.Description);
                }
            }

            var defArgs = _def != null ? _def.Arguments.ToArray() : new ArgumentDescriptor[0];
            var argIndex = 0;
            foreach (var arg in _args)
            {
                var defArg = argIndex < defArgs.Length ? defArgs[argIndex] : null;
                if (defArg != null)
                {
                    if (defArg.PassByMethod == PassByMethod.Reference || defArg.PassByMethod == PassByMethod.ReferencePlus)
                    {
                        var readScope = scope.Clone();
                        readScope.SuppressInitializedCheck = true;
                        var argValue = arg.ReadValue(readScope);
                        if (argValue != null && defArg.DataType != null) argValue.CheckTypeConversion(scope, arg.Span, defArg.DataType);
                        scope.Merge(readScope);

                        var writeScope = scope.Clone();
                        arg.WriteValue(writeScope, Value.CreateUnknownFromDataType(defArg.DataType));
                        scope.Merge(writeScope);
                    }
                    else
                    {
                        var argValue = arg.ReadValue(scope);
                        if (argValue != null && defArg.DataType != null) argValue.CheckTypeConversion(scope, arg.Span, defArg.DataType);
                    }
                }
                else
                {
                    arg.ReadValue(scope);
                }
                argIndex++;
            }

            if (_def == null) return Value.Void;
            return Value.CreateUnknownFromDataType(_def.DataType);
        }

        public override bool CanAssignValue(CAScope scope)
        {
            return false;
        }

        public override int Precedence
        {
            get { return 0; }
        }

        private Value Read_oldvalue(CAScope scope)
        {
            if (_args.Count != 1)
            {
                ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
            }

            return Value.CreateUnknownFromDataType(_args[0].ReadValue(scope).DataType);
        }

        private Value Read_abs(CAScope scope)
        {
            if (_args.Count != 1)
            {
                ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
            }

            return _args[0].ReadValue(scope);
            //return Value.CreateUnknownFromDataType(_args[0].DataType);
        }

        private Value Read_count(CAScope scope)
        {
            if (_args.Count < 1 || _args.Count > 2)
            {
                ReportError(_funcNameSpan, CAError.CA0057, "1 or 2");	// Function expects {0} argument(s).
            }

            return Value.CreateUnknownFromDataType(DataType.Int);
        }

        private Value Read_sum(CAScope scope)
        {
            if (_args.Count < 1 || _args.Count > 2)
            {
                ReportError(_funcNameSpan, CAError.CA0057, "1 or 2");	// Function expects {0} argument(s).
            }

            return Value.CreateUnknownFromDataType(DataType.Int);
        }

        private Value Read_max(CAScope scope)
        {
            if (_args.Count != 1)
            {
                ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
            }

            return Value.CreateUnknownFromDataType(_args[0].ReadValue(scope).DataType);
        }

        private Value Read_min(CAScope scope)
        {
            if (_args.Count != 1)
            {
                ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
            }

            return Value.CreateUnknownFromDataType(_args[0].ReadValue(scope).DataType);
        }

        private Value Read_widthof(CAScope scope)
        {
            if (_args.Count != 1)
            {
                ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
            }

            return new NumberValue(DataType.Int, null);
        }

        private float CalcArgumentMatchScore()
        {
            return DataType.CalcArgumentListCompatibility(
                sigArguments: _def.Signature.Arguments,
                passedDataTypes: _args.Select(x => x.DataType).ToList());
        }
    }
}
