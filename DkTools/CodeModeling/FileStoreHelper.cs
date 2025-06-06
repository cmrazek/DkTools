using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DK.Preprocessing;
using DkTools.Classifier;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DkTools.CodeModeling
{
    static class FileStoreHelper
    {
        public static FileStore GetOrCreateForTextBuffer(ITextBuffer buf)
        {
            if (buf == null) throw new ArgumentNullException("buf");

            if (buf.ContentType.TypeName != Constants.DkContentType) return null;

            FileStore cache;
            if (buf.Properties.TryGetProperty(typeof(FileStore), out cache)) return cache;

            cache = new FileStore(ProbeToolsPackage.Instance.App);
            buf.Properties[typeof(FileStore)] = cache;

            return cache;
        }

        public static CodeModel GetCodeModelOrNull(ITextBuffer buffer) => GetOrCreateForTextBuffer(buffer)?.Model;

        public static DefinitionProvider GetDefinitionProviderOrNull(ITextBuffer buffer) => GetOrCreateForTextBuffer(buffer)?.Model?.DefinitionProvider;

        public static CodeModel GetCurrentModelSync(this FileStore fileStore,
            DkAppSettings appSettings,
            string fileName,
            ITextSnapshot snapshot,
            string reason,
            CodeScanMode scanMode,
            CancellationToken cancel)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            if (fileStore.Model != null)
            {
                var modelSnapshot = fileStore.Model.Snapshot as ITextSnapshot;
                if (snapshot != null && (modelSnapshot == null || modelSnapshot.Version.VersionNumber < snapshot.Version.VersionNumber))
                {
                    fileStore.Model = fileStore.CreatePreprocessedModelSync(appSettings, fileName, snapshot, reason, scanMode, cancel);
                }
            }
            else
            {
                fileStore.Model = fileStore.CreatePreprocessedModelSync(appSettings, fileName, snapshot, reason, scanMode, cancel);
            }

            return fileStore.Model;
        }

        public static CodeModel GetMostRecentModelSync(this FileStore fileStore,
            DkAppSettings appSettings,
            string fileName,
            ITextSnapshot snapshot,
            string reason,
            CodeScanMode scanMode,
            CancellationToken cancel)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            if (fileStore.Model == null)
            {
                fileStore.Model = fileStore.CreatePreprocessedModelSync(appSettings, fileName, snapshot, reason, scanMode, cancel);
            }

            return fileStore.Model;
        }

        public static CodeModel CreatePreprocessedModelSync(this FileStore fileStore,
            DkAppSettings appSettings,
            string fileName,
            ITextSnapshot snapshot,
            string reason,
            CodeScanMode scanMode,
            CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            var source = new CodeSource();
            source.Append(snapshot.GetText(), fileName, 0, snapshot.Length, true, true, false);
            source.Flush();

            var model = fileStore.CreatePreprocessedModel(appSettings, source, fileName, true, reason, scanMode, cancel, null);
            model.Snapshot = snapshot;
            return model;
        }

        public static CodeModel CreatePreprocessedModelSync(this FileStore fileStore,
            DkAppSettings appSettings,
            string fileName,
            ITextSnapshot snapshot,
            bool visible,
            string reason,
            CodeScanMode scanMode,
            CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            CodeSource source;
            IEnumerable<IncludeDependency> includeDependencies = null;
            if (visible || string.IsNullOrEmpty(fileName))
            {
                source = new CodeSource();
                source.Append(snapshot.GetText(), fileName, 0, snapshot.Length, true, true, false);
                source.Flush();
            }
            else
            {
                var merger = new FileMerger(appSettings);
                merger.MergeFile(fileName, snapshot.GetText(), false, true);
                source = merger.MergedContent;

                includeDependencies = (from f in merger.FileNames
                                       select new IncludeDependency(f, false, true, merger.GetFileContent(f))).ToArray();
            }

            var model = fileStore.CreatePreprocessedModel(appSettings, source, fileName, visible, reason, scanMode, cancel, includeDependencies);
            model.Snapshot = snapshot;
            return model;
        }

        public static IEnumerable<FunctionDropDownItem> GetFunctionDropDownList(this FileStore fileStore, CodeModel model)
        {
            if (fileStore == null) throw new ArgumentNullException(nameof(fileStore));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var prepModel = model.PreprocessorModel;
            if (prepModel == null) yield break;

            foreach (var func in model.PreprocessorModel.LocalFunctions)
            {
                var def = func.Definition;
                if (def.EntireSpan.Length == 0) continue;
                if (!def.SourceFileName.Equals(model.FilePath, StringComparison.OrdinalIgnoreCase)) continue;

                yield return new FunctionDropDownItem(def, def.Name, new CodeSpan(def.SourceStartPos, def.SourceStartPos), def.EntireSpan);
            }
        }
    }

    public class FunctionDropDownItem : INotifyPropertyChanged
    {
        public FunctionDropDownItem(FunctionDefinition definition, string name, CodeSpan span, CodeSpan entireFunctionSpan)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Name = name;
            Span = span;
            EntireFunctionSpan = entireFunctionSpan;
        }

        public FunctionDefinition Definition { get; private set; }
        public string Name { get; private set; }
        public CodeSpan Span { get; private set; }
        public CodeSpan EntireFunctionSpan { get; private set; }

        public FrameworkElement ClassifiedSignatureElement
        {
            get => Definition.Signature.ClassifiedString.ToWpfTextBlock();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Visibility _visibility = Visibility.Visible;
        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Visibility)));
                }
            }
        }
    }
}
