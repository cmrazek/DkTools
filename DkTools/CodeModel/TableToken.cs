﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class TableToken : WordToken
	{
		public TableToken(GroupToken parent, Scope scope, Span span, string text, Definition def)
			: base(parent, scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.TableName;

			if (def != null) SourceDefinition = def;
			else
			{
				var table = ProbeEnvironment.GetTable(text);
				if (table != null) this.SourceDefinition = table.Definition;
			}
		}

		public static TableToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var word = file.PeekWord();
			if (!ProbeEnvironment.IsProbeTable(word)) return null;

			var table = ProbeEnvironment.GetTable(word);
			Definition def = table != null ? table.Definition : null;

			return new TableToken(parent, scope, file.MoveNextSpan(word.Length), word, def);
		}
	}
}
