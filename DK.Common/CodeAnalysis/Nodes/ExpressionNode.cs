using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.Definitions;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
    class ExpressionNode : GroupNode
    {
        public ExpressionNode(Statement stmt)
            : base(stmt, null)
        {
        }

        public override int Precedence => 0;

        private static readonly string[] InOperatorDelimStrings = new string[] { "," };

        public static ExpressionNode Read(ReadParams p, DataType refDataType, params string[] stopStrings)
        {
            return Read(p, refDataType, false, stopStrings);
        }

        private static bool CheckForStopStrings(ReadParams p, string[] stopStrings)
        {
            if (stopStrings != null)
            {
                foreach (var str in stopStrings)
                {
                    if (str.IsWord())
                    {
                        if (p.Code.PeekExactWholeWord(str)) return true;
                    }
                    else
                    {
                        if (p.Code.PeekExact(str)) return true;
                    }
                }
            }

            return false;
        }

        public static ExpressionNode Read(ReadParams p, DataType refDataType, bool stayOnSameLine, params string[] stopStrings)
        {
            ExpressionNode exp = null;
            var code = p.Code;
            var lastPos = code.Position;
            var parseDataType = refDataType;

            while (!code.EndOfFile)
            {
                switch (code.PeekChar())
                {
                    case ';':
                    case '{':
                    case '}':
                        return exp;
                }

                if (CheckForStopStrings(p, stopStrings)) return exp;

                if (!code.Read()) break;

                if (stayOnSameLine)
                {
                    if (code.PositionsAreOnDifferentLines(lastPos, code.TokenStartPostion))
                    {
                        code.Position = code.TokenStartPostion;
                        break;
                    }
                    lastPos = code.Position;
                }

                if (exp == null) exp = new ExpressionNode(p.Statement);

                switch (code.Type)
                {
                    case CodeType.Number:
                        exp.AddChild(new NumberNode(p.Statement, code.Span, code.Text));
                        break;
                    case CodeType.StringLiteral:
                        if (code.Text.StartsWith("'"))
                        {
                            exp.AddChild(new CharLiteralNode(p.Statement, code.Span, CodeParser.StringLiteralToString(code.Text)));
                        }
                        else
                        {
                            exp.AddChild(new StringLiteralNode(p.Statement, code.Span, CodeParser.StringLiteralToString(code.Text)));
                        }
                        break;
                    case CodeType.Word:
                        exp.AddChild(exp.ReadWord(p, parseDataType));
                        break;
                    case CodeType.Operator:
                        switch (code.Text)
                        {
                            case "(":
                                {
                                    var opText = code.Text;
                                    var startPos = code.Span.Start;
                                    var resumePos = code.Position;
                                    var dataType = DataType.TryParse(new DataType.ParseArgs(code, p.AppSettings)
                                    {
                                        Flags = DataType.ParseFlag.Strict,
                                        DataTypeCallback = name =>
                                            {
                                                return p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.
                                                    GetAny<DataTypeDefinition>(startPos + p.FuncOffset, name).FirstOrDefault();
                                            },
                                        VariableCallback = name =>
                                            {
                                                return p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.
                                                    GetAny<VariableDefinition>(startPos + p.FuncOffset, name).FirstOrDefault();
                                            },
                                        TableFieldCallback = (tableName, fieldName) =>
                                            {
                                                foreach (var tableDef in p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromFile(tableName))
                                                {
                                                    if (tableDef.AllowsChild)
                                                    {
                                                        foreach (var fieldDef in tableDef.GetChildDefinitions(fieldName, p.AppSettings))
                                                        {
                                                            return new Definition[] { tableDef, fieldDef };
                                                        }
                                                    }
                                                }

                                                return null;
                                            },
                                        VisibleModel = false
                                    });
                                    if (dataType != null && code.ReadExact(')'))
                                    {
                                        // This is a cast
                                        var span = new CodeSpan(startPos, code.Span.End);
                                        exp.AddChild(new CastNode(p.Statement, span, dataType, ExpressionNode.Read(p, dataType, stayOnSameLine, stopStrings)));
                                    }
                                    else
                                    {
                                        code.Position = resumePos;
                                        exp.AddChild(exp.ReadNestable(p, parseDataType, code.Span, opText, null));
                                    }
                                }
                                break;
                            //case "[":
                            //	exp.AddChild(exp.ReadNestable(p, code.Span, code.Text, stopStrings));
                            //	break;
                            case "-":
                                {
                                    var lastNode = exp.LastChild;
                                    if (lastNode == null || lastNode is OperatorNode) exp.AddChild(new OperatorNode(p.Statement, code.Span, code.Text, SpecialOperator.UnaryMinus));
                                    else exp.AddChild(new OperatorNode(p.Statement, code.Span, code.Text, null));
                                }
                                break;
                            case "?":
                                parseDataType = refDataType;
                                exp.AddChild(ConditionalNode.Read(p, parseDataType, code.Span, stopStrings));
                                break;
                            case "=":
                                {
                                    var rDataType = exp.NumChildren > 0 ? exp.LastChild.DataType : null;
                                    exp.AddChild(new OperatorNode(p.Statement, code.Span, code.Text, null));
                                    var rExp = ExpressionNode.Read(p, rDataType, stopStrings);
                                    if (rExp != null) exp.AddChild(rExp);
                                }
                                break;
                            case "==":
                            case "!=":
                            case "<":
                            case "<=":
                            case ">":
                            case ">=":
                                {
                                    if (exp.NumChildren > 0)
                                    {
                                        var dt = exp.LastChild.DataType;
                                        if (dt != null) parseDataType = dt;
                                    }
                                    exp.AddChild(new OperatorNode(p.Statement, code.Span, code.Text, null));
                                }
                                break;
                            case "in":
                                {
                                    exp.AddChild(new OperatorNode(p.Statement, code.Span, "in", null));

                                    if (code.ReadExact('('))
                                    {
                                        var brackets = new BracketsNode(p.Statement, code.Span);
                                        exp.AddChild(brackets);
                                        var errorSpan = code.Span;
                                        var expectComma = false;
                                        var gotItem = false;

                                        while (!code.EndOfFile)
                                        {
                                            if (code.ReadExact(')'))
                                            {
                                                if (!expectComma && gotItem)
                                                {
                                                    if (exp.ErrorReported == null) exp.ReportError(errorSpan, CAError.CA0132);   // Expected expression.
                                                }
                                                break;
                                            }

                                            if (expectComma)
                                            {
                                                if (code.ReadExact(','))
                                                {
                                                    expectComma = false;
                                                    errorSpan = code.Span;
                                                }
                                                else
                                                {
                                                    if (exp.ErrorReported == null) exp.ReportError(errorSpan, CAError.CA0131);  // Expected ','.
                                                }
                                            }
                                            else
                                            {
                                                if (CheckForStopStrings(p, stopStrings)) return exp;
                                                var rDataType = exp.NumChildren > 0 ? exp.LastChild.DataType : null;
                                                var delimStopStrings = stopStrings != null ? stopStrings.Concat(InOperatorDelimStrings).ToArray() : InOperatorDelimStrings;
                                                var itemExp = ExpressionNode.Read(p, refDataType, delimStopStrings);
                                                if (itemExp != null)
                                                {
                                                    brackets.AddChild(itemExp);
                                                    errorSpan = exp.Span;
                                                    gotItem = true;
                                                    expectComma = true;
                                                }
                                                else
                                                {
                                                    if (exp.ErrorReported == null) exp.ReportError(errorSpan, CAError.CA0132);   // Expected expression.
                                                }
                                            }
                                        }

                                        if (!gotItem)
                                        {
                                            if (exp.ErrorReported == null) exp.ReportError(errorSpan, CAError.CA0133);    // 'in' operator requires at least 1 expression.
                                        }
                                    }
                                }
                                break;
                            default:
                                exp.AddChild(new OperatorNode(p.Statement, code.Span, code.Text, null));
                                break;
                        }
                        break;
                    default:
                        exp.ReportError(code.Span, CAError.CA0001, code.Text);	// Unknown '{0}'.
                        exp.AddChild(new UnknownNode(p.Statement, code.Span, code.Text));
                        break;
                }
            }

            return exp;
        }

        private Node ReadWord(ReadParams p, DataType refDataType)
        {
            var code = p.Code;
            var word = code.Text;
            var wordSpan = code.Span;

            if (code.ReadExact('('))
            {
                // This is a function call

                switch (word)
                {
                    case "avg":
                    case "count":
                    case "sum":
                    case "max":
                    case "min":
                        return AggregateFunctionCallNode.Read(p, wordSpan, word);
                }

                return FunctionCallNode.Read(p, wordSpan, word);
            }

            if (code.ReadExact('.'))
            {
                var dotSpan = code.Span;

                if (code.ReadWord())
                {
                    var childWord = code.Text;
                    var combinedWord = string.Concat(word, ".", childWord);
                    var combinedSpan = wordSpan.Envelope(code.Span);

                    if (code.ReadExact('('))
                    {
                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsChild
                                                   select d))
                        {
                            var childDef = parentDef.GetChildDefinitions(p.AppSettings).FirstOrDefault(c => c.Name == childWord && c.ArgumentsRequired);
                            if (childDef != null)
                            {
                                return FunctionCallNode.Read(p, combinedSpan, combinedWord, childDef);
                            }
                        }

                        ReportError(combinedSpan, CAError.CA0003, combinedWord);	// Function '{0}' not found.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                    else // No opening bracket
                    {
                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsChild
                                                   select d))
                        {
                            var childDef = parentDef.GetChildDefinitions(p.AppSettings).FirstOrDefault(c => c.Name == childWord && !c.ArgumentsRequired);
                            if (childDef != null)
                            {
                                return TryReadSubscript(p, combinedSpan, combinedWord, childDef);
                            }
                        }

                        ReportError(combinedSpan, CAError.CA0001, combinedWord);	// Unknown '{0}'.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                }
                else // No word after dot
                {
                    ReportError(dotSpan, CAError.CA0004);	// Expected identifier to follow '.'
                    return new UnknownNode(p.Statement, wordSpan.Envelope(dotSpan), string.Concat(word, "."));
                }
            }

            // Try to read array accessor
            if (code.PeekExact('['))
            {
                // Read a list of array accessors with a single expression
                var arrayResetPos = code.TokenStartPostion;
                var arrayExps = new List<ExpressionNode[]>();
                var lastArrayStartPos = code.Position;
                while (!code.EndOfFile)
                {
                    lastArrayStartPos = code.Position;
                    if (code.ReadExact('['))
                    {
                        var exp1 = ExpressionNode.Read(p, null, "]", ",");
                        if (exp1 != null)
                        {
                            if (code.ReadExact(']'))
                            {
                                // Brackets with single expression
                                arrayExps.Add(new ExpressionNode[] { exp1 });
                            }
                            else if (code.ReadExact(','))
                            {
                                var exp2 = ExpressionNode.Read(p, null, "]");
                                if (exp2 != null)
                                {
                                    if (code.ReadExact(']'))
                                    {
                                        arrayExps.Add(new ExpressionNode[] { exp1, exp2 });
                                    }
                                    else
                                    {
                                        code.Position = lastArrayStartPos;
                                        break;
                                    }
                                }
                                else
                                {
                                    code.Position = lastArrayStartPos;
                                    break;
                                }
                            }
                            else
                            {
                                code.Position = lastArrayStartPos;
                                break;
                            }
                        }
                        else
                        {
                            code.Position = lastArrayStartPos;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                var defs = p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word).ToArray();

                // Try to match to a variable defined as an array
                if (arrayExps.Count > 0)
                {
                    // Check if it's a variable being accessed
                    foreach (var def in defs)
                    {
                        if (def is VariableDefinition)
                        {
                            var vardef = def as VariableDefinition;
                            var arrayLengths = vardef.ArrayLengths;
                            if (arrayLengths == null) continue;

                            if (arrayLengths.Length == arrayExps.Count && arrayExps.All(x => x.Length == 1))
                            {
                                return new IdentifierNode(p.Statement, wordSpan, word, def, (from e in arrayExps select e[0]));
                            }
                            else if (arrayLengths.Length == arrayExps.Count - 1 &&
                                vardef.DataType != null &&
                                vardef.DataType.AllowsSubscript &&
                                arrayExps.Take(arrayLengths.Length).All(x => x.Length == 1))
                            {
                                // Last array accessor is a string subscript
                                return new IdentifierNode(p.Statement, wordSpan, word, def,
                                    (from e in arrayExps.Take(arrayExps.Count - 1) select e[0]),
                                    arrayExps.Last());
                            }
                        }
                    }
                }

                // Try to match to a string that allows a subscript with 1 or 2 arguments
                code.Position = arrayResetPos;
                var subDef = (from d in defs where d.DataType != null && d.DataType.AllowsSubscript select d).FirstOrDefault();
                if (subDef != null)
                {
                    return TryReadSubscript(p, wordSpan, word, subDef);
                }
            }

            if (refDataType != null)
            {
                if (refDataType.HasCompletionOptions)
                {
                    var enumOptDef = refDataType.GetEnumOption(word);
                    if (enumOptDef != null) return new IdentifierNode(p.Statement, wordSpan, word, enumOptDef);
                }

                switch (refDataType.ValueType)
                {
                    case ValType.Table:
                        {
                            var table = p.AppSettings.Dict.GetTable(word);
                            if (table != null) return new IdentifierNode(p.Statement, wordSpan, word, table.Definition);

                            var indrel = p.AppSettings.Dict.GetRelInd(word);
                            if (indrel != null) return new IdentifierNode(p.Statement, wordSpan, word, indrel.Definition);
                        }
                        break;
                    case ValType.IndRel:
                        {
                            var indrel = p.AppSettings.Dict.GetRelInd(word);
                            if (indrel != null) return new IdentifierNode(p.Statement, wordSpan, word, indrel.Definition);
                        }
                        break;
                }
            }

            var wordDefs = (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                            where !d.RequiresChild && !d.ArgumentsRequired && !d.RequiresRefDataType
                            orderby d.SelectionOrder descending
                            select d).ToArray();
            if (wordDefs.Length > 0)
            {
                return new IdentifierNode(p.Statement, wordSpan, word, wordDefs[0]);
            }

            return new UnknownNode(p.Statement, wordSpan, word);

            // Single word. Don't attempt to find the definition now because it could be an enum option.
            //return new IdentifierNode(p.Statement, wordSpan, word, null);
        }

        private Node ReadNestable(ReadParams p, DataType refDataType, CodeSpan openSpan, string text, string[] stopStrings)
        {
            GroupNode groupNode;
            string endText;
            switch (text)
            {
                case "(":
                    groupNode = new BracketsNode(p.Statement, openSpan);
                    endText = ")";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("text");
            }

            if (stopStrings == null) stopStrings = new string[] { endText };
            else stopStrings = stopStrings.Concat(new string[] { endText }).ToArray();

            while (!p.Code.EndOfFile)
            {
                if (p.Code.ReadExact(endText))
                {
                    groupNode.Span = groupNode.Span.Envelope(p.Code.Span);
                    break;
                }

                var exp = ExpressionNode.Read(p, refDataType, stopStrings);
                if (exp == null) break;
                groupNode.AddChild(exp);
            }

            return groupNode;
        }

        private IdentifierNode TryReadSubscript(ReadParams p, CodeSpan nameSpan, string name, Definition def)
        {
            if (def.DataType == null || def.DataType.AllowsSubscript == false)
            {
                return new IdentifierNode(p.Statement, nameSpan, name, def);
            }

            var code = p.Code;
            var resetPos = code.Position;

            if (code.ReadExact('['))
            {
                var exp1 = ExpressionNode.Read(p, null, "]", ",");
                if (exp1 != null)
                {
                    if (code.ReadExact(','))
                    {
                        var exp2 = ExpressionNode.Read(p, null, "]", ",");
                        if (exp2 != null)
                        {
                            if (code.ReadExact(']'))
                            {
                                return new IdentifierNode(p.Statement, nameSpan, name, def,
                                    subscriptAccessExps: new ExpressionNode[] { exp1, exp2 });
                            }
                        }
                    }
                    else if (code.ReadExact(']'))
                    {
                        return new IdentifierNode(p.Statement, nameSpan, name, def,
                            subscriptAccessExps: new ExpressionNode[] { exp1 });
                    }
                }
            }

            // No match; reset back to before the array accessors started
            code.Position = resetPos;

            return new IdentifierNode(p.Statement, nameSpan, name, def);
        }
    }
}
