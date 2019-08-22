using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Adornments;

namespace DkTools.Classifier
{
	internal struct ProbeClassifiedRun
	{
		public ProbeClassifierType Type { get; private set; }
		public string Text { get; private set; }

		public ProbeClassifiedRun(ProbeClassifierType type, string text)
		{
			Type = type;
			Text = text;
		}

		public override string ToString()
		{
			return Text;
		}

		public ClassifiedTextRun ToClassifiedTextRun()
		{
			return new ClassifiedTextRun(ProbeClassifier.GetClassificationTypeName(Type), Text);
		}
	}

	internal class ProbeClassifiedString
	{
		private ProbeClassifiedRun[] _runs;

		public static readonly ProbeClassifiedString Empty = new ProbeClassifiedString();

		public ProbeClassifiedString()
		{
			_runs = new ProbeClassifiedRun[0];
		}

		public ProbeClassifiedString(params ProbeClassifiedRun[] runs)
		{
			_runs = runs;
		}

		public ProbeClassifiedString(IEnumerable<ProbeClassifiedRun> runs)
		{
			if (runs != null) _runs = runs.ToArray();
			else _runs = new ProbeClassifiedRun[0];
		}

		public ProbeClassifiedString(ProbeClassifierType type, string text)
		{
			_runs = new ProbeClassifiedRun[] { new ProbeClassifiedRun(type, text) };
		}

		public ProbeClassifiedRun[] Runs
		{
			get { return _runs; }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var run in _runs)
			{
				sb.Append(run.ToString());
			}
			return sb.ToString();
		}

		public bool IsEmpty
		{
			get { return _runs.Length == 0; }
		}

		public IEnumerable<ClassifiedTextRun> ToClassifiedTextRuns()
		{
			return _runs.Select(r => r.ToClassifiedTextRun()).ToArray();
		}

		public ClassifiedTextElement ToClassifiedTextElement()
		{
			return new ClassifiedTextElement(_runs.Select(r => r.ToClassifiedTextRun()));
		}
	}

	internal class ProbeClassifiedStringBuilder
	{
		private List<ProbeClassifiedRun> _runs = new List<ProbeClassifiedRun>();

		public ProbeClassifiedStringBuilder()
		{
		}

		public ProbeClassifiedStringBuilder(params ProbeClassifiedRun[] runs)
		{
			_runs.AddRange(runs);
		}

		public ProbeClassifiedStringBuilder(IEnumerable<ProbeClassifiedRun> runs)
		{
			if (runs != null) _runs.AddRange(runs);
		}

		public IEnumerable<ProbeClassifiedRun> Runs
		{
			get { return _runs; }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var run in _runs)
			{
				sb.Append(run.ToString());
			}
			return sb.ToString();
		}

		public ProbeClassifiedString ToClassifiedString()
		{
			return new ProbeClassifiedString(_runs);
		}

		public bool IsEmpty
		{
			get { return _runs.Count == 0; }
		}

		public void AddRun(ProbeClassifiedRun run)
		{
			_runs.Add(run);
		}

		public void Add(ProbeClassifierType type, string text)
		{
			_runs.Add(new ProbeClassifiedRun(type, text));
		}

		public void AddSpace()
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Normal, " "));
		}

		public void AddKeyword(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Keyword, text));
		}

		public void AddDataType(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.DataType, text));
		}

		public void AddNumber(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Number, text));
		}

		public void AddStringLiteral(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.StringLiteral, text));
		}

		public void AddOperator(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Operator, text));
		}

		public void AddDelimiter(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Delimiter, text));
		}

		public void AddConstant(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Constant, text));
		}

		public void AddTableName(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.TableName, text));
		}

		public void AddTableField(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.TableField, text));
		}

		public void AddComment(string text)
		{
			_runs.Add(new ProbeClassifiedRun(ProbeClassifierType.Comment, text));
		}

		public void AddClassifiedString(ProbeClassifiedString pcs)
		{
			_runs.AddRange(pcs.Runs);
		}

		public void AddClassifiedString(ProbeClassifiedStringBuilder pcs)
		{
			_runs.AddRange(pcs.Runs);
		}
	}
}
