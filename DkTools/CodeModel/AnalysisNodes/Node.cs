using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.AnalysisNodes
{
	/// <summary>
	/// Used to represent a single 'item' during code analysis.
	/// </summary>
	abstract class Node
	{
		public abstract bool RequiresRValueEnumOption { get; }
		public abstract bool IsValidRValueEnumOption(string optionText);

		private Span _span;
		private Value _value;

		public Node(Span span, Value value)
		{
			_span = span;
			_value = value;
		}

		public Span Span
		{
			get { return _span; }
		}

		public Value Value
		{
			get { return _value; }
		}
	}
}
