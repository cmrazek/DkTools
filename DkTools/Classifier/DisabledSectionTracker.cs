using DK.Code;

namespace DkTools.Classifier
{
	internal class DisabledSectionTracker
	{
		private CodeSpan[] _sections;
		private bool _disabled;
		private int _sectionIndex;

		public DisabledSectionTracker(CodeSpan[] disabledSections)
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
				if (sec.End <= offset)
				{
					_sectionIndex++;
				}
				else if (sec.Start > offset)
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
