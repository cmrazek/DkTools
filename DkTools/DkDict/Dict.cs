using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	static class Dict
	{
		private static CodeParser _code;
		private static CodeSource _source;
		private static Dictionary<string, Table> _tables = new Dictionary<string, Table>();
		private static Dictionary<string, RelInd> _relinds = new Dictionary<string, RelInd>();
		private static Dictionary<string, Stringdef> _stringdefs = new Dictionary<string, Stringdef>();
		private static Dictionary<string, Typedef> _typedefs = new Dictionary<string, Typedef>();
		private static Dictionary<string, Interface> _interfaces = new Dictionary<string, Interface>();
		private static Dictionary<string, Interface> _impliedInterfaces = new Dictionary<string, Interface>();

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

				var dictContent = _source.Text;
				File.WriteAllText(Path.Combine(ProbeToolsPackage.AppDataDir, "dict.txt"), dictContent);

				_code = new CodeParser(dictContent);
				ReadDict();

				if (!_relinds.ContainsKey("physical")) _relinds.Add("physical", new RelInd(RelIndType.Index, "physical", 0, string.Empty, FilePosition.Empty));

				_source = null;	// So it can be GC'd

				elapsed = DateTime.Now.Subtract(curTime);
				Log.Write(LogLevel.Info, "DICT parsing complete. (elapsed: {0})", elapsed);

				LoadDkRepo();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error when reading DICT file.");
			}
		}

		private static void LoadDkRepo()
		{
			Log.Info("Loading WBDK Repository.");
			var startTime = DateTime.Now;

			DICTSRVRLib.PRepository repo = null;
			DICTSRVRLib.PDictionary dict = null;
			try
			{
				var appName = ProbeEnvironment.CurrentApp;
				repo = new DICTSRVRLib.PRepository();
				dict = repo.LoadDictionary(appName, string.Empty, DICTSRVRLib.PDS_Access.Access_READ);
				if (dict == null)
				{
					Log.Info("The WBDK repository for app '{0}' does not exist.", appName);
					return;
				}

				for (int i = 1, ii = dict.InterfaceTypeCount; i <= ii; i++)
				{
					var intf = dict.InterfaceTypes[i];
					var intfName = intf.Name;

					Interface existingIntf;
					if (_interfaces.TryGetValue(intfName, out existingIntf))
					{
						existingIntf.LoadFromRepo(intf);
					}
					else
					{
						existingIntf = new Interface(intfName, FilePosition.Empty);
						existingIntf.LoadFromRepo(intf);
						_interfaces[intfName] = existingIntf;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error when loading WBDK repository.");
			}
			finally
			{
				if (dict != null)
				{
					Marshal.ReleaseComObject(dict);
					dict = null;
				}
				if (repo != null)
				{
					Marshal.ReleaseComObject(repo);
					repo = null;
				}
			}

			var elapsed = DateTime.Now.Subtract(startTime);
			Log.Info("WBDK repository loading complete. (elapsed: {0})", elapsed);
		}

		private static void ReportError(int pos, string message)
		{
			Log.Write(LogLevel.Warning, "DICT offset {1}: {0}", message, pos);
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
							else if (word.Equals("drop", StringComparison.OrdinalIgnoreCase))
							{
								ReadDrop();
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

			AddImplicitTables();
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
				else if (word.Equals("nopick", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateNoPick();
				}
				else if (word.Equals("primary", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreatePrimary();
				}
				else if (word.Equals("relationship", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateRelationship();
				}
				else if (word.Equals("time", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateTime();
				}
				else if (word.Equals("interfacetype", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateInterfaceType();
				}
				else if (word.Equals("workspace", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateWorkspace();
				}
				else if (word.Equals("typedef", StringComparison.OrdinalIgnoreCase))
				{
					ReadCreateTypedef();
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
				else if (word.Equals("workspace", StringComparison.OrdinalIgnoreCase))
				{
					ReadAlterWorkspace();
				}
				else if (word.Equals("typedef", StringComparison.OrdinalIgnoreCase))
				{
					ReadAlterTypedef();
				}
				else
				{
					ReportError(_code.TokenStartPostion, "Unrecognized word '{0}' after 'alter'.", _code.Text);
				}
			}
		}

		private static void ReadDrop()
		{
			if (_code.ReadWord())
			{
				var word = _code.Text;
				if (word.Equals("index", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropIndex();
				}
				else if (word.Equals("table", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropTable();
				}
				else if (word.Equals("relationship", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropRelationship();
				}
				else if (word.Equals("time", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropTime();
				}
				else if (word.Equals("workspace", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropWorkspace();
				}
				else if (word.Equals("interfacetype", StringComparison.OrdinalIgnoreCase))
				{
					ReadDropInterfaceType();
				}
				else
				{
					ReportError(_code.TokenStartPostion, "Unrecognized word '{0}' after 'drop'.", _code.Text);
				}
			}
		}

		public static IEnumerable<Definition> AllDictDefinitions
		{
			get
			{
				foreach (var table in _tables.Values)
				{
					foreach (var def in table.Definitions) yield return def;
				}
				foreach (var relind in _relinds.Values) yield return relind.Definition;
				foreach (var sd in _stringdefs.Values) yield return sd.Definition;
				foreach (var td in _typedefs.Values) yield return td.Definition;
				foreach (var intf in _interfaces.Values) yield return intf.Definition;
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
			var namePos = _code.TokenStartPostion;

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

			var nameFilePos = _source.GetFilePosition(namePos);
			var table = new Table(tableName, tableNum, tableNum2, nameFilePos);
			_tables[tableName] = table;

			// Implicit columns
			table.AddColumn(new Column(tableName, "rowno", DataType.Unsigned9, nameFilePos, true));

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
					continue;
				}

				var col = TryReadColumn(tableName);
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
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) table.Prompt = ReadPromptOrCommentAttribute();
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) table.Comment = ReadPromptOrCommentAttribute();
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
				else
				{
					ReportError(_code.Position, "Expected table attribute.");
					return;
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
				RelInd relind;
				if (!_relinds.TryGetValue(tableName, out relind))
				{
					ReportError(_code.TokenStartPostion, "Table '{0}' has not been defined.", tableName);
					return;
				}
				else table = relind;
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
					var col = TryReadColumn(tableName);
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
							Code = _code,
							DataTypeCallback = (name) =>
								{
									Typedef td;
									if (_typedefs.TryGetValue(name, out td)) return td.Definition;
									return null;
								},
							AllowTags = true
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

		private static void ReadDropTable()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected index name after 'drop index'.");
				return;
			}

			Table table;
			if (!_tables.TryGetValue(name, out table))
			{
				ReportError(_code.TokenStartPostion, "Dropped table '{0}' does not exist.", name);
				return;
			}

			_tables.Remove(name);
		}

		public static Table GetTable(string tableName)
		{
			Table table;
			if (_tables.TryGetValue(tableName, out table)) return table;

			if (tableName.Length >= 2 && char.IsDigit(tableName[tableName.Length - 1]))
			{
				var baseTableName = tableName.Substring(0, tableName.Length - 1);
				if (_tables.TryGetValue(baseTableName, out table)) return table;
			}

			return null;
		}

		public static IEnumerable<Table> Tables
		{
			get { return _tables.Values; }
		}

		public static bool IsTable(string name)
		{
			return _tables.ContainsKey(name);
		}

		private static void AddImplicitTables()
		{
			Table updates;
			FilePosition updatesFilePos = FilePosition.Empty;
			if (!_tables.TryGetValue("updates", out updates))
			{
				updates = new Table("updates", 399, 0, updatesFilePos);
				_tables["updates"] = updates;
			}
			else
			{
				updatesFilePos = updates.FilePosition;
			}

			// Special implicit columns for updates
			updates.AddColumn(new Column("updates", "column", new DataType(ValType.String, null, "char(30)"), updatesFilePos, true));
			updates.AddColumn(new Column("updates", "tablename", new DataType(ValType.String, null, "char(8)"), updatesFilePos, true));
			updates.AddColumn(new Column("updates", "urowno", DataType.Unsigned9, updatesFilePos, true));
			updates.AddColumn(new Column("updates", "entered", DataType.Date, updatesFilePos, true));
			updates.AddColumn(new Column("updates", "new", DataType.Char255, updatesFilePos, true));
			updates.AddColumn(new Column("updates", "old", DataType.Char255, updatesFilePos, true));
			updates.AddColumn(new Column("updates", "seqno_updatesix", DataType.Unsigned9, updatesFilePos, true));

			if (!_relinds.ContainsKey("updatesix"))
			{
				var updatesix = new RelInd(RelIndType.Index, "updatesix", 0, "updates", updatesFilePos);
				updatesix.AddSortColumn("tablename");
				updatesix.AddSortColumn("urowno");
				updatesix.AddSortColumn("entered");
				updatesix.AddSortColumn("seqno_updatesix");
				_relinds["updatesix"] = updatesix;
			}
		}
		#endregion

		#region Columns
		private static Column TryReadColumn(string tableName)
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected column name.");
				return null;
			}
			var colName = _code.Text;
			var colNamePos = _code.TokenStartPostion;
			
			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = (name) =>
				{
					Typedef td;
					if (_typedefs.TryGetValue(name, out td)) return td.Definition;
					return null;
				},
				AllowTags = true
			});
			if (dataType == null)
			{
				ReportError(_code.Position, "Expected column data type.");
				return null;
			}

			var col = new Column(tableName, colName, dataType, _source.GetFilePosition(colNamePos), false);

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
					else if (word.Equals("nopersist", StringComparison.OrdinalIgnoreCase))
					{
						if (col.Persist == Column.PersistMode.Form) col.Persist = Column.PersistMode.FormOnly;
						else if (col.Persist == Column.PersistMode.Zoom) col.Persist = Column.PersistMode.ZoomNoPersist;
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
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) col.Prompt = ReadPromptOrCommentAttribute();
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) col.Comment = ReadPromptOrCommentAttribute();
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase)) col.Description = ReadDescriptionAttribute();
					else if (word.Equals("group", StringComparison.OrdinalIgnoreCase)) col.Group = ReadPromptOrCommentAttribute();
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
					else if (word.Equals("custom", StringComparison.OrdinalIgnoreCase))
					{
						if (_code.ReadStringLiteral())
						{
							col.CustomProgId = CodeParser.StringLiteralToString(_code.Text);

							if (_code.ReadStringLiteral())
							{
								col.CustomLicense = CodeParser.StringLiteralToString(_code.Text);
							}
						}
						else
						{
							ReportError(_code.Position, "Expected prog ID to follow 'custom'.");
							continue;
						}
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

		#region Application
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
						ReadPromptOrCommentAttribute();
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
		#endregion

		#region Stringdef
		private static void ReadStringdef(bool create, bool alter)
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected identifier after 'create stringdef'.");
				return;
			}
			var name = _code.Text;
			var namePos = _code.Position;

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

			var nameFilePos = _source.GetFilePosition(namePos);

			var sd = new Stringdef(name, value, desc, nameFilePos);
			_stringdefs[name] = sd;
			if (tags != null)
			{
				foreach (var t in tags) sd.AddTag(t);
			}

			_code.ReadExact(';');
		}

		public static Stringdef GetStringdef(string name)
		{
			Stringdef sd;
			if (_stringdefs.TryGetValue(name, out sd)) return sd;
			return null;
		}

		public static IEnumerable<Stringdef> Stringdefs
		{
			get { return _stringdefs.Values; }
		}
		#endregion

		#region Typedef
		private static void ReadCreateTypedef()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected typedef name to follow 'create typedef'.");
				return;
			}

			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = _code,
				AllowTags = true
			});
			if (dataType == null)
			{
				ReportError(_code.Position, "Expected typedef data type.");
				return;
			}

			_typedefs[name] = new Typedef(name, dataType);
		}

		private static void ReadAlterTypedef()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected typedef name to follow 'alter typedef'.");
				return;
			}

			// Alter typedef is not supported at this time
		}

		public static Typedef GetTypedef(string name)
		{
			Typedef td;
			if (_typedefs.TryGetValue(name, out td)) return td;
			return null;
		}

		public static IEnumerable<Typedef> Typedefs
		{
			get { return _typedefs.Values; }
		}
		#endregion

		#region Indexes / Relationships
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
			var namePos = _code.TokenStartPostion;

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

			var relind = new RelInd(RelIndType.Index, indexName, 0, tableName, _source.GetFilePosition(namePos));
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
						break;
					}
				}
			}

			var table = GetTable(tableName);
			if (table != null) table.AddRelInd(relind);
		}

		private static void ReadCreateRelationship()
		{
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected relationship name to follow 'create relationship'.");
				return;
			}
			var name = _code.Text;
			var namePos = _code.TokenStartPostion;

			if (!_code.ReadNumber())
			{
				ReportError(_code.Position, "Expected schema number name to follow 'create relationship' name.");
				return;
			}
			var number = int.Parse(_code.Text);

			var nameFilePos = _source.GetFilePosition(namePos);
			var relind = new RelInd(RelIndType.Relationship, name, number, string.Empty, nameFilePos);
			_relinds[name] = relind;

			Table parentTable = null;
			Table childTable = null;
			var parentMany = false;
			var childMany = false;

			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{')) break;

				if (_code.ReadWord())
				{
					var word = _code.Text;
					if (word.Equals("updates", StringComparison.OrdinalIgnoreCase)) relind.Updates = true;
					else if (word.Equals("nopick", StringComparison.OrdinalIgnoreCase)) relind.NoPick = true;
					else if (word.Equals("pick", StringComparison.OrdinalIgnoreCase)) relind.NoPick = false;
					else if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) relind.Prompt = ReadPromptOrCommentAttribute();
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) relind.Comment = ReadPromptOrCommentAttribute();
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
						parentMany = false;

						if (!_code.ReadWord())
						{
							ReportError(_code.Position, "Expected parent table name to follow 'one' in 'creation relationship' statement.");
							break;
						}
						var parentName = _code.Text;
						if (!_tables.TryGetValue(parentName, out parentTable))
						{
							ReportError(_code.TokenStartPostion, "Parent table '{0}' does not exist in 'create relationship'.", parentName);
							break;
						}

						_code.ReadExactWholeWordI("to");
						if (_code.ReadExactWholeWordI("one"))
						{
							childMany = false;

							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'one' in 'creation relationship' statement.");
								break;
							}
							var childName = _code.Text;
							childTable = GetTable(childName);
							if (childTable == null)
							{
								ReportError(_code.TokenStartPostion, "Child table '{0}' does not exist in 'create relationship'.", childName);
								break;
							}

							relind.LinkDesc = string.Format("one {0} to one {1}", parentName, childName);

							// WBDK seems to tolerate ending the statement here
							if (!_code.PeekExactWholeWordI("order") && !_code.PeekExact('(') && !_code.PeekExact('{')) break;
						}
						else if (_code.ReadExactWholeWordI("many"))
						{
							childMany = true;

							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'one' in 'creation relationship' statement.");
								break;
							}
							var childName = _code.Text;
							childTable = GetTable(childName);
							if (childTable == null)
							{
								ReportError(_code.TokenStartPostion, "Child table '{0}' does not exist in 'create relationship'.", childName);
								break;
							}

							relind.LinkDesc = string.Format("one {0} to many {1}", parentName, childName);

							// WBDK seems to tolerate ending the statement here
							if (!_code.PeekExactWholeWordI("order") && !_code.PeekExact('(') && !_code.PeekExact('{')) break;
						}
						else
						{
							ReportError(_code.Position, "Syntax error in 'creation relationship' statement after 'one'.");
							break;
						}
					}
					else if (word.Equals("many", StringComparison.OrdinalIgnoreCase))
					{
						parentMany = true;

						if (!_code.ReadWord())
						{
							ReportError(_code.Position, "Expected parent table name to follow 'many' in 'creation relationship' statement.");
							break;
						}
						var parentName = _code.Text;
						if (!_tables.TryGetValue(parentName, out parentTable))
						{
							ReportError(_code.TokenStartPostion, "Parent table '{0}' does not exist in 'create relationship'.", parentName);
							break;
						}

						_code.ReadExactWholeWordI("to");
						if (_code.ReadExactWholeWordI("many"))
						{
							childMany = true;

							if (!_code.ReadWord())
							{
								ReportError(_code.Position, "Expected child table name to follow 'many' in 'creation relationship' statement.");
								break;
							}
							var childName = _code.Text;
							childTable = GetTable(childName);
							if (childTable == null)
							{
								ReportError(_code.TokenStartPostion, "Child table '{0}' does not exist in 'create relationship'.", childName);
								break;
							}

							relind.LinkDesc = string.Format("many {0} to many {1}", parentName, childName);

							// WBDK seems to tolerate ending the statement here
							if (!_code.PeekExactWholeWordI("order") && !_code.PeekExact('(') && !_code.PeekExact('{')) break;
						}
						else
						{
							ReportError(_code.Position, "Syntax error in 'creation relationship' statement after 'many'.");
							break;
						}
					}
					else if (word.Equals("order", StringComparison.OrdinalIgnoreCase))
					{
						if (_code.ReadExactWholeWordI("by"))
						{
							if (childTable == null)
							{
								ReportError(_code.TokenStartPostion, "Found 'order by' before parent/child relationship in 'create relationship'.");
								break;
							}

							_code.ReadExactWholeWordI("unique");

							while (!_code.EndOfFile)
							{
								if (_code.PeekExact('(') || _code.PeekExact('{')) break;
								if (_code.ReadExact(',')) continue;

								var colName = _code.ReadWordR();
								if (!string.IsNullOrEmpty(word) && childTable.GetColumn(colName) != null) relind.AddSortColumn(colName);
								else break;
							}
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

					var col = TryReadColumn(name);
					if (col == null) break;
					else relind.AddColumn(col);
				}
			}

			var hasTable = false;
			if (relind.Columns.Any())
			{
				_tables[relind.Name] = relind;
				hasTable = true;
			}

			if (parentTable != null && childTable != null)
			{
				parentTable.AddRelInd(relind);

				if (parentMany)
				{
					// child must be many if parent is many

					parentTable.AddColumn(new Column(parentTable.Name, string.Concat("has_", name, "_", childTable.Name),
						DataType.Unsigned2, nameFilePos, true));

					childTable.AddColumn(new Column(childTable.Name, string.Concat("rowno_", name, "_", parentTable.Name),
						DataType.Unsigned9, nameFilePos, true));

					if (hasTable)
					{
						relind.AddColumn(new Column(name, string.Concat("rowno_", name, "_", parentTable.Name),
							DataType.Unsigned9, nameFilePos, true));

						relind.AddColumn(new Column(name, string.Concat("rowno_", name, "_", childTable.Name, "2"),
							DataType.Unsigned9, nameFilePos, true));
					}
				}
				else // one parent
				{
					if (childMany)
					{
						parentTable.AddColumn(new Column(parentTable.Name, string.Concat("has_", name, "_", childTable.Name),
							DataType.Unsigned2, nameFilePos, true));

						childTable.AddColumn(new Column(childTable.Name, string.Concat("rowno_", name, "_", parentTable.Name),
							DataType.Unsigned9, nameFilePos, true));
					}
					else // one child
					{
						parentTable.AddColumn(new Column(parentTable.Name, string.Concat("rowno_", name, "_", childTable.Name),
							DataType.Unsigned2, nameFilePos, true));

						childTable.AddColumn(new Column(childTable.Name, string.Concat("rowno_", name, "_", parentTable.Name),
							DataType.Unsigned9, nameFilePos, true));
					}
				}
			}
		}

		private static void ReadCreateTime()
		{
			if (!_code.ReadExactWholeWordI("relationship"))
			{
				ReportError(_code.Position, "Expected 'relationship' to follow 'create time'.");
				return;
			}

			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected relationship name to follow 'create time relationship'.");
				return;
			}
			var namePos = _code.TokenStartPostion;

			if (!_code.ReadNumber())
			{
				ReportError(_code.Position, "Expected schema number in 'create time relationship'.");
				return;
			}
			var number = int.Parse(_code.Text);

			var nameFilePos = _source.GetFilePosition(namePos);
			var relind = new RelInd(RelIndType.TimeRelationship, name, number, string.Empty, nameFilePos);
			_relinds[name] = relind;

			Table masterTable = null;
			Table historyTable = null;

			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{')) break;

				var word = _code.ReadWordR();
				if (!string.IsNullOrEmpty(word))
				{
					if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) relind.Prompt = ReadPromptOrCommentAttribute();
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) relind.Comment = ReadPromptOrCommentAttribute();
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase)) relind.Description = ReadDescriptionAttribute();
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) relind.AddTag(tag);
					}
					else if (word.Equals("order", StringComparison.OrdinalIgnoreCase))
					{
						if (historyTable == null)
						{
							ReportError(_code.TokenStartPostion, "Found 'order' before master/history tables in 'create time relationship'.");
							break;
						}

						_code.ReadExactWholeWordI("by");

						while (!_code.EndOfFile)
						{
							if (_code.PeekExact('(') || _code.PeekExact('{')) break;
							if (_code.ReadExact(',')) continue;

							var colName = _code.PeekWordR();
							if (historyTable.GetColumn(colName) != null)
							{
								_code.MovePeeked();
								relind.AddSortColumn(colName);
							}
							else break;
						}

						break;
					}
					else if (_tables.TryGetValue(word, out masterTable))
					{
						var sb = new StringBuilder();
						sb.Append(word);

						_code.ReadExactWholeWordI("to");
						sb.Append(" to ");

						var historyTableName = _code.ReadWordR();
						if ((historyTable = GetTable(historyTableName)) != null)
						{
							sb.Append(historyTableName);
							relind.LinkDesc = sb.ToString();
						}
						else
						{
							ReportError(_code.TokenStartPostion, "Expected history table name to follow 'to' in 'create time relationship'.");
							break;
						}
					}
					else
					{
						ReportError(_code.TokenStartPostion, "Unrecognized word '{0}' in 'create time relationship'.");
						break;
					}
				}
				else
				{
					ReportError(_code.TokenStartPostion, "Syntax error in 'create time relationship'.");
					break;
				}
			}

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				if (!_code.ReadExact(')')) _code.ReadExact('}');
			}

			if (masterTable != null && historyTable != null)
			{
				masterTable.AddRelInd(relind);

				masterTable.AddColumn(new Column(masterTable.Name, string.Concat("has_", name, "_", historyTable.Name),
					DataType.Unsigned2, nameFilePos, true));

				if (masterTable.GetColumn("entered") == null)
				{
					masterTable.AddColumn(new Column(masterTable.Name, "entered", DataType.Date, nameFilePos, true));
				}

				if (masterTable.GetColumn("next_effective") == null)
				{
					masterTable.AddColumn(new Column(masterTable.Name, "next_effective", DataType.Date, nameFilePos, true));
				}

				var sb = new StringBuilder();
				sb.Append("enum { \" \"");
				var completionOptions = new List<string>();
				completionOptions.Add("\" \"");
				foreach (var col in masterTable.Columns)
				{
					sb.Append(", ");
					sb.Append(col.Name);
					completionOptions.Add(col.Name);
				}
				sb.Append(" }");
				var fieldDataType = sb.ToString();
				historyTable.AddColumn(new Column(historyTable.Name, "field",
					new DataType(ValType.Enum, string.Format("like {0}.field", historyTable.Name), fieldDataType, completionOptions),
					nameFilePos, true));

				historyTable.AddColumn(new Column(historyTable.Name, "value", new DataType(ValType.String, "char(255)", "char(255)"), nameFilePos, true));

				historyTable.AddColumn(new Column(historyTable.Name, string.Concat("rowno_", name, "_", masterTable.Name), DataType.Unsigned9, nameFilePos, true));

				historyTable.AddColumn(new Column(historyTable.Name, string.Concat("seqno_", name, "_", historyTable.Name), DataType.Unsigned9, nameFilePos, true));

				historyTable.AddColumn(new Column(historyTable.Name, "tdm_flags", DataType.Unsigned2, nameFilePos, true));

				// Create snapshot table
				var snapName = string.Concat(masterTable.Name, "s");
				if (!_tables.ContainsKey(snapName))
				{
					var snapTable = new Table(snapName, 0, 0, masterTable.FilePosition);

					snapTable.AddColumn(new Column(snapName, "rowno", DataType.Unsigned9, nameFilePos, true));

					foreach (var col in masterTable.Columns)
					{
						if (!col.Implicit)
						{
							snapTable.AddColumn(new Column(snapName, col.Name, col.DataType, col.FilePosition, false));
						}
					}

					if (snapTable.GetColumn("entered") == null)
					{
						snapTable.AddColumn(new Column(snapName, "entered", DataType.Date, nameFilePos, true));
					}

					if (snapTable.GetColumn("next_effective") == null)
					{
						snapTable.AddColumn(new Column(snapName, "next_effective", DataType.Date, nameFilePos, true));
					}

					if (snapTable.GetColumn("snap_option") == null)
					{
						snapTable.AddColumn(new Column(snapName, "snap_option", DataType.Unsigned2, nameFilePos, true));
					}

					snapTable.AddColumn(new Column(snapName, string.Concat("rowno_", masterTable.Name, "r_", masterTable.Name), DataType.Unsigned9, nameFilePos, true));

					_tables[snapName] = snapTable;

					var snapRelName = string.Concat(masterTable.Name, "r");
					if (!_relinds.ContainsKey(snapRelName))
					{
						_relinds.Add(snapRelName, new RelInd(RelIndType.TimeRelationship, snapRelName, 0, string.Empty, nameFilePos));
					}
				}

			}
		}

		private static void ReadDropIndex()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected index name after 'drop index'.");
				return;
			}
			var namePos = _code.TokenStartPostion;

			if (!_code.ReadExactWholeWordI("on"))
			{
				ReportError(_code.Position, "Expected 'on' to follow dropped index name.");
				return;
			}

			var tableName = _code.ReadWordR();
			if (string.IsNullOrEmpty(tableName))
			{
				ReportError(_code.Position, "Expected 'on tablename' to follow dropped index name.");
				return;
			}

			RelInd relind;
			if (!_relinds.TryGetValue(name, out relind))
			{
				ReportError(namePos, "Dropped index '{0}' does not exist.", name);
				return;
			}

			_relinds.Remove(name);
		}

		private static void ReadDropRelationship()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected relationship name after 'drop relationship'.");
				return;
			}

			RelInd relind;
			if (!_relinds.TryGetValue(name, out relind))
			{
				ReportError(_code.TokenStartPostion, "Dropped relationship '{0}' does not exist.", name);
				return;
			}

			_relinds.Remove(name);
		}

		private static void ReadDropTime()
		{
			if (!_code.ReadExactWholeWordI("relationship"))
			{
				ReportError(_code.Position, "Expected 'relationship' to follow 'drop time'.");
				return;
			}

			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected time relationship name after 'drop time relationship'.");
				return;
			}

			RelInd relind;
			if (!_relinds.TryGetValue(name, out relind))
			{
				ReportError(_code.TokenStartPostion, "Dropped time relationship '{0}' does not exist.", name);
				return;
			}

			_relinds.Remove(name);
		}

		public static RelInd GetRelInd(string name)
		{
			RelInd relind;
			if (_relinds.TryGetValue(name, out relind)) return relind;
			return null;
		}

		public static IEnumerable<RelInd> RelInds
		{
			get { return _relinds.Values; }
		}
		#endregion

		#region Attributes
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

		private static string ReadPromptOrCommentAttribute()
		{
			// WBDK dict parser has a quirk where if there are multiple string separated by a space, then only the last string takes effect.
			var ret = string.Empty;
			var gotString = false;
			while (_code.ReadStringLiteral())
			{
				ret = CodeParser.StringLiteralToString(_code.Text);
				gotString = true;
			}

			if (!gotString)
			{
				if (_code.ReadWord())
				{
					Stringdef sd;
					if (_stringdefs.TryGetValue(_code.Text, out sd))
					{
						ret = sd.Text;
					}
					else
					{
						ret = string.Concat("#", _code.Text, "#");
					}
				}
			}

			return ret;
		}
		#endregion

		#region Interfaces
		private static void ReadCreateInterfaceType()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected interface type name to follow 'create interface'.");
				return;
			}

			var nameFilePos = _source.GetFilePosition(_code.Position);

			var intf = new Interface(name, nameFilePos);
			_interfaces[name] = intf;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(';')) break;

				var word = _code.ReadWordR();
				if (!string.IsNullOrEmpty(word))
				{
					if (word.Equals("path", StringComparison.OrdinalIgnoreCase)) intf.Path = ReadPromptOrCommentAttribute();
					else if (word.Equals("framework", StringComparison.OrdinalIgnoreCase)) intf.Framework = true;
					else if (word.Equals("progid", StringComparison.OrdinalIgnoreCase)) intf.ProgId = ReadPromptOrCommentAttribute();
					else if (word.Equals("clsid", StringComparison.OrdinalIgnoreCase)) intf.ClsId = ReadPromptOrCommentAttribute();
					else if (word.Equals("tlibid", StringComparison.OrdinalIgnoreCase))
					{
						intf.TLibId = ReadPromptOrCommentAttribute();
						if (!_code.ReadExactWholeWordI("major"))
						{
							ReportError(_code.Position, "Expected 'major' to follow 'tlibid' type.");
							break;
						}
						if (!_code.ReadNumber())
						{
							ReportError(_code.Position, "Expected number to follow 'major'.");
							break;
						}
						if (!_code.ReadExactWholeWordI("minor"))
						{
							ReportError(_code.Position, "Expected 'minor' to follow 'major' number.");
							break;
						}
						if (!_code.ReadNumber())
						{
							ReportError(_code.Position, "Expected number to follow 'minor'.");
							break;
						}
					}
					else if (word.Equals("iid", StringComparison.OrdinalIgnoreCase)) intf.Iid = ReadPromptOrCommentAttribute();
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase)) intf.Description = ReadDescriptionAttribute();
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase))
					{
						var tag = ReadTagAttribute();
						if (tag != null) intf.AddTag(tag);
					}
					else if (word.Equals("interface", StringComparison.OrdinalIgnoreCase))
					{
						if (_code.ReadStringLiteral()) intf.InterfaceName = CodeParser.StringLiteralToString(_code.Text);
						else if (_code.ReadWord()) intf.InterfaceName = _code.Text;
						else
						{
							ReportError(_code.Position, "Expected string literal or identifier to follow 'interface'.");
							break;
						}
					}
					else if (word.Equals("default", StringComparison.OrdinalIgnoreCase)) intf.Default = true;
					else if (word.Equals("defaultevent", StringComparison.OrdinalIgnoreCase)) intf.DefaultEvent = true;
					else
					{
						ReportError(_code.TokenStartPostion, "Unrecognized token '{0}' in 'create interfacetype'.", word);
						break;
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in 'create interfacetype'.");
					break;
				}
			}
		}

		public static IEnumerable<Interface> Interfaces
		{
			get { return _interfaces.Values; }
		}

		public static Interface GetInterface(string name)
		{
			Interface intf;
			if (_interfaces.TryGetValue(name, out intf)) return intf;
			return null;
		}

		private static void ReadDropInterfaceType()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected typedef name to follow 'drop interfacetype'.");
				return;
			}

			if (!_interfaces.ContainsKey(name))
			{
				ReportError(_code.TokenStartPostion, "Dropped interface type '{0}' does not exist.", name);
				return;
			}

			_interfaces.Remove(name);
		}

		public static void AddImpliedInterface(string name, Interface intf)
		{
			_impliedInterfaces[name] = intf;
		}

		public static Interface GetImpliedInterface(string name)
		{
			Interface intf;
			if (_impliedInterfaces.TryGetValue(name, out intf)) return intf;
			return null;
		}
		#endregion

		#region Workspaces
		private static void ReadCreateWorkspace()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected workspace name to follow 'create workspace'.");
				return;
			}

			ReadWorkspaceAttributes();

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				while (!_code.EndOfFile)
				{
					if (_code.ReadExact(')') || _code.ReadExact('}')) break;
					if (_code.ReadExact(',')) continue;
					if (!ReadWorkspaceItem()) break;
				}
			}
		}

		private static void ReadAlterWorkspace()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected workspace name to follow 'alter workspace'.");
				return;
			}

			ReadWorkspaceAttributes();

			if (_code.ReadExact('(') || _code.ReadExact('{'))
			{
				while (!_code.EndOfFile)
				{
					if (_code.ReadExact(')') || _code.ReadExact('}')) break;
					if (_code.ReadExact(',')) continue;

					if (_code.ReadExactWholeWordI("add") ||
						_code.ReadExactWholeWordI("alter") ||
						_code.ReadExactWholeWordI("drop"))
					{
						if (!ReadWorkspaceItem()) break;
					}
					else
					{
						ReportError(_code.Position, "Expected 'add', 'alter' or 'drop' in 'alter workspace.");
						break;
					}
				}
			}
		}

		private static void ReadWorkspaceAttributes()
		{
			while (!_code.EndOfFile)
			{
				if (_code.PeekExact('(') || _code.PeekExact('{') || _code.PeekExact(';')) return;

				var word = _code.ReadWordR();
				if (!string.IsNullOrEmpty(word))
				{
					if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) ReadPromptOrCommentAttribute();
					else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) ReadPromptOrCommentAttribute();
					else if (word.Equals("description", StringComparison.OrdinalIgnoreCase)) ReadDescriptionAttribute();
					else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase)) ReadTagAttribute();
					else if (word.Equals("image", StringComparison.OrdinalIgnoreCase)) ReadPromptOrCommentAttribute();
					else
					{
						ReportError(_code.Position, "Unknown word '{0}' in workspace.", word);
						return;
					}
				}
				else
				{
					ReportError(_code.Position, "Syntax error in workspace.");
					return;
				}
			}
		}

		private static bool ReadWorkspaceItem()
		{
			// FileName
			if (!_code.ReadWord())
			{
				ReportError(_code.Position, "Expected file name in workspace.");
				return false;
			}

			// TablePath
			var gotPath = false;
			while (true)
			{
				if (!_code.ReadWord()) break;
				gotPath = true;
				if (!_code.ReadExact('\\')) break;
			}
			if (!gotPath)
			{
				ReportError(_code.Position, "Expected table path in workspace item.");
				return false;
			}

			// Attributes
			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(',')) break;
				if (_code.PeekExact(')') || _code.PeekExact('}')) break;

				var word = _code.ReadWordR();
				if (word.Equals("prompt", StringComparison.OrdinalIgnoreCase)) ReadPromptOrCommentAttribute();
				else if (word.Equals("comment", StringComparison.OrdinalIgnoreCase)) ReadPromptOrCommentAttribute();
				else if (word.Equals("tag", StringComparison.OrdinalIgnoreCase)) ReadTagAttribute();
				else if (word.Equals("preload", StringComparison.OrdinalIgnoreCase)) { }
				else
				{
					ReportError(_code.Position, "Unknown word '{0}' in workspace item.", word);
					return false;
				}
			}

			return true;
		}

		private static void ReadDropWorkspace()
		{
			var name = _code.ReadWordR();
			if (string.IsNullOrEmpty(name))
			{
				ReportError(_code.Position, "Expected workspace name to follow 'drop workspace'.");
				return;
			}
		}
		#endregion
	}
}
