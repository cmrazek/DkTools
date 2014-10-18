using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ProbeExplorer
{
	internal static class RepoInfo
	{
		public const string NullStr = "(null)";

		public static IEnumerable<RepoInfoItem> GenerateInfoItems(object obj, int indent)
		{
			if (obj == null) return new RepoInfoItem[0];

			List<RepoInfoItem> items = new List<RepoInfoItem>();
			try
			{
				var dict = new SortedDictionary<string, object>();

				#region IPAction
				try
				{
					if (obj is DICTSRVRLib.IPAction)
					{
						var repo = obj as DICTSRVRLib.IPAction;
						dict["IPAction:Column"] = new MoreInfoItem("IPAction:Column", repo.Column, null);
						dict["IPAction:Context"] = repo.Context;
						dict["IPAction:Name"] = repo.Name;
						dict["IPAction:NoMenu"] = repo.NoMenu;
						dict["IPAction:Type"] = repo.Type;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPColumn
				try
				{
					if (obj is DICTSRVRLib.IPColumn)
					{
						var repo = obj as DICTSRVRLib.IPColumn;
						dict["IPColumn:Name"] = repo.Name;
						dict["IPColumn:Audit"] = repo.Audit;
						dict["IPColumn:CustomCLSID"] = repo.CustomCLSID;
						dict["IPColumn:CustomLicence"] = repo.CustomLicence;
						dict["IPColumn:CustomPersistFile"] = repo.CustomPersistFile;
						dict["IPColumn:Itype"] = repo.Itype;
						dict["IPColumn:Number"] = repo.Number;
						dict["IPColumn:Persistent"] = repo.Persistent;
						dict["IPColumn:Vtype"] = repo.Vtype;
						dict["IPColumn:GroupPrompt"] = repo.GroupPrompt[0];
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPDataDef
				try
				{
					var data = obj as DICTSRVRLib.IPDataDef;
					if (data != null)
					{
						dict["IPDataDef:BaseTypeDefine"] = data.BaseTypeDefine;
						dict["IPDataDef:CurrencyMaskAsLocal"] = data.CurrencyMaskAsLocal;
						dict["IPDataDef:DateEdRule"] = data.DateEdRule;
						dict["IPDataDef:DateFlexYrFuture"] = data.DateFlexYrFuture;
						dict["IPDataDef:DateFlexYrPast"] = data.DateFlexYrPast;
						dict["IPDataDef:DateFormat"] = data.DateFormat;
						dict["IPDataDef:EnumAlterable"] = data.EnumAlterable;
						dict["IPDataDef:EnumCount"] = data.Enumcount;
						dict["IPDataDef:EnumFixedWidth"] = data.EnumFixedWidth;
						dict["IPDataDef:EnumFormatWidth"] = data.EnumFormatwidth;
						dict["IPDataDef:EnumNo"] = data.EnumNo;
						dict["IPDataDef:EnumNoPick"] = data.EnumNoPick;
						dict["IPDataDef:EnumWidth"] = data.Enumwidth;
						dict["IPDataDef:GraphicCols"] = data.GraphicCols;
						dict["IPDataDef:GraphicRows"] = data.GraphicRows;
						dict["IPDataDef:HashValue"] = data.HashValue;
						dict["IPDataDef:InterfaceName"] = data.InterfaceName;
						dict["IPDataDef:LCPref"] = data.LCPref;
						dict["IPDataDef:LCPrefCollate"] = data.LCPrefCollate;
						dict["IPDataDef:LCPrefRefName"] = data.LCPrefRefName;
						dict["IPDataDef:Length"] = data.Length;
						dict["IPDataDef:MaskLCPref"] = data.MaskLCPref;
						dict["IPDataDef:MaskLCPrefLocale"] = data.MaskLCPrefLocale;
						dict["IPDataDef:MaskLCPrefRefName"] = data.MaskLCPrefRefName;
						dict["IPDataDef:NeutralFormat"] = data.NeutralFormat;
						dict["IPDataDef:NumericLeadingzeros"] = data.NumericLeadingzeros;
						dict["IPDataDef:Precision"] = data.Precision;
						dict["IPDataDef:RawBasetypeExAttr"] = data.RawBasetypeExAttr;
						dict["IPDataDef:Readonly"] = data.Readonly;
						dict["IPDataDef:RefLevel"] = data.RefLevel;
						dict["IPDataDef:Scale"] = data.Scale;
						dict["IPDataDef:SectionLevel"] = data.SectionLevel;
						dict["IPDataDef:StringCaps"] = data.StringCaps;
						dict["IPDataDef:Type"] = data.Type;
						dict["IPDataDef:VariantType"] = data.VariantType;
						dict["IPDataDef:Varying"] = data.Varying;
						dict["IPDataDef:Enumlist"] = data.Enumlist[0];
						dict["IPDataDef:Mask"] = data.Mask[0];
						dict["IPDataDef:TypeText"] = data.TypeText[0];
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPDictionary
				try
				{
					if (obj is DICTSRVRLib.IPDictionary)
					{
						var repo = obj as DICTSRVRLib.IPDictionary;
						dict["IPDictionary:AccessType"] = repo.AccessType;
						dict["IPDictionary:AppIID"] = repo.AppIID;
						dict["IPDictionary:AutoUpdFiles"] = repo.AutoUpdFiles;
						dict["IPDictionary:FileCount"] = repo.FileCount;
						dict["IPDictionary:FullPath"] = repo.FullPath;
						dict["IPDictionary:InstanceId"] = repo.InstanceId;
						dict["IPDictionary:InterfaceTypeCount"] = repo.InterfaceTypeCount;
						dict["IPDictionary:LanguageIDCount"] = repo.LanguageIDCount;
						dict["IPDictionary:LocaleID"] = repo.LocaleID;
						dict["IPDictionary:Name"] = repo.Name;
						dict["IPDictionary:RelationshipCount"] = repo.RelationshipCount;
						dict["IPDictionary:StringDefineCount"] = repo.StringDefineCount;
						dict["IPDictionary:TableCount"] = repo.TableCount;
						dict["IPDictionary:TypeDefineCount"] = repo.TypeDefineCount;
						dict["IPDictionary:Version"] = repo.Version;
						dict["IPDictionary:WorkspaceCount"] = repo.WorkspaceCount;

						for (int i = 1, ii = repo.RelationshipCount; i <= ii; i++)
						{
							var rel = repo.Relationships[i];
							var name = string.Format("IPDictionary:Relationships[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, rel, rel.Name);
						}

						for (int i = 1, ii = repo.StringDefineCount; i <= ii; i++)
						{
							var sd = repo.StringDefines[i];
							var name = string.Format("IPDictionary:StringDefines[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, sd, sd.Name);
						}

						for (int i = 1, ii = repo.TableCount; i <= ii; i++)
						{
							var table = repo.Tables[i];
							var name = string.Format("IPDictionary:Tables[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, table, table.Name);
						}

						for (int i = 1, ii = repo.TypeDefineCount; i <= ii; i++)
						{
							var td = repo.TypeDefines[i];
							var name = string.Format("IPDictionary:TypeDefines[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, td, td.Name);
						}

						for (int i = 1, ii = repo.WorkspaceCount; i <= ii; i++)
						{
							var ws = repo.Workspaces[i];
							var name = string.Format("IPDictionary:Workspaces[{0:000}]:{1}", i);
							dict[name] = new MoreInfoItem(name, ws, ws.Name);
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPDictObj
				try
				{
					var dobj = obj as DICTSRVRLib.IPDictObj;
					if (dobj != null)
					{
						dict["IPDictObj:DevInfo"] = dobj.DevInfo;
						dict["IPDictObj:IsModified"] = dobj.IsModified;
						dict["IPDictObj:IsNew"] = dobj.IsNew;
						dict["IPDictObj:Key"] = dobj.Key;
						dict["IPDictObj:Modified"] = dobj.Modified;
						dict["IPDictObj:ModifiedUTC"] = dobj.ModifiedUTC;
						dict["IPDictObj:System"] = dobj.System;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPDObjDesc
				try
				{
					var repo = obj as DICTSRVRLib.IPDObjDesc;
					if (repo != null)
					{
						dict["IPDObjDesc:GermaneKeyCount"] = repo.GermaneKeyCount;
						dict["IPDObjDesc:Image"] = repo.Image;
						dict["IPDObjDesc:Name"] = repo.Name;
						dict["IPDObjDesc:TagCount"] = repo.TagCount;
						dict["IPDObjDesc:Comment"] = repo.Comment[0];
						dict["IPDObjDesc:Prompt"] = repo.Prompt[0];

						for (int i = 1, ii = repo.GermaneKeyCount; i <= ii; i++)
						{
							var key = repo.GermaneKeys[i];
							var name = string.Format("IPDObjDesc:GermaneKeys[{0:000}]:{1}", i, key);
							dict[name] = key;
						}

						var sb = new StringBuilder();
						for (int i = 1, ii = repo.TagCount; i <= ii; i++)
						{
							var tag = repo.Tags[i];
							var germaneKey = tag.GermaneKey;
							var tagName = tag.Name;
							var name = string.Format("IPDObjDesc:Tags[{0:000}]", i);
							var value = germaneKey == null ? tagName : string.Concat(germaneKey, ":", tagName);
							dict[name] = new MoreInfoItem(name, tag, value);
						}

						DICTSRVRLib.PDS_AccelHK heldKey = DICTSRVRLib.PDS_AccelHK.AccelHK_NONE;
						int virtKey = 0;
						repo.GetAcceleratorKey(ref heldKey, ref virtKey);
						dict["IPDObjDesc:AcceleratorKey"] = string.Format("HeldKey: {0}  VirtKey: {1}", heldKey, virtKey);
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPFile
				try
				{
					var repo = obj as DICTSRVRLib.IPFile;
					if (repo != null)
					{
						dict["IPFile:AppName"] = repo.AppName;
						dict["IPFile:Name"] = repo.Name;
						dict["IPFile:Path"] = repo.Path;
						dict["IPFile:Preload"] = repo.Preload;
						dict["IPFile:RelationshipCount"] = repo.RelationshipCount;
						dict["IPFile:TableCount"] = repo.TableCount;

						for (int i = 1, ii = repo.RelationshipCount; i <= ii; i++)
						{
							var rel = repo.Relationships[i];
							var name = string.Format("IPFile:Relationships[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, rel, rel.Name);
						}

						for (int i = 1, ii = repo.TableCount; i <= ii; i++)
						{
							var table = repo.Tables[i];
							var name = string.Format("IPFile:Tables[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, table, table.Name);
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPFrmItem
				try
				{
					var repo = obj as DICTSRVRLib.IPFrmItem;
					if (repo != null)
					{
						dict["IPFrmItem:Name"] = repo.Name;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPHelp
				try
				{
					var repo = obj as DICTSRVRLib.IPHelp;
					if (repo != null)
					{
						dict["IPHelp:HelpContextID"] = repo.HelpContextID;
						dict["IPHelp:HelpKey"] = repo.HelpKey;
						dict["IPHelp:WhatsThisContextID"] = repo.WhatsThisContextID;
						dict["IPHelp:HelpFile"] = repo.HelpFile[0];
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPHelpKey
				try
				{
					var repo = obj as DICTSRVRLib.IPHelpKey;
					if (repo != null)
					{
						dict["IPHelpKey:HelpKey"] = repo.HelpKey;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPIndex
				try
				{
					if (obj is DICTSRVRLib.IPIndex)
					{
						var repo = obj as DICTSRVRLib.IPIndex;
						dict["IPIndex:AutoSequence"] = repo.AutoSequence;
						dict["IPIndex:AutoSequenceBase"] = repo.AutoSequenceBase;
						dict["IPIndex:AutoSequenceIncrement"] = repo.AutoSequenceIncrement;
						dict["IPIndex:Clustered"] = repo.Clustered;
						dict["IPIndex:ColumnCount"] = repo.ColumnCount;
						dict["IPIndex:Descending"] = repo.Descending;
						dict["IPIndex:Name"] = repo.Name;
						dict["IPIndex:NoPick"] = repo.NoPick;
						dict["IPIndex:Number"] = repo.Number;
						dict["IPIndex:Primary"] = repo.Primary;
						dict["IPIndex:Unique"] = repo.Unique;

						for (int i = 1, ii = repo.ColumnCount; i <= ii; i++)
						{
							var col = repo.Columns[i];
							var name = string.Format("IPTable:Columns[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, col, col.Name);
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPInterfaceType
				try
				{
					if (obj is DICTSRVRLib.IPInterfaceType)
					{
						var repo = obj as DICTSRVRLib.IPInterfaceType;
						dict["IPInterfaceType:CallType"] = repo.CallType;
						dict["IPInterfaceType:InterfaceName"] = repo.InterfaceName;
						dict["IPInterfaceType:LocatorID"] = repo.LocatorID;
						dict["IPInterfaceType:LocatorType"] = repo.LocatorType;
						dict["IPInterfaceType:LocatorTypeText"] = repo.LocatorTypeText[0];
						dict["IPInterfaceType:Major"] = repo.Major;
						dict["IPInterfaceType:MethodCount"] = repo.MethodCount;
						dict["IPInterfaceType:Minor"] = repo.Minor;
						dict["IPInterfaceType:Name"] = repo.Name;
						dict["IPInterfaceType:PropertyCount"] = repo.PropertyCount;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPInterfaceTypeCOM
				try
				{
					if (obj is DICTSRVRLib.IPInterfaceTypeCOM)
					{
						var repo = obj as DICTSRVRLib.IPInterfaceTypeCOM;
						dict["IPInterfaceTypeCOM:ClassGUID"] = repo.ClassGUID;
						dict["IPInterfaceTypeCOM:ClassName"] = repo.ClassName;
						dict["IPInterfaceTypeCOM:InterfaceGUID"] = repo.InterfaceGUID;
						dict["IPInterfaceTypeCOM:Parent"] = new MoreInfoItem("IPInterfaceTypeCOM:Parent", repo.Parent, repo.Parent != null ? repo.Parent.Name : NullStr);
						dict["IPInterfaceTypeCOM:TypeLibGUID"] = repo.TypeLibGUID;
						dict["IPInterfaceTypeCOM:WSDLFile"] = repo.WSDLFile;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPInterfaceTypeNET
				try
				{
					if (obj is DICTSRVRLib.IPInterfaceTypeNET)
					{
						var repo = obj as DICTSRVRLib.IPInterfaceTypeNET;
						dict["IPInterfaceTypeNET:AssemblyName"] = repo.AssemblyName;
						dict["IPInterfaceTypeNET:Located"] = repo.Located;
						dict["IPInterfaceTypeNET:Parent"] = new MoreInfoItem("IPInterfaceTypeNET:Parent", repo.Parent, repo.Parent != null ? repo.Parent.Name : NullStr);
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPRelationship
				try
				{
					var repo = obj as DICTSRVRLib.IPRelationship;
					if (repo != null)
					{
						dict["IPRelationship:Name"] = repo.Name;
						dict["IPRelationship:Path"] = repo.Path;
						dict["IPRelationship:Snapshot"] = repo.Snapshot;
						dict["IPRelationship:Type"] = repo.Type;
						dict["IPRelationship:Comment"] = repo.Comment[0];
						dict["IPRelationship:Prompt"] = repo.Prompt[0];

						var child = repo.Child;
						dict["IPRelationship:Child"] = child != null ? new MoreInfoItem("IPRelationship:Child", child, child.Name) : null;

						var parent = repo.Parent;
						dict["IPRelationship:Parent"] = parent != null ? new MoreInfoItem("IPRelationship:Parent", parent, parent.Name) : null;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPStringDefine
				try
				{
					var repo = obj as DICTSRVRLib.IPStringDefine;
					if (repo != null)
					{
						dict["IPStringDefine:Name"] = repo.Name;
						dict["IPStringDefine:String"] = repo.String[0];
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPTable
				try
				{
					if (obj is DICTSRVRLib.IPTable)
					{
						var repo = obj as DICTSRVRLib.IPTable;
						dict["IPTable:ActionCount"] = repo.ActionCount;
						dict["IPTable:ColumnCount"] = repo.ColumnCount;
						dict["IPTable:DatabaseNum"] = repo.DatabaseNum;
						dict["IPTable:History"] = repo.History;
						dict["IPTable:IndexCount"] = repo.IndexCount;
						dict["IPTable:LoadedHelp"] = repo.LoadedHelp;
						dict["IPTable:LooselyCoupled"] = repo.LooselyCoupled;
						dict["IPTable:Modal"] = repo.Modal;
						dict["IPTable:Name"] = repo.Name;
						dict["IPTable:Number"] = repo.Number;
						dict["IPTable:Persist"] = repo.Persist;
						dict["IPTable:Pick"] = repo.Pick;
						dict["IPTable:PrimaryIndex"] = repo.PrimaryIndex != null ? new MoreInfoItem("IPTable:PrimaryIndex", repo.PrimaryIndex, repo.PrimaryIndex.Name) : null;
						dict["IPTable:Sequel"] = repo.Sequel;
						dict["IPTable:Snapshot"] = repo.Snapshot;
						dict["IPTable:Updates"] = repo.Updates;

						for (int i = 1, ii = repo.ActionCount; i <= ii; i++)
						{
							var action = repo.Actions[i];
							var name = string.Format("IPTable:Actions[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, action, action.Name);
						}

						for (int i = 1, ii = repo.ColumnCount; i <= ii; i++)
						{
							var col = repo.Columns[i];
							var name = string.Format("IPTable:Columns[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, col, col.Name);
						}

						for (int i = 1, ii = repo.IndexCount; i <= ii; i++)
						{
							var index = repo.Indexes[i];
							var name = string.Format("IPTable:Indexes[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, index, index.Name);
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPTag
				try
				{
					if (obj is DICTSRVRLib.IPTag)
					{
						var repo = obj as DICTSRVRLib.IPTag;
						dict["IPTag:Data"] = repo.Data;
						dict["IPTag:GermaneKey"] = repo.GermaneKey;
						dict["IPTag:Name"] = repo.Name;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPTrigger
				try
				{
					if (obj is DICTSRVRLib.IPTrigger)
					{
						var repo = obj as DICTSRVRLib.IPTrigger;
						dict["IPTrigger:Name"] = repo.Name;
						dict["IPTrigger:Sft"] = repo.Sft;
						dict["IPTrigger:IsClient"] = repo.IsClient();
						dict["IPTrigger:IsServer"] = repo.IsServer();
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPTypeDefine
				try
				{
					if (obj is DICTSRVRLib.IPTypeDefine)
					{
						var repo = obj as DICTSRVRLib.IPTypeDefine;
						dict["IPTypeDefine:Name"] = repo.Name;
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				#region IPWorkspace
				try
				{
					if (obj is DICTSRVRLib.IPWorkspace)
					{
						var repo = obj as DICTSRVRLib.IPWorkspace;
						dict["FileCount"] = repo.FileCount;
						dict["Name"] = repo.Name;

						for (int i = 1, ii = repo.FileCount; i <= ii; i++)
						{
							var file = repo.Files[i];
							var name = string.Format("IPWorkspace:Files[{0:000}]", i);
							dict[name] = new MoreInfoItem(name, file, file.Name);
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
				#endregion

				foreach (var key in dict.Keys)
				{
					var index = key.IndexOf(':');
					var intf = string.Empty;
					var propName = key;
					if (index >= 0)
					{
						intf = key.Substring(0, index);
						propName = key.Substring(index + 1);
					}

					var value = dict[key];
					var text = value != null ? value.ToString() : string.Empty;

					items.Add(new RepoInfoItem(intf, propName, value, text, indent));
				}

				return items;
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return items;
			}
		}

		public static string GetObjectTypeText(object obj)
		{
			if (obj == null) return string.Empty;

			var type = obj.GetType();
			if (type == typeof(string)) return "string";
			if (type == typeof(int)) return "int";
			if (type == typeof(uint)) return "uint";
			if (type == typeof(short)) return "short";
			if (type == typeof(ushort)) return "ushort";
			if (type == typeof(char)) return "char";

			var sb = new StringBuilder();
			Action<string> addType = new Action<string>(typeName =>
			{
				if (sb.Length > 0) sb.Append(", ");
				sb.Append(typeName);
			});

			if (obj is DICTSRVRLib.IInternalNetInfo) addType("IInternalNetInfo");
			if (obj is DICTSRVRLib.IInternalNetMember) addType("IInternalNetMember");
			if (obj is DICTSRVRLib.IInternalNetObject) addType("IInternalNetObject");
			if (obj is DICTSRVRLib.IPAction) addType("IPAction");
			if (obj is DICTSRVRLib.IPColumn) addType("IPColumn");
			if (obj is DICTSRVRLib.IPDataDef) addType("IPDataDef");
			if (obj is DICTSRVRLib.IPDictionary) addType("IPDictionary");
			if (obj is DICTSRVRLib.IPDictionaryConsts) addType("IPDictionaryConsts");
			if (obj is DICTSRVRLib.IPDictObj) addType("IPDictObj");
			if (obj is DICTSRVRLib.IPDObjDesc) addType("IPDObjDesc");
			if (obj is DICTSRVRLib.IPFile) addType("IPFile");
			if (obj is DICTSRVRLib.IPFrmItem) addType("IPFrmItem");
			if (obj is DICTSRVRLib.IPHelp) addType("IPHelp");
			if (obj is DICTSRVRLib.IPHelpKey) addType("IPHelpKey");
			if (obj is DICTSRVRLib.IPIndex) addType("IPIndex");
			if (obj is DICTSRVRLib.IPInterfaceType) addType("IPInterfaceType");
			if (obj is DICTSRVRLib.IPInterfaceTypeCOM) addType("IPInterfaceTypeCOM");
			if (obj is DICTSRVRLib.IPInterfaceTypeNET) addType("IPInterfaceTypeNET");
			if (obj is DICTSRVRLib.IPLoadMonitor) addType("IPLoadMonitor");
			if (obj is DICTSRVRLib.IPRelationship) addType("IPRelationship");
			if (obj is DICTSRVRLib.IPRepository) addType("IPRepository");
			if (obj is DICTSRVRLib.IPStringDefine) addType("IPStringDefine");
			if (obj is DICTSRVRLib.IPTable) addType("IPTable");
			if (obj is DICTSRVRLib.IPTag) addType("IPTag");
			if (obj is DICTSRVRLib.IPTrigger) addType("IPTrigger");
			if (obj is DICTSRVRLib.IPTypeDefine) addType("IPTypeDefine");
			if (obj is DICTSRVRLib.IPWorkspace) addType("IPWorkspace");
			if (obj is DICTSRVRLib.PDataDef) addType("PDataDef");
			if (obj is DICTSRVRLib.PDictionary) addType("PDictionary");
			if (obj is DICTSRVRLib.PHelp) addType("PHelp");
			if (obj is DICTSRVRLib.PInterfaceType) addType("PInterfaceType");
			if (obj is DICTSRVRLib.PRepository) addType("PRepository");
			if (sb.Length != 0) return sb.ToString();

			var name = type.Name;
			var index = name.LastIndexOf('.');
			if (index >= 0) name = name.Substring(index + 1);
			return name;
		}
	}
}
