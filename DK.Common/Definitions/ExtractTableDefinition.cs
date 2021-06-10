using DK.AppEnvironment;
using DK.Code;
using DK.Syntax;
using System.Collections.Generic;

namespace DK.Definitions
{
	public class ExtractTableDefinition : Definition
	{
		private bool _permanent;
		private List<ExtractFieldDefinition> _fields = new List<ExtractFieldDefinition>();

		public ExtractTableDefinition(string name, FilePosition filePos, bool permanent)
			: base(name, filePos, permanent ? string.Concat("permx:", name) : null)
		{
			_permanent = permanent;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Table; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_permanent) return string.Concat("extract permanent ", Name);
				else return string.Concat("extract ", Name);
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoText(ProbeClassifierType.TableName, Name),
			new QuickInfoDescription(_permanent ? "Permanent extract" : "Temporary extract")
		);

		public void AddField(ExtractFieldDefinition field)
		{
			_fields.Add(field);
			field.ExtractDefinition = this;
		}

		public ExtractFieldDefinition GetField(string fieldName)
		{
			foreach (var field in _fields)
			{
				if (field.Name == fieldName) return field;
			}
			return null;
		}

		public IEnumerable<ExtractFieldDefinition> Fields
		{
			get { return _fields; }
		}

		public bool Permanent
		{
			get { return _permanent; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings)
		{
			foreach (var field in _fields)
			{
				if (field.Name == name) yield return field;
			}
		}

		public override IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => _fields;

		public override bool ArgumentsRequired
		{
			get { return false; }
		}
	}
}
