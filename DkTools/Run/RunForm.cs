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
		private RunOptions _options;
		
		public RunForm()
		{
			_options = ProbeToolsPackage.Instance.RunOptions;

			InitializeComponent();
		}

		private void RunForm_Load(object sender, EventArgs e)
		{
			try
			{
				if (_options.RunSam && !_options.RunCam)
				{
					radSam.Checked = true;
					_focusControl = radSam;
				}
				else if (!_options.RunSam && _options.RunCam)
				{
					radCam.Checked = true;
					_focusControl = radCam;
				}
				else
				{
					radSamAndCam.Checked = true;
					_focusControl = radSamAndCam;
				}

				chkDiags.Checked = _options.Diags;
				chkLoadSam.Checked = _options.LoadSam;
				chkSetDbDate.Checked = _options.SetDbDate;
				txtTransReportTimeout.Text = _options.TransReportTimeout.ToString();
				txtTransAbortTimeout.Text = _options.TransAbortTimeout.ToString();
				txtMinChannels.Text = _options.MinChannels.ToString();
				txtMaxChannels.Text = _options.MaxChannels.ToString();
				txtLoadSamTime.Text = _options.LoadSamTime.ToString();
				chkCamDevMode.Checked = _options.CamDevMode;
				c_samExtraArgs.Text = _options.SamArgs;
				c_camExtraArgs.Text = _options.CamArgs;
				c_samCmdLine.Text = _options.CreateSamArgsString();
				c_camCmdLine.Text = _options.CreateCamArgsString();
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

				_options.RunSam = radSam.Checked || radSamAndCam.Checked;
				_options.RunCam = radCam.Checked || radSamAndCam.Checked;
				_options.Diags = chkDiags.Checked;
				_options.SetDbDate = chkSetDbDate.Checked;
				_options.TransReportTimeout = transReportTimeout;
				_options.TransAbortTimeout = transAbortTimeout;
				_options.MinChannels = minChannels;
				_options.MaxChannels = maxChannels;
				_options.LoadSamTime = loadSamTime;
				_options.CamDevMode = chkCamDevMode.Checked;
				_options.SamArgs = c_samExtraArgs.Text;
				_options.CamArgs = c_camExtraArgs.Text;

				_options.SaveSettingsToStorage();

				if (_options.RunSam && _options.RunCam)
				{
					if (RunSam(_options)) RunCam(_options);
				}
				else if (_options.RunSam)
				{
					RunSam(_options);
				}
				else if (_options.RunCam)
				{
					RunCam(_options);
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
				_options.LoadSettingsFromStorage();
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

			var args = options.CreateSamArgsString();

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

				ProcessStartInfo info = new ProcessStartInfo(exePathName, args);
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
			var args = options.CreateCamArgsString();

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

				ProcessStartInfo info = new ProcessStartInfo(exePathName, args);
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

		private void UpdateSamCmdLine()
		{
			c_samCmdLine.Text = _options.CreateSamArgsString();
		}

		private void UpdateCamCmdLine()
		{
			c_camCmdLine.Text = _options.CreateCamArgsString();
		}

		private void chkSetDbDate_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				_options.SetDbDate = chkSetDbDate.Checked;
				UpdateSamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void chkLoadSam_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				_options.LoadSam = chkLoadSam.Checked;
				UpdateSamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void txtLoadSamTime_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int value;
				if (int.TryParse(txtLoadSamTime.Text, out value) && value >= 0)
				{
					_options.LoadSamTime = value;
					UpdateSamCmdLine();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void txtTransReportTimeout_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int value;
				if (int.TryParse(txtTransReportTimeout.Text, out value) && value >= 0 && value <= 99)
				{
					_options.TransReportTimeout = value;
					UpdateSamCmdLine();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void txtTransAbortTimeout_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int value;
				if (int.TryParse(txtTransAbortTimeout.Text, out value) && value >= 0 && value <= 99)
				{
					_options.TransAbortTimeout = value;
					UpdateSamCmdLine();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void txtMinChannels_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int value;
				if (int.TryParse(txtMinChannels.Text, out value) && value >= 1 && value <= 2)
				{
					_options.MinChannels = value;
					UpdateSamCmdLine();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void txtMaxChannels_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int value;
				if (int.TryParse(txtMaxChannels.Text, out value) && value >= 1 && value <= 48)
				{
					_options.MaxChannels = value;
					UpdateSamCmdLine();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void c_samExtraArgs_TextChanged(object sender, EventArgs e)
		{
			try
			{
				_options.SamArgs = c_samExtraArgs.Text;
				UpdateSamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void chkCamDevMode_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				_options.CamDevMode = chkCamDevMode.Checked;
				UpdateCamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void c_camExtraArgs_TextChanged(object sender, EventArgs e)
		{
			try
			{
				_options.CamArgs = c_camExtraArgs.Text;
				UpdateCamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void chkDiags_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				_options.Diags = chkDiags.Checked;
				UpdateSamCmdLine();
				UpdateCamCmdLine();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
	}
}