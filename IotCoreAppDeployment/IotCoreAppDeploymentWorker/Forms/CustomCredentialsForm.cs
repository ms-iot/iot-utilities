using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Iot.IotCoreAppDeployment
{
    public partial class CustomCredentialsForm : Form
    {
        public CustomCredentialsForm(string username, string password)
        {
            InitializeComponent();

            this.Text = Resource.DeploymentWorker_EnterCustomCredentials;
            usernameLabel.Text = Resource.DeploymentWorker_CustomUsername;
            passwordLabel.Text = Resource.DeploymentWorker_CustomPassword;
            okButton.Text = Resource.DeploymentWorker_Ok;
            cancelButton.Text = Resource.DeploymentWorker_Cancel;

            usernameText.Text = username;
            passwordText.Text = password;
        }
        public string Username
        {
            get
            {
                return usernameText.Text;
            }
        }
        public string Password
        {
            get
            {
                return passwordText.Text;
            }
        }

        private void CustomCredentialsForm_Load(object sender, EventArgs e)
        {

        }

        private void CustomCredentialsForm_Shown(object sender, EventArgs e)
        {
            SetForegroundWindow(this.Handle);
            usernameText.Focus();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void passwordLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
