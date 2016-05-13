namespace Microsoft.Iot.IotCoreAppDeployment
{
    partial class CustomCredentialsForm
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.usernameLabel = new System.Windows.Forms.Label();
            this.usernameText = new System.Windows.Forms.TextBox();
            this.passwordText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(197, 160);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(116, 160);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // passwordLabel
            // 
            this.passwordLabel.Location = new System.Drawing.Point(12, 78);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(260, 23);
            this.passwordLabel.TabIndex = 3;
            this.passwordLabel.Click += new System.EventHandler(this.passwordLabel_Click);
            // 
            // usernameLabel
            // 
            this.usernameLabel.Location = new System.Drawing.Point(12, 9);
            this.usernameLabel.Name = "usernameLabel";
            this.usernameLabel.Size = new System.Drawing.Size(260, 23);
            this.usernameLabel.TabIndex = 2;
            // 
            // usernameText
            // 
            this.usernameText.Location = new System.Drawing.Point(15, 45);
            this.usernameText.Name = "usernameText";
            this.usernameText.Size = new System.Drawing.Size(257, 20);
            this.usernameText.TabIndex = 0;
            // 
            // passwordText
            // 
            this.passwordText.Location = new System.Drawing.Point(15, 113);
            this.passwordText.Name = "passwordText";
            this.passwordText.Size = new System.Drawing.Size(260, 20);
            this.passwordText.TabIndex = 1;
            this.passwordText.UseSystemPasswordChar = true;
            // 
            // CustomCredentialsForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(284, 204);
            this.Controls.Add(this.passwordText);
            this.Controls.Add(this.usernameText);
            this.Controls.Add(this.usernameLabel);
            this.Controls.Add(this.passwordLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CustomCredentialsForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.CustomCredentialsForm_Load);
            this.Shown += new System.EventHandler(this.CustomCredentialsForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.Label usernameLabel;
        private System.Windows.Forms.TextBox usernameText;
        private System.Windows.Forms.TextBox passwordText;
    }
}