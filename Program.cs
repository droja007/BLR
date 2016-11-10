#region CODE_CHANGES
//
#endregion CODE_CHANGES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;


namespace LoanReview
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {

            // Get application GUID as defined in AssemblyInfo.cs.
            var appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value;

            // Unique id for global mutex - Global prefix means it is global to the machine.
            var mutexId = string.Format("Global\\{{{0}~{1}~{2}}}", Environment.UserDomainName, Environment.UserName, appGuid);

            // Need a place to store a return value in Mutex() constructor call.
            bool createdNew;

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
            {
                var hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(1000, false);
                        if (hasHandle == false)
                        {
                            try
                            {
                                throw new TimeoutException("Timeout waiting for exclusive access");
                            }
                            catch (TimeoutException)
                            {
                                var dialogResult = MessageBox.Show("Program is Already running...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                if (dialogResult == DialogResult.OK)
                                { Environment.Exit(1); }
                                //TODO - Fix formatting

                                //Thread.Sleep(2000);
                                //Environment.Exit(1);
                            }
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact that the mutex was abandoned in another process, it will
                        // still get acquired.
                        hasHandle = true;
                    }

                    System.Windows.Forms.Application.EnableVisualStyles();
                    System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);


                    //Form_Splash_Screen SplashScreen = new Form_Splash_Screen();
                    //SplashScreen.Show();
                    //Thread.Sleep(5000);
                    //SplashScreen.Close();


                    Form_User_Login formLogin = new Form_User_Login();

                    if (formLogin.ShowDialog() == DialogResult.OK)
                    {
                        if (formLogin.GetRole() == 4)
                        {
                            System.Windows.Forms.Application.Run(new Form_User_App_4(formLogin.GetActiveUser(), formLogin.GetActiveEntity()));
                        }
                        //else if (formLogin.GetRole() == 2)
                        //{
                        //    System.Windows.Forms.Application.Run(new Form_User_App_2(formLogin.GetActiveUser(), formLogin.GetActiveEntity()));
                        //}
                        //else if (formLogin.GetRole() == 3)
                        //{
                        //    System.Windows.Forms.Application.Run(new Form_User_App_3(formLogin.GetActiveUser(), formLogin.GetActiveEntity()));
                        //}
                        else
                        {
                            System.Windows.Forms.Application.Run(new Form_User_App(formLogin.GetActiveUser(), formLogin.GetActiveEntity()));
                        }
                    }
                }
                catch (Exception ex) 
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }
    }
}