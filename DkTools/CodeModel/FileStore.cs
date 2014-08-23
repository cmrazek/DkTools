using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal class FileStore
	{
		private Dictionary<string, CodeFile> _sameDirIncludeFiles = new Dictionary<string, CodeFile>();
		private Dictionary<string, CodeFile> _globalIncludeFiles = new Dictionary<string, CodeFile>();
		private CodeModel _model;

		public static FileStore GetOrCreateForTextBuffer(VsText.ITextBuffer buf)
		{
			if (buf == null) throw new ArgumentNullException("buf");

			FileStore cache;
			if (buf.Properties.TryGetProperty(typeof(FileStore), out cache)) return cache;

			cache = new FileStore();
			buf.Properties[typeof(FileStore)] = cache;

			return cache;
		}

		public CodeFile TryGetIncludeFile(string fileName, bool searchSameDir)
		{
			CodeFile file;
			if (searchSameDir)
			{
				if (_sameDirIncludeFiles.TryGetValue(fileName.ToLower(), out file)) return file;
			}
			else
			{
				if (_globalIncludeFiles.TryGetValue(fileName.ToLower(), out file)) return file;
			}

			return null;
		}

		public void SaveIncludeFile(string fileName, bool sameDir, CodeFile file)
		{
			if (sameDir) _sameDirIncludeFiles[fileName.ToLower()] = file;
			else _globalIncludeFiles[fileName.ToLower()] = file;
		}

		public CodeModel GetOrCreateModelForSnapshot(VsText.ITextSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");

			if (_model != null)
			{
				if (snapshot != null && _model.Snapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
				{
					_model = new CodeModel(snapshot);
					_model.Snapshot = snapshot;
				}
			}
			else
			{
				_model = new CodeModel(snapshot);
				_model.Snapshot = snapshot;
			}

			return _model;
		}

		public CodeModel TryGetModel()
		{
			return _model;
		}
	}
}
