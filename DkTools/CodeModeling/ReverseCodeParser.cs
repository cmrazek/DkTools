using DK.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DkTools.CodeModeling
{
    internal class ReverseCodeParser
    {
        private LiveCodeTracker _tracker;
        private int _startPos;
        private List<CodeItem> _items;

        public ReverseCodeParser(LiveCodeTracker tracker, int startPosition)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));

            if (startPosition < 0 || startPosition > _tracker.TextBuffer.CurrentSnapshot.Length) throw new ArgumentOutOfRangeException(nameof(startPosition));
            _startPos = startPosition;
        }

        public CodeItem? GetPreviousItem()
        {
            if (_items == null || _items.Count == 0)
            {
                var results = _tracker.GetCodeItemsLeadingUpToPosition(_startPos);
                _items = results.Items.Where(i => i.Span.Start < _startPos).ToList();
                _startPos = results.StartParsingPosition;
            }

            if (_items.Count == 0) return null;

            var item = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            return item;
        }

        public CodeItem? GetPreviousItemNestable(params string[] stopItems)
        {
            var item = GetPreviousItem();
            if (item == null) return null;

            if (stopItems?.Contains(item.Value.Text) == true) return null;

            if (item.Value.Type == CodeType.Operator)
            {
                switch (item.Value.Text)
                {
                    case "}":
                        return GetPreviousItemNestable_Inner(item.Value, "{") ?? item;
                    case ")":
                        return GetPreviousItemNestable_Inner(item.Value, "(") ?? item;
                    case "]":
                        return GetPreviousItemNestable_Inner(item.Value, "(") ?? item;
                }
            }

            return item;
        }

        private CodeItem? GetPreviousItemNestable_Inner(CodeItem firstItem, string endText)
        {
			while (true)
			{
                var item = GetPreviousItem();
                if (item == null) return null;

				if (item.Value.Type == CodeType.Operator)
				{
					if (item.Value.Text == endText)
                    {
                        return new CodeItem(CodeType.Operator, new CodeSpan(item.Value.Span.Start, firstItem.Span.End), string.Concat(endText, firstItem.Text));
                    }

					switch (item.Value.Text)
					{
						case ")":
							if (GetPreviousItemNestable_Inner(item.Value, "(") == null) return null;
							break;
						case "}":
							if (GetPreviousItemNestable_Inner(item.Value, "{") == null) return null;
							break;
						case "]":
							if (GetPreviousItemNestable_Inner(item.Value, "[") == null) return null;
							break;
					}
				}
			}
        }

        public IEnumerable<CodeItem> GetPreviousItems()
        {
            CodeItem? item;
            while ((item = GetPreviousItem()).HasValue) yield return item.Value;
        }

        public IEnumerable<CodeItem> GetPreviousItemsNestable(params string[] stopItems)
        {
            CodeItem? item;
            while ((item = GetPreviousItemNestable(stopItems)).HasValue) yield return item.Value;
        }
    }
}
