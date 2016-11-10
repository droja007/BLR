using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace LoanReviewAutomation
{
    //DR - this class reads the file and transfers the data to the passed in Dictionary
    class CardCode
    {
        StringBuilder logBuilder = new StringBuilder();
        bool hasLog = false;

        //DR - adds the line and file which was unable to be parsed
        public void logLine(string file, string line)
        {
            logBuilder.AppendLine("-------------------------------------------------------------------------------------");
            logBuilder.AppendLine("Unable to parse line in file :: " + file);
            logBuilder.AppendLine("Line :: " + line);
            logBuilder.AppendLine("-------------------------------------------------------------------------------------");
        }

        public string getLogToString()
        {
            return logBuilder.ToString();
        }
        public bool getHasLog()
        {
            return hasLog;
        }

        public void CardCode1(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
            }

            //DR - path for the csv file
            string path = file;
            Console.WriteLine("Parsing data for: " + path);

            //DR - Makes a streamreader from the file
            Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(stream);

            //DR - reads the first line to skip it since these are the column names and not the data we need
            string line = reader.ReadLine();

            if (tranCode == 502)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 20)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if(DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if(DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if(DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if(DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(1))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[19].Replace("\"", "").Trim());
                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ACT_CD502 = Currentline[4].Replace("\"", "").Trim();
                        //DR - the change date may not exist so set it to min value if it fails validation
                        DateTime test = DateTime.MinValue;
                        if (DateTime.TryParse(Currentline[5].Replace("\"", "").Trim(), out test))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CTL_EFF_DATE = test;
                        }
                        else
                        {
                            test = DateTime.MinValue;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CTL_EFF_DATE = test;
                        }
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ARM_PLAN = Currentline[6].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECALC_DAYS = Currentline[7].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_LD_TIME = Currentline[8].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_LD_TIME = Currentline[9].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CV_LD_TIME = Currentline[10].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BT_NO = Currentline[11].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_EFF_DT_NXT_NOT = Currentline[12].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SEP_IR_PI_NOT = Currentline[13].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ARM_CD = Currentline[14].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ENT_UPD_ID = Currentline[15].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].DATA_VERF = Currentline[16].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SYS_ID502 = Currentline[17].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ORIG_LTERM = Currentline[18].Replace("\"", "").Trim();
                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }
            }
            else if (tranCode == 504)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 20)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if (DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(1))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[19].Replace("\"", "").Trim());

                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - if it's only made up of dashes, replace with Remove
                        for (int i = 4; i < 19; i++ )
                        {

                            if (Currentline[i].Replace("\"", "").Trim().Distinct().Count() == 1 && Currentline[i].Replace("\"", "").Trim().Contains("-"))
                            {
                                Currentline[i] = "Remove";
                            }
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ACT_CD504 = Currentline[4].Replace("\"", "").Trim();
                        //DR - the change date may not exist so set it to min value if it fails validation
                        DateTime test = DateTime.MinValue;
                        if (DateTime.TryParse(Currentline[5].Replace("\"", "").Trim(), out test))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CHANGE_DATE = test;
                        }
                        else
                        {
                            test = DateTime.MinValue;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CHANGE_DATE = test;
                        }
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_TYPE = Currentline[6].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_AMOUNT = Currentline[7].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INTEREST_TYPE = Currentline[8].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INTEREST_RATE = Currentline[9].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SERVICE_FEE_TYPE = Currentline[10].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SERVICE_FEE_AMT_RATE = Currentline[11].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REP_RES = Currentline[12].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ENTRY_ID = Currentline[13].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SYS_ID504 = Currentline[4].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PAY_OPT = Currentline[15].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REASON_PI = Currentline[16].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REASON_IR = Currentline[17].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REASON_REP_RES = Currentline[18].Replace("\"", "").Trim();
                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }
            }
        }
        public void CardCode2(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            if (tranCode == 502)
            {

            }
            else if (tranCode == 504)
            {

            }
        }
        public void CardCode3(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
            }

            //DR - path for the csv file
            string path = file;
            Console.WriteLine("Parsing data for: " + path);

            //DR - Makes a streamreader from the file
            Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(stream);

            //DR - reads the first line to skip it since these are the column names and not the data we need
            string line = reader.ReadLine();

            if (tranCode == 502)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 7)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if (DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(3))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[6].Replace("\"","").Trim());
                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CONTROL_TYPE = Currentline[4].Replace("\"","").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CONTROL_DESCRIPTION = Currentline[5].Replace("\"", "").Trim();

                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }


            }
            else if (tranCode == 504)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 15)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if (DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(3))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[14].Replace("\"", "").Trim());
                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - if it's only made up of dashes, replace with Remove
                        for (int i = 4; i < 14; i++)
                        {

                            if (Currentline[i].Replace("\"", "").Trim().Distinct().Count() == 1 && Currentline[i].Replace("\"", "").Trim().Contains("-"))
                            {
                                Currentline[i] = "Remove";
                            }
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].COUNTY_TAX = Currentline[4].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].COUNTY_RSN = Currentline[5].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CITY_TAX = Currentline[6].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].CITY_RSN = Currentline[7].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].HAZ_PREM = Currentline[8].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].HAZ_RSN = Currentline[9].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MIP_MIP = Currentline[10].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MIP_RSN = Currentline[11].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIEN_LIEN = Currentline[12].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIEN_RSN = Currentline[13].Replace("\"", "").Trim();

                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }
            }
        }
        public void CardCode4(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
            }

            //DR - path for the csv file
            string path = file;
            Console.WriteLine("Parsing data for: " + path);

            //DR - Makes a streamreader from the file
            Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(stream);

            //DR - reads the first line to skip it since these are the column names and not the data we need
            string line = reader.ReadLine();

            if (tranCode == 502)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 17)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if (DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(4))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[16].Replace("\"", "").Trim());
                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - if it's only made up of dashes, replace with Remove
                        for (int i = 4; i < 16; i++)
                        {

                            if (Currentline[i].Replace("\"", "").Trim().Distinct().Count() == 1 && Currentline[i].Replace("\"", "").Trim().Contains("-"))
                            {
                                Currentline[i] = "Remove";
                            }
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ORIG_IR_CHG_DATE = Currentline[4].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BT_IR_CHG = Currentline[5].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NEXT_IR_EFF_CALC = Currentline[6].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CHG_LETTER = Currentline[7].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_NOT_LEAD_TIME_MONTH = Currentline[8].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_NOT_LEAD_TIME_DAYS = Currentline[9].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ORIG_PI_CHG_DATE = Currentline[10].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BT_PI_CHG = Currentline[11].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NEXT_PI_EFF_CALC = Currentline[12].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_NOT_LEAD_TIME_MONTH = Currentline[13].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_NOT_LEAD_TIME_DAYS = Currentline[14].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CHG_LETTER = Currentline[15].Replace("\"", "").Trim();
                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }


            }
            else if (tranCode == 504)
            {
                //DR - splits each line and stores the value of the column 
                while ((line = reader.ReadLine()) != null)
                {
                    //DR - splits the line into an array of values
                    string[] Currentline = line.Split(',');

                    //DR - checks to see if the line has the appropriate amount of fields.
                    bool fieldCheck = false;
                    if (Currentline.Length == 11)
                    {
                        fieldCheck = true;
                    }
                    else fieldCheck = false;

                    //DR - if the line has the correct amount of fields then execute rest of code
                    if (fieldCheck)
                    {
                        int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                        //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                        if (DictionaryP1CSReport.ContainsKey(reportDate))
                        {
                            if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                                {
                                    if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(4))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        //DR - add loan number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                        }

                        int recordNumber = Convert.ToInt32(Currentline[10].Replace("\"", "").Trim());
                        //DR - add record number to dictionary if it hasn't been added already
                        if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                        {
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                            DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                        }

                        //DR - set the fields
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_INDEX = Currentline[4].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CARRY_OVER = Currentline[5].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PAYMENT_RATE = Currentline[6].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PR_INDEX = Currentline[7].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PR_CARRY_OVER = Currentline[8].Replace("\"", "").Trim();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CARRY_DIFF = Currentline[9].Replace("\"", "").Trim();
                    }
                    else
                    {
                        //DR - add line to log
                        logLine(file, line);
                        hasLog = true;
                    }
                }
            }
        }
        public void CardCode5(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            
        }
        public void CardCode6(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 25)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(6))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[24].Replace("\"", "").Trim());
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CAL_MTH = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_RNDING_TYP = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_RNDING_BASIS = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MARGIN = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_AVG_CD = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NMB_IR_VAL = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_FST_IR = Currentline[10].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BTW_IR = Currentline[11].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CARRY_OVER_CD = Currentline[12].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_CARRY_OVER_AMOUNT = Currentline[13].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_CODE = Currentline[14].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_LEAD_TIME_MONTH = Currentline[15].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_LEAD_TIME_DAYS = Currentline[16].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_AVG_CD = Currentline[17].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_NMB_VAL = Currentline[18].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_PMT_FST = Currentline[19].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_PMT_BTW = Currentline[20].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDEX_CALC_MTH = Currentline[21].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ORIG_IR = Currentline[22].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ALT_NG = Currentline[23].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode7(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 20)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0].Replace("\"", "").Trim());

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(7))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[19].Replace("\"", "").Trim());
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_MAX_IR_INCR = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_MAX_IR_DECR = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_MIN_IR_INCR = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_MIN_IR_DECR = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BTW_NO_IR_CAP = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDX_CD_A = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDX_LD_TIME_A_MONTH = Currentline[10].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDX_LD_TIME_A_DAYS = Currentline[11].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDX_AVG_CD = Currentline[12].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].INDX_VAL = Currentline[13].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_1ST_INDX_VAL = Currentline[14].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BTW_INDX_VAL = Currentline[15].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_INDX = Currentline[16].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_IX_RND_TYP = Currentline[17].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_IX_RND_BAS = Currentline[18].Replace("\"", "").Trim();
                    
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode8(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 12)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[11]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_1_MAX_IR_INCREASE = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_1_MAX_IR_DECREASE = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_1_BASE_RATE_CHANGE = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_1_BASE_RATE_NUM_PMT = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_1_BASE_RATE_NXT_SEL_DATE = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NXT_NO_IR_CAP_DATE = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMTS_TO_IR_CAP_BASE_RATE = Currentline[10].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode9(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 14)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[13]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_2_MAX_IR_INCREASE = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_2_MAX_IR_DECREASE = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_2_BASE_RATE_CHANGE = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_2_BASE_RATE_NUM_PMT = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PER_CHG_PERIODIC_2_BASE_RATE_NXT_SEL_DATE = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_MAX_IR_INC_PCT = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_MAX_IR_INC_AMT = Currentline[10].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_MAX_IR_DEC_PCT = Currentline[11].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_MAX_IR_DEC_AMT = Currentline[12].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode10(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 19)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[18]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].SKIP_IR = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_1_CALC_MTH = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_1_NMB_CHG = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_1_PCT_INC = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_1_PMT_CHG = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_1_AMTZ_MAX = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PRN_CAL_MTH = Currentline[10].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_CALC_MTH = Currentline[11].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_NMB_CHG = Currentline[12].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_PCT_INC = Currentline[13].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_PMT_CHG = Currentline[14].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_AMTZ_MAX = Currentline[15].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_2_INIT_OPT_CALC_PCT = Currentline[16].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].ORIG_PI = Currentline[17].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode11(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 12)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[11]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_3_CALC_MTH = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_3_FLOOR_MARGIN = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_CALCULATION_FIELDS_3_AMTZ_MAX = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_OF_LOAN_PI_LIMITS_MAX_PCT = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_OF_LOAN_PI_LIMITS_MAX_AMT = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_OF_LOAN_PI_LIMITS_MIN_PCT = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LIFE_OF_LOAN_PI_LIMITS_MIN_AMT = Currentline[10].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode12(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 11)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[10]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_PERCENT_CHANGE_REQUIREMENTS_MAX_INC = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_PERCENT_CHANGE_REQUIREMENTS_MAX_DEC = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_PERCENT_CHANGE_REQUIREMENTS_MIN_INC = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PI_PERCENT_CHANGE_REQUIREMENTS_MIN_DEC = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PMT_BTW_NO_PI_CAPS = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NXT_NO_PI_CAP_DATE = Currentline[9].Replace("\"", "").Trim();

                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode13(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {
            //DR - Date(string) -> LoanNumber -> Record Number -> EO5T row. Ex. DictionaryEO5TReport[7/15/16][1234][1] returns EO5T
            //DR - add report date to dictionary if it hasn't been added already
            if (!DictionaryEO5TReport.ContainsKey(reportDate))
            {
                DictionaryEO5TReport[reportDate] = new Dictionary<int, Dictionary<int, EO5T_Report>>();
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
                //DR - splits the line into an array of values
                string[] Currentline = line.Split(',');

                //DR - checks to see if the line has the appropriate amount of fields.
                bool fieldCheck = false;
                if (Currentline.Length == 16)
                {
                    fieldCheck = true;
                }
                else fieldCheck = false;

                //DR - if the line has the correct amount of fields then execute rest of code
                if (fieldCheck)
                {
                    int loanNumber = Convert.ToInt32(Currentline[0]);

                    //DR - if the report date, loan number, tran code, and card code exist in the P1CS reports skip the line
                    if (DictionaryP1CSReport.ContainsKey(reportDate))
                    {
                        if (DictionaryP1CSReport[reportDate].ContainsKey(loanNumber))
                        {
                            if (DictionaryP1CSReport[reportDate][loanNumber].ContainsKey(tranCode))
                            {
                                if (DictionaryP1CSReport[reportDate][loanNumber][tranCode].Contains(12))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    //DR - add loan number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate].ContainsKey(loanNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber] = new Dictionary<int, EO5T_Report>();
                    }

                    int recordNumber = Convert.ToInt32(Currentline[15]);
                    //DR - add record number to dictionary if it hasn't been added already
                    if (!DictionaryEO5TReport[reportDate][loanNumber].ContainsKey(recordNumber))
                    {
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber] = new EO5T_Report();
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].LOAN_NUMBER = loanNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].REPORT_DATE = Convert.ToDateTime(reportDate);
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].RECORD_NUMBER = recordNumber;
                        DictionaryEO5TReport[reportDate][loanNumber][recordNumber].TRAN_CODE = tranCode;
                    }

                    //DR - set the fields
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].PRN_BAL_CAP_PCT = Currentline[4].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].FULL_AM_IR_CD = Currentline[5].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MAX_PRN_BAL = Currentline[6].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NEG_AM_CD_IR_CAL = Currentline[7].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].NEG_AM_CD_LETTER = Currentline[8].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MAX_TERM_X_PCT = Currentline[9].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].MAX_TERM_X_PMT = Currentline[10].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].IR_PI_ADJ_CD = Currentline[11].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].DEFERED_INTEREST_PI_FLG = Currentline[12].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].DEFERED_INTEREST_MAX_INC = Currentline[13].Replace("\"", "").Trim();
                    DictionaryEO5TReport[reportDate][loanNumber][recordNumber].DEFERED_INTEREST_MAX_DEC = Currentline[14].Replace("\"", "").Trim();
                }
                else
                {
                    //DR - add line to log
                    logLine(file, line);
                    hasLog = true;
                }
            }
        }
        public void CardCode14(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {

        }
        public void CardCode15(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {

        }
        public void CardCode16(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {

        }
        public void CardCode17(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {

        }
        public void CardCode18(string reportDate, int tranCode, ref Dictionary<string, Dictionary<int, Dictionary<int, EO5T_Report>>> DictionaryEO5TReport, ref Dictionary<string, Dictionary<int, Dictionary<int, ArrayList>>> DictionaryP1CSReport, string file)
        {

        }
    }
}
