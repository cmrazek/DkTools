namespace DkTools.Run
{
	partial class RunForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.radCam = new System.Windows.Forms.RadioButton();
			this.radSam = new System.Windows.Forms.RadioButton();
			this.radSamAndCam = new System.Windows.Forms.RadioButton();
			this.chkDiags = new System.Windows.Forms.CheckBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.chkLoadSam = new System.Windows.Forms.CheckBox();
			this.chkSetDbDate = new System.Windows.Forms.CheckBox();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabApp = new System.Windows.Forms.TabPage();
			this.txtCamDiagsDevModeWarning = new System.Windows.Forms.TextBox();
			this.tabSam = new System.Windows.Forms.TabPage();
			this.c_samCmdLineLabel = new System.Windows.Forms.Label();
			this.c_samCmdLine = new System.Windows.Forms.TextBox();
			this.c_samExtraArgs = new System.Windows.Forms.TextBox();
			this.c_samExtraArgsLabel = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.txtMaxChannels = new System.Windows.Forms.TextBox();
			this.txtMinChannels = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.txtTransAbortTimeout = new System.Windows.Forms.TextBox();
			this.txtTransReportTimeout = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.txtLoadSamTime = new System.Windows.Forms.TextBox();
			this.tabCam = new System.Windows.Forms.TabPage();
			this.c_camCmdLine = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.c_camExtraArgs = new System.Windows.Forms.TextBox();
			this.chkCamDevMode = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.chkCamDesignMode = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tabApp.SuspendLayout();
			this.tabSam.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.tabCam.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.radCam);
			this.groupBox1.Controls.Add(this.radSam);
			this.groupBox1.Controls.Add(this.radSamAndCam);
			this.groupBox1.Location = new System.Drawing.Point(6, 6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(115, 91);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Application";
			// 
			// radCam
			// 
			this.radCam.AutoSize = true;
			this.radCam.Location = new System.Drawing.Point(6, 65);
			this.radCam.Name = "radCam";
			this.radCam.Size = new System.Drawing.Size(70, 17);
			this.radCam.TabIndex = 2;
			this.radCam.TabStop = true;
			this.radCam.Text = "&CAM only";
			this.radCam.UseVisualStyleBackColor = true;
			this.radCam.CheckedChanged += new System.EventHandler(this.radCam_CheckedChanged);
			// 
			// radSam
			// 
			this.radSam.AutoSize = true;
			this.radSam.Location = new System.Drawing.Point(6, 42);
			this.radSam.Name = "radSam";
			this.radSam.Size = new System.Drawing.Size(70, 17);
			this.radSam.TabIndex = 1;
			this.radSam.TabStop = true;
			this.radSam.Text = "&SAM only";
			this.radSam.UseVisualStyleBackColor = true;
			this.radSam.CheckedChanged += new System.EventHandler(this.radSam_CheckedChanged);
			// 
			// radSamAndCam
			// 
			this.radSamAndCam.AutoSize = true;
			this.radSamAndCam.Location = new System.Drawing.Point(6, 19);
			this.radSamAndCam.Name = "radSamAndCam";
			this.radSamAndCam.Size = new System.Drawing.Size(95, 17);
			this.radSamAndCam.TabIndex = 0;
			this.radSamAndCam.TabStop = true;
			this.radSamAndCam.Text = "SAM &and CAM";
			this.radSamAndCam.UseVisualStyleBackColor = true;
			this.radSamAndCam.CheckedChanged += new System.EventHandler(this.radSamAndCam_CheckedChanged);
			// 
			// chkDiags
			// 
			this.chkDiags.AutoSize = true;
			this.chkDiags.Location = new System.Drawing.Point(127, 26);
			this.chkDiags.Name = "chkDiags";
			this.chkDiags.Size = new System.Drawing.Size(53, 17);
			this.chkDiags.TabIndex = 1;
			this.chkDiags.Text = "&Diags";
			this.chkDiags.UseVisualStyleBackColor = true;
			this.chkDiags.CheckedChanged += new System.EventHandler(this.chkDiags_CheckedChanged);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(184, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "&OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(265, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// chkLoadSam
			// 
			this.chkLoadSam.AutoSize = true;
			this.chkLoadSam.Location = new System.Drawing.Point(6, 29);
			this.chkLoadSam.Name = "chkLoadSam";
			this.chkLoadSam.Size = new System.Drawing.Size(71, 17);
			this.chkLoadSam.TabIndex = 1;
			this.chkLoadSam.Text = "&LoadSam";
			this.chkLoadSam.UseVisualStyleBackColor = true;
			this.chkLoadSam.CheckedChanged += new System.EventHandler(this.chkLoadSam_CheckedChanged);
			// 
			// chkSetDbDate
			// 
			this.chkSetDbDate.AutoSize = true;
			this.chkSetDbDate.Location = new System.Drawing.Point(6, 6);
			this.chkSetDbDate.Name = "chkSetDbDate";
			this.chkSetDbDate.Size = new System.Drawing.Size(83, 17);
			this.chkSetDbDate.TabIndex = 0;
			this.chkSetDbDate.Text = "Set D&BDate";
			this.chkSetDbDate.UseVisualStyleBackColor = true;
			this.chkSetDbDate.CheckedChanged += new System.EventHandler(this.chkSetDbDate_CheckedChanged);
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.tabApp);
			this.tabControl.Controls.Add(this.tabSam);
			this.tabControl.Controls.Add(this.tabCam);
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(343, 216);
			this.tabControl.TabIndex = 0;
			// 
			// tabApp
			// 
			this.tabApp.Controls.Add(this.txtCamDiagsDevModeWarning);
			this.tabApp.Controls.Add(this.groupBox1);
			this.tabApp.Controls.Add(this.chkDiags);
			this.tabApp.Location = new System.Drawing.Point(4, 22);
			this.tabApp.Name = "tabApp";
			this.tabApp.Padding = new System.Windows.Forms.Padding(3);
			this.tabApp.Size = new System.Drawing.Size(335, 190);
			this.tabApp.TabIndex = 0;
			this.tabApp.Text = "Applications";
			this.tabApp.UseVisualStyleBackColor = true;
			// 
			// txtCamDiagsDevModeWarning
			// 
			this.txtCamDiagsDevModeWarning.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtCamDiagsDevModeWarning.ForeColor = System.Drawing.Color.Red;
			this.txtCamDiagsDevModeWarning.Location = new System.Drawing.Point(145, 50);
			this.txtCamDiagsDevModeWarning.Multiline = true;
			this.txtCamDiagsDevModeWarning.Name = "txtCamDiagsDevModeWarning";
			this.txtCamDiagsDevModeWarning.Size = new System.Drawing.Size(182, 47);
			this.txtCamDiagsDevModeWarning.TabIndex = 2;
			this.txtCamDiagsDevModeWarning.Text = "Note: When diags are enabled CAM.NET will run in dev mode.";
			// 
			// tabSam
			// 
			this.tabSam.Controls.Add(this.c_samCmdLineLabel);
			this.tabSam.Controls.Add(this.c_samCmdLine);
			this.tabSam.Controls.Add(this.c_samExtraArgs);
			this.tabSam.Controls.Add(this.c_samExtraArgsLabel);
			this.tabSam.Controls.Add(this.groupBox3);
			this.tabSam.Controls.Add(this.groupBox2);
			this.tabSam.Controls.Add(this.label9);
			this.tabSam.Controls.Add(this.chkSetDbDate);
			this.tabSam.Controls.Add(this.chkLoadSam);
			this.tabSam.Controls.Add(this.txtLoadSamTime);
			this.tabSam.Location = new System.Drawing.Point(4, 22);
			this.tabSam.Name = "tabSam";
			this.tabSam.Padding = new System.Windows.Forms.Padding(3);
			this.tabSam.Size = new System.Drawing.Size(335, 190);
			this.tabSam.TabIndex = 1;
			this.tabSam.Text = "SAM Settings";
			this.tabSam.UseVisualStyleBackColor = true;
			// 
			// c_samCmdLineLabel
			// 
			this.c_samCmdLineLabel.AutoSize = true;
			this.c_samCmdLineLabel.Location = new System.Drawing.Point(8, 165);
			this.c_samCmdLineLabel.Name = "c_samCmdLineLabel";
			this.c_samCmdLineLabel.Size = new System.Drawing.Size(80, 13);
			this.c_samCmdLineLabel.TabIndex = 8;
			this.c_samCmdLineLabel.Text = "Command Line:";
			// 
			// c_samCmdLine
			// 
			this.c_samCmdLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.c_samCmdLine.Location = new System.Drawing.Point(94, 162);
			this.c_samCmdLine.Name = "c_samCmdLine";
			this.c_samCmdLine.ReadOnly = true;
			this.c_samCmdLine.Size = new System.Drawing.Size(225, 20);
			this.c_samCmdLine.TabIndex = 9;
			// 
			// c_samExtraArgs
			// 
			this.c_samExtraArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.c_samExtraArgs.Location = new System.Drawing.Point(94, 136);
			this.c_samExtraArgs.Name = "c_samExtraArgs";
			this.c_samExtraArgs.Size = new System.Drawing.Size(225, 20);
			this.c_samExtraArgs.TabIndex = 7;
			this.c_samExtraArgs.TextChanged += new System.EventHandler(this.c_samExtraArgs_TextChanged);
			// 
			// c_samExtraArgsLabel
			// 
			this.c_samExtraArgsLabel.AutoSize = true;
			this.c_samExtraArgsLabel.Location = new System.Drawing.Point(6, 139);
			this.c_samExtraArgsLabel.Name = "c_samExtraArgsLabel";
			this.c_samExtraArgsLabel.Size = new System.Drawing.Size(58, 13);
			this.c_samExtraArgsLabel.TabIndex = 6;
			this.c_samExtraArgsLabel.Text = "Extra Args:";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.txtMaxChannels);
			this.groupBox3.Controls.Add(this.txtMinChannels);
			this.groupBox3.Controls.Add(this.label8);
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Location = new System.Drawing.Point(200, 52);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(119, 79);
			this.groupBox3.TabIndex = 5;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Resource Channels";
			// 
			// txtMaxChannels
			// 
			this.txtMaxChannels.Location = new System.Drawing.Point(42, 46);
			this.txtMaxChannels.Name = "txtMaxChannels";
			this.txtMaxChannels.Size = new System.Drawing.Size(40, 20);
			this.txtMaxChannels.TabIndex = 1;
			this.txtMaxChannels.TextChanged += new System.EventHandler(this.txtMaxChannels_TextChanged);
			// 
			// txtMinChannels
			// 
			this.txtMinChannels.Location = new System.Drawing.Point(42, 19);
			this.txtMinChannels.Name = "txtMinChannels";
			this.txtMinChannels.Size = new System.Drawing.Size(40, 20);
			this.txtMinChannels.TabIndex = 1;
			this.txtMinChannels.TextChanged += new System.EventHandler(this.txtMinChannels_TextChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 49);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(30, 13);
			this.label8.TabIndex = 0;
			this.label8.Text = "Max:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 22);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(27, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "Min:";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.txtTransAbortTimeout);
			this.groupBox2.Controls.Add(this.txtTransReportTimeout);
			this.groupBox2.Location = new System.Drawing.Point(6, 52);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(188, 78);
			this.groupBox2.TabIndex = 4;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Timeouts";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 49);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(62, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "TransAbort:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(127, 49);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(53, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "(seconds)";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(127, 22);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "(seconds)";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(69, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "TransReport:";
			// 
			// txtTransAbortTimeout
			// 
			this.txtTransAbortTimeout.Location = new System.Drawing.Point(81, 46);
			this.txtTransAbortTimeout.Name = "txtTransAbortTimeout";
			this.txtTransAbortTimeout.Size = new System.Drawing.Size(40, 20);
			this.txtTransAbortTimeout.TabIndex = 1;
			this.txtTransAbortTimeout.TextChanged += new System.EventHandler(this.txtTransAbortTimeout_TextChanged);
			// 
			// txtTransReportTimeout
			// 
			this.txtTransReportTimeout.Location = new System.Drawing.Point(81, 19);
			this.txtTransReportTimeout.Name = "txtTransReportTimeout";
			this.txtTransReportTimeout.Size = new System.Drawing.Size(40, 20);
			this.txtTransReportTimeout.TabIndex = 0;
			this.txtTransReportTimeout.TextChanged += new System.EventHandler(this.txtTransReportTimeout_TextChanged);
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(153, 30);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(69, 13);
			this.label9.TabIndex = 3;
			this.label9.Text = "(milliseconds)";
			// 
			// txtLoadSamTime
			// 
			this.txtLoadSamTime.Location = new System.Drawing.Point(87, 27);
			this.txtLoadSamTime.Name = "txtLoadSamTime";
			this.txtLoadSamTime.Size = new System.Drawing.Size(60, 20);
			this.txtLoadSamTime.TabIndex = 2;
			this.txtLoadSamTime.TextChanged += new System.EventHandler(this.txtLoadSamTime_TextChanged);
			// 
			// tabCam
			// 
			this.tabCam.Controls.Add(this.chkCamDesignMode);
			this.tabCam.Controls.Add(this.c_camCmdLine);
			this.tabCam.Controls.Add(this.label6);
			this.tabCam.Controls.Add(this.label5);
			this.tabCam.Controls.Add(this.c_camExtraArgs);
			this.tabCam.Controls.Add(this.chkCamDevMode);
			this.tabCam.Location = new System.Drawing.Point(4, 22);
			this.tabCam.Name = "tabCam";
			this.tabCam.Padding = new System.Windows.Forms.Padding(3);
			this.tabCam.Size = new System.Drawing.Size(335, 190);
			this.tabCam.TabIndex = 2;
			this.tabCam.Text = "CAM Settings";
			this.tabCam.UseVisualStyleBackColor = true;
			// 
			// c_camCmdLine
			// 
			this.c_camCmdLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.c_camCmdLine.Location = new System.Drawing.Point(94, 165);
			this.c_camCmdLine.Name = "c_camCmdLine";
			this.c_camCmdLine.ReadOnly = true;
			this.c_camCmdLine.Size = new System.Drawing.Size(235, 20);
			this.c_camCmdLine.TabIndex = 4;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 168);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(80, 13);
			this.label6.TabIndex = 3;
			this.label6.Text = "Command Line:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(8, 142);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(58, 13);
			this.label5.TabIndex = 1;
			this.label5.Text = "Extra Args:";
			// 
			// c_camExtraArgs
			// 
			this.c_camExtraArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.c_camExtraArgs.Location = new System.Drawing.Point(94, 139);
			this.c_camExtraArgs.Name = "c_camExtraArgs";
			this.c_camExtraArgs.Size = new System.Drawing.Size(235, 20);
			this.c_camExtraArgs.TabIndex = 2;
			this.c_camExtraArgs.TextChanged += new System.EventHandler(this.c_camExtraArgs_TextChanged);
			// 
			// chkCamDevMode
			// 
			this.chkCamDevMode.AutoSize = true;
			this.chkCamDevMode.Location = new System.Drawing.Point(6, 6);
			this.chkCamDevMode.Name = "chkCamDevMode";
			this.chkCamDevMode.Size = new System.Drawing.Size(76, 17);
			this.chkCamDevMode.TabIndex = 0;
			this.chkCamDevMode.Text = "Dev Mode";
			this.chkCamDevMode.UseVisualStyleBackColor = true;
			this.chkCamDevMode.CheckedChanged += new System.EventHandler(this.chkCamDevMode_CheckedChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.btnCancel);
			this.panel1.Controls.Add(this.btnOk);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 216);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(343, 30);
			this.panel1.TabIndex = 1;
			// 
			// chkCamDesignMode
			// 
			this.chkCamDesignMode.AutoSize = true;
			this.chkCamDesignMode.Location = new System.Drawing.Point(6, 29);
			this.chkCamDesignMode.Name = "chkCamDesignMode";
			this.chkCamDesignMode.Size = new System.Drawing.Size(89, 17);
			this.chkCamDesignMode.TabIndex = 5;
			this.chkCamDesignMode.Text = "Design Mode";
			this.chkCamDesignMode.UseVisualStyleBackColor = true;
			this.chkCamDesignMode.CheckedChanged += new System.EventHandler(this.chkCamDesignMode_CheckedChanged);
			// 
			// RunForm
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(343, 246);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(this.panel1);
			this.MaximumSize = new System.Drawing.Size(32767, 285);
			this.MinimumSize = new System.Drawing.Size(359, 285);
			this.Name = "RunForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Run SAM/CAM";
			this.Load += new System.EventHandler(this.RunForm_Load);
			this.Shown += new System.EventHandler(this.RunForm_Shown);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.tabControl.ResumeLayout(false);
			this.tabApp.ResumeLayout(false);
			this.tabApp.PerformLayout();
			this.tabSam.ResumeLayout(false);
			this.tabSam.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.tabCam.ResumeLayout(false);
			this.tabCam.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radCam;
		private System.Windows.Forms.RadioButton radSam;
		private System.Windows.Forms.RadioButton radSamAndCam;
		private System.Windows.Forms.CheckBox chkDiags;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox chkLoadSam;
		private System.Windows.Forms.CheckBox chkSetDbDate;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabApp;
		private System.Windows.Forms.TabPage tabSam;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TabPage tabCam;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtTransAbortTimeout;
		private System.Windows.Forms.TextBox txtTransReportTimeout;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.TextBox txtMaxChannels;
		private System.Windows.Forms.TextBox txtMinChannels;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TextBox txtLoadSamTime;
		private System.Windows.Forms.CheckBox chkCamDevMode;
		private System.Windows.Forms.Label c_samCmdLineLabel;
		private System.Windows.Forms.TextBox c_samCmdLine;
		private System.Windows.Forms.TextBox c_samExtraArgs;
		private System.Windows.Forms.Label c_samExtraArgsLabel;
		private System.Windows.Forms.TextBox c_camCmdLine;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox c_camExtraArgs;
		private System.Windows.Forms.TextBox txtCamDiagsDevModeWarning;
		private System.Windows.Forms.CheckBox chkCamDesignMode;
	}
}