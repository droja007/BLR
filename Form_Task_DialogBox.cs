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
    public partial class Form_Task_DialogBox : Form
    {
        static LoanReviewEntities Entity;
        static Form_Task_DialogBox taskbox;
        static int Workflow_ID;
        static int Loan_ID;
        static int Loan_Number;
        static int User_ID;
        static string returnval;

        public Form_Task_DialogBox()
        {
            InitializeComponent();
        }

        //public Form_Task_DialogBox(LoanReviewEntities entity)
        //{
        //    Entity = entity;
        //}

        public static string Show(LoanReviewEntities LRAPPEntity, int LoanNumber, int LoanID, int WorkflowID, int UserID)
        {
            Entity = LRAPPEntity;
            Workflow_ID = WorkflowID;
            Loan_ID = LoanID;
            Loan_Number = LoanNumber;
            User_ID = UserID;
            taskbox = new Form_Task_DialogBox();
            taskbox.ShowDialog();
            return returnval;
        }

        private void Form_Task_DialogBox_Load(object sender, EventArgs e)
        {
            for (int x = 1; x < 10; x++)
            {
                TaskStatusCombobox.Items.Add(x.ToString());
            }

            LoanReviewCombobox.Items.Add("Loan review offshore completed");
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            DateTime tempDate;
            if (!String.IsNullOrEmpty(TaskDatetxtbox.Text.Replace('/', ' ').Trim()))
            {
                if (TaskDatetxtbox.Text.Trim().Length == 10)
                {
                    if ((DateTime.TryParse(TaskDatetxtbox.Text, out tempDate) ? true : false) == true)
                    {
                        if (TaskStatusCombobox.SelectedIndex != -1)
                        {
                            if (UserIdTextbox.Text.Length > 0)
                            {
                                if (LoanReviewCombobox.SelectedIndex != -1)
                                {
                                    string status = (LoanReviewCombobox.SelectedItem.ToString() == "Loan review offshore completed") ? "LROCMP" : " ";
                                    
                                    int? Exists = (from loan in Entity.Task_Tracking.AsNoTracking() where loan.LoanNumber == Loan_Number select loan).Count();

                                    if (Exists == 0)
                                    {
                                        Entity.InsertTaskTracking(Loan_ID, Loan_Number, Workflow_ID, TaskDatetxtbox.Text.ToString(), TaskStatusCombobox.SelectedItem.ToString(), UserIdTextbox.Text.ToString(), User_ID, "", status);
                                    }
                                    else
                                    {
                                        Entity.UpdateTaskTracking(Loan_ID, Loan_Number, Workflow_ID, TaskDatetxtbox.Text.ToString(), TaskStatusCombobox.SelectedItem.ToString(), UserIdTextbox.Text.ToString(), User_ID, "", status);
                                    }

                                    returnval = "1";
                                }
                                else
                                {
                                    MessageBox.Show("Please Select a Loan Review before submitting.", "Invalid Status Selection");
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Please Enter an MSP User ID before submitting.", "Invalid User ID");
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please Select a Status before submitting.", "Invalid Status Selection");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a real date.", "Invalid Date Selection");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Please Insert a Date with the correct format before submitting. ex: 01/01/2016", "Invalid Task Date Format");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Please Insert a Date before submitting.", "Invalid Task Date");
                return;
            }
            taskbox.Dispose();

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            returnval = "0";
            taskbox.Dispose();
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
