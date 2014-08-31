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

		private bool _gotLocalFileInfo;
		private string _localFileName;
		private Span _localFileSpan;
		private bool _localPrimaryFile;

		public abstract bool CompletionVisible { get; }
		public abstract StatementCompletion.CompletionType CompletionType { get; }
		public abstract string CompletionDescription { get; }
		public abstract Classifier.ProbeClassifierType ClassifierType { get; }
		public abstract string QuickInfoText { get; }

		public Definition(string name, Token sourceToken, bool global)
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
			xml.WriteAttributeString("name", _name);
			xml.WriteAttributeString("global", _global.ToString());

			string fileName;
			Span span;
			bool primaryFile;
			GetLocalFileSpan(out fileName, out span, out primaryFile);
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				xml.WriteAttributeString("localFileName", fileName);
				xml.WriteAttributeString("localOffset", span.Start.Offset.ToString());
			}
			xml.WriteAttributeString("primaryFile", primaryFile.ToString());

			xml.WriteEndElement();
		}

		/// <summary>
		/// Gets the location of the definition, in the file where it originated (not the preprocessed content).
		/// </summary>
		/// <param name="fileName">(out) file that contains the definition.</param>
		/// <param name="span">(out) Span of the definition, transformed to be in the originating file.</param>
		public void GetLocalFileSpan(out string fileName, out Span span, out bool primaryFile)
		{
			if (_sourceFile == null)
			{
				fileName = null;
				span = Span.Empty;
				primaryFile = false;
				return;
			}

			if (!_gotLocalFileInfo)
			{
				_sourceFile.CodeSource.GetFileSpan(_sourceSpan, out _localFileName, out _localFileSpan, out _localPrimaryFile);
				_gotLocalFileInfo = true;
			}
			fileName = _localFileName;
			span = _localFileSpan;
			primaryFile = _localPrimaryFile;
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
