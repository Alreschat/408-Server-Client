namespace ClientApp
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
            this.tbActivity = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbIP = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbClient = new System.Windows.Forms.TextBox();
            this.btStart = new System.Windows.Forms.Button();
            this.btRequest = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.listClients = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // tbActivity
            // 
            this.tbActivity.Location = new System.Drawing.Point(284, 29);
            this.tbActivity.Name = "tbActivity";
            this.tbActivity.ReadOnly = true;
            this.tbActivity.Size = new System.Drawing.Size(358, 321);
            this.tbActivity.TabIndex = 7;
            this.tbActivity.TabStop = false;
            this.tbActivity.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "IP Address";
            // 
            // tbIP
            // 
            this.tbIP.Location = new System.Drawing.Point(16, 29);
            this.tbIP.Name = "tbIP";
            this.tbIP.Size = new System.Drawing.Size(120, 20);
            this.tbIP.TabIndex = 3;
            this.tbIP.Text = "127.0.0.1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(166, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Port Number";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(169, 29);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(80, 20);
            this.tbPort.TabIndex = 5;
            this.tbPort.Text = "8910";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Client Name";
            // 
            // tbClient
            // 
            this.tbClient.Location = new System.Drawing.Point(16, 77);
            this.tbClient.Name = "tbClient";
            this.tbClient.Size = new System.Drawing.Size(120, 20);
            this.tbClient.TabIndex = 7;
            this.tbClient.Text = "Test";
            // 
            // btStart
            // 
            this.btStart.Location = new System.Drawing.Point(169, 73);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(80, 24);
            this.btStart.TabIndex = 8;
            this.btStart.Text = "Start";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.btStart_Click);
            // 
            // btRequest
            // 
            this.btRequest.Enabled = false;
            this.btRequest.Location = new System.Drawing.Point(92, 328);
            this.btRequest.Name = "btRequest";
            this.btRequest.Size = new System.Drawing.Size(79, 24);
            this.btRequest.TabIndex = 9;
            this.btRequest.Text = "Request";
            this.btRequest.UseVisualStyleBackColor = true;
            this.btRequest.Click += new System.EventHandler(this.btRequest_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(281, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(123, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Client Activity/Messages";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 110);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Lobby List";
            // 
            // listClients
            // 
            this.listClients.FormattingEnabled = true;
            this.listClients.Location = new System.Drawing.Point(16, 126);
            this.listClients.Name = "listClients";
            this.listClients.Size = new System.Drawing.Size(233, 199);
            this.listClients.TabIndex = 16;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 374);
            this.Controls.Add(this.listClients);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btRequest);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.tbClient);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbIP);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbActivity);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Client Module";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox tbActivity;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbIP;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbClient;
        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.Button btRequest;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox listClients;
    }
}

