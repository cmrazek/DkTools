using DK.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DK.Modeling.Tokens
{
	public partial class CodeFile : GroupToken
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
			_model = model ?? throw new ArgumentNullException(nameof(model));
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

		#region Regions
		private Dictionary<int, Region> _regions = new Dictionary<int, Region>();
		private bool _commentRegionsFound;

		private enum RegionType
		{
			Comment,
			User
		}

		private class Region
		{
			public CodeSpan span;
			public RegionType type;
			public string title;
		}

		private void AddCommentRegion(CodeSpan span)
		{
			// Start and end must be on separate lines.
			var startLineEnd = Code.FindEndOfLine(span.Start);
			if (span.End > startLineEnd)
			{
				_regions[span.Start] = new Region
				{
					//scope = scope,
					span = span,
					type = RegionType.Comment,
					title = Code.GetText(span).GetFirstLine().Trim()
				};
			}
		}

		public void StartUserRegion(int pos, string title)
		{
			_regions[pos] = new Region
			{
				span = new CodeSpan(pos, pos),
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
				reg.span = new CodeSpan(reg.span.Start, pos);
			}
		}

		public string GetRegionText(CodeSpan span)
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
				if (!_commentRegionsFound)
				{
					FindCommentRegions();
					_commentRegionsFound = true;
				}

				foreach (var reg in base.OutliningRegions) yield return reg;

				foreach (var reg in _regions.Values)
				{
					switch (reg.type)
					{
						case RegionType.Comment:
							yield return new OutliningRegion
							{
								Span = reg.span,
								CollapseToDefinition = true,
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
						var span = new CodeSpan(section.Start, section.End);
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

		private static readonly Regex _rxUserRegion = new Regex(@"^(?://|/*)\s*\#(region|endregion)\b(.*)$");

		private void FindCommentRegions()
		{
			var parser = new CodeParser(_source.Text);
			parser.ReturnComments = true;

			var insideComment = false;
			var commentSpan = CodeSpan.Empty;
			Match match;

			while (parser.Read())
			{
				if (parser.Type == CodeType.Comment)
				{
					if ((match = _rxUserRegion.Match(parser.Text)).Success)
					{
						if (insideComment)
						{
							// Finish the comment region here so that it doesn't break the region
							AddCommentRegion(commentSpan);
							insideComment = false;
						}

						if (match.Groups[1].Value == "region")
						{
							StartUserRegion(parser.Span.Start, match.Groups[2].Value.Trim());
						}
						else
						{
							EndUserRegion(parser.Span.End);
						}
					}
					else
					{
						if (insideComment)
						{
							commentSpan.End = parser.Span.End;
						}
						else
						{
							insideComment = true;
							commentSpan = parser.Span;
						}
					}
				}
				else
				{
					if (insideComment) AddCommentRegion(commentSpan);
					insideComment = false;
				}
			}
		}
		#endregion

		#region Parsing
		private CodeSource _source;
		private string _fileName;
		private CodeParser _code;

		public void Parse(CodeSource source, string fileName, IEnumerable<string> parentFiles, bool visible)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_code = new CodeParser(_source.Text);
			_fileName = fileName;
			_parentFiles = parentFiles.ToArray();

			FileContextHelper.FileNameIsClass(_fileName, out _className);

			var scope = new Scope(this, 0, ScopeHint.None, visible, _model.DefinitionProvider, _model.AppSettings);
			scope.ClassName = _className;
			Scope = scope;

			while (_code.SkipWhiteSpace())
			{
				var stmt = StatementToken.TryParse(scope);
				if (stmt != null) AddToken(stmt);
			}

			Span = new CodeSpan(0, _code.Length);
		}

		internal CodeParser CodeParser
		{
			get { return _code; }
		}
		#endregion
	}
}
