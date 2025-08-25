using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.Definitions;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
    internal static class ExpressionNode
    {
        /// <summary>
        /// Reads an expression from the code.
        /// </summary>
        /// <param name="p">Read parameters/context</param>
        /// <param name="refDataType">The expected data type that will fit this expression.</param>
        /// <param name="leftOperatorPrecedence">The precedence of the operator to the left.</param>
        /// <param name="errorIfNothingFound">If true but nothing could be parsed, then an UnknownNode will be returned with an error.</param>
        /// <returns></returns>
        public static Node Read(ReadParams p, DataType refDataType, int leftOperatorPrecedence = 0, bool errorIfNothingFound = false)
        {
            var code = p.Code;
            Node node = null;
            var resetPos = p.Code.Position;

            if (code.ReadWord())
            {
                node = ReadWord(p, refDataType, overrideWord: null, overrideWordSpan: null);
            }
            else if (code.ReadNumber())
            {
                node = new NumberNode(p.Statement, code.Span, code.Text);
            }
            else if (code.ReadStringLiteral())
            {
                if (code.Text.StartsWith("'"))
                {
                    node = new CharLiteralNode(p.Statement, code.Span, CodeParser.StringLiteralToString(code.Text));
                }
                else
                {
                    node = new StringLiteralNode(p.Statement, code.Span, CodeParser.StringLiteralToString(code.Text));
                }
            }
            else if (code.ReadExact('('))
            {
                var openBracketSpan = code.Span;
                var dataType = DataType.TryParse(new DataType.ParseArgs(code, p.AppSettings)
                {
                    Flags = DataType.ParseFlag.Strict,
                    DataTypeCallback = name =>
                    {
                        return p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.
                            GetAny<DataTypeDefinition>(openBracketSpan.Start + p.FuncOffset, name).FirstOrDefault();
                    },
                    VariableCallback = name =>
                    {
                        return p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.
                            GetAny<VariableDefinition>(openBracketSpan.Start + p.FuncOffset, name).FirstOrDefault();
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
                    var castSpan = new CodeSpan(openBracketSpan.Start, code.Span.End);
                    var castExp = Read(p, dataType, leftOperatorPrecedence);
                    if (castExp != null)
                    {
                        node = new CastNode(p.Statement, castSpan, dataType, castExp);
                    }
                    else
                    {
                        return new UnknownNode(p.Statement, castSpan, $"({dataType.Source})");
                    }
                }
                else
                {
                    node = BracketsNode.Read(p, openBracketSpan, refDataType);
                    if (node == null) return new UnknownNode(p.Statement, openBracketSpan, "(");
                }
            }
            else if (code.ReadExact('$'))
            {
                var dollarSpan = code.Span;
                if (code.ReadWord())
                {
                    var wordSpan = code.Span.Envelope(dollarSpan);
                    node = ReadWord(p, refDataType: null, overrideWord: $"${code.Text}", overrideWordSpan: wordSpan);
                }
                else
                {
                    code.Position = resetPos;
                }
            }

            if (node != null)
            {
                while (!code.EndOfFile)
                {
                    if (TryReadOperator(p, refDataType, leftOperatorPrecedence, node, out var operatorNode))
                    {
                        node = operatorNode;
                    }
                    else break;
                }

                return node;
            }

            // If we got here, then something is still ahead, but couldn't be anticipated.
            // This is probably a syntax error.
            if (errorIfNothingFound && code.Read())
            {
                switch (code.Text)
                {
                    case ")":
                    case "]":
                    case "}":
                        return new UnknownNode(p.Statement, code.Span, code.Text, CAError.CA10076, code.Text);  // Unmatched '{0}'.
                    default:
                        return new UnknownNode(p.Statement, code.Span, code.Text);
                }
            }

            return node;
        }

        private static bool TryReadOperator(ReadParams p, DataType refDataType, int leftOperatorPrecedence,
            Node leftNode, out Node operatorNodeOut)
        {
            var code = p.Code;
            var resetPos = code.Position;

            if (code.ReadExact("==", "!=", "<=", ">=", "*=", "/=", "%=", "+=", "-=",
                "=", "*", "/", "%", "+", "-", "<", ">", "&&", "||") ||
                code.ReadExactWholeWord("like", "and", "or"))
            {
                var opSpan = code.Span;
                var opType = OperatorNode.OperatorTextToType(code.Text);
                var precedence = OperatorNode.OperatorPrecedence(opType);
                if (precedence >= leftOperatorPrecedence)
                {
                    var rightNode = Read(p, leftNode.DataType ?? refDataType, precedence);
                    if (rightNode != null)
                    {
                        operatorNodeOut = new OperatorNode(p.Statement, leftNode.Span.Envelope(rightNode.Span), opType, leftNode, rightNode);
                        return true;
                    }
                }
            }
            else if (code.ReadExact('?'))
            {
                var opSpan = code.Span;
                if (OperatorNode.TernaryPrecedence >= leftOperatorPrecedence)
                {
                    var conditionalNode = ConditionalNode.Read(p, refDataType, opSpan, leftNode);
                    if (conditionalNode != null)
                    {
                        operatorNodeOut = conditionalNode;
                        return true;
                    }
                }
            }
            else if (code.ReadExactWholeWord("in"))
            {
                var inNode = InNode.Read(p, code.Span, leftNode);
                if (inNode != null)
                {
                    operatorNodeOut = inNode;
                    return true;
                }
            }

            code.Position = resetPos;
            operatorNodeOut = null;
            return false;
        }

        private static Node ReadWord(ReadParams p, DataType refDataType, string overrideWord = null, CodeSpan? overrideWordSpan = null)
        {
            var code = p.Code;
            var word = overrideWord ?? code.Text;
            var wordSpan = overrideWordSpan ?? code.Span;

            if (code.ReadExact('('))
            {
                // This is a function call
                var openBracketSpan = code.Span;

                switch (word)
                {
                    case "avg":
                    case "count":
                    case "sum":
                    case "max":
                    case "min":
                        return AggregateFunctionCallNode.Read(p, wordSpan, word);
                }

                return FunctionCallNode.Read(p, wordSpan, word, funcDefs: null, openBracketSpan);
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
                        var openBracketSpan = code.Span;

                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsChild && !d.NotGlobal
                                                   select d))
                        {
                            var childDefs = parentDef.GetChildDefinitions(p.AppSettings)
                                .Where(c => c.Name == childWord && c.ArgumentsRequired).ToList();
                            if (childDefs.Count > 0)
                            {
                                var parentNode = new IdentifierNode(p.Statement, wordSpan, word, parentDef);
                                var childNode = FunctionCallNode.Read(p, combinedSpan, combinedWord, childDefs, openBracketSpan);
                                return new ParentChildNode(parentNode, childNode);
                            }
                        }

                        p.CodeAnalyzer.ReportError(combinedSpan, CAError.CA10003, combinedWord);	// Function '{0}' not found.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                    else // No opening bracket
                    {
                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsChild
                                                   select d))
                        {
                            var childDef = parentDef.GetChildDefinitions(childWord, p.AppSettings).FirstOrDefault(c => !c.ArgumentsRequired);
                            if (childDef != null)
                            {
                                var parentNode = new IdentifierNode(p.Statement, wordSpan, word, parentDef);
                                var childNode = TryReadSubscript(p, combinedSpan, combinedWord, childDef);
                                return new ParentChildNode(parentNode, childNode);
                            }
                        }

                        p.CodeAnalyzer.ReportError(combinedSpan, CAError.CA10001, combinedWord);	// Unknown '{0}'.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                }
                else // No word after dot
                {
                    p.CodeAnalyzer.ReportError(dotSpan, CAError.CA10004);	// Expected identifier to follow '.'
                    return new UnknownNode(p.Statement, wordSpan.Envelope(dotSpan), string.Concat(word, "."));
                }
            }
            else if (code.ReadExact("$$"))
            {
                var dollarSpan = code.Span;

                if (code.ReadWord())
                {
                    var childWord = code.Text;
                    var combinedWord = string.Concat(word, "$$", childWord);
                    var combinedSpan = wordSpan.Envelope(code.Span);

                    // Double-dollar does not currently have any methods available.

                    foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                where d.AllowsDoubleDollarChild
                                                select d))
                    {
                        var childDef = parentDef.GetDoubleDollarChildDefinitions(childWord, p.AppSettings).FirstOrDefault(c => !c.ArgumentsRequired);
                        if (childDef != null)
                        {
                            var parentNode = new IdentifierNode(p.Statement, wordSpan, word, parentDef);
                            var childNode = TryReadSubscript(p, combinedSpan, combinedWord, childDef);
                            return new ParentChildNode(parentNode, childNode);
                        }
                    }

                    p.CodeAnalyzer.ReportError(combinedSpan, CAError.CA10001, combinedWord);	// Unknown '{0}'.
                    return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                }
                else // No word after double-dollar
                {
                    p.CodeAnalyzer.ReportError(dollarSpan, CAError.CA10005);	// Expected identifier to follow '$'.
                    return new UnknownNode(p.Statement, wordSpan.Envelope(dollarSpan), string.Concat(word, "$$"));
                }
            }
            else if (code.ReadExact('$'))
            {
                var dollarSpan = code.Span;

                if (code.ReadWord())
                {
                    var childWord = code.Text;
                    var combinedWord = string.Concat(word, "$", childWord);
                    var combinedSpan = wordSpan.Envelope(code.Span);

                    if (code.ReadExact('('))
                    {
                        var openBracketSpan = code.Span;

                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsDollarChild && !d.NotGlobal
                                                   select d))
                        {
                            var childDefs = parentDef.GetDollarChildDefinitions(p.AppSettings)
                                .Where(c => c.Name == childWord && c.ArgumentsRequired).ToList();
                            if (childDefs != null)
                            {
                                var parentNode = new IdentifierNode(p.Statement, wordSpan, word, parentDef);
                                var childNode = FunctionCallNode.Read(p, combinedSpan, combinedWord, childDefs, openBracketSpan);
                                return new ParentChildNode(parentNode, childNode);
                            }
                        }

                        p.CodeAnalyzer.ReportError(combinedSpan, CAError.CA10003, combinedWord);	// Function '{0}' not found.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                    else // No opening bracket
                    {
                        foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
                                                   where d.AllowsDollarChild
                                                   select d))
                        {
                            var childDef = parentDef.GetDollarChildDefinitions(childWord, p.AppSettings).FirstOrDefault(c => !c.ArgumentsRequired);
                            if (childDef != null)
                            {
                                var parentNode = new IdentifierNode(p.Statement, wordSpan, word, parentDef);
                                var childNode = TryReadSubscript(p, combinedSpan, combinedWord, childDef);
                                return new ParentChildNode(parentNode, childNode);
                            }
                        }

                        p.CodeAnalyzer.ReportError(combinedSpan, CAError.CA10001, combinedWord);	// Unknown '{0}'.
                        return new UnknownNode(p.Statement, combinedSpan, combinedWord);
                    }
                }
                else // No word after dollar
                {
                    p.CodeAnalyzer.ReportError(dollarSpan, CAError.CA10005);	// Expected identifier to follow '$'.
                    return new UnknownNode(p.Statement, wordSpan.Envelope(dollarSpan), string.Concat(word, "$"));
                }
            }

            // Try to read array accessor
            if (code.PeekExact('['))
            {
                // Read a list of array accessors with a single expression
                var arrayResetPos = code.TokenStartPostion;
                var arrayExps = new List<Node[]>();
                var lastArrayStartPos = code.Position;
                while (!code.EndOfFile)
                {
                    lastArrayStartPos = code.Position;
                    if (code.ReadExact('['))
                    {
                        var exp1 = ExpressionNode.Read(p, refDataType: null);
                        if (exp1 != null)
                        {
                            if (code.ReadExact(']'))
                            {
                                // Brackets with single expression
                                arrayExps.Add(new Node[] { exp1 });
                            }
                            else if (code.ReadExact(','))
                            {
                                var exp2 = ExpressionNode.Read(p, refDataType: null);
                                if (exp2 != null)
                                {
                                    if (code.ReadExact(']'))
                                    {
                                        arrayExps.Add(new Node[] { exp1, exp2 });
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
                            var varArrayLengths = vardef.ArrayLengths;
                            if (varArrayLengths == null) continue;

                            if (varArrayLengths.Length == arrayExps.Count - 1 &&
                                vardef.DataType != null &&
                                vardef.DataType.AllowsSubscript &&
                                arrayExps.Take(varArrayLengths.Length).All(x => x.Length == 1))
                            {
                                // Last array accessor is a string subscript
                                return new IdentifierNode(p.Statement, wordSpan, word, def,
                                    arrayAccessExps: arrayExps.Take(arrayExps.Count - 1).Select(x => x[0]).ToArray(),
                                    subscriptAccessExps: arrayExps.Last());
                            }
                            else if (arrayExps.All(x => x.Length == 1))
                            {
                                return new IdentifierNode(p.Statement, wordSpan, word, def,
                                    arrayAccessExps: arrayExps.Select(x => x[0]).ToArray(),
                                    subscriptAccessExps: null);
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

        private static IdentifierNode TryReadSubscript(ReadParams p, CodeSpan nameSpan, string name, Definition def)
        {
            if (def.DataType == null || def.DataType.AllowsSubscript == false)
            {
                return new IdentifierNode(p.Statement, nameSpan, name, def);
            }

            var code = p.Code;
            var resetPos = code.Position;

            if (code.ReadExact('['))
            {
                var exp1 = ExpressionNode.Read(p, refDataType: null);
                if (exp1 != null)
                {
                    if (code.ReadExact(','))
                    {
                        var exp2 = ExpressionNode.Read(p, refDataType: null);
                        if (exp2 != null)
                        {
                            if (code.ReadExact(']'))
                            {
                                return new IdentifierNode(p.Statement, nameSpan, name, def,
                                    subscriptAccessExps: new Node[] { exp1, exp2 });
                            }
                        }
                    }
                    else if (code.ReadExact(']'))
                    {
                        return new IdentifierNode(p.Statement, nameSpan, name, def,
                            subscriptAccessExps: new Node[] { exp1 });
                    }
                }
            }

            // No match; reset back to before the array accessors started
            code.Position = resetPos;

            return new IdentifierNode(p.Statement, nameSpan, name, def);
        }
    }
}
