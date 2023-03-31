using DK.AppEnvironment;
using DK.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DkTools.AppEnvironment
{
    class DkFileSystemWatcher
    {
        private DkAppContext _app;
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        public DkFileSystemWatcher(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            _app.AppChanged += DkAppContext_AppChanged;

            CreateFileSystemWatcher(_app.Settings);
        }

        private void DkAppContext_AppChanged(object sender, AppSettingsEventArgs e)
        {
            try
            {
                StopFileSystemWatcher();
                CreateFileSystemWatcher(e.AppSettings);
            }
            catch (Exception ex)
            {
                _app.Log.Error(ex);
            }
        }

        private void CreateFileSystemWatcher(DkAppSettings appSettings)
        {
            // Create a master list of parent directories only so there are no redundant file system watchers created.
            var masterDirs = new List<string>();
            foreach (var dir in appSettings.SourceDirs.Concat(appSettings.IncludeDirs))
            {
                var placedDir = false;

                for (int i = 0; i < masterDirs.Count; i++)
                {
                    if (FileHelper.PathIsSameOrChildDir(masterDirs[i], dir))
                    {
                        // What we have saved is a child, so swap it with it's parent
                        masterDirs[i] = dir;
                        placedDir = true;
                        break;
                    }
                    else if (FileHelper.PathIsSameOrChildDir(dir, masterDirs[i]))
                    {
                        // This directory is already covered by one in the master list.
                        placedDir = true;
                        break;
                    }
                }

                if (!placedDir)
                {
                    masterDirs.Add(dir);
                }
            }

            // Create a watcher for each master dir.
            foreach (var dir in masterDirs)
            {
                try
                {
                    if (!Directory.Exists(dir)) continue;

                    _app.Log.Info("Creating file system watcher for directory: {0}", dir);

                    var watcher = new FileSystemWatcher();
                    _watchers.Add(watcher);

                    watcher.Path = dir;
                    watcher.Filter = "*.*";
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
                    watcher.IncludeSubdirectories = true;

                    watcher.Changed += OnFileChanged;
                    watcher.Deleted += OnFileDeleted;
                    watcher.Renamed += OnFileRenamed;
                    watcher.Created += OnFileCreated;
                    watcher.Error += OnError;

                    watcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    _app.Log.Error(ex, "Exception when trying to create FileSystemWatcher for '{0}'.", dir);
                }
            }
        }

        private void StopFileSystemWatcher()
        {
            while (_watchers.Count > 0)
            {
                try
                {
                    var watcher = _watchers.First();

                    _app.Log.Info("Stopping file system watcher for directory: {0}", watcher.Path);

                    watcher.Dispose();
                    _watchers.RemoveAt(0);
                }
                catch (Exception ex)
                {
                    _app.Log.Error(ex, "Exception when trying to stop FileSystemWatcher.");
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (DkEnvironment.IsProbeFile(e.FullPath, _app.FileSystem))
            {
                _app.Log.Debug("File change detected: {0}", e.FullPath);
                _app.OnFileChanged(e.FullPath);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (DkEnvironment.IsProbeFile(e.FullPath, _app.FileSystem))
            {
                _app.Log.Debug("File deletion detected: {0}", e.FullPath);
                _app.OnFileDeleted(e.FullPath);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (DkEnvironment.IsProbeFile(e.OldFullPath, _app.FileSystem))
            {
                _app.Log.Debug("File rename detected: {0} -> {1}", e.OldFullPath, e.FullPath);
                _app.OnFileDeleted(e.OldFullPath);
            }
            else if (DkEnvironment.IsProbeFile(e.FullPath, _app.FileSystem))
            {
                _app.Log.Debug("File rename detected: {0} -> {1}", e.OldFullPath, e.FullPath);
                _app.OnFileChanged(e.FullPath);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (DkEnvironment.IsProbeFile(e.FullPath, _app.FileSystem))
            {
                _app.Log.Debug("File create detected: {0}", e.FullPath);
                _app.OnFileChanged(e.FullPath);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _app.Log.Warning("File system watcher error: {0}", e.GetException());
        }
    }
}
