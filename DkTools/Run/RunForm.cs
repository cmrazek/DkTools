using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DkTools.Run
{
	public partial class RunForm : Form
	{
		public enum RunApp
		{
			SamAndCam,
			Sam,
			Cam
		}

		private Control _focusControl = null;

		public RunForm()
		{
			InitializeComponent();
		}

		private void RunForm_Load(object sender, EventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.RunOptions;

				if (options.RunSam && !options.RunCam)
				{
					radSam.Checked = true;
					_focusControl = radSam;
				}
				else if (!options.RunSam && options.RunCam)
				{
					radCam.Checked = true;
					_focusControl = radCam;
				}
				else
				{
					radSamAndCam.Checked = true;
					_focusControl = radSamAndCam;
				}

				chkDiags.Checked = options.Diags;
				chkLoadSam.Checked = options.LoadSam;
				chkSetDbDate.Checked = options.SetDbDate;
				txtTransReportTimeout.Text = options.TransReportTimeout.ToString();
				txtTransAbortTimeout.Text = options.TransAbortTimeout.ToString();
				txtMinChannels.Text = options.MinChannels.ToString();
				txtMaxChannels.Text = options.MaxChannels.ToString();
				txtLoadSamTime.Text = options.LoadSamTime.ToString();
				chkCamDevMode.Checked = options.CamDevMode;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RunForm_Shown(object sender, EventArgs e)
		{
			try
			{
				if (_focusControl != null) _focusControl.Focus();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				int transReportTimeout, transAbortTimeout, minChannels, maxChannels, loadSamTime;

				if (!ValidateNumericTextBox(txtTransReportTimeout, "TransReport Timeout", 0, 99, out transReportTimeout)) return;
				if (!ValidateNumericTextBox(txtTransAbortTimeout, "TransAbort Timeout", 0, 99, out transAbortTimeout)) return;
				if (!ValidateNumericTextBox(txtMinChannels, "Minimum Resource Channels", 1, 2, out minChannels)) return;
				if (!ValidateNumericTextBox(txtMaxChannels, "Maximum Resource Channels", 1, 48, out maxChannels)) return;
				if (!ValidateNumericTextBox(txtLoadSamTime, "LoadSam Time", 0, 1000000, out loadSamTime)) return;

				var options = ProbeToolsPackage.Instance.RunOptions;

				options.RunSam = radSam.Checked || radSamAndCam.Checked;
				options.RunCam = radCam.Checked || radSamAndCam.Checked;
				options.Diags = chkDiags.Checked;
				options.SetDbDate = chkSetDbDate.Checked;
				options.TransReportTimeout = transReportTimeout;
				options.TransAbortTimeout = transAbortTimeout;
				options.MinChannels = minChannels;
				options.MaxChannels = maxChannels;
				options.LoadSamTime = loadSamTime;
				options.CamDevMode = chkCamDevMode.Checked;

				options.SaveSettingsToStorage();

				if (options.RunSam && options.RunCam)
				{
					if (RunSam(options)) RunCam(options);
				}
				else if (options.RunSam)
				{
					RunSam(options);
				}
				else if (options.RunCam)
				{
					RunCam(options);
				}

				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			try
			{
				DialogResult = DialogResult.Cancel;
				Close();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private bool RunSam(Run.RunOptions options)
		{
			if (options.SetDbDate)
			{
				using (var pr = new ProcessRunner())
				{
					int exitCode = pr.ExecuteProcess("setdbdat", "today force", ProbeEnvironment.TempDir, true);
					if (exitCode != 0)
					{
						this.ShowError(string.Format("\"setdbdat today force\" returned exit code {0}.\n\n" +
							"(The SAM will still start, but the dbdate may be incorrect)", exitCode));
					}
				}
			}

			StringBuilder args = new StringBuilder();
			args.Append(string.Format("/N{0}", CleanSamName(string.Concat(ProbeEnvironment.CurrentApp, "_", System.Environment.UserName))));
			args.Append(string.Format(" /p{0}", ProbeEnvironment.SamPort));
			args.Append(" /o0");
			args.Append(string.Format(" /y{0:00}{1:00}", options.TransReportTimeout, options.TransAbortTimeout));
			args.Append(string.Format(" /z{0}", options.MinChannels));
			args.Append(string.Format(" /Z{0}", options.MaxChannels));
			args.Append(string.Format(" /P{0}", ProbeEnvironment.CurrentApp));
			if (options.Diags) args.Append(" /d2");

			using (Process proc = new Process())
			{
				var platformPath = ProbeEnvironment.PlatformPath;
				if (string.IsNullOrEmpty(platformPath))
				{
					this.ShowError(Res.Err_DkPlatformDirUnknown);
					return false;
				}

				var exePathName = Path.Combine(platformPath, "SAM.exe");
				if (!File.Exists(exePathName))
				{
					this.ShowError("SAM.exe not found in path.");
					return false;
				}

				ProcessStartInfo info = new ProcessStartInfo(exePathName, args.ToString());
				info.UseShellExecute = false;
				info.RedirectStandardOutput = false;
				info.RedirectStandardError = false;
				info.CreateNoWindow = false;
				info.WorkingDirectory = ProbeEnvironment.ExeDirs.FirstOrDefault();
				proc.StartInfo = info;
				if (!proc.Start())
				{
					this.ShowError("Unable to start the SAM.");
					return false;
				}
			}

			if (options.LoadSam && !options.Diags) RunLoadSam(options);

			return true;
		}

		private bool RunCam(Run.RunOptions options)
		{
			StringBuilder args = new StringBuilder();
			args.Append("appname=" + ProbeEnvironment.CurrentApp);
			if (options.Diags) args.Append(" devmode");
			else if (options.CamDevMode) args.Append(" devmode=2");

			using (Process proc = new Process())
			{
				var platformPath = ProbeEnvironment.PlatformPath;
				if (string.IsNullOrEmpty(platformPath))
				{
					this.ShowError(Res.Err_DkPlatformDirUnknown);
					return false;
				}

				var exePathName = Path.Combine(platformPath, "..\\CAMNet\\CAMNet.exe");
				if (!File.Exists(exePathName))
				{
					this.ShowError("CAMNet.exe not found in path.");
					return false;
				}

				ProcessStartInfo info = new ProcessStartInfo(exePathName, args.ToString());
				info.UseShellExecute = false;
				info.RedirectStandardOutput = false;
				info.RedirectStandardError = false;
				info.CreateNoWindow = false;
				info.WorkingDirectory = ProbeEnvironment.ExeDirs.FirstOrDefault();
				proc.StartInfo = info;
				if (!proc.Start())
				{
					this.ShowError("Unable to start the CAM.");
					return false;
				}
			}

			return true;
		}

		private bool RunLoadSam(Run.RunOptions options)
		{
			string exeFileName = Path.Combine(Path.Combine(ProbeEnvironment.ExeDirs.FirstOrDefault(), "uattools"), "loadsam.exe");
			if (!File.Exists(exeFileName)) return false;

			string args = string.Format("/Nloadsam {0}", options.LoadSamTime);

			using (Process proc = new Process())
			{
				ProcessStartInfo info = new ProcessStartInfo(exeFileName, args);
				info.UseShellExecute = false;
				info.RedirectStandardOutput = false;
				info.RedirectStandardError = false;
				info.CreateNoWindow = false;
				info.WorkingDirectory = Path.GetDirectoryName(exeFileName);
				proc.StartInfo = info;
				if (!proc.Start()) return false;
			}

			return true;
		}

		private bool ValidateNumericTextBox(TextBox txtBox, string name, int minValue, int maxValue, out int valueOut)
		{
			int value;
			if (!Int32.TryParse(txtBox.Text, out value))
			{
				FocusControl(txtBox);
				this.ShowError(string.Format("{0} does not contain a valid number.", name));
				valueOut = 0;
				return false;
			}

			if (value < minValue || value > maxValue)
			{
				FocusControl(txtBox);
				this.ShowError(string.Format("{0} must be between {1} and {2}", name, minValue, maxValue));
				valueOut = 0;
				return false;
			}

			valueOut = value;
			return true;
		}

		private void FocusControl(Control ctrl)
		{
			// Select the page this control is on.
			TabPage tabPage = null;
			Control ctrlIter = ctrl.Parent;
			while (ctrlIter != null)
			{
				Type type = ctrlIter.GetType();
				if (type == typeof(TabPage) || type.IsSubclassOf(typeof(TabPage)))
				{
					tabPage = null;
					break;
				}
				ctrlIter = ctrlIter.Parent;
			}
			if (tabPage != null) tabControl.SelectedTab = tabPage;

			// Select the control.
			ctrl.Focus();
		}

		private string CleanSamName(string name)
		{
			StringBuilder sb = new StringBuilder(name.Length);
			foreach (char ch in name)
			{
				if (Char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
			}
			return sb.ToString();
		}

	}
}