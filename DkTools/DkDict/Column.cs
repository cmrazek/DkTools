using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.DkDict
{
	class Column
	{
		public string Name { get; private set; }
		public DataType DataType { get; set; }
		public string Accel { get; set; }
		public bool NoAudit { get; set; }
		public PersistMode Persist { get; set; }
		public string Image { get; set; }
		public bool Tool { get; set; }
		public string Prompt { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string Group { get; set; }
		public bool EndGroup { get; set; }
		public string CustomProgId { get; set; }
		public string CustomLicense { get; set; }

		private List<Tag> _tags;

		public enum PersistMode
		{
			Form,
			FormOnly,
			Zoom,
			ZoomNoPersist
		}

		public Column(string name, DataType dataType)
		{
			Name = name;
			DataType = dataType;
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}
	}
}
