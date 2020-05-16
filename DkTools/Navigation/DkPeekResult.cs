using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Navigation
{
	public class DkPeekResult : IPeekResult
	{
		private Definition _def;

		internal DkPeekResult(Definition definition)
		{
			_def = definition ?? throw new ArgumentNullException(nameof(definition));
		}

		//
		// Summary:
		//     Gets properties used for displaying this result to the user.
		//
		// Returns:
		//     Properties used for displaying this result to the user.
		public IPeekResultDisplayInfo DisplayInfo { get; }

		public bool CanNavigateTo => !_def.FilePosition.IsEmpty;

		public Action<IPeekResult, object, object> PostNavigationCallback => (result, a, dataFromNavigateTo) => { };

		public void Dispose()
		{
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler Disposed;

		public void NavigateTo(object data)
		{
			Log.Debug("DkPeekResult.NavigateTo: {0}", data);	// TODO
		}
	}
}
