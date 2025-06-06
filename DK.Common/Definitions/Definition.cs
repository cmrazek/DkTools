using DK.AppEnvironment;
using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Definitions
{
    public abstract class Definition
    {
        private string _name;
        private FilePosition _filePos;
        private string _externalRefId;

        public abstract bool CompletionVisible { get; }
        public abstract ProbeCompletionType CompletionType { get; }
        public abstract ProbeClassifierType ClassifierType { get; }
        public abstract string QuickInfoTextStr { get; }
        public abstract QuickInfoLayout QuickInfo { get; }
        public abstract string PickText { get; }
        public abstract ServerContext ServerContext { get; }

        private const int k_maxWpfWidth = 600;

        public static readonly Definition[] EmptyArray = new Definition[0];

        public Definition(string name, FilePosition filePos, string externalRefId)
        {
#if DEBUG
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
#endif
            _name = name;
            _filePos = filePos;
            _externalRefId = externalRefId;
        }

        /// <summary>
        /// Gets the name of the definition.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the file name where this definition was defined.
        /// </summary>
        public string SourceFileName => _filePos.FileName;

        /// <summary>
        /// Gets the position in the source file name where this definition was defined.
        /// </summary>
        public int SourceStartPos => _filePos.Position;

        public string LocationText
        {
            get
            {
                if (!string.IsNullOrEmpty(_filePos.FileName)) return _filePos.FileName;
                return "(unknown)";
            }
        }

        public FilePosition FilePosition => _filePos;

        public void DumpTree(System.Xml.XmlWriter xml)
        {
            xml.WriteStartElement(GetType().Name);
            DumpTreeAttribs(xml);
            DumpTreeInner(xml);
            xml.WriteEndElement();
        }

        public virtual void DumpTreeAttribs(System.Xml.XmlWriter xml)
        {
            xml.WriteAttributeString("name", _name);
            xml.WriteAttributeString("sourceFileName", _filePos.FileName);
            xml.WriteAttributeString("sourceStartPos", _filePos.Position.ToString());
        }

        public virtual void DumpTreeInner(System.Xml.XmlWriter xml) { }

#if DEBUG
        public string Dump()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Type [{0}]", GetType());
            sb.AppendFormat(" Name [{0}]", Name);
            sb.AppendFormat(" File [{0}]", _filePos.IsEmpty ? "(null)" : _filePos.FileName);
            sb.AppendFormat(" Offset [{0}]", _filePos.Position);
            sb.AppendFormat(" CompletionType [{0}]", CompletionType);
            sb.AppendFormat(" QuickInfoText [{0}]", QuickInfoTextStr.ToSingleLine());
            return sb.ToString();
        }
#endif

        public string ExternalRefId => _externalRefId;

        public override bool Equals(object obj)
        {
            var def = obj as Definition;
            if (def == null) return false;

            return _name == def._name && _filePos == def._filePos;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode() ^ _filePos.GetHashCode();
        }

        public virtual bool AllowsFunctionBody => false;
        public virtual bool ArgumentsRequired => false;
        public virtual IEnumerable<ArgumentDescriptor> Arguments => ArgumentDescriptor.EmptyArray;
        public virtual FunctionSignature ArgumentsSignature => null;
        public virtual bool HasVariableArgumentCount => false;
        public virtual DataType DataType => null;
        public virtual bool AllowsChild => false;
        public virtual bool RequiresChild => false;
        public virtual bool AllowsDollarChild => false;
        public virtual bool AllowsDoubleDollarChild => false;

        public virtual bool RequiresParent(string curClassName) => false;
        public virtual IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings) => Definition.EmptyArray;
        public virtual IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => Definition.EmptyArray;
        public virtual IEnumerable<Definition> GetDollarChildDefinitions(string name, DkAppSettings appSettings) => Definition.EmptyArray;
        public virtual IEnumerable<Definition> GetDollarChildDefinitions(DkAppSettings appSettings) => Definition.EmptyArray;
        public virtual IEnumerable<Definition> GetDoubleDollarChildDefinitions(string name, DkAppSettings appSettings) => Definition.EmptyArray;
        public virtual IEnumerable<Definition> GetDoubleDollarChildDefinitions(DkAppSettings appSettings) => Definition.EmptyArray;

        /// <summary>
        /// If true, this definition may only be detected as a single word when the reference data type calls for this type of object.
        /// </summary>
        public virtual bool RequiresRefDataType => false;

        public virtual FunctionSignature Signature => null;
        public virtual bool CanRead => false;
        public virtual bool CanWrite => false;

        /*
         * Selection Orders:
         * Column	10
         * Global	20
         * Argument	30
         * Local	40
         */
        public virtual int SelectionOrder => 0;

        public virtual bool CaseSensitive => true;

        public virtual bool NotGlobal => false;
    }
}
