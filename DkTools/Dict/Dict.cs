using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DICTSRVRLib;

namespace DkTools.Dict
{
	internal sealed class Dict : IDisposable
	{
		private PRepository _repo;
		private PDictionary _dict;

		public Dict(string appName)
		{
			_repo = new PRepository();
			_dict = _repo.LoadDictionary(appName, string.Empty, DICTSRVRLib.PDS_Access.Access_BROWSE);
		}

		public void Dispose()
		{
			_dict = null;
			_repo = null;
		}

		public IEnumerable<IPTable> Tables
		{
			get
			{
				if (_dict == null) yield break;
				for (int i = 1, ii = _dict.TableCount; i <= ii; i++)
				{
					var table = _dict.Tables[i];
					if (table != null) yield return table;
				}
			}
		}

		public IPTable GetTable(string name)
		{
			if (_dict == null) return null;
			return _dict.Tables[name];
		}

		public IEnumerable<IPStringDefine> StringDefines
		{
			get
			{
				if (_dict == null) yield break;
				for (int i = 1, ii = _dict.StringDefineCount; i <= ii; i++)
				{
					var sd = _dict.StringDefines[i];
					if (sd != null) yield return sd;
				}
			}
		}

		public IPStringDefine GetStringDefine(string name)
		{
			if (_dict == null) return null;
			return _dict.StringDefines[name];
		}

		public IEnumerable<IPTypeDefine> TypeDefines
		{
			get
			{
				if (_dict == null) yield break;
				for (int i = 1, ii = _dict.TypeDefineCount; i <= ii; i++)
				{
					var td = _dict.TypeDefines[i];
					if (td != null) yield return td;
				}
			}
		}

		public IPTypeDefine GetTypeDefine(string name)
		{
			if (_dict == null) return null;
			return _dict.TypeDefines[name];
		}

		public IEnumerable<IPRelationship> Relationships
		{
			get
			{
				if (_dict == null) yield break;
				for (int i = 1, ii = _dict.RelationshipCount; i <= ii; i++)
				{
					var rel = _dict.Relationships[i];
					if (rel != null) yield return rel;
				}
			}
		}

		public IPRelationship GetRelationship(string name)
		{
			if (_dict == null) return null;
			return _dict.Relationships[name];
		}

		public IEnumerable<IPInterfaceType> Interfaces
		{
			get
			{
				if (_dict == null) yield break;
				for (int i = 1, ii = _dict.InterfaceTypeCount; i <= ii; i++)
				{
					var it = _dict.InterfaceTypes[i];
					if (it != null) yield return it;
				}
			}
		}

		public IPInterfaceType GetInterface(string name)
		{
			if (_dict == null) return null;
			return _dict.InterfaceTypes[name];
		}

	}
}
