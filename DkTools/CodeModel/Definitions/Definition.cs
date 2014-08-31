using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel.Definitions
{
	internal abstract class Definition
	{
		private string _name;
		private bool _global;
		private Token _sourceToken;
		private CodeFile _sourceFile;
		private string _sourceFileName;
		private Span _sourceSpan;
		private bool _sourcePrimary;
		private bool _preprocessor;

		// TODO: remove
		//private bool _gotLocalFileInfo;
		//private string _localFileName;
		//private Span _localFileSpan;
		//private bool _localPrimaryFile;

		public abstract bool CompletionVisible { get; }
		public abstract StatementCompletion.CompletionType CompletionType { get; }
		public abstract string CompletionDescription { get; }
		public abstract Classifier.ProbeClassifierType ClassifierType { get; }
		public abstract string QuickInfoText { get; }

		public Definition(Scope scope, string name, Token sourceToken, bool global)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
#endif
			_name = name;
			_global = global;

			if (sourceToken != null)
			{
				_sourceToken = sourceToken;
				_sourceFile = sourceToken.File;
				_sourceSpan = sourceToken.Span;
				_sourcePrimary = true;

				if (_sourceFile != null)
				{
					_sourceFileName = _sourceFile.FileName;
				}
				else if (sourceToken is ExternalToken)
				{
					_sourceFileName = (sourceToken as ExternalToken).FileName;
				}
				else
				{
					throw new InvalidOperationException("Source token has no file object.");
				}
			}

			_preprocessor = scope.Preprocessor;
		}

		public string Name
		{
			get { return _name; }
		}

		public Token SourceToken
		{
			get { return _sourceToken; }
		}

		public bool Global
		{
			get { return _global; }
		}

		public CodeFile SourceFile
		{
			get { return _sourceFile; }
		}

		public string SourceFileName
		{
			get { return _sourceFileName; }
		}

		public Span SourceSpan
		{
			get { return _sourceSpan; }
			set { _sourceSpan = value; }
		}

		public bool SourcePrimary
		{
			get { return _sourcePrimary; }
		}

		public string LocationText
		{
			get
			{
				return string.Concat(_sourceFileName, "(", _sourceSpan.Start.LineNum + 1, ")");
			}
		}

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement(GetType().Name);
			DumpTreeAttribs(xml);
			xml.WriteEndElement();
		}

		public virtual void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("name", _name);
			xml.WriteAttributeString("global", _global.ToString());
			xml.WriteAttributeString("sourceSpan", _sourceSpan.ToString());
			xml.WriteAttributeString("preprocessor", _preprocessor.ToString());
		}

		public bool Preprocessor
		{
			get { return _preprocessor; }
		}

		/// <summary>
		/// Migrates a definition from the preprocessor to the visible model.
		/// </summary>
		/// <param name="visibleFile">The new CodeFile. At the time of calling, this object is not yet populated, and will not contain a code tree.</param>
		/// <param name="visibleSource">The new CodeSource. This object is populated with the actual text visible on screen.</param>
		/// <returns>True if the definition resides in the visible model; otherwise false.</returns>
		/// <remarks>If a subclass overrides this method, it should call the base method AFTER performing it's own transformation, so that _sourceFile still points to the original.
		/// Also, SourceFile could be null for external or built-in definitions.</remarks>
		public virtual bool MoveFromPreprocessorToVisibleModel(CodeFile visibleFile, CodeSource visibleSource)
		{
			if (_sourceFile != null)
			{
				string fileName;
				Span span;
				bool primary;
				_sourceFile.CodeSource.GetFileSpan(_sourceSpan, out fileName, out span, out primary);
				_sourceFileName = fileName;
				_sourceSpan = span;
				_sourcePrimary = primary;
				_sourceFile = visibleFile;
			}

			_preprocessor = false;
			return _sourcePrimary;
		}

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Type [{0}]", GetType());
			sb.AppendFormat(" File [{0}]", SourceFile != null ? SourceFile.FileName : "(null)");
			sb.AppendFormat(" Offset [{0}]", SourceSpan.Start.Offset);
			sb.AppendFormat(" CompletionType [{0}]", CompletionType);
			sb.AppendFormat(" CompletionDescription [{0}]", CompletionDescription);
			return sb.ToString();
		}
#endif
	}
}
