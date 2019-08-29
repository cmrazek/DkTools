using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DkTools.CodeProcessing
{
	internal class CodeProcessor
	{
		private ProbeAppSettings _appSettings;
		private string _fileName = string.Empty;
		private List<CodeFile> _files = new List<CodeFile>();
		private List<CodeLine> _lines = new List<CodeLine>();
		private string _baseFileName = string.Empty;
		private MergeMode _mergeMode = MergeMode.Normal;
		private List<string> _replace = new List<string>();
		private List<CodeError> _errors = new List<CodeError>();
		private int _insertIndex = -1;
		private bool _showMergeComments = false;

		private enum MergeMode
		{
			Normal,
			ReplaceStart,
			ReplaceWith,
			Insert
		}

		public CodeProcessor()
		{ }

		public void ProcessFile(ProbeAppSettings appSettings, string fileName)
		{
			if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));

			_appSettings = appSettings;
			_fileName = fileName;

			_files.Clear();
			_lines.Clear();
			_replace.Clear();

			var mergeFileNames = _appSettings.FindLocalFiles(fileName, true).ToArray();
			if (mergeFileNames.Length == 0)
			{
				_errors.Add(new CodeError(null, "No files found."));
				return;
			}

			// Process the base file (don't end with "&")
			foreach (var baseFileName in (from f in mergeFileNames where !f.EndsWith("&") select f))
			{
				if (!string.IsNullOrEmpty(_baseFileName))
				{
					_errors.Add(new CodeError(null, string.Format("More than one base file found.  File '{0}' is ignored.", baseFileName)));
					continue;
				}

				_baseFileName = baseFileName;

				var file = new CodeFile(baseFileName, true);
				_files.Add(file);
				AddBaseFile(file);
			}

			// Process the local files (end with "&")
			foreach (var localFileName in (from f in mergeFileNames where f.EndsWith("&") select f))
			{
				if (!_files.Any(f => f.FileName.Equals(localFileName, StringComparison.OrdinalIgnoreCase)))
				{
					var file = new CodeFile(localFileName, false);
					_files.Add(file);
					MergeLocalFile(file);
				}
			}
		}

		public IEnumerable<CodeLine> Lines
		{
			get { return _lines; }
		}

		public IEnumerable<CodeFile> Files
		{
			get { return _files; }
		}

		public IEnumerable<CodeError> Errors
		{
			get { return _errors; }
		}

		public bool HasErrors
		{
			get { return _errors.Count != 0; }
		}

		private void AddBaseFile(CodeFile file)
		{
			using (var reader = new StreamReader(file.FileName))
			{
				int lineNum = 1;
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					_lines.Add(new CodeLine(file, lineNum++, line));
				}

				if (_showMergeComments)
				{
					_lines.Add(new CodeLine(null, 0, string.Concat("// end of base file ", file.FileName)));
				}
			}
		}

		private void MergeLocalFile(CodeFile file)
		{
			_mergeMode = MergeMode.Normal;

			using (var reader = new StreamReader(file.FileName))
			{
				if (_showMergeComments)
				{
					_lines.Add(new CodeLine(null, 0, string.Concat("// start of local file ", file.FileName)));
				}

				int lineNum = 1;
				while (!reader.EndOfStream)
				{
					var line = new CodeLine(file, lineNum, reader.ReadLine());
					MergeLocalLine(line);
					lineNum++;
				}

				if (_showMergeComments)
				{
					_lines.Add(new CodeLine(null, 0, string.Concat("// end of local file ", file.FileName)));
				}
			}
		}

		private Regex _rxReplace = new Regex(@"^\s*#replace\s*(.*)$");
		private Regex _rxWith = new Regex(@"^\s*#with\s*(.*)$");
		private Regex _rxEndReplace = new Regex(@"^\s*#endreplace\s*(.*)$");
		private Regex _rxInsert = new Regex(@"^\s*#insert\s+(.+)$");
		private Regex _rxEndInsert = new Regex(@"^\s*#endinsert\s*(.*)$");

		private void MergeLocalLine(CodeLine line)
		{
			Match match;

			switch (_mergeMode)
			{
				case MergeMode.Normal:
					if ((match = line.Match(_rxReplace)).Success)
					{
						_mergeMode = MergeMode.ReplaceStart;
						_replace.Clear();
						var trailing = match.Groups[1].Value.Trim();
						if (!string.IsNullOrEmpty(trailing)) _replace.Add(trailing);
					}
					else if ((match = line.Match(_rxInsert)).Success)
					{
						var labelName = match.Groups[1].Value.Trim();
						_insertIndex = FindLabel(labelName);
						if (_insertIndex < 0)
						{
							_errors.Add(new CodeError(line,
								string.Format("Label '{0}' not found.", labelName)));
						}
						else
						{
							if (_showMergeComments)
							{
								_lines.Insert(_insertIndex, new CodeLine(null, 0,
									string.Format("// insert from {0} ({1})", line.FileName, line.LineNum)));
								_insertIndex++;
							}
						}
						_mergeMode = MergeMode.Insert;
					}
					else
					{
						_lines.Add(line);
					}
					break;

				case MergeMode.ReplaceStart:
					if ((match = line.Match(_rxWith)).Success)
					{
						if (_replace.Count == 0)
						{
							_errors.Add(new CodeError(line, "Empty #replace statement."));
							_insertIndex = -1;
						}
						else if ((_insertIndex = FindReplace()) < 0)
						{
							_errors.Add(new CodeError(line, "#replace not found."));
						}
						else
						{
							_lines.RemoveRange(_insertIndex, _replace.Count);

							if (_showMergeComments)
							{
								_lines.Insert(_insertIndex, new CodeLine(null, 0,
									string.Format("// replace from {0} ({1})", line.FileName, line.LineNum)));
								_insertIndex++;
							}

							if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
							{
								_lines.Insert(_insertIndex, new CodeLine(line.File, line.LineNum, match.Groups[1].Value));
							}
						}

						_mergeMode = MergeMode.ReplaceWith;
						_replace.Clear();
					}
					else
					{
						_replace.Add(line.Text);
					}
					break;

				case MergeMode.ReplaceWith:
					if ((match = line.Match(_rxEndReplace)).Success)
					{
						if (_insertIndex >= 0)
						{
							if (_showMergeComments)
							{
								_lines.Insert(_insertIndex, new CodeLine(null, 0, "// end of replace"));
								_insertIndex++;
							}
						}

						_mergeMode = MergeMode.Normal;
						_insertIndex = -1;

						if (!String.IsNullOrWhiteSpace(match.Groups[1].Value))
						{
							_lines.Add(new CodeLine(line.File, line.LineNum, match.Groups[1].Value));
						}
					}
					else
					{
						if (_insertIndex >= 0)
						{
							_lines.Insert(_insertIndex, line);
							_insertIndex++;
						}
					}
					break;

				case MergeMode.Insert:
					if ((match = line.Match(_rxEndInsert)).Success)
					{
						if (_insertIndex >= 0 && _showMergeComments)
						{
							_lines.Insert(_insertIndex, new CodeLine(null, 0, "// end of insert"));
						}

						_mergeMode = MergeMode.Normal;
						_insertIndex = -1;
					}
					else
					{
						if (_insertIndex > 0)
						{
							_lines.Insert(_insertIndex, line);
							_insertIndex++;
						}
					}
					break;
			}
		}

		private int FindLabel(string labelName)
		{
			int index = 0;
			foreach (var line in _lines)
			{
				if (line.IsLabel(labelName)) return index;
				index++;
			}
			return -1;
		}

		private int FindReplace()
		{
			var replaceCount = _replace.Count;
			var cleanReplace = (from r in _replace select CleanReplaceLine(r)).ToArray();

			for (int lineIndex = 0, maxLineIndex = _lines.Count - replaceCount; lineIndex < maxLineIndex; lineIndex++)
			{
				var match = true;
				for (int replaceIndex = 0; replaceIndex < replaceCount; replaceIndex++)
				{
					var cleanLine = CleanReplaceLine(_lines[lineIndex + replaceIndex].Text);
					if (cleanLine != cleanReplace[replaceIndex])
					{
						match = false;
						break;
					}
				}

				if (match) return lineIndex;
			}

			return -1;
		}

		private StringBuilder _cleanSb = new StringBuilder();
		private StringBuilder _cleanSbWord = new StringBuilder();
		private string CleanReplaceLine(string text)
		{
			_cleanSb.Clear();
			_cleanSbWord.Clear();
			foreach (char ch in text)
			{
				if (!Char.IsWhiteSpace(ch))
				{
					_cleanSbWord.Append(ch);
				}
				else if (_cleanSbWord.Length > 0)
				{
					if (_cleanSb.Length > 0) _cleanSb.Append(" ");
					_cleanSb.Append(_cleanSbWord.ToString());
					_cleanSbWord.Clear();
				}
			}

			return _cleanSb.ToString();
		}

		public bool ShowMergeComments
		{
			get { return _showMergeComments; }
			set { _showMergeComments = value; }
		}
	}
}
