using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.DkDict
{
	class Dict
	{
		private static CodeParser _code;
		private static CodeSource _source;
		private static Dictionary<string, Table> _tables = new Dictionary<string, Table>();
		private static Dictionary<string, RelInd> _relinds = new Dictionary<string, RelInd>();
		private static Dictionary<string, Stringdef> _stringdefs = new Dictionary<string, Stringdef>();

		public static void Load()
		{
			try
			{
				Log.Write(LogLevel.Info, "Parsing DICT...");
				var startTime = DateTime.Now;

				// Find the dict file
				var dictPathName = string.Empty;
				foreach (var srcDir in ProbeEnvironment.SourceDirs)
				{
					dictPathName = Path.Combine(srcDir, "DICT");
					if (File.Exists(dictPathName)) break;
					dictPathName = string.Empty;
				}

				if (dictPathName == null)
				{
					Log.Write(LogLevel.Warning, "No DICT file could be found.");
					return;
				}
				var merger = new FileMerger();
				merger.MergeFile(dictPathName, null, false, true);

				var mergedSource = merger.MergedContent;
				var mergedReader = new CodeSource.CodeSourcePreprocessorReader(mergedSource);

				var fileStore = new FileStore();
				var preprocessor = new Preprocessor(fileStore);
				_source = new CodeSource();
				preprocessor.Preprocess(mergedReader, _source, dictPathName, null, FileContext.Dictionary);

				var curTime = DateTime.Now;
				var elapsed = curTime.Subtract(startTime);
				Log.Write(LogLevel.Info, "DICT preprocessing completed. (elapsed: {0})", elapsed);

#if DEBUG
				var dictContent = _source.Text;
				Log.Write(LogLevel.Debug, "  dict length: {0}", dictContent.Length);	// TODO: remove
				File.WriteAllText("d:\\temp\\dict.txt", dictContent);	// TODO: remove
#endif

				_code = new CodeParser(_source.Text);
				ReadDict();

				elapsed = DateTime.Now.Subtract(curTime);
				Log.Write(LogLevel.Info, "DICT parsing complete. (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Error when reading DICT file.");
			}
		}

		private static void ReportError(int pos, string message)
		{
			var filePos = _source.GetFilePosition(pos);
			Log.Write(LogLevel.Warning, "{0}(offset: {1}; prep: {3}): {2}", filePos.FileName, filePos.Position, message, pos);
		}

		private static void ReportError(int pos, string format, params object[] args)
		{
			ReportError(pos, string.Format(format, args));
		}

		private static void ReadDict()
		{
			var suppressErrors = false;

			while (_code.Read())
			{
				switch (_code.Type)
				{
					case CodeType.Word:
						{
							var word = _code.Text;
							if (word.Equals("create", StringComparison.OrdinalIgnoreCase))
							{
								ReadCreate();
								suppressErrors = false;
							}
							else if (word.Equals("alter", StringComparison.OrdinalIgnoreCase))
							{
								ReadAlter();
								suppressErrors = false;
							}
							else
							{
								if (!suppressErrors)
								{
									ReportError(_code.TokenStartPostion, "Unrecognized word '{0}'.", _code.Text);
									suppressErrors = true;
								}
							}
						}
						break;
					case CodeType.Operator:
						if (_code.Text != ";")
						{
							if (!suppressErrors)
							{
								ReportError(_code.TokenStartPostion, "Unrecognized operator '{0}'.", _code.Text);
								suppressErrors = true;
							}
						}
						break;
					default:
						if (!suppressErrors)
						{
							ReportError(_code.TokenStartPostion, "Unrecognized token '{0}'.", _code.Text);
							suppressErrors = true;
						}
						break;
				}
			}
		}

		private static void ReadCreate()
		{
			if (_code.ReadWord())
			{
				var word = _code.Text;
				if (word.Equals("table", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateTable();
				}
				else if (word.Equals("stringdef", StringComparison.OrdinalIgnoreCase))
				{
					ReadStringdef(true, false);
				}
				else if (word.Equals("index", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateIndex(false, false, false);
				}
				else if (word.Equals("unique", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateUnique();
				}
				else if (word.Equals("relationship", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateRelationship();
				}
				else
				{
					ReportError(_code.TokenStartPostion, "Unrecognized word '{0}' after 'create'.", _code.Text);
				}
			}
			else
			{
				ReportError(_code.TokenStartPostion, "Expected word after 'create'.");
			}
		}

		private static void ReadAlter()
		{
			if (_code.ReadWord())
			{
				var word = _code.Text;
				if (word.Equals("application", StringComparison.OrdinalIgnoreCase))
				{
					ReadAlterApplication();
				}
				else if (word.Equals("stringdef", StringComparison.OrdinalIgnoreCase))
				{
					ReadStringdef(false, true);
				}
				else if (word.Equals("table", StringComparison.OrdinalIgnoreCase))
				{
					ReadAlterTable();
				}
				else
				{
					ReportError(_code.TokenStartPostion, "Unrecognized word '{0}' after 'alter'.", _code.Text);
				}
			}
		}

		#region Tables
		private static void ReadCreateTable()
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected table name after 'create table'.");
				return;
			}
			if (!ProbeEnvironment.IsValidTableName(_code.Text))
			{
				ReportError(_code.TokenStartPostion, "Invalid table name '{0}'.", _code.Text);
				return;
			}
			var tableName = _code.Text;

			if (!_code.ReadNumber())
			{
				ReportError(_code.Position, "Expected schema number to follow table name.");
				return;
			}
			var tableNum = int.Parse(_code.Text);

			int tableNum2 = 0;
			if (_code.ReadNumber())
			{
				tableNum2 = int.Parse(_code.Text);
			}

			var table = new Table(tableName, tableNum, tableNum2);
			_tables[tableName] = table;

			// Table attributes
			ReadTableAttributes(table, "(", "{");

			if (!_code.ReadExact('(') && !_code.ReadExact('{'))
			{
				ReportError(_code.Position, "Expected '(' in create table.");
				return;
			}

			// Columns
			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(')') || _code.ReadExact('}')) break;
				if (_code.ReadExact(',') || _code.ReadExact(';')) continue;

				if (_code.ReadExactWholeWord("updates"))
				{
					if (!_code.ReadWord())
					{
						ReportError(_code.Position, "Expected master table name to follow 'updates'.");
						continue;
					}
					table.MasterTable = _code.Text;
				}

				var col = TryReadColumn();
				if (col == null) break;
				else table.AddColumn(col);
			}
		}

		private static void ReadTableAttributes(Table table, params string[] endTokens)
		{
			while (!_code.EndOfFile)
			{
				foreach (var endToken in endTokens)
				{
					if (endToken.IsWord())
					{
						if (_code.PeekExactWholeWordI(endToken)) return;
					}
					else
					{
						if (_code.PeekExact(endToken)) return;
					}
				}

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("updates", StringComparison.OrdinalIgnoreCase)) table.Updates = true;
					else if (word.Equals("display", StringComparison.OrdinalIgnoreCase)) table.Display = true;
					else if (word.Equals("modal", StringComparison.OrdinalIgnoreCase)) table.Modal = true;
					else if (word.Equals("nopick", StringComparison.OrdinalIgnoreCase)) table.NoPick = true;
					else if (word.Equals("pick", StringComparison.OrdinalIgnoreCase)) table.NoPick = false;
					else if (word.Equals("database", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadNumber())
						{
							ReportError(_code.Position, "Expected number to follow 'database'.");
							continue;
						}
						table.DatabaseNumber = int.Parse(_code.Text);
					}
					else if (word.Equals("snapshot", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadNumber())
						{
							ReportError(_code.Position, "Expected number to follow 'snapshot'.");
							continue;
						}
						table.SnapshotFrequency = int.Parse(_code.Text);
					}
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'prompt'.");
							continue;
						}
						table.Prompt = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'comment'.");
							continue;
						}
						table.Comment = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("image", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'image'.");
							continue;
						}
						table.Image = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase))
					{
						table.Description = ReadDescriptionAttribute();
					}
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) table.AddTag(tag);
					}
					else
					{
						ReportError(_code.TokenStartPostion, "Unknown word '{0}' in create table statement.", word);
						return;
					}
				}
			}
		}

		private static void ReadAlterTable()
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected table name to follow 'alter table'.");
				return;
			}
			var tableName = _code.Text;
			Table table;
			if (!_tables.TryGetValue(tableName, out table))
			{
				ReportError(_code.TokenStartPostion, "Table '{0}' has not been defined.", tableName);
				return;
			}

			ReadTableAttributes(table, ";", "before", "after", "add", "alter", "drop", "move");

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(';')) return;
				if (_code.ReadExact(',')) continue;

				var colPos = -1;

				if (_code.ReadExactWholeWordI("before"))
				{
					_code.ReadExactWholeWordI("column");
					var colName = _code.ReadWordR();
					if (string.IsNullOrEmpty(colName))
					{
						ReportError(_code.Position, "Expected column name to follow 'before column'.");
						return;
					}
					colPos = table.GetColumnPosition(colName);
					if (colPos < 0)
					{
						ReportError(_code.TokenStartPostion, "Before column '{0}' does not exist.", colName);
						return;
					}
				}
				else if (_code.ReadExactWholeWordI("after"))
				{
					_code.ReadExactWholeWordI("column");
					var colName = _code.ReadWordR();
					if (string.IsNullOrEmpty(colName))
					{
						ReportError(_code.Position, "Expected column name to follow 'before column'.");
						return;
					}
					colPos = table.GetColumnPosition(colName);
					if (colPos < 0)
					{
						ReportError(_code.TokenStartPostion, "Before column '{0}' does not exist.", colName);
						return;
					}
					colPos++;
				}

				if (_code.ReadExactWholeWordI("add"))
				{
					_code.ReadExactWholeWordI("column");
					var col = TryReadColumn();
					if (col != null)
					{
						if (colPos >= 0) table.InsertColumn(colPos, col);
						else table.AddColumn(col);
					}
				}
				else if (_code.ReadExactWholeWordI("alter"))
				{
					_code.ReadExactWholeWordI("column");

					var colName = _code.ReadWordR();
					if (string.IsNullOrEmpty(colName))
					{
						ReportError(_code.Position, "Expected column name to follow 'alter'.");
						return;
					}
					var col = table.GetColumn(colName);
					if (col == null)
					{
						ReportError(_code.Position, "Alter column '{0}' does not exist.", colName);
						return;
					}

					if (!_code.ReadExactWholeWordI("sametype"))
					{
						var dataType = DataType.TryParse(new DataType.ParseArgs
						{
							Code = _code
							// TODO: add support for typedefs
						});
						if (dataType == null)
						{
							ReportError(_code.Position, "Expected column data type.");
							return;
						}
						col.DataType = dataType;
					}

					ReadColumnAttributes(col);
				}
				else if (_code.ReadExactWholeWordI("drop"))
				{
					_code.ReadExactWholeWordI("column");
					var colName = _code.ReadWordR();
					if (string.IsNullOrEmpty(colName))
					{
						ReportError(_code.Position, "Expected column name to follow 'drop'.");
						return;
					}
					table.DropColumn(colName);
				}
				else if (_code.ReadExactWholeWordI("move"))
				{
					_code.ReadExactWholeWordI("column");

					var colName = _code.ReadWordR();
					if (string.IsNullOrEmpty(colName))
					{
						ReportError(_code.Position, "Expected column name to follow 'drop'.");
						return;
					}

					if (colPos < 0)
					{
						ReportError(_code.TokenStartPostion, "Cannot move column without position column.");
						return;
					}

					table.MoveColumn(colPos, colName);
				}
				else
				{
					ReportError(_code.Position, "Syntax error in 'alter table'.");
					return;
				}
			}
		}
		#endregion

		#region Columns
		private static Column TryReadColumn()
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected column name.");
				return null;
			}
			var name = _code.Text;
			
			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = _code
				// TODO: add support for typedefs
			});
			if (dataType == null)
			{
				ReportError(_code.Position, "Expected column data type.");
				return null;
			}

			var col = new Column(name, dataType);

			ReadColumnAttributes(col);

			return col;
		}

		private static void ReadColumnAttributes(Column col)
		{
			while (!_code.EndOfFile)
			{
				if (_code.PeekExact(',') || _code.PeekExact(';') || _code.PeekExact(')') || _code.PeekExact('}')) break;

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("accel", StringComparison.OrdinalIgnoreCase)) col.Accel = ReadAccelSequence();
					else if (word.Equals("noaudit", StringComparison.OrdinalIgnoreCase)) col.NoAudit = true;
					else if (word.Equals("audit", StringComparison.OrdinalIgnoreCase)) col.NoAudit = false;
					else if (word.Equals("form", StringComparison.OrdinalIgnoreCase)) col.Persist = Column.PersistMode.Form;
					else if (word.Equals("formonly", StringComparison.OrdinalIgnoreCase)) col.Persist = Column.PersistMode.FormOnly;
					else if (word.Equals("tool", StringComparison.OrdinalIgnoreCase)) col.Tool = true;
					else if (word.Equals("endgroup", StringComparison.OrdinalIgnoreCase)) col.EndGroup = true;
					else if (word.Equals("zoom", StringComparison.OrdinalIgnoreCase))
					{
						col.Persist = Column.PersistMode.Zoom;
						if (_code.ReadExactWholeWord("nopersist")) col.Persist = Column.PersistMode.ZoomNoPersist;
					}
					else if (word.Equals("image"))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'image'.");
							continue;
						}
						col.Image = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'prompt'.");
							continue;
						}
						col.Prompt = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'comment'.");
							continue;
						}
						col.Comment = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase))
					{
						var sb = new StringBuilder();
						while (_code.ReadStringLiteral())
						{
							if (sb.Length > 0) sb.AppendLine();
							sb.Append(CodeParser.StringLiteralToString(_code.Text));
						}
						col.Description = sb.ToString();
					}
					else if (word.Equals("group", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'group'.");
							continue;
						}
						col.Group = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("row", StringComparison.OrdinalIgnoreCase) ||
						word.Equals("col", StringComparison.OrdinalIgnoreCase))
					{
						if (_code.ReadExact('+') || _code.ReadExact('-')) { }
						_code.ReadNumber();
					}
					else if (word.Equals("rows", StringComparison.OrdinalIgnoreCase) ||
						word.Equals("cols", StringComparison.OrdinalIgnoreCase))
					{
						_code.ReadNumber();
					}
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) col.AddTag(tag);
					}
					else
					{
						ReportError(_code.TokenStartPostion, "Unknown word '{0}' in column definition.", word);
						break;
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in column definition.");
					break;
				}
			}
		}
		#endregion

		private static string ReadAccelSequence()
		{
			var sb = new StringBuilder();

			var expectingDelim = false;

			while (!_code.EndOfFile)
			{
				if (expectingDelim)
				{
					if (_code.PeekExact('+'))
					{
						sb.Append("+");
						_code.MovePeeked();
						expectingDelim = false;
					}
					else break;
				}
				else
				{
					_code.Peek();
					if (_code.Type == CodeType.Word || _code.Type == CodeType.Number)
					{
						sb.Append(_code.Text);
						_code.MovePeeked();
						expectingDelim = true;
					}
					else break;
				}
			}

			return sb.ToString();
		}

		private static void ReadAlterApplication()
		{
			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{')) break;

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("AppIID", StringComparison.OrdinalIgnoreCase) ||
						word.Equals("prompt", StringComparison.OrdinalIgnoreCase) ||
						word.Equals("comment", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal after '{0}'.", word);
							continue;
						}
					}
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase))
					{
						while (_code.ReadStringLiteral()) ;
					}
					else if (word.Equals("langid", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadNumber())
						{
							ReportError(_code.Position, "Expected numeric literal after '{0}'.", word);
							continue;
						}
					}
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase)) ReadTagAttribute();
					else
					{
						ReportError(_code.TokenStartPostion, "Unexpected word '{0}' in 'alter application' statement.", word);
						break;
					}
				}
				else
				{
					ReportError(_code.Position, "Expected keyword in 'alter application' statement.");
					break;
				}
			}

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				if (_code.ReadExactWholeWord("extends"))
				{
					if (_code.ReadExactWholeWord("AppIID"))
					{
						_code.ReadStringLiteral();
					}
				}

				if (_code.ReadExact(')') || _code.ReadExact('}'))
				{
				}
			}
		}

		private static void ReadStringdef(bool create, bool alter)
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected identifier after 'create stringdef'.");
				return;
			}
			var name = _code.Text;

			string value = null;
			string desc = null;
			List<Tag> tags = null;
			while (!_code.EndOfFile)
			{
				if (_code.PeekExact(';')) break;

				if (_code.ReadStringLiteral())
				{
					var langText = CodeParser.StringLiteralToString(_code.Text);
					var langId = 0;

					if (_code.ReadNumber())
					{
						langId = int.Parse(_code.Text);
					}

					if (value == null || langId == 0) value = langText;
				}
				else if (_code.ReadExactWholeWord("description"))
				{
					var sb = new StringBuilder();
					while (_code.ReadStringLiteral())
					{
						if (sb.Length > 0) sb.AppendLine();
						sb.Append(CodeParser.StringLiteralToString(_code.Text));
					}
					desc = sb.ToString();
				}
				else if (_code.ReadExactWholeWord("tag"))
				{
					var tag = ReadTagAttribute();
					if (tag != null)
					{
						if (tags == null) tags = new List<Tag>();
						tags.Add(tag);
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in 'create stringdef'.");
					break;
				}
			}

			if (value == null) value = string.Empty;
			if (desc == null) desc = string.Empty;

			var sd = new Stringdef(name, value, desc);
			_stringdefs[name] = sd;
			if (tags != null)
			{
				foreach (var t in tags) sd.AddTag(t);
			}

			_code.ReadExact(';');
		}

		private static Tag ReadTagAttribute()
		{
			if (!_code.ReadTagName())
			{
				ReportError(_code.Position, "Expected tag name to follow 'tag'.");
				return null;
			}
			var tagName = _code.Text;

			if (!_code.ReadStringLiteral())
			{
				ReportError(_code.Position, "Expected string literal to follow tag name.");
				return null;
			}
			var tagValue = CodeParser.StringLiteralToString(_code.Text);

			return new Tag(tagName, tagValue);
		}

		private static string ReadDescriptionAttribute()
		{
			var sb = new StringBuilder();
			while (_code.ReadStringLiteral())
			{
				if (sb.Length > 0) sb.AppendLine();
				sb.Append(CodeParser.StringLiteralToString(_code.Text));
			}
			return sb.ToString();
		}

		private static void ReadCreateUnique()
		{
			var primary = false;
			var nopick = false;

			var word = _code.PeekWordR();
			if (word.Equals("primary", StringComparison.OrdinalIgnoreCase))
			{
				primary = true;
				_code.MovePeeked();
				word = _code.PeekWordR();
			}
			else if (word.Equals("NOPICK", StringComparison.OrdinalIgnoreCase))
			{
				nopick = true;
				_code.MovePeeked();
				word = _code.PeekWordR();
			}

			if (!word.Equals("index", StringComparison.OrdinalIgnoreCase))
			{
				ReportError(_code.Position, "Expected 'index' to follow 'create unique'.");
				return;
			}
			_code.MovePeeked();

			ReadCreateIndex(true, primary, nopick);
		}

		private static void ReadCreatePrimary()
		{
			if (!_code.PeekWordR().Equals("index", StringComparison.OrdinalIgnoreCase))
			{
				ReportError(_code.Position, "Expected 'index' to follow 'create primary'.");
				return;
			}
			_code.MovePeeked();

			ReadCreateIndex(false, true, false);
		}

		private static void ReadCreateNoPick()
		{
			if (!_code.PeekWordR().Equals("index", StringComparison.OrdinalIgnoreCase))
			{
				ReportError(_code.Position, "Expected 'index' to follow 'create NOPICK'.");
				return;
			}
			_code.MovePeeked();

			ReadCreateIndex(false, false, true);
		}

		private static void ReadCreateIndex(bool unique, bool primary, bool nopick)
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected index name to follow 'index'.");
				return;
			}
			var indexName = _code.Text;

			if (!_code.ReadExactWholeWordI("on"))
			{
				ReportError(_code.Position, "Expected 'on' to follow index name.");
				return;
			}

			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected index table name to follow 'on'.");
				return;
			}
			var tableName = _code.Text;

			var relind = new RelInd(true, indexName, tableName);
			_relinds[indexName] = relind;
			relind.Unique = unique;
			relind.Primary = primary;
			relind.NoPick = nopick;

			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{')) break;

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("description", StringComparison.OrdinalIgnoreCase))
					{
						relind.Description = ReadDescriptionAttribute();
					}
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) relind.AddTag(tag);
					}
					else
					{
						ReportError(_code.TokenStartPostion, "Unexpected '{0}' in 'create index' statement.", word);
						break;
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in 'create index' statement.");
					break;
				}
			}

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				while (!_code.EndOfFile)
				{
					if (_code.ReadExact(')') || _code.ReadExact('}')) break;
					if (_code.ReadExact(',')) continue;

					if (_code.ReadWord())
					{
						relind.AddSortColumn(_code.Text);
					}
					else
					{
						ReportError(_code.Position, "Syntax error in 'create index' columns.");
					}
				}
			}
		}

		private static void ReadCreateRelationship()
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected relationship name to follow 'create relationship'.");
				return;
			}
			var name = _code.Text;

			if (!_code.ReadNumber())
			{
				ReportError(_code.Position, "Expected schema number name to follow 'create relationship' name.");
				return;
			}
			var number = int.Parse(_code.Text);

			var relind = new RelInd(false, name, string.Empty);
			_relinds[name] = relind;
			relind.Number = number;

			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{')) break;

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("updates", StringComparison.OrdinalIgnoreCase)) relind.Updates = true;
					else if (word.Equals("nopick", StringComparison.OrdinalIgnoreCase)) relind.NoPick = true;
					else if (word.Equals("pick", StringComparison.OrdinalIgnoreCase)) relind.NoPick = false;
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'prompt'.");
							continue;
						}
						relind.Prompt = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadStringLiteral())
						{
							ReportError(_code.Position, "Expected string literal to follow 'comment'.");
							continue;
						}
						relind.Comment = CodeParser.StringLiteralToString(_code.Text);
					}
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase))
					{
						relind.Description = ReadDescriptionAttribute();
					}
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) relind.AddTag(tag);
					}
					else if (word.Equals("one", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadWord())
						{
							ReportError(_code.Position, "Expected parent table name to follow 'one' in 'creation relationship' statement.");
							continue;
						}
						var parentName = _code.Text;

						_code.ReadExactWholeWordI("to");
						if (_code.ReadExactWholeWordI("one"))
						{
							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'one' in 'creation relationship' statement.");
								continue;
							}
							var childName = _code.Text;

							relind.LinkDesc = string.Format("one {0} to one {1}", parentName, childName);
						}
						else if (_code.ReadExactWholeWordI("many"))
						{
							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'one' in 'creation relationship' statement.");
								continue;
							}
							var childName = _code.Text;

							relind.LinkDesc = string.Format("one {0} to many {1}", parentName, childName);
						}
						else
						{
							ReportError(_code.Position, "Syntax error in 'creation relationship' statement after 'one'.");
							continue;
						}
					}
					else if (word.Equals("many", StringComparison.OrdinalIgnoreCase))
					{
						if (!_code.ReadWord())
						{
							ReportError(_code.Position, "Expected parent table name to follow 'many' in 'creation relationship' statement.");
							continue;
						}
						var parentName = _code.Text;

						_code.ReadExactWholeWordI("to");
						if (_code.ReadExactWholeWordI("many"))
						{
							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'many' in 'creation relationship' statement.");
								continue;
							}
							var childName = _code.Text;

							relind.LinkDesc = string.Format("many {0} to many {1}", parentName, childName);
						}
						else
						{
							ReportError(_code.Position, "Syntax error in 'creation relationship' statement after 'many'.");
							continue;
						}
					}
					else if (word.Equals("order", StringComparison.OrdinalIgnoreCase))
					{
						if (_code.ReadExactWholeWordI("by"))
						{
							var sb = new StringBuilder();
							sb.Append("order by");
							if (_code.ReadExactWholeWordI("unique")) sb.Append(" unique");

							while (_code.ReadWord())
							{
								sb.Append(' ');
								sb.Append(_code.Text);
							}

							relind.OrderByDesc = sb.ToString();
						}
					}
					else
					{
						ReportError(_code.Position, "Unrecognized word '{0}' in 'creation relationship' statement.", word);
						break;
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in 'create relationship' statement.");
					break;
				}
			}

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				while (!_code.EndOfFile)
				{
					if (_code.ReadExact(')') || _code.ReadExact('}')) break;
					if (_code.ReadExact(',') || _code.ReadExact(';')) continue;

					var col = TryReadColumn();
					if (col == null) break;
					else relind.AddColumn(col);
				}
			}

		}

	}
}
