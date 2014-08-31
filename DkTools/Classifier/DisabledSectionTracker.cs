using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.Classifier
{
	internal class DisabledSectionTracker
	{
		private Span[] _sections;
		private bool _disabled;
		private int _sectionIndex;

		public DisabledSectionTracker(Span[] disabledSections)
		{
			_sections = disabledSections;
		}

		public bool SetOffset(int offset)
		{
			_sectionIndex = 0;
			return Advance(offset);
		}

		public bool Advance(int offset)
		{
			_disabled = false;
			while (_sectionIndex < _sections.Length)
			{
				var sec = _sections[_sectionIndex];
				if (sec.End.Offset <= offset)
				{
					_sectionIndex++;
				}
				else if (sec.Start.Offset > offset)
				{
					_disabled = false;
					break;
				}
				else
				{
					_disabled = true;
					break;
				}
			}

			return _disabled;
		}
	}
}
