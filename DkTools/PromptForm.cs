using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DkTools
{
	public partial class PromptForm : Form
	{
		private string _value = "";
		private bool _allowEmpty = false;

		public PromptForm()
		{
			InitializeComponent();
		}

		private void PromptForm_Load(object sender, EventArgs e)
		{
			try
			{
				txtValue.Text = _value;
				txtValue.Focus();
				txtValue.SelectAll();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public bool AllowEmpty
		{
			get { return _allowEmpty; }
			set { _allowEmpty = value; }
		}

		private void EnableControls(object sender, EventArgs e)
		{
			try
			{
				btnOk.Enabled = _allowEmpty || !string.IsNullOrEmpty(txtValue.Text);
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
				_value = txtValue.Text;
				if (!_allowEmpty && string.IsNullOrEmpty(_value)) return;

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

		public string Prompt
		{
			get { return txtPrompt.Text; }
			set { txtPrompt.Text = value; }
		}
	}
}