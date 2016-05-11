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
    public partial class TargetNameForm : Form
    {
        public TargetNameForm(string initialValue)
        {
            InitializeComponent();

            this.Text = Resource.DeploymentWorker_GetTargetFromUserTitle;
            targetLabelText.Text = Resource.DeploymentWorker_GetTargetFromUser;
            okButton.Text = Resource.DeploymentWorker_Ok;
            cancelButton.Text = Resource.DeploymentWorker_Cancel;

            targetNameText.Text = initialValue;

        }

        public string TargetName
        {
            get
            {
                return targetNameText.Text;
            }
        }

        private void TargetNameForm_Load(object sender, EventArgs e)
        {
        }

        private void TargetNameForm_Shown(object sender, EventArgs e)
        {
            SetForegroundWindow(this.Handle);
            targetNameText.Focus();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
