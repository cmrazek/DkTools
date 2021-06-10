using DK.Modeling;
using Microsoft.VisualStudio.Text;
using System;

namespace DkTools.CodeModeling
{
	static class CodeModelExtensions
	{
		/// <summary>
		/// Adjusts a position from another snapshot to the model's snapshot.
		/// </summary>
		public static int AdjustPosition(this CodeModel model, int pos, ITextSnapshot snapshot)
		{
			var modelSnapshot = model.Snapshot as ITextSnapshot;
			if (snapshot == null || modelSnapshot == null || modelSnapshot == snapshot)
			{
				return pos;
			}

			var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(snapshot, pos).TranslateTo(modelSnapshot, PointTrackingMode.Positive);
			return pt.Position;
		}

		public static int AdjustPosition(this CodeModel model, SnapshotPoint snapPt)
		{
			return model.AdjustPosition(snapPt.Position, snapPt.Snapshot);
		}

		public static int TranslateOffset(this CodeModel model, int offset, Microsoft.VisualStudio.Text.ITextSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");

			var modelSnapshot = model.Snapshot as ITextSnapshot;
			if (modelSnapshot == null) throw new InvalidOperationException("Model has no snapshot.");

			if (modelSnapshot != snapshot)
			{
				var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(snapshot, offset).TranslateTo(modelSnapshot, PointTrackingMode.Positive);
				return pt.Position;
			}
			else
			{
				return offset;
			}
		}
	}
}
