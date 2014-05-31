using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools
{
	internal static class CodeModelStore
	{
		private static Dictionary<ITextBuffer, CodeModel.CodeModel> _items = new Dictionary<ITextBuffer, CodeModel.CodeModel>();

		private const int k_refreshTime = 1;	// seconds
		private const int k_purgeTime = 60;		// seconds

		public static CodeModel.CodeModel GetModelForBuffer(ITextBuffer buffer, ITextSnapshot snapshot, bool createIfMissing)
		{
#if DEBUG
			if (buffer == null) throw new ArgumentNullException("buffer");
#endif

			CodeModel.CodeModel model;
			bool found;

			lock (_items)
			{
				found = _items.TryGetValue(buffer, out model);
			}

			if (found)
			{
				if (snapshot != null && model.Snapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
				{
					model = new CodeModel.CodeModel(snapshot);
					model.Snapshot = snapshot;
					SaveModelIfNewer(buffer, model);
				}
			}
			else if (createIfMissing)
			{
				if (snapshot == null) snapshot = buffer.CurrentSnapshot;

				model = new CodeModel.CodeModel(snapshot);
				model.Snapshot = snapshot;
				SaveModelIfNewer(buffer, model);
			}

			if (model != null) model.LastAccessTime = DateTime.Now;
			return model;
		}

		public static void OnIdle()
		{
			var now = DateTime.Now;
			var purgeTime = now.AddSeconds(-k_purgeTime);
			var refreshTime = now.AddSeconds(-k_refreshTime);

			List<ITextBuffer> purgeList = null;
			List<ITextBuffer> refreshList = null;

			lock (_items)
			{
				foreach (var buf in _items.Keys)
				{
					var model = _items[buf];
					var time = model.LastAccessTime;
					if (time <= purgeTime)
					{
						if (purgeList == null) purgeList = new List<ITextBuffer>();
						purgeList.Add(buf);
					}
					else if (model.Snapshot != null && model.Snapshot.Version.VersionNumber < buf.CurrentSnapshot.Version.VersionNumber && model.RefreshTime < refreshTime)
					{
						if (refreshList == null) refreshList = new List<ITextBuffer>();
						refreshList.Add(buf);
						model.RefreshTime = now;
					}
				}

				if (purgeList != null)
				{
					foreach (var buf in purgeList)
					{
						_items.Remove(buf);
					}
				}
			}

			if (refreshList != null)
			{
				foreach (var buf in refreshList)
				{
					var currSnapshot = buf.CurrentSnapshot;
					ProbeToolsPackage.Instance.StartBackgroundWorkItem(() =>
						{
							var model = new CodeModel.CodeModel(currSnapshot);
							model.Snapshot = currSnapshot;
							model.LastAccessTime = _items[buf].LastAccessTime;
							SaveModelIfNewer(buf, model);
						});
				}
			}
		}

		private static void SaveModelIfNewer(ITextBuffer buffer, CodeModel.CodeModel model)
		{
			lock (_items)
			{
				CodeModel.CodeModel nowModel;
				if (!_items.TryGetValue(buffer, out nowModel) || nowModel.Snapshot.Version.VersionNumber < model.Snapshot.Version.VersionNumber)
				{
					_items[buffer] = model;
				}
			}
		}

		public static string GetFileNameForBuffer(ITextBuffer buffer)
		{
			// http://social.msdn.microsoft.com/Forums/vstudio/en-US/ef5cd137-56e4-4077-8e31-6d282668e8ad/filename-from-itextbuffer-or-itextbuffer-from-projectitem

			IVsTextBuffer bufferAdapter;
			if (!buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out bufferAdapter) || bufferAdapter == null) return null;

			var persistFileFormat = bufferAdapter as IPersistFileFormat;
			if (persistFileFormat == null) return null;

			string fileName = null;
			uint formatIndex = 0;
			persistFileFormat.GetCurFile(out fileName, out formatIndex);

			return fileName;
		}

		public static int Count
		{
			get { return _items.Count; }
		}
	}
}
