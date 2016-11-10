using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoanReview
{
    public partial class Form_User_Login_Reset_Password : Form
    {
        //MA -
        private vUser _AppUser = new vUser();

        //MA -
        private LoanReviewEntities _LoginEntity { get; set; }

        //MA -
        private string Username;

        //MA -
        private string Hash;

        //MA -
        private string NewHash;

        //MA -
        private string Action;

        //MA -
        private Logger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityLoginObject"></param>
        /// <param name="UserName"></param>
        /// <param name="Option"></param>
        public Form_User_Login_Reset_Password(LoanReviewEntities entityLoginObject, string UserName, string Option)
        {            
            //MA -
            _LoginEntity = entityLoginObject;

            //MA -
            Username = UserName.ToString();

            //MA -
            Action = Option;
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        private void LoadUser(string userName)
        {
            //MA -
            var User = (from Users in _LoginEntity.vUsers
                        where Users.Name == userName
                        select Users).Single();
            //MA -
            _AppUser.Id = Convert.ToInt32(User.Id);
            _AppUser.Name = User.Name;
            _AppUser.Password = textBox_User_CurrentPassword.Text;
            _AppUser.Salt = GetUserSalt(_AppUser.Id);

            //MA -
            Hash = GenerateHash(_AppUser.Password, _AppUser.Salt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private bool DoesUserExist(string userName)
        {
            //MA -
            var userQuery = from user in _LoginEntity.vUsers where user.Name == userName select user;

            //MA -
            if (userQuery.Count() != 0)
            {
                LoadUser(userName);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Password"></param>
        /// <param name="Salt"></param>
        /// <returns></returns>
        private string GenerateHash(string Password, string Salt)
        {
            //MA -
            SHA256 _sha256 = SHA256Managed.Create();

            //MA -
            byte[] password;
            password = Encoding.ASCII.GetBytes(Password + Salt);

            //MA -
            string hashValue;
            hashValue = BitConverter.ToString(SHA256.Create().ComputeHash(password)).Replace("-", "").ToLower();

            return hashValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        private string GetUserSalt(int UserId)
        {
            //MA -
            var saltQuery = (from user in _LoginEntity.vUsers where user.Id == UserId select new { user.Salt }).Single();

            return saltQuery.Salt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="passwordHash"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private bool DoesOldPasswordMatch(string passwordHash, int userId)
        {
            //MA -
            var userQuery = (from user in _LoginEntity.vUsers where user.Id == userId select new { user.Password }).Single();

            //MA -
            if (passwordHash == userQuery.Password)
                return true;
            else
            {
                //CreateUserHistoryEntry(3);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Password1"></param>
        /// <param name="Password2"></param>
        /// <returns></returns>
        private bool DoesNewPasswordMatch(string Password1, string Password2)
        {
            //MA -
            if (Password1.Equals(Password2))
            {
                GenerateHash(Password1, _AppUser.Salt);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GenerateSalt()
        {
            //MA -
            RNGCryptoServiceProvider crypRng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[12];
            crypRng.GetBytes(buff);

            return Convert.ToBase64String(buff);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GenerateToken()
        {
            //MA -
            RNGCryptoServiceProvider crypRng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[6];
            crypRng.GetBytes(buff);

            return Convert.ToBase64String(buff);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Password"></param>
        /// <returns></returns>
        private bool ConfirmPasswordChange(string Password)
        {
            //MA -
            _AppUser.Salt = GenerateSalt();
            _AppUser.Password = GenerateHash(Password, _AppUser.Salt);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (DoesUserExist(Username))
            {
                if (DoesOldPasswordMatch(Hash, _AppUser.Id) == true)
                {
                    if (DoesNewPasswordMatch(textBox_User_NewPassword.Text, textBox_User_NewPassword2.Text) == true)
                    {
                        if (ConfirmPasswordChange(textBox_User_NewPassword.Text) == true)
                        {
                            _LoginEntity.UpdatePassword(_AppUser.Id, _AppUser.Password, _AppUser.Salt, 0);
                            MessageBox.Show(string.Format("You have successfully change your password."), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show(string.Format("New Passwords do not match."), "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Current Password does not match."), "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(string.Format("User '{0}' does not exist!", Username), "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_User_Login_Reset_Password_Load(object sender, EventArgs e)
        {
            //MA -
            textBox_User_CurrentPassword.UseSystemPasswordChar = true;
            textBox_User_NewPassword.UseSystemPasswordChar = true;
            textBox_User_NewPassword2.UseSystemPasswordChar = true;

            //MA -
            if (Action == "Forgot Password")
            {
                label4.Text = "A temporary password will be sent to your email. Are you sure you would like to continue ?";
                panelTokenConfirmation.Show();
                this.StartPosition = FormStartPosition.CenterParent;
            }
            else if (Action == "Reset Password")
            {
                panelResetPassword.Show();
                this.StartPosition = FormStartPosition.CenterParent;
            }
            else
            {
                label1.Text = "Temporary Password :";
                this.panelResetPassword.Show();
                this.Refresh();
                //panelResetPassword.Show();
                this.StartPosition = FormStartPosition.CenterParent;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool sendtoken()
        {
            //MA -
            bool reset;
            _AppUser.Name = Username;
            _AppUser.Salt = GenerateSalt();
            string token = GenerateToken();
            _AppUser.Password = GenerateHash(token, _AppUser.Salt);

            //MA -
            EmailMessage emailmessage;

            //MA -
            var email = (from Users in _LoginEntity.vUsers
                         where Users.Name == Username
                         select Users.Email).Single();

            try
            {
                //MA -
                emailmessage = new EmailMessage(email, "Temporary Password", "Hello  you have requested a Temporary password. Please reset your password, once you have logged in. Temporary Password is " + token.ToString() + "\n" + "**THIS IS AN AUTOMATED MESSAGE. PLEASE DO NOT RESPOND TO THIS EMAIL.**");
                _LoginEntity.UpdatePassword(_AppUser.Id, _AppUser.Password, _AppUser.Salt, 1);
                reset = true;
                emailmessage = null;
            }
            catch (Exception e)
            {
                emailmessage = null;
                reset = false;
            }

            return reset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTokenProceed_Click(object sender, EventArgs e)
        {
            //MA -
            if (DoesUserExist(Username))
            {
                if (sendtoken() == true)
                {
                    this.panelTokenConfirmation.Hide();
                    //this.Close();
                    MessageBox.Show(string.Format("Your temporary password has been emailed."), "Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    label1.Text = "Temporary Password :";
                    this.panelResetPassword.Show();
                    this.Refresh();
                }
                else
                {
                    MessageBox.Show(string.Format("An error has occurred and your password has not been emailed."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show(string.Format("User '{0}' does not exist!", Username), "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}