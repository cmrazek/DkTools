using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		public Interface(string name)
		{
			_name = name;
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
					_def = new InterfaceTypeDefinition(_name);
				}
				return _def;
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public IEnumerable<Definition> MethodDefinitions
		{
			get
			{
				// TODO: Add support for method definitions
				return new Definition[0];
			}
		}

		public IEnumerable<Definition> PropertyDefinitions
		{
			get
			{
				// TODO: Add support for property definitions
				return new Definition[0];
			}
		}

		public Definition GetMethod(string name)
		{
			// TODO: Add support for methods
			return null;
		}

		public Definition GetProperty(string name)
		{
			// TODO: Add support for properties
			return null;
		}
	}
}
