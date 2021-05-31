using DK.AppEnvironment;
using DK.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DkTools.Run
{
	public class RunItem : INotifyPropertyChanged
	{
		public string Title { get; set; }
		public string FilePath { get; set; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public bool IsSystemDefined { get; set; }

		private bool _optionsVisible;

		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		public string RunButtonDisplayText => $"Run {Title}";

		public JToken ToJson()
		{
			if (IsSystemDefined)
			{
				return new JObject
				{
					{ "sys", Title }
				};
			}
			else
			{
				return new JObject
				{
					{ "title", Title },
					{ "file", FilePath },
					{ "args", Arguments },
					{ "workingDir", WorkingDirectory }
				};
			}
		}

		public static RunItem FromJson(JToken json, DkAppSettings appSettings)
		{
			if (json.Type != JTokenType.Object) return null;

			var sys = json["sys"]?.ToString();
			if (!string.IsNullOrEmpty(sys))
			{
				return RunItemCatalogue.GetSystemDefinedRunItem(sys, appSettings);
			}

			var title = json["title"]?.ToString();
			var filePath = json["file"]?.ToString();
			var args = json["args"]?.ToString();
			var workingDir = json["workingDir"]?.ToString();

			if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(filePath))
			{
				return new RunItem
				{
					Title = title,
					FilePath = filePath,
					Arguments = args,
					WorkingDirectory = workingDir
				};
			}

			return null;
		}

		public void OnOptionsButtonClicked()
		{
			_optionsVisible = !_optionsVisible;
			FirePropertyChanged(nameof(OptionsPaneVisibility));
		}

		public Visibility OptionsPaneVisibility => _optionsVisible ? Visibility.Visible : Visibility.Collapsed;
	}
}
