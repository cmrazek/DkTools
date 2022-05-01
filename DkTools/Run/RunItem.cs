using DK.AppEnvironment;
using DK.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace DkTools.Run
{
	public class RunItem : INotifyPropertyChanged
	{
		private RunItemType _type;
		private string _title;
		private bool _optionsVisible;

		private string _filePath;
		private string _args;
		private string _workingDir;
		private int _port = DefaultPort;
		private bool _samLazyLoadDlls = DefaultLazyLoadDlls;
		private int _transReportTimeout = DefaultTransReportTimeout;
		private int _transAbortTimeout = DefaultTransAbortTimeout;
		private int _minRMs = DefaultMinResourceChannels;
		private int _maxRMs = DefaultMaxResourceChannels;
		private int _diagLevel = DefaultDiagLevel;
		private bool _devMode = false;
		private bool _designMode = false;
		private bool _setDbDate = DefaultSetDbDate;
		private bool _canMoveUp;
		private bool _canMoveDown;
		private bool _selected;
		private bool _waitForExit = true;
		private bool _captureOutput = true;

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

		public event EventHandler Changed;

		public static RunItem CreateSam()
		{
			return new RunItem
			{
				_type = RunItemType.Sam,
				_title = RunItemCatalogue.SystemRunItem_SAM,
			};
		}

		public static RunItem CreateCam()
		{
			return new RunItem
			{
				_type = RunItemType.Cam,
				_title = RunItemCatalogue.SystemRunItem_CAM
			};
		}

		public static RunItem CreateOther(string title, bool optionsVisible)
		{
			return new RunItem
			{
				_type = RunItemType.Custom,
				_title = title,
				_optionsVisible = optionsVisible
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

			switch (propName)
			{
				case nameof(Title):
					if (_type == RunItemType.Sam || _type == RunItemType.Cam) FirePropertyChanged(nameof(GeneratedArguments));
					FirePropertyChanged(nameof(RunButtonDisplayText));
					break;

				case nameof(FilePath):
				case nameof(Arguments):
				case nameof(WorkingDirectory):
				case nameof(SamPortText):
				case nameof(TransReportTimeoutText):
				case nameof(TransAbortTimeoutText):
				case nameof(MinResourceChannelsText):
				case nameof(MaxResourceChannelsText):
				case nameof(LazyLoadDlls):
				case nameof(DiagLevel):
				case nameof(DevMode):
				case nameof(DesignMode):
				case nameof(SetDbDate):
					if (_type == RunItemType.Sam || _type == RunItemType.Cam) FirePropertyChanged(nameof(GeneratedArguments));
					break;

				case nameof(WaitForExit):
					FirePropertyChanged(nameof(CustomOptionsWaitForExitVisibility));
					break;
			}
		}

		public string RunButtonDisplayText => $"Run {_title}";

		public JToken ToJson()
		{
			switch (_type)
			{
				case RunItemType.Sam:
					return new JObject
					{
						{ "type", _type.ToString() },
						{ "title", _title },
						{ "port", _port },
						{ "lazyLoadDlls", _samLazyLoadDlls },
						{ "transReportTimeout", _transReportTimeout },
						{ "transAbortTimeout", _transAbortTimeout },
						{ "minRMs", _minRMs },
						{ "maxRMs", _maxRMs },
						{ "diagLevel", _diagLevel },
						{ "setDbDate", _setDbDate },
						{ "selected", _selected }
					};

				case RunItemType.Cam:
					return new JObject
					{
						{ "type", _type.ToString() },
						{ "title", _title },
						{ "diagLevel", _diagLevel },
						{ "devMode", _devMode },
						{ "designMode", _designMode },
						{ "selected", _selected }
					};

				case RunItemType.Custom:
					return new JObject
					{
						{ "type", _type.ToString() },
						{ "title", _title },
						{ "filePath", _filePath },
						{ "args", _args },
						{ "workingDir", _workingDir },
						{ "selected", _selected },
						{ "waitForExit", _waitForExit },
						{ "captureOutput", _captureOutput }
					};

				default:
					throw new InvalidRunItemTypeException();
			}
		}

		public static RunItem FromJson(JToken json, DkAppSettings appSettings)
		{
			if (json.Type != JTokenType.Object) return null;
			if (!Enum.TryParse<RunItemType>(json["type"]?.ToString(), out var type)) return null;

			switch (type)
			{
				case RunItemType.Sam:
					return new RunItem
					{
						_type = RunItemType.Sam,
						_title = json["title"]?.ToString(),
						_port = json["port"].ToInt(MinPort, MaxPort, DefaultPort),
						_samLazyLoadDlls = json["lazyLoadDlls"].ToBool(DefaultLazyLoadDlls),
						_transReportTimeout = json["transReportTimeout"].ToInt(MinTimeout, MaxTimeout, DefaultTransReportTimeout),
						_transAbortTimeout = json["transAbortTimeout"].ToInt(MinTimeout, MaxTimeout, DefaultTransAbortTimeout),
						_minRMs = json["minRMs"].ToInt(MinNumResourceChannels, MaxNumResourceChannels, DefaultMinResourceChannels),
						_maxRMs = json["maxRMs"].ToInt(MinNumResourceChannels, MaxNumResourceChannels, DefaultMaxResourceChannels),
						_diagLevel = json["diagLevel"].ToInt(MinDiagLevel, MaxDiagLevel, DefaultDiagLevel),
						_setDbDate = json["setDbDate"].ToBool(DefaultSetDbDate),
						_selected = json["selected"].ToBool(false)
					};

				case RunItemType.Cam:
					return new RunItem
					{
						_type = RunItemType.Cam,
						_title = json["title"]?.ToString(),
						_diagLevel = json["diagLevel"].ToInt(MinDiagLevel, MaxDiagLevel, DefaultDiagLevel),
						_devMode = json["devMode"].ToBool(false),
						_designMode = json["designMode"].ToBool(false),
						_selected = json["selected"].ToBool(false)
					};

				case RunItemType.Custom:
					return new RunItem
					{
						_type = RunItemType.Custom,
						_title = json["title"]?.ToString(),
						_filePath = json["filePath"]?.ToString(),
						_args = json["args"]?.ToString(),
						_workingDir = json["workingDir"]?.ToString(),
						_selected = json["selected"].ToBool(false),
						_waitForExit = json["waitForExit"].ToBool(true),
						_captureOutput = json["captureOutput"].ToBool(true)
					};

				default:
					return null;
			}
		}

		public void OnOptionsButtonClicked()
		{
			_optionsVisible = !_optionsVisible;
			FirePropertyChanged(nameof(OptionsPaneVisibility));
			FirePropertyChanged(nameof(OptionsPaneVisibilityNot));
		}

		public Visibility OptionsPaneVisibility => _optionsVisible ? Visibility.Visible : Visibility.Collapsed;
		public Visibility OptionsPaneVisibilityNot => !_optionsVisible ? Visibility.Visible : Visibility.Collapsed;
		public Visibility CustomOptionsVisibility => _type == RunItemType.Custom ? Visibility.Visible : Visibility.Collapsed;
		public Visibility CustomOptionsWaitForExitVisibility => _type == RunItemType.Custom && _waitForExit == true ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SamOptionsVisibility => _type == RunItemType.Sam ? Visibility.Visible : Visibility.Collapsed;
		public Visibility CamOptionsVisibility => _type == RunItemType.Cam ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SamOrCamOptionsVisibility => (_type == RunItemType.Sam || _type == RunItemType.Cam) ? Visibility.Visible : Visibility.Collapsed;

		public string Title
		{
			get => _title;
			set
			{
				if (string.IsNullOrWhiteSpace(value)) return;
				if (_title != value)
				{
					_title = value;
					FirePropertyChanged(nameof(Title));
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public string ArgumentsLabelText => _type == RunItemType.Custom ? "Arguments:" : "Extra Args:";

		public string WorkingDirectory
		{
			get => _workingDir;
			set
			{
				if (_workingDir != value)
				{
					_workingDir = value;
					FirePropertyChanged(nameof(WorkingDirectory));
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public string SamPortText
		{
			get => _port.ToString();
			set
			{
				if (int.TryParse(value, out var port) && port >= MinPort && port <= MaxPort)
				{
					_port = port;
					FirePropertyChanged(nameof(SamPortText));
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
						Changed?.Invoke(this, EventArgs.Empty);

						if (_type == RunItemType.Cam && _diagLevel > 0 && _devMode == false)
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
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
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public bool WaitForExit
		{
			get => _waitForExit;
			set
			{
				if (_waitForExit != value)
				{
					_waitForExit = value;
					FirePropertyChanged(nameof(WaitForExit));
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public bool CaptureOutput
		{
			get => _captureOutput;
			set
			{
				if (_captureOutput != value)
				{
					_captureOutput = value;
					FirePropertyChanged(nameof(_captureOutput));
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public string GeneratedArguments
		{
			get
			{
				switch (_type)
				{
					case RunItemType.Sam:
						return GenerateSamArguments(ProbeToolsPackage.Instance.App.Settings);
					case RunItemType.Cam:
						return GenerateCamArguments(ProbeToolsPackage.Instance.App.Settings);
					default:
						return _args;
				}
			}
		}

		internal void Run(DkAppSettings appSettings, OutputPane pane, CancellationToken cancel)
		{
			switch (_type)
			{
				case RunItemType.Sam:
					if (_setDbDate) RunSetDbDate(appSettings, pane, cancel);
					pane?.WriteLine(string.Empty);
					RunSam(appSettings, pane, cancel);
					break;

				case RunItemType.Cam:
					RunCam(appSettings, pane, cancel);
					break;

				case RunItemType.Custom:
					RunProcess(_title, _filePath, _args, _workingDir, pane, cancel, _waitForExit, _captureOutput);
					break;

				default:
					throw new InvalidRunItemTypeException();
			}
		}

		private void RunSetDbDate(DkAppSettings appSettings, OutputPane pane, CancellationToken cancel)
		{
			if (cancel.IsCancellationRequested) return;

			using (var proc = new Process())
			{
				pane?.WriteLine("Setting database date (setdbdat today force)");

				var filePath = Path.Combine(appSettings.PlatformPath, "setdbdat.exe");
				if (!File.Exists(filePath)) throw new RunItemException("setdbdat.exe not found.");

				RunProcess("setdbdat.exe", filePath, "today force", appSettings.PlatformPath, pane, cancel, waitForExit: true, capture: true);
			}
		}

		private void RunSam(DkAppSettings appSettings, OutputPane pane, CancellationToken cancel)
		{
			if (cancel.IsCancellationRequested) return;

			using (var proc = new Process())
			{
				var filePath = Path.Combine(appSettings.PlatformPath, "SAM.exe");
				if (!File.Exists(filePath)) throw new RunItemException("SAM.exe not found.");

				var args = GenerateSamArguments(appSettings);

				RunProcess(_title, filePath, args, appSettings.ExeDirs.FirstOrDefault() ?? appSettings.PlatformPath, pane, cancel, waitForExit: false, capture: false);
			}
		}

		private string GenerateSamArguments(DkAppSettings appSettings)
		{
			var args = new StringBuilder();

			var samName = CleanSamName(string.Concat(appSettings.AppName, "_", System.Environment.UserName));
			args.Append($"/N{samName}");
			args.Append($" /p{_port}");
			args.Append($" /o{(LazyLoadDlls ? 0 : 1)}");
			args.Append($" /y{_transReportTimeout:00}{_transAbortTimeout:00}");
			args.Append($" /z{_minRMs}");
			args.Append($" /Z{_maxRMs}");
			args.Append($" /P \"{appSettings.AppName}\"");
			if (DiagLevel > 0) args.Append($" /d{DiagLevel}");

			if (!string.IsNullOrWhiteSpace(_args)) args.Append($" {_args}");

			return args.ToString();
		}

		private void RunCam(DkAppSettings appSettings, OutputPane pane, CancellationToken cancel)
		{
			if (cancel.IsCancellationRequested) return;

			using (var proc = new Process())
			{
				var filePath = Path.GetFullPath(Path.Combine(appSettings.PlatformPath, "..\\CAMNet\\CAMNet.exe"));
				if (!File.Exists(filePath)) throw new RunItemException("CAMNet.exe not found.");

				var args = GenerateCamArguments(appSettings);

				RunProcess(_title, filePath, args, Path.GetDirectoryName(filePath), pane, cancel, waitForExit: false, capture: false);
			}
		}

		private string GenerateCamArguments(DkAppSettings appSettings)
		{
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

			return args.ToString();
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

		private void RunProcess(string title, string filePath, string args, string workingDir, OutputPane pane, CancellationToken cancel, bool waitForExit, bool capture)
		{
			if (cancel.IsCancellationRequested) return;

			ProbeToolsPackage.Instance.App.Log.Info("Running: {0}\r\nFile Path: {1}\r\nArguments: {2}\r\nWorking Dir: {3}", title, filePath, args, workingDir);
			pane?.WriteLine($"Running: {title}");
			pane?.WriteLine($"  {filePath} {args}");

			if (string.IsNullOrWhiteSpace(filePath)) throw new RunItemException($"No file path is configured.");

			if (string.IsNullOrWhiteSpace(workingDir) && !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
			{
				workingDir = Path.GetDirectoryName(filePath);
				ProbeToolsPackage.Instance.App.Log.Info("Derived working dir: {0}", workingDir);
			}

			if (waitForExit && capture)
			{
				var sw = new Stopwatch();
				sw.Start();

				var runner = new ProcessRunner
				{
					CaptureOutput = true,
					CaptureError = true
				};
				var exitCode = runner.CaptureProcess(filePath, args, workingDir, pane, cancel);

				sw.Stop();
				pane?.WriteLine($"Exit Code: {exitCode} (elapsed: {sw.Elapsed})");
			}
			else
			{
				using (var proc = new Process())
				{
					var psi = new ProcessStartInfo(filePath, args ?? string.Empty);
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = false;
					psi.RedirectStandardError = false;
					psi.CreateNoWindow = false;
					psi.WorkingDirectory = workingDir;

					proc.StartInfo = psi;

					var sw = new Stopwatch();
					if (waitForExit) sw.Start();

					if (!proc.Start()) throw new RunItemException($"Unable to start {title}.");

					if (waitForExit)
					{
						proc.WaitForExit();
						sw.Stop();
						pane?.WriteLine($"Exit Code: {proc.ExitCode} (elapsed: {sw.Elapsed})");
					}
				}
			}
		}

		public bool CanMoveUp
		{
			get => _canMoveUp;
			set
			{
				if (_canMoveUp != value)
				{
					_canMoveUp = value;
					FirePropertyChanged(nameof(CanMoveUp));
				}
			}
		}

		public bool CanMoveDown
		{
			get => _canMoveDown;
			set
			{
				if (_canMoveDown != value)
				{
					_canMoveDown = value;
					FirePropertyChanged(nameof(CanMoveDown));
				}
			}
		}

		public bool Selected
		{
			get => _selected;
			set
			{
				if (_selected != value)
				{
					_selected = value;
					FirePropertyChanged(nameof(Selected));
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}
	}

	enum RunItemType
	{
		Custom,
		Sam,
		Cam
	}

	class RunItemException : Exception
	{
		public RunItemException(string message) : base(message) { }
	}

	class InvalidRunItemTypeException : Exception { }
}
