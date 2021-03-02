﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.Classifier;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class Interface
	{
		public string Path { get; set; }
		public bool Framework { get; set; }
		public string ProgId { get; set; }
		public string ClsId { get; set; }
		public string TLibId { get; set; }
		public string Iid { get; set; }
		public string Description { get; set; }
		public string InterfaceName { get; set; }
		public bool Default { get; set; }
		public bool DefaultEvent { get; set; }

		private string _name;
		private List<Tag> _tags;
		private InterfaceTypeDefinition _def;
		private FilePosition _filePos;
		private DataType _dataType;

		public Interface(string name, FilePosition filePos)
		{
			_name = name;
			_filePos = filePos;
			_def = new InterfaceTypeDefinition(this, filePos);
			_dataType = new DataType(ValType.Interface, "",
				new ProbeClassifiedString(ProbeClassifierType.Interface, _name),
				CodeModel.Definitions.Definition.EmptyArray, CodeModel.DataType.CompletionOptionsType.InterfaceMembers)
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
	}
}
