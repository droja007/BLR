using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoanReview
{
    public partial class Form_Message_Box : Form
    {
        public Form_Message_Box()
        {
            InitializeComponent();
        }

        static Form_Message_Box mbox;
        static string ProcessValue;
        static Control ActiveControl;

        public static string Show(string Title, string Text, ComboBox control, string Button1Text, string Button2Text)
        {
            mbox = new Form_Message_Box();
            mbox.Text = Title;
            mbox.MessageLabel.Text = Text;
            mbox.PanelControl.Controls.Add(control);
            control.Dock = DockStyle.Right;
            control.SelectedIndex = 0;
            mbox.ProcessBtn.Text = Button1Text.ToString();
            mbox.CancelBtn.Text = Button2Text.ToString();
            ActiveControl = control;
            mbox.ShowDialog();
            return ProcessValue;
        }

        



        private void Form_Message_Box_Load(object sender, EventArgs e)
        {

        }

        private void ProcessBtn_Click(object sender, EventArgs e)
        {
            if (ActiveControl.GetType() == typeof(ComboBox))
            {
                ComboBox ctrl = (ComboBox)ActiveControl;

                ProcessValue = ctrl.SelectedItem.ToString().Replace(" ", ""); ;

            }
            mbox.Dispose();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            ProcessValue = "Cancel";
            mbox.Dispose();
        }

        protected override void WndProc(ref Message message)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MOVE = 0xF010;

            switch (message.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = message.WParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                        return;
                    break;
            }

            base.WndProc(ref message);
        }

    }
}
