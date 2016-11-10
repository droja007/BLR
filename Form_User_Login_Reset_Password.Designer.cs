namespace LoanReview
{
    partial class Form_User_Login_Reset_Password
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
            this.panelResetPassword = new System.Windows.Forms.Panel();
            this.textBox_User_NewPassword2 = new System.Windows.Forms.TextBox();
            this.textBox_User_NewPassword = new System.Windows.Forms.TextBox();
            this.textBox_User_CurrentPassword = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panelTokenConfirmation = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.btnTokenProceed = new System.Windows.Forms.Button();
            this.panelResetPassword.SuspendLayout();
            this.panelTokenConfirmation.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelResetPassword
            // 
            this.panelResetPassword.AutoSize = true;
            this.panelResetPassword.Controls.Add(this.panelTokenConfirmation);
            this.panelResetPassword.Controls.Add(this.textBox_User_NewPassword2);
            this.panelResetPassword.Controls.Add(this.textBox_User_NewPassword);
            this.panelResetPassword.Controls.Add(this.textBox_User_CurrentPassword);
            this.panelResetPassword.Controls.Add(this.button1);
            this.panelResetPassword.Controls.Add(this.label3);
            this.panelResetPassword.Controls.Add(this.label2);
            this.panelResetPassword.Controls.Add(this.label1);
            this.panelResetPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelResetPassword.Location = new System.Drawing.Point(0, 0);
            this.panelResetPassword.Name = "panelResetPassword";
            this.panelResetPassword.Size = new System.Drawing.Size(294, 145);
            this.panelResetPassword.TabIndex = 0;
            // 
            // textBox_User_NewPassword2
            // 
            this.textBox_User_NewPassword2.Location = new System.Drawing.Point(145, 85);
            this.textBox_User_NewPassword2.Name = "textBox_User_NewPassword2";
            this.textBox_User_NewPassword2.Size = new System.Drawing.Size(136, 20);
            this.textBox_User_NewPassword2.TabIndex = 20;
            this.textBox_User_NewPassword2.UseSystemPasswordChar = true;
            // 
            // textBox_User_NewPassword
            // 
            this.textBox_User_NewPassword.Location = new System.Drawing.Point(145, 49);
            this.textBox_User_NewPassword.Name = "textBox_User_NewPassword";
            this.textBox_User_NewPassword.Size = new System.Drawing.Size(136, 20);
            this.textBox_User_NewPassword.TabIndex = 19;
            this.textBox_User_NewPassword.UseSystemPasswordChar = true;
            // 
            // textBox_User_CurrentPassword
            // 
            this.textBox_User_CurrentPassword.Location = new System.Drawing.Point(145, 13);
            this.textBox_User_CurrentPassword.Name = "textBox_User_CurrentPassword";
            this.textBox_User_CurrentPassword.Size = new System.Drawing.Size(136, 20);
            this.textBox_User_CurrentPassword.TabIndex = 18;
            this.textBox_User_CurrentPassword.UseSystemPasswordChar = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(206, 110);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 17;
            this.button1.Text = "Confirm";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Retype New Password";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "New Password";
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.Location = new System.Drawing.Point(13, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(269, 36);
            this.label1.TabIndex = 14;
            this.label1.Text = "Current Password";
            // 
            // panelTokenConfirmation
            // 
            this.panelTokenConfirmation.Controls.Add(this.btnTokenProceed);
            this.panelTokenConfirmation.Controls.Add(this.label4);
            this.panelTokenConfirmation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTokenConfirmation.Location = new System.Drawing.Point(0, 0);
            this.panelTokenConfirmation.Name = "panelTokenConfirmation";
            this.panelTokenConfirmation.Size = new System.Drawing.Size(294, 145);
            this.panelTokenConfirmation.TabIndex = 21;
            this.panelTokenConfirmation.Visible = false;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(26, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(244, 73);
            this.label4.TabIndex = 0;
            // 
            // btnTokenProceed
            // 
            this.btnTokenProceed.Location = new System.Drawing.Point(195, 110);
            this.btnTokenProceed.Name = "btnTokenProceed";
            this.btnTokenProceed.Size = new System.Drawing.Size(75, 23);
            this.btnTokenProceed.TabIndex = 1;
            this.btnTokenProceed.Text = "Proceed";
            this.btnTokenProceed.UseVisualStyleBackColor = true;
            this.btnTokenProceed.Click += new System.EventHandler(this.btnTokenProceed_Click);
            // 
            // Form_User_Login_Reset_Password
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(294, 145);
            this.Controls.Add(this.panelResetPassword);
            this.Name = "Form_User_Login_Reset_Password";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Reset Password";
            this.Load += new System.EventHandler(this.Form_User_Login_Reset_Password_Load);
            this.panelResetPassword.ResumeLayout(false);
            this.panelResetPassword.PerformLayout();
            this.panelTokenConfirmation.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelResetPassword;
        private System.Windows.Forms.Panel panelTokenConfirmation;
        private System.Windows.Forms.Button btnTokenProceed;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_User_NewPassword2;
        private System.Windows.Forms.TextBox textBox_User_NewPassword;
        private System.Windows.Forms.TextBox textBox_User_CurrentPassword;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;


    }
}