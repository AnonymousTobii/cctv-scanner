namespace CCTV_Sentinel
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges15 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges16 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges13 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges14 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            label1 = new Label();
            lblStatus = new Label();
            guna2BorderlessForm1 = new Guna.UI2.WinForms.Guna2BorderlessForm(components);
            btnStart = new Guna.UI2.WinForms.Guna2Button();
            btnStop = new Guna.UI2.WinForms.Guna2Button();
            txtSubnet = new Guna.UI2.WinForms.Guna2TextBox();
            dgvResults = new Guna.UI2.WinForms.Guna2DataGridView();
            label2 = new Label();
            guna2ControlBox1 = new Guna.UI2.WinForms.Guna2ControlBox();
            guna2ControlBox2 = new Guna.UI2.WinForms.Guna2ControlBox();
            ((System.ComponentModel.ISupportInitialize)dgvResults).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(254, 224);
            label1.Name = "label1";
            label1.Size = new Size(47, 20);
            label1.TabIndex = 4;
            label1.Text = "status";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(307, 224);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(36, 20);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "N/A";
            // 
            // guna2BorderlessForm1
            // 
            guna2BorderlessForm1.ContainerControl = this;
            guna2BorderlessForm1.DockIndicatorTransparencyValue = 0.6D;
            guna2BorderlessForm1.ResizeForm = false;
            guna2BorderlessForm1.ShadowColor = SystemColors.MenuHighlight;
            guna2BorderlessForm1.TransparentWhileDrag = true;
            // 
            // btnStart
            // 
            btnStart.CustomizableEdges = customizableEdges15;
            btnStart.DisabledState.BorderColor = Color.DarkGray;
            btnStart.DisabledState.CustomBorderColor = Color.DarkGray;
            btnStart.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnStart.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnStart.Font = new Font("Segoe UI", 9F);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(512, 106);
            btnStart.Name = "btnStart";
            btnStart.ShadowDecoration.CustomizableEdges = customizableEdges16;
            btnStart.Size = new Size(225, 56);
            btnStart.TabIndex = 6;
            btnStart.Text = "Start";
            btnStart.Click += btnStart_Click_1;
            // 
            // btnStop
            // 
            btnStop.CustomizableEdges = customizableEdges13;
            btnStop.DisabledState.BorderColor = Color.DarkGray;
            btnStop.DisabledState.CustomBorderColor = Color.DarkGray;
            btnStop.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnStop.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnStop.Font = new Font("Segoe UI", 9F);
            btnStop.ForeColor = Color.White;
            btnStop.Location = new Point(769, 106);
            btnStop.Name = "btnStop";
            btnStop.ShadowDecoration.CustomizableEdges = customizableEdges14;
            btnStop.Size = new Size(225, 56);
            btnStop.TabIndex = 7;
            btnStop.Text = "Stop";
            btnStop.Click += btnStop_Click;
            // 
            // txtSubnet
            // 
            txtSubnet.CustomizableEdges = customizableEdges11;
            txtSubnet.DefaultText = "";
            txtSubnet.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            txtSubnet.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            txtSubnet.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            txtSubnet.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            txtSubnet.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            txtSubnet.Font = new Font("Segoe UI", 9F);
            txtSubnet.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
            txtSubnet.Location = new Point(149, 102);
            txtSubnet.Margin = new Padding(3, 4, 3, 4);
            txtSubnet.Name = "txtSubnet";
            txtSubnet.PlaceholderText = "";
            txtSubnet.SelectedText = "";
            txtSubnet.ShadowDecoration.CustomizableEdges = customizableEdges12;
            txtSubnet.Size = new Size(286, 60);
            txtSubnet.TabIndex = 8;
            // 
            // dgvResults
            // 
            dataGridViewCellStyle4.BackColor = Color.White;
            dgvResults.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(100, 88, 255);
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = Color.White;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
            dgvResults.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dgvResults.ColumnHeadersHeight = 4;
            dgvResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.White;
            dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            dgvResults.DefaultCellStyle = dataGridViewCellStyle6;
            dgvResults.GridColor = Color.FromArgb(231, 229, 255);
            dgvResults.Location = new Point(44, 247);
            dgvResults.Name = "dgvResults";
            dgvResults.RowHeadersVisible = false;
            dgvResults.RowHeadersWidth = 51;
            dgvResults.Size = new Size(1051, 288);
            dgvResults.TabIndex = 10;
            dgvResults.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            dgvResults.ThemeStyle.AlternatingRowsStyle.Font = null;
            dgvResults.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            dgvResults.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            dgvResults.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            dgvResults.ThemeStyle.BackColor = Color.White;
            dgvResults.ThemeStyle.GridColor = Color.FromArgb(231, 229, 255);
            dgvResults.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            dgvResults.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvResults.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            dgvResults.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvResults.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvResults.ThemeStyle.HeaderStyle.Height = 4;
            dgvResults.ThemeStyle.ReadOnly = false;
            dgvResults.ThemeStyle.RowsStyle.BackColor = Color.White;
            dgvResults.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvResults.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            dgvResults.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            dgvResults.ThemeStyle.RowsStyle.Height = 29;
            dgvResults.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dgvResults.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Cambria", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.Black;
            label2.Location = new Point(434, 34);
            label2.Name = "label2";
            label2.Size = new Size(232, 33);
            label2.TabIndex = 11;
            label2.Text = "Anonymous Tobi\r\n";
            // 
            // guna2ControlBox1
            // 
            guna2ControlBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2ControlBox1.CustomizableEdges = customizableEdges9;
            guna2ControlBox1.FillColor = Color.Transparent;
            guna2ControlBox1.IconColor = Color.Black;
            guna2ControlBox1.Location = new Point(1073, -1);
            guna2ControlBox1.Name = "guna2ControlBox1";
            guna2ControlBox1.ShadowDecoration.CustomizableEdges = customizableEdges10;
            guna2ControlBox1.Size = new Size(56, 36);
            guna2ControlBox1.TabIndex = 12;
            // 
            // guna2ControlBox2
            // 
            guna2ControlBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2ControlBox2.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            guna2ControlBox2.CustomizableEdges = customizableEdges7;
            guna2ControlBox2.FillColor = Color.Transparent;
            guna2ControlBox2.IconColor = Color.Black;
            guna2ControlBox2.Location = new Point(1026, -1);
            guna2ControlBox2.Name = "guna2ControlBox2";
            guna2ControlBox2.ShadowDecoration.CustomizableEdges = customizableEdges8;
            guna2ControlBox2.Size = new Size(56, 36);
            guna2ControlBox2.TabIndex = 13;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightCyan;
            ClientSize = new Size(1128, 558);
            Controls.Add(guna2ControlBox2);
            Controls.Add(guna2ControlBox1);
            Controls.Add(label2);
            Controls.Add(dgvResults);
            Controls.Add(txtSubnet);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(lblStatus);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dgvResults).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private Label lblStatus;
        private Guna.UI2.WinForms.Guna2BorderlessForm guna2BorderlessForm1;
        private Guna.UI2.WinForms.Guna2Button btnStop;
        private Guna.UI2.WinForms.Guna2Button btnStart;
        private Guna.UI2.WinForms.Guna2DataGridView dgvResults;
        private Guna.UI2.WinForms.Guna2TextBox txtSubnet;
        private Label label2;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox2;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox1;
    }
}
