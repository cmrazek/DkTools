using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.DkDict
{
	class Stringdef
	{
		public string Name { get; set; }
		public string Text { get; set; }
		public string Description { get; set; }

		private List<Tag> _tags;

		public Stringdef(string name, string text, string desc)
		{
			Name = name;
			Text = text;
			Description = desc;
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}
	}
}
