namespace LoanReview
{
    partial class Form_Task_DialogBox
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
            this.MessageLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.TaskDatetxtbox = new System.Windows.Forms.MaskedTextBox();
            this.TaskStatusCombobox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.UserIdTextbox = new System.Windows.Forms.TextBox();
            this.LoanReviewCombobox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // MessageLabel
            // 
            this.MessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageLabel.Location = new System.Drawing.Point(12, 9);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(320, 43);
            this.MessageLabel.TabIndex = 0;
            this.MessageLabel.Text = "Please Complete the required Task information. Required before submitting.";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(13, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "Date";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(13, 103);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 23);
            this.label2.TabIndex = 2;
            this.label2.Text = "Task Status";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(138, 199);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(75, 23);
            this.btnProcess.TabIndex = 3;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(228, 199);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // TaskDatetxtbox
            // 
            this.TaskDatetxtbox.Location = new System.Drawing.Point(140, 68);
            this.TaskDatetxtbox.Mask = "00/00/0000";
            this.TaskDatetxtbox.Name = "TaskDatetxtbox";
            this.TaskDatetxtbox.Size = new System.Drawing.Size(92, 20);
            this.TaskDatetxtbox.TabIndex = 5;
            this.TaskDatetxtbox.ValidatingType = typeof(System.DateTime);
            // 
            // TaskStatusCombobox
            // 
            this.TaskStatusCombobox.FormattingEnabled = true;
            this.TaskStatusCombobox.Location = new System.Drawing.Point(140, 103);
            this.TaskStatusCombobox.Name = "TaskStatusCombobox";
            this.TaskStatusCombobox.Size = new System.Drawing.Size(92, 21);
            this.TaskStatusCombobox.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(13, 137);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 23);
            this.label3.TabIndex = 7;
            this.label3.Text = "MSP User ID";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UserIdTextbox
            // 
            this.UserIdTextbox.Location = new System.Drawing.Point(140, 139);
            this.UserIdTextbox.MaxLength = 3;
            this.UserIdTextbox.Name = "UserIdTextbox";
            this.UserIdTextbox.Size = new System.Drawing.Size(101, 20);
            this.UserIdTextbox.TabIndex = 8;
            // 
            // LoanReviewCombobox
            // 
            this.LoanReviewCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LoanReviewCombobox.FormattingEnabled = true;
            this.LoanReviewCombobox.Location = new System.Drawing.Point(137, 171);
            this.LoanReviewCombobox.Name = "LoanReviewCombobox";
            this.LoanReviewCombobox.Size = new System.Drawing.Size(148, 21);
            this.LoanReviewCombobox.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(12, 171);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(118, 23);
            this.label4.TabIndex = 11;
            this.label4.Text = "Loan Review Complete";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_Task_DialogBox
            // 
            this.AcceptButton = this.btnProcess;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(315, 234);
            this.ControlBox = false;
            this.Controls.Add(this.LoanReviewCombobox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.UserIdTextbox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TaskStatusCombobox);
            this.Controls.Add(this.TaskDatetxtbox);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.MessageLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Task_DialogBox";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form_Task_DialogBox";
            this.Load += new System.EventHandler(this.Form_Task_DialogBox_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.MaskedTextBox TaskDatetxtbox;
        private System.Windows.Forms.ComboBox TaskStatusCombobox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox UserIdTextbox;
        private System.Windows.Forms.ComboBox LoanReviewCombobox;
        private System.Windows.Forms.Label label4;
    }
}