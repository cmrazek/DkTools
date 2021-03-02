using EnvDTE;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools.PeekDefinition
{
	public class DkPeekResultSource : IPeekResultSource
	{
		private IPeekResultFactory _peekResultFactory;
		private DkPeekableItem _item;

		internal DkPeekResultSource(IPeekResultFactory peekResultFactory, DkPeekableItem item)
		{
			_peekResultFactory = peekResultFactory ?? throw new ArgumentNullException(nameof(peekResultFactory));
			_item = item ?? throw new ArgumentNullException(nameof(item));
		}

		public void FindResults(string relationshipName, IPeekResultCollection resultCollection,
			CancellationToken cancellationToken, IFindPeekResultsCallback callback)
		{
			var def = _item.Definition;
			var filePath = def.FilePosition.FileName;
			var fileName = System.IO.Path.GetFileName(filePath);

			int startLine = 0;
			int startLinePos = 0;
			if (System.IO.File.Exists(filePath))
			{
				var pos = def.FilePosition.Position;
				var fileContent = File.ReadAllText(filePath);
				Util.CalcLineAndPosFromOffset(fileContent, pos, out startLine, out startLinePos);
			}

			var displayInfo = new PeekResultDisplayInfo2(
				label: $"{fileName} - ({startLine + 1}, {startLinePos + 1})",
				labelTooltip: filePath,
				title: fileName,
				titleTooltip: filePath,
				startIndexOfTokenInLabel: 0,
				lengthOfTokenInLabel: 0);

			var result = _peekResultFactory.Create(
				displayInfo: displayInfo,
				image: default(ImageMoniker),
				filePath: def.FilePosition.FileName,
				startLine: startLine,
				startIndex: startLinePos,
				endLine: startLine,
				endIndex: startLinePos,
				idStartLine: startLine,
				idStartIndex: startLinePos,
				idEndLine: startLine,
				idEndIndex: startLinePos,
				isReadOnly: false);

			resultCollection.Add(result);
		}
	}
}
