using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Tokens
{
	internal sealed partial class CodeFile : GroupToken
	{
		#region Variables
		private CodeModel _model;
		private string[] _parentFiles;
		private Dictionary<string, DataType> _definedDataTypes = new Dictionary<string, DataType>();
		private string _className;
		#endregion

		#region Construction
		public CodeFile(CodeModel model)
			: base(new Scope(model))
		{
			if (model == null) throw new ArgumentNullException("model");
			_model = model;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public CodeSource CodeSource
		{
			get { return _source; }
		}

		public CodeModel Model
		{
			get { return _model; }
		}

		public IEnumerable<string> ParentFiles
		{
			get { return _parentFiles; }
		}

		public string ClassName
		{
			get { return _className; }
		}
		#endregion

		// TODO: remove
		//#region Position calculations
		//public int Position
		//{
		//	get { return _pos; }
		//	set
		//	{
		//		if (value < 0 || value > _length) throw new ArgumentOutOfRangeException();
		//		_pos = value;
		//	}
		//}

		//public int FindStartOfLine(int pos)
		//{
		//	if (pos > _length) pos = _length;
		//	while (pos > 0 && _src[pos - 1] != '\n') pos--;
		//	return pos;
		//}

		//public int FindEndOfPreviousLine(int pos)
		//{
		//	var offset = FindStartOfLine(pos);
		//	if (offset <= 0) return 0;

		//	offset--;
		//	if (offset > 0 && _src[offset] == '\n' && _src[offset - 1] == '\r') offset--;
		//	return offset;
		//}

		//public int FindEndOfLine(int pos)
		//{
		//	while (pos < _length && !_src[pos].IsEndOfLineChar()) pos++;
		//	return pos;
		//}

		//public int FindStartOfNextLine(int pos)
		//{
		//	pos = FindEndOfLine(pos);
		//	if (pos < _length && _src[pos] == '\r') pos++;
		//	if (pos < _length && _src[pos] == '\n') pos++;
		//	return pos;
		//}
		//#endregion

		#region Regions
		private Dictionary<int, Region> _regions = new Dictionary<int, Region>();

		private enum RegionType
		{
			Comment,
			User
		}

		private class Region
		{
			public Scope scope;
			public Span span;
			public RegionType type;
			public string title;
		}

		// TODO: remove
		//private void AddCommentRegion(Scope scope, Span span)
		//{
		//	if (scope.Visible)
		//	{
		//		// Start and end must be on separate lines.
		//		var startLineEnd = FindEndOfLine(span.Start);
		//		if (span.End > startLineEnd)
		//		{
		//			_regions[span.Start] = new Region
		//			{
		//				scope = scope,
		//				span = span,
		//				type = RegionType.Comment,
		//				title = GetText(span).GetFirstLine().Trim()
		//			};
		//		}
		//	}
		//}

		public void StartUserRegion(Scope scope, int pos, string title)
		{
			_regions[pos] = new Region
			{
				scope = scope,
				span = new Span(pos, pos),
				type = RegionType.User,
				title = title.Trim()
			};
		}

		private void EndUserRegion(int pos)
		{
			// Find the region with the highest start, where the end has not been found yet.
			// - same as the start (uninitialized)
			// - equal to pos (already found by a previous call)

			var maxStart = int.MinValue;
			int start;

			foreach (var reg in _regions.Values)
			{
				if (reg.type != RegionType.User) continue;
				if (reg.span.End == pos) return;

				start = reg.span.Start;
				if (reg.span.End == start && start < pos && start > maxStart)
				{
					maxStart = reg.span.Start;
				}
			}

			if (maxStart != int.MinValue)
			{
				// Update the end position of the found region.
				var reg = _regions[maxStart];
				reg.span = new Span(reg.span.Start, pos);
			}
		}

		public string GetRegionText(Span span)
		{
			var str = _code.GetText(span);
			if (str.Length > Constants.OutliningMaxContextChars)
			{
				str = str.Substring(0, Constants.OutliningMaxContextChars) + "...";
			}
			return str;
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				foreach (var reg in base.OutliningRegions) yield return reg;

				foreach (var reg in _regions.Values)
				{
					switch (reg.type)
					{
						case RegionType.Comment:
							yield return new OutliningRegion
							{
								Span = reg.span,
								CollapseToDefinition = (reg.scope.Hint & ScopeHint.NotOnRoot) == 0,	// Auto-hide comments on the root
								Text = reg.title,
								TooltipText = GetRegionText(reg.span)
							};
							break;

						case RegionType.User:
							if (reg.span.End > reg.span.Start)
							{
								yield return new OutliningRegion
								{
									Span = reg.span,
									CollapseToDefinition = true,	// Auto-hide all regions
									Text = reg.title,
									TooltipText = GetRegionText(reg.span)
								};
							}
							break;
					}
				}

				var disabledSections = _model.DisabledSections;
				if (disabledSections != null)
				{
					foreach (var section in disabledSections)
					{
						var span = new Span(section.Start, section.End);
						yield return new OutliningRegion
						{
							Span = span,
							CollapseToDefinition = true,
							Text = Constants.DefaultOutliningText,
							TooltipText = GetRegionText(span)
						};
					}
				}
			}
		}
		#endregion

		#region Parsing
		private CodeSource _source;
		private string _fileName;
		private TokenParser.Parser _code;

		public void Parse(CodeSource source, string fileName, IEnumerable<string> parentFiles, bool visible)
		{
			if (source == null) throw new ArgumentNullException("source");
			_source = source;
			_code = new TokenParser.Parser(_source.Text);
			_fileName = fileName;
			_parentFiles = parentFiles.ToArray();

			FunctionFileScanning.FFUtil.FileNameIsClass(_fileName, out _className);

			var scope = new Scope(this, 0, ScopeHint.None, visible, _model.DefinitionProvider);
			scope.ClassName = _className;
			Scope = scope;

			while (_code.SkipWhiteSpace())
			{
				var stmt = StatementToken.TryParse(scope);
				if (stmt != null) AddToken(stmt);
			}

			Span = new Span(0, _code.Length);
		}

		public TokenParser.Parser CodeParser
		{
			get { return _code; }
		}
		#endregion
	}
}
