using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling;
using DK.Modeling.Tokens.Statements;
using DK.Preprocessing;
using DK.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/*
Main file layout:
[int]				Signature
[int]				Version
[int]				Data size
[int]				Hash length
[var]				Hash

File data layout:

[int]				Address of next node
[int]				Signature
[int]				Record version
[string]			File name (lower)
[date1]				Last scan date (lower half of DateTime.Ticks)
[date2]				Last scan date (upper half of DateTime.Ticks)
[string]			Class name (lower)
[int]				Number of dependent files
[int]				Number of functions
[int]				Number of references
[int]				Number of permanent extracts
Dependent Files:
	[int]			Signature
	[string]		Path name of dependent file
Functions:
	[int]			Signature
	[string]		Function name
	[string]		Actual file name (lower)
	[int]			Position in actual file
	[string]		Signature
References:
	[int]			Signature
	[string]		Ext Ref ID
	[string]		Actual file name (lower)
	[int]			Position in actual file
Permanent Extracts:
	[int]			Signature
	[string]		Name of permanent extract
	[string]		Actual file name (lower)
	[int]			Position in actual file
	[int]			Number of columns
		[int]		Signature
		[string]	Column name
		[string]	Data type
		[string]	Actual file name (lower)
		[int]		Position in actual file
*/

namespace DK.Repository
{
	public class AppRepo
	{
		private DkAppSettings _appSettings;
		private string _repoDir;
		private string _repoFileName;
		private StringRepo _strings;
		private List<int> _data;
		private DateTime _lastShrink = DateTime.MinValue;

		private const int ShrinkIntervalSeconds = 180;

		#region File Layout
		private const string RepoBaseDir = ".dk";

		private const int RepoSignature = 1869636978;		// 'repo'
		private const int Version = 2;
		private const int FileSignature = 1701603686;		// 'file'
		private const int DepSignature = 544236900;			// 'dep '
		private const int RefSignature = 543581554;			// 'ref'
		private const int FuncSignature = 1668183398;		// 'func'
		private const int PermExSignature = 2020438640;		// 'prmx'
		private const int PermExColSignature = 1818458224;	// 'pxcl'

		private enum FileLayout
		{
			NextAddress,
			Signature,
			RecordVersion,
			FileName,
			ScanDate1,
			ScanDate2,
			ClassName,
			NumDeps,
			NumFuncs,
			NumRefs,
			NumPermEx,
			ArrStart
		}

		private enum DepLayout : int
		{
			Signature,
			FileName,
			Size
		}

		private enum RefLayout
		{
			Signature,
			ExtRefId,
			FileName,
			Position,
			Size
		}

		private enum FuncLayout
		{
			Signature,
			Name,
			FileName,
			Position,
			FunctionSignature,
			Size
		}

		private enum PermExLayout
		{
			Signature,
			Name,
			FileName,
			Position,
			NumCols,
			ArrIndex
		}

		private enum PermExColLayout
		{
			Signature,
			Name,
			DataType,
			FileName,
			Position,
			Size
		}
		#endregion

		#region Construction
		public AppRepo(DkAppSettings appSettings)
		{
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

			try
			{
				var repoDir = _appSettings.ExeDirs.FirstOrDefault();
				if (!string.IsNullOrEmpty(repoDir))
				{
					if (!Directory.Exists(repoDir)) Directory.CreateDirectory(repoDir);

					_repoDir = Path.Combine(repoDir, RepoBaseDir);
					if (!Directory.Exists(_repoDir)) Directory.CreateDirectory(_repoDir);

					_repoFileName = Path.Combine(_repoDir, AppNameEncode(_appSettings.AppName));
					if (File.Exists(_repoFileName))
					{
						Log.Debug("Loading DK repository.");
						Read();
						GenerateGlobalDefinitions();
						Log.Debug("DK repository loaded.");
					}
					else
					{
						_strings = new StringRepo();
						_data = new List<int>();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when reading global data.");

				// Reset data to blank
				_strings = new StringRepo();
				_data = new List<int>();
			}
		}

		private string AppNameEncode(string str)
		{
			var sb = new StringBuilder(str.Length);
			var invalidChars = Path.GetInvalidPathChars();

			foreach (var ch in str)
			{
				if (ch == '#') sb.Append("##");
				else if (ch <= ' ' || invalidChars.Contains(ch)) sb.AppendFormat("#{0:X}#");
				else sb.Append(ch);
			}

			return sb.ToString();
		}
		#endregion

		#region File Node Management
		public void UpdateFile(CodeModel model, FFScanMode scanMode)
		{
			lock (this)
			{
				var newData = CreateFileDataFromModel(model, scanMode);

				RemoveFile(model.FilePath);

				var newAddress = FindUnusedAddress(newData.Count);
				if (newAddress == -1)
				{
					AddFileNode(newData);
				}
				else
				{
					ReplaceFileNode(newAddress, newData);
				}
			}
		}

		private void AddFileNode(List<int> fileData)
		{
			_data.Add(_data.Count + fileData.Count + 2);
			_data.Add(FileSignature);
			_data.AddRange(fileData);
		}

		private void CheckFileAddress(int address)
		{
			if (address < 0 || address >= _data.Count) throw new InvalidAddressException();
			if (_data[address + (int)FileLayout.Signature] != FileSignature) throw new InvalidAddressException();
		}

		private void CheckDepAddress(int addr)
		{
			if (addr < 0 || addr >= _data.Count) throw new InvalidAddressException();
			if (_data[addr + (int)DepLayout.Signature] != DepSignature) throw new InvalidAddressException();
		}

		private void CheckFuncAddress(int addr)
		{
			if (addr < 0 || addr >= _data.Count) throw new InvalidAddressException();
			if (_data[addr + (int)FuncLayout.Signature] != FuncSignature) throw new InvalidAddressException();
		}

		private void CheckRefAddress(int addr)
		{
			if (addr < 0 || addr >= _data.Count) throw new InvalidAddressException();
			if (_data[addr + (int)RefLayout.Signature] != RefSignature) throw new InvalidAddressException();
		}

		private void CheckPermExAddress(int addr)
		{
			if (addr < 0 || addr >= _data.Count) throw new InvalidAddressException();
			if (_data[addr + (int)PermExLayout.Signature] != PermExSignature) throw new InvalidAddressException();
		}

		private void CheckPermExColAddress(int addr)
		{
			if (addr < 0 || addr >= _data.Count) throw new InvalidAddressException();
			if (_data[addr + (int)PermExColLayout.Signature] != PermExColSignature) throw new InvalidAddressException();
		}

		private void ReplaceFileNode(int address, List<int> fileData)
		{
#if DEBUG
			CheckFileAddress(address);
#endif
			var fileLength = fileData.Count;
			var endAddress = _data[address];
			var lengthOfNode = endAddress - address - 1;
			if (lengthOfNode < fileLength) throw new InvalidOperationException("Node is not large enough.");

			address += 2;	// record starts with next address followed by signature

			var fileIndex = 0;
			while (fileLength-- != 0) _data[address++] = fileData[fileIndex++];
			while (address < endAddress) _data[address++] = 0;  // Clear out left-over data
		}

		private void ClearFileNode(int addr)
		{
#if DEBUG
			CheckFileAddress(addr);
#endif
			var nextAddr = _data[addr];
			addr += 2;
			while (addr < nextAddr) _data[addr++] = 0;
		}

		private int FindFileAddress(string fileName)
		{
			var fileNameId = _strings.GetId(fileName.ToLower());
			var addr = 0;
			var end = _data.Count;

			while (addr < end)
			{
				if (_data[addr + (int)FileLayout.FileName] == fileNameId) return addr;
				addr = _data[addr];
			}

			return -1;
		}

		/// <summary>
		/// Finds a free node fitting a minimum size.
		/// </summary>
		/// <param name="minLength">The length of data to go into the node (excluding nextAddr and signature)</param>
		/// <returns>The best fitting node, or -1 if no suitable node could be found.</returns>
		private int FindUnusedAddress(int minLength)
		{
			var addr = 0;
			var end = _data.Count;
			var smallestAddr = -1;
			var smallestLength = -1;

			while (addr < end)
			{
				var nextAddr = _data[addr];

				if (_data[addr + (int)FileLayout.FileName] == 0)
				{
					var length = nextAddr - addr - 2;
					if (length == minLength) return addr;
					if (length < minLength)
					{
						addr = nextAddr;
						continue;
					}
					if (smallestLength == -1 || length < smallestLength)
					{
						smallestLength = length;
						smallestAddr = addr;
					}
				}

				addr = nextAddr;
			}

			return smallestAddr;
		}

		private void RemoveFile(string fileName)
		{
			var fileNameId = _strings.GetId(fileName.ToLower());
			if (fileNameId < 0) return;

			var addr = 0;
			var end = _data.Count;
			while (addr < end)
			{
				var nextAddr = _data[addr];

				if (File_GetFileNameId(addr) == fileNameId)
				{
					ClearFileNode(addr);
				}

				addr = nextAddr;
			}
		}
		#endregion

		#region Data Model Parsing
		private List<int> CreateFileDataFromModel(CodeModel model, FFScanMode scanMode)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			var data = new List<int>();
			var scanDate = scanMode == FFScanMode.Exports ? DateTime.MinValue.Ticks : DateTime.Now.Ticks;

			var depends = GetDistinctIncludeDependencies(model);
			var funcs = GetFunctionsFromModel(model);
			var refs = scanMode == FFScanMode.Deep ? (IEnumerable<Reference>)GetReferencesFromModel(model) : new Reference[0];
			var permExs = scanMode == FFScanMode.Deep ? (IEnumerable<ExtractStatement>)GetPermanentExtractsFromModel(model) : new ExtractStatement[0];

			// Any changes made to this layout must also be mirrored in ShrinkStrings()

			data.Add(Version);
			data.Add(_strings.Store(model.FilePath.ToLower()));
			data.Add((int)(((ulong)scanDate) & 0xffffffff));
			data.Add((int)(((ulong)scanDate) >> 32));
			data.Add(_strings.Store(model.ClassName));
			data.Add(depends.Count);
			data.Add(funcs.Count);
			data.Add(refs.Count());
			data.Add(permExs.Count());

			foreach (var dep in depends)
			{
				data.Add(DepSignature);
				data.Add(_strings.Store(dep.FileName.ToLower()));
			}

			foreach (var func in funcs)
			{
				data.Add(FuncSignature);
				data.Add(_strings.Store(func.Name));
				data.Add(_strings.Store(func.FilePosition.FileName.ToLower()));
				data.Add(func.FilePosition.Position);
				data.Add(_strings.Store(func.Signature.ToDbString()));
			}

			foreach (var rf in refs)
			{
				data.Add(RefSignature);
				data.Add(_strings.Store(rf.ExternalRefId));
				data.Add(_strings.Store(rf.FilePosition.FileName.ToLower()));
				data.Add(rf.FilePosition.Position);
			}

			foreach (var permEx in permExs)
			{
				var fields = permEx.Fields.ToList();

				data.Add(PermExSignature);
				data.Add(_strings.Store(permEx.Name));
				data.Add(_strings.Store(permEx.FilePosition.FileName.ToLower()));
				data.Add(permEx.FilePosition.Position);
				data.Add(fields.Count);
				foreach (var field in fields)
				{
					data.Add(PermExColSignature);
					data.Add(_strings.Store(field.Name));
					data.Add(_strings.Store((field.DataType ?? DataType.Int).ToCodeString()));
					data.Add(_strings.Store(field.FilePosition.FileName.ToLower()));
					data.Add(field.FilePosition.Position);
				}
			}

			return data;
		}

		private void ShrinkStrings()
		{
			Log.Debug("Shrinking DK repo strings");

			var strings = new StringRepo(_strings.Count);

			IterateFiles(file =>
			{
				_data[file + (int)FileLayout.FileName] = strings.Store(File_GetFileName(file));
				_data[file + (int)FileLayout.ClassName] = strings.Store(File_GetClassName(file));

				File_IterateDeps(file, dep =>
				{
					_data[dep + (int)DepLayout.FileName] = strings.Store(Dep_GetFileName(dep));
					return true;
				});

				File_IterateFuncs(file, func =>
				{
					_data[func + (int)FuncLayout.Name] = strings.Store(Func_GetName(func));
					_data[func + (int)FuncLayout.FileName] = strings.Store(Func_GetFilePosition(func).FileName);
					_data[func + (int)FuncLayout.FunctionSignature] = strings.Store(Func_GetSignature(func));
					return true;
				});

				File_IterateRefs(file, rf =>
				{
					_data[rf + (int)RefLayout.ExtRefId] = strings.Store(Ref_GetExtRef(rf));
					_data[rf + (int)RefLayout.FileName] = strings.Store(Ref_GetFilePosition(rf).FileName);
					return true;
				});

				File_IteratePermExs(file, px =>
				{
					_data[px + (int)PermExLayout.Name] = strings.Store(PermEx_GetName(px));
					_data[px + (int)PermExLayout.FileName] = strings.Store(PermEx_GetFilePosition(px).FileName);

					return PermEx_IterateCols(px, col =>
					{
						_data[col + (int)PermExColLayout.Name] = strings.Store(PermExCol_GetName(col));
						_data[col + (int)PermExColLayout.DataType] = strings.Store(_strings.GetString(_data[col + (int)PermExColLayout.DataType]));
						_data[col + (int)PermExColLayout.FileName] = strings.Store(PermExCol_GetFilePosition(col).FileName);
						return true;
					});
				});

				return true;
			});

			_strings = strings;
			Log.Debug("Finished shrinking DK repository strings.");
		}

		private List<IncludeDependency> GetDistinctIncludeDependencies(CodeModel model)
		{
			var results = new List<IncludeDependency>();

			foreach (var incl in model.PreprocessorModel.IncludeDependencies)
			{
				if (!results.Any(x => x.FileName.EqualsI(incl.FileName)))
				{
					results.Add(incl);
				}
			}

			return results;
		}

		private List<FunctionDefinition> GetFunctionsFromModel(CodeModel model)
		{
			var funcs = new List<FunctionDefinition>();

			switch (model.FileContext)
			{
				case FileContext.Function:
				case FileContext.ClientClass:
				case FileContext.ServerClass:
				case FileContext.NeutralClass:
					funcs.AddRange(model.DefinitionProvider.GetGlobalFromFile<FunctionDefinition>().Where(x => !x.Extern));
					break;
			}

			return funcs;
		}

		private List<Reference> GetReferencesFromModel(CodeModel model)
		{
			var refList = new List<Reference>();

			foreach (var token in model.File.FindDownward(t => t.SourceDefinition != null &&
				!string.IsNullOrEmpty(t.SourceDefinition.ExternalRefId) &&
				t.File != null))
			{
				var refId = token.SourceDefinition.ExternalRefId;
				if (!string.IsNullOrEmpty(refId))
				{
					refList.Add(new Reference
					{
						ExternalRefId = refId,
						FilePosition = token.File.CodeSource.GetFilePosition(token.Span.Start)
					});
				}
			}

			foreach (var rf in model.PreprocessorReferences)
			{
				var def = rf.Definition;
				var refId = def.ExternalRefId;
				if (!string.IsNullOrEmpty(refId))
				{
					refList.Add(new Reference
					{
						ExternalRefId = refId,
						FilePosition = rf.FilePosition
					});
				}
			}

			return refList;
		}

		private class Reference
		{
			public string ExternalRefId { get; set; }
			public FilePosition FilePosition { get; set; }
		}

		private List<ExtractStatement> GetPermanentExtractsFromModel(CodeModel model)
		{
			return model.File.FindDownward<ExtractStatement>().Where(x => x.IsPermanent).ToList();
		}

		private void PurgeNonexistentFiles()
		{
			IterateFiles(file =>
			{
				if (!File.Exists(File_GetFileName(file)))
				{
					ClearFileNode(file);
				}

				return true;
			});
		}

		public void OnExportsComplete()
		{
			lock (this)
			{
				GenerateGlobalDefinitions();
			}
		}

		public void OnScanComplete()
		{
			lock (this)
			{
				PurgeNonexistentFiles();

				if (DateTime.Now.Subtract(_lastShrink).TotalSeconds > ShrinkIntervalSeconds)
				{
					ShrinkStrings();
					_lastShrink = DateTime.Now;
				}

				Write();

				Log.Info("Scan Complete: File Data: {0} String Count: {1}", _data.Count * 4, _strings.Count);

//#if DEBUG
//				_strings.DumpToFile(_repoFileName + "-strings.txt");
//				DumpToFile(_repoFileName + "-repo.txt");
//#endif

				GenerateGlobalDefinitions();
			}
		}
		#endregion

		#region Iteration Methods
		#region Files
		private bool IterateFiles(Func<int, bool> callback)
		{
			if (_data == null) return true;

			var addr = 0;
			var end = _data.Count;

			while (addr < end)
			{
				if (_data[addr + (int)FileLayout.FileName] != 0)
				{
					if (!callback(addr)) return false;
				}
				addr = _data[addr];
			}

			return true;
		}

		private int File_GetFileNameId(int fileAddr)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			return _data[fileAddr + (int)FileLayout.FileName];
		}
		private string File_GetFileName(int fileAddr)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			return _strings.GetString(_data[fileAddr + (int)FileLayout.FileName]);
		}

		private DateTime File_GetDate(int fileAddr)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			long ticks = _data[fileAddr + (int)FileLayout.ScanDate2];
			ticks <<= 32;
			ticks |= (uint)_data[fileAddr + (int)FileLayout.ScanDate1];
			return new DateTime(ticks);
		}

		private void File_SetDate(int fileAddr, DateTime value)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			var ticks = value.Ticks;
			_data[fileAddr + (int)FileLayout.ScanDate1] = (int)(((ulong)ticks) & 0xffffffff);
			_data[fileAddr + (int)FileLayout.ScanDate2] = (int)(((ulong)ticks) >> 32);
		}

		private int File_GetClassNameId(int fileAddr)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			return _data[fileAddr + (int)FileLayout.ClassName];
		}

		private string File_GetClassName(int fileAddr)
		{
#if DEBUG
			CheckFileAddress(fileAddr);
#endif
			return _strings.GetString(_data[fileAddr + (int)FileLayout.ClassName]);
		}
		#endregion

		#region Deps
		private bool File_IterateDeps(int fileAddr, Func<int, bool> callback)
		{
			var numDeps = _data[fileAddr + (int)FileLayout.NumDeps];
			if (numDeps == 0) return true;

			var depAddr = fileAddr + (int)FileLayout.ArrStart;

			while (numDeps-- != 0)
			{
				if (!callback(depAddr)) return false;
				depAddr += (int)DepLayout.Size;
			}

			return true;
		}

		private int Dep_GetFileNameId(int depAddr)
		{
#if DEBUG
			CheckDepAddress(depAddr);
#endif
			return _data[depAddr + (int)DepLayout.FileName];
		}

		private string Dep_GetFileName(int depAddr)
		{
#if DEBUG
			CheckDepAddress(depAddr);
#endif
			return _strings.GetString(_data[depAddr + (int)DepLayout.FileName]);
		}
		#endregion

		#region Funcs
		private bool File_IterateFuncs(int fileAddr, Func<int, bool> callback)
		{
			var numFuncs = _data[fileAddr + (int)FileLayout.NumFuncs];
			if (numFuncs == 0) return true;

			var numDeps = _data[fileAddr + (int)FileLayout.NumDeps];
			var funcAddr = fileAddr + (int)FileLayout.ArrStart + numDeps * (int)DepLayout.Size;

			while (numFuncs-- != 0)
			{
				if (!callback(funcAddr)) return false;
				funcAddr += (int)FuncLayout.Size;
			}

			return true;
		}

		private int Func_GetNameId(int funcAddr)
		{
#if DEBUG
			CheckFuncAddress(funcAddr);
#endif
			return _data[funcAddr + (int)FuncLayout.Name];
		}

		private string Func_GetName(int funcAddr)
		{
#if DEBUG
			CheckFuncAddress(funcAddr);
#endif
			return _strings.GetString(_data[funcAddr + (int)FuncLayout.Name]);
		}

		private string Func_GetSignature(int funcAddr)
		{
#if DEBUG
			CheckFuncAddress(funcAddr);
#endif
			return _strings.GetString(_data[funcAddr + (int)FuncLayout.FunctionSignature]);
		}

		private FilePosition Func_GetFilePosition(int funcAddr)
		{
#if DEBUG
			CheckFuncAddress(funcAddr);
#endif
			return new FilePosition(_strings.GetString(_data[funcAddr + (int)FuncLayout.FileName]), _data[funcAddr + (int)FuncLayout.Position]);
		}
		#endregion

		#region Refs
		private bool File_IterateRefs(int fileAddr, Func<int, bool> callback)
		{
			var numRefs = _data[fileAddr + (int)FileLayout.NumRefs];
			if (numRefs == 0) return true;

			var numDeps = _data[fileAddr + (int)FileLayout.NumDeps];
			var numFuncs = _data[fileAddr + (int)FileLayout.NumFuncs];
			var refAddr = fileAddr + (int)FileLayout.ArrStart + numDeps * (int)DepLayout.Size + numFuncs * (int)FuncLayout.Size;

			while (numRefs-- != 0)
			{
				if (!callback(refAddr)) return false;
				refAddr += (int)RefLayout.Size;
			}

			return true;
		}

		private int Ref_GetExtRefId(int refAddr)
		{
#if DEBUG
			CheckRefAddress(refAddr);
#endif
			return _data[refAddr + (int)RefLayout.ExtRefId];
		}

		private string Ref_GetExtRef(int refAddr)
		{
#if DEBUG
			CheckRefAddress(refAddr);
#endif
			return _strings.GetString(_data[refAddr + (int)RefLayout.ExtRefId]);
		}

		private FilePosition Ref_GetFilePosition(int refAddr)
		{
#if DEBUG
			CheckRefAddress(refAddr);
#endif
			return new FilePosition(_strings.GetString(_data[refAddr + (int)RefLayout.FileName]), _data[refAddr + (int)RefLayout.Position]);
		}
		#endregion

		#region PermExs
		private bool File_IteratePermExs(int fileAddr, Func<int, bool> callback)
		{
			var numPermExs = _data[fileAddr + (int)FileLayout.NumPermEx];
			if (numPermExs == 0) return true;

			var numDeps = _data[fileAddr + (int)FileLayout.NumDeps];
			var numFuncs = _data[fileAddr + (int)FileLayout.NumFuncs];
			var numRefs = _data[fileAddr + (int)FileLayout.NumRefs];
			var permExAddr = fileAddr + (int)FileLayout.ArrStart + numDeps * (int)DepLayout.Size + numFuncs * (int)FuncLayout.Size + numRefs * (int)RefLayout.Size;

			while (numPermExs-- != 0)
			{
				if (!callback(permExAddr)) return false;

				var numCols = _data[permExAddr + (int)PermExLayout.NumCols];
				permExAddr += (int)PermExLayout.ArrIndex + numCols * (int)PermExColLayout.Size;
			}

			return true;
		}

		private int PermEx_GetNameId(int addr)
		{
#if DEBUG
			CheckPermExAddress(addr);
#endif
			return _data[addr + (int)PermExLayout.Name];
		}

		private string PermEx_GetName(int addr)
		{
#if DEBUG
			CheckPermExAddress(addr);
#endif
			return _strings.GetString(_data[addr + (int)PermExLayout.Name]);
		}

		private FilePosition PermEx_GetFilePosition(int addr)
		{
#if DEBUG
			CheckPermExAddress(addr);
#endif
			return new FilePosition(_strings.GetString(_data[addr + (int)PermExLayout.FileName]), _data[addr + (int)PermExLayout.Position]);
		}
		#endregion

		#region PermExCols
		private bool PermEx_IterateCols(int permExAddr, Func<int, bool> callback)
		{
			var numCols = _data[permExAddr + (int)PermExLayout.NumCols];
			if (numCols == 0) return true;

			var colAddr = permExAddr + (int)PermExLayout.ArrIndex;

			while (numCols-- != 0)
			{
				if (!callback(colAddr)) return false;
				colAddr += (int)PermExColLayout.Size;
			}

			return true;
		}

		private string PermExCol_GetName(int addr)
		{
#if DEBUG
			CheckPermExColAddress(addr);
#endif
			return _strings.GetString(_data[addr + (int)PermExColLayout.Name]);
		}

		private FilePosition PermExCol_GetFilePosition(int addr)
		{
#if DEBUG
			CheckPermExColAddress(addr);
#endif
			return new FilePosition(_strings.GetString(_data[addr + (int)PermExColLayout.FileName]), _data[addr + (int)PermExColLayout.Position]);
		}

		private DataType PermExCol_GetDataType(int addr)
		{
#if DEBUG
			CheckPermExColAddress(addr);
#endif
			return DataType.TryParse(new DataType.ParseArgs(new CodeParser(_strings.GetString(_data[addr + (int)PermExColLayout.DataType])), _appSettings)) ?? DataType.Int;
		}
		#endregion
		#endregion

		#region Saving / Loading
		private void Write()
		{
			if (string.IsNullOrEmpty(_repoFileName)) return;

			Log.Info("Saving repository to: {0}", _repoFileName);

			byte[] fileData;
			using (var memStream = new MemoryStream())
			using (var writer = new BinaryWriter(memStream))
			{
				writer.Write(_data.Count);
				foreach (var x in _data) writer.Write(x);
				_strings.Write(writer);
				writer.Flush();
				writer.Seek(0, SeekOrigin.Begin);
				fileData = new byte[memStream.Length];
				memStream.Read(fileData, 0, fileData.Length);
			}

			byte[] hash;
			using (var sha = SHA256.Create())
			{
				hash = sha.ComputeHash(fileData);
			}

			using (var fileStream = new FileStream(_repoFileName, FileMode.Create, FileAccess.Write))
			using (var writer = new BinaryWriter(fileStream))
			{
				writer.Write(RepoSignature);
				writer.Write(Version);
				writer.Write(fileData.Length);
				writer.Write(hash.Length);
				writer.Write(hash);
				writer.Write(fileData);
			}
		}

		private void Read()
		{
			if (string.IsNullOrEmpty(_repoFileName)) throw new InvalidRepoException("Repo file name is not set.");

			Log.Info("Loading repository: {0}", _repoFileName);

			_data = new List<int>();
			_strings = new StringRepo();

			byte[] fileData;
			byte[] fileHash;
			using (var fileStream = new FileStream(_repoFileName, FileMode.Open, FileAccess.Read))
			using (var rdr = new BinaryReader(fileStream))
			{
				var sig = rdr.ReadInt32();
				if (sig != RepoSignature) throw new InvalidRepoException("Repo does not have the correct signature.");

				var version = rdr.ReadInt32();
				if (version != Version) throw new InvalidRepoException("Unsupported repo version.");

				var fileDataLength = rdr.ReadInt32();
				if (fileDataLength < 0) throw new InvalidRepoException("Invalid file data length.");

				var hashLength = rdr.ReadInt32();
				if (hashLength < 0) throw new InvalidRepoException("Invalid hash length.");

				fileHash = rdr.ReadBytes(hashLength);
				fileData = rdr.ReadBytes(fileDataLength);
			}

			byte[] hash;
			using (var sha = SHA256.Create())
			{
				hash = sha.ComputeHash(fileData);
			}
			if (!CompareHashes(hash, fileHash)) throw new InvalidRepoException("Invalid hash in repo.");

			using (var memStream = new MemoryStream(fileData))
			using (var rdr = new BinaryReader(memStream))
			{
				var dataCount = rdr.ReadInt32();
				if (dataCount < 0) throw new InvalidRepoException("Invalid length of data array.");
				while (dataCount-- != 0) _data.Add(rdr.ReadInt32());
				_strings.Read(rdr);
			}

			Log.Info("Successfully loaded repository.");
		}

		private static bool CompareHashes(byte[] a, byte[] b)
		{
			if (a.Length != b.Length) return false;

			for (int i = 0, ii = a.Length; i < ii; i++)
			{
				if (a[i] != b[i]) return false;
			}

			return true;
		}
		#endregion

		#region Language Support
		private DefinitionCollection _defs = new DefinitionCollection();

		public bool TryGetFileDate(string fileName, out DateTime modified)
		{
			lock (this)
			{
				var fileNameId = _strings.GetId(fileName.ToLower());
				if (fileNameId < 0)
				{
					modified = default;
					return false;
				}

				DateTime? value = null;

				IterateFiles(file =>
				{
					if (File_GetFileNameId(file) == fileNameId)
					{
						value = File_GetDate(file);
						return false;
					}

					return true;
				});

				if (value.HasValue)
				{
					modified = value.Value;
					return true;
				}

				modified = default;
				return false;
			}
		}

		public void ResetScanDateOnDependentFiles(string fileName)
		{
			lock (this)
			{
				var fileNameId = _strings.GetId(fileName.ToLower());
				if (fileNameId < 0) return;

				IterateFiles(file =>
				{
					File_IterateDeps(file, dep =>
					{
						if (Dep_GetFileNameId(dep) == fileNameId)
						{
							File_SetDate(file, DateTime.MinValue);
						}
						return true;
					});
					return true;
				});
			}
		}

		public void ResetScanDateOnFile(string fileName)
		{
			lock (this)
			{
				var fileNameId = _strings.GetId(fileName.ToLower());
				if (fileNameId < 0) return;

				IterateFiles(file =>
				{
					if (File_GetFileNameId(file) == fileNameId)
					{
						File_SetDate(file, DateTime.MinValue);
					}
					return true;
				});
			}
		}

		public IEnumerable<string> GetDependentFiles(string fileName, int maxResults = 0)
		{
			lock (this)
			{
				var fileNameId = _strings.GetId(fileName.ToLower());
				if (fileNameId < 0) return new string[0];

				var results = new List<string>();

				IterateFiles(file =>
				{
					return File_IterateDeps(file, dep =>
					{
						if (Dep_GetFileNameId(dep) == fileNameId)
						{
							results.Add(File_GetFileName(file));
							if (maxResults != 0 && results.Count >= maxResults) return false;
						}
						return true;
					});
				});

				return results;
			}
		}

		public IEnumerable<FilePosition> FindAllReferences(string extRefId)
		{
			lock (this)
			{
				var refId = _strings.GetId(extRefId);
				if (refId < 0) return new FilePosition[0];

				var results = new List<FilePosition>();

				IterateFiles(file =>
				{
					return File_IterateRefs(file, rf =>
					{
						if (Ref_GetExtRefId(rf) == refId)
						{
							results.Add(Ref_GetFilePosition(rf));
						}
						return true;
					});
				});

				return results;
			}
		}

		public IEnumerable<FunctionDefinition> SearchForFunctionDefinitions(string funcName)
		{
			lock (this)
			{
				var funcNameId = _strings.GetId(funcName);
				if (funcNameId < 0) return new FunctionDefinition[0];

				var results = new List<FunctionDefinition>();

				IterateFiles(file =>
				{
					return File_IterateFuncs(file, func =>
					{
						if (Func_GetNameId(func) == funcNameId)
						{
							results.Add(new FunctionDefinition(
								signature: FunctionSignature.ParseFromDb(Func_GetSignature(func), _appSettings),
								filePos: Func_GetFilePosition(func)
							));
						}
						return true;
					});
				});

				return results;
			}
		}

		public IEnumerable<ClassDefinition> GetClassDefinitions(string className) => _defs.Get<ClassDefinition>(className);
		public IEnumerable<ExtractTableDefinition> GetPermanentExtractDefinitions(string extractName) => _defs.Get<ExtractTableDefinition>(extractName);
		public IEnumerable<Definition> GetGlobalDefinitions() => _defs.All;

		private void GenerateGlobalDefinitions()
		{
			var results = new List<Definition>();

			IterateFiles(file =>
			{
				if (File_GetClassNameId(file) != 0)
				{
					var fileName = File_GetFileName(file);
					var serverContext = ServerContextHelper.FromFileName(fileName);
					var classDef = new ClassDefinition(File_GetClassName(file), fileName, serverContext);

					File_IterateFuncs(file, func =>
					{
						var sig = FunctionSignature.ParseFromDb(Func_GetSignature(func), _appSettings);
						var funcDef = new FunctionDefinition(sig, Func_GetFilePosition(func));
						classDef.AddFunction(funcDef);
						return true;
					});

					results.Add(classDef);
				}
				else
				{
					File_IterateFuncs(file, func =>
					{
						var sig = FunctionSignature.ParseFromDb(Func_GetSignature(func), _appSettings);
						var funcDef = new FunctionDefinition(sig, Func_GetFilePosition(func));
						results.Add(funcDef);
						return true;
					});
				}

				File_IteratePermExs(file, permEx =>
				{
					var permExDef = new ExtractTableDefinition(PermEx_GetName(permEx), PermEx_GetFilePosition(permEx), permanent: true);

					PermEx_IterateCols(permEx, col =>
					{
						var colDef = new ExtractFieldDefinition(PermExCol_GetName(col), PermExCol_GetFilePosition(col), permExDef, PermExCol_GetDataType(col));
						permExDef.AddField(colDef);
						return true;
					});

					results.Add(permExDef);
					return true;
				});

				return true;
			});

			_defs.Clear();
			_defs.Add(results);

			Log.Debug("Global definitions refreshed: Count [{0}]", _defs.Count);
		}
		#endregion

		#region Debug
		private void DumpToFile(string fileName)
		{
			using (var dw = new DumpWriter(fileName))
			{
				IterateFiles(file =>
				{
					dw.WriteLine($"File: {File_GetFileName(file)} Scanned: {File_GetDate(file)} Class: {File_GetClassName(file)}");
					using (var fileScope = dw.Indent())
					{
						File_IterateDeps(file, dep =>
						{
							dw.WriteLine($"Dep: {Dep_GetFileName(dep)}");
							return true;
						});

						File_IterateFuncs(file, func =>
						{
							dw.WriteLine($"Func: {Func_GetName(func)} {Func_GetFilePosition(func)}");
							return true;
						});

						File_IterateRefs(file, rf =>
						{
							dw.WriteLine($"Ref: {Ref_GetExtRef(rf)} {Ref_GetFilePosition(rf)}");
							return true;
						});

						File_IteratePermExs(file, px =>
						{
							dw.WriteLine($"PermEx: {PermEx_GetName(px)} {PermEx_GetFilePosition(px)}");

							using (var pxScope = dw.Indent())
							{
								return PermEx_IterateCols(px, col =>
								{
									dw.WriteLine($"Col: {PermExCol_GetName(col)} DataType: {PermExCol_GetDataType(col)} {PermExCol_GetFilePosition(col)}");
									return true;
								});
							}
						});
					}
					return true;
				});
			}
		}

		/*
Permanent Extracts:
	[string]		Name of permanent extract
	[string]		Actual file name (lower)
	[int]			Position in actual file
	[int]			Number of columns
		[string]	Column name
		[string]	Data type
		[string]	Actual file name (lower)
		[int]		Position in actual file
*/
				#endregion
	}
}
