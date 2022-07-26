using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Preprocessing
{
    internal class IncludeFileCache
    {
        private DkAppContext _app;
        private LinkedList<IncludeFileNode> _files = new LinkedList<IncludeFileNode>();
        private int _cacheSize;
        private int _numFiles;

        private const int MaxCacheSize = 4 * 1024 * 1024;
        private const int MinFileCount = 5;

        private class IncludeFileNode
        {
            public string FullPathName { get; set; }
            public CodeSource Source { get; set; }
        }

        public IncludeFileCache(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public CodeSource GetIncludeFileSource(DkAppSettings appSettings, string fullPathName, bool doMerge)
        {
            lock (_files)
            {
                var node = _files.Where(x => x.FullPathName.EqualsI(fullPathName)).FirstOrDefault();
                if (node != null)
                {
                    _files.Remove(node);
                    _files.AddFirst(node);
                    return node.Source;
                }
            }

            try
            {
                if (_app.FileSystem.FileExists(fullPathName))
                {
                    var fileContent = _app.FileSystem.GetFileText(fullPathName);

                    CodeSource source;
                    if (doMerge)
                    {
                        var merger = new FileMerger(appSettings);
                        merger.MergeFile(fullPathName, fileContent, showMergeComments: false, fileIsPrimary: false);
                        source = merger.MergedContent;
                    }
                    else
                    {
                        source = new CodeSource();
                        source.Append(fileContent, fullPathName, fileStartPos: 0, fileEndPos: fileContent.Length, actualContent: true, primaryFile: false, disabled: false);
                        source.Flush();
                    }

                    lock (_files)
                    {
                        _files.AddFirst(new IncludeFileNode
                        {
                            FullPathName = fullPathName,
                            Source = source
                        });

                        _cacheSize += source.Length;
                        _numFiles++;

                        while (_cacheSize > MaxCacheSize && _numFiles > MinFileCount)
                        {
                            var node = _files.Last;
                            _files.RemoveLast();
                            _cacheSize -= node.Value.Source.Length;
                            _numFiles--;
                        }
                    }

                    return source;
                }
            }
            catch (Exception ex)
            {
                _app.Log.Error(ex, "Exception when loading include file: {0}", fullPathName);
                return null;
            }

            return null;
        }

        public void OnFileChanged(string fullPathName)
        {
            lock (_files)
            {
                var node = _files.Where(x => x.FullPathName.EqualsI(fullPathName)).FirstOrDefault();
                if (node != null) _files.Remove(node);
            }
        }

        public void OnAppChanged()
        {
            lock (_files)
            {
                _files.Clear();
            }
        }
    }
}
