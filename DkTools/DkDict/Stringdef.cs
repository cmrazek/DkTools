using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class Stringdef
	{
		public string Name { get; set; }
		public string Text { get; set; }
		public string Description { get; set; }

		private List<Tag> _tags;
		private StringDefDefinition _def;

		public Stringdef(string name, string text, string desc, FilePosition filePos)
		{
			Name = name;
			Text = text;
			Description = desc;

			_def = new StringDefDefinition(this, filePos);
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}

		public StringDefDefinition Definition
		{
			get
			{
				return _def;
			}
		}
	}
}
