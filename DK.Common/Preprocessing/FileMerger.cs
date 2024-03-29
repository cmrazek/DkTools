﻿using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DK.Preprocessing
{
	public class FileMerger
	{
		private DkAppSettings _appSettings;
		private List<Line> _lines;
		private string _origFileName = "";
		private List<string> _localFileNames = new List<string>();
		private string _currentLocalFileName = "";
		private int _currentLocalLine = 0;
		private MergeMode _mode = MergeMode.Normal;
		private List<Line> _replace = new List<Line>();
		private int _replaceLine;
		private List<LabelPos> _labels;
		private int _insertLine;
		private CodeSource _mergedContent;
		private bool _showMergeComments;
		private string _primaryFileName;
		private string _origContent;
		private Dictionary<string, string> _localFileContent = new Dictionary<string, string>();

		private enum MergeMode
		{
			Normal,
			ReplaceStart,
			ReplaceWith,
			Insert,
		}

		private struct Line
		{
			public string fileName;
			public int pos;
			public string text;

			public Line(string fileName, int pos, string text)
			{
				this.fileName = fileName;
				this.pos = pos;
				this.text = text;
			}
		}

		public FileMerger(DkAppSettings appSettings)
		{
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		}

		public void MergeFile(string fullPathName, string content, bool showMergeComments, bool fileIsPrimary)
		{
			// Locate all needed copies of files
			_primaryFileName = fullPathName;
			_origFileName = "";
			_localFileNames.Clear();
			_showMergeComments = showMergeComments;

			var relativeFileName = fullPathName;
			if (PathUtil.IsPathRooted(fullPathName)) fullPathName = UnrootFileName(fullPathName);
			FindFiles(fullPathName);

			if (string.IsNullOrEmpty(_origFileName))
			{
				_origFileName = fullPathName;
				_mergedContent = new CodeSource();
				_mergedContent.Append(content, fullPathName, fileStartPos: 0, fileEndPos: content.Length, actualContent: true, primaryFile: true, disabled: false);
				_mergedContent.Flush();
				return;
			}

			if (content == null) _origContent = _appSettings.FileSystem.GetFileText(_origFileName);
			else _origContent = content;

			// Perform localization
			CreateLineDataFromOrigFile(_origContent);
			foreach (string localFileName in _localFileNames)
			{
				try
				{
					MergeFile(localFileName);
				}
				catch (Exception ex)
				{
					_appSettings.Log.Error(ex, "Error when merging local file '{0}' into '{1}'.", localFileName, fullPathName);
				}
			}

			// Generate the final source.
			_mergedContent = new CodeSource();
			int lineIndex = 0;
			foreach (var line in _lines)
			{
				var primary = fileIsPrimary && _primaryFileName.Equals(line.fileName, StringComparison.OrdinalIgnoreCase);
				var endPos = line.pos + line.text.Length;
				_mergedContent.Append(line.text, new CodeAttributes(line.fileName, line.pos, endPos, true, primary, false));
				if (!line.text.EndsWith("\n") && lineIndex + 1 < _lines.Count)
				{
					// Does not end with crlf. Need to insert between the lines.
					_mergedContent.Append("\r\n", new CodeAttributes(line.fileName, endPos, endPos, false, primary, false));
				}
				lineIndex++;
			}
			_mergedContent.Flush();
		}

		private string UnrootFileName(string fileName)
		{
			fileName = _appSettings.FileSystem.GetFullPath(fileName);

			foreach (var dir in _appSettings.SourceDirs)
			{
				if ((dir.EndsWith("\\") && fileName.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) ||
					(!dir.EndsWith("\\") && fileName.StartsWith(dir + "\\", StringComparison.OrdinalIgnoreCase)))
				{
					fileName = fileName.Substring(dir.Length).Trim();
					while (fileName.StartsWith("\\")) fileName = fileName.Substring(1);
					return fileName;
				}
			}

			foreach (var dir in _appSettings.IncludeDirs)
			{
				if ((dir.EndsWith("\\") && fileName.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) ||
					(!dir.EndsWith("\\") && fileName.StartsWith(dir + "\\", StringComparison.OrdinalIgnoreCase)))
				{
					fileName = fileName.Substring(dir.Length).Trim();
					while (fileName.StartsWith("\\")) fileName = fileName.Substring(1);
					return fileName;
				}
			}

			// If this file is not in any source/include directory, then just use the file name without any path.
			// This can happen during code QA, where the code is kept in another folder.
			return PathUtil.GetFileName(fileName);
		}

		private void FindFiles(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return;

			// strip trailing '&' off the end of the filename, if it exists
			if (fileName.EndsWith("&") || fileName.EndsWith("+")) fileName = fileName.Substring(0, fileName.Length - 1);
			if (string.IsNullOrEmpty(fileName)) return;

			foreach (string probeDir in _appSettings.SourceDirs)
			{
				try
				{
					if (!_appSettings.FileSystem.DirectoryExists(probeDir)) continue;
					FindFiles_SearchDir(probeDir, fileName);
				}
				catch (Exception ex)
				{
					_appSettings.Log.Error(ex, "Exception when scanning source directory [{0}]", probeDir);
				}
			}

			if (string.IsNullOrEmpty(_origFileName))
			{
				_localFileNames.Clear();
				foreach (string includeDir in _appSettings.IncludeDirs)
				{
					try
					{
						if (!_appSettings.FileSystem.DirectoryExists(includeDir)) continue;
						FindFiles_SearchDir(includeDir, fileName);
					}
					catch (Exception ex)
					{
						_appSettings.Log.Error(ex, "Exception when scanning include directory [{0}]", includeDir);
					}
				}
			}
		}

		private void FindFiles_SearchDir(string dir, string fileName)
		{
			string pathName = PathUtil.CombinePath(dir, fileName);
			if (_appSettings.FileSystem.FileExists(pathName))
			{
				// this is the original file
				_origFileName = pathName;
			}
			else if (_appSettings.FileSystem.FileExists(pathName + "&"))
			{
				// this is a local file
				var ampFileName = pathName + "&";
				if (!_localFileNames.Any(x => string.Equals(x, ampFileName, StringComparison.OrdinalIgnoreCase))) _localFileNames.Add(ampFileName);
			}
			else if (_appSettings.FileSystem.FileExists(pathName + "+"))
			{
				// this is a local file
				var ampFileName = pathName + "+";
				if (!_localFileNames.Any(x => string.Equals(x, ampFileName, StringComparison.OrdinalIgnoreCase))) _localFileNames.Add(ampFileName);
			}

			foreach (string subDir in _appSettings.FileSystem.GetDirectoriesInDirectory(dir))
			{
				try
				{
					FindFiles_SearchDir(subDir, fileName);
				}
				catch (Exception ex)
				{
					_appSettings.Log.Error(ex, "Exception when scanning directory [{0}]", subDir);
				}
			}
		}

		private void CreateLineDataFromOrigFile(string fileText)
		{
			var pos = 0;
			var linePos = pos;
			var sb = new StringBuilder();

			_lines = new List<Line>();

			foreach (var ch in fileText)
			{
				sb.Append(ch);
				if (ch == '\n')
				{
					_lines.Add(new Line(_origFileName, linePos, sb.ToString()));
					sb.Clear();
				}
				pos++;
				if (ch == '\n') linePos = pos;
			}
			_lines.Add(new Line(_origFileName, pos, sb.ToString()));
		}

		private void MergeFile(string localFileName)
		{
			if (_showMergeComments) _lines.Add(new Line(string.Empty, 0, string.Concat("// start of local file ", localFileName)));

			AnalyzeOrigFile();

			_currentLocalFileName = localFileName;
			_currentLocalLine = 1;

			var fileText = _appSettings.FileSystem.GetFileText(localFileName);
			_localFileContent[localFileName.ToLower()] = fileText;

			var pos = 0;
			var linePos = pos;
			var sb = new StringBuilder();

			foreach (var ch in fileText)
			{
				sb.Append(ch);
				if (ch == '\n')
				{
					ProcessLocalLine(new Line(localFileName, linePos, sb.ToString().TrimEnd()));
					sb.Clear();
				}
				pos++;
				if (ch == '\n') linePos = pos;
			}

			ProcessLocalLine(new Line(localFileName, pos, sb.ToString().TrimEnd()));

			if (_showMergeComments) _lines.Add(new Line(string.Empty, 0, string.Concat("// end of local file ", localFileName)));
		}

		private static Regex _rxLabelLine = new Regex(@"^\s*\#label\b");

		private void AnalyzeOrigFile()
		{
			_labels = new List<LabelPos>();

			Line line;
			for (int lineNum = 0, lineCount = _lines.Count; lineNum < lineCount; lineNum++)
			{
				line = _lines[lineNum];

				var match = _rxLabelLine.Match(line.text);
				if (match.Success)
				{
					_labels.Add(new LabelPos
					{
						name = line.text.Substring(match.Index + match.Length).Trim(),
						insertLineNum = lineNum + 1 // insert line after this one
					});
				}
			}
		}

		private static Regex _rxReplaceLine = new Regex(@"^\s*\#replace\b");
		private static Regex _rxInsertLine = new Regex(@"^\s*\#insert\b");
		private static Regex _rxWithLine = new Regex(@"^\s*\#with\b");
		private static Regex _rxEndReplaceLine = new Regex(@"^\s*\#endreplace\b");
		private static Regex _rxEndInsertLine = new Regex(@"^\s*\#endinsert\b");

		private void ProcessLocalLine(Line line)
		{
			Match match;

			switch (_mode)
			{
				case MergeMode.Normal:
					if ((match = _rxReplaceLine.Match(line.text)).Success)
					{
						_mode = MergeMode.ReplaceStart;
						_replace.Clear();
					}
					else if ((match = _rxInsertLine.Match(line.text)).Success)
					{
						var labelName = line.text.Substring(match.Index + match.Length).Trim();
						_insertLine = GetLabelInsert(labelName);
						if (_insertLine < 0)
						{
							_appSettings.Log.Warning("{0}: #label '{1}' not found.", _currentLocalFileName, labelName);
							_insertLine = _lines.Count;
						}

						if (_showMergeComments)
						{
							_lines.Insert(_insertLine, new Line(string.Empty, 0, string.Format("// insert from {0}({1})", _currentLocalFileName, _currentLocalLine)));
							BumpLabels(_insertLine, 1);
							_insertLine += 1;
						}

						_mode = MergeMode.Insert;
					}
					else
					{
						_lines.Add(line);
					}
					break;

				case MergeMode.ReplaceStart:
					if ((match = _rxWithLine.Match(line.text)).Success)
					{
						if (_replace.Count == 0)
						{
							_appSettings.Log.Warning("{0}: empty #replace statement.", _currentLocalFileName);
						}
						else
						{
							_replaceLine = FindReplace();
							if (_replaceLine < 0)
							{
								_appSettings.Log.Warning("{0}: #replace at line {1} not found.", _currentLocalFileName, _currentLocalLine);
							}
							else
							{
								_lines.RemoveRange(_replaceLine, _replace.Count);
								BumpLabels(_replaceLine, -_replace.Count);

								if (_showMergeComments)
								{
									_lines.Insert(_replaceLine, new Line(string.Empty, 0, string.Format("// replace from {0}({1})", _currentLocalFileName, _currentLocalLine)));
									BumpLabels(_replaceLine, 1);
									_replaceLine += 1;
								}
							}
						}

						_replace.Clear();
						_mode = MergeMode.ReplaceWith;

						// Check if there's additional text after the #with
						var remain = line.text.Substring(match.Index + match.Length);
						if (!string.IsNullOrWhiteSpace(remain))
						{
							var prefixLength = match.Index + match.Length;
							_replace.Add(new Line(line.fileName, line.pos + prefixLength, remain));
						}
					}
					else
					{
						_replace.Add(line);
					}

					break;

				case MergeMode.ReplaceWith:
					if ((match = _rxEndReplaceLine.Match(line.text)).Success)
					{
						if (_showMergeComments)
						{
							_lines.Insert(_replaceLine, new Line(string.Empty, 0, "// end of replace"));
							BumpLabels(_replaceLine, 1);
							_replaceLine += 1;
						}

						_mode = MergeMode.Normal;
					}
					else
					{
						_lines.Insert(_replaceLine, line);
						BumpLabels(_replaceLine, 1);
						_replaceLine += 1;
					}
					break;

				case MergeMode.Insert:
					if ((match = _rxEndInsertLine.Match(line.text)).Success)
					{
						if (_showMergeComments)
						{
							_lines.Insert(_insertLine, new Line(string.Empty, 0, "// end of insert"));
							BumpLabels(_insertLine, 1);
						}

						_mode = MergeMode.Normal;
					}
					else
					{
						_lines.Insert(_insertLine, line);
						BumpLabels(_insertLine, 1);
						_insertLine += 1;
					}
					break;
			}

		}

		private int FindReplace()
		{
			int maxOrigLine = _lines.Count - _replace.Count + 1;
			int lineNum = 0;

			for (lineNum = 0; lineNum <= maxOrigLine - 1; lineNum++)
			{
				// check if this lines up
				bool match = true;
				int origLineNum = lineNum;
				int localLineNum = 0;

				for (localLineNum = 0; localLineNum <= _replace.Count - 1; localLineNum++)
				{
					if (CleanLineForCompare(_lines[origLineNum].text) != CleanLineForCompare(_replace[localLineNum].text))
					{
						match = false;
						break;
					}
					origLineNum++;
				}

				if (match) return lineNum;
			}

			return -1;
		}

		private string CleanLineForCompare(string line)
		{
			var sb = new StringBuilder(line.Length);
			var gotWhiteSpace = true;

			foreach (var ch in line)
			{
				if (char.IsWhiteSpace(ch))
				{
					if (!gotWhiteSpace) sb.Append(" ");
					gotWhiteSpace = true;
				}
				else
				{
					sb.Append(ch);
					gotWhiteSpace = false;
				}
			}

			// Remove trailing whitespace
			while (sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1])) sb.Length--;

			return sb.ToString();
		}

		private int GetLabelInsert(string labelName)
		{
			var label = (from l in _labels where string.Equals(l.name, labelName, StringComparison.OrdinalIgnoreCase) select l).FirstOrDefault();
			if (label != null) return label.insertLineNum;
			return -1;
		}

		private void BumpLabels(int startLineNum, int delta)
		{
			foreach (var lbl in _labels)
			{
				if (lbl.insertLineNum >= startLineNum) lbl.insertLineNum += delta;
			}
		}

		private class LabelPos
		{
			public string name;
			public int insertLineNum;
		}

		public CodeSource MergedContent
		{
			get { return _mergedContent; }
		}

		public IEnumerable<string> FileNames
		{
			get
			{
				yield return _origFileName;
				foreach (var fn in _localFileNames) yield return fn;
			}
		}

		public IEnumerable<string> LocalFileNames
		{
			get
			{
				return _localFileNames;
			}
		}

		public bool IsMixed
		{
			get { return _localFileNames.Count != 0; }
		}

		public string GetFileContent(string localFileName)
		{
			if (string.Equals(_origFileName, localFileName, StringComparison.OrdinalIgnoreCase)) return _origContent;

			if (_localFileContent.TryGetValue(localFileName.ToLower(), out var content)) return content;

			_appSettings.Log.Warning("File does not exist: {0}", localFileName);
			return string.Empty;
		}
	}
}
