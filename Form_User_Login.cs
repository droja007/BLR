using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoanReview
{
    public partial class Form_User_Login : Form
    {
        #region CONSTRUCTORS

        public Form_User_Login()
        {
            InitializeComponent();
        }

        #endregion CONSTRUCTORS

        #region PRIVATE_MEMBERS

        private vUser _LoginUser { get; set; }
        private LoanReviewEntities _LoginEntity { get; set; }
        private Logger logger;
        private System.Windows.Forms.Timer LoginTimer = new System.Windows.Forms.Timer();
        private Form_Progress_Loading objfrmShowProgress;
        #endregion PRIVATE_MEMBERS

        #region GUI_METHODS

        public vUser GetActiveUser()
        {
            return _LoginUser;
        }

        public LoanReviewEntities GetActiveEntity()
        {
            return _LoginEntity;
        }

        private bool TryCreateUser(string UserName, string UserPassword, string UserSalt, int UserRole)
        {
        RetryCreateUser:
            try
            {
                _LoginEntity.CreateUser(UserName, UserPassword, UserSalt, UserRole);

                ////AO takes New User's ID and re-initialized logger
                //var userQuery = (from user in _LoginEntity.vUsers where user.Name == UserName select user.Id).Single();
                //logger = new Logger(00000, (int)userQuery);

                return true;
            }
            catch (Exception ex)
            {
                string currentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;
                string error = ex.Message + ", This error came from : " + currentMethod;
                logger.TryWriteEntry(error + "\n" + ex.StackTrace);

                var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (dialogResult == System.Windows.Forms.DialogResult.Retry)
                    goto RetryCreateUser;

                return false;
            }
        }

        private bool DoesUserExist(string userName)
        {
            try
            {
                var userQuery = from user in _LoginEntity.vUsers.AsNoTracking() where user.Name == userName select user;

                if (userQuery.Count() != 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool IsUserLoggedIn(int userId)
        {
            try
            {
                var userQuery = (from user in _LoginEntity.vUsers.AsNoTracking() where user.Id == userId select new { user.IsActive }).Single();

                if (userQuery.IsActive == true)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool DoesRoleMatch(int userId, int userSelectedRole)
        {
            try
            {
                //MA: Retrieves the User Role when they created an account.
                var userQuery = (from user in _LoginEntity.vUsers.AsNoTracking() where user.Id == userId select user).Single();

                int userPermissionRole = Convert.ToInt32(userQuery.PermissionRole);
                int DepartmentID = Convert.ToInt32(userQuery.Department);
                //MA: Checks if the Registered Role is equal or less than the Role they signed in with.
                if (userSelectedRole <= userPermissionRole)
                {
                    //MA: Prevents specific department team from accessing Tier 3.
                    if (userSelectedRole == 3 && DepartmentID == 2)
                    {
                        return false;
                    }
                    else
                    {
                        _LoginUser.ActiveRole = userSelectedRole;
                        _LoginUser.PermissionRole = userPermissionRole;
                        return true;

                    }
                }
                else
                {
                    CreateUserHistoryEntry(4);
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool DoesPasswordMatch(string passwordHash, int userId)
        {
            try
            {
                var userQuery = (from user in _LoginEntity.vUsers.AsNoTracking() where user.Id == userId select new { user.Password }).Single();

                if (passwordHash == userQuery.Password)
                    return true;
                else
                {
                    CreateUserHistoryEntry(3);
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void UpdateUserFailedLogins(int userId)
        {
            _LoginEntity.UpdateFailedLogin(_LoginUser.Id);
        }

        private void CreateUserHistoryEntry(int actionId)
        {
            _LoginEntity.CreateHistoryEvent(_LoginUser.Id, null, _LoginUser.ActiveRole, actionId, 1);
        }

        private bool TryLoginUser(int userId)
        {
        RetryLoginUser:
            try
            {
                _LoginEntity.LoginUser(userId);

                CreateUserHistoryEntry(1);

                return true;
            }
            catch (Exception ex)
            {
                CreateUserHistoryEntry(2);

                var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (dialogResult == System.Windows.Forms.DialogResult.Retry)
                    goto RetryLoginUser;

                return false;
            }
        }

        private bool CheckBlankControls()
        {
            try
            {
                var emptyControlNames = new List<string>();

                foreach (Control c in groupBox_Login.Controls)
                {
                    if (c is TextBox)
                    {
                        TextBox textbox = c as TextBox;
                        if (textbox.Text == string.Empty)
                        {
                            // Text box is empty
                            emptyControlNames.Add(c.Name);
                        }
                    }
                }

                if (emptyControlNames.Count == 0)
                    return true;
                else
                {
                    StringBuilder sb = new StringBuilder(emptyControlNames.Count);

                    emptyControlNames.Reverse();

                    foreach (var controlName in emptyControlNames)
                    {
                        sb.Append(controlName.Split('_')[2] + ",");
                    }

                    MessageBox.Show("Please fill in " + sb.ToString().TrimEnd(','), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GenerateHash(string Password, string Salt)
        {
            SHA256 _sha256 = SHA256Managed.Create();

            byte[] password;
            password = Encoding.ASCII.GetBytes(Password + Salt);

            string hashValue;
            hashValue = BitConverter.ToString(SHA256.Create().ComputeHash(password)).Replace("-", "").ToLower();

            return hashValue;
        }

        private string GenerateSalt()
        {
            RNGCryptoServiceProvider crypRng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[12];
            crypRng.GetBytes(buff);

            return Convert.ToBase64String(buff);
        }

        private string GetUserSalt(int UserId)
        {
            var saltQuery = (from user in _LoginEntity.vUsers.AsNoTracking() where user.Id == UserId select new { user.Salt }).Single();

            return saltQuery.Salt;
        }

        private bool TryGenerateUser(bool CreateNewUser)
        {
            try
            {
                _LoginUser = new vUser();

                _LoginUser.Name = textBox_User_UserName.Text;
                //_LoginUser.PermissionRole = (int)comboBox_User_Role.SelectedItem;

                if (!CreateNewUser)
                {
                    // Retreive user id from table.
                    var userQuery = (from user in _LoginEntity.vUsers.AsNoTracking() where user.Name == _LoginUser.Name select new { user.Id }).Single();
                    _LoginUser.Id = userQuery.Id;

                    _LoginUser.Salt = GetUserSalt(_LoginUser.Id);
                }
                else
                    _LoginUser.Salt = GenerateSalt();

                _LoginUser.Password = GenerateHash(textBox_User_Password.Text, _LoginUser.Salt);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Login Credentials. Make sure your username and password are correct.", "Invalid Login Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool TryPopulateUserRoles()
        {
        RetryPopulateComboBox:
            try
            {
                var query = from roles in _LoginEntity.vRoles.AsNoTracking() select roles.Id;

                foreach (var role in query)
                {
                    comboBox_User_Role.Items.Add(role);
                }

                comboBox_User_Role.SelectedIndex = 0;

                return true;
            }
            catch (Exception ex)
            {
                var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (dialogResult == DialogResult.Retry)
                    goto RetryPopulateComboBox;
                return false;
            }
        }

        #endregion GUI_METHODS

        #region GUI_EVENTS

        public int GetRole()
        {
            return _LoginUser.ActiveRole.Value;
        }

        //DR - pinghost returns true if server is pingable. returns false for anything else
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
                return false;
            }
            return pingable;
        }

        private void button_User_Login_Click(object sender, EventArgs e)
        {
            try
            {
                //MA: Ping the server
                if (!PingHost(ConfigurationManager.AppSettings["IP Address"]))
                {
                    MessageBox.Show("Database Connection is unavailable at the moment please call Erik Stevens.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (CheckBlankControls())
                {
                    if (TryGenerateUser(false))
                    {
                        if (DoesUserExist(_LoginUser.Name))
                        {
                            if (DoesPasswordMatch(_LoginUser.Password, _LoginUser.Id))
                            {
                                if (DoesRoleMatch(_LoginUser.Id, (int)comboBox_User_Role.SelectedItem))
                                {
                                    if (CheckExistingSession(_LoginUser.Id))
                                    {
                                        if (AppStatus())
                                        {
                                            if (TryLoginUser(_LoginUser.Id))
                                            {
                                                if (IsUserLoggedIn(_LoginUser.Id))
                                                {
                                                    _LoginEntity.UpdateActiveServer(_LoginUser.Id, Environment.MachineName);
                                                    _LoginEntity.UpdateSession(_LoginUser.Id, true);
                                                    _LoginEntity.UpdateActiveRole(_LoginUser.Id, _LoginUser.ActiveRole.Value);
                                                    var dialogResult = MessageBox.Show("Successfully Logged into TEST VERSION. Work done here is not saved and not posted to production.", "Logged Into TEST VERSION!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                                    if (dialogResult == System.Windows.Forms.DialogResult.OK)
                                                        this.DialogResult = System.Windows.Forms.DialogResult.OK;
                                                    else
                                                        Application.Exit();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var result = MessageBox.Show("The Application is currently down for Maintenance. Please try logging in at a later time.", "Application Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            Environment.Exit(0);
                                        }
                                    }
                                    else
                                    {
                                        var result = MessageBox.Show("You currently have a session open on another machine. Do you want to close that session?", "Invalid Credentials", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                                        if (result == DialogResult.OK)
                                        {
                                            _LoginEntity.UpdateActiveServer(_LoginUser.Id, "");
                                            _LoginEntity.UpdateSession(_LoginUser.Id, false);
                                            Timer("Logging off server", 20000);
                                        }
                                    }
                                }
                                else
                                {
                                    UpdateUserFailedLogins(_LoginUser.Id);
                                    //MessageBox.Show("Role is incorrect.", "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    MessageBox.Show("You do not have permission to access this role.", "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            else
                            {
                                UpdateUserFailedLogins(_LoginUser.Id);
                                MessageBox.Show("Invalid Login Credentials. Make sure your username and password are correct.", "Invalid Login Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                textBox_User_Password.Focus();
                                textBox_User_Password.Text = "";
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invalid Login Credentials. Make sure your username and password are correct.", "Invalid Login Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool AppStatus()
        {
            try
            {
                var AppStatus = (from App in _LoginEntity.Cloud_Application_Status.AsNoTracking() select new { App.DatabaseStatus }).Single();

                if (Convert.ToBoolean(AppStatus.DatabaseStatus))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private bool CheckExistingSession(int userId)
        {
            try
            {
                var Session = (from Users in _LoginEntity.vUsers.AsNoTracking()
                               where Users.Id == userId
                               select new { Users.IsActive }).Single();

                var ActiveServer = (from Users in _LoginEntity.vUsers.AsNoTracking()
                                    where Users.Id == userId
                                    select new { Users.ActiveServer }).Single();

                if (Session.IsActive == false && (ActiveServer.ActiveServer == "" | ActiveServer.ActiveServer == null))
                {
                    return true;
                }
                else if (ActiveServer.ActiveServer == Environment.MachineName)
                {
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void button_User_CreateNew_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckBlankControls())
                {
                    if (TryGenerateUser(true))
                    {
                        if (!DoesUserExist(_LoginUser.Name))
                        {
                            if (TryCreateUser(_LoginUser.Name, _LoginUser.Password, _LoginUser.Salt, comboBox_User_Role.SelectedIndex + 1))
                            {
                                // Create new user object with new information.
                                //TryGenerateUser(false);

                                //CreateUserHistoryEntry(5);
                                MessageBox.Show(string.Format("User '{0}' created!", _LoginUser.Name), "User created!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show(string.Format("User '{0}' already exists!", _LoginUser.Name), "User already exists!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form_Login_Load(object sender, EventArgs e)
        {
            if (!PingHost(ConfigurationManager.AppSettings["IP Address"]))
            {
                MessageBox.Show("Database Connection is unavailable at the moment please call Erik Stevens.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            else
            {
                _LoginEntity = new LoanReviewEntities();

                TryPopulateUserRoles();

                textBox_User_UserName.Text = System.Environment.UserDomainName + @"\" + System.Environment.UserName;

                textBox_User_Password.Select();
            }
        }

        //MA: Opens the Form Reset Password
        private void ChangePasswordLabel_Clicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                if (textBox_User_UserName.Text == "")
                {
                    MessageBox.Show(string.Format("Username cannot be empty."), "Invalid Username", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    Form_User_Login_Reset_Password frmResestPassword = new Form_User_Login_Reset_Password(_LoginEntity, textBox_User_UserName.Text.ToString(), "Reset Password");

                    this.SendToBack();
                    frmResestPassword.ShowDialog();
                    frmResestPassword.BringToFront();
                    frmResestPassword.Activate();
                    frmResestPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //MA: Opens the Form Reset Password with the Forgot password panel.
        private void ForgotPasswordLabel_Clicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                if (textBox_User_UserName.Text == "")
                {
                    MessageBox.Show(string.Format("Username cannot be empty."), "Invalid Username", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    Form_User_Login_Reset_Password frmResetPassword = new Form_User_Login_Reset_Password(_LoginEntity, textBox_User_UserName.Text.ToString(), "Forgot Password");
                    this.SendToBack();
                    frmResetPassword.ShowDialog();
                    frmResetPassword.BringToFront();
                    frmResetPassword.Activate();
                    frmResetPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Timer(string ActionName, int Time)
        {
            LoginTimer.Tick += new System.EventHandler(OnTimerEvent);
            LoginTimer.Interval = Time;
            LoginTimer.Start();

            Control control = new Control();
            foreach (var ctrl in this.Controls)
            {
                control = (Control)ctrl;
                control.Enabled = false;
            }

            StartProgress(ActionName);

        }

        private void OnTimerEvent(object sender, EventArgs e)
        {
            CloseProgress();

            Control control = new Control();
            foreach (var ctrl in this.Controls)
            {
                control = (Control)ctrl;
                control.Enabled = true;
            }
            button_User_Login.PerformClick();
            LoginTimer.Stop();

        }

        private void StartProgress(string strStatusText)
        {
            objfrmShowProgress = new Form_Progress_Loading();
            objfrmShowProgress.StartPosition = FormStartPosition.CenterParent;
            objfrmShowProgress.strStatus.Text = strStatusText;
            ShowProgress();
        }

        private void CloseProgress()
        {
            Thread.Sleep(200);
            objfrmShowProgress.Invoke((MethodInvoker)delegate() { objfrmShowProgress.Close(); });
        }

        private void ShowProgress()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    try
                    {
                        objfrmShowProgress.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    Thread thread = new Thread(ShowProgress);
                    thread.IsBackground = false;
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        #endregion GUI_EVENTS
    }
}