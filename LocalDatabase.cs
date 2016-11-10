using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanReview
{
    public class LocalDatabase
    {

        private vUser _AppUser { get; set; }
        private LoanReviewEntities _AppEntity { get; set; }
        private int loanid;
        public SQLiteConnection dbConnection;
        SQLiteCommand command;
        private int workflowID;

        public LocalDatabase(vUser userLoginObject, LoanReviewEntities entityLoginObject, int LoanID, int WorkflowID)
        {
            _AppUser = userLoginObject;
            _AppEntity = entityLoginObject;
            loanid = LoanID;
            workflowID = WorkflowID;
        }


        public void LocalDBSetup()
        {
            try
            {
                //DR - get the appdata roaming directory
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string destinationFilePath = System.IO.Path.Combine(appData, "Loan Review\\LocalDBs");
                //DR - gets the localDB's filepath
                string unique = DateTime.Now.ToString("MM-d-yyyy-H-mm-ss");
                string sqlFileName = _AppUser.Id.ToString() + "_" + _AppUser.ActiveRole.ToString() + "_" + loanid.ToString() + "_" + workflowID + "_" + unique + ".sqlite";
                string sqlFilePath = System.IO.Path.Combine(destinationFilePath, sqlFileName);

                //DR - checks to see if directory exists already and creates the directory if it does not. 
                bool exists = System.IO.Directory.Exists(destinationFilePath);

                if (!exists)
                {
                    System.IO.Directory.CreateDirectory(destinationFilePath);
                }

                //DR - checks to see if localDB file exists already and creates the directory if it does not
                exists = System.IO.File.Exists(sqlFilePath);

                if (!exists)
                {
                    //MA:Create Local SQLITE database.
                    SQLiteConnection.CreateFile(sqlFilePath);
                }

                //MA:Connect to the created database.
                dbConnection = new SQLiteConnection("Data Source=" + sqlFilePath + ";Version=3;");

                //MA:Open the Connection to the database.
                dbConnection.Open();

                //MA:Stores the sql statement to create the table.
                string sql = "create table if not exists UserInputTable (LoanID int NOT NULL, UserID int NOT NULL, DocID int NOT NULL, FieldID int NOT NULL, Value varchar(500), UserTimeStamp datetime NOT NULL, InputID int NOT NULL, IsMatching bit NOT NULL, SelectedDocument varchar(128) NOT NULL, MSPValue varchar(500) not null, Workflow int NOT NULL, Reportable bit NOT NULL)";

                //MA:Assign the query and connection to the command to be executed.
                command = new SQLiteCommand(sql, dbConnection);

                //MA:Execute the the command.
                command.ExecuteNonQuery();
                command.Dispose();


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }
        public void LocalSaveUserInput(int LoanID, int UserID, int DocID, int FieldID, string Value, int InputID, int IsMatching, string SelectedDocument, int Workflow, string CurrentMspValue, int Reportable)
        {
            try
            {
                string sql = "Insert into UserInputTable (LoanID, UserID, DocID, FieldID, Value, UserTimeStamp, InputID, IsMatching, SelectedDocument, Workflow, MSPValue, Reportable) values (@LoanID, @UserID, @DocID, @FieldID, @Value, @UserTimeStamp, @InputID, @IsMatching, @SelectedDocument, @Workflow, @MSPValue, @Reportable)";
                command = new SQLiteCommand(sql, dbConnection);

                //MA:Assigns the value to the parameters needed in the Sql statement.
                command.Parameters.AddWithValue("@LoanID", LoanID);
                command.Parameters.AddWithValue("@UserID", UserID);
                command.Parameters.AddWithValue("@DocID", DocID);
                command.Parameters.AddWithValue("@FieldID", FieldID);
                command.Parameters.AddWithValue("@Value", Value);
                command.Parameters.AddWithValue("@UserTimeStamp", DateTime.Now);
                command.Parameters.AddWithValue("@InputID", InputID);
                command.Parameters.AddWithValue("@IsMatching", IsMatching);
                command.Parameters.AddWithValue("@SelectedDocument", SelectedDocument);
                command.Parameters.AddWithValue("@Workflow", Workflow);
                command.Parameters.AddWithValue("@MSPValue", CurrentMspValue);
                command.Parameters.AddWithValue("@Reportable", Reportable);
                command.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        //DR update the value for a specific field
        public void LocalUpdateUserInput(int LoanID, int UserID, int DocID, int FieldID, string Value, int InputID, int IsMatching, string SelectedDocument, int Workflow)
        {
            try
            {
                string sql = "Update UserInputTable set Value = @Value, SelectedDocument = @SelectedDocument, IsMatching = @IsMatching where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow";
                command = new SQLiteCommand(sql, dbConnection);

                //MA:Assigns the value to the parameters needed in the Sql statement.
                command.Parameters.AddWithValue("@LoanID", LoanID);
                command.Parameters.AddWithValue("@UserID", UserID);
                command.Parameters.AddWithValue("@DocID", DocID);
                command.Parameters.AddWithValue("@FieldID", FieldID);
                command.Parameters.AddWithValue("@Value", Value);
                command.Parameters.AddWithValue("@InputID", InputID);
                command.Parameters.AddWithValue("@IsMatching", IsMatching);
                command.Parameters.AddWithValue("@SelectedDocument", SelectedDocument);
                command.Parameters.AddWithValue("@Workflow", Workflow);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        //DR update the selected document for a specific field
        public void LocalUpdateSelectedDocument(int LoanID, int UserID, int DocID, int FieldID, int InputID, string SelectedDocument)
        {
            try
            {
                string sql = "Update UserInputTable set SelectedDocument = @SelectedDocument where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID";
                command = new SQLiteCommand(sql, dbConnection);

                //MA:Assigns the value to the parameters needed in the Sql statement.
                command.Parameters.AddWithValue("@LoanID", LoanID);
                command.Parameters.AddWithValue("@UserID", UserID);
                command.Parameters.AddWithValue("@DocID", DocID);
                command.Parameters.AddWithValue("@FieldID", FieldID);
                command.Parameters.AddWithValue("@InputID", InputID);
                command.Parameters.AddWithValue("@SelectedDocument", SelectedDocument);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        //DR update the reportability for a specific field
        public void LocalUpdateReportable(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Reportable)
        {
            try
            {
                string sql = "Update UserInputTable set Reportable = @Reportable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID";
                command = new SQLiteCommand(sql, dbConnection);

                //MA:Assigns the value to the parameters needed in the Sql statement.
                command.Parameters.AddWithValue("@LoanID", LoanID);
                command.Parameters.AddWithValue("@UserID", UserID);
                command.Parameters.AddWithValue("@DocID", DocID);
                command.Parameters.AddWithValue("@FieldID", FieldID);
                command.Parameters.AddWithValue("@InputID", InputID);
                command.Parameters.AddWithValue("@Reportable", Reportable);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        //DR - delete row from local database file
        public void LocalRemoveUserInput(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Workflow)
        {
            try
            {
                string sql = "delete from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow";
                command = new SQLiteCommand(sql, dbConnection);
                command.Parameters.AddWithValue("@LoanID", LoanID);
                command.Parameters.AddWithValue("@UserID", UserID);
                command.Parameters.AddWithValue("@DocID", DocID);
                command.Parameters.AddWithValue("@FieldID", FieldID);
                command.Parameters.AddWithValue("@InputID", InputID);
                command.Parameters.AddWithValue("@Workflow", Workflow);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        //DR - gets the count of userinputs to see if that row exists and returns true if it does
        public bool LocalDoesExist(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Workflow)
        {
            string sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            int count = int.Parse(command.ExecuteScalar().ToString());
            if (count == 0)
            {
                command.Dispose();
                return false;
            }
            command.Dispose();
            return true;
        }

        //DR - returns true if reportable; otherwise return false
        public bool LocalIsReportable(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Workflow, int Reportable)
        {
            bool isReportable = false;

            //DR - if it does not exist, it cannot be matching so return false
            string sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow and Reportable = @Reportable";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@Reportable", Reportable);
            int count = int.Parse(command.ExecuteScalar().ToString());

            if (count > 0)
            {
                isReportable = true;
            }

            command.Dispose();
            return isReportable;
        }

        //DR - returns true if matching; otherwise, return false
        public bool LocalIsMatching(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Workflow)
        {
            bool isMatching = false;

            //DR - if it does not exist, it cannot be matching so return false
            string sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow and IsMatching = @IsMatching";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@IsMatching", 1);
            int count = int.Parse(command.ExecuteScalar().ToString());

            if (count > 0)
            {
                isMatching = true;
            }

            command.Dispose();
            return isMatching;
        }

        //DR - returns true if field is overriden. Otherwise return false
        public bool LocalIsOverriden(int LoanID, int UserID, int DocID, int FieldID, int InputID, int Workflow)
        {
            bool isOverriden = false;

            string sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and Workflow = @Workflow and value = @Value";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@Value", "DocumentOverride");
            int count = int.Parse(command.ExecuteScalar().ToString());
            command.Parameters.Clear();
            if (count > 0)
            {
                command.Dispose();
                isOverriden = true;
                return isOverriden;
            }

            sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and value = @Value";
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@Value", "FieldOverride");
            count = int.Parse(command.ExecuteScalar().ToString());
            command.Parameters.Clear();

            if (count > 0)
            {
                command.Dispose();
                isOverriden = true;
                return isOverriden;
            }

            sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and value = @Value";
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@Value", "NotApplicable");
            count = int.Parse(command.ExecuteScalar().ToString());
            command.Parameters.Clear();

            if (count > 0)
            {
                command.Dispose();
                isOverriden = true;
                return isOverriden;
            }

            sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and DocID = @DocID and FieldID = @FieldID and InputID = @InputID and value = @Value";
            command.Parameters.AddWithValue("@LoanID", LoanID);
            command.Parameters.AddWithValue("@UserID", UserID);
            command.Parameters.AddWithValue("@DocID", DocID);
            command.Parameters.AddWithValue("@FieldID", FieldID);
            command.Parameters.AddWithValue("@InputID", InputID);
            command.Parameters.AddWithValue("@Workflow", Workflow);
            command.Parameters.AddWithValue("@Value", "ImageValidationTask");
            count = int.Parse(command.ExecuteScalar().ToString());
            command.Parameters.Clear();

            if (count > 0)
            {
                command.Dispose();
                isOverriden = true;
                return isOverriden;
            }

            return isOverriden;
        }

        //DR - returns an int for ismatching values
        public int LocalMatchingCount(int InputID, int IsMatching)
        {
            int matchingCount = 0;

            string sql = "select count(*) from UserInputTable where InputID = @InputID and IsMatching = @IsMatching";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@IsMatching", IsMatching);
            command.Parameters.AddWithValue("@InputID", InputID);
            matchingCount = int.Parse(command.ExecuteScalar().ToString());
            command.Parameters.Clear();

            return matchingCount;
        }

        //dr - transfers data from localuserinput to liveuserinput table
        public void TransferDataToLiveDB()
        {
            string sql = "select * from UserInputTable";
            command = new SQLiteCommand(sql, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                _AppEntity.SaveUserInput(Convert.ToInt32(reader["LoanID"]), Convert.ToInt32(reader["UserID"]), Convert.ToInt32(reader["DocID"]), Convert.ToInt32(reader["FieldID"]), Convert.ToDateTime(reader["UserTimeStamp"]), Convert.ToString(reader["Value"]), Convert.ToInt32(reader["InputID"]), Convert.ToInt32(reader["IsMatching"]), Convert.ToString(reader["SelectedDocument"]), Convert.ToInt32(reader["Workflow"]), Convert.ToString(reader["MSPValue"]), Convert.ToInt32(reader["Reportable"]));
            }
            reader.Dispose();
            command.Dispose();
        }

        //DR - transfers workflow from list to WorkflowTracking table
        public bool TransferWorkflowToLive(ArrayList imageValList)
        {
            bool isThereIVT = false;
            //DR - open up an imageval task for each document flagged as IVT
            foreach (object d in imageValList)
            {
                int doc = Convert.ToInt32(d);
                _AppEntity.InsertWorkflow(loanid, 2, doc);
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 12, workflowID);

                isThereIVT = true;
            }
            return isThereIVT;
        }

        //DR - checks to see if all data from localDB was transfered to liveDB
        public bool WasUserInputTransferSuccessful(int inputID)
        {
            string sql = "select count(*) from UserInputTable where LoanID = @LoanID and UserID = @UserID and InputID = @InputID and Workflow = @Workflow";
            command = new SQLiteCommand(sql, dbConnection);
            command.Parameters.AddWithValue("@LoanID", loanid);
            command.Parameters.AddWithValue("@UserID", _AppUser.Id);
            command.Parameters.AddWithValue("@InputID", inputID);
            command.Parameters.AddWithValue("@Workflow", workflowID);
            //DR - gets the count of userinputs from localDB
            int localDBCount = int.Parse(command.ExecuteScalar().ToString());
            //DR - gets the count of userinputs from the liveDB
            int liveDBCount = (from l in _AppEntity.UserInputs.AsNoTracking() where l.LoanID == loanid && l.UserID == _AppUser.Id && l.InputID == inputID && l.Workflow == workflowID select l.LoanID).Count();

            //DR - if counts match, transfer was successful and return true
            if (localDBCount == liveDBCount)
            {
                command.Dispose();
                return true;
            }
            command.Dispose();
            return false;
        }
        //DR - checks to see if all tasks from imageValTask was transfered to WorkflowTracking table 
        public bool WasWorkflowTransferSuccessful(int workflowID, ArrayList imageValList)
        {

            //Dr - gets a count of tasks opened from local imageValList
            int localTrackingCount = imageValList.Count;

            var query = from l in _AppEntity.WorkflowTrackings.AsNoTracking() where l.LoanID == loanid && l.WorkflowID == workflowID && l.WorkflowTier == 1 select l;

            //DR - tracks a count of tasks open from live WorkflowTracking table
            int liveTrackingCount = query.Count();

            //DR - checks to see that the correct documents were added to the workflowtracking table
            foreach (var l in query)
            {
                //DR - if there's a document in the workflow table that is not in the imagevallist for a loan and workflow, return false
                if (!imageValList.Contains(l.DocumentID))
                {
                    return false;
                }
            }

            //DR - if counts match, transfer was successful and return true
            if (localTrackingCount == liveTrackingCount)
            {
                return true;
            }
            return false;
        }


        //DR - deletes all data from localuserinput table
        public void DeleteLocalDBUserInput()
        {
            //DR - Commented out for now so we can retain the data for testing purposes

            //string sql = "delete from UserInputTable";
            //command = new SQLiteCommand(sql, dbConnection);
            //command.ExecuteNonQuery();
        }

        public void TransferDocTicksToLive(Dictionary<String, ulong> DocTickDictionary)
        {
            int tier;
            //DR - determines what the tier is
            switch (_AppUser.ActiveRole)
            {
                case 1:
                    tier = 1;
                    break;
                case 2:
                    tier = 2;
                    break;
                case 3:
                    tier = 3;
                    break;
                default:
                    tier = 0;
                    break;
            }
            //DR - inserts all the ticks into the liveDB's Document Timers table
            foreach (var tick in DocTickDictionary)
            {
                string docName = tick.Key;
                //DR - query for the docId
                var query = (from doc in _AppEntity.vDocuments.AsNoTracking() where doc.Name == docName select doc.Id).Single();

                int docID = Convert.ToInt32(query);

                _AppEntity.DocTimerInsert(loanid, _AppUser.Id, docID, tier, Convert.ToInt32(tick.Value));
            }
        }
    }
}
