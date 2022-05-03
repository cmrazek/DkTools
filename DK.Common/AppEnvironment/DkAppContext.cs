using DK.Diagnostics;
using DK.Preprocessing;
using System;

namespace DK.AppEnvironment
{
    public class DkAppContext
    {
        private IFileSystem _fs;
        private ILogger _log;
        private IAppConfigSource _config;
        private DkAppSettings _settings;
        private IncludeFileCache _includeFileCache;

        public event EventHandler<AppSettingsEventArgs> AppChanged;
        public event EventHandler RefreshAllDocumentsRequired;
        public event EventHandler<RefreshDocumentEventArgs> RefreshDocumentRequired;
        public event EventHandler<FileEventArgs> FileChanged;
        public event EventHandler<FileEventArgs> FileDeleted;

        public DkAppContext(IFileSystem fileSystem, ILogger log, IAppConfigSource config)
        {
            _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _settings = new DkAppSettings(this);
            _includeFileCache = new IncludeFileCache(this);
        }

        public IAppConfigSource Config => _config;
        public IFileSystem FileSystem => _fs;
        internal IncludeFileCache IncludeFileCache => _includeFileCache;
        public ILogger Log => _log;
        public DkAppSettings Settings => _settings;

        public void LoadAppSettings(string appName)
        {
            var settings = DkEnvironment.LoadAppSettings(this, appName);
            _settings = settings;
            OnAppChanged(settings);
            OnRefreshAllDocumentsRequired();
        }

        public void OnAppChanged(DkAppSettings newAppSettings)
        {
            try
            {
                _includeFileCache.OnAppChanged();
                AppChanged?.Invoke(null, new AppSettingsEventArgs(newAppSettings));
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void OnRefreshAllDocumentsRequired()
        {
            try
            {
                RefreshAllDocumentsRequired?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void OnRefreshDocumentRequired(string filePath)
        {
            try
            {
                RefreshDocumentRequired?.Invoke(null, new RefreshDocumentEventArgs(filePath));
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void OnFileChanged(string filePath)
        {
            try
            {
                FileChanged?.Invoke(null, new FileEventArgs(filePath));
                _includeFileCache.OnFileChanged(filePath);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void OnFileDeleted(string filePath)
        {
            try
            {
                FileDeleted?.Invoke(null, new FileEventArgs(filePath));
                _includeFileCache.OnFileChanged(filePath);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
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

    public class AppSettingsEventArgs : EventArgs
    {
        public DkAppSettings AppSettings { get; private set; }

        public AppSettingsEventArgs(DkAppSettings appSettings)
        {
            AppSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }
    }
}
