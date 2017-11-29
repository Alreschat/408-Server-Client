namespace WindowsFormsApp2
{
    partial class Form1
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
            this.lbPort = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.btStart = new System.Windows.Forms.Button();
            this.lbClients = new System.Windows.Forms.Label();
            this.lbActivity = new System.Windows.Forms.Label();
            this.tbActivity = new System.Windows.Forms.RichTextBox();
            this.listClients = new System.Windows.Forms.ListBox();
            this.lbIP = new System.Windows.Forms.Label();
            this.tbIP = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lbPort
            // 
            this.lbPort.AutoSize = true;
            this.lbPort.Location = new System.Drawing.Point(13, 59);
            this.lbPort.Name = "lbPort";
            this.lbPort.Size = new System.Drawing.Size(71, 13);
            this.lbPort.TabIndex = 0;
            this.lbPort.Text = "Listening Port";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(16, 75);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(129, 20);
            this.tbPort.TabIndex = 1;
            this.tbPort.Text = "8910";
            // 
            // btStart
            // 
            this.btStart.Location = new System.Drawing.Point(163, 45);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(79, 38);
            this.btStart.TabIndex = 2;
            this.btStart.Text = "Start";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.btStart_Click);
            // 
            // lbClients
            // 
            this.lbClients.AutoSize = true;
            this.lbClients.Location = new System.Drawing.Point(12, 104);
            this.lbClients.Name = "lbClients";
            this.lbClients.Size = new System.Drawing.Size(55, 13);
            this.lbClients.TabIndex = 4;
            this.lbClients.Text = "Lobby List";
            // 
            // lbActivity
            // 
            this.lbActivity.AutoSize = true;
            this.lbActivity.Location = new System.Drawing.Point(271, 13);
            this.lbActivity.Name = "lbActivity";
            this.lbActivity.Size = new System.Drawing.Size(128, 13);
            this.lbActivity.TabIndex = 6;
            this.lbActivity.Text = "Server Activity/Messages";
            // 
            // tbActivity
            // 
            this.tbActivity.Location = new System.Drawing.Point(274, 31);
            this.tbActivity.Name = "tbActivity";
            this.tbActivity.ReadOnly = true;
            this.tbActivity.Size = new System.Drawing.Size(368, 321);
            this.tbActivity.TabIndex = 7;
            this.tbActivity.TabStop = false;
            this.tbActivity.Text = "";
            // 
            // listClients
            // 
            this.listClients.FormattingEnabled = true;
            this.listClients.Location = new System.Drawing.Point(16, 120);
            this.listClients.Name = "listClients";
            this.listClients.Size = new System.Drawing.Size(226, 225);
            this.listClients.TabIndex = 8;
            // 
            // lbIP
            // 
            this.lbIP.AutoSize = true;
            this.lbIP.Location = new System.Drawing.Point(13, 13);
            this.lbIP.Name = "lbIP";
            this.lbIP.Size = new System.Drawing.Size(58, 13);
            this.lbIP.TabIndex = 9;
            this.lbIP.Text = "IP Address";
            // 
            // tbIP
            // 
            this.tbIP.Location = new System.Drawing.Point(16, 29);
            this.tbIP.Name = "tbIP";
            this.tbIP.Size = new System.Drawing.Size(129, 20);
            this.tbIP.TabIndex = 10;
            this.tbIP.Text = "127.0.0.1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 374);
            this.Controls.Add(this.tbIP);
            this.Controls.Add(this.lbIP);
            this.Controls.Add(this.listClients);
            this.Controls.Add(this.tbActivity);
            this.Controls.Add(this.lbActivity);
            this.Controls.Add(this.lbClients);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.lbPort);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Server Module";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbPort;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.Label lbClients;
        private System.Windows.Forms.Label lbActivity;
        private System.Windows.Forms.RichTextBox tbActivity;
        private System.Windows.Forms.ListBox listClients;
        private System.Windows.Forms.Label lbIP;
        private System.Windows.Forms.TextBox tbIP;
    }
}

