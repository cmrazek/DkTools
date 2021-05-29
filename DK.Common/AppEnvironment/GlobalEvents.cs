using DK.Diagnostics;
using DK.Preprocessing;
using System;

namespace DK.AppEnvironment
{
	public static class GlobalEvents
	{
		public static event EventHandler AppChanged;
		public static event EventHandler RefreshAllDocumentsRequired;
		public static event EventHandler<RefreshDocumentEventArgs> RefreshDocumentRequired;
		public static event EventHandler<FileEventArgs> FileChanged;
		public static event EventHandler<FileEventArgs> FileDeleted;


		public static void OnAppChanged()
		{
			try
			{
				AppChanged?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static void OnRefreshAllDocumentsRequired()
		{
			try
			{
				RefreshAllDocumentsRequired?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static void OnRefreshDocumentRequired(string filePath)
		{
			try
			{
				RefreshDocumentRequired?.Invoke(null, new RefreshDocumentEventArgs(filePath));
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static void OnFileChanged(string filePath)
		{
			try
			{
				FileChanged?.Invoke(null, new FileEventArgs(filePath));
				IncludeFileCache.OnFileChanged(filePath);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static void OnFileDeleted(string filePath)
		{
			try
			{
				FileDeleted?.Invoke(null, new FileEventArgs(filePath));
				IncludeFileCache.OnFileChanged(filePath);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}
	}

	public class RefreshDocumentEventArgs : EventArgs
	{
		public string FilePath { get; private set; }

		public RefreshDocumentEventArgs(string filePath)
		{
			FilePath = filePath;
		}
	}

	public class FileEventArgs : EventArgs
	{
		public FileEventArgs(string filePath)
		{
			FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
		}

		public string FilePath { get; private set; }
	}
}
