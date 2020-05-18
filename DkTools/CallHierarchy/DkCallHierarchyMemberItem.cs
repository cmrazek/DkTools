using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;
using DkTools.FunctionFileScanning;
using DkTools.Navigation;
using EnvDTE;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace DkTools.CallHierarchy
{
	class DkCallHierarchyMemberItem : ICallHierarchyMemberItem
	{
		private string _filePath;
		private string _functionFullName;
		private string _className;
		private string _functionName;
		private FilePosition _filePos;
		private string _extRefId;
		private List<ICallHierarchyItemDetails> _details = new List<ICallHierarchyItemDetails>();

		private const string CallsSearchCategory = "Calls";

		/// <summary>
		/// A function node in the View Call Hierarchy tool window.
		/// </summary>
		/// <param name="filePath">The pathname of the file that was active when the search was started.
		/// This could be null if the search was started from the DK Explorer window.</param>
		/// <param name="functionFullName">Full name of the function, including class name.</param>
		/// <param name="className">Class name. Will be null for functions not in a class.</param>
		/// <param name="functionName">Name of the function.</param>
		/// <param name="filePos">File and position to the function declaration.</param>
		/// <param name="extRefId">Reference ID for the function.</param>
		public DkCallHierarchyMemberItem(string filePath, string functionFullName, string className, string functionName, FilePosition filePos, string extRefId)
		{
			_filePath = filePath;
			_functionFullName = functionFullName;
			_className = className;
			_functionName = functionName;
			_filePos = filePos;
			_extRefId = extRefId;
		}

		public string NameSeparator => ".";
		public bool SupportsNavigateTo => !_filePos.IsEmpty;
		public IEnumerable<ICallHierarchyItemDetails> Details => _details;
		public string SortText => _functionFullName;
		public ImageSource DisplayGlyph => null;
		public string MemberName => _functionFullName;
		public string ContainingTypeName => Path.GetFileName(_filePos.FileName);
		public string ContainingNamespaceName => null;
		public bool Valid => true;
		public IEnumerable<CallHierarchySearchCategory> SupportedSearchCategories
		{
			get
			{
				yield return new CallHierarchySearchCategory(CallsSearchCategory, $"Calls to '{_functionFullName}'");
			}
		}
		public bool SupportsFindReferences => true;

		public void CancelSearch(string categoryName)
		{
		}

		public void FindReferences()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			GoToDefinitionHelper.TriggerFindReferences(_extRefId, _functionFullName);
		}

		public void ItemSelected()
		{
		}

		public void NavigateTo()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Shell.OpenDocument(_filePos);
		}

		public void ResumeSearch(string categoryName)
		{
		}

		public void StartSearch(string categoryName, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (categoryName == CallsSearchCategory)
				{
					var ds = DefinitionStore.Current;
					if (ds == null) return;

					using (var db = new FFDatabase())
					{
						var appId = db.ExecuteScalar<long>("select rowid from app where name = @app_name collate nocase",
							"@app_name", ds.AppName);

						foreach (var result in GetCallingFunctionsInSolution(db, appId, _extRefId,
							searchScope == CallHierarchySearchScope.CurrentDocument ? _filePath : null))
						{
							callback.AddResult(result);
						}
					}

					callback.SearchSucceeded();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				callback.SearchFailed(ex.ToString());
			}
		}

		public void SuspendSearch(string categoryName)
		{
		}

		private void AddDetail(DkCallHierarchyItemDetails detail)
		{
			_details.Add(detail);
		}

		private IEnumerable<DkCallHierarchyMemberItem> GetCallingFunctionsInSolution(FFDatabase db, long appId, string funcRefId, string limitFilePath)
		{
			var items = new Dictionary<string, DkCallHierarchyMemberItem>();
			var codeStore = new CodeInfoStore();

			// select file_id, alt_file_id, pos, func_ref_id from ref where app_id = 1 and ext_ref_id = 'func:equivalent_to_active_card_status' and func_ref_id is not null;
			using (var cmd = db.CreateCommand(@"
select ref.func_ref_id, file_.file_name, alt_file.file_name, ref.pos, ref.func_file_name, ref.func_pos
from ref
inner join file_ on file_.rowid = ref.file_id
left outer join alt_file on alt_file.rowid = ref.alt_file_id
where ref.app_id = @app_id
and ref.ext_ref_id = @ext_ref_id
and ref.func_ref_id is not null
and ref.func_ref_id != ref.ext_ref_id
",
				"@app_id", appId,
				"@ext_ref_id", funcRefId))
			{
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						var parentFuncRefId = rdr.GetString(0);
						var fileName = rdr.GetString(1);
						var altFileName = rdr.GetStringOrNull(2);
						var pos = rdr.GetInt32(3);
						var funcFileName = rdr.GetString(4);
						var funcPos = rdr.GetInt32(5);

						if (limitFilePath != null && string.Compare(funcFileName, limitFilePath, StringComparison.OrdinalIgnoreCase) != 0) continue;

						var detailFilePath = string.IsNullOrEmpty(altFileName) ? fileName : altFileName;
						if (!string.IsNullOrEmpty(limitFilePath) && string.Compare(detailFilePath, limitFilePath, StringComparison.OrdinalIgnoreCase) != 0) continue;

						var parentFuncFullName = FunctionDefinition.ParseFullNameFromExtRefId(parentFuncRefId);
						var parentFuncClassName = FunctionDefinition.ParseClassNameFromExtRefId(parentFuncRefId);
						var parentFuncName = FunctionDefinition.ParseFunctionNameFromExtRefId(parentFuncRefId);
						if (string.IsNullOrEmpty(parentFuncFullName) || string.IsNullOrEmpty(parentFuncName)) continue;

						var funcKey = string.Concat(parentFuncRefId, "|", funcFileName.ToLower(), "|", funcPos);

						var detailFilePos = new FilePosition(detailFilePath, pos);
						if (!codeStore.FilePositionToLineAndOffset(detailFilePos, out var detailLineNum, out var detailLinePos))
						{
							Log.Warning("Failed to get line number and position for FilePosition [{0}]", detailFilePos);
							continue;
						}
						var detailText = codeStore.GetTextLineAtFilePosition(detailFilePos);
						if (string.IsNullOrWhiteSpace(detailText)) detailText = parentFuncFullName;
						else detailText = detailText.Trim();

						var detail = new DkCallHierarchyItemDetails(detailText, detailLineNum, detailLinePos, detailFilePos);

						if (items.TryGetValue(funcKey, out var item))
						{
							item.AddDetail(detail);
						}
						else
						{
							item = new DkCallHierarchyMemberItem(_filePath, parentFuncFullName, parentFuncClassName, parentFuncName,
								new FilePosition(funcFileName, funcPos), parentFuncRefId);
							item.AddDetail(detail);
							items[funcKey] = item;
						}
					}
				}
			}

			return items.Values;
		}
	}
}
