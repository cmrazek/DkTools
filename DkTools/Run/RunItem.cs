using DK.AppEnvironment;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace DkTools.Run
{
	public class RunItem : INotifyPropertyChanged
	{
		private string _title;
		private bool _optionsVisible;
		private bool _systemDefined;

		private string _filePath;
		private string _args;
		private string _workingDir;
		private int _samPort = DefaultPort;
		private bool _samLazyLoadDlls = DefaultLazyLoadDlls;
		private int _transReportTimeout = DefaultTransReportTimeout;
		private int _transAbortTimeout = DefaultTransAbortTimeout;
		private int _minRMs = DefaultMinResourceChannels;
		private int _maxRMs = DefaultMaxResourceChannels;
		private int _diagLevel = DefaultDiagLevel;
		private bool _devMode = false;
		private bool _designMode = false;

		public const int DefaultPort = 5001;
		public const int MinPort = 1;
		public const int MaxPort = 65535;
		public const bool DefaultLazyLoadDlls = true;
		public const int DefaultTransReportTimeout = 10;
		public const int DefaultTransAbortTimeout = 20;
		public const int MinTimeout = 1;
		public const int MaxTimeout = 99;
		public const int DefaultMinResourceChannels = 1;
		public const int DefaultMaxResourceChannels = 2;
		public const int MinNumResourceChannels = 1;
		public const int MaxNumResourceChannels = 200;
		public const int DefaultDiagLevel = 0;
		public const int MinDiagLevel = 0;
		public const int MaxDiagLevel = 3;

		public static RunItem CreateSam()
		{
			return new RunItem
			{
				_title = RunItemCatalogue.SystemRunItem_SAM,
				_systemDefined = true
			};
		}

		public static RunItem CreateCam()
		{
			return new RunItem
			{
				_title = RunItemCatalogue.SystemRunItem_CAM,
				_systemDefined = true
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		public string RunButtonDisplayText => $"Run {_title}";

		public JToken ToJson()
		{
			if (_systemDefined)
			{
				return new JObject
				{
					{ "sys", _title }
				};
			}
			else
			{
				return new JObject
				{
					{ "title", _title },
					{ "file", _filePath },
					{ "args", _args },
					{ "workingDir", _workingDir }
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
					_title = title,
					_filePath = filePath,
					_args = args,
					_workingDir = workingDir
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
		public Visibility CustomOptionsVisibility => _systemDefined == false ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SamOptionsVisibility => _systemDefined && _title == RunItemCatalogue.SystemRunItem_SAM ? Visibility.Visible : Visibility.Collapsed;
		public Visibility CamOptionsVisibility => _systemDefined && _title == RunItemCatalogue.SystemRunItem_CAM ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SamOrCamOptionsVisibility => _systemDefined && (_title == RunItemCatalogue.SystemRunItem_CAM || _title == RunItemCatalogue.SystemRunItem_CAM) ? Visibility.Visible : Visibility.Collapsed;

		public string Title
		{
			get => _title;
			set
			{
				if (_systemDefined) return;
				if (string.IsNullOrWhiteSpace(value)) return;
				if (_title != value)
				{
					_title = value;
					FirePropertyChanged(nameof(Title));
				}
			}
		}

		public string FilePath
		{
			get => _filePath;
			set
			{
				if (_filePath != value)
				{
					_filePath = value;
					FirePropertyChanged(nameof(FilePath));
				}
			}
		}

		public string Arguments
		{
			get => _args;
			set
			{
				if (_args != value)
				{
					_args = value;
					FirePropertyChanged(nameof(Arguments));
				}
			}
		}

		public string WorkingDirectory
		{
			get => _workingDir;
			set
			{
				if (_workingDir != value)
				{
					_workingDir = value;
					FirePropertyChanged(nameof(WorkingDirectory));
				}
			}
		}

		public string SamPortText
		{
			get => _samPort.ToString();
			set
			{
				if (int.TryParse(value, out var port) && port >= MinPort && port <= MaxPort)
				{
					_samPort = port;
					FirePropertyChanged(nameof(SamPortText));
				}
			}
		}

		public string TransReportTimeoutText
		{
			get => _transReportTimeout.ToString();
			set
			{
				if (int.TryParse(value, out var timeout) && timeout >= MinTimeout && timeout <= MaxTimeout)
				{
					_transReportTimeout = timeout;
					FirePropertyChanged(nameof(TransReportTimeoutText));
				}
			}
		}

		public string TransAbortTimeoutText
		{
			get => _transAbortTimeout.ToString();
			set
			{
				if (int.TryParse(value, out var timeout) && timeout >= MinTimeout && timeout <= MaxTimeout)
				{
					_transAbortTimeout = timeout;
					FirePropertyChanged(nameof(TransAbortTimeoutText));
				}
			}
		}

		public string MinResourceChannelsText
		{
			get => _minRMs.ToString();
			set
			{
				if (int.TryParse(value, out var numRMs) && numRMs >= MinNumResourceChannels && numRMs <= MaxNumResourceChannels)
				{
					_minRMs = numRMs;
					FirePropertyChanged(nameof(MinResourceChannelsText));
				}
			}
		}

		public string MaxResourceChannelsText
		{
			get => _maxRMs.ToString();
			set
			{
				if (int.TryParse(value, out var numRMs) && numRMs >= MinNumResourceChannels && numRMs <= MaxNumResourceChannels)
				{
					_maxRMs = numRMs;
					FirePropertyChanged(nameof(MaxResourceChannelsText));
				}
			}
		}

		public bool LazyLoadDlls
		{
			get => _samLazyLoadDlls;
			set
			{
				if (_samLazyLoadDlls != value)
				{
					_samLazyLoadDlls = value;
					FirePropertyChanged(nameof(LazyLoadDlls));
				}
			}
		}

		public int DiagLevel
		{
			get => _diagLevel;
			set
			{
				if (value >= MinDiagLevel && value <= MaxDiagLevel)
				{
					if (_diagLevel != value)
					{
						_diagLevel = value;
						FirePropertyChanged(nameof(DiagLevel));

						if (_systemDefined && _title == RunItemCatalogue.SystemRunItem_CAM && _diagLevel > 0 && _devMode == false)
						{
							_devMode = true;
							FirePropertyChanged(nameof(DevMode));
						}
					}
				}
			}
		}

		public bool DevMode
		{
			get => _devMode;
			set
			{
				if (_devMode != value)
				{
					_devMode = value;
					FirePropertyChanged(nameof(DevMode));
				}
			}
		}

		public bool DesignMode
		{
			get => _designMode;
			set
			{
				if (_designMode != value)
				{
					_designMode = value;
					FirePropertyChanged(nameof(DesignMode));
				}
			}
		}

		public void Run(DkAppSettings appSettings)
		{
			if (_systemDefined)
			{
				if (_title == RunItemCatalogue.SystemRunItem_SAM)
				{
					using (var proc = new Process())
					{
						var filePath = RunItemCatalogue.GetSamFilePath(appSettings);
						var args = GenerateSamCommandLineArgs(appSettings);

						var psi = new ProcessStartInfo(filePath, args);
						psi.UseShellExecute = false;
						psi.RedirectStandardOutput = false;
						psi.RedirectStandardError = false;
						psi.CreateNoWindow = false;
						psi.WorkingDirectory = RunItemCatalogue.GetSamWorkingDir(appSettings);

						proc.StartInfo = psi;
						if (!proc.Start()) throw new RunItemException("Unable to start the SAM.");
					}
				}
				else if (_title == RunItemCatalogue.SystemRunItem_CAM)
				{

					using (var proc = new Process())
					{
						var filePath = RunItemCatalogue.GetCamFilePath(appSettings);
						var args = GenerateCamCommandLineArgs(appSettings);

						var psi = new ProcessStartInfo(filePath, args);
						psi.UseShellExecute = false;
						psi.RedirectStandardOutput = false;
						psi.RedirectStandardError = false;
						psi.CreateNoWindow = false;
						psi.WorkingDirectory = RunItemCatalogue.GetCamWorkingDir(appSettings);

						proc.StartInfo = psi;
						if (!proc.Start()) throw new RunItemException("Unable to start the CAM.");
					}
				}
				else throw new RunItemException($"Unknown system defined run item '{_title}'.");
			}
			else
			{
				if (string.IsNullOrWhiteSpace(_filePath)) throw new RunItemException("No file path configured.");

				using (var proc = new Process())
				{
					var psi = new ProcessStartInfo(_filePath, _args ?? string.Empty);
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = false;
					psi.RedirectStandardError = false;
					psi.CreateNoWindow = false;
					psi.WorkingDirectory = string.IsNullOrWhiteSpace(_workingDir) ? null : _workingDir;

					proc.StartInfo = psi;
					if (!proc.Start()) throw new RunItemException($"Unable to start {_title}.");
				}
			}
		}

		private string GenerateSamCommandLineArgs(DkAppSettings appSettings)
		{
			var sb = new StringBuilder();

			var samName = CleanSamName(string.Concat(appSettings.AppName, "_", System.Environment.UserName));
			sb.Append($"/N{samName}");
			sb.Append($" /p{_samPort}");
			sb.Append($" /o{(LazyLoadDlls ? 0 : 1)}");
			sb.Append($" /y{_transReportTimeout:00}{_transAbortTimeout:00}");
			sb.Append($" /z{_minRMs}");
			sb.Append($" /Z{_maxRMs}");
			sb.Append($" /P \"{appSettings.AppName}\"");
			if (DiagLevel > 0) sb.Append($" /d{DiagLevel}");

			if (!string.IsNullOrWhiteSpace(_args)) sb.Append($" {_args}");

			return sb.ToString();
		}

		private string GenerateCamCommandLineArgs(DkAppSettings appSettings)
		{
			var sb = new StringBuilder();
			sb.Append("appname=" + appSettings.AppName);
			sb.Append(" networkname=" + CleanSamName(System.Environment.UserName + "_" + System.Environment.MachineName));

			if (_diagLevel > 0) sb.AppendFormat(" devmode={0}", this.DiagLevel);
			else if (_devMode) sb.Append(" devmode");

			if (_designMode) sb.Append(" designmode=true");

			if (!string.IsNullOrWhiteSpace(_args))
			{
				sb.Append(" ");
				sb.Append(_args);
			}

			return sb.ToString();
		}

		private string CleanSamName(string name)
		{
			StringBuilder sb = new StringBuilder(name.Length);
			foreach (char ch in name)
			{
				if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
			}
			return sb.ToString();
		}
	}

	class RunItemException : Exception
	{
		public RunItemException(string message) : base(message) { }
	}
}
