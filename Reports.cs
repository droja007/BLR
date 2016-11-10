using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LoanReviewAutomation
{
    class Reports
    {
        public void ReportTasksToOpen(string email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            LDGEntities LDG = new LDGEntities();

            //DR - grabs us documents from workflowtracking table where tasks where open on today's date
            var ivtTasks = from IVT in ImagingApp.vWorkflowTracking_Loandata.AsNoTracking() where IVT.TaskStatus == 1 
                               && IVT.DateOpened.Month == date.Month
                           && IVT.DateOpened.Day == date.Day
                           && IVT.DateOpened.Year == date.Year
                           select IVT;

            //DR - gives us report headers
            string report = "Tasks To Open\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Task ID,Comment");

            //DR - contains loannum for each loanid
            Dictionary<int, string> DictionaryLoanNum = new Dictionary<int, string>();

            //DR -adds the loanNum to the dictionary 
            foreach (var document in ivtTasks)
            {
                var LN = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == document.LoanID select l.LoanNumber).Single();
                string loanNum = LN.ToString();

                if (!DictionaryLoanNum.ContainsKey(document.LoanID))
                {
                    DictionaryLoanNum[document.LoanID] = loanNum;
                }

            }

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (KeyValuePair<int, string> loan in DictionaryLoanNum)
            {
                //var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loanID.Key select l).Single();

                int commentCount = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Count();

                //DR - if there's no entry in IVTComments it means T2 submit didn't execute properly and the loan will go back into queue.
                if (commentCount == 0)
                {
                    continue;
                }

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loan.Value + "\nTask ID: " + "IMAVAL\n\nComments:\n\n";


                string comment = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Single();

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL,");
                }

                count++;
            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "LoanReview: Tasks To Open" + date.ToString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Tasks To Open", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportTasksToOpen(string[] email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            LDGEntities LDG = new LDGEntities();

            //DR - grabs us documents from workflowtracking table where tasks where open on today's date
            var ivtTasks = from IVT in ImagingApp.vWorkflowTracking_Loandata.AsNoTracking()
                           where IVT.TaskStatus == 1
                               && IVT.DateOpened.Month == date.Month
                               && IVT.DateOpened.Day == date.Day
                               && IVT.DateOpened.Year == date.Year
                           select IVT;

            //DR - gives us report headers
            string report = "Tasks To Open\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Task ID,Comment");

            //DR - contains loannum for each loanid
            Dictionary<int, string> DictionaryLoanNum = new Dictionary<int, string>();

            //DR -adds the loanNum to the dictionary 
            foreach (var document in ivtTasks)
            {
                var LN = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == document.LoanID select l.LoanNumber).Single();
                string loanNum = LN.ToString();

                if (!DictionaryLoanNum.ContainsKey(document.LoanID))
                {
                    DictionaryLoanNum[document.LoanID] = loanNum;
                }

            }

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (KeyValuePair<int, string> loan in DictionaryLoanNum)
            {
                //var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loanID.Key select l).Single();

                int commentCount = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Count();

                //DR - if there's no entry in IVTComments it means T2 submit didn't execute properly and the loan will go back into queue.
                if (commentCount == 0)
                {
                    continue;
                }

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loan.Value + "\nTask ID: " + "IMAVAL\n\nComments:\n\n";



                string comment = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Single();

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL,");
                }


                count++;
            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "LoanReview: Tasks To Open" + date.ToString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Tasks To Open", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportTasksThatDidNotOpen(string email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            LDGEntities LDG = new LDGEntities();

            //DR - grabs us documents from workflowtracking table where tasks where task status is 1 and dateopened is not equal to today
            var ivtTasks = from IVT in ImagingApp.vWorkflowTracking_Loandata.AsNoTracking() where IVT.TaskStatus == 1 && IVT.DateOpened != DateTime.Today select IVT;

            //DR - gives us report headers
            string report = "Tasks that did not open\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Task ID,Comment");

            //DR - contains loannum for each loanid
            Dictionary<int, string> DictionaryLoanNum = new Dictionary<int, string>();

            //DR -adds the loanNum to the dictionary 
            foreach (var document in ivtTasks)
            {
                var LN = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == document.LoanID select l.LoanNumber).Single();
                string loanNum = LN.ToString();
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');
                var taskTracker = (from t in LDG.LDG_TASK_TRACKING.AsNoTracking() where t.LN_NO == paddedLoanNumber && t.TSK_ID == "IMAVAL" select t);

                if (taskTracker.Count() > 0)
                {
                    continue;
                }

                if (!DictionaryLoanNum.ContainsKey(document.LoanID))
                {
                    DictionaryLoanNum[document.LoanID] = loanNum;
                }

            }

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (KeyValuePair<int, string> loan in DictionaryLoanNum)
            {
                //var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loanID.Key select l).Single();

                int commentCount = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Count();

                //DR - if there's no entry in IVTComments it means T2 submit didn't execute properly and the loan will go back into queue.
                if (commentCount == 0)
                {
                    continue;
                }

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loan.Value + "\nTask ID: " + "IMAVAL\n\nComments:\n\n";

                string comment = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Single();

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL,");
                }


                count++;
            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "LoanReview: Tasks That Did Not Open" + DateTime.Today.ToString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Tasks That Did Not Open", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportTasksThatDidNotOpen(string[] email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            LDGEntities LDG = new LDGEntities();

            //DR - grabs us documents from workflowtracking table where tasks where task status is 1 and dateopened is not equal to today
            var ivtTasks = from IVT in ImagingApp.vWorkflowTracking_Loandata.AsNoTracking() where IVT.TaskStatus == 1 && IVT.DateOpened != DateTime.Today select IVT;

            //DR - gives us report headers
            string report = "Tasks that did not open\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Task ID,Comment");

            //DR - contains loannum for each loanid
            Dictionary<int, string> DictionaryLoanNum = new Dictionary<int, string>();

            //DR -adds the loanNum to the dictionary 
            foreach (var document in ivtTasks)
            {
                var LN = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == document.LoanID select l.LoanNumber).Single();
                string loanNum = LN.ToString();
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');
                var taskTracker = (from t in LDG.LDG_TASK_TRACKING.AsNoTracking() where t.LN_NO == paddedLoanNumber && t.TSK_ID == "IMAVAL" select t);

                if (taskTracker.Count() > 0)
                {
                    continue;
                }

                if (!DictionaryLoanNum.ContainsKey(document.LoanID))
                {
                    DictionaryLoanNum[document.LoanID] = loanNum;
                }

            }

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (KeyValuePair<int, string> loan in DictionaryLoanNum)
            {
                //var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loanID.Key select l).Single();

                int commentCount = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Count();

                //DR - if there's no entry in IVTComments it means T2 submit didn't execute properly and the loan will go back into queue.
                if (commentCount == 0)
                {
                    continue;
                }

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loan.Value + "\nTask ID: " + "IMAVAL\n\nComments:\n\n";

                string comment = (from c in ImagingApp.IVTComments.AsNoTracking() where c.LoanID == loan.Key select c.Comment).Single();

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loan.Value + ",IMAVAL,");
                }


                count++;
            }

            report = report + "----------------------------\n";


            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "LoanReview: Tasks That Did Not Open" + DateTime.Today.ToString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Tasks That Did Not Open", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportManualReviewLoans(string email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            //DR - grabs us the manual review loans from the ManualReviewLoans table
            var loans = from ml in ImagingApp.ManualReviewLoans.AsNoTracking() select ml;

            //DR - gives us report headers
            string report = "Loans to Manually Review\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Comment");

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (var loan in loans)
            {
                var loanNum = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loan.LoanID select l.LoanNumber).Single();

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum + "\nComments:\n\n";

                string comment = loan.Comment;

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loanNum + "," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loanNum + ",");
                }


                count++;
            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "LoanReview: Loans to Manually Review" + DateTime.Today.ToString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Loans to Manually Review", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportManualReviewLoans(string[] email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            //DR - grabs us the manual review loans from the ManualReviewLoans table
            var loans = from ml in ImagingApp.ManualReviewLoans.AsNoTracking() select ml;

            //DR - gives us report headers
            string report = "Loans to Manually Review\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine("Loan Number,Comment");

            int count = 1;
            Console.WriteLine("Building Report");
            //DR - adds the comments to the report
            foreach (var loan in loans)
            {
                var loanNum = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == loan.LoanID select l.LoanNumber).Single();

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum + "\nComments:\n\n";

                string comment = loan.Comment;

                if (comment != null)
                {
                    report = report + comment + "\n";
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loanNum + "," + comment);
                }
                else
                {
                    //writer.WriteLine("Loan Number,Task ID,Comment");
                    writer.WriteLine(loanNum + ",");
                }


                count++;
            }

            report = report + "----------------------------\n";


            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "LoanReview: Loans to Manually Review" + DateTime.Today.ToString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Loans to Manually Review", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportFieldExceptions(string email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.MSPUpdated == false && UI.UserTimeStamp < DateTime.Today
                             select UI;

            //DR - gives us report headers
            string report = "Fields that weren't updated in MSP\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            //writer.WriteLine("Loan Number,Field Name,Field Description,Workstation,MSP Value,Value to Update With");
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,MSP Workstation,,"
            + @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");

            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //DR - get the field value from loandata
                var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);

                string StringLoanDataValue;

                if (LoanDataValue == null)
                {
                    StringLoanDataValue = "";
                }
                else
                {
                    StringLoanDataValue = LoanDataValue.ToString();
                }

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();

                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if (ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();

                //DR - stores value after change
                string newValue;

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";

                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2
                                select u).Single();

                string tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                   where u.LoanID == field.LoanID && u.DocID == field.DocID
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3
                                   select u.Value).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking()
                                    where u.LoanID == field.LoanID && u.DocID == field.DocID
                                        && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4
                                    select u;

                if (tier1val.Value != tier2val)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if (error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    if (tier2val != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;
                
                var DataTranslation = (from DT in ImagingApp.vDataTranslations.AsNoTracking()
                                       where DT.DocID == field.DocID &&
                                       DT.FieldID == field.FieldID
                                       select DT);

                //DR - if the field has any DataTranslation, it's value may need to be translated
                if (DataTranslation.Count() > 0)
                {
                    foreach (var DT in DataTranslation)
                    {
                        //DR - if fieldvalue matches pretranslation, translate it into its actual MSPvalue
                        if (DT.PostTranslation.Trim().Equals(newValue.Trim()))
                        {
                            newValue = DT.PreTranslation;
                        }
                    }
                }


                //DR - update MSPUpdated to 1 if field value matches msp value
                if (CompareData(newValue, vField.Client_Data_Type, StringLoanDataValue))
                {
                    ImagingApp.UserInputMSPUpdated(field.LoanID, field.UserID, field.DocID, field.FieldID, field.InputID, field.Workflow, 1);
                }
                else //DR - add to report if the field value doesn't match msp
                {
                    writer.WriteLine(loanNum + "," + dealID + "," + productType + "," + reviewDate + "," + fieldChanged + "," + StringLoanDataValue + "," + newValue
                    + ","+vField.MspWorkstation+",," + taskAssigned + "," + docNameFromPPV + ",," + auditStatus + "," + error + "," + commentTier2Column + ",," + QCReviewerID + "," + QCReviewDate + "," + QCOppIdentified + "," + QCCorrectedValue + "," + commentTier3Column);

                    report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                        + " \nField Name: " + vField.Name
                        + " \nField Description: " + vField.Description
                        + " \nWorkstation: " + vField.MspWorkstation
                        + " \nMSP Value: " + StringLoanDataValue
                        + " \nValue to Update with: " + newValue
                        + " \n";

                    count++;
                }

            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "Field Exception Report " + DateTime.Today.ToString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Field Exception Report", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportFieldExceptions(string email, DateTime ignoreDate)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - set the timestamp to have 8pm as an hour so we can truely ignore user inputs from the date passed in
            TimeSpan ts = new TimeSpan(20, 0, 0);
            ignoreDate = ignoreDate.Date + ts;

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.MSPUpdated == false && UI.UserTimeStamp < DateTime.Today
                             && UI.UserTimeStamp > ignoreDate
                             select UI;

            //DR - gives us report headers
            string report = "Fields that weren't updated in MSP\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            //writer.WriteLine("Loan Number,Field Name,Field Description,Workstation,MSP Value,Value to Update With");
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,MSP Workstation,,"
            + @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");

            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //DR - get the field value from loandata
                var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);

                string StringLoanDataValue;

                if (LoanDataValue == null)
                {
                    StringLoanDataValue = "";
                }
                else
                {
                    StringLoanDataValue = LoanDataValue.ToString();
                }

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();

                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if (ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();

                //DR - stores value after change
                string newValue;

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";

                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2
                                select u).Single();

                string tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                   where u.LoanID == field.LoanID && u.DocID == field.DocID
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3
                                   select u.Value).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking()
                                    where u.LoanID == field.LoanID && u.DocID == field.DocID
                                        && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4
                                    select u;

                if (tier1val.Value != tier2val)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if (error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    if (tier2val != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;

                var DataTranslation = (from DT in ImagingApp.vDataTranslations.AsNoTracking()
                                       where DT.DocID == field.DocID &&
                                       DT.FieldID == field.FieldID
                                       select DT);

                //DR - if the field has any DataTranslation, it's value may need to be translated
                if (DataTranslation.Count() > 0)
                {
                    foreach (var DT in DataTranslation)
                    {
                        //DR - if fieldvalue matches pretranslation, translate it into its actual MSPvalue
                        if (DT.PostTranslation.Trim().Equals(newValue.Trim()))
                        {
                            newValue = DT.PreTranslation;
                        }
                    }
                }

                //DR - update MSPUpdated to 1 if field value matches msp value
                if (CompareData(newValue, vField.Client_Data_Type, StringLoanDataValue))
                {
                    ImagingApp.UserInputMSPUpdated(field.LoanID, field.UserID, field.DocID, field.FieldID, field.InputID, field.Workflow, 1);
                }
                else //DR - add to report if the field value doesn't match msp
                {
                    writer.WriteLine(loanNum + "," + dealID + "," + productType + "," + reviewDate + "," + fieldChanged + "," + StringLoanDataValue + "," + newValue
                    + "," + vField.MspWorkstation + ",," + taskAssigned + "," + docNameFromPPV + ",," + auditStatus + "," + error + "," + commentTier2Column + ",," + QCReviewerID + "," + QCReviewDate + "," + QCOppIdentified + "," + QCCorrectedValue + "," + commentTier3Column);

                    report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                        + " \nField Name: " + vField.Name
                        + " \nField Description: " + vField.Description
                        + " \nWorkstation: " + vField.MspWorkstation
                        + " \nMSP Value: " + StringLoanDataValue
                        + " \nValue to Update with: " + newValue
                        + " \n";

                    count++;
                }

            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "Field Exception Report " + DateTime.Today.ToString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Field Exception Report", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportFieldExceptions(string[] email)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.MSPUpdated == false && UI.UserTimeStamp < DateTime.Today
                             select UI;

            //DR - gives us report headers
            string report = "Fields that weren't updated in MSP\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            //writer.WriteLine("Loan Number,Field Name,Field Description,Workstation,MSP Value,Value to Update With");
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,MSP Workstation,,"
            + @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");

            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //DR - get the field value from loandata
                var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);

                string StringLoanDataValue;

                if (LoanDataValue == null)
                {
                    StringLoanDataValue = "";
                }
                else
                {
                    StringLoanDataValue = LoanDataValue.ToString();
                }

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();

                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if (ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();

                //DR - stores value after change
                string newValue;

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";

                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2
                                select u).Single();

                string tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                   where u.LoanID == field.LoanID && u.DocID == field.DocID
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3
                                   select u.Value).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking()
                                    where u.LoanID == field.LoanID && u.DocID == field.DocID
                                        && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4
                                    select u;

                if (tier1val.Value != tier2val)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if (error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    if (tier2val != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;

                var DataTranslation = (from DT in ImagingApp.vDataTranslations.AsNoTracking()
                                       where DT.DocID == field.DocID &&
                                       DT.FieldID == field.FieldID
                                       select DT);

                //DR - if the field has any DataTranslation, it's value may need to be translated
                if (DataTranslation.Count() > 0)
                {
                    foreach (var DT in DataTranslation)
                    {
                        //DR - if fieldvalue matches pretranslation, translate it into its actual MSPvalue
                        if (DT.PostTranslation.Trim().Equals(newValue.Trim()))
                        {
                            newValue = DT.PreTranslation;
                        }
                    }
                }

                //DR - update MSPUpdated to 1 if field value matches msp value
                if (CompareData(newValue, vField.Client_Data_Type, StringLoanDataValue))
                {
                    ImagingApp.UserInputMSPUpdated(field.LoanID, field.UserID, field.DocID, field.FieldID, field.InputID, field.Workflow, 1);
                }
                else //DR - add to report if the field value doesn't match msp
                {
                    writer.WriteLine(loanNum + "," + dealID + "," + productType + "," + reviewDate + "," + fieldChanged + "," + StringLoanDataValue + "," + newValue
                    + "," + vField.MspWorkstation + ",," + taskAssigned + "," + docNameFromPPV + ",," + auditStatus + "," + error + "," + commentTier2Column + ",," + QCReviewerID + "," + QCReviewDate + "," + QCOppIdentified + "," + QCCorrectedValue + "," + commentTier3Column);

                    report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                        + " \nField Name: " + vField.Name
                        + " \nField Description: " + vField.Description
                        + " \nWorkstation: " + vField.MspWorkstation
                        + " \nMSP Value: " + StringLoanDataValue
                        + " \nValue to Update with: " + newValue
                        + " \n";

                    count++;
                }

            }

            report = report + "----------------------------\n";


            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "Field Exception Report " + DateTime.Today.ToString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Field Exception Report", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportFieldExceptions(string[] email, DateTime ignoreDate)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - set the timestamp to have 8pm as an hour so we can truely ignore user inputs from the date passed in
            TimeSpan ts = new TimeSpan(20, 0, 0);
            ignoreDate = ignoreDate.Date + ts;

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.MSPUpdated == false && UI.UserTimeStamp < DateTime.Today
                             && UI.UserTimeStamp > ignoreDate
                             select UI;

            //DR - gives us report headers
            string report = "Fields that weren't updated in MSP\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            //writer.WriteLine("Loan Number,Field Name,Field Description,Workstation,MSP Value,Value to Update With");
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,MSP Workstation,,"
            + @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");

            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //DR - get the field value from loandata
                var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);

                string StringLoanDataValue;

                if (LoanDataValue == null)
                {
                    StringLoanDataValue = "";
                }
                else
                {
                    StringLoanDataValue = LoanDataValue.ToString();
                }

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();

                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if (ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();

                //DR - stores value after change
                string newValue;

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";

                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2
                                select u).Single();

                string tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                   where u.LoanID == field.LoanID && u.DocID == field.DocID
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3
                                   select u.Value).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking()
                                    where u.LoanID == field.LoanID && u.DocID == field.DocID
                                        && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4
                                    select u;

                if (tier1val.Value != tier2val)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if (error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    if (tier2val != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;

                var DataTranslation = (from DT in ImagingApp.vDataTranslations.AsNoTracking()
                                       where DT.DocID == field.DocID &&
                                       DT.FieldID == field.FieldID
                                       select DT);

                //DR - if the field has any DataTranslation, it's value may need to be translated
                if (DataTranslation.Count() > 0)
                {
                    foreach (var DT in DataTranslation)
                    {
                        //DR - if fieldvalue matches pretranslation, translate it into its actual MSPvalue
                        if (DT.PostTranslation.Trim().Equals(newValue.Trim()))
                        {
                            newValue = DT.PreTranslation;
                        }
                    }
                }

                //DR - update MSPUpdated to 1 if field value matches msp value
                if (CompareData(newValue, vField.Client_Data_Type, StringLoanDataValue))
                {
                    ImagingApp.UserInputMSPUpdated(field.LoanID, field.UserID, field.DocID, field.FieldID, field.InputID, field.Workflow, 1);
                }
                else //DR - add to report if the field value doesn't match msp
                {
                    writer.WriteLine(loanNum + "," + dealID + "," + productType + "," + reviewDate + "," + fieldChanged + "," + StringLoanDataValue + "," + newValue
                    + "," + vField.MspWorkstation + ",," + taskAssigned + "," + docNameFromPPV + ",," + auditStatus + "," + error + "," + commentTier2Column + ",," + QCReviewerID + "," + QCReviewDate + "," + QCOppIdentified + "," + QCCorrectedValue + "," + commentTier3Column);

                    report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                        + " \nField Name: " + vField.Name
                        + " \nField Description: " + vField.Description
                        + " \nWorkstation: " + vField.MspWorkstation
                        + " \nMSP Value: " + StringLoanDataValue
                        + " \nValue to Update with: " + newValue
                        + " \n";

                    count++;
                }

            }

            report = report + "----------------------------\n";


            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "Field Exception Report " + DateTime.Today.ToString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Field Exception Report", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportTableau(string email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.UserTimeStamp.Year == date.Year && UI.UserTimeStamp.Day == date.Day && UI.UserTimeStamp.Month == date.Month
                             select UI;

            //DR - gives us report headers
            string report = "Tableau Report\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,,,"
            +@"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");
            
            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);
                //var LoanDataValue = field.MSPValue;

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();
                
                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if(ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();
                
                //DR - stores values before and after change
                string previousValue, newValue;                

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";
                
                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking() where u.LoanID == field.LoanID && u.DocID == field.DocID 
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2 select u).Single();

                var tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking() where u.LoanID == field.LoanID && u.DocID == field.DocID 
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3 select u).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking() where u.LoanID == field.LoanID && u.DocID == field.DocID 
                                       && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4 select u;

                var LoanDataValue = tier2val.MSPValue;

                if (LoanDataValue == null)
                {
                    previousValue = "";
                }
                else
                {
                    previousValue = LoanDataValue.ToString();
                }

                if(tier1val.Value != tier2val.Value)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if(error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    LoanDataValue = tier3val.MSPValue;

                    if (LoanDataValue == null)
                    {
                        previousValue = "";
                    }
                    else
                    {
                        previousValue = LoanDataValue.ToString();
                    }

                    if (tier2val.Value != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;
               // writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,,,"
            //+ @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");
                writer.WriteLine(loanNum+","+dealID+","+productType+","+reviewDate+","+fieldChanged+","+previousValue+","+newValue
                    +",,,"+taskAssigned+","+docNameFromPPV+",,"+auditStatus+","+error+","+commentTier2Column+",,"+QCReviewerID+","+QCReviewDate+","+QCOppIdentified+","+QCCorrectedValue+","+commentTier3Column);

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                    + " \nField Name: " + vField.Name
                    + " \nField Description: " + vField.Description
                    + " \nWorkstation: " + vField.MspWorkstation
                    + " \nMSP Value: " + previousValue
                    + " \nValue to Update with: " + newValue
                    + " \n";

                count++;
            }

            report = report + "----------------------------\n";

            writer.Flush();
            ms.Position = 0;
            Attachment attach = new Attachment(ms, "Tableau Report" + date.ToShortDateString() + ".csv", "text/csv");
            EmailMessage em = new EmailMessage(email, "LoanReview: Tableau Report", report, attach);
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportTableau(string[] email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            CAPMKTSEntities CapMkts = new CAPMKTSEntities();

            //DR - grabs us the userinputs from the UserInput table where Tier 4 userinput's MSPUpdated = 0
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.InputID == 5 && UI.UserTimeStamp.Year == date.Year && UI.UserTimeStamp.Day == date.Day && UI.UserTimeStamp.Month == date.Month
                             select UI;

            //DR - gives us report headers
            string report = "Tableau Report\n";

            //DR - create memory stream for file
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,,,"
            + @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");

            int count = 1;
            Console.WriteLine("Building Report");

            //DR - adds the field exceptions to the report unless it's matching msp
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - get the field from the fields table
                var vField = (from f in ImagingApp.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                //var LoanDataValue = loan.GetType().GetProperty("Field" + field.FieldID).GetValue(loan);
                //var LoanDataValue = field.MSPValue;

                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                //DR - padded loan number
                string paddedLoanNumber = loanNum.ToString().PadLeft(10, '0');

                //DR - get the deal ID
                string dealID = (from l in CapMkts.Loan_Bayview.AsNoTracking() where l.bvln == paddedLoanNumber select l.deal_id).Single();

                //DR - holds productType
                string productType = null;
                string ArmPlanID = loan.ArmPlanID;

                if (ArmPlanID == null)
                {
                    productType = "FIXED";
                }
                else
                {
                    productType = "ARM";
                }

                //DR - gets the reviewDate
                string reviewDate = field.UserTimeStamp.ToShortDateString();
                //DR - gets the fieldname
                string fieldChanged = vField.Name.ToString();

                //DR - stores values before and after change
                string previousValue, newValue;

                if (field.Value == null)
                {
                    newValue = "";
                }
                else
                {
                    newValue = field.Value;
                }

                string taskAssigned = "LRVAPP";

                string auditStatus = "YES";

                string error;

                var tier1val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 2
                                select u).Single();

                var tier2val = (from u in ImagingApp.vUserInputs.AsNoTracking()
                                where u.LoanID == field.LoanID && u.DocID == field.DocID
                                    && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 3
                                select u).Single();

                var tier3valCount = from u in ImagingApp.vUserInputs.AsNoTracking()
                                    where u.LoanID == field.LoanID && u.DocID == field.DocID
                                        && u.FieldID == field.FieldID && u.Workflow == field.Workflow && u.InputID == 4
                                    select u;

                var LoanDataValue = tier2val.MSPValue;

                if (LoanDataValue == null)
                {
                    previousValue = "";
                }
                else
                {
                    previousValue = LoanDataValue.ToString();
                }

                if (tier1val.Value != tier2val.Value)
                {
                    error = "YES";
                }
                else
                {
                    error = "NO";
                }

                string commentTier2Column = "";

                if (error == "YES")
                {
                    commentTier2Column = "Previous Tier's Input";
                }

                string QCReviewerID = "";
                string QCReviewDate = "";
                string QCOppIdentified = "";
                string QCCorrectedValue = "";
                string commentTier3Column = "";

                //DR - Tier 3 values may not exist. Only set these variables if it does
                if (tier3valCount.Count() > 0)
                {
                    var tier3val = tier3valCount.Single();

                    QCReviewerID = tier3val.UserID.ToString();
                    QCReviewDate = tier3val.UserTimeStamp.ToShortDateString();

                    LoanDataValue = tier3val.MSPValue;

                    if (LoanDataValue == null)
                    {
                        previousValue = "";
                    }
                    else
                    {
                        previousValue = LoanDataValue.ToString();
                    }

                    if (tier2val.Value != tier3val.Value.ToString())
                    {
                        QCOppIdentified = "YES";
                    }
                    else
                    {
                        QCOppIdentified = "NO";
                    }

                    if (QCOppIdentified == "YES")
                    {
                        QCCorrectedValue = tier3val.Value.ToString();
                        commentTier3Column = "Previous Tier's Input";
                    }

                }
                string docNameFromPPV = tier1val.SelectedDocument;
                // writer.WriteLine(@"Loan_Number,Deal_ID,Product_Type,Review_Date,Field_changed,Previous_Value,New_Value,,,"
                //+ @"Task Assigned,Doc Name from PPV,,Audit Status,Error,Comment,,QC_Reviewer_ID,QC_Review_Date,QC_Opp_Identified,QC_Corrected_Value,Comment");
                writer.WriteLine(loanNum + "," + dealID + "," + productType + "," + reviewDate + "," + fieldChanged + "," + previousValue + "," + newValue
                    + ",,," + taskAssigned + "," + docNameFromPPV + ",," + auditStatus + "," + error + "," + commentTier2Column + ",," + QCReviewerID + "," + QCReviewDate + "," + QCOppIdentified + "," + QCCorrectedValue + "," + commentTier3Column);

                report = report + "----------------------------\n#" + count + "\nLoan Number: " + loanNum
                    + " \nField Name: " + vField.Name
                    + " \nField Description: " + vField.Description
                    + " \nWorkstation: " + vField.MspWorkstation
                    + " \nMSP Value: " + previousValue
                    + " \nValue to Update with: " + newValue
                    + " \n";

                count++;
            }

            report = report + "----------------------------\n";


            writer.Flush();
            ms.Position = 0;
            foreach (string e in email)
            {
                MemoryStream copy = new MemoryStream(ms.ToArray());
                Attachment attach = new Attachment(copy, "Tableau Report" + date.ToShortDateString() + ".csv", "text/csv");
                EmailMessage em = new EmailMessage(e, "LoanReview: Tableau Report", report, attach);
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportLoansCompletedCount(string email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            //DR - grabs us the userinputs from the UserInput table which were done on the date
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.UserTimeStamp.Month == date.Month
                             && UI.UserTimeStamp.Day == date.Day
                             && UI.UserTimeStamp.Year == date.Year
                             && UI.InputID == 5
                             select UI;

            //DR - gives us report headers
            StringBuilder report = new StringBuilder();
            report.AppendLine("Loans Completed Count");
            report.AppendLine("");

            //DR - set up our counters
            int Tier1Count = 0;
            int Tier2Count = 0;
            int Tier3Count = 0;
            int Tier4Count = 0;
            int IVT1Count = 0;
            int IVT2Count = 0;
            int IVT3Count = 0;
            int IVT4Count = 0;

            //DR - Workflow > Tier > ArrayList of loans. Ex. loansCompleted[1][1] returns an arraylist
            Dictionary<int, Dictionary<int, ArrayList>> loansCompleted = new Dictionary<int, Dictionary<int, ArrayList>>();

            Console.WriteLine("Building Report");

            //DR - adds the loanNums to the dictionary
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                int currentTier = 0;
                //DR - sets the current Tier based on inputID
                switch (field.InputID)
                {
                    case 1:
                        //DR - ignore inputid 1 fields
                        continue;
                    case 2:
                        currentTier = 1;
                        break;
                    case 3:
                        currentTier = 2;
                        break;
                    case 4:
                        currentTier = 3;
                        break;
                    case 5:
                        currentTier = 4;
                        break;
                }

                if (!loansCompleted.ContainsKey(field.Workflow))
                {
                    loansCompleted[field.Workflow] = new Dictionary<int, ArrayList>();
                }

                if (!loansCompleted[field.Workflow].ContainsKey(currentTier))
                {
                    loansCompleted[field.Workflow][currentTier] = new ArrayList();
                }

                if (!loansCompleted[field.Workflow][currentTier].Contains(loanNum))
                {
                    loansCompleted[field.Workflow][currentTier].Add(loanNum);
                }
            }

            //DR - increments each count
            foreach (KeyValuePair<int, Dictionary<int, ArrayList>> workflow in loansCompleted)
            {
                foreach (KeyValuePair<int, ArrayList> tier in loansCompleted[workflow.Key])
                {
                    foreach (int loanNum in loansCompleted[workflow.Key][tier.Key])
                    {
                        switch (workflow.Key)
                        {
                            case 1:
                                switch (tier.Key)
                                {
                                    case 1:
                                        Tier1Count++;
                                        break;
                                    case 2:
                                        Tier2Count++;
                                        break;
                                    case 3:
                                        Tier3Count++;
                                        break;
                                    case 4:
                                        Tier4Count++;
                                        break;
                                }
                                break;
                            case 2:
                                switch (tier.Key)
                                {
                                    case 1:
                                        IVT1Count++;
                                        break;
                                    case 2:
                                        IVT2Count++;
                                        break;
                                    case 3:
                                        IVT3Count++;
                                        break;
                                    case 4:
                                        IVT4Count++;
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
            int total = Tier4Count + IVT4Count;
            report.AppendLine("----------------------------");
            report.AppendLine("New Loans Completed: " + Tier4Count);
            report.AppendLine("IMAVAL Loans Completed: " + IVT4Count);
            report.AppendLine("ALL Loans Completed Total: " + total);
            report.AppendLine("----------------------------");

            int tier1Count = 0;
            int tier2Count = 0;
            int tier3Count = 0;
            int tier4Count = 0;
            int ivt1Count = 0;
            int ivt2Count = 0;
            int ivt3Count = 0;
            int ivt4Count = 0;


            //MA - Gets the amount of loan in normal workflow
            var tq = (from loan in ImagingApp.vLoanDatas
                      where loan.OwnedByUserID == null
                      && loan.NormalWorkflowCompleted == false
                      group loan by loan.Tier
                          into grp
                          select new
                          {
                              Tier = grp.Key,
                              Count = grp.Select(x => x.ID).Distinct().Count()
                          });

            //MA - Stores the amount of loans for each tier
            foreach (var count in tq)
            {
                if (count.Tier == 1)
                {
                    tier1Count = count.Count;
                }
                else if (count.Tier == 2)
                {
                    tier2Count = count.Count;
                }
                else if (count.Tier == 3)
                {
                    tier3Count = count.Count;
                }
                else
                    tier4Count = count.Count;
            }

            //MA - Gets the amount of loan in IMAVAL workflow
            var IVTtq = (from loan in ImagingApp.vWorkflowTracking_Loandata
                         where loan.OwnedByUserID == null
                         && loan.TaskStatus == 2
                         && loan.WorkflowStatus == false
                         && loan.NormalWorkflowCompleted == true
                         group loan by loan.WorkflowTier
                             into grp
                             select new
                             {
                                 WorkflowTier = grp.Key,
                                 Count = grp.Select(x => x.LoanID).Distinct().Count()
                             });

            //MA - Stores the amount of loans for each tier
            foreach (var count in IVTtq)
            {
                if (count.WorkflowTier == 1)
                {
                    ivt1Count = count.Count;
                }
                else if (count.WorkflowTier == 2)
                {
                    ivt2Count = count.Count;
                }
                else if (count.WorkflowTier == 3)
                {
                    ivt3Count = count.Count;
                }
                else
                {
                    ivt4Count = count.Count;
                }
            }

            report.AppendLine("----------------------------");
            report.AppendLine("New Loans Available by Tier: ");
            report.AppendLine("Tier 1: " + tier1Count);
            report.AppendLine("Tier 2: " + tier2Count);
            report.AppendLine("Tier 3: " + tier3Count);
            report.AppendLine("Tier 4: " + tier4Count);
            report.AppendLine("----------------------------");
            report.AppendLine("IMAVAL Loans Available by Tier: ");
            report.AppendLine("Tier 1: " + ivt1Count);
            report.AppendLine("Tier 2: " + ivt2Count);
            report.AppendLine("Tier 3: " + ivt3Count);
            report.AppendLine("Tier 4: " + ivt4Count);
            report.AppendLine("----------------------------");

            EmailMessage em = new EmailMessage(email, "LoanReview: Loans Completed Report", report.ToString());
            Console.WriteLine("Report sent");
            return;
        }

        public void ReportLoansCompletedCount(string[] email, DateTime date)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            //DR - grabs us the userinputs from the UserInput table which were done on the date
            var userInputs = from UI in ImagingApp.vUserInputs.AsNoTracking()
                             where UI.UserTimeStamp.Month == date.Month
                             && UI.UserTimeStamp.Day == date.Day
                             && UI.UserTimeStamp.Year == date.Year
                             && UI.InputID == 5
                             select UI;

            //DR - gives us report headers
            StringBuilder report = new StringBuilder();
            report.AppendLine("Loans Completed Count");
            report.AppendLine("");

            //DR - set up our counters
            int Tier1Count = 0;
            int Tier2Count = 0;
            int Tier3Count = 0;
            int Tier4Count = 0;
            int IVT1Count = 0;
            int IVT2Count = 0;
            int IVT3Count = 0;
            int IVT4Count = 0;

            //DR - Workflow > Tier > ArrayList of loans. Ex. loansCompleted[1][1] returns an arraylist
            Dictionary<int, Dictionary<int, ArrayList>> loansCompleted = new Dictionary<int, Dictionary<int, ArrayList>>();

            Console.WriteLine("Building Report");

            //DR - adds the loanNums to the dictionary
            foreach (var field in userInputs)
            {
                //DR - get the associated loan
                var loan = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == field.LoanID select l).Single();
                //DR - gets the loan number
                int loanNum = loan.LoanNumber;

                int currentTier = 0;
                //DR - sets the current Tier based on inputID
                switch (field.InputID)
                {
                    case 1:
                        //DR - ignore inputid 1 fields
                        continue;
                    case 2:
                        currentTier = 1;
                        break;
                    case 3:
                        currentTier = 2;
                        break;
                    case 4:
                        currentTier = 3;
                        break;
                    case 5:
                        currentTier = 4;
                        break;
                }

                if (!loansCompleted.ContainsKey(field.Workflow))
                {
                    loansCompleted[field.Workflow] = new Dictionary<int, ArrayList>();
                }

                if (!loansCompleted[field.Workflow].ContainsKey(currentTier))
                {
                    loansCompleted[field.Workflow][currentTier] = new ArrayList();
                }

                if (!loansCompleted[field.Workflow][currentTier].Contains(loanNum))
                {
                    loansCompleted[field.Workflow][currentTier].Add(loanNum);
                }
            }

            //DR - increments each count
            foreach (KeyValuePair<int, Dictionary<int, ArrayList>> workflow in loansCompleted)
            {
                foreach (KeyValuePair<int, ArrayList> tier in loansCompleted[workflow.Key])
                {
                    foreach (int loanNum in loansCompleted[workflow.Key][tier.Key])
                    {
                        switch (workflow.Key)
                        {
                            case 1:
                                switch (tier.Key)
                                {
                                    case 1:
                                        Tier1Count++;
                                        break;
                                    case 2:
                                        Tier2Count++;
                                        break;
                                    case 3:
                                        Tier3Count++;
                                        break;
                                    case 4:
                                        Tier4Count++;
                                        break;
                                }
                                break;
                            case 2:
                                switch (tier.Key)
                                {
                                    case 1:
                                        IVT1Count++;
                                        break;
                                    case 2:
                                        IVT2Count++;
                                        break;
                                    case 3:
                                        IVT3Count++;
                                        break;
                                    case 4:
                                        IVT4Count++;
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
            int total = Tier4Count + IVT4Count;
            report.AppendLine("----------------------------");
            report.AppendLine("New Loans Completed: " + Tier4Count);
            report.AppendLine("IMAVAL Loans Completed: " + IVT4Count);
            report.AppendLine("ALL Loans Completed Total: " + total);
            report.AppendLine("----------------------------");

            int tier1Count = 0;
            int tier2Count = 0;
            int tier3Count = 0;
            int tier4Count = 0;
            int ivt1Count = 0;
            int ivt2Count = 0;
            int ivt3Count = 0;
            int ivt4Count = 0;


            //MA - Gets the amount of loan in normal workflow
            var tq = (from loan in ImagingApp.vLoanDatas
                      where loan.OwnedByUserID == null
                      && loan.NormalWorkflowCompleted == false
                      group loan by loan.Tier
                          into grp
                          select new
                          {
                              Tier = grp.Key,
                              Count = grp.Select(x => x.ID).Distinct().Count()
                          });

            //MA - Stores the amount of loans for each tier
            foreach (var count in tq)
            {
                if (count.Tier == 1)
                {
                    tier1Count = count.Count;
                }
                else if (count.Tier == 2)
                {
                    tier2Count = count.Count;
                }
                else if (count.Tier == 3)
                {
                    tier3Count = count.Count;
                }
                else
                    tier4Count = count.Count;
            }

            //MA - Gets the amount of loan in IMAVAL workflow
            var IVTtq = (from loan in ImagingApp.vWorkflowTracking_Loandata
                         where loan.OwnedByUserID == null
                         && loan.TaskStatus == 2
                         && loan.WorkflowStatus == false
                         && loan.NormalWorkflowCompleted == true
                         group loan by loan.WorkflowTier
                             into grp
                             select new
                             {
                                 WorkflowTier = grp.Key,
                                 Count = grp.Select(x => x.LoanID).Distinct().Count()
                             });

            //MA - Stores the amount of loans for each tier
            foreach (var count in IVTtq)
            {
                if (count.WorkflowTier == 1)
                {
                    ivt1Count = count.Count;
                }
                else if (count.WorkflowTier == 2)
                {
                    ivt2Count = count.Count;
                }
                else if (count.WorkflowTier == 3)
                {
                    ivt3Count = count.Count;
                }
                else
                {
                    ivt4Count = count.Count;
                }
            }

            report.AppendLine("----------------------------");
            report.AppendLine("New Loans Available by Tier: ");
            report.AppendLine("Tier 1: " + tier1Count);
            report.AppendLine("Tier 2: " + tier2Count);
            report.AppendLine("Tier 3: " + tier3Count);
            report.AppendLine("Tier 4: " + tier4Count);
            report.AppendLine("----------------------------");
            report.AppendLine("IMAVAL Loans Available by Tier: ");
            report.AppendLine("Tier 1: " + ivt1Count);
            report.AppendLine("Tier 2: " + ivt2Count);
            report.AppendLine("Tier 3: " + ivt3Count);
            report.AppendLine("Tier 4: " + ivt4Count);
            report.AppendLine("----------------------------");

            foreach (string e in email)
            {
                EmailMessage em = new EmailMessage(e, "LoanReview: Loans Completed Report", report.ToString());
                em = null;
            }
            Console.WriteLine("Report sent");
            return;
        }

        //DR - compares data based on the data type and returns true if they're equal and false if they are not equal
        private bool CompareData(string userInput, string dataType, string dbData)
        {
            //DR - if both dbData and userInput are equal to null or empty string return true
            if ((userInput == null | userInput.Trim() == "") && (dbData == null | dbData.Trim() == ""))
            {
                ////DR - don't return true if it's a recon fee comment code
                //if (dataType != "reconfee")
                //{
                //    return true;
                //}
                return true;
            }

            switch (dataType.ToLower())
            {
                case "int":

                    double unused;

                    if (!Double.TryParse(userInput, out unused) | !Double.TryParse(dbData, out unused))
                    {
                        return false;
                    }
                    double uInput = double.Parse(userInput);
                    double dData = double.Parse(dbData);

                    if (uInput.Equals(dData))
                    {
                        return true;
                    }
                    return false;
                case "phone":
                    string uInput2 = userInput;
                    uInput2 = uInput2.Replace("(", "");
                    uInput2 = uInput2.Replace(")", "");
                    uInput2 = uInput2.Replace("-", "");

                    string dData2 = dbData;
                    dData2 = dData2.Replace("(", "");
                    dData2 = dData2.Replace(")", "");
                    dData2 = dData2.Replace("-", "");

                    if (uInput2.Equals(dData2))
                    {
                        return true;
                    }
                    return false;
                case "double":
                    string removeSymbols = userInput;
                    removeSymbols = removeSymbols.Replace("$", "");
                    removeSymbols = removeSymbols.Replace(" ", "");
                    removeSymbols = removeSymbols.Replace(",", "");
                    var ch = '.';
                    if (removeSymbols.Equals("."))
                    {
                        return false;
                    }
                    else if (removeSymbols.IndexOf(ch) != removeSymbols.LastIndexOf(ch))
                    {
                        return false;
                    }

                    double unused2;

                    if (!Double.TryParse(removeSymbols, out unused2) | !Double.TryParse(dbData, out unused2))
                    {
                        return false;
                    }

                    double uInput3 = double.Parse(removeSymbols);
                    double dData3 = double.Parse(dbData);

                    if (uInput3.Equals(dData3))
                    {
                        return true;
                    }
                    return false;
                case "tin":
                case "ssn":
                    string uInput4 = userInput;
                    uInput4 = uInput4.Replace("-", "");
                    string dData4 = dbData;
                    dData4 = dData4.Replace("-", "");
                    if (uInput4.Trim().Equals(dData4.Trim()))
                    {
                        return true;
                    }
                    return false;
                case "date":
                    //if user and database have no date input return true
                    if (userInput.Replace("/", "").Trim() == "" && dbData.Replace("/", "").Trim() == "")
                    {
                        return true;
                    }

                    DateTime unused3;

                    if (!DateTime.TryParse(userInput, out unused3) | !DateTime.TryParse(dbData, out unused3))
                    {
                        return false;
                    }

                    DateTime uInput5 = Convert.ToDateTime(userInput);
                    DateTime dData5 = Convert.ToDateTime(dbData);

                    if (uInput5.CompareTo(dData5) == 0)
                    {
                        return true;
                    }

                    return false;
                case "ssn/tin":
                    string uInput6 = userInput;
                    string dData6 = dbData;

                    bool uInputIsTin = false;
                    bool dDataIsTin = false;

                    //DR - if it has a dash, it is a tin
                    if (uInput6.Contains("-"))
                    {
                        uInputIsTin = true;
                    }
                    if (dData6.Contains("-"))
                    {
                        dDataIsTin = true;
                    }

                    //DR- if they both aren't Tin or if they both aren't SSN, return false
                    if (uInputIsTin != dDataIsTin)
                    {
                        return false;
                    }

                    uInput6 = uInput6.Replace("-", "");
                    dData6 = dData6.Replace("-", "");

                    if (uInput6.Trim().Equals(dData6.Trim()))
                    {
                        return true;
                    }
                    return false;
                case "prepaymentpenalty":
                    ////DR - prepayment penalty always gets escalated unless they didn't enter data
                    //string uinput7 = userInput;
                    //uinput7 = uinput7.Replace("Pre-payment penalty expired:(", "");
                    //uinput7 = uinput7.Replace(")", "");
                    //uinput7 = uinput7.Replace("/", "");
                    //if (uinput7.Trim().Equals(""))
                    //{
                        return true;
                    //}
                    //return false;
                case "commentcode":
                    //DR - always return true for the recon fee comment code
                    return true;
                case "zip":
                    string uInput8 = userInput;
                    uInput8 = uInput8.Replace("-", "");
                    uInput8 = uInput8.Replace("0000", "");
                    string dData8 = dbData;
                    dData8 = dData8.Replace("-", "");
                    if (uInput8.Trim().Equals(dData8.Trim()))
                    {
                        return true;
                    }
                    return false;
                case "combobox":
                    string[] arr = userInput.Split('|');
                    string uInput9 = arr[0];
                    string dData9 = dbData;
                    if (uInput9.Trim().ToLower().Equals(dData9.Trim().ToLower()))
                    {
                        arr = null;
                        return true;
                    }
                    return false;
                case "mmyy":
                    //if user and database have no date input return true
                    if (userInput.Replace("/", "").Trim() == "" && dbData.Replace("/", "").Trim() == "")
                    {
                        return true;
                    }

                    DateTime dData10;
                    string[] uInputArr = userInput.Split('/');
                    int uInputMonth, uInputYear;

                    //DR - needs to be checked seperately or exception will be thrown if checked together with everything else
                    if(uInputArr.Length < 2)
                    {
                        return false;
                    }

                    if (!DateTime.TryParse(dbData, out dData10) | !int.TryParse(uInputArr[0], out uInputMonth)
                        | !int.TryParse(uInputArr[1], out uInputYear))
                    {
                        return false;
                    }

                    if (dData10.Month == uInputMonth && (dData10.Year % 100) == uInputYear)
                    {
                        return true;
                    }
                    return false;
                default:
                    if (userInput.Replace(" ", "").Trim().ToLower().Equals(dbData.Replace(" ", "").Trim().ToLower()))
                    {
                        return true;
                    }
                    return false;
            }
        }

        private void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
                Console.WriteLine("Exception Occured while releasing object " + ex.ToString());
            }
            finally
            {
                GC.Collect();
            }
        }

    }
}
