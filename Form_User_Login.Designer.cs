namespace LoanReview
{
    partial class Form_User_Login
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_User_Login));
            this.button_User_CreateNew = new System.Windows.Forms.Button();
            this.groupBox_Login = new System.Windows.Forms.GroupBox();
            this.linkLabelForgotPassword = new System.Windows.Forms.LinkLabel();
            this.linkLabelChangePassword = new System.Windows.Forms.LinkLabel();
            this.label_User_Role = new System.Windows.Forms.Label();
            this.comboBox_User_Role = new System.Windows.Forms.ComboBox();
            this.textBox_User_Password = new System.Windows.Forms.TextBox();
            this.label_User_UserName = new System.Windows.Forms.Label();
            this.label_User_Password = new System.Windows.Forms.Label();
            this.textBox_User_UserName = new System.Windows.Forms.TextBox();
            this.button_User_Login = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox_Login.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // button_User_CreateNew
            // 
            this.button_User_CreateNew.Location = new System.Drawing.Point(246, 68);
            this.button_User_CreateNew.Name = "button_User_CreateNew";
            this.button_User_CreateNew.Size = new System.Drawing.Size(75, 23);
            this.button_User_CreateNew.TabIndex = 4;
            this.button_User_CreateNew.Text = "Create User";
            this.button_User_CreateNew.UseVisualStyleBackColor = true;
            this.button_User_CreateNew.Visible = false;
            this.button_User_CreateNew.Click += new System.EventHandler(this.button_User_CreateNew_Click);
            // 
            // groupBox_Login
            // 
            this.groupBox_Login.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox_Login.Controls.Add(this.linkLabelForgotPassword);
            this.groupBox_Login.Controls.Add(this.linkLabelChangePassword);
            this.groupBox_Login.Controls.Add(this.label_User_Role);
            this.groupBox_Login.Controls.Add(this.comboBox_User_Role);
            this.groupBox_Login.Controls.Add(this.textBox_User_Password);
            this.groupBox_Login.Controls.Add(this.label_User_UserName);
            this.groupBox_Login.Controls.Add(this.label_User_Password);
            this.groupBox_Login.Controls.Add(this.textBox_User_UserName);
            this.groupBox_Login.Controls.Add(this.button_User_Login);
            this.groupBox_Login.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.groupBox_Login.Location = new System.Drawing.Point(12, 97);
            this.groupBox_Login.Name = "groupBox_Login";
            this.groupBox_Login.Size = new System.Drawing.Size(309, 164);
            this.groupBox_Login.TabIndex = 6;
            this.groupBox_Login.TabStop = false;
            this.groupBox_Login.Text = "Login";
            // 
            // linkLabelForgotPassword
            // 
            this.linkLabelForgotPassword.AutoSize = true;
            this.linkLabelForgotPassword.Location = new System.Drawing.Point(187, 89);
            this.linkLabelForgotPassword.Name = "linkLabelForgotPassword";
            this.linkLabelForgotPassword.Size = new System.Drawing.Size(86, 13);
            this.linkLabelForgotPassword.TabIndex = 17;
            this.linkLabelForgotPassword.TabStop = true;
            this.linkLabelForgotPassword.Text = "Forgot Password";
            this.linkLabelForgotPassword.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ForgotPasswordLabel_Clicked);
            // 
            // linkLabelChangePassword
            // 
            this.linkLabelChangePassword.Location = new System.Drawing.Point(70, 89);
            this.linkLabelChangePassword.Name = "linkLabelChangePassword";
            this.linkLabelChangePassword.Size = new System.Drawing.Size(95, 18);
            this.linkLabelChangePassword.TabIndex = 16;
            this.linkLabelChangePassword.TabStop = true;
            this.linkLabelChangePassword.Text = "Change Password";
            this.linkLabelChangePassword.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChangePasswordLabel_Clicked);
            // 
            // label_User_Role
            // 
            this.label_User_Role.AutoSize = true;
            this.label_User_Role.Location = new System.Drawing.Point(38, 135);
            this.label_User_Role.Name = "label_User_Role";
            this.label_User_Role.Size = new System.Drawing.Size(29, 13);
            this.label_User_Role.TabIndex = 12;
            this.label_User_Role.Text = "Role";
            // 
            // comboBox_User_Role
            // 
            this.comboBox_User_Role.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_User_Role.FormattingEnabled = true;
            this.comboBox_User_Role.Location = new System.Drawing.Point(73, 131);
            this.comboBox_User_Role.Name = "comboBox_User_Role";
            this.comboBox_User_Role.Size = new System.Drawing.Size(75, 21);
            this.comboBox_User_Role.TabIndex = 9;
            // 
            // textBox_User_Password
            // 
            this.textBox_User_Password.Location = new System.Drawing.Point(73, 66);
            this.textBox_User_Password.MaxLength = 256;
            this.textBox_User_Password.Name = "textBox_User_Password";
            this.textBox_User_Password.Size = new System.Drawing.Size(200, 20);
            this.textBox_User_Password.TabIndex = 7;
            this.textBox_User_Password.UseSystemPasswordChar = true;
            // 
            // label_User_UserName
            // 
            this.label_User_UserName.AutoSize = true;
            this.label_User_UserName.Location = new System.Drawing.Point(12, 33);
            this.label_User_UserName.Name = "label_User_UserName";
            this.label_User_UserName.Size = new System.Drawing.Size(55, 13);
            this.label_User_UserName.TabIndex = 8;
            this.label_User_UserName.Text = "Username";
            // 
            // label_User_Password
            // 
            this.label_User_Password.AutoSize = true;
            this.label_User_Password.Location = new System.Drawing.Point(14, 70);
            this.label_User_Password.Name = "label_User_Password";
            this.label_User_Password.Size = new System.Drawing.Size(53, 13);
            this.label_User_Password.TabIndex = 10;
            this.label_User_Password.Text = "Password";
            // 
            // textBox_User_UserName
            // 
            this.textBox_User_UserName.Location = new System.Drawing.Point(73, 29);
            this.textBox_User_UserName.MaxLength = 128;
            this.textBox_User_UserName.Name = "textBox_User_UserName";
            this.textBox_User_UserName.Size = new System.Drawing.Size(200, 20);
            this.textBox_User_UserName.TabIndex = 6;
            // 
            // button_User_Login
            // 
            this.button_User_Login.Location = new System.Drawing.Point(198, 131);
            this.button_User_Login.Name = "button_User_Login";
            this.button_User_Login.Size = new System.Drawing.Size(75, 21);
            this.button_User_Login.TabIndex = 11;
            this.button_User_Login.Text = "Login";
            this.button_User_Login.UseVisualStyleBackColor = true;
            this.button_User_Login.Click += new System.EventHandler(this.button_User_Login_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(194, 79);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // Form_User_Login
            // 
            this.AcceptButton = this.button_User_Login;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(335, 275);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button_User_CreateNew);
            this.Controls.Add(this.groupBox_Login);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_User_Login";
            this.Padding = new System.Windows.Forms.Padding(15, 0, 15, 15);
            this.Text = "Loan Review TEST VERSION";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form_Login_Load);
            this.groupBox_Login.ResumeLayout(false);
            this.groupBox_Login.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_User_CreateNew;
        private System.Windows.Forms.GroupBox groupBox_Login;
        private System.Windows.Forms.Label label_User_Role;
        private System.Windows.Forms.ComboBox comboBox_User_Role;
        private System.Windows.Forms.TextBox textBox_User_Password;
        private System.Windows.Forms.Label label_User_UserName;
        private System.Windows.Forms.Label label_User_Password;
        private System.Windows.Forms.TextBox textBox_User_UserName;
        private System.Windows.Forms.Button button_User_Login;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.LinkLabel linkLabelForgotPassword;
        private System.Windows.Forms.LinkLabel linkLabelChangePassword;
    }
}

