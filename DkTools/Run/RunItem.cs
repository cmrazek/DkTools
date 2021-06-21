using DK.AppEnvironment;
using DK.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		private bool _setDbDate = DefaultSetDbDate;

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
		public const bool DefaultSetDbDate = true;

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

		public bool SetDbDate
		{
			get => _setDbDate;
			set
			{
				if (_setDbDate != value)
				{
					_setDbDate = value;
					FirePropertyChanged(nameof(SetDbDate));
				}
			}
		}

		public void Run(DkAppSettings appSettings)
		{
			if (_systemDefined)
			{
				if (_title == RunItemCatalogue.SystemRunItem_SAM)
				{
					if (_setDbDate) RunSetDbDate(appSettings);
					RunSam(appSettings);
				}
				else if (_title == RunItemCatalogue.SystemRunItem_CAM)
				{
					RunCam(appSettings);
				}
				else throw new RunItemException($"Unknown system defined run item '{_title}'.");
			}
			else
			{
				RunProcess(_title, _filePath, _args, _workingDir);
			}
		}

		private void RunSetDbDate(DkAppSettings appSettings)
		{
			using (var proc = new Process())
			{
				var filePath = Path.Combine(appSettings.PlatformPath, "setdbdat.exe");
				if (!File.Exists(filePath)) throw new RunItemException("setdbdat.exe not found.");

				RunProcess("setdbdat.exe", filePath, "today force", appSettings.PlatformPath, waitForExit: -1);
			}
		}

		private void RunSam(DkAppSettings appSettings)
		{
			using (var proc = new Process())
			{
				var filePath = Path.Combine(appSettings.PlatformPath, "SAM.exe");
				if (!File.Exists(filePath)) throw new RunItemException("SAM.exe not found.");

				var args = new StringBuilder();

				var samName = CleanSamName(string.Concat(appSettings.AppName, "_", System.Environment.UserName));
				args.Append($"/N{samName}");
				args.Append($" /p{_samPort}");
				args.Append($" /o{(LazyLoadDlls ? 0 : 1)}");
				args.Append($" /y{_transReportTimeout:00}{_transAbortTimeout:00}");
				args.Append($" /z{_minRMs}");
				args.Append($" /Z{_maxRMs}");
				args.Append($" /P \"{appSettings.AppName}\"");
				if (DiagLevel > 0) args.Append($" /d{DiagLevel}");

				if (!string.IsNullOrWhiteSpace(_args)) args.Append($" {_args}");

				RunProcess(_title, filePath, args.ToString(), appSettings.ExeDirs.FirstOrDefault() ?? appSettings.PlatformPath);
			}
		}

		private void RunCam(DkAppSettings appSettings)
		{
			using (var proc = new Process())
			{
				var filePath = Path.GetFullPath(Path.Combine(appSettings.PlatformPath, "..\\CAMNet\\CAMNet.exe"));
				if (!File.Exists(filePath)) throw new RunItemException("CAMNet.exe not found.");

				var args = new StringBuilder();
				args.Append("appname=" + appSettings.AppName);
				args.Append(" networkname=" + CleanSamName(System.Environment.UserName + "_" + System.Environment.MachineName));

				if (_diagLevel > 0) args.AppendFormat(" devmode={0}", this.DiagLevel);
				else if (_devMode) args.Append(" devmode");

				if (_designMode) args.Append(" designmode=true");

				if (!string.IsNullOrWhiteSpace(_args))
				{
					args.Append(" ");
					args.Append(_args);
				}

				RunProcess(_title, filePath, args.ToString(), Path.GetDirectoryName(filePath));
			}
		}

		private string CleanSamName(string name)
		{
			var sb = new StringBuilder(name.Length);
			foreach (var ch in name)
			{
				if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
			}
			return sb.ToString();
		}

		private void RunProcess(string title, string filePath, string args, string workingDir, int waitForExit = 0)
		{
			Log.Info("Running: {0}\r\nFile Path: {1}\r\nArguments: {2}\r\nWorking Dir: {3}", title, filePath, args, workingDir);

			if (string.IsNullOrWhiteSpace(filePath)) throw new RunItemException($"No file path is configured.");

			if (string.IsNullOrWhiteSpace(workingDir) && !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
			{
				workingDir = Path.GetDirectoryName(filePath);
				Log.Info("Derived working dir: {0}", workingDir);
			}

			using (var proc = new Process())
			{
				var psi = new ProcessStartInfo(filePath, args ?? string.Empty);
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardError = false;
				psi.CreateNoWindow = false;
				psi.WorkingDirectory = workingDir;

				proc.StartInfo = psi;
				if (!proc.Start()) throw new RunItemException($"Unable to start {title}.");

				if (waitForExit != 0)
				{
					if (waitForExit < 0) proc.WaitForExit();
					else proc.WaitForExit(waitForExit);
					if (proc.ExitCode != 0) throw new RunItemException($"{title} returned exit code {proc.ExitCode}");
				}
			}
		}
	}

	class RunItemException : Exception
	{
		public RunItemException(string message) : base(message) { }
	}
}
