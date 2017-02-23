// TODO: remove
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DkTools.CodeAnalysis.Statements;
//using DkTools.CodeModel;

//namespace DkTools.CodeAnalysis.Nodes
//{
//	class UnresolvedNode : TextNode
//	{
//		public UnresolvedNode(Statement stmt, Span span, string text)
//			: base(stmt, span, text)
//		{
//		}

//		public override int Precedence
//		{
//			get
//			{
//				return 2;
//			}
//		}

//		public override void Simplify(RunScope scope)
//		{
//			Parent.ReplaceNodes(Resolve(scope, null), this);
//		}

//		public Node Resolve(RunScope scope, DataType context)
//		{
//			var def = (from d in Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(Span.Start + scope.FuncOffset, Text)
//					   where !d.RequiresChild && !d.ArgumentsRequired
//					   select d).FirstOrDefault();
//			if (def != null)
//			{
//				return new IdentifierNode(Statement, Span, Text, def);
//			}
//			else
//			{
//				var node = new UnknownNode(Statement, Span, Text);
//				node.ReportError(Span, CAError.CA0001, Text);	// Unknown '{0}'.
//				return node;
//			}
//		}
//	}
//}
