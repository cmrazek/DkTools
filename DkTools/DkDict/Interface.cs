using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.DkDict
{
	class Interface
	{
		public string Name { get; private set; }
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

		private List<Tag> _tags;

		public Interface(string name)
		{
			Name = name;
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}
	}
}
