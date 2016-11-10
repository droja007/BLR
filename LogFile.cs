using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace LoanReviewAutomation
{
    class LogFile
    {
        //DR - returns a string for the error message
        public static string CreateErrorMessage(Exception serviceException, string identifiers)
        {
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                messageBuilder.AppendLine("-------------------------------------------------------------------------------------");
                messageBuilder.AppendLine("The Exception occurred at: " + DateTime.Now.ToString());

                messageBuilder.AppendLine("Exception :: " + serviceException.ToString());
                if (serviceException.InnerException != null)
                {
                    messageBuilder.AppendLine("InnerException :: " + serviceException.InnerException.ToString());
                }
                if (!identifiers.Equals("")) messageBuilder.AppendLine("Special Identifiers :: " + identifiers);
                messageBuilder.AppendLine("-------------------------------------------------------------------------------------");
                return messageBuilder.ToString();
            }
            catch
            {
                messageBuilder.AppendLine("Exception:: Unknown Exception. " + DateTime.Now.ToString());
                return messageBuilder.ToString();
            }

        }

        //DR - returns a string for message
        public static string CreateLogMessage(string message)
        {
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                messageBuilder.AppendLine("-------------------------------------------------------------------------------------");
                messageBuilder.AppendLine("The Message occurred at: " + DateTime.Now.ToString());

                messageBuilder.AppendLine("Message :: " + message);
                messageBuilder.AppendLine("-------------------------------------------------------------------------------------");
                return messageBuilder.ToString();
            }
            catch
            {
                messageBuilder.AppendLine("Exception:: Unknown Exception. " + DateTime.Now.ToString());
                return messageBuilder.ToString();
            }

        }

        //DR - writes to the Loan review logfile, creates the directory and logfile if they don't exist
        public static void LogFileWrite(string message, string destinationFilePath)
        {
            FileStream fileStream = null;
            StreamWriter streamWriter = null;
            try
            {
                //DR - gets the log's filepath
                string unique = DateTime.Now.ToString("MM-d-yyyy");
                string logFileName = "LogFile - " + unique + ".txt";
                string logFilePath = System.IO.Path.Combine(destinationFilePath, logFileName);


                DirectoryInfo logDirInfo = null;
                FileInfo logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists) logDirInfo.Create();

                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(message);
            }
            finally
            {
                if (streamWriter != null) streamWriter.Close();
                if (fileStream != null) fileStream.Close();
            }

        }
    }
}
