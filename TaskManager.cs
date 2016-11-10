using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanReviewAutomation
{
    class TaskManager
    {
        //DR- updates IVT loans in workflow tracking depending on their status in LDG task tracking
        public void UpdateTaskStatus()
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();
            LDGEntities LDG = new LDGEntities();

            //DR - grabs us documents from workflowtracking table for tasks
            var ivtTasks = from IVT in ImagingApp.vWorkflowTracking_Loandata.AsNoTracking() where IVT.TaskStatus == 1 select IVT;

            //DR - holds loan number for each loanID
            Dictionary<int, string> DictionaryLoanNumber = new Dictionary<int, string>();

            Console.WriteLine("Checking to see if tasks need to be updated");
            //DR - gets the loan number for each loanid in workflowtracking
            foreach (var document in ivtTasks)
            {
                if (!DictionaryLoanNumber.ContainsKey(document.LoanID))
                {
                    string LN = (from l in ImagingApp.vLoanDatas.AsNoTracking() where l.ID == document.LoanID select l.LoanNumber).Single().ToString();

                    DictionaryLoanNumber[document.LoanID] = LN;
                }
            }

            int count = 0;
            //DR -update taskstatus to 2 in workflowtracking if the task is closed in LDG tasktracking
            foreach (KeyValuePair<int, string> loan in DictionaryLoanNumber)
            {
                string LN = DictionaryLoanNumber[loan.Key];
                string paddedLoanNumber = LN.ToString().PadLeft(10, '0');
                var taskTracker = (from t in LDG.LDG_TASK_TRACKING.AsNoTracking() where t.LN_NO == paddedLoanNumber && t.TSK_ID == "IMAVAL" select t);

                if (taskTracker.Count() == 0)
                {
                    continue;
                }

                foreach (var task in taskTracker)
                {
                    if (task.TSK_STATUS_CD == "C" | task.TSK_STATUS_CD == "D")
                    {
                        ImagingApp.UpdateWorkflowTracking(loan.Key, 2);
                        Console.WriteLine("Task Status updated for LoanID/LoanNumber: " + loan.Key + "/" + loan.Value);
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                Console.WriteLine("There were no tasks to update.");
            }
        }

        //DR - runs the dtsx package
        public void RunDtsxPackage()
        {
            string strCmdText;
            //DR - /C tells the command prompt to execute the following command without user input
            //strCmdText = @"/C dtexec /file ""\\miafile01b\DataIntegrationApps\PROJECTS\GROUP_PROJECTS\Loan_Review\DavidDTSpackage\Integration Services Project1\bin\Package.dtsx""";
            strCmdText = @"/C dtexec /file ""\\miafile01b\DataIntegrationApps\PROJECTS\GROUP_PROJECTS\Loan_Review\LoanReviewAutomationDTSXPackageLocation\Package.dtsx""";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }

        //DR - sets app to maintenance mode
        public void MaintenanceModeSet()
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            Console.WriteLine("Taking App Down");

            ImagingApp.MaintenanceModeSet();

            CheckCloudStatus();
        }

        //DR - removes app from maintenance mode
        public void MaintenanceModeRemove()
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            Console.WriteLine("Taking App Live");

            ImagingApp.MaintenanceModeRemove();

            CheckCloudStatus();
        }

        //DR - outputs whether app is up or not
        public void CheckCloudStatus()
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            bool cloudStatus = (from c in ImagingApp.Cloud_Application_Status.AsNoTracking() select c.DatabaseStatus).Single();

            if (cloudStatus)
            {
                Console.WriteLine("Cloud is on the line");
            }
            else
            {
                Console.WriteLine("Cloud is off the line");
            }
        }

        //DR - returns true if app is up
        public bool CloudStatus()
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            bool cloudStatus = (from c in ImagingApp.Cloud_Application_Status.AsNoTracking() select c.DatabaseStatus).Single();

            if (cloudStatus)
            {
                return true;
            }

            return false;
        }

        //DR - imports E05T data to E05T report table
        public void ImportEditReport(string path)
        {
            ImagingAppsEntities ImagingApp = new ImagingAppsEntities();

            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryReport[7/15/16][1234][1] returns EO5T
            Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryE05TReport = new Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>>();
            //DR - Date(string) -> LoanNumber -> Tran code - > Cards Ex. DictionaryP1CSReport[7/15/16][1234][504] returns ArrayList of cards
            Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport = new Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>>();

            //DR - gets the file paths for all files with the supplied extension. Does Not enter any child folders.
            var files = Directory.GetFiles(path, "*.csv", SearchOption.TopDirectoryOnly);
            //DR - holds the list of files to move after we've parsed them
            ArrayList filesToMove = new ArrayList();

            //DR - if there are no files to parse, program ends
            if (files.Length == 0)
            {

                Console.WriteLine("Unable to locate files to parse in specified directory: " + path);
                Console.WriteLine("Program Ended");
                return;

            }

            //DR - creates a datetime for today which is used in the directory creation
            DateTime thisDay = DateTime.Today;
            string today = thisDay.ToString("MMM d yyyy");
            //where our completed files will go
            string destinationFilePath = System.IO.Path.Combine(path, "Completed " + today);

            //DR - checks to see if directory exists already and creates the directory if it does not. 
            if (!System.IO.Directory.Exists(destinationFilePath))
            {
                //DR - creates the directory based on today's date MMM d yyyy
                System.IO.Directory.CreateDirectory(destinationFilePath);
                Console.WriteLine("Creating completed directory: " + destinationFilePath);
            }

            StringBuilder log = new StringBuilder();
            bool hasLog = false;

            //DR - setup P1CS Dictionary
            this.parseP1CSreports(ref DictionaryP1CSReport, destinationFilePath);

            //DR - reads the data for each file in the directory of the supplied extensions and adds the data to the dictionary
            foreach (string file in files)
            {
                try
                {
                    //DR - if file isn't an edit report of tran 502 or 504 ignore it
                    if (!(file.Contains("E05T-ARM_LOAN_EDIT_TRAN502") | file.Contains("E05T-ARM_LOAN_EDIT_TRAN504")))
                    {
                        continue;
                    }

                    string reportDate = "";
                    string cardCode = "";
                    int tranCode = 0;
                    CardCode cc = new CardCode();

                    if (file.Contains("E05T-ARM_LOAN_EDIT_TRAN502"))
                    {
                        cardCode = file.Replace(".csv", "").Split(new string[] { "E05T-ARM_LOAN_EDIT_TRAN502" }, StringSplitOptions.None)[1].Split('_')[0];
                        reportDate = file.Replace(".csv", "").Split(new string[] { "E05T-ARM_LOAN_EDIT_TRAN502" }, StringSplitOptions.None)[1].Split('_')[1];
                        tranCode = 502;

                        switch (cardCode.ToLower())
                        {
                            case "cc1":
                                cc.CardCode1(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc2":
                                cc.CardCode2(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc3":
                                cc.CardCode3(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc4":
                                cc.CardCode4(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc5":
                                cc.CardCode5(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc6":
                                cc.CardCode6(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc7":
                                cc.CardCode7(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc8":
                                cc.CardCode8(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc9":
                                cc.CardCode9(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc10":
                                cc.CardCode10(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc11":
                                cc.CardCode11(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc12":
                                cc.CardCode12(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc13":
                                cc.CardCode13(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc14":
                                cc.CardCode14(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc15":
                                cc.CardCode15(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc16":
                                cc.CardCode16(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc17":
                                cc.CardCode17(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc18":
                                cc.CardCode18(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                        }
                    }
                    else if (file.Contains("E05T-ARM_LOAN_EDIT_TRAN504"))
                    {
                        cardCode = file.Replace(".csv", "").Split(new string[] { "E05T-ARM_LOAN_EDIT_TRAN504" }, StringSplitOptions.None)[1].Split('_')[0];
                        reportDate = file.Replace(".csv", "").Split(new string[] { "E05T-ARM_LOAN_EDIT_TRAN504" }, StringSplitOptions.None)[1].Split('_')[1];
                        tranCode = 504;

                        switch (cardCode.ToLower())
                        {
                            case "cc1":
                                cc.CardCode1(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc2":
                                cc.CardCode2(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc3":
                                cc.CardCode3(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                            case "cc4":
                                cc.CardCode4(reportDate, tranCode, ref DictionaryE05TReport, ref DictionaryP1CSReport, file);
                                break;
                        }
                    }
                    if (cc.getHasLog())
                    {
                        log.AppendLine(cc.getLogToString());
                        hasLog = true;
                    }
                    cc = null;
                    //DR - adds the file name to the list
                    filesToMove.Add(file);
                }
                catch (Exception ex)
                {
                    LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "File :: "+file),destinationFilePath);
                }
            }

            Console.WriteLine("Begin Insert at "+DateTime.Now);
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryReport[7/15/16][1234][1] returns EO5T
            //DR - save each record in the E05T table
            foreach (KeyValuePair<string, Dictionary<int, Dictionary<int, EO5T_Report>>> rDate in DictionaryE05TReport)
            {
                foreach (KeyValuePair<int, Dictionary<int, EO5T_Report>> loanNum in DictionaryE05TReport[rDate.Key])
                {
                    foreach (KeyValuePair<int, EO5T_Report> record in DictionaryE05TReport[rDate.Key][loanNum.Key])
                    {
                        try
                        {
                            EO5T_Report report = record.Value;

                            ImagingApp.EO5TInsert(
                            report.LOAN_NUMBER,
report.REPORT_DATE,
report.TRAN_CODE,
report.RECORD_NUMBER,
report.ACT_CD502,
report.CTL_EFF_DATE,
report.ARM_PLAN,
report.RECALC_DAYS,
report.IR_LD_TIME,
report.PI_LD_TIME,
report.CV_LD_TIME,
report.PMT_BT_NO,
report.IR_EFF_DT_NXT_NOT,
report.SEP_IR_PI_NOT,
report.ARM_CD,
report.ENT_UPD_ID,
report.DATA_VERF,
report.SYS_ID502,
report.ORIG_LTERM,
report.INITIAL_IR,
report.INITIAL_PI,
report.INITIAL_INDEX,
report.INITIAL_PMT_RATE,
report.INITIAL_NOTC_DT,
report.NEXT_CALC_DT,
report.OPT_PI_START_DT,
report.OPT_PI_START_CD,
report.CONTROL_TYPE,
report.CONTROL_DESCRIPTION,
report.ORIG_IR_CHG_DATE,
report.PMT_BT_IR_CHG,
report.NEXT_IR_EFF_CALC,
report.IR_CHG_LETTER,
report.IR_NOT_LEAD_TIME_MONTH,
report.IR_NOT_LEAD_TIME_DAYS,
report.ORIG_PI_CHG_DATE,
report.PMT_BT_PI_CHG,
report.NEXT_PI_EFF_CALC,
report.PI_NOT_LEAD_TIME_MONTH,
report.PI_NOT_LEAD_TIME_DAYS,
report.PI_CHG_LETTER,
report.FINAL_OPT_PI_CHG_DT,
report.OPT_PI_ACT_AMT,
report.OPPI_THCD,
report.OPPI_A_PC,
report.NXT_NGAM_CALC_DT,
report.NGAM_PER,
report.NGAM_LEAD,
report.PI_DISC,
report.IR_CAL_MTH,
report.IR_RNDING_TYP,
report.IR_RNDING_BASIS,
report.MARGIN,
report.IR_AVG_CD,
report.NMB_IR_VAL,
report.PMT_FST_IR,
report.PMT_BTW_IR,
report.IR_CARRY_OVER_CD,
report.IR_CARRY_OVER_AMOUNT,
report.INDEX_CODE,
report.INDEX_LEAD_TIME_MONTH,
report.INDEX_LEAD_TIME_DAYS,
report.INDEX_AVG_CD,
report.INDEX_NMB_VAL,
report.INDEX_PMT_FST,
report.INDEX_PMT_BTW,
report.INDEX_CALC_MTH,
report.ORIG_IR,
report.ALT_NG,
report.PER_CHG_MAX_IR_INCR,
report.PER_CHG_MAX_IR_DECR,
report.PER_CHG_MIN_IR_INCR,
report.PER_CHG_MIN_IR_DECR,
report.PMT_BTW_NO_IR_CAP,
report.INDX_CD_A,
report.INDX_LD_TIME_A_MONTH,
report.INDX_LD_TIME_A_DAYS,
report.INDX_AVG_CD,
report.INDX_VAL,
report.PMT_1ST_INDX_VAL,
report.PMT_BTW_INDX_VAL,
report.IR_INDX,
report.IR_IX_RND_TYP,
report.IR_IX_RND_BAS,
report.PER_CHG_PERIODIC_1_MAX_IR_INCREASE,
report.PER_CHG_PERIODIC_1_MAX_IR_DECREASE,
report.PER_CHG_PERIODIC_1_BASE_RATE_CHANGE,
report.PER_CHG_PERIODIC_1_BASE_RATE_NUM_PMT,
report.PER_CHG_PERIODIC_1_BASE_RATE_NXT_SEL_DATE,
report.NXT_NO_IR_CAP_DATE,
report.PMTS_TO_IR_CAP_BASE_RATE,
report.PER_CHG_PERIODIC_2_MAX_IR_INCREASE,
report.PER_CHG_PERIODIC_2_MAX_IR_DECREASE,
report.PER_CHG_PERIODIC_2_BASE_RATE_CHANGE,
report.PER_CHG_PERIODIC_2_BASE_RATE_NUM_PMT,
report.PER_CHG_PERIODIC_2_BASE_RATE_NXT_SEL_DATE,
report.LIFE_MAX_IR_INC_PCT,
report.LIFE_MAX_IR_INC_AMT,
report.LIFE_MAX_IR_DEC_PCT,
report.LIFE_MAX_IR_DEC_AMT,
report.SKIP_IR,
report.PI_CALCULATION_FIELDS_1_CALC_MTH,
report.PI_CALCULATION_FIELDS_1_NMB_CHG,
report.PI_CALCULATION_FIELDS_1_PCT_INC,
report.PI_CALCULATION_FIELDS_1_PMT_CHG,
report.PI_CALCULATION_FIELDS_1_AMTZ_MAX,
report.PRN_CAL_MTH,
report.PI_CALCULATION_FIELDS_2_CALC_MTH,
report.PI_CALCULATION_FIELDS_2_NMB_CHG,
report.PI_CALCULATION_FIELDS_2_PCT_INC,
report.PI_CALCULATION_FIELDS_2_PMT_CHG,
report.PI_CALCULATION_FIELDS_2_AMTZ_MAX,
report.PI_CALCULATION_FIELDS_2_INIT_OPT_CALC_PCT,
report.ORIG_PI,
report.PI_CALCULATION_FIELDS_3_CALC_MTH,
report.PI_CALCULATION_FIELDS_3_FLOOR_MARGIN,
report.PI_CALCULATION_FIELDS_3_AMTZ_MAX,
report.LIFE_OF_LOAN_PI_LIMITS_MAX_PCT,
report.LIFE_OF_LOAN_PI_LIMITS_MAX_AMT,
report.LIFE_OF_LOAN_PI_LIMITS_MIN_PCT,
report.LIFE_OF_LOAN_PI_LIMITS_MIN_AMT,
report.PI_PERCENT_CHANGE_REQUIREMENTS_MAX_INC,
report.PI_PERCENT_CHANGE_REQUIREMENTS_MAX_DEC,
report.PI_PERCENT_CHANGE_REQUIREMENTS_MIN_INC,
report.PI_PERCENT_CHANGE_REQUIREMENTS_MIN_DEC,
report.PMT_BTW_NO_PI_CAPS,
report.NXT_NO_PI_CAP_DATE,
report.PRN_BAL_CAP_PCT,
report.FULL_AM_IR_CD,
report.MAX_PRN_BAL,
report.NEG_AM_CD_IR_CAL,
report.NEG_AM_CD_LETTER,
report.MAX_TERM_X_PCT,
report.MAX_TERM_X_PMT,
report.IR_PI_ADJ_CD,
report.DEFERED_INTEREST_PI_FLG,
report.DEFERED_INTEREST_MAX_INC,
report.DEFERED_INTEREST_MAX_DEC,
report.PAYMENT_RATE_CAL_MTH,
report.PAYMENT_RATE_RND_BAS,
report.PAYMENT_RATE_RND_TYP,
report.PAYMENT_RATE_MARGIN,
report.PAYMENT_RATE_AVG_CD,
report.PMT_RATE_CALC_NBM_VAL,
report.PMT_RATE_CALC_PMT_FST,
report.PMT_RATE_CALC_PMT_BTW,
report.PMT_RATE_INT_CD,
report.CARRYOVER_INT_AMT,
report.PR_PERCENT_INDX,
report.PMT_RATE_LD_TIME,
report.PMT_RATE_OFFSET,
report.PER_CHG_MAX_PR_INCR,
report.PER_CHG_MAX_PR_DECR,
report.PER_CHG_MIN_PR_INCR,
report.PER_CHG_MIN_PR_DECR,
report.NO_PR_CAP_PMT_BTW,
report.NO_PR_CAP_DATE,
report.PMT_TO_PR_CAP_BASE,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_MTH,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_CODE,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_LD_TIME,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_AVG_CD,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_NBM_VAL,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_PMT_FST,
report.INDEX_PAYMENT_RATE_CALCULATIONS_1_PMT_BTW,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_CODE,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_LD_TIME,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_AVG_CD,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_NBM_VAL,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_PMT_FST,
report.INDEX_PAYMENT_RATE_CALCULATIONS_2_PMT_BTW,
report.IX_RND_TY,
report.IX_RND_BAS,
report.CONVERTIBLE_CD,
report.CONVERTIBLE_ST,
report.CONVERTIBLE_OPT_RPY,
report.CONVERTIBLE_FEE_AMOUNT,
report.CONVERTIBLE_FEE_PERCENT,
report.CONVERTIBLE_FEE_MTH,
report.CONVERTIBLE_OPTION_ORG_DATE,
report.CONVERTIBLE_OPTION_PMT_BTW,
report.CONVERTIBLE_OPTION_NXT_DATE,
report.CONVERTIBLE_OPTION_LD_TM_NOTF,
report.CONVERTIBLE_OPTION_LETTER,
report.CONVERTIBLE_OPTION_STP_DATE,
report.DEMAND_CONVERTIBLE_NOTIFY_DATE,
report.DEMAND_CONVERTIBLE_NOTIFY_PMTS_BTW,
report.DEMAND_LETTER_CD,
report.CONV_OPT_NOTF_DATE,
report.ACT_CD504,
report.CHANGE_DATE,
report.PI_TYPE,
report.PI_AMOUNT,
report.INTEREST_TYPE,
report.INTEREST_RATE,
report.SERVICE_FEE_TYPE,
report.SERVICE_FEE_AMT_RATE,
report.REP_RES,
report.ENTRY_ID,
report.SYS_ID504,
report.PAY_OPT,
report.REASON_PI,
report.REASON_IR,
report.REASON_REP_RES,
report.PMT_PD,
report.DST_TYP,
report.PLAN_CODE,
report.OVER_SHORT_SPREAD,
report.OVER_SHORT_RSN,
report.HUD_PART,
report.HUD_235,
report.MISC_AMOUNT,
report.MISC_CD,
report.MISC_RDN,
report.ALT_DUE_DAY,
report.ALT_DUE_ADJ,
report.COUNTY_TAX,
report.COUNTY_RSN,
report.CITY_TAX,
report.CITY_RSN,
report.HAZ_PREM,
report.HAZ_RSN,
report.MIP_MIP,
report.MIP_RSN,
report.LIEN_LIEN,
report.LIEN_RSN,
report.IR_INDEX,
report.IR_CARRY_OVER,
report.PAYMENT_RATE,
report.PR_INDEX,
report.PR_CARRY_OVER,
report.IR_CARRY_DIFF
    );
                        }
                        catch (Exception ex)
                        {
                            LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "Loan Number: "+record.Value.LOAN_NUMBER+" Report Date: "+record.Value.REPORT_DATE+" Record Number: "+record.Value.RECORD_NUMBER), destinationFilePath);
                        }
                    }
                }
            }

            Console.WriteLine("End Insert at " + DateTime.Now);
            //DR - create log
            if(hasLog)
            {
                LogFile.LogFileWrite(log.ToString(), destinationFilePath);
            }

            //DR - moves all the files we've parsed data for
            foreach (string file in filesToMove)
            {
                try
                {
                    //DR - moves the report to the completed directory. 
                    string fileName = System.IO.Path.GetFileName(file);
                    string move = System.IO.Path.Combine(destinationFilePath, fileName);
                    System.IO.File.Move(file, move);
                    Console.WriteLine("Moving " + file + " to " + move);
                }
                catch (Exception ex)
                {
                    LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, ""), destinationFilePath);
                }
            }
        }

        public void parseP1CSreports(ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string EO5TCompletedPath)
        {
            string P1CSReportPath = @"\\bvappshare\applications\common\TransferStaging\OCIE\Inbound\DATAMINE_JOBS\PRIMARY_REPORTS\P1CSPRT2_LoanReview";

            //DR - gets the file paths for all files with the supplied extension. Does Not enter any child folders.
            var files = Directory.GetFiles(P1CSReportPath, "*.csv", SearchOption.TopDirectoryOnly);

            //DR - if there are no files to parse, method ends
            if (files.Length == 0)
            {
                Console.WriteLine("No P1CS Report files in: " + P1CSReportPath);
                return;
            }

            //DR - reads the data for each file in the directory of the supplied extensions and adds the data to the dictionary
            foreach (string file in files)
            {
                try
                {
                    //DR - if file isn't a P1CS Error report ignore it
                    if (!file.Contains(@"P1CS-LOAN_CHANGE_FILE_MAINTENANCE_REPORT_[opPRT_2[cp_ErrorCodes_"))
                    {
                        continue;
                    }

                    string reportDate = file.Replace(".csv", "").Split(new string[] { @"P1CS-LOAN_CHANGE_FILE_MAINTENANCE_REPORT_[opPRT_2[cp_ErrorCodes_" }, StringSplitOptions.None)[1];

                    //DR - Date(string) -> LoanNumber -> Cards Ex. DictionaryP1CSReport[7/15/16][1234] returns ArrayList of cards
                    //DR - add report date to dictionary if it hasn't been added already
                    if (!DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        DictionaryP1CSReport[reportDate] = new Dictionary<int, Dictionary<int, ArrayList>>();
                    }

                    //DR - path for the csv file
                    string path = file;
                    Console.WriteLine("Parsing data for: " + path);

                    //DR - Makes a streamreader from the file
                    Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader reader = new StreamReader(stream);

                    //DR - reads the first line to skip it since these are the column names and not the data we need
                    string line = reader.ReadLine();

                    //DR - splits each line and stores the value of the column 
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            //DR - splits the line into an array of values
                            string[] Currentline = line.Split(',');

                            int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                            //DR - add loan number to dictionary
                            if(!DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                DictionaryP1CSReport[reportDate][loanNumber] = new Dictionary<int, ArrayList>();
                            }

                            int tranCode = Convert.ToInt32(Currentline[2].Replace("\"", "").Trim().Split(' ')[0]);

                            //DR - add trancode to dictionary
                            if(!DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                DictionaryP1CSReport[reportDate][loanNumber][tranCode] = new ArrayList();
                            }

                            int cardcode = Convert.ToInt32(Currentline[2].Replace("\"", "").Trim().Split(' ')[7]);

                            //DR - add card code to arraylist
                            if (!DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(cardcode))
                            {
                                DictionaryP1CSReport[reportDate][loanNumber][tranCode].Add(cardcode);
                            }

                        }
                        catch (Exception ex)
                        {
                            LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "P1CS log"), EO5TCompletedPath);
                        }

                    }

                }
                catch (Exception ex)
                {
                    LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "P1CS log"), EO5TCompletedPath);
                }
            }
        }
    }
}
