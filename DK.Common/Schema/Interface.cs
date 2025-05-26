using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Collections.Generic;

namespace DK.Schema
{
	public class Interface
	{
		public string Path { get; set; }
		public bool Framework { get; set; }
		public string ProgId { get; set; }
		public string ClsId { get; set; }
		public string TLibId { get; set; }
		public string Iid { get; set; }
		public string Description { get; set; }
		public string PlatformName { get; set; }
		public bool Default { get; set; }
		public bool DefaultEvent { get; set; }

		private string _name;
		private List<Tag> _tags;
		private InterfaceTypeDefinition _def;
		private FilePosition _filePos;
		private DataType _dataType;
		private List<InterfaceMethodDefinition> _methods;
		private List<InterfacePropertyDefinition> _properties;

		public Interface(string name, FilePosition filePos)
		{
			_name = name;
			_filePos = filePos;
			_def = new InterfaceTypeDefinition(this, filePos);
			_dataType = new DataType(ValType.Interface, "",
				new ProbeClassifiedString(ProbeClassifierType.Interface, _name),
				DK.Definitions.Definition.EmptyArray, DataType.CompletionOptionsType.InterfaceMembers)
			{
				Interface = this
			};
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}

		public InterfaceTypeDefinition Definition
		{
			get
			{
				if (_def == null)
				{
					_def = new InterfaceTypeDefinition(_name, _filePos);
				}
				return _def;
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public DataType MakeArrayDataType()
		{
			return new DataType(_dataType)
			{
				InterfaceArray = true,
				Source = _dataType.Source.Append(new ProbeClassifiedRun(ProbeClassifierType.Operator, "[]"))
			};
		}

		public DataType MakePointerDataType()
		{
			return new DataType(_dataType)
			{
				InterfacePointer = true,
				Source = _dataType.Source.Append(new ProbeClassifiedRun(ProbeClassifierType.Operator, "*"))
			};
		}

        public void AddMethod(InterfaceMethodDefinition method)
		{
			if (_methods == null) _methods = new List<InterfaceMethodDefinition>();
			_methods.Add(method ?? throw new ArgumentNullException(nameof(method)));
		}

        public void AddProperty(InterfacePropertyDefinition property)
        {
            if (_properties == null) _properties = new List<InterfacePropertyDefinition>();
            _properties.Add(property ?? throw new ArgumentNullException(nameof(property)));
        }

		public IEnumerable<InterfaceMethodDefinition> Methods => _methods;

		public IEnumerable<InterfacePropertyDefinition> Properties => _properties;
    }
}
