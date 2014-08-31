using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class FunctionPlaceholderToken : WordToken
	{
		private Position _bodyStart;
		private Span? _entireFunctionSpan;

		public FunctionPlaceholderToken(GroupToken parent, Scope scope, Span span, string text, FunctionDefinition sourceDef)
			: base(parent, scope, span, text)
		{
			SourceDefinition = sourceDef;
			_bodyStart = sourceDef.BodyStartPosition;
		}

		public Span EntireFunctionSpan
		{
			get
			{
				if (!_entireFunctionSpan.HasValue)
				{
					var bodyToken = Scope.File.FindDownward(_bodyStart, t => t is BracesToken && t.Span.Start == _bodyStart).LastOrDefault();
					if (bodyToken == null)
					{
						_entireFunctionSpan = Span;
					}
					else
					{
						_entireFunctionSpan = new Span(Span.Start, bodyToken.Span.End);
					}
				}
				return _entireFunctionSpan.Value;
			}
		}
	}
}
