namespace Microsoft.Iot.IotCoreAppDeployment
{
    partial class TargetNameForm
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
            this.targetNameText = new System.Windows.Forms.TextBox();
            this.targetLabelText = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // targetNameText
            // 
            this.targetNameText.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.targetNameText.Location = new System.Drawing.Point(17, 39);
            this.targetNameText.Name = "targetNameText";
            this.targetNameText.Size = new System.Drawing.Size(257, 20);
            this.targetNameText.TabIndex = 0;
            this.targetNameText.WordWrap = false;
            // 
            // targetLabelText
            // 
            this.targetLabelText.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.targetLabelText.Location = new System.Drawing.Point(14, 3);
            this.targetLabelText.Name = "targetLabelText";
            this.targetLabelText.Size = new System.Drawing.Size(260, 23);
            this.targetLabelText.TabIndex = 1;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(199, 65);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(118, 65);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // TargetNameForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(288, 98);
            this.Controls.Add(this.targetNameText);
            this.Controls.Add(this.targetLabelText);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TargetNameForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.TargetNameForm_Load);
            this.Shown += new System.EventHandler(this.TargetNameForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox targetNameText;
        private System.Windows.Forms.Label targetLabelText;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
    }
}