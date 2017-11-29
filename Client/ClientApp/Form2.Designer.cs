namespace ClientApp
{
    partial class Form2
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
            this.surrButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // surrButton
            // 
            this.surrButton.Location = new System.Drawing.Point(23, 46);
            this.surrButton.Name = "surrButton";
            this.surrButton.Size = new System.Drawing.Size(231, 152);
            this.surrButton.TabIndex = 0;
            this.surrButton.Text = "Surrender";
            this.surrButton.UseVisualStyleBackColor = true;
            this.surrButton.Click += new System.EventHandler(this.surrButton_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.surrButton);
            this.Name = "Form2";
            this.Text = "Form2";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button surrButton;
    }
}