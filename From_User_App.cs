using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Threading;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Configuration;

namespace LoanReview
{
    public partial class Form_User_App : Form
    {
        #region CONSTRUCTORS


        public Form_User_App(vUser userLoginObject, LoanReviewEntities entityLoginObject)
        {
            //MA - Stores the User information from the login.
            _AppUser = userLoginObject;

            //MA - References and stores the tables, views and stored procedures for Imaging Apps database
            _AppEntity = entityLoginObject;
            InitializeComponent();
        }
        #endregion

        #region PRIVATE_MEMBERS

        private vUser _AppUser { get; set; }
        private LoanReviewEntities _AppEntity { get; set; }
        private ArrayList docList; //DR - holds our list of documents
        private ArrayList imageValList; //DR - holds a list of documents flagged as IVT
        private Dictionary<String, Dictionary<int, object>> DatabaseDictionary; //DR - DB Data : Tabs->FieldIDs->Value.
        private Dictionary<String, Dictionary<int, object>> UserInputDictionary; //MA: Store values of loan from current User.
        private Dictionary<String, Dictionary<int, object>> UserTier1Dictionary; //MA: Store values from loan in tier 1.
        private Dictionary<String, Dictionary<int, object>> UserTier2Dictionary; //MA: Store values from loan in tier 2.
        private Dictionary<int, Dictionary<Control, ArrayList>> FieldDependencyDictionary; //DR - DocID->ParentID->FieldControlChild
        private Dictionary<int, Dictionary<int, ComboBox>> DocumentListDictionary; //DR - DocID->FieldID->ComboBox, stores all of our fields DocumentList ComboBox for SelectedDocument
        private Dictionary<int, Dictionary<int, CheckBox>> fieldCheckDictionary; //DR - DocID->FieldID->CheckBox, stores the checkboxes to override a field
        private Dictionary<int, Dictionary<int, bool>> FieldHasPromptDictionary; //DR- DocID->FieldID->Bool, returns true if a field has a prompt message
        private Dictionary<int, Dictionary<int, string>> FieldPromptMessageDictionary; //DR- DocID->FieldID->Bool, returns a prompt message for a field
        private Dictionary<int, Dictionary<int, Label>> FieldLabelDictionary; //DR - DocID->FieldID->Label
        private Dictionary<int, Dictionary<int, ArrayList>> FieldDependenciesDictionary;
        private Dictionary<int, Dictionary<int, ArrayList>> RedundantFieldsDictionary; //DR - Major DocID -> Major FieldID -> ArrayList<Minor DocID, Minor FieldID>
        private Dictionary<string, bool> TabPageContainsWarningDictionary; //MA - Stores whether a Document contains a warning message or not
        private Dictionary<string, string> TabPageWarningPromptDictionary; //MA - Stores the warning messages to display for each individual document

        private Dictionary<String, CheckBox> docCheckDictionary; //DR - stores the checkboxes to override a doc page
        private Dictionary<String, TabPage> uncheckedDictionary; //DR- list of docs which haven't had the checkAnswers(submitbtn) execute
        private Dictionary<String, TabPage> checkedDictionary; //DR -  list of docs which have had the checkAnswers(submitbtn) execute
        private Dictionary<String, TabPage> shortcutDictionary; //DR - list of shortcuts with their docs
        private Dictionary<String, System.Windows.Forms.Timer> DocTimerDictionary;
        private Dictionary<String, ulong> DocTickDictionary;
        private Dictionary<int, ArrayList> FieldNAByDefaultDictionary; //DR- DocID -> ArrayList of controls to manually fieldOverride with value NotApplicable
        private Dictionary<int, int> fieldCountDictionary; //DR list of field counts per document
        private Dictionary<int, bool> fieldCriticalCheckDictionary; //DR - FieldID->IsCritical, pass in a fieldID and it will return true if critical field

        private int loanid; //MA - Stores loan id
        private int loanNum; //MA - Stores the Loan Number
        private int workflowID = 1; //DR - determines the workflow for userinputs
        private int UnmatchedCount = 0; //MA: Used to hold the amount of any errors when comparing.   
        private int loanCompletedCount; //DR- counts the amount of loans completed for the session
        private int tabIndex = 1; //DR - used for tabindexing
        private int row = 0; //DR - used for tablePanel rows
        private int checkAnswersPromptMessageCount; //DR - holds the number of promptmessages that need to be displayed for checkAnswers
        private bool nonNumberEntered = false; //DR - used for number value textboxes
        private bool isLoggingOut = false; 
        private bool finalSubmitClicked = false; //DR - to check if a loan was submitted
        private bool checkDBConnection = false; //DR - used for CheckDBConnection
        private bool ConnectionImage = false; //MA: Used to determine what image to display for the database connection.
        private bool tier3HasNonCritical = false; //DR - Flags the loan to escalate to Tier 4 automatically based on Tier 2 non critical input
        private bool isTerminated = false; //DR - gets set to true when application will terminate
        private bool checkAnswersBool = false; //DR - to pop the dialog box if certain fields are not matching
        private string checkAnswersPromptMessageString = ""; //DR- holds the string of all the promptmessages we need to display to the user if certain fields are not matching
        private string PaymentDueDate = "";
        private string DealID;
        private System.Timers.Timer loanTimer; //DR - to run a check on loan availability every X amount in time
        private SQLiteCommand command; //MA - References a query used on the local database
        private Form_Progress_Loading objfrmShowProgress; //MA- Used to reference the loading form
        private LocalDatabase LocalDatabase; //MA- Used to reference the local sqlite database
        private ToolTip toolTips; //DR - holds tooltips
        private Control _currentToolTipControl = null;

        #endregion PRIVATE_MEMBERS

        #region PUBLIC_MEMBERS

        //public bool isMessageBoxOpen = false;

        #endregion PUBLIC_MEMBERS

        #region METHODS

        /// <summary>
        /// MA - Loads the dictionaries for Tier 1 and/or Tier 2 inputs
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="field">Field ID</param>
        /// <param name="InputCode">Input ID Code</param>
        private void LoadUserInputDictionary(int documentId, int field, int InputCode)
        {
            //MA - Queries the database base on the Tier(Input Code for Tier) information needed
            switch (InputCode)
            {

                case 2:
                    //MA - Tier 1 query
                    var FieldValue2 = (from vDF in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                       where vDF.LoanID == loanid &&
                                       vDF.isMatching == false &&
                                       vDF.InputID == 2 &&
                                       vDF.DocID == documentId &&
                                       vDF.FieldID == field &&
                                       vDF.Workflow == workflowID
                                       select vDF.Value).Single();

                    //MA - Stores the value of User in Tier 1 in the dictionary 
                    UserTier1Dictionary[(GetDocumentName(documentId))][field] = FieldValue2.ToString();
                    break;

                case 3:
                    
                    //MA - Tier 2 query
                    var FieldValue3 = from vDF in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                      where vDF.LoanID == loanid &&
                                      vDF.isMatching == false &&
                                      vDF.InputID == 3 &&
                                      vDF.DocID == documentId &&
                                      vDF.FieldID == field &&
                                       vDF.Workflow == workflowID
                                      select vDF.Value;

                    foreach (var value in FieldValue3)
                    {
                        //MA - Stores the value of User in Tier 2 in the dictionary 
                        UserTier2Dictionary[(GetDocumentName(documentId))][field] = value.ToString();
                    }
                    break;
                    
            }
        }

        /// <summary>
        /// MA - Retrieves the name of a document based on its ID. 
        /// </summary>
        /// <param name="documentID">Document ID</param>
        /// <returns>Document Name</returns>
        private string GetDocumentName(int documentID)
        { 
            var DocName = (from Documents in _AppEntity.vDocuments.AsNoTracking()
                           where Documents.Id == documentID
                           select Documents.Name).Single();

            return DocName.ToString();
        }

        //DR - returns the document id for a given document
        private int GetDocumentID(string documentName)
        {
            var docID = (from Documents in _AppEntity.vDocuments.AsNoTracking()
                         where Documents.Name == documentName
                         select Documents.Id).Single();

            return Convert.ToInt32(docID);
        }

        /// <summary>
        /// MA - Retrieves the ID of a Field based on its Name
        /// </summary>
        /// <param name="FieldID">Field ID</param>
        /// <returns>Field Name</returns>
        private string GetFieldName(int FieldID)
        {
            var fieldname = (from fields in _AppEntity.Fields.AsNoTracking()
                         where fields.Id == FieldID
                         select fields.Name).Single();

            return fieldname;

        }

        /// <summary>
        /// MA - Retrieves the value of field from database
        /// </summary>
        /// <param name="DocID">Document ID</param>
        /// <param name="FieldID">Field ID</param>
        /// <returns>MSP Value</returns>
        private string GetCurrentMSPValue(int DocID, int FieldID)
        {
            string docName = GetDocumentName(DocID);
            string MSPValue;

            //MA - If the document exist in the dictionary store the value if not set as null
            if (DatabaseDictionary[docName][FieldID] != null)
            {
                MSPValue = DatabaseDictionary[docName][FieldID].ToString();
            }
            else
            {
                MSPValue = "NULL";
            }
            return MSPValue;
        }

        //DR - returns a Mask based on the type passed in
        private string GetMask(string ClientDataType)
        {
            switch (ClientDataType.ToLower())
            {
                case "double":
                    return "############################################################.##";
                case "int":
                    return "############################################################";
                case "phone":
                    return "(000)-000-0000";
                case "date":
                    return "00/00/0000";
                case "mmyy":
                    return "00/00";
                case "zip":
                    return "#####-####";
                case "ssn":
                    return "000000000 ";
                case "tin":
                    return "00-0000000";
                case "ssn/tin":
                    return "CCCCCCCCCC";
                case "proptype":
                    return "AA";
                case "prepaymentpenalty":
                    //DR - the @ must come before the string or else the code will fail to execute
                    return @"Pre-p\ayment pen\alty expired:(00/00/0000)";
                default:
                    return "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
            }
        }

        //DR - Returns only the number from the end of the string
        private int GetFieldID(string input)
        {
            double output;

            //DR - if it fails to parse return 0
            if (!Double.TryParse(Regex.Match(input, @"(\d+)$").Value, out output))
            {
                return 0;
            }
            return Convert.ToInt32(output);
        }

        //DR - puts a space inbetween camel casing. Example: RemoveCamelCasing >>> Remove Camel Casing
        private string RemoveCamelCasing(string CamelCase)
        {
            string s = Regex.Replace(Regex.Replace(CamelCase, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");

            return s;
        }

        //DR - removes duplicate characters from a string
        static string RemoveDuplicateChars(string key)
        {
            //DR- Removes duplicate chars using char arrays.
            int keyLength = key.Length;

            //DR- Store encountered letters in this array.
            char[] table = new char[keyLength];
            int tableLength = 0;

            //DR- Store the result in this array.
            char[] result = new char[keyLength];
            int resultLength = 0;

            //DR- Loop through all characters
            foreach (char value in key)
            {
                //DR- Scan the table to see if the letter is in it.
                bool exists = false;
                for (int i = 0; i < tableLength; i++)
                {
                    if (value == table[i])
                    {
                        exists = true;
                        break;
                    }
                }
                //DR- If the letter is new, add to the table and the result.
                if (!exists)
                {
                    table[tableLength] = value;
                    tableLength++;

                    result[resultLength] = value;
                    resultLength++;
                }
            }
            //DR- Return the string at this range.
            return new string(result, 0, resultLength);
        }

        //DR - Creates a control based on the controlType and adds it to the tablePanel.
        //DR - The created control's name will always be DOCUMENTID_FieldControlCONTROLTYPE_FIELDID. Example: 1_FieldControlTextbox_2
        private Control CreateFieldControl(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, int column, int overrideColumn)
        {
            //DR - gets field dependencies if any
            var FieldDependencies = from FD in _AppEntity.vFieldDependencies.AsNoTracking()
                                    where FD.DocID == documentId && FD.ParentID == field.FieldId
                                    select FD;

            // ES - Currently not using.
            var DocumentDependencies = from DD in _AppEntity.Fields_CrossDocument_Dependency.AsNoTracking()
                                       where DD.DocID == documentId
                                       && DD.FieldID == field.FieldId
                                       select DD;

            //DR - gets field values if any
            var FieldValues = from FV in _AppEntity.vFieldValues.AsNoTracking()
                              where FV.DocID == documentId && FV.FieldID == field.FieldId
                              select FV;

            Control control = null;

            //DR - create drop down with document names
            ComboBox documentList = new ComboBox()
            {
                Name = documentId + "_DocumentLocationComboBox_" + field.FieldId,
                //AutoSize = true,
                Height = 75,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Left,
                TabStop = false,
                Margin = new Padding(20, 3, 0, 3)
            };
            foreach (string docName in docList)
            {
                documentList.Items.Add(docName);
            }

            //DR - add extra documents according to David
            documentList.Items.Add("Bankruptcy Plan");
            documentList.Items.Add("Death Certificate");
            documentList.Items.Add("Miscellaneous/other");
            documentList.Items.Add("Property Inspection Report");
            documentList.Items.Add("Broker Price Opinion");
            documentList.Items.Add("Adjustment of Terms");
            documentList.Items.Add("Rate Reduction Rider");
            documentList.Items.Add("Assumption");

            documentList.SelectedItem = field.DocumentName;
            tablePanel.Controls.Add(documentList, column + 1, row);
            //DR - add the documentList to the dictionary
            DocumentListDictionary[documentId][field.FieldId] = documentList;

            switch (_AppUser.ActiveRole)
            {
                case 1:
                    //DR- if field has promp set the dictionary entry to true  
                    if (field.HasPrompt)
                    {
                        FieldHasPromptDictionary[documentId][field.FieldId] = true;

                        if (!FieldPromptMessageDictionary.ContainsKey(documentId))
                        {
                            FieldPromptMessageDictionary[documentId] = new Dictionary<int, string>();
                        }
                        //DR- add the prompt message to the dictionary
                        FieldPromptMessageDictionary[documentId][field.FieldId] = field.PromptMessage;
                    }
                    else
                    {
                        FieldHasPromptDictionary[documentId][field.FieldId] = false;
                    }

                    //DR - get RedundantFields if any
                    var RedundantFields = from RF in _AppEntity.vRedundantFields.AsNoTracking()
                                          where RF.MajorDocID == documentId && RF.MajorFieldID == field.FieldId
                                          select RF;

                    //DR - adds each redundantfield to the dictionary
                    foreach (var rf in RedundantFields)
                    {
                        if (!RedundantFieldsDictionary.ContainsKey(rf.MajorDocID))
                        {
                            RedundantFieldsDictionary[rf.MajorDocID] = new Dictionary<int, ArrayList>();
                        }
                        if (!RedundantFieldsDictionary[rf.MajorDocID].ContainsKey(rf.MajorFieldID))
                        {
                            RedundantFieldsDictionary[rf.MajorDocID].Add(rf.MajorFieldID, new ArrayList());
                        }

                        RedundantFieldsDictionary[rf.MajorDocID][rf.MajorFieldID].Add(new Tuple<int, int>(rf.MinorDocID, rf.MinorFieldID));
                    }

                    break;
            }


            //DR - determines what control to create based on the field's control type
            switch (field.ControlType.ToLower())
            {
                case "textbox":
                    control = CreateFieldControlTextBox(field, tablePanel, FieldDependencies, DocumentDependencies, FieldValues, documentId, column, overrideColumn);
                    break;
                case "groupbox":
                    control = CreateFieldControlGroupBox(field, tablePanel, FieldDependencies, DocumentDependencies, FieldValues, documentId, column, overrideColumn);
                    break;
                case "checkbox":
                    control = CreateFieldControlCheckBox(field, tablePanel, FieldDependencies, DocumentDependencies, FieldValues, documentId, column, overrideColumn);
                    break;
                case "combobox":
                    control = CreateFieldControlComboBox(field, tablePanel, FieldDependencies, DocumentDependencies, FieldValues, documentId, column, overrideColumn);
                    break;
            }
            return control;
        }

        //DR- Creates a field control of type MaskedTextBox
        private MaskedTextBox CreateFieldControlTextBox(vDocuments_Fields field, TableLayoutPanel tablePanel, IQueryable<vFieldDependency> FieldDependencies, IQueryable<Fields_CrossDocument_Dependency> DocumentDependencies, IQueryable<vFieldValue> FieldValues, int documentId, int column, int overrideColumn)
        {
            MaskedTextBox maskedTextBox = new MaskedTextBox()
            {
                Name = documentId + "_FieldControlTextBox_" + field.FieldId,
                AutoSize = true,
                Dock = DockStyle.Left,
                PromptChar = ' ',
                HidePromptOnLeave = true,
                TabIndex = tabIndex++,
                TabStop = true,
                Width = 200
            };

            //DR- determines whether the maskedTextBox uses a mask or an event handler to validate key presses
            switch (field.Client_Data_Type.ToLower())
            {
                case "double":
                    maskedTextBox.KeyDown += new KeyEventHandler((s, e) => MaskedTextBox_KeyDown(s, e));
                    maskedTextBox.KeyPress += new KeyPressEventHandler((s, e) => MaskedTextBox_KeyPress(s, e));
                    break;
                case "phone":
                    maskedTextBox.Click += new EventHandler((s, e) => maskedTextBox_Clicked(s, e, maskedTextBox, 1));
                    maskedTextBox.Mask = GetMask(field.Client_Data_Type); //DR - passes in the type to get the mask
                    break;
                case "date":
                case "ssn":
                case "zip":
                case "mmyy":
                    maskedTextBox.Click += new EventHandler((s, e) => maskedTextBox_Clicked(s, e, maskedTextBox, 0));
                    maskedTextBox.Mask = GetMask(field.Client_Data_Type); //DR - passes in the type to get the mask
                    break;
                case "prepaymentpenalty":
                    maskedTextBox.Click += new EventHandler((s, e) => maskedTextBox_Clicked(s, e, maskedTextBox, 29));
                    maskedTextBox.Mask = GetMask(field.Client_Data_Type); //DR - passes in the type to get the mask
                    break;
                case "ssn/tin":
                    maskedTextBox.Click += new EventHandler((s, e) => maskedTextBox_Clicked(s, e, maskedTextBox, 0));
                    if (_AppUser.ActiveRole == 1)
                    {
                        maskedTextBox.Mask = GetMask("ssn"); //DR - passes in the type to get the mask
                    }
                    else
                    {
                        maskedTextBox.Mask = GetMask(field.Client_Data_Type);
                    }
                    maskedTextBox.KeyDown += new KeyEventHandler((s, e) => MaskedTextBox_KeyDown2(s, e));
                    maskedTextBox.KeyPress += new KeyPressEventHandler((s, e) => MaskedTextBox_KeyPress(s, e));
                    break;
                default:
                    maskedTextBox.Mask = GetMask(field.Client_Data_Type); //DR - passes in the type to get the mask
                    break;
            }

            tablePanel.Controls.Add(maskedTextBox, column, row);

            switch (_AppUser.ActiveRole)
            {
                case 1:
                    CheckBox checkboxOverride = new CheckBox() //DR- creates a new checkbox for the field
                    {
                        Name = "FieldOverride_checkbox_" + field.FieldId.ToString(), //field.FieldName,
                        Dock = DockStyle.Right,
                        TabStop = false,
                        Padding = new Padding(0, 3, 0, 0),
                        AutoSize = true
                    };
                    tablePanel.Controls.Add(checkboxOverride, overrideColumn, row);
                    //DR - adds the checkbox to the dictionary
                    fieldCheckDictionary[documentId][field.FieldId] = checkboxOverride;
                    checkboxOverride.Click += new EventHandler((s, e) => FieldCheck_Clicked(s, e, documentId, field.FieldId, checkboxOverride, maskedTextBox)); //DR - event handler for field chkbox
                    //DR - if it has a dependency, make a call to the appropriate helper method
                    if (FieldDependencies.Count() > 0)
                    {
                        FieldControlTextBoxHelper(field, tablePanel, documentId, maskedTextBox);
                    }
                    //DR- add to the dictionary so we can override it later
                    if (field.IsNotRequired && Convert.ToBoolean(field.N_A_By_Default))
                    {
                        FieldNAByDefaultDictionary[documentId].Add(maskedTextBox);
                    }
                    break;
            }
            return maskedTextBox;
        }

        //DR- Creates a field control of type GroupBox
        private GroupBox CreateFieldControlGroupBox(vDocuments_Fields field, TableLayoutPanel tablePanel, IQueryable<vFieldDependency> FieldDependencies, IQueryable<Fields_CrossDocument_Dependency> DocumentDependencies, IQueryable<vFieldValue> FieldValues, int documentId, int column, int overrideColumn)
        {
            GroupBox groupBox = new GroupBox()
            {
                Name = documentId + "_FieldControlGroupBox_" + field.FieldId,
                Text = field.FieldDescription,
                Height = 50,
                Width = 120,
                Dock = DockStyle.Left,
                TabIndex = tabIndex++,
                TabStop = true
            };
            tablePanel.Controls.Add(groupBox, column, row);
            //DR - if it has predefined values, add them to the control
            if (FieldValues.Count() > 0)
            {
                List<string> list = new List<string>();
                //DR- put the values in a list so we can reverse the order so that the groupbox displays the order of the items properly
                foreach (var rowFV in FieldValues)
                {
                    list.Add(rowFV.Value);
                }
                RadioButton radButton;
                foreach (string value in list.Reverse<string>())
                {
                    radButton = new RadioButton()
                    {
                        Name = documentId + "_FieldValueRadioButton_" + field.FieldId,
                        Text = value,
                        AutoSize = true,
                        Dock = DockStyle.Left,
                        TabIndex = tabIndex++,
                        TabStop = true
                    };
                    groupBox.Controls.Add(radButton);
                    //radButton.Click += new EventHandler((s, e) => RadioButtonGroupbox_Click(s, e, field, tablePanel, documentId, groupBox, radButton));
                    radButton.Select();
                    radButton = null;
                }
                list.Clear();
                list = null;
            }

            switch (_AppUser.ActiveRole)
            {
                case 1:
                    CheckBox checkboxOverride = new CheckBox() //DR- creates a new checkbox for the field
                    {
                        Name = "FieldOverride_checkbox_" + field.FieldId.ToString(), //field.FieldName,
                        Dock = DockStyle.Right,
                        TabStop = false,
                        Padding = new Padding(0, 3, 0, 0),
                        AutoSize = true
                    };
                    tablePanel.Controls.Add(checkboxOverride, overrideColumn, row);
                    //DR - adds the checkbox to the dictionary
                    fieldCheckDictionary[documentId][field.FieldId] = checkboxOverride;
                    checkboxOverride.Click += new EventHandler((s, e) => FieldCheck_Clicked(s, e, documentId, field.FieldId, checkboxOverride, groupBox)); //DR - event handler for field chkbox
                    //DR - if it has a dependency, make a call to the appropriate helper method
                    if (FieldDependencies.Count() > 0)
                    {
                        FieldControlGroupBoxHelper(field, tablePanel, documentId, groupBox);
                    }
                    //DR- add to the dictionary so we can override it later
                    if (field.IsNotRequired && Convert.ToBoolean(field.N_A_By_Default))
                    {
                        FieldNAByDefaultDictionary[documentId].Add(groupBox);
                    }
                    break;
            }
            return groupBox;
        }
        //DR- Creates a field control of type CheckBox
        private CheckBox CreateFieldControlCheckBox(vDocuments_Fields field, TableLayoutPanel tablePanel, IQueryable<vFieldDependency> FieldDependencies, IQueryable<Fields_CrossDocument_Dependency> DocumentDependencies, IQueryable<vFieldValue> FieldValues, int documentId, int column, int overrideColumn)
        {
            CheckBox checkBox = new CheckBox()
            {
                Name = documentId + "_FieldControlCheckBox_" + field.FieldId,
                AutoSize = true,
                Padding = new Padding(0, 3, 0, 0),
                Dock = DockStyle.Top,
                TabIndex = tabIndex++,
                TabStop = true
            };
            tablePanel.Controls.Add(checkBox, column, row);


            switch (_AppUser.ActiveRole)
            {
                case 1:
                    CheckBox checkboxOverride = new CheckBox() //DR- creates a new checkbox for the field
                    {
                        Name = "FieldOverride_checkbox_" + field.FieldId.ToString(), //field.FieldName,
                        Dock = DockStyle.Right,
                        TabStop = false,
                        Padding = new Padding(0, 3, 0, 0),
                        AutoSize = true
                    };
                    tablePanel.Controls.Add(checkboxOverride, overrideColumn, row);
                    //DR - adds the checkbox to the dictionary
                    fieldCheckDictionary[documentId][field.FieldId] = checkboxOverride;
                    checkboxOverride.Click += new EventHandler((s, e) => FieldCheck_Clicked(s, e, documentId, field.FieldId, checkboxOverride, checkBox)); //DR - event handler for field chkbox
                    //DR - if it has a dependency, make a call to the appropriate helper method
                    if (FieldDependencies.Count() > 0)
                    {
                        FieldControlCheckBoxHelper(field, tablePanel, documentId, checkBox);
                    }
                    //DR- add to the dictionary so we can override it later
                    if (field.IsNotRequired && Convert.ToBoolean(field.N_A_By_Default))
                    {
                        FieldNAByDefaultDictionary[documentId].Add(checkBox);
                    }
                    break;
            }
            return checkBox;
        }
        //DR- Creates a field control of type ComboBox
        private ComboBox CreateFieldControlComboBox(vDocuments_Fields field, TableLayoutPanel tablePanel, IQueryable<vFieldDependency> FieldDependencies, IQueryable<Fields_CrossDocument_Dependency> DocumentDependencies, IQueryable<vFieldValue> FieldValues, int documentId, int column, int overrideColumn)
        {
            ComboBox comboBox = new ComboBox()
            {
                Name = documentId + "_FieldControlComboBox_" + field.FieldId,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Left,
                TabIndex = tabIndex++,
                TabStop = true
            };
            tablePanel.Controls.Add(comboBox, column, row);
            //DR - if it has predefined values, add them to the control
            if (FieldValues.Count() > 0)
            {
                foreach (var rowFV in FieldValues)
                {
                    string value = rowFV.Value;
                    comboBox.Items.Add(value);
                    value = null;
                }
            }
            switch (_AppUser.ActiveRole)
            {
                case 1:
                    CheckBox checkboxOverride = new CheckBox() //DR- creates a new checkbox for the field
                    {
                        Name = "FieldOverride_checkbox_" + field.FieldId.ToString(), //field.FieldName,
                        Dock = DockStyle.Right,
                        TabStop = false,
                        Padding = new Padding(0, 3, 0, 0),
                        AutoSize = true
                    };
                    tablePanel.Controls.Add(checkboxOverride, overrideColumn, row);
                    //DR - adds the checkbox to the dictionary
                    fieldCheckDictionary[documentId][field.FieldId] = checkboxOverride;
                    checkboxOverride.Click += new EventHandler((s, e) => FieldCheck_Clicked(s, e, documentId, field.FieldId, checkboxOverride, comboBox)); //DR - event handler for field chkbox
                    //DR - if it has a dependency, make a call to the appropriate helper method
                    if (FieldDependencies.Count() > 0)
                    {
                        FieldControlComboBoxHelper(field, tablePanel, documentId, comboBox);
                    }
                    //DR- add to the dictionary so we can override it later
                    if (field.IsNotRequired && Convert.ToBoolean(field.N_A_By_Default))
                    {
                        FieldNAByDefaultDictionary[documentId].Add(comboBox);
                    }
                    break;
            }
            return comboBox;
        }

        /// <summary>
        /// MA - Sets the properties and creates the loading form
        /// </summary>
        /// <param name="strStatusText"></param>
        private void LoadingStartProgress(string strStatusText)
        {
#if (!DEBUG)
            //MA - Creates a new loading form
            objfrmShowProgress = new Form_Progress_Loading();

            //MA - Sets the position of where the form will appear
            objfrmShowProgress.StartPosition = FormStartPosition.Manual;
            Point p = new Point(this.Bounds.Location.X + (this.Width - objfrmShowProgress.Width) / 2,
                         this.Bounds.Location.Y + (this.Height - objfrmShowProgress.Height) / 2);
            objfrmShowProgress.Location = p;

            //MA - Set the message that will display in the loading form
            objfrmShowProgress.strStatus.Text = strStatusText;

            //MA - Hides the loading form from showing an icon on the taskbar
            objfrmShowProgress.ShowInTaskbar = false;
            LoadingShowProgress();
#endif

        }

        /// <summary>
        /// MA - Closes the loading form
        /// </summary>
        private void LoadingCloseProgress()
        {
#if (!DEBUG)
            Thread.Sleep(200);

            //MA - Forces the loading form to close 
            objfrmShowProgress.Invoke((MethodInvoker)delegate() { objfrmShowProgress.Close(); objfrmShowProgress.Dispose(); });
#endif
        }

        /// <summary>
        /// MA - Displays the loading form
        /// </summary>
        private void LoadingShowProgress()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    try
                    {
                        //MA - Display the loading form
                        objfrmShowProgress.ShowDialog();
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    //MA - Create a new thread and display the loading form
                    Thread thread = new Thread(LoadingShowProgress);
                    thread.IsBackground = false;
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //DR - loads the listbox(es) for navigation
        private void LoadListDictionaries() 
        {

            listBox_DocumentNav_Unchecked.Items.Clear();
            listBox_DocumentNav_Checked.Items.Clear();

            foreach (KeyValuePair<String, TabPage> doc in uncheckedDictionary)
            {
                listBox_DocumentNav_Unchecked.Items.Add(doc.Key);
            }

            foreach (KeyValuePair<String, TabPage> doc in checkedDictionary)
            {
                listBox_DocumentNav_Checked.Items.Add(doc.Key);
            }

        }

        /// <summary>
        /// MA - Gets the Event code for a field that is a dependency
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="documentId">Document ID</param>
        /// <returns>Event Code</returns>
        private int GetDependencyEvent(vDocuments_Fields field, int documentId)
        {
            int EventCode = Convert.ToInt32((from FDep in _AppEntity.FieldDependencies
                                             where FDep.DocID == documentId
                                             && FDep.ParentID == field.FieldId
                                             select FDep.EventCodeID).Distinct().Single());
            return EventCode;
        }

        //DR - assigns the appropriate event handlers for field specific logic and dependencies
        private void FieldControlTextBoxHelper(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, MaskedTextBox textBox)
        {
            switch (GetDependencyEvent(field, documentId))
            {
                case 2:
                    textBox.TextChanged += ((s, e) => FieldControlTextboxToggleChildren(s, e, field, tablePanel, documentId, textBox));
                    FieldDependencyFirstTimeSetup(field, tablePanel, documentId, textBox, false);
                    break;
            }

        }

        //DR - assigns the appropriate event handlers for field specific logic and dependencies
        private void FieldControlCheckBoxHelper(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, CheckBox checkBox)
        {
            switch (GetDependencyEvent(field, documentId))
            {
                case 1:
                    checkBox.Click += ((s, e) => FieldControlCheckBoxToggleChildren(s, e, field, tablePanel, documentId, checkBox));
                    //DR - call for first time setup
                    FieldDependencyFirstTimeSetup(field, tablePanel, documentId, checkBox, false);
                    break;
            }
        }

        //DR - assigns the appropriate event handlers for field specific logic and dependencies
        private void FieldControlGroupBoxHelper(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, GroupBox groupBox)
        {
            switch (GetDependencyEvent(field, documentId))
            {
                case 5:
                    foreach (RadioButton radButton in groupBox.Controls.OfType<RadioButton>())
                    {
                        radButton.Click += ((s, e) => FieldControlGroupBoxSsnTinChangeMask(s, e, field, tablePanel, documentId, groupBox, radButton));
                    }
                    FieldDependencyFirstTimeSetup(field, tablePanel, documentId, groupBox, false);
                    break;
            }
        }

        //DR - assigns the appropriate event handlers for field specific logic and dependencies
        private void FieldControlComboBoxHelper(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, ComboBox comboBox)
        {

            switch (GetDependencyEvent(field, documentId))
            {
                case 3:
                    comboBox.SelectedIndexChanged += ((s, e) => FieldControlComboBoxToggleChildren(s, e, field, tablePanel, documentId, comboBox));
                    FieldDependencyFirstTimeSetup(field, tablePanel, documentId, comboBox, false);
                    break;
            }

        }

        //DR - First time setup for field dependencies
        public void FieldDependencyFirstTimeSetup(vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, Control ParentControl, bool enableChildrenOnSetup)
        {
            //DR - first time setup to add the document id
            if (!FieldDependencyDictionary.ContainsKey(documentId))
            {
                FieldDependencyDictionary[documentId] = new Dictionary<Control, ArrayList>();
                FieldDependenciesDictionary[documentId] = new Dictionary<int, ArrayList>();
            }
            //DR - first time setup to add the field dependencies
            if (FieldDependencyDictionary.ContainsKey(documentId))
            {
                if (!FieldDependencyDictionary[documentId].ContainsKey(ParentControl))
                {
                    FieldDependencyDictionary[documentId][ParentControl] = new ArrayList();
                    FieldDependenciesDictionary[documentId][field.FieldId] = new ArrayList();
                    int parentID = GetFieldID(ParentControl.Name);
                    var FieldDependencies = from FD in _AppEntity.vFieldDependencies.AsNoTracking() where FD.DocID == documentId && FD.ParentID == parentID select FD;
                    Control control;
                    foreach (var rowFD in FieldDependencies)
                    {
                        var count = (from d in _AppEntity.vDocuments_Fields.AsNoTracking() where d.DocumentId == documentId && rowFD.ChildID == d.FieldId select d);

                        if (count.Count() == 1)
                        {
                            row++;
                            var childDF = count.Single();

                            Label label = new Label()
                            {
                                Name = tablePanel.Name + "_label_" + childDF.FieldId,
                                Text = childDF.FieldName + @":",
                                AutoSize = true,
                                Padding = new Padding(0, 7, 0, 0),
                                Dock = DockStyle.Left,
                            };
                            FieldLabelDictionary[documentId][childDF.FieldId] = label;
                            tablePanel.Controls.Add(label, 1, row);
                            control = CreateFieldControl(childDF, tablePanel, documentId, 2, 0);

                            //MA - Loads the context menu for the control
                            LoadMenu(label, documentId);
                            //DR - add the child to the field dependency dictionary
                            FieldDependencyDictionary[documentId][ParentControl].Add(control);
                            FieldDependenciesDictionary[documentId][field.FieldId].Add(childDF.FieldId);
                            //DR - adds the field name to the tab. No value is entered because we haven't gotten a loan yet
                            DatabaseDictionary[childDF.DocumentName].Add(childDF.FieldId, null);
                            UserInputDictionary[childDF.DocumentName].Add(childDF.FieldId, null);
                            //DR - adds tooltips for the specified field label
                            toolTips.SetToolTip(label, childDF.FieldDescription+"\n"+childDF.HelpInfo);
                            //toolTips.SetToolTip(control, childDF.HelpInfo);
                        }




                    }
                    foreach (Control c in FieldDependencyDictionary[documentId][ParentControl])
                    {
                        c.Enabled = enableChildrenOnSetup;
                        int childID = GetFieldID(c.Name);
                        //DR - disable the field override
                        fieldCheckDictionary[documentId][childID].Enabled = enableChildrenOnSetup;
                        //DR - disable the documentlist
                        DocumentListDictionary[documentId][childID].Enabled = enableChildrenOnSetup;

                    }
                }
            }
        }

        /// <summary>
        /// MA - Populates the Tabpage with controls of Fields from MSP and sets up the 
        /// the corresponding dependencies and functionality for the specific tab page
        /// </summary>
        /// <param name="tabPage"></param>
        /// <param name="documentId"></param>
        /// <param name="docName"></param>
        private void TryPopulateDocumentFields(TabPage tabPage, int documentId, string docName)
        {
            try
            {
                //MA - Create a new table layout panel
                ModifiedTablePanel tablePanel = new ModifiedTablePanel()
                {
                    Name = tabPage.Name + "_panel_" + documentId,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    dBuffer = true,
                };

                //MA - Assign click event handler for the table layout panel
               // tablePanel.Click += new EventHandler((s, e) => tablePanel_Click(s, e, tablePanel));
                
                //MA - Prevent the drawing of controls on the table layout panel
                tablePanel.SuspendLayout();

                //MA - Add the table layout panel to the tab page
                tabPage.Controls.Add(tablePanel);


                //MA - Query to get all the fields from MSP(ImagingApps.LoanReview.LoanData)
                var fieldQuery = (from fields in _AppEntity.vDocuments_Fields.AsNoTracking()
                                  where documentId == fields.DocumentId
                                  select fields).ToList().OrderBy(x => x.FieldOrder);

                row = 0;
                Label docLabel = new Label()
                {
                    Text = "Document Override: ",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 15, 0, 0),

                };
                CheckBox docCheckbox = new CheckBox() //DR- creates a new checkbox for each doc
                {
                    Name = "DocumentOverride_checkbox_" + documentId.ToString(),
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 12, 0, 0),
                    AutoSize = true,
                };
                Label overrideLabel = new Label()
                {
                    Text = "Field Override",
                    AutoSize = false,
                    Dock = DockStyle.Right,
                    Height = 34,
                    Width = 75,
                    Padding = new Padding(0, 15, 0, 0)
                };
                Label docFound = new Label()
                {
                    Text = "Found in Document",
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Width = 25,
                    Padding = new Padding(20, 15, 0, 0)
                };

                //var doc = (from d in _AppEntity.vDocuments.AsNoTracking() where d.Id == documentId select d).Single();


                //DR- creates a new checkbox for each doc
                //DR- adds the checkbox with doc name to the dictionary so we have a reference to it
                docCheckDictionary.Add(docName, docCheckbox);
                fieldCheckDictionary.Add(documentId, new Dictionary<int, CheckBox>());
                FieldNAByDefaultDictionary.Add(documentId, new ArrayList());

                //DR - event handler for chkDoc
                docCheckbox.Click += new EventHandler((s, e) => DocCheck_Clicked(s, e, tabPage, docName, documentId));
                tablePanel.Controls.Add(docCheckbox, 1, row);
                tablePanel.Controls.Add(docLabel, 0, row);

                // ES - Increment row twice for row headers.
                row++;
                row++;

                tablePanel.Controls.Add(overrideLabel, 0, row);
                tablePanel.Controls.Add(docFound, 3, row);

                foreach (var field in fieldQuery)
                {
                    //DR - checks to see if field is a depedant on another field, if so let the parent handle its creation and move to the next field
                    int isChild = (from child in _AppEntity.vFieldDependencies.AsNoTracking()
                                   where child.ChildID == field.FieldId && child.DocID == documentId
                                   select child).Count();

                    if (!(isChild > 0))
                    {
                        row++;
                        Label label = new Label()
                        {
                            Name = tablePanel.Name + "_label_" + field.FieldId,
                            Text = field.FieldName + @":",
                            AutoSize = true,
                            Padding = new Padding(0, 7, 0, 0),
                            Dock = DockStyle.Left,
                        };
                        if (field.ControlType == "groupbox")
                        {
                            label.Padding = new Padding(0, 20, 0, 0);
                        }
                        FieldLabelDictionary[documentId][field.FieldId] = label;

                        tablePanel.Controls.Add(label, 1, row);
                        Control c = CreateFieldControl(field, tablePanel, documentId, 2, 0);
                        LoadMenu(label, documentId);
                        //DR - adds the field name to the tab. No value is entered because we haven't gotten a loan yet
                        DatabaseDictionary[tabPage.Text].Add(field.FieldId, null);
                        UserInputDictionary[tabPage.Text].Add(field.FieldId, null);
                        //DR - adds tooltips for the specified field label
                        toolTips.SetToolTip(label, field.FieldDescription+Environment.NewLine+field.HelpInfo);
                        //toolTips.SetToolTip(c, field.HelpInfo);
                        label.ContextMenuStrip.Opened += ((s, e) => ContextMenuStrip_Opened(s, e, label));
                        label.ContextMenuStrip.Closed += ((s, e) => ContextMenuStrip_Closed(s, e, label));
                    }
                }
                Label g = new Label()
                {
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Width = 25,
                    Padding = new Padding(20, 15, 0, 0)
                };
                row++;
                tablePanel.Controls.Add(g, 0, row);

                //DR - adds the mouseeventhandler for our tablepanel's mousemove event
                tablePanel.MouseMove += new MouseEventHandler((s, e) => toolTips_MouseMove(s, e, tablePanel));
                tablePanel.ResumeLayout();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// MA - Creates a context menu for labels depending on current tier
        /// </summary>
        /// <param name="FieldControl"></param>
        /// <param name="DocumentID"></param>
        private void LoadMenu(Control FieldControl, int DocumentID)
        {
            //MA - Creates a context menu
            ContextMenuStrip menu = new ContextMenuStrip();

            //MA - Get the ID of a field
            int fid = GetFieldID(FieldControl.Name);

            //MA - Determine which tier the user is on
            switch (_AppUser.ActiveRole)
            {
                //MA - If tier 1, load a context menu only for fields that have fields dependencies
                case 1:
                    if (FieldDependenciesDictionary.ContainsKey(DocumentID))
                    {
                        if (FieldDependenciesDictionary[DocumentID].ContainsKey(fid))
                        {
                            //MA - Create a sub menu for the context menu
                            ToolStripMenuItem submenu = new ToolStripMenuItem();
                            submenu.Text = "Field Dependencies";

                            //MA - For each field dependency add a toolstrip to the sub menu
                            foreach (int field in FieldDependenciesDictionary[DocumentID][fid])
                            {
                                ToolStripMenuItem item = new ToolStripMenuItem();
                                item.Text = GetFieldName(field);
                                submenu.DropDownItems.Add(item);

                                //Add event handler to add to dictionary
                            }

                            //MA - Add the submenu to the context menu
                            menu.Items.Add(submenu);
                        }
                    }
                    //MA - Assign the context menu to the corresponding control
                    FieldControl.ContextMenuStrip = menu;
                    break;

                //MA - If tier 2, load the context menu for each field with the selected document from tier 1 
                case 2:
                    //MA - Create a sub menu for the context menu
                    ToolStripMenuItem submenu2 = new ToolStripMenuItem();
                    submenu2.Text = "Selected Document";

                    //MA - Gets the selected document in tier 1 from Userinput table in Imaging Apps
                    var Tier1selecteddocument = (from UI in _AppEntity.vUserInputs.AsNoTracking()
                                                 where UI.LoanID == loanid
                                                 && UI.DocID == DocumentID
                                                 && UI.FieldID == fid
                                                 && UI.InputID == 2
                                                 && UI.Workflow == workflowID
                                                 select UI.SelectedDocument).Single();

                    //MA - Creates a toolstrip with the selected document from tier 1 to the sub menu
                    ToolStripMenuItem item2 = new ToolStripMenuItem();
                    item2.Text = "Tier 1: " + Tier1selecteddocument.ToString();

                
                    //MA - Add the toolstrip to the submenu
                    submenu2.DropDownItems.Add(item2);
                    menu.Items.Add(submenu2);

                    //MA - Assign the context menu to the corresponding control
                    FieldControl.ContextMenuStrip = menu;
                    break;

                //MA - If tier 3, load the context menu for each field with the selected document from tier 1 and tier 2
                case 3:

                    //MA - Create a sub menu for the context menu
                    ToolStripMenuItem submenu3 = new ToolStripMenuItem();
                    submenu3.Text = "Selected Document";

                    //MA - Gets the selected document in tier 1 from Userinput table in Imaging Apps
                    var TierSelecteddocument = (from UI in _AppEntity.UserInputs
                                                where UI.LoanID == loanid
                                                && UI.DocID == DocumentID
                                                && UI.FieldID == fid
                                                && UI.InputID == 2
                                                && UI.Workflow == workflowID
                                                select UI.SelectedDocument).Single();

                    //MA - Creates a toolstrip with the selected document from tier 1 to the sub menu
                    ToolStripMenuItem subitem = new ToolStripMenuItem();
                    subitem.Text = "Tier 1: " + TierSelecteddocument.ToString();

                    //MA - Gets the selected document in tier 2 from Userinput table in Imaging Apps
                    var Tier2selecteddocument = (from UI in _AppEntity.UserInputs
                                                 where UI.LoanID == loanid
                                                 && UI.DocID == DocumentID
                                                 && UI.FieldID == fid
                                                 && UI.InputID == 3
                                                 && UI.Workflow == workflowID
                                                 select UI.SelectedDocument).Single();

                    //MA - Creates a toolstrip with the selected document from tier 2 to the sub menu
                    ToolStripMenuItem subitem2 = new ToolStripMenuItem();
                    subitem2.Text = "Tier 2: " + Tier2selecteddocument.ToString();

                    //MA - Add the toolstrip to the submenu
                    submenu3.DropDownItems.Add(subitem);
                    submenu3.DropDownItems.Add(subitem2);
                    menu.Items.Add(submenu3);

                    //MA - Assign the context menu to the corresponding control
                    FieldControl.ContextMenuStrip = menu;
                    break;
            }

            //MA - Changes color of control when context menu is opened and closed
            FieldControl.ContextMenuStrip.Opened += ((s, e) => ContextMenuStrip_Opened(s, e, FieldControl));
            FieldControl.ContextMenuStrip.Closed += ((s, e) => ContextMenuStrip_Closed(s, e, FieldControl));
        }

        /// <summary>
        /// MA - When the Context menu is closed the background color of the label will be default
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="fieldControl"></param>
        private void ContextMenuStrip_Closed(object s, ToolStripDropDownClosedEventArgs e, Control fieldControl)
        {
            //MA - set the backcolor of control to default
            fieldControl.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// MA - When the Context menu is opened the background color of the label will be light blue
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="fieldControl"></param>
        private void ContextMenuStrip_Opened(object s, EventArgs e, Control fieldControl)
        {
            //MA - set the backcolor of control to light blue
            fieldControl.BackColor = Color.LightBlue;
        }

        /// <summary>
        ///  MA - Prevents the tabpage from reloading and enables focus
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="tablePanel"></param>
        private void tablePanel_Click(object s, EventArgs e, ModifiedTablePanel tablePanel)
        {
            tablePanel.SuspendLayout();
            tablePanel.Focus();
            tablePanel.ResumeLayout(true);
        }


        //DR - Tier 2
        private void TryPopulateDocumentFields2(TabPage tabPage, int documentId, string docName)
        {
            try
            {
                //MA -
                ModifiedTablePanel tablePanel = new ModifiedTablePanel()
                {
                    Name = tabPage.Name + "_panel_" + documentId,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    dBuffer = true,
                };

                //MA - Prevent drawing on the table layout panel
                tablePanel.SuspendLayout();

                //MA - Add the table layout panel to the tabpage
                tabPage.Controls.Add(tablePanel);

                //MA - Gets the fields that were incorrect from Tier 1 loan
                var fieldQuery = (from field in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                  where field.LoanID == loanid
                                  && field.isMatching == false
                                  && field.Reportable == true
                                  && field.DocID == documentId
                                  && field.InputID == 2
                                  && field.Workflow == workflowID
                                  select field).ToList().OrderBy(x => x.FieldOrder);

                row = 0;

                //MA - Add the static controls to the table layout panel
                Label MSPLabel = new Label()
                {
                    Name = tablePanel.Name + "_MSPlabel",
                    Text = "MSP Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    //Anchor = AnchorStyles.Top,
                };
                Label Tier1Label = new Label()
                {
                    Name = tablePanel.Name + "_Tier1Entrylabel",
                    Text = "Tier 1 Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    //Anchor = AnchorStyles.Top,
                };
                Label label = new Label()
                {
                    Name = tablePanel.Name + "_label",
                    Text = @"New Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    //Anchor = AnchorStyles.Top,
                };
                Label docLabel = new Label()
                {
                    Text = "Document Override: ",
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 15, 0, 0),
                };
                CheckBox docCheckbox = new CheckBox() //DR- creates a new checkbox for each doc
                {
                    Name = "DocumentOverride_checkbox_" + documentId.ToString(),
                    Dock = DockStyle.Top,
                    Padding = new Padding(15, 12, 0, 0),
                    AutoSize = true,
                };
                Label docFound = new Label()
                {
                    Text = "Found in Document",
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Padding = new Padding(15, 3, 0, 3)
                };
                var doc = (from d in _AppEntity.vDocuments.AsNoTracking() where d.Id == documentId select d).Single();

                //DR- creates a new checkbox for each doc
                //DR- adds the checkbox with doc name to the dictionary so we have a reference to it
                docCheckDictionary.Add(docName, docCheckbox);
                //DR - event handler for chkDoc
                docCheckbox.Click += new EventHandler((s, e) => DocCheck_Clicked(s, e, tabPage, doc.Name, doc.Id));
                tablePanel.Controls.Add(docCheckbox, 2, row);
                tablePanel.Controls.Add(docLabel, 1, row);
                row++;

                //MA - Organizes order of how labels will be displayed on doc
                tablePanel.Controls.Add(MSPLabel, 3, row);
                tablePanel.Controls.Add(Tier1Label, 5, row);
                tablePanel.Controls.Add(label, 7, row);
                tablePanel.Controls.Add(docFound, 8, row);

                row++;

                foreach (var field in fieldQuery)
                {
                    //DR - DocumentsField query for our createfieldcontrol method
                    var dField = (from docfield in _AppEntity.vDocuments_Fields.AsNoTracking() where docfield.FieldId == field.FieldID && docfield.DocumentId == documentId select docfield).Single();

                    LoadUserInputDictionary(documentId, field.FieldID, 2);

                    TextBox MSPTextbox = new TextBox()
                    {
                        Name = tablePanel.Name + "_MSPtextBox_" + field.FieldID,
                        BackColor = Color.White,
                        Enabled = false,
                        Dock = DockStyle.Left,
                        Width = 150,
                    };

                    CheckBox MSPCheckBox = new CheckBox()
                    {
                        Name = tablePanel.Name + "_MSPcheckbox_" + field.FieldID,
                        Enabled = true,
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Padding = new Padding(15, 3, 0, 0),
                        BackColor = Color.Transparent
                    };

                    TextBox Tier1Entry = new TextBox()
                    {
                        Name = tablePanel.Name + "_Tier1EntrytextBox_" + field.FieldID,
                        Text = UserTier1Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString(),
                        Enabled = false,
                        Dock = DockStyle.Left,
                        BackColor = Color.White,
                        Width = 150,
                    };

                    CheckBox Tier1EntryCheckBox = new CheckBox()
                    {
                        Name = tablePanel.Name + "_Tier1Entrycheckbox_" + field.FieldID,
                        Enabled = true,
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Padding = new Padding(15, 3, 0, 0),
                        BackColor = Color.Transparent
                    };


                    Label fieldLabel = new Label()
                    {
                        Name = tablePanel.Name + "_label_" + field.FieldID,
                        Text = field.FieldName + @": ",
                        AutoSize = true,
                        Dock = DockStyle.Left,
                        Padding = new Padding(0, 6, 0, 0),
                    };
                    FieldLabelDictionary[documentId][field.FieldID] = fieldLabel;
                    //LoadMenu(fieldLabel, documentId);
                    CheckBox UserTier2CheckBox = new CheckBox()
                    {
                        Name = tablePanel.Name + "_UserTier2checkbox_" + field.FieldID,
                        Enabled = true,
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Padding = new Padding(15, 3, 0, 0),
                        BackColor = Color.Transparent
                    };


                    DatabaseDictionary[tabPage.Text].Add(field.FieldID, null); //adds the field name to the tab. No value is entered because we haven't gotten a loan yet
                    
#if DEBUG
                    //AO for faster debugging
                    //selectAllbtn.Click += new EventHandler((s, e) => selectAllbtn_Clicked(s, e, tabPage, documentId));//AO - event handler for button
#endif
                    //MA - Orders the controls to be displayed on tabpage
                    tablePanel.Controls.Add(fieldLabel, 1, row);
                    tablePanel.Controls.Add(MSPCheckBox, 2, row);
                    tablePanel.Controls.Add(MSPTextbox, 3, row);
                    tablePanel.Controls.Add(Tier1EntryCheckBox, 4, row);
                    tablePanel.Controls.Add(Tier1Entry, 5, row);
                    tablePanel.Controls.Add(UserTier2CheckBox, 6, row);
                    Control control = CreateFieldControl(dField, tablePanel, documentId, 7, 0);
                    control.Enabled = false;

                    //MA - Load the context menu for the control
                    LoadMenu(fieldLabel, documentId);
                    //tablePanel.Controls.Add(InformationButton, 9, row);
                    row++;


                    //DR - adds tooltips for the specified field label
                    toolTips.SetToolTip(fieldLabel, field.FieldDescription + Environment.NewLine + field.HelpInfo);
                    //toolTips.SetToolTip(control, field.HelpInfo);
                    toolTips.SetToolTip(Tier1Entry, UserTier1Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString());

                    //DR & MA - adds the eventhandler for each .click event for the checkbox
                    MSPCheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, MSPCheckBox, Tier1EntryCheckBox, UserTier2CheckBox, MSPTextbox, control, true, false, documentId, field.FieldID));
                    Tier1EntryCheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, Tier1EntryCheckBox, MSPCheckBox, UserTier2CheckBox, Tier1Entry, control, false, false, documentId, field.FieldID));
                    UserTier2CheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, UserTier2CheckBox, MSPCheckBox, Tier1EntryCheckBox, null, control, false, true, documentId, field.FieldID));
                    //InformationButton.Click += new EventHandler((s, e) => InformationButton_Clicked(s, e, tabPage, documentId, InformationButton, InfoPanel, rtbInfo));
                    
                    fieldCountDictionary[documentId]++; //DR - increments the fieldcount for each field added
                    //DR - if dictionary doesn't contain a field, add it
                    if (!fieldCriticalCheckDictionary.ContainsKey(field.FieldID))
                    {
                        fieldCriticalCheckDictionary[field.FieldID] = field.IsCritical;
                    }
                }
                Label g = new Label()
                {
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Width = 25,
                    Padding = new Padding(20, 15, 0, 0)
                };
                row++;
                tablePanel.Controls.Add(g, 0, row);

                tablePanel.MouseDown += new MouseEventHandler((s, e) => tablePanel_MouseDown(s, e, tablePanel, documentId));
                //DR - adds the mouseeventhandler for our tablepanel's mousemove event
                tablePanel.MouseMove += new MouseEventHandler((s, e) => toolTips_MouseMove(s, e, tablePanel));
                tablePanel.ResumeLayout();
#if DEBUG
                //tablePanel.Controls.Add(selectAllbtn, 0, ++row);//AO check all button is added for faster debugging
#endif
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //DR - Tier 3
        private void TryPopulateDocumentFields3(TabPage tabPage, int documentId, string docName)
        {
            row = 0;
            try
            {
                //MA - Create the table layout panel for the tab page
                ModifiedTablePanel tablePanel = new ModifiedTablePanel()
                {
                    Name = tabPage.Name + "_panel_" + documentId,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    AutoScroll = true
                };

                //MA - Prevent drawing on the table layout panel
                tablePanel.SuspendLayout();

                //MA - Add the table layout panel to the tab page
                tabPage.Controls.Add(tablePanel);


                //MA - Create the static controls
                Label MSPLabel = new Label()
                {
                    Name = tablePanel.Name + "_MSPlabel",
                    Text = "MSP Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left
                };
                Label Tier1Label = new Label()
                {
                    Name = tablePanel.Name + "_Tier1Entrylabel",
                    Text = "Tier 1 Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left
                };
                Label Tier2Label = new Label()
                {
                    Name = tablePanel.Name + "_Tier2Entrylabel",
                    Text = "Tier 2 Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left
                };
                Label label = new Label()
                {
                    Name = tablePanel.Name + "_label",
                    Text = @"New Value ",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                };
                Label docFound = new Label()
                {
                    Text = "Found in Document",
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Padding = new Padding(0, 6, 0, 0)
                };

                //MA - Get the document information for specific document ID
                var doc = (from d in _AppEntity.vDocuments.AsNoTracking() where d.Id == documentId select d).Single();
                
                //MA - Checks if document is overridable
                if (doc.IsNotRequired)
                {

                    Label docLabel = new Label()
                    {
                        Text = "Document Override: ",
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Padding = new Padding(0, 15, 0, 0),
                    };
                    CheckBox docCheckbox = new CheckBox() //DR- creates a new checkbox for each doc
                    {
                        Name = "DocumentOverride_checkbox_" + documentId.ToString(),
                        Dock = DockStyle.Top,
                        Padding = new Padding(0, 12, 0, 0),
                        AutoSize = true,
                    };
                    //DR- creates a new checkbox for each doc
                    //DR- adds the checkbox with doc name to the dictionary so we have a reference to it
                    docCheckDictionary.Add(docName, docCheckbox);
                    //DR - event handler for chkDoc
                    docCheckbox.Click += new EventHandler((s, e) => DocCheck_Clicked(s, e, tabPage, doc.Name, doc.Id));
                    tablePanel.Controls.Add(docCheckbox, 2, row);
                    tablePanel.Controls.Add(docLabel, 1, row);
                    row++;
                }

                //MA - Add the static controls to the table layout panel
                tablePanel.Controls.Add(MSPLabel, 4, row);
                tablePanel.Controls.Add(Tier1Label, 7, row);
                tablePanel.Controls.Add(Tier2Label, 10, row);
                tablePanel.Controls.Add(label, 13, row);
                tablePanel.Controls.Add(docFound, 14, row);
                
                row++;

                //MA - Query to get the fields that were incorrect in tier 2
                var fieldQuery = (from field in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                  where field.LoanID == loanid
                                  && field.isMatching == false
                                  && field.Reportable == true
                                  && field.DocID == documentId
                                  && field.InputID == 3
                                  && field.Workflow == workflowID
                                  select field).ToList().OrderBy(x => x.FieldOrder);

                foreach (var field in fieldQuery)
                {
                    //DR - only load the field if it is critical
                    if (field.IsCritical)
                    {
                        LoadUserInputDictionary(documentId, field.FieldID, 2);
                        LoadUserInputDictionary(documentId, field.FieldID, 3);
                        //DR - DocumentsField query for our createfieldcontrol method
                        var dField = (from docfield in _AppEntity.vDocuments_Fields.AsNoTracking() where docfield.FieldId == field.FieldID && docfield.DocumentId == documentId select docfield).Single();
                        
                        //MA - Create static controls for table layout panel
                        Label fieldLabel = new Label()
                        {
                            Name = tablePanel.Name + "_label_" + field.FieldName,
                            Text = field.FieldName + @": ",
                            AutoSize = true,
                            Padding = new Padding(0, 4, 0, 0),
                            Dock = DockStyle.Left,
                        };

                        //MA - store the document, fieeld and value
                        FieldLabelDictionary[documentId][field.FieldID] = fieldLabel;
                        TextBox MSPTextbox = new TextBox()
                        {
                            Name = tablePanel.Name + "_MSPtextBox_" + field.FieldID,
                            BackColor = Color.White,
                            Enabled = false,
                            Dock = DockStyle.Top,
                            Width = 150,
                        };

                        CheckBox MSPCheckBox = new CheckBox()
                        {
                            Name = tablePanel.Name + "_MSPcheckbox_" + field.FieldID,
                            Enabled = true,
                            AutoSize = true,
                            Dock = DockStyle.Top,
                            Padding = new Padding(20, 2, 0, 0),
                            BackColor = Color.Transparent
                        };
                        //textbox
                        TextBox Tier1Entry = new TextBox()
                        {
                            Name = tablePanel.Name + "_Tier1EntrytextBox_" + field.FieldID,
                            Text = UserTier1Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString(),
                            Enabled = false,
                            //Dock = DockStyle.Left,
                            BackColor = Color.White,
                            Width = 150,
                        };
                        CheckBox Tier1EntryCheckBox = new CheckBox()
                        {
                            Name = tablePanel.Name + "_Tier1Entrycheckbox_" + field.FieldID,
                            Enabled = true,
                            AutoSize = true,
                            Padding = new Padding(20, 2, 0, 0),
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent
                        };
                        TextBox Tier2Entry = new TextBox()
                        {
                            Name = tablePanel.Name + "_Tier2EntrytextBox_" + field.FieldID,
                            Text = UserTier2Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString(),
                            Enabled = false,
                            //Dock = DockStyle.Left,
                            BackColor = Color.White,
                            Width = 150,
                        };

                        CheckBox Tier2EntryCheckBox = new CheckBox()
                        {
                            Name = tablePanel.Name + "_Tier2Entrycheckbox_" + field.FieldID,
                            Enabled = true,
                            AutoSize = true,
                            Padding = new Padding(20, 2, 0, 0),
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent
                        };
                        CheckBox UserTier3CheckBox = new CheckBox()
                        {
                            Name = tablePanel.Name + "_UserTier3checkbox_" + field.FieldID,
                            Enabled = true,
                            AutoSize = true,
                            Padding = new Padding(20, 2, 0, 0),
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent
                        };

                        DatabaseDictionary[tabPage.Text].Add(field.FieldID, null); //adds the field name to the tab. No value is entered because we haven't gotten a loan yet
                        UserInputDictionary[tabPage.Text].Add(field.FieldID, null);

#if DEBUG
                        selectAllbtn.Click += new EventHandler((s, e) => selectAllbtn_Clicked(s, e, tabPage, documentId));//AO - event handler for button
#endif
                        //MA - Sets how controls are going to be displayed on TableLayoutPanel
                        tablePanel.Controls.Add(fieldLabel, 1, row);
                        tablePanel.Controls.Add(MSPCheckBox, 2, row);
                        tablePanel.Controls.Add(MSPTextbox, 4, row);
                        tablePanel.Controls.Add(Tier1EntryCheckBox, 5, row);
                        tablePanel.Controls.Add(Tier1Entry, 7, row);
                        tablePanel.Controls.Add(Tier2EntryCheckBox, 8, row);
                        tablePanel.Controls.Add(Tier2Entry, 10, row);
                        tablePanel.Controls.Add(UserTier3CheckBox, 11, row);
                        Control control = CreateFieldControl(dField, tablePanel, documentId, 13, 0);
                        control.Enabled = false;
                        //tablePanel.Controls.Add(InformationButton, 15, row);
                        row++;

                        //DR - adds tooltips for the specified field label
                        toolTips.SetToolTip(fieldLabel, field.FieldDescription + Environment.NewLine + field.HelpInfo);
                        //toolTips.SetToolTip(control, field.HelpInfo);
                        toolTips.SetToolTip(Tier1Entry, UserTier1Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString());
                        toolTips.SetToolTip(Tier2Entry, UserTier2Dictionary[(GetDocumentName(documentId))][field.FieldID].ToString());

                        //DR & MA - adds the eventhandler for each .click event for the checkbox
                        MSPCheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, MSPCheckBox, Tier1EntryCheckBox, Tier2EntryCheckBox, UserTier3CheckBox, MSPTextbox, control, true, false, documentId, field.FieldID));
                        Tier1EntryCheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, Tier1EntryCheckBox, MSPCheckBox, Tier2EntryCheckBox, UserTier3CheckBox, Tier1Entry, control, false, false, documentId, field.FieldID));
                        Tier2EntryCheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, Tier2EntryCheckBox, MSPCheckBox, Tier1EntryCheckBox, UserTier3CheckBox, Tier2Entry, control, false, false, documentId, field.FieldID));
                        UserTier3CheckBox.Click += new EventHandler((s, e) => CheckBox_Click(s, e, tabPage, UserTier3CheckBox, MSPCheckBox, Tier1EntryCheckBox, Tier2EntryCheckBox, null, control, false, true, documentId, field.FieldID));
                        //InformationButton.Click += new EventHandler((s, e) => InformationButton_Clicked(s, e, tabPage, documentId, InformationButton, InfoPanel, rtbInfo));

                        fieldCountDictionary[documentId]++; //DR - increments the fieldcount for each field added
                    }

                }
                Label g = new Label()
                {
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    Width = 25,
                    Padding = new Padding(20, 15, 0, 0)
                };
                row++;
                tablePanel.Controls.Add(g, 0, row);

                tablePanel.MouseDown += new MouseEventHandler((s, e) => tablePanel_MouseDown(s, e, tablePanel, documentId));
                //DR - adds the mouseeventhandler for our tablepanel's mousemove event
                tablePanel.MouseMove += new MouseEventHandler((s, e) => toolTips_MouseMove(s, e, tablePanel));
                tablePanel.ResumeLayout();
#if DEBUG
                tablePanel.Controls.Add(selectAllbtn, 0, ++row);//AO adds select all checkboxes button to controls
#endif
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //DR - Tier 1
        private void TryPopulateDocumentTabs()
        {
            try
            {

                toolTips = new ToolTip();

                switch (workflowID)
                {
                    case 1:
                        //MA: Sort documents by alphabetical order.
                        var docQuery = (from documents in _AppEntity.vDocuments.AsNoTracking()
                                        select documents).ToList().OrderBy(x => x.DocOrder);
                        
                        foreach (var doc in docQuery)
                        {
                            TabPage tabPage = new TabPage()
                            {
                                Text = doc.Name,
                                ToolTipText = doc.Description,
                            };
                            this.uncheckedDictionary.Add(doc.Name, tabPage);
                            this.shortcutDictionary.Add(doc.ShortcutKey, tabPage); //DR - adds the shortcut and its tabpage to the dictionary
                            this.DatabaseDictionary.Add(doc.Name, new Dictionary<int, object>()); //DR - stores the name of the tab
                            this.UserInputDictionary.Add(doc.Name, new Dictionary<int, object>()); //MA - stores users input data
                            this.DocTimerDictionary.Add(doc.Name, null); //DR - adds the docs name to the DocTimerDictionary. Does not create the timer yet
                            this.DocTickDictionary.Add(doc.Name, 0);//DR - adds a fresh entry for a document with ticks equal to 0 because timer hasn't started yet
                            this.DocumentListDictionary.Add(doc.Id, new Dictionary<int, ComboBox>()); //DR - adds the document to the dictionary
                            this.FieldLabelDictionary.Add(doc.Id, new Dictionary<int, Label>());
                            this.FieldHasPromptDictionary.Add(doc.Id, new Dictionary<int, bool>()); //DR- adds the document to the dictionary
                            this.TabPageContainsWarningDictionary.Add(tabPage.Text, false);
                            TabPageWarningPromptDictionary.Add(tabPage.Text, null);

                            tabControl_Doc_Control.Controls.Add(tabPage);
                            TryPopulateDocumentFields(tabPage, doc.Id, doc.Name);
                        }
                        break;
                    case 2:
                        var docQueryIVT = (from doc in _AppEntity.WorkflowTrackings.AsNoTracking() where doc.LoanID == loanid select doc.Document).ToList().OrderBy(x => x.DocOrder);

                        foreach (var doc in docQueryIVT)
                        {
                            TabPage tabPage = new TabPage()
                            {
                                Text = doc.Name,
                                ToolTipText = doc.Description,
                            };
                            this.uncheckedDictionary.Add(doc.Name, tabPage);
                            this.shortcutDictionary.Add(doc.ShortcutKey, tabPage); //DR - adds the shortcut and its tabpage to the dictionary
                            this.DatabaseDictionary.Add(doc.Name, new Dictionary<int, object>()); //DR - stores the name of the tab
                            this.UserInputDictionary.Add(doc.Name, new Dictionary<int, object>()); //MA - stores users input data
                            this.DocTimerDictionary.Add(doc.Name, null); //DR - adds the docs name to the DocTimerDictionary. Does not create the timer yet
                            this.DocTickDictionary.Add(doc.Name, 0);//DR - adds a fresh entry for a document with ticks equal to 0 because timer hasn't started yet
                            this.DocumentListDictionary.Add(doc.Id, new Dictionary<int, ComboBox>()); //DR - adds the document to the dictionary
                            this.FieldLabelDictionary.Add(doc.Id, new Dictionary<int, Label>());
                            this.FieldHasPromptDictionary.Add(doc.Id, new Dictionary<int, bool>()); //DR- adds the document to the dictionary
                            this.TabPageContainsWarningDictionary.Add(tabPage.Text, false);
                            TabPageWarningPromptDictionary.Add(tabPage.Text, null);

                            tabControl_Doc_Control.Controls.Add(tabPage);
                            TryPopulateDocumentFields(tabPage, doc.Id, doc.Name);
                        }
                        break;
                }

                //MA
                tabControl_Doc_Control.SelectedIndexChanged += new EventHandler((s, e) => tabPage_Click(s, e));//AO allows scroll on tab change
                LoadListDictionaries();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //DR - Tier 2
        private void TryPopulateDocumentTabs2()
        {
            try
            {

                toolTips = new ToolTip();

                //MA - Gets the documents that contain incorrect fields from tier 1 inputs
                var docQuery = (from documents in _AppEntity.vDocuments.AsNoTracking()
                                join UserData in _AppEntity.vUserInputs.AsNoTracking()
                                on documents.Id equals UserData.DocID
                                where (loanid == UserData.LoanID) && (UserData.isMatching == false) && (UserData.Reportable == true) && (UserData.InputID == 2) && (UserData.Workflow == workflowID)
                                select documents).Distinct().ToList().OrderBy(x => x.DocOrder);

                foreach (var doc in docQuery)
                {
                    TabPage tabPage = new TabPage()
                    {
                        Text = doc.Name,
                        ToolTipText = doc.Description
                    };
                    this.uncheckedDictionary.Add(doc.Name, tabPage);
                    this.shortcutDictionary.Add(doc.ShortcutKey, tabPage); //DR - adds the shortcut and its tabpage to the dictionary
                    this.DatabaseDictionary.Add(doc.Name, new Dictionary<int, object>()); //stores the name of the tab
                    this.UserInputDictionary.Add(doc.Name, new Dictionary<int, object>());
                    this.UserTier1Dictionary.Add(doc.Name, new Dictionary<int, object>());
                    this.fieldCountDictionary.Add(doc.Id, 0);
                    this.DocTimerDictionary.Add(doc.Name, null); //DR - adds the docs name to the DocTimerDictionary. Does not create the timer yet
                    this.DocTickDictionary.Add(doc.Name, 0);//DR - adds a fresh entry for a document with ticks equal to 0 because timer hasn't started yet
                    this.DocumentListDictionary.Add(doc.Id, new Dictionary<int, ComboBox>()); //DR - adds the document to the dictionary
                    this.FieldLabelDictionary.Add(doc.Id, new Dictionary<int, Label>());
                    tabControl_Doc_Control.Controls.Add(tabPage);

                    TryPopulateDocumentFields2(tabPage, doc.Id, doc.Name);
                }
                
                tabControl_Doc_Control.SelectedIndexChanged += new EventHandler((s, e) => tabPage_Click(s, e));//AO allows scroll on tab change
                LoadListDictionaries();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //DR - Tier 3
        private void TryPopulateDocumentTabs3()
        {
            try
            {

                toolTips = new ToolTip();
                tier3HasNonCritical = false;

                //MA -Gets the documents that contain incorrect fields from tier 2 inputs
                var docQuery = (from documents in _AppEntity.vDocuments.AsNoTracking()
                                join UserData in _AppEntity.vUserInputs.AsNoTracking()
                                on documents.Id equals UserData.DocID
                                where (loanid == UserData.LoanID) && (UserData.isMatching == false) && (UserData.Reportable == true) && (UserData.InputID == 3) && (UserData.Workflow == workflowID)
                                select documents).Distinct().ToList().OrderBy(x => x.DocOrder);

                foreach (var doc in docQuery)
                {
                    //MA - query to check if the doc has a unmatched critical field, if it doesn't it will not get loaded
                    var fieldQuery = (from field in _AppEntity.Fields.AsNoTracking()
                                      join UserIn in _AppEntity.UserInputs.AsNoTracking()
                                      on field.Id equals UserIn.FieldID
                                      join document in _AppEntity.Documents.AsNoTracking()
                                      on UserIn.DocID equals document.Id
                                      where UserIn.LoanID == loanid && (UserIn.isMatching == false)
                                      && UserIn.DocID == doc.Id
                                      && UserIn.InputID == 3
                                      select field).ToList().OrderBy(x => x.Name);
                    bool hasCritical = false;

                    foreach (var field in fieldQuery)
                    {
                        //DR - if there is a critical field, set hasCritical to true so we know to load the document
                        if (field.IsCritical)
                        {
                            hasCritical = true;
                        }
                        //DR - set global bool to true so finalsubmit button knows to escalate the loan to tier 4 
                        //     because non crit fields from tier2 need to be changed in msp
                        else
                        {
                            tier3HasNonCritical = true;
                        }
                    }

                    //DR - load the document if it has unmatched critical fields
                    if (hasCritical)
                    {
                        TabPage tabPage = new TabPage()
                        {
                            Text = doc.Name,
                            ToolTipText = doc.Description,
                            AutoScroll = true,
                        };

                        this.uncheckedDictionary.Add(doc.Name, tabPage);
                        this.shortcutDictionary.Add(doc.ShortcutKey, tabPage); //DR - adds the shortcut and its tabpage to the dictionary
                        this.DatabaseDictionary.Add(doc.Name, new Dictionary<int, object>()); //stores the name of the tab
                        this.UserInputDictionary.Add(doc.Name, new Dictionary<int, object>());
                        this.UserTier1Dictionary.Add(doc.Name, new Dictionary<int, object>());
                        this.UserTier2Dictionary.Add(doc.Name, new Dictionary<int, object>());
                        this.fieldCountDictionary.Add(doc.Id, 0);
                        this.DocTimerDictionary.Add(doc.Name, null); //DR - adds the docs name to the DocTimerDictionary. Does not create the timer yet
                        this.DocTickDictionary.Add(doc.Name, 0);//DR - adds a fresh entry for a document with ticks equal to 0 because timer hasn't started yet
                        this.DocumentListDictionary.Add(doc.Id, new Dictionary<int, ComboBox>()); //DR - adds the document to the dictionary
                        this.FieldLabelDictionary.Add(doc.Id, new Dictionary<int, Label>());
                        tabControl_Doc_Control.Controls.Add(tabPage);
                        TryPopulateDocumentFields3(tabPage, doc.Id, doc.Name);
                    }
                }
                tabControl_Doc_Control.SelectedIndexChanged += new EventHandler((s, e) => tabPage_Click(s, e));//AO allows scroll on tab change
                LoadListDictionaries();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void tablePanel_MouseDown(object sender, MouseEventArgs e, TableLayoutPanel tablePanel,int DocumentID)
        {
            Control control = tablePanel.GetChildAtPoint(e.Location);
            //DR - if control is not null
            if (control != null)
            {
                //DR - if control is disabled
                if (!control.Enabled)
                {

                    if (control.Text.ToString().Equals("") | control.Text.ToString().Equals(null))
                    {
                        if (control.GetType().ToString() == "Textbox")
                        {

                            return;
                        }
                    }

                    switch (_AppUser.ActiveRole)
                    {
                        case 2:
                        case 3:
                            //if (control.Name.Contains("17"))
                            if (Convert.ToInt32(ExtractID(control.Name, "Document")) == 17)
                            {
                                string fieldid = ExtractID(control.Name, "Control").ToString();
                                string ControlDateText = _AppEntity.GetControlDateForField(loanNum, fieldid, "Payment Line").Single();

                                if (ControlDateText != "")
                                {
                                    MessageBox.Show("Corresponding Control Date is " + ControlDateText);
                                }
                            }
                            else if (Convert.ToInt32(ExtractID(control.Name, "Document")) == 16)
                            {
                                string fieldid = ExtractID(control.Name, "Control").ToString();
                                string ControlDateText = _AppEntity.GetControlDateForField(loanNum, fieldid, "Arm").Single();

                                if ((ControlDateText ?? "") != "")
                                {
                                    MessageBox.Show("Corresponding Control Date is " + ControlDateText);
                                }
                            }
                            break;
                    }
                    //DR - if control's text is equal to empty string or null return without doing anything


                    //return;
                    //MessageBox.Show(control.Text.ToString());

                }
            }
        }


        private string ExtractID(string controlname, string Type)
        {   
            string id = "";
            string[] names = controlname.Split('_').ToArray();
            switch (Type)
            {
                case "Document":
                    if (names.Count() == 5)
                    {
                        id = names[2].ToString();
                    }
                    else if (names.Count() == 3)
                    {
                        id = names[2].ToString();
                    }
                    break;
                case "Control":
                    if (names.Count() == 3)
                    {
                        id = names[2].ToString();
                    }
                    else if (names.Count() == 5)
                    {
                        id = names[4].ToString();
                    }

                    if (controlname.Contains("FieldControl"))
                    {
                        id = names[2].ToString();
                    }
                    break;
            }

            return id;
        }

        //DR - check answer for textbox
        private void CheckAnswerTextBox(TabPage currentTab, int docId, MaskedTextBox textBox, int inputID)
        {
            try
            {
                //DR - declare placeholder variables
                int fieldId;
                string currentTabName;
                string dataType;
                string dbData;
                string userAnswer;

                fieldId = GetFieldID(textBox.Name);
                currentTabName = currentTab.Text;// ex "'Note'

                var dataTypeQuery = (from dt in _AppEntity.vFields.AsNoTracking() where dt.Id == fieldId select dt.Client_Data_Type).Single();
                dataType = dataTypeQuery.ToString();
                var dbEntry = DatabaseDictionary[currentTabName][fieldId];

                //DR - if dbEntry is null you cannot call toString() so set dbData to entry string
                if (dbEntry == null)
                {
                    dbData = "";
                }
                else
                {
                    dbData = dbEntry.ToString();
                }

                userAnswer = textBox.Text;
                UserInputDictionary[currentTabName][fieldId] = userAnswer;

                if (CompareData(userAnswer, dataType, dbData)) //DR - if comparedata returns true the comparison was a success
                {
                    if (inputID == 1)
                    {
                        textBox.Enabled = false;
                        DocumentListDictionary[docId][fieldId].Enabled = false;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, textBox.Text, inputID, 1, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }
                else
                {
                    if (inputID == 1)
                    {
                        errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
                        errorProvider1.SetError(textBox, "Data does not match MSP value. Validate the data you entered before submitting.");
                        textBox.ForeColor = Color.Red;

                        //DR- if certain fields are not matching and have a prompt message, add the prompt message so that it can be displayed once checkanswers is finished with all fields
                        if (FieldHasPromptDictionary[docId][fieldId])
                        {
                            checkAnswersBool = true;
                            checkAnswersPromptMessageCount++;
                            checkAnswersPromptMessageString = checkAnswersPromptMessageString + "\n" + checkAnswersPromptMessageCount + ": " + FieldPromptMessageDictionary[docId][fieldId] + "\n";
                        }
                    }
                    else
                    {
                        UnmatchedCount++;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, textBox.Text, inputID, 0, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }

                fieldId = 0;
                currentTabName = null;
                dataType = null;
                dbData = null;
                userAnswer = null;
                textBox = null;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //DR - check answer for checkbox
        private void CheckAnswerCheckBox(TabPage currentTab, int docId, CheckBox checkBox, int inputID)
        {
            try
            {
                int fieldId;
                string currentTabName;
                string dataType;
                string dbData;
                string userAnswer;
                fieldId = GetFieldID(checkBox.Name);
                currentTabName = currentTab.Text;// ex "'Note'

                var dataTypeQuery = (from dt in _AppEntity.vFields.AsNoTracking() where dt.Id == fieldId select dt.Client_Data_Type).Single();
                dataType = dataTypeQuery.ToString();
                var dbEntry = DatabaseDictionary[currentTabName][fieldId];

                //DR - if dbEntry is null you cannot call toString() so set dbData to entry string
                if (dbEntry == null)
                {
                    dbData = "";
                }
                else
                {
                    dbData = dbEntry.ToString();
                }


                if (checkBox.Checked)
                {
                    userAnswer = "Y";
                }
                else
                {
                    userAnswer = "N";
                }

                UserInputDictionary[currentTabName][fieldId] = userAnswer;

                if (CompareData(userAnswer, dataType, dbData)) //DR - if comparedata returns true the comparison was a success
                {
                    if (inputID == 1)
                    {
                        checkBox.Enabled = false;
                        DocumentListDictionary[docId][fieldId].Enabled = false;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, userAnswer, inputID, 1, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }
                else
                {
                    if (inputID == 1)
                    {
                        errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
                        errorProvider1.SetError(checkBox, "Data does not match MSP value. Validate the data you entered before submitting.");
                        checkBox.ForeColor = Color.Red;

                        //DR- if certain fields are not matching and have a prompt message, add the prompt message so that it can be displayed once checkanswers is finished with all fields
                        if (FieldHasPromptDictionary[docId][fieldId])
                        {
                            checkAnswersBool = true;
                            checkAnswersPromptMessageCount++;
                            checkAnswersPromptMessageString = checkAnswersPromptMessageString + "\n" + checkAnswersPromptMessageCount + ": " + FieldPromptMessageDictionary[docId][fieldId] + "\n";
                        }
                    }
                    else
                    {
                        UnmatchedCount++;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, userAnswer, inputID, 0, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }

                fieldId = 0;
                currentTabName = null;
                dataType = null;
                dbData = null;
                userAnswer = null;
                checkBox = null;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //DR - check answer for groupbox
        private void CheckAnswerGroupBox(TabPage currentTab, int docId, GroupBox groupBox, int inputID)
        {
            try
            {
                int fieldId;
                string currentTabName;
                string dataType;
                string dbData;
                string userAnswer;
                RadioButton selectedRadioButton = groupBox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);

                fieldId = GetFieldID(groupBox.Name);
                currentTabName = currentTab.Text;// ex "'Note'

                var dataTypeQuery = (from dt in _AppEntity.vFields.AsNoTracking() where dt.Id == fieldId select dt.Client_Data_Type).Single();
                dataType = dataTypeQuery.ToString();
                var dbEntry = DatabaseDictionary[currentTabName][fieldId];

                //DR - if dbEntry is null you cannot call toString() so set dbData to entry string
                if (dbEntry == null)
                {
                    dbData = "";
                }
                else
                {
                    dbData = dbEntry.ToString();
                }

                userAnswer = selectedRadioButton.Text;
                UserInputDictionary[currentTabName][fieldId] = userAnswer;

                if (CompareData(userAnswer, dataType, dbData)) //DR - if comparedata returns true the comparison was a success
                {
                    if (inputID == 1)
                    {
                        groupBox.Enabled = false;
                        DocumentListDictionary[docId][fieldId].Enabled = false;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, selectedRadioButton.Text, inputID, 1, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }
                else
                {
                    if (inputID == 1)
                    {
                        errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
                        errorProvider1.SetError(groupBox, "Data does not match MSP value. Validate the data you entered before submitting.");
                        selectedRadioButton.ForeColor = Color.Red;

                        //DR- if certain fields are not matching and have a prompt message, add the prompt message so that it can be displayed once checkanswers is finished with all fields
                        if (FieldHasPromptDictionary[docId][fieldId])
                        {
                            checkAnswersBool = true;
                            checkAnswersPromptMessageCount++;
                            checkAnswersPromptMessageString = checkAnswersPromptMessageString + "\n" + checkAnswersPromptMessageCount + ": " + FieldPromptMessageDictionary[docId][fieldId] + "\n";
                        }
                    }
                    else
                    {
                        UnmatchedCount++;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, selectedRadioButton.Text, inputID, 0, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }

                fieldId = 0;
                currentTabName = null;
                dataType = null;
                dbData = null;
                userAnswer = null;
                selectedRadioButton = null;
                groupBox = null;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //DR - check answer for combobox
        private void CheckAnswerComboBox(TabPage currentTab, int docId, ComboBox comboBox, int inputID)
        {
            try
            {
                int fieldId;
                string currentTabName;
                string dataType;
                string dbData;
                string userAnswer;
                fieldId = GetFieldID(comboBox.Name);
                currentTabName = currentTab.Text;// ex "'Note'

                var dataTypeQuery = (from dt in _AppEntity.vFields.AsNoTracking() where dt.Id == fieldId select dt.Client_Data_Type).Single();
                dataType = dataTypeQuery.ToString();
                var dbEntry = DatabaseDictionary[currentTabName][fieldId];

                //DR - if dbEntry is null you cannot call toString() so set dbData to entry string
                if (dbEntry == null)
                {
                    dbData = "";
                }
                else
                {
                    dbData = dbEntry.ToString();
                }

                userAnswer = comboBox.Text;
                UserInputDictionary[currentTabName][fieldId] = userAnswer;

                if (CompareData(userAnswer, dataType, dbData)) //DR - if comparedata returns true the comparison was a success
                {
                    if (inputID == 1)
                    {
                        comboBox.Enabled = false;
                        DocumentListDictionary[docId][fieldId].Enabled = false;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, comboBox.Text, inputID, 1, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }
                else
                {
                    if (inputID == 1)
                    {
                        errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
                        errorProvider1.SetError(comboBox, "Data does not match MSP value. Validate the data you entered before submitting.");
                        comboBox.ForeColor = Color.Red;

                        //DR- if certain fields are not matching and have a prompt message, add the prompt message so that it can be displayed once checkanswers is finished with all fields
                        if (FieldHasPromptDictionary[docId][fieldId])
                        {
                            checkAnswersBool = true;
                            checkAnswersPromptMessageCount++;
                            checkAnswersPromptMessageString = checkAnswersPromptMessageString + "\n" + checkAnswersPromptMessageCount + ": " + FieldPromptMessageDictionary[docId][fieldId] + "\n";
                        }
                    }
                    else
                    {
                        UnmatchedCount++;
                    }
                    LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docId, fieldId, comboBox.Text, inputID, 0, DocumentListDictionary[docId][fieldId].Text, workflowID, GetCurrentMSPValue(docId, fieldId), 1);
                }

                fieldId = 0;
                currentTabName = null;
                dataType = null;
                dbData = null;
                userAnswer = null;
                comboBox = null;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //DR - compares data based on the data type and returns true if they're equal and false if they are not equal
        private bool CompareData(string userInput, string dataType, string dbData)
        {
            //DR - if both dbData and userInput are equal to null or empty string return true
            if ((userInput == null | userInput.Trim() == "") && (dbData == null | dbData.Trim() == ""))
            {
                //DR - don't return true if it's a recon fee comment code
                if (dataType != "commentcode" && dataType != "prepaymentpenalty")
                {
                    return true;
                }
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
                    //DR - prepayment penalty always gets escalated unless they didn't enter data
                    //string uinput7 = userInput;
                    //uinput7 = uinput7.Replace("Pre-payment penalty expired:(", "");
                    //uinput7 = uinput7.Replace(")", "");
                    //uinput7 = uinput7.Replace("/", "");
                    //if (uinput7.Trim().Equals(""))
                    //{
                    //    return true;
                    //}
                    return false;
                case "commentcode":
                    //DR - always return false for the recon fee comment code
                    return false;
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
                    if (uInputArr.Length < 2)
                    {
                        return false;
                    }

                    if (!DateTime.TryParse(dbData, out dData10)| !int.TryParse(uInputArr[0], out uInputMonth) 
                        | !int.TryParse(uInputArr[1], out uInputYear))
                    {
                        return false;
                    }

                    if(dData10.Month == uInputMonth && (dData10.Year % 100) == uInputYear)
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

        /// <summary>
        /// MA - Retrieves the last login of the user
        /// </summary>
        private void GetLastLogin()
        {
            int id = _AppUser.Id;
            var query = from user in _AppEntity.Users.AsNoTracking() where user.Id == id select user;

            foreach (var user in query)
            {
                //MA - Formatted Last Login Date for cleaner look on GUI
                DateTime dt = (DateTime)user.LastLogin;
                toolStripStatusLogin.Text = dt.Month + "/" + dt.Day + "/" + dt.ToString("yy") + " " + dt.ToShortTimeString();
            }
        }

        //DR - initializes the document timer
        public void InitDocTimer(String docName)
        {
            DocTimerDictionary[docName] = new System.Windows.Forms.Timer();
            DocTimerDictionary[docName].Tick += new EventHandler((s, e) => DocTimer_Tick(s, e, docName));
            DocTimerDictionary[docName].Interval = 1000;
            DocTimerDictionary[docName].Start();
        }

        //DR - pause all the timers
        public void PauseDocTimers()
        {
            foreach (var timer in DocTimerDictionary)
            {
                if (timer.Value != null)
                {
                    timer.Value.Stop();
                }
            }
        }
        //DR - dispose all timers
        public void DisposeDocTimers()
        {
            //Dr- return without doing anything if doctimer is = null
            if (DocTimerDictionary == null)
            {
                return;
            }

            foreach (var timer in DocTimerDictionary)
            {
                if (timer.Value != null)
                {
                    timer.Value.Stop();
                    timer.Value.Dispose();
                }

            }
        }

        //DR - initializes the loan timer
        public void InitTimer()
        {
            loanTimer = new System.Timers.Timer(15000);
            loanTimer.Elapsed += LoanTimer_Tick;
            loanTimer.Start();
            LoanTimer_Tick(null, null);
        }

        //DR - Sets tier counts and role counts for GUI labels
        private void CountGUI()
        {
            try
            {
                int tier1Count = 0;
                int tier2Count = 0;
                int tier3Count = 0;
                int tier4Count = 0;
                int role1Count = 0;
                int role2Count = 0;
                int role3Count = 0;

                //MA - Determine which workflow the user is on
                switch (workflowID)
                {
                    case 1:

                        //MA - Gets the amount of loan in normal workflow
                        var tq = (from loan in _AppEntity.vLoanDatas
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


                        break;
                    case 2:
                        //MA - Gets the amount of loan in IMAVAL workflow
                        var IVTtq = (from loan in _AppEntity.vWorkflowTracking_Loandata
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
                                tier1Count = count.Count;
                            }
                            else if (count.WorkflowTier == 2)
                            {
                                tier2Count = count.Count;
                            }
                            else if (count.WorkflowTier == 3)
                            {
                                tier3Count = count.Count;
                            }


                        }

                        break;
                }

                //MA - Gets the amount of users logged in into the 
                var roleQuery = (from users in _AppEntity.vUsers.AsNoTracking()
                                 where users.IsActive == true
                                 group users by users.ActiveRole
                                     into usr
                                     select new
                                     {
                                         Key = usr.Key,
                                         Count = usr.Select(x => x.Id).Distinct().Count()
                                     });

                var userQuery = from users in roleQuery select users;


                //MA - Stores the amount of users in each tier
                foreach (var user in userQuery)
                {
                    if (user.Key == 1)
                    {
                        role1Count = user.Count;
                    }
                    else if (user.Key == 2)
                    {
                        role2Count = user.Count;
                    }
                    else if (user.Key == 3)
                    {
                        role3Count = user.Count;
                    }

                }

                //var roleQuery = from u in _AppEntity.vUsers.AsNoTracking() where u.IsActive == true select u;

                //foreach(var user in roleQuery)
                //{
                //    switch (user.ActiveRole)
                //    {
                //        case 1:
                //            role1Count++;
                //            break;
                //        case 2:
                //            role2Count++;
                //            break;
                //        case 3:
                //            role3Count++;
                //            break;
                //    }
                //}

                //MA - Forces the labels for the amounts to change despite the thread currently active
                Invoke((MethodInvoker)(() => ChangeGUI(tier1Count, tier2Count, tier3Count, role1Count, role2Count, role3Count)));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        /// <summary>
        /// MA - Sets the values for the labels indicating the amount 
        /// loans in each tier for each workflow as well as the amount
        /// of users currently logged on each tier
        /// </summary>
        /// <param name="tier1Count"></param>
        /// <param name="tier2Count"></param>
        /// <param name="tier3Count"></param>
        /// <param name="role1Count"></param>
        /// <param name="role2Count"></param>
        /// <param name="role3Count"></param>
        private void ChangeGUI(int tier1Count, int tier2Count, int tier3Count, int role1Count, int role2Count, int role3Count)
        {
            try
            {
                if (workflowID == 2)
                {
                    Tier1Label.Text = "IVT Tier I:";
                    Tier2Label.Text = "IVT Tier II:";
                    Tier3Label.Text = "IVT Tier III:";
                }
                else
                {
                    Tier1Label.Text = "Tier I:";
                    Tier2Label.Text = "Tier II:";
                    Tier3Label.Text = "Tier III:";
                }
                lblTierI.Text = tier1Count.ToString();
                lblTierII.Text = tier2Count.ToString();
                lblTierIII.Text = tier3Count.ToString();

                if (tier1Count == 0) { lblTierI.ForeColor = Color.Red; } else lblTierI.ForeColor = Color.Black;
                lblTierII.Text = tier2Count.ToString();
                if (tier2Count == 0) { lblTierII.ForeColor = Color.Red; } else lblTierII.ForeColor = Color.Black;
                lblTierIII.Text = tier3Count.ToString();
                if (tier3Count == 0) { lblTierIII.ForeColor = Color.Red; } else lblTierIII.ForeColor = Color.Black;

                lblRoleI.Text = role1Count.ToString();
                lblRoleII.Text = role2Count.ToString();
                lblRoleIII.Text = role3Count.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        /// <summary>
        /// MA - checks the session status of the user
        /// </summary>
        private void CheckSessionStatus()
        {
            try
            {
                //MA - Check if the server is active on a server
                var Server = (from Users in _AppEntity.Users.AsNoTracking()
                              where Users.Id == _AppUser.Id
                              select new { Users.ActiveServer, Users.IsActive }).Single();

                //MA - If the active server doesnt match the current server or the session is
                // inactive then log the user off
                if (Server.IsActive == false | Server.ActiveServer != Environment.MachineName)
                {
                    //DR - if statement is required so terminated messagebox only appears once
                    if (!isTerminated)
                    {
                        isTerminated = true;

                        if (loanid > 0)
                        {
                            switch (_AppUser.ActiveRole)
                            {
                                case 1:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                    }
                                    break;
                                case 2:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                    }
                                    break;
                                case 3:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                    }
                                    break;
                            }
                        }
                        //MessageBox.Show("Your Session has been terminated. This Application will be closed.", "Session Terminated", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        Application.Exit();
                    }


                }
            }
            catch (Exception ex)
            {
                throw;
            }


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
                pinger.Dispose();
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
                return false;
            }
            return pingable;
        }

        private void CheckDBConnection()
        {
            try
            {
                if (!PingHost(ConfigurationManager.AppSettings["IP Address"]))
                {

                    //DR - checkDBconnection variable is always false so it will run once
                    if (!checkDBConnection)
                    {
                        ConnectionImage = false;
                        toolStripStatusDBpic.Image = LoanReview.Properties.Resources.Connection_Bad;
                        toolStripStatusDBpic.Invalidate();

                        //DR - set checkDBConnection variable to true so timer doesn't pop multiple messageboxes
                        checkDBConnection = true;

                        var dialogResult = MessageBox.Show("Database connection is unavailable. Retry to test conection. Cancel to close application", "Database connection is lost", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        //DR - set checkDBConnection to false and retry the connection
                        if (dialogResult == DialogResult.Retry)
                        {
                            checkDBConnection = false;
                            CheckDBConnection();
                        }
                        else //DR - they hit cancel or close which means they wish to exit the application
                        {

                            var dialogResult2 = MessageBox.Show("Are you sure you wish to close the application? Data will be lost", "Application Exit", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                            //DR - exit the application if they hit ok
                            if (dialogResult2 == DialogResult.OK)
                            {
                                Application.Exit();
                            }
                            else //DR - they hit cancel or close which means they wish to retry the connection
                            {
                                checkDBConnection = false;
                                CheckDBConnection();
                            }
                        }
                    }
                }
                else
                {
                    //MA
                    if (ConnectionImage == false)
                    {
                        toolStripStatusDBpic.Image = LoanReview.Properties.Resources.Connection_Good;
                        ConnectionImage = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// MA - populates the local dictionary that holds Database Data
        /// </summary>
        /// <param name="loanNum"></param>
        private void TryLoadDBDictionary(int loanNum)
        {
            try
            {
                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier1
                    case 1:
                        lblLoanID.Text = loanid.ToString();
                        lblLoanNumber.Text = this.loanNum.ToString();
                        lblPaymentDueDate.Text = PaymentDueDate;
                        lblDealID.Text = DealID.ToString();
                        Loanbtn.Enabled = false;
                        tabControl_Doc_Control.Enabled = true;

                        //DR-query for the row of data based on the loadID
                        var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();

                        //DR-nested loops to add the data from the table in the database to our local dictionary
                        //DR-iterates through each tab
                        foreach (var currentTab in DatabaseDictionary)
                        {
                            string tabKey = currentTab.Key;

                            //DR-iterates though each field in the tab and stores the value from columns
                            for (int i = 0; i <= DatabaseDictionary[tabKey].Count - 1; i++)
                            {
                                int fID = DatabaseDictionary[tabKey].ElementAt(i).Key;
                                DatabaseDictionary[tabKey][fID] = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);
                            }
                        }
                        break;
                    //DR - Tier 2 + 3
                    default:
                        lblLoanID.Text = loanid.ToString();
                        lblLoanNumber.Text = this.loanNum.ToString();
                        lblPaymentDueDate.Text = PaymentDueDate;
                        lblDealID.Text = DealID.ToString();
                        Loanbtn.Enabled = false;
                        tabControl_Doc_Control.Enabled = true;
                        finalSubmitbtn.Enabled = true;

                        //DR-query for the row of data based on the loadID
                        var loanDataQuery2 = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();

                        //DR-nested loops to add the data from the table in the database to our local dictionary
                        //DR-iterates through each tab
                        foreach (var currentTab in DatabaseDictionary)
                        {
                            string tabKey = currentTab.Key;

                            //DR-iterates though each field in the tab and stores the value from columns
                            for (int i = 0; i <= DatabaseDictionary[tabKey].Count - 1; i++)
                            {
                                int fID = DatabaseDictionary[tabKey].ElementAt(i).Key;
                                DatabaseDictionary[tabKey][fID] = loanDataQuery2.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery2);
                            }
                        }
                        //Iterate through each tab.
                        foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                        {
                            string DocumentKey = @tp.Text;

                            foreach (TableLayoutPanel Panel in tp.Controls.OfType<TableLayoutPanel>())
                            {
                                foreach (var tempBox in Panel.Controls.OfType<TextBox>())
                                {
                                    if (tempBox.Name.Contains("_MSPtextBox_")) //DR & MA - check to see if it's an MSPtextBox and assign a value to it
                                    {
                                        int fID = GetFieldID(tempBox.Name);

                                        //DR - if database value is equal to null or empty string, set textbox to ""
                                        if (DatabaseDictionary[DocumentKey][fID] != null && !DatabaseDictionary[DocumentKey][fID].ToString().Equals(""))
                                        {
                                            tempBox.Text = DatabaseDictionary[DocumentKey][fID].ToString();
                                            toolTips.SetToolTip(tempBox, tempBox.Text.ToString());
                                        }
                                        else
                                        {
                                            tempBox.Text = "";
                                            DatabaseDictionary[DocumentKey][fID] = "";
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }

        //DR - checks to see if there are available loans for the user
        private bool LoanAvailabilityCheck()
        {
            int count = 0;
            switch (workflowID)
            {
                case 1:
                    count = (from LD in _AppEntity.vLoanDatas.AsNoTracking()
                             where !(from UI in _AppEntity.UserInputs
                                     where UI.UserID == _AppUser.Id
                                     select UI.LoanID).Contains(LD.ID)
                                     && (LD.Tier == _AppUser.ActiveRole) && (LD.NormalWorkflowCompleted == false) && (LD.OwnedByUserID == null)
                             select LD.ID).Count();
                    break;
                case 2:
                    count = (from WT in _AppEntity.vWorkflowTracking_Loandata.AsNoTracking()
                             where !(from UI in _AppEntity.UserInputs
                                     where UI.UserID == _AppUser.Id && UI.Workflow == 2
                                     select UI.LoanID).Contains(WT.LoanID)
                                     && (WT.TaskStatus == 2)
                                     && (WT.WorkflowStatus == false)
                                     && (WT.WorkflowTier == _AppUser.ActiveRole)
                                     && (WT.NormalWorkflowCompleted == true)
                                     && (WT.OwnedByUserID == null)
                             select WT.LoanID).Distinct().Count();
                    break;
            }


            if (count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// MA - Checks if all tabs are submitted
        /// </summary>
        /// <returns></returns>
        private bool areAllTabsSubmitted()
        {
            bool result = true;

            if (uncheckedDictionary.Count() > 0)
            {
                result = false;
            }
            return result;
        }
        //DR - Tier 1
        private void FinalSubmit()
        {
            try
            {
                if (!areAllTabsSubmitted())
                {
                    var empyText = MessageBox.Show("Please submit all Documents before Submitting Loan", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.Refresh();
                LoadingStartProgress("Submitting");
                //Iterate through each tab.
                foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                {
                    string DocumentKey = @tp.Text;
                    int docId = (from doc in _AppEntity.vDocuments.AsNoTracking()
                                 where doc.Name == DocumentKey
                                 select doc.Id).Single();

                    foreach (TableLayoutPanel Panel in tp.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR - declare placeholder variables
                        MaskedTextBox textBox;
                        CheckBox checkBox;
                        GroupBox groupBox;
                        ComboBox comboBox;

                        //DR - loop through each control and do the appropriate comparison based on its controltype
                        foreach (Control c in Panel.Controls)
                        {
                            //DR - the control must be have fieldcontrol in its name 
                            if (c.Name.ToLower().Contains("fieldcontrol"))
                            {
                                int fID = GetFieldID(c.Name);
                                //DR - if the field is matching skip it to reduce redundant data or skip it if it is overriden
                                if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, docId, fID, 1, workflowID) && !LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, docId, fID, 2, workflowID))
                                {
                                    //DR - if control is of type textbox
                                    if (c.Name.ToLower().Contains("textbox"))
                                    {
                                        textBox = (MaskedTextBox)c;
                                        CheckAnswerTextBox(tp, docId, textBox, 2);
                                        textBox = null;
                                    }
                                    //DR - if control is of type checkbox
                                    else if (c.Name.ToLower().Contains("checkbox"))
                                    {
                                        checkBox = (CheckBox)c;
                                        CheckAnswerCheckBox(tp, docId, checkBox, 2);
                                        checkBox = null;
                                    }
                                    //DR - if control is of type groupbox
                                    else if (c.Name.ToLower().Contains("groupbox"))
                                    {
                                        groupBox = (GroupBox)c;
                                        CheckAnswerGroupBox(tp, docId, groupBox, 2);
                                        groupBox = null;
                                    }
                                    //DR - if control is of type combobox
                                    else if (c.Name.ToLower().Contains("combobox"))
                                    {
                                        comboBox = (ComboBox)c;
                                        CheckAnswerComboBox(tp, docId, comboBox, 2);
                                        comboBox = null;
                                    }
                                }
                            }
                        }
                    }
                }

                //DR - execute redundantfield logic
                foreach (KeyValuePair<int, Dictionary<int, ArrayList>> dictionary in RedundantFieldsDictionary)
                {
                    foreach (KeyValuePair<int, ArrayList> field in dictionary.Value)
                    {
                        //DR - if it's not overriden go ahead and set its minor fields to not reportable
                        if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, dictionary.Key, field.Key, 2, workflowID))
                        {
                            //DR - Tuple.Item1 is Minor DocID and Tuple.Item2 is Minor FieldID
                            foreach (Tuple<int, int> tuple in field.Value)
                            {
                                //DR - if the minor field wasn't matching before, decrease unmatchedcount so the loan doesn't escalate off of non reportable fields
                                if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, tuple.Item1, tuple.Item2, 2, workflowID) && LocalDatabase.LocalIsReportable(loanid, _AppUser.Id, tuple.Item1, tuple.Item2, 2, workflowID, 1))
                                {
                                    UnmatchedCount--;
                                }
                                LocalDatabase.LocalUpdateReportable(loanid, _AppUser.Id, tuple.Item1, tuple.Item2, 2, 0);
                                LocalDatabase.LocalUpdateReportable(loanid, _AppUser.Id, tuple.Item1, tuple.Item2, 1, 0);
                            }
                        }
                    }
                }

                LocalDatabase.TransferDataToLiveDB();
                PauseDocTimers();
                LocalDatabase.TransferDocTicksToLive(DocTickDictionary);

                //DR - a check to see if all inputs made it to liveDB
                if (!(LocalDatabase.WasUserInputTransferSuccessful(1) && LocalDatabase.WasUserInputTransferSuccessful(2)))//DR - transfer failed 1st time
                {
                    //DR - remove userinputs from liveDB if there were any and try transfer again
                    _AppEntity.RemoveUserInput(loanid, _AppUser.Id, 1, workflowID);
                    _AppEntity.RemoveUserInput(loanid, _AppUser.Id, 2, workflowID);
                    LocalDatabase.TransferDataToLiveDB();
                    if (!(LocalDatabase.WasUserInputTransferSuccessful(1) && LocalDatabase.WasUserInputTransferSuccessful(2))) //DR - transfer failed 2nd time remove userinput again if there were any
                    {
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                        }
                        String email = _AppUser.Email;


                        EmailMessage emailMessage = new EmailMessage(email, "Loan Failed to sumbit", " LoanID: " + loanid.ToString() + " UserName: " + _AppUser.Name + " UserID: " + _AppUser.Id.ToString() + " Tier: " + _AppUser.ActiveRole.ToString() + " Workflow: " + workflowID + " Time tried to submit: " + DateTime.Now.ToString());

                        LoadingCloseProgress();
                        MessageBox.Show("Loan failed to submit. An email was sent to your supervisor", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        resetControls();
                        emailMessage = null;
                        return;
                    }
                }

                switch (workflowID)
                {
                    case 1:
                        if (UnmatchedCount > 0)
                        {
                            _AppEntity.LoanEscalation(0, _AppUser.ActiveRole.Value, _AppUser.Id, loanid);
                        }
                        else
                        {
                            _AppEntity.LoanEscalation(1, _AppUser.ActiveRole.Value, _AppUser.Id, loanid);
                        }
                        break;
                    case 2:
                        if (UnmatchedCount > 0)
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 0);
                        }
                        else
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 1);
                        }
                        break;
                }
                //DR - Adds the loan submitted action to the Histories table
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 7, workflowID);

                LoadingCloseProgress();
                var t = MessageBox.Show("Loan Successfully Submitted!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                loanCompletedCount++;

                finalSubmitClicked = true; //DR - lets the formclosing know not to delete userinput

                resetControls();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //DR - Tier 2
        private void FinalSubmit2()
        {
            try
            {
                if (uncheckedDictionary.Count > 0)
                {
                    MessageBox.Show("Please select a checkbox for each field before sumbitting", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                this.Refresh();
                LoadingStartProgress("Submitting");

                int fieldNonCriticalCount = 0;

                //DR - iterates through controls to increment unmatched count or not based on user's selection
                foreach (TabPage tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                {
                    int docID = (from doc in _AppEntity.vDocuments.AsNoTracking() where doc.Name == tp.Text select doc.Id).Single();

                    foreach (TableLayoutPanel tablePanel in tp.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR - checks to see if the document is flagged for IVT, if it's not do regular final submit
                        if (!imageValList.Contains(docID))
                        {
                            foreach (CheckBox checkbox in tablePanel.Controls.OfType<CheckBox>())
                            {
                                if (checkbox.Checked)
                                {
                                    int FieldID = GetFieldID(checkbox.Name);

                                    if (checkbox.Name.ToLower().Contains("mspcheckbox"))
                                    {
                                        LocalDatabase.LocalUpdateSelectedDocument(loanid, _AppUser.Id, docID, FieldID, 3, DocumentListDictionary[docID][FieldID].Text);
                                    }
                                    //DR - increment unmatched count 
                                    else if (checkbox.Name.ToLower().Contains("tier1entrycheckbox"))
                                    {
                                        LocalDatabase.LocalUpdateSelectedDocument(loanid, _AppUser.Id, docID, FieldID, 3, DocumentListDictionary[docID][FieldID].Text);
                                        UnmatchedCount++;
                                        //DR - if field is not critical, increment fieldNonCriticalCount
                                        if (!fieldCriticalCheckDictionary[FieldID])
                                        {
                                            fieldNonCriticalCount++;
                                        }
                                    }
                                    //DR - update userinput with actual value
                                    else if (checkbox.Name.ToLower().Contains("usertier2checkbox"))
                                    {
                                        Control control = null;

                                        foreach (Control c in tablePanel.Controls.OfType<Control>())
                                        {
                                            if (c.Name.ToLower().Contains("fieldcontrol") && (GetFieldID(c.Name).Equals(FieldID)))
                                            {
                                                control = c;
                                            }
                                        }

                                        string userAnswer = "";

                                        //DR - if control is of type textbox
                                        if (control.Name.ToLower().Contains("textbox"))
                                        {
                                            MaskedTextBox textbox = (MaskedTextBox)control;
                                            userAnswer = textbox.Text;
                                            textbox = null;
                                        }
                                        else if (control.Name.ToLower().Contains("checkbox"))
                                        //DR - if control is of type checkbox
                                        {
                                            CheckBox chkbox = (CheckBox)control;
                                            if (chkbox.Checked)
                                            {
                                                userAnswer = "Y";
                                            }
                                            else
                                            {
                                                userAnswer = "N";
                                            }
                                            chkbox = null;
                                        }
                                        //DR - if control is of type groupbox
                                        else if (control.Name.ToLower().Contains("groupbox"))
                                        {
                                            GroupBox groupbox = (GroupBox)control;
                                            RadioButton selectedRadioButton = groupbox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
                                            userAnswer = selectedRadioButton.Text;
                                            groupbox = null;
                                            selectedRadioButton = null;
                                        }
                                        //DR - if control is of type combobox
                                        else if (control.Name.ToLower().Contains("combobox"))
                                        {
                                            ComboBox combobox = (ComboBox)control;
                                            userAnswer = combobox.Text;
                                            combobox = null;
                                        }
                                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, FieldID, userAnswer, 3, 0, DocumentListDictionary[docID][FieldID].Text, workflowID);
                                        control = null;
                                        UnmatchedCount++;
                                        //DR - if field is not critical, increment fieldNonCriticalCount
                                        if (!fieldCriticalCheckDictionary[GetFieldID(checkbox.Name)])
                                        {
                                            fieldNonCriticalCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //DR - Transfer the information from the local DB to the Imaging Apps Database
                LocalDatabase.TransferDataToLiveDB();

                //DR - Stop all the document timers
                PauseDocTimers();

                //DR - Transfer the values of each document timer to the Imaging Apps Database
                LocalDatabase.TransferDocTicksToLive(DocTickDictionary);
                bool isThereIVT = LocalDatabase.TransferWorkflowToLive(imageValList);

                //DR - a check to see if all userInputs made it to liveDB
                if (!LocalDatabase.WasUserInputTransferSuccessful(3))//DR - transfer failed 1st time
                {
                    //DR - remove userinputs from liveDB if there were any and try transfer again
                    _AppEntity.RemoveUserInput(loanid, _AppUser.Id, 3, workflowID);
                    LocalDatabase.TransferDataToLiveDB();
                    if (!LocalDatabase.WasUserInputTransferSuccessful(3)) //DR - transfer failed 2nd time remove userinput again if there were any
                    {
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                        }
                        String email = _AppUser.Email;

                        EmailMessage emailMessage = new EmailMessage(email, "Loan Failed to sumbit", " LoanID: " + loanid.ToString() + " UserName: " + _AppUser.Name + " UserID: " + _AppUser.Id.ToString() + " Tier: " + _AppUser.ActiveRole.ToString() + " Workflow: " + workflowID + " Time tried to submit: " + DateTime.Now.ToString());

                        LoadingCloseProgress();
                        MessageBox.Show("Loan failed to submit. An email was sent to your supervisor", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        resetControls();
                        return;
                    }
                }

                if (workflowID == 1)
                {
                    //DR - a check to see if all workflows made it to workflowtracking table
                    if (!LocalDatabase.WasWorkflowTransferSuccessful(2, imageValList))//DR - transfer failed 1st time
                    {
                        //DR - remove workflows from WorkflowTracking table if there were any and try transfer again
                        _AppEntity.DeleteFromWorkflowTracking(loanid, 2, 1);
                        LocalDatabase.TransferWorkflowToLive(imageValList);
                        if (!LocalDatabase.WasWorkflowTransferSuccessful(2, imageValList)) //DR - transfer failed 2nd time remove workflows again if there were any and remove userinputs
                        {
                            //DR - remove workflows from WorkflowTracking table if there were any and try transfer again
                            _AppEntity.DeleteFromWorkflowTracking(loanid, 2, 1);
                            LocalDatabase.TransferWorkflowToLive(imageValList);

                            if (!LocalDatabase.WasWorkflowTransferSuccessful(2, imageValList)) //DR - transfer failed 3rd time remove workflows again if there were any and remove userinputs
                            {
                                _AppEntity.DeleteFromWorkflowTracking(loanid, 2, 1);
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);

                                String email = _AppUser.Email;


                                EmailMessage emailMessage = new EmailMessage(email, "Loan Failed to Submit because task could not be created", " LoanID: " + loanid.ToString() + " UserName: " + _AppUser.Name + " UserID: " + _AppUser.Id.ToString() + " Tier: " + _AppUser.ActiveRole.ToString() + " Workflow: " + workflowID + " Time tried to submit: " + DateTime.Now.ToString());

                                LoadingCloseProgress();
                                MessageBox.Show("Loan failed to submit. An email was sent to your supervisor", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                resetControls();
                                emailMessage = null;
                                return;
                            }
                        }
                    }
                    //MA - Close the loading form
                    LoadingCloseProgress();
                    if (isThereIVT)
                    {
                    //DR - add IVTComment
                    RedoInput:
                        string input = "";
                        input = ShowDialog("You have flagged this loan to be opened for a Image Validation Task. Please enter a comment explaining why: ", "Enter a reason for IVT");

                        if (input == null)
                        {
                            goto RedoInput;
                        }
                        else if (input.Trim() == "")
                        {
                            goto RedoInput;
                        }

                        int count = (from c in _AppEntity.IVTComments.AsNoTracking() where c.LoanID == loanid && c.UserID == _AppUser.Id select c).Count();

                        if (count > 0)
                        {
                            _AppEntity.RemoveIVTComment(loanid, _AppUser.Id);
                        }
                        _AppEntity.InsertIVTComment(loanid, _AppUser.Id, input);
                    }

                    //MA - Start the loading form
                    LoadingStartProgress("Submitting");
                }


                switch (workflowID)
                {
                    case 1:
                        //DR - if all the unmatchedcounts are non critical fields escalate the loan directly to tier 4
                        if (UnmatchedCount > 0 && (UnmatchedCount == fieldNonCriticalCount))
                        {
                            //DR - escalate the loan to tier 4
                            _AppEntity.LoanEscalation(0, _AppUser.ActiveRole + 1, _AppUser.Id, loanid);
                        }
                        //MA: Check if there are any unmatched fields
                        else if (UnmatchedCount > 0)
                        {
                            //MA: If yes, escalate the loan.
                            _AppEntity.LoanEscalation(0, _AppUser.ActiveRole, _AppUser.Id, loanid);
                        }
                        else
                        {
                            //MA: If no, set the loan to completed.
                            _AppEntity.LoanEscalation(1, _AppUser.ActiveRole, _AppUser.Id, loanid);
                        }

                        break;
                    case 2:
                        //DR - if all the unmatchedcounts are non critical fields escalate the loan directly to tier 4
                        if (UnmatchedCount > 0 && (UnmatchedCount == fieldNonCriticalCount))
                        {
                            //DR - escalate the loan to tier 4
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole + 1, 0);
                        }
                        else if (UnmatchedCount > 0)
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 0);
                        }
                        else
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 1);
                        }
                        break;
                }

                //DR - Adds the loan submitted action to the Histories table
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 7, workflowID);

                //MA - Close the loading form
                LoadingCloseProgress();

                var t = MessageBox.Show("Loan Successfully Submitted!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                loanCompletedCount++;

                finalSubmitClicked = true;//DR - lets the formclosing know not to delete userinput

                //MA - Resets all controls and variables to default values
                resetControls();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //DR - Tier 3
        private void FinalSubmit3()
        {
            try
            {
                if (uncheckedDictionary.Count > 0)
                {
                    MessageBox.Show("Please select a checkbox for each field before sumbitting", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                this.Refresh();
                LoadingStartProgress("Submitting");
                //DR - iterates through controls to increment unmatched count or not based on user's selection
                foreach (TabPage tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                {
                    int docID = (from doc in _AppEntity.vDocuments.AsNoTracking() where doc.Name == tp.Text select doc.Id).Single();

                    foreach (TableLayoutPanel tablePanel in tp.Controls.OfType<TableLayoutPanel>())
                    {
                        foreach (CheckBox checkbox in tablePanel.Controls.OfType<CheckBox>())
                        {
                            if (checkbox.Checked)
                            {
                                int FieldID = GetFieldID(checkbox.Name);
                                if (checkbox.Name.ToLower().Contains("mspcheckbox"))
                                {
                                    LocalDatabase.LocalUpdateSelectedDocument(loanid, _AppUser.Id, docID, FieldID, 4, DocumentListDictionary[docID][FieldID].Text);
                                }
                                //DR - increment unmatched count 
                                else if (checkbox.Name.ToLower().Contains("tier1entrycheckbox"))
                                {
                                    LocalDatabase.LocalUpdateSelectedDocument(loanid, _AppUser.Id, docID, FieldID, 4, DocumentListDictionary[docID][FieldID].Text);
                                    UnmatchedCount++;
                                }
                                else if (checkbox.Name.ToLower().Contains("tier2entrycheckbox"))
                                {
                                    LocalDatabase.LocalUpdateSelectedDocument(loanid, _AppUser.Id, docID, FieldID, 4, DocumentListDictionary[docID][FieldID].Text);
                                    UnmatchedCount++;
                                }
                                else if (checkbox.Name.ToLower().Contains("usertier3checkbox"))
                                {
                                    Control control = null;

                                    foreach (Control c in tablePanel.Controls.OfType<Control>())
                                    {
                                        if (c.Name.ToLower().Contains("fieldcontrol") && (GetFieldID(c.Name).Equals(FieldID)))
                                        {
                                            control = c;
                                        }
                                    }

                                    string userAnswer = "";

                                    //DR - if control is of type textbox
                                    if (control.Name.ToLower().Contains("textbox"))
                                    {
                                        MaskedTextBox textbox = (MaskedTextBox)control;
                                        userAnswer = textbox.Text;
                                        textbox = null;
                                    }
                                    else if (control.Name.ToLower().Contains("checkbox"))
                                    //DR - if control is of type checkbox
                                    {
                                        CheckBox chkbox = (CheckBox)control;
                                        if (chkbox.Checked)
                                        {
                                            userAnswer = "Y";
                                        }
                                        else
                                        {
                                            userAnswer = "N";
                                        }
                                        chkbox = null;
                                    }
                                    //DR - if control is of type groupbox
                                    else if (control.Name.ToLower().Contains("groupbox"))
                                    {
                                        GroupBox groupbox = (GroupBox)control;
                                        RadioButton selectedRadioButton = groupbox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
                                        userAnswer = selectedRadioButton.Text;
                                        groupbox = null;
                                        selectedRadioButton = null;
                                    }
                                    //DR - if control is of type combobox
                                    else if (control.Name.ToLower().Contains("combobox"))
                                    {
                                        ComboBox combobox = (ComboBox)control;
                                        userAnswer = combobox.Text;
                                        combobox = null;
                                    }
                                    LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, FieldID, userAnswer, 4, 0, DocumentListDictionary[docID][FieldID].Text, workflowID);
                                    control = null;
                                    UnmatchedCount++;
                                }
                            }
                        }
                    }
                }

                LocalDatabase.TransferDataToLiveDB();
                PauseDocTimers();
                LocalDatabase.TransferDocTicksToLive(DocTickDictionary);

                //DR - a check to see if all inputs made it to liveDB
                if (!LocalDatabase.WasUserInputTransferSuccessful(4))//DR - transfer failed 1st time
                {
                    //DR - remove userinputs from liveDB if there were any and try transfer again
                    _AppEntity.RemoveUserInput(loanid, _AppUser.Id, 4, workflowID);
                    LocalDatabase.TransferDataToLiveDB();
                    if (!LocalDatabase.WasUserInputTransferSuccessful(4)) //DR - transfer failed 2nd time remove userinput again if there were any
                    {
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                        }
                        String email = _AppUser.Email;

                        EmailMessage emailMessage = new EmailMessage(email, "Loan Failed to sumbit", " LoanID: " + loanid.ToString() + " UserName: " + _AppUser.Name + " UserID: " + _AppUser.Id.ToString() + " Tier: " + _AppUser.ActiveRole.ToString() + " Workflow: " + workflowID + " Time tried to submit: " + DateTime.Now.ToString());

                        LoadingCloseProgress();
                        MessageBox.Show("Loan failed to submit. An email was sent to your supervisor", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        resetControls();
                        emailMessage = null;
                        return;
                    }

                }

                //MA: Check if there are any unmatched fields
                //    OR
                //DR - if tier2 had non crit fields that needed to be escalated to tier 4
                //if (UnmatchedCount > 0 | tier3HasNonCritical)
                //{
                //    //MA: If yes, escalate the loan.
                //    _AppEntity.LoanEscalation(0, _AppUser.ActiveRole, _AppUser.Id, loanid);
                //}
                //else
                //{
                //    //MA: If no, set the loan to completed.
                //    _AppEntity.LoanEscalation(1, _AppUser.ActiveRole, _AppUser.Id, loanid);
                //}

                switch (workflowID)
                {
                    case 1:
                        if (UnmatchedCount > 0 | tier3HasNonCritical)
                        {
                            //MA: If yes, escalate the loan.
                            _AppEntity.LoanEscalation(0, _AppUser.ActiveRole, _AppUser.Id, loanid);
                        }
                        else
                        {
                            //MA: If no, set the loan to completed.
                            _AppEntity.LoanEscalation(1, _AppUser.ActiveRole, _AppUser.Id, loanid);
                        }

                        break;
                    case 2:
                        if (UnmatchedCount > 0 | tier3HasNonCritical)
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 0);
                        }
                        else
                        {
                            _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 1);
                        }
                        break;
                }

                //DR - Adds the loan submitted action to the Histories table
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 7, workflowID);

                LoadingCloseProgress();
                var t = MessageBox.Show("Loan Successfully Submitted!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                loanCompletedCount++;

                finalSubmitClicked = true;//DR - lets the formclosing know not to delete userinput

                resetControls();//AO - Resets all controls to default values
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 500,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Width = 400, Height = 100 };
            TextBox textBox = new TextBox() { Left = 50, Top = 100, Width = 400, Height = 300, Multiline = true };
            Button confirmation = new Button() { Text = "OK", Left = 350, Width = 100, Top = 420, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.BringToFront();
            //prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        //DR - disposes of TabPages, TableLayoutPanels and all it's controls that won't be used anymore
        private void DisposeOfTabControls()
        {
            tabControl_Doc_Control.SelectedIndexChanged -= tabPage_Click;
            foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
            {
                foreach (TableLayoutPanel panel in tp.Controls.OfType<TableLayoutPanel>())
                {
                    foreach (Control c in tp.Controls)
                    {
                        c.KeyDown -= MaskedTextBox_KeyDown;
                        c.KeyPress -= MaskedTextBox_KeyPress;
                        c.Click -= null;
                        c.TextChanged -= null;
                        c.Dispose();
                    }
                    panel.MouseMove -= null;
                    panel.Dispose();
                }
                tp.Dispose();
            }
        }

        //DR - Disposes of inner dictionary content
        private void DisposeOfDictionaryJunk()
        {
            //DR - Tiers 1, 2, and 3 dictionaries
            foreach (KeyValuePair<string, Dictionary<int, object>> dictionary in DatabaseDictionary)
            {
                DatabaseDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<string, Dictionary<int, object>> dictionary in UserInputDictionary)
            {
                UserInputDictionary[dictionary.Key].Clear();
            }
            foreach (KeyValuePair<int, Dictionary<Control, ArrayList>> dictionary in FieldDependencyDictionary)
            {
                FieldDependencyDictionary[dictionary.Key].Clear();
            }
            foreach (KeyValuePair<string, TabPage> dictionary in uncheckedDictionary)
            {
                uncheckedDictionary[dictionary.Key].Dispose();
            }

            foreach (KeyValuePair<string, TabPage> dictionary in checkedDictionary)
            {
                checkedDictionary[dictionary.Key].Dispose();
            }

            foreach (KeyValuePair<string, TabPage> dictionary in shortcutDictionary)
            {
                shortcutDictionary[dictionary.Key].Dispose();
            }

            foreach (KeyValuePair<int, Dictionary<int, ComboBox>> dictionary in DocumentListDictionary)
            {
                DocumentListDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<string, CheckBox> dictionary in docCheckDictionary)
            {
                docCheckDictionary[dictionary.Key].Dispose();
            }

            foreach (KeyValuePair<int, Dictionary<int, Label>> dictionary in FieldLabelDictionary)
            {
                FieldLabelDictionary[dictionary.Key].Clear();
            }

            //DR - Tier 1 dictionary(s)
            foreach (KeyValuePair<int, Dictionary<int, CheckBox>> dictionary in fieldCheckDictionary)
            {
                fieldCheckDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<int, ArrayList> dictionary in FieldNAByDefaultDictionary)
            {
                FieldNAByDefaultDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<int, Dictionary<int, bool>> dictionary in FieldHasPromptDictionary)
            {
                FieldHasPromptDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<int, Dictionary<int, string>> dictionary in FieldPromptMessageDictionary)
            {
                FieldPromptMessageDictionary[dictionary.Key].Clear();
            }

            foreach (KeyValuePair<int, Dictionary<int, ArrayList>> dictionary in RedundantFieldsDictionary)
            {
                RedundantFieldsDictionary[dictionary.Key].Clear();
            }

            //DR- Tier 2 and 3 dictionaries
            foreach (KeyValuePair<string, Dictionary<int, object>> dictionary in UserTier1Dictionary)
            {
                UserTier1Dictionary[dictionary.Key].Clear();
            }

            //DR - Tier 3 dictionary(s)
            foreach (KeyValuePair<string, Dictionary<int, object>> dictionary in UserTier2Dictionary)
            {
                UserTier2Dictionary[dictionary.Key].Clear();
            }


        }

        /// <summary>
        /// MA - Resets all the controls in the form to accept new loan
        /// </summary>
        private void resetControls()
        {
            try
            {
                tabIndex = 1;
                loanid = 0;
                this.loanNum = 0;
                UnmatchedCount = 0;
                checkAnswersPromptMessageCount = 0;
                lblSessionCompleted.Text = loanCompletedCount.ToString();
                LoanTimer_Tick(null, null);
                Loanbtn.Enabled = true;
                lblLoanID.Text = "";
                lblLoanNumber.Text = "";
                lblPaymentDueDate.Text = "";
                lblDealID.Text = "";
                DocTimerbtn.Text = "Pause";
                DocTimerbtn.Visible = false;
                ViewWarningStripStatusLabel.Visible = false;
                WorkflowComboBox.Enabled = true;
                finalSubmitbtn.Enabled = false;
                checkAnswersBool = false;
                checkAnswersPromptMessageString = "";
                PaymentDueDate = "";
                
                DisposeOfTabControls();
                tabControl_Doc_Control.SelectedIndexChanged -= tabPage_Click;
                tabControl_Doc_Control.Visible = false;
                tabControl_Doc_Control.Controls.Clear();
                listBox_DocumentNav_Checked.Items.Clear();
                listBox_DocumentNav_Unchecked.Items.Clear();
                if (LocalDatabase != null)
                {
                    //DR - this line must come before closing the DB connection
                    LocalDatabase.DeleteLocalDBUserInput();

                    if (LocalDatabase.dbConnection != null)
                    {
                        LocalDatabase.dbConnection.Close();
                        LocalDatabase.dbConnection.Dispose();
                    }
                }

                btnCheckAnswers.Enabled = false;
                //DR - setting the label to empty string must be called after disposing all doc timers
                DisposeDocTimers();
                lblDocTime.Text = "";
                //DR - call DisposeOfDicJunk before clearing dictionaries
                DisposeOfDictionaryJunk();
                //DR - Tiers 1, 2, and 3 dictionaries
                DatabaseDictionary.Clear();
                UserInputDictionary.Clear();
                FieldDependencyDictionary.Clear();
                uncheckedDictionary.Clear();
                checkedDictionary.Clear();
                DocTimerDictionary.Clear();
                DocTickDictionary.Clear();
                shortcutDictionary.Clear();
                DocumentListDictionary.Clear();
                docCheckDictionary.Clear();
                FieldLabelDictionary.Clear();
                //DR - Tier 1 dictionary(s)
                fieldCheckDictionary.Clear();
                FieldNAByDefaultDictionary.Clear();
                FieldHasPromptDictionary.Clear();
                FieldPromptMessageDictionary.Clear();
                TabPageContainsWarningDictionary.Clear();
                TabPageWarningPromptDictionary.Clear();
                RedundantFieldsDictionary.Clear();
                //DR - Tier 2 dictionary(s)
                fieldCriticalCheckDictionary.Clear();
                //DR- Tier 2 and 3 dictionaries
                UserTier1Dictionary.Clear();
                fieldCountDictionary.Clear();
                //DR - Tier 3 dictionary(s)
                UserTier2Dictionary.Clear();
                imageValList.Clear();
                LocalDatabase = null;
                GC.Collect();
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        #endregion METHODS

        #region EVENT_HANDLERS

        private void FieldControlTextboxToggleChildren(object s, EventArgs e, vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, MaskedTextBox textBox)
        {
            try
            {
                if (FieldDependencyDictionary[documentId].ContainsKey(textBox))
                {
                    //DR- get the text without mask characters
                    string text = textBox.Text;
                    text = text.Replace("Pre-payment penalty expired:(", "");
                    text = text.Replace("(", "");
                    text = text.Replace(")", "");
                    text = text.Replace("-", "");
                    text = text.Replace("/", "");
                    text = text.Trim();

                    //DR - if it's checked, enabled its children controls. If it's unchecked reset the values of its children controls and disable it
                    if (text.Length > 0)
                    {
                        foreach (Control c in FieldDependencyDictionary[documentId][textBox])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR- if the field is not overriden or is matching, enable the field and its documentList
                            if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, documentId, childID, 2, workflowID) && !LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                            {
                                c.Enabled = true;
                                //DR - enable the documentlist
                                DocumentListDictionary[documentId][childID].Enabled = true;
                            }
                            //DR - enable the field override
                            fieldCheckDictionary[documentId][childID].Enabled = true;
                        }
                    }
                    else if ((text.Length == 0))
                    {

                        foreach (Control c in FieldDependencyDictionary[documentId][textBox])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - fire the event to get the fieldOverride to perform uncheck work
                            if (fieldCheckDictionary[documentId][childID].Checked)
                            {
                                //DR - forces the controls event to fire
                                String methodName = "On" + "Click";

                                System.Reflection.MethodInfo miOne = fieldCheckDictionary[documentId][childID].GetType().GetMethod(
                                      methodName,
                                      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                if (miOne == null)
                                    throw new ArgumentException("Cannot find event thrower named " + methodName);

                                miOne.Invoke(fieldCheckDictionary[documentId][childID], new object[] { new EventArgs() });
                            }

                            //DR- if the field is not matching, disable the field and its documentList
                            if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                            {

                                fieldCheckDictionary[documentId][childID].Enabled = false;

                                //DR - disable the documentlist
                                DocumentListDictionary[documentId][childID].Enabled = false;

                                c.Enabled = false;

                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    c.Text = "";
                                }
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    ComboBox comboBox = (ComboBox)c;
                                    comboBox.SelectedIndex = -1;
                                    comboBox = null;
                                }
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    CheckBox chkbx = (CheckBox)c;
                                    //DR - forces the controls event to fire
                                    String methodName = "On" + "Click";

                                    System.Reflection.MethodInfo mi = chkbx.GetType().GetMethod(
                                          methodName,
                                          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                    if (mi == null)
                                        throw new ArgumentException("Cannot find event thrower named " + methodName);

                                    mi.Invoke(chkbx, new object[] { new EventArgs() });
                                    chkbx = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR - Insert Enabled/Disabled into UserInputTable
        private void FieldControlCheckBoxToggleChildren(object sender, EventArgs e, vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, CheckBox checkBox)
        {
            try
            {
                if (FieldDependencyDictionary[documentId].ContainsKey(checkBox))
                {
                    //DR - if it's checked, enabled its children controls. If it's unchecked reset the values of its children controls and disable it
                    if (checkBox.Checked)
                    {
                        foreach (Control c in FieldDependencyDictionary[documentId][checkBox])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR- if the field is not overriden or is matching, enable the field and its documentList
                            if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, documentId, childID, 2, workflowID) && !LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                            {
                                c.Enabled = true;
                                //DR - enable the documentlist
                                DocumentListDictionary[documentId][childID].Enabled = true;
                            }
                            //DR - enable the field override
                            fieldCheckDictionary[documentId][childID].Enabled = true;
                        }

                    }
                    else
                    {
                        DialogResult dialogResult = MessageBox.Show("Are you sure you wish to disable dependencies? Related data will be lost", "Field Dependency", MessageBoxButtons.YesNo);

                        //DR - return and do nothing if the user clicked no
                        if (dialogResult == DialogResult.No)
                        {
                            checkBox.Checked = true;
                            return;
                        }

                        foreach (Control c in FieldDependencyDictionary[documentId][checkBox])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - fire the event to get the fieldOverride to perform uncheck work
                            if (fieldCheckDictionary[documentId][childID].Checked)
                            {
                                //DR - forces the controls event to fire
                                String methodName = "On" + "Click";

                                System.Reflection.MethodInfo miOne = fieldCheckDictionary[documentId][childID].GetType().GetMethod(
                                      methodName,
                                      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                if (miOne == null)
                                    throw new ArgumentException("Cannot find event thrower named " + methodName);

                                miOne.Invoke(fieldCheckDictionary[documentId][childID], new object[] { new EventArgs() });
                            }

                            //DR- if the field is not matching, disable the field and its documentList
                            if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                            {
                                fieldCheckDictionary[documentId][childID].Enabled = false;

                                //DR - disable the documentlist
                                DocumentListDictionary[documentId][childID].Enabled = false;

                                c.Enabled = false;

                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    c.Text = "";
                                }
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    ComboBox comboBox = (ComboBox)c;
                                    comboBox.SelectedIndex = -1;
                                    comboBox = null;
                                }
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    CheckBox chkbx = (CheckBox)c;
                                    //DR - forces the controls event to fire
                                    String methodName = "On" + "Click";

                                    System.Reflection.MethodInfo mi = chkbx.GetType().GetMethod(
                                          methodName,
                                          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                    if (mi == null)
                                        throw new ArgumentException("Cannot find event thrower named " + methodName);

                                    mi.Invoke(chkbx, new object[] { new EventArgs() });
                                    chkbx = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void FieldControlComboBoxToggleChildren(object sender, EventArgs e, vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, ComboBox comboBox)
        {
            try
            {
                if (FieldDependencyDictionary[documentId].ContainsKey(comboBox))
                {
                    //DR - if it's selectedindex is greater than 0, enable its children controls. If it's unchecked reset the values of its children controls and disable it
                    if (comboBox.SelectedIndex >= 0)
                    {
                        int FieldID = GetFieldID(comboBox.Name);
                        string ConditionalValue = comboBox.SelectedItem.ToString();
                        var FieldConditionalValue = (from vFD in _AppEntity.vFieldDependencies
                                                     where vFD.DocID == documentId
                                                     && vFD.ParentID == FieldID
                                                     select new { vFD.ChildID, vFD.ConditionalValue });

                        Dictionary<int, List<string>> p = new Dictionary<int, List<string>>();
                        bool HasValues = false;
                        foreach (var fields in FieldConditionalValue)
                        {
                            if (fields.ConditionalValue != null)
                            {
                                List<string> Values = fields.ConditionalValue.Split('_').ToList();
                                p.Add(fields.ChildID, Values);
                                HasValues = true;
                            }
                            else
                            {
                                List<string> Values = new List<string>();
                                Values.Add("Anything");
                                p.Add(fields.ChildID, Values);
                            }
                        }


                        if (HasValues)
                        {
                            foreach (Control c in FieldDependencyDictionary[documentId][comboBox])
                            {
                                if (p[GetFieldID(c.Name)].Contains(ConditionalValue) | p[GetFieldID(c.Name)].Contains("Anything"))
                                {
                                    int childID = GetFieldID(c.Name);
                                    //DR- if the field is not overriden or is matching, enable the field and its documentList
                                    if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, documentId, childID, 2, workflowID) && !LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                                    {
                                        c.Enabled = true;
                                        //DR - enable the documentlist
                                        DocumentListDictionary[documentId][childID].Enabled = true;
                                    }
                                    //DR - enable the field override
                                    fieldCheckDictionary[documentId][childID].Enabled = true;
                                }
                                else
                                {
                                    c.Enabled = false;
                                    int childID = GetFieldID(c.Name);
                                    //DR - Disable the field override
                                    fieldCheckDictionary[documentId][childID].Enabled = false;
                                    //DR - Disable the documentlist
                                    DocumentListDictionary[documentId][childID].Enabled = false;

                                    if (c.GetType() == typeof(MaskedTextBox))
                                    {
                                        c.Text = "";
                                    }
                                    else if (c.GetType() == typeof(ComboBox))
                                    {
                                        ComboBox ctrl = (ComboBox)c;

                                        ctrl.SelectedIndex = -1;
                                        ctrl = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (Control c in FieldDependencyDictionary[documentId][comboBox])
                            {
                                int childID = GetFieldID(c.Name);
                                //DR- if the field is not overriden or is matching, enable the field and its documentList
                                if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, documentId, childID, 2, workflowID) && !LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                                {
                                    c.Enabled = true;
                                    //DR - enable the documentlist
                                    DocumentListDictionary[documentId][childID].Enabled = true;
                                }
                                //DR - enable the field override
                                fieldCheckDictionary[documentId][childID].Enabled = true;
                            }
                        }

                        foreach (KeyValuePair<int, List<string>> dictionary in p)
                        {
                            p[dictionary.Key].Clear();
                        }
                        p.Clear();
                    }
                    else
                    {
                        foreach (Control c in FieldDependencyDictionary[documentId][comboBox])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - fire the event to get the fieldOverride to perform uncheck work
                            if (fieldCheckDictionary[documentId][childID].Checked)
                            {
                                //DR - forces the controls event to fire
                                String methodName = "On" + "Click";

                                System.Reflection.MethodInfo miOne = fieldCheckDictionary[documentId][childID].GetType().GetMethod(
                                      methodName,
                                      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                if (miOne == null)
                                    throw new ArgumentException("Cannot find event thrower named " + methodName);

                                miOne.Invoke(fieldCheckDictionary[documentId][childID], new object[] { new EventArgs() });
                            }

                            //DR- if the field is not matching, disable the field and its documentList
                            if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, documentId, childID, 1, workflowID))
                            {

                                fieldCheckDictionary[documentId][childID].Enabled = false;

                                //DR - disable the documentlist
                                DocumentListDictionary[documentId][childID].Enabled = false;

                                c.Enabled = false;

                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    c.Text = "";
                                }
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    ComboBox cBox = (ComboBox)c;
                                    cBox.SelectedIndex = -1;
                                    cBox = null;
                                }
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    CheckBox chkbx = (CheckBox)c;
                                    //DR - forces the controls event to fire
                                    String methodName = "On" + "Click";

                                    System.Reflection.MethodInfo mi = chkbx.GetType().GetMethod(
                                          methodName,
                                          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                    if (mi == null)
                                        throw new ArgumentException("Cannot find event thrower named " + methodName);

                                    mi.Invoke(chkbx, new object[] { new EventArgs() });
                                    chkbx = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR- The groupbox for field SSN or TIN? to change its children's mask 
        private void FieldControlGroupBoxSsnTinChangeMask(object sender, EventArgs e, vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, GroupBox groupBox, RadioButton radButton)
        {
            try
            {
                //DR- return if the dictionary doesn't contain the groupbox
                if (!FieldDependencyDictionary[documentId].ContainsKey(groupBox))
                {
                    return;
                }

                foreach (MaskedTextBox textbox in FieldDependencyDictionary[documentId][groupBox].OfType<MaskedTextBox>())
                {
                    textbox.Mask = GetMask(radButton.Text);
                    textbox.Enabled = true;
                    int childID = GetFieldID(textbox.Name);
                    fieldCheckDictionary[documentId][childID].Enabled = true;
                    DocumentListDictionary[documentId][childID].Enabled = true;
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //AO made for faster debugging. Selects MSP checkboxes only
        private void selectAllbtn_Clicked(object sender, System.EventArgs e, TabPage currentTab, int docId)
        {
            foreach (TableLayoutPanel current in currentTab.Controls.OfType<TableLayoutPanel>())
            {
                foreach (CheckBox chkbx in current.Controls.OfType<CheckBox>())
                {
                    if (chkbx.Name.Contains("_MSPcheckbox_"))
                    {
                        chkbx.Checked = true;
                        //DR - forces the controls event to fire
                        String methodName = "On" + "Click";

                        System.Reflection.MethodInfo mi = chkbx.GetType().GetMethod(
                              methodName,
                              System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        if (mi == null)
                            throw new ArgumentException("Cannot find event thrower named " + methodName);

                        mi.Invoke(chkbx, new object[] { e });
                    }
                }
            }
        }

        //DR - Tier 2
        //DR & MA - used to uncheck the other two checkboxes in a row and toggles textbox based on passed value.
        //DR - once checked a checkbox can never be unchecked unless one of the other two checkboxes are checked
        //DR - bools are used to determine whether it's the new value checkbox or msp checkbox
        private void CheckBox_Click(object sender, System.EventArgs e, TabPage currentTab, CheckBox itself, CheckBox check1, CheckBox check2, TextBox textBox, Control control, bool mspValueEnabled, bool newValueEnabled, int docID, int fID)
        {
            try
            {
                bool exists = LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, 3, workflowID); ;

                itself.CheckState = CheckState.Checked;
                check1.Checked = false;
                check2.Checked = false;
                control.Enabled = newValueEnabled;
                string MSPValue = GetCurrentMSPValue(docID, fID);


                //DR - update the previous record for that field if there was one or save the new record in the localdb
                if (newValueEnabled)
                {
                    if (exists)
                    {
                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, "", 3, 0, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, "", 3, 0, DocumentListDictionary[docID][fID].Text, workflowID, MSPValue, 1);
                    }
                }
                else if (mspValueEnabled)
                {
                    if (exists)
                    {
                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 3, 1, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 3, 1, DocumentListDictionary[docID][fID].Text, workflowID, MSPValue, 1);
                    }
                }
                else
                {
                    //DR - if they agree with Tier 1 userinput for image validation, then prompt them if they wish to do so and override the document as normal
                    if (textBox.Text == "ImageValidationTask")
                    {
                        string docName = GetDocumentName(docID);
                        DialogResult result = MessageBox.Show("Do you wish to open an Image Validation Task for " + docName + "? Any data input for this document will be lost.", "Image Validation Task", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            docCheckDictionary[docName].Checked = true;
                            DocOverrideManually("ImageValidationTask", currentTab, docName, docID);
                            return;
                        }
                        else
                        {
                            itself.Checked = false;
                            //DR - forces the controls event to fire
                            String methodName = "On" + "Click";

                            System.Reflection.MethodInfo miOne = check1.GetType().GetMethod(
                                  methodName,
                                  System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                            if (miOne == null)
                                throw new ArgumentException("Cannot find event thrower named " + methodName);

                            miOne.Invoke(check1, new object[] { new EventArgs() });
                            return;
                        }

                    }
                    if (exists)
                    {
                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 3, 0, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 3, 0, DocumentListDictionary[docID][fID].Text, workflowID, MSPValue, 1);
                    }
                }

                string sql = "select count(DocID) from UserInputTable where DocID = '" + docID.ToString() + "'";
                command = new SQLiteCommand(sql, LocalDatabase.dbConnection);
                //DR - gets the count of userinputs for a document
                int count = int.Parse(command.ExecuteScalar().ToString());
                //DR - if the userinput count matches the fieldCount for a document, move the document to the checkedDictionary
                if (fieldCountDictionary[docID] == count)
                {
                    if (uncheckedDictionary.ContainsKey(currentTab.Text))
                    {
                        //DR - add to checked and remove from unchecked list
                        checkedDictionary.Add(currentTab.Text, uncheckedDictionary[currentTab.Text]);
                        uncheckedDictionary.Remove(currentTab.Text);
                        LoadListDictionaries();
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //DR - Tier 3
        //DR & MA - used to uncheck the other two checkboxes in a row and toggles textbox based on passed value.
        //DR - once checked a checkbox can never be unchecked unless one of the other two checkboxes are checked
        private void CheckBox_Click(object sender, System.EventArgs e, TabPage currentTab, CheckBox itself, CheckBox check1, CheckBox check2, CheckBox check3, TextBox textBox, Control control, bool mspValueEnabled, bool newValueEnabled, int docID, int fID)
        {
            try
            {
                bool exists = LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, 4, workflowID);

                itself.CheckState = CheckState.Checked;
                check1.Checked = false;
                check2.Checked = false;
                check3.Checked = false;
                control.Enabled = newValueEnabled;


                //DR - remove the previous record for that field if there was one and save the new record in the localdb
                if (newValueEnabled)
                {
                    if (exists)
                    {

                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, "", 4, 0, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, "", 4, 0, DocumentListDictionary[docID][fID].Text, workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }

                }
                else if (mspValueEnabled)
                {
                    if (exists)
                    {
                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 4, 1, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 4, 1, DocumentListDictionary[docID][fID].Text, workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }

                }
                else
                {
                    if (exists)
                    {
                        LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 4, 0, DocumentListDictionary[docID][fID].Text, workflowID);
                    }
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text, 4, 0, DocumentListDictionary[docID][fID].Text, workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }
                }

                string sql = "select count(DocID) from UserInputTable where DocID = '" + docID.ToString() + "'";
                command = new SQLiteCommand(sql, LocalDatabase.dbConnection);
                //DR - gets the count of userinputs for a document
                int count = int.Parse(command.ExecuteScalar().ToString());
                //DR - if the userinput count matches the fieldCount for a document, move the document to the checkedDictionary
                if (fieldCountDictionary[docID] == count)
                {
                    if (uncheckedDictionary.ContainsKey(currentTab.Text))
                    {
                        //DR - add to checked and remove from unchecked list
                        checkedDictionary.Add(currentTab.Text, uncheckedDictionary[currentTab.Text]);
                        uncheckedDictionary.Remove(currentTab.Text);
                        LoadListDictionaries();
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// MA - Sets the cursor to the specified position on textbox when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="textbox"></param>
        /// <param name="pos"></param>
        private void maskedTextBox_Clicked(object sender, EventArgs e, MaskedTextBox textbox, int pos)
        {
            textbox.Select(pos, 0);
        }

        //DR- event to determine what type of key was pressed
        private void MaskedTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            //DR- Initialize the flag to false.
            nonNumberEntered = false;

            //DR- Determine whether the keystroke is a number from the top of the keyboard.
            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                //DR- Determine whether the keystroke is a number from the keypad.
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    //DR- Determine whether the keystroke is a backspace or decimal.
                    if (e.KeyCode != Keys.Back && e.KeyCode != Keys.Decimal && e.KeyCode != Keys.OemPeriod)
                    {
                        //DR- A non-numerical keystroke was pressed.
                        //DR- Set the flag to true and evaluate in KeyPress event.
                        nonNumberEntered = true;
                    }
                }
            }
            //DR-If shift key was pressed, it's not a number.
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }

        }

        //DR- event to determine what type of key was pressed
        //DR- this method allows dashes to be pressed while the first method does not
        private void MaskedTextBox_KeyDown2(object sender, KeyEventArgs e)
        {

            //DR- Initialize the flag to false.
            nonNumberEntered = false;

            //DR- Determine whether the keystroke is a number from the top of the keyboard.
            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                //DR- Determine whether the keystroke is a number from the keypad.
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    //DR- Determine whether the keystroke is a backspace, decimal, or dash.
                    if (e.KeyCode != Keys.Back && e.KeyCode != Keys.Decimal && e.KeyCode != Keys.OemMinus)
                    {
                        //DR- A non-numerical keystroke was pressed.
                        //DR- Set the flag to true and evaluate in KeyPress event.
                        nonNumberEntered = true;
                    }
                }
            }
            //DR-If shift key was pressed, it's not a number.
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }

        }
        //DR- This event occurs after the KeyDown event and can be used to prevent characters from entering the control.
        private void MaskedTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            //DR- Check for the flag being set in the KeyDown event.
            if (nonNumberEntered == true)
            {
                //DR- Stop the character from being entered into the control since it is non-numerical.
                e.Handled = true;
            }
        }

        //DR - Field override logic
        private void FieldCheck_Clicked(object sender, System.EventArgs e, int docID, int fID, CheckBox checkBox, Control control)
        {
            try
            {
                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                var field = (from fQuery in _AppEntity.vFields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - get the field info from fields table
                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data

                MaskedTextBox maskTextBox;
                ComboBox comboBox;
                CheckBox chkBox;
                if (checkBox.Checked)
                {
                    ComboBox ddl = new ComboBox()
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    ddl.Items.Add("Field Override");
                    if (field.IsNotRequired)
                    {
                        ddl.Items.Add("Not Applicable");
                    }

                    string value = Form_Message_Box.Show("Field Override", "Select a reason for Field Override", ddl, "Proceed", "Cancel");
                    ddl.Items.Clear();
                    ddl.Dispose();

                    //DR - if they don't want to override, return and uncheck the checkbox
                    if (value == "Cancel" | value == null)
                    {
                        checkBox.Checked = false;
                        return;
                    }

                    //DR - do textbox specific logic if the control is a textbox
                    if (control.Name.ToLower().Contains("textbox"))
                    {
                        maskTextBox = (MaskedTextBox)control;

                        switch (field.Client_Data_Type.ToLower())
                        {
                            case "double":

                                break;
                            default:
                                //DR - set the mask to type string so we can set the value of the textbox
                                maskTextBox.Mask = GetMask("string");
                                break;
                        }
                        maskTextBox.Text = "";
                        maskTextBox = null;
                    }
                    else if (control.Name.ToLower().Contains("combobox"))
                    {
                        comboBox = (ComboBox)control;
                        comboBox.SelectedIndex = -1;
                        comboBox = null;

                    }
                    else if (control.Name.ToLower().Contains("checkbox"))
                    {
                        chkBox = (CheckBox)control;
                        chkBox.Checked = false;
                        chkBox = null;
                    }

                    control.Enabled = false;
                    DocumentListDictionary[docID][fID].Enabled = false;

                    //DR - commentcodes must always be escalated
                    if (field.Client_Data_Type.ToLower().Trim().Equals("commentcode") | field.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                        //DR - increments so final submit button knows loan must be escalated
                        UnmatchedCount++;
                    }
                    //DR - if field in database is null or empty string, unmatchedcount is not incremented because the loan won't need to escalate
                    else if (fieldValue == null)
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }
                    else if (fieldValue.ToString().Trim().Equals(""))
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }
                    //DR - field has value in database, loan must be escalated
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                        //DR - increments so final submit button knows loan must be escalated
                        UnmatchedCount++;
                    }
                    //DR - if the document doesn't have an entry return
                    if (!FieldDependencyDictionary.ContainsKey(docID))
                    {
                        return;
                    }
                    //DR - field override the control's children if any
                    if (FieldDependencyDictionary[docID].ContainsKey(control))
                    {
                        foreach (Control c in FieldDependencyDictionary[docID][control])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - check the field override
                            fieldCheckDictionary[docID][childID].Checked = true;
                            fieldCheckDictionary[docID][childID].Enabled = false;

                            FieldOverrideManually(value, docID, childID, fieldCheckDictionary[docID][childID], c);
                        }
                    }
                }
                else
                {
                    //DR - do textbox specific logic if the control is a textbox
                    if (control.Name.ToLower().Contains("textbox"))
                    {
                        maskTextBox = (MaskedTextBox)control;
                        maskTextBox.Text = null;


                        switch (field.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                        {
                            case "double":

                                break;
                            default:
                                maskTextBox.Mask = GetMask(field.Client_Data_Type.ToLower()); //DR - passes in the type to get the mask
                                break;
                        }
                        maskTextBox = null;
                    }
                    control.Enabled = true;
                    DocumentListDictionary[docID][fID].Enabled = true;
                    if (field.Client_Data_Type.ToLower().Trim().Equals("commentcode") | field.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                    {
                        UnmatchedCount--;
                    }
                    //DR - unmatched count wasn't incremented before so no need to unincrement it
                    else if (fieldValue == null)
                    {

                    }
                    else if (fieldValue.ToString().Trim().Equals(""))
                    {

                    }
                    else //DR - unincrement unmatchedcount 
                    {
                        UnmatchedCount--;
                    }

                    LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, 2, workflowID);
                    //DR - if the document doesn't have an entry return
                    if (!FieldDependencyDictionary.ContainsKey(docID))
                    {
                        return;
                    }
                    //DR - field override the control's children if any
                    if (FieldDependencyDictionary[docID].ContainsKey(control))
                    {
                        foreach (Control c in FieldDependencyDictionary[docID][control])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - uncheck the field override
                            fieldCheckDictionary[docID][childID].Checked = false;

                            FieldOverrideManually(null, docID, childID, fieldCheckDictionary[docID][childID], c);
                            c.Enabled = false;
                            DocumentListDictionary[docID][childID].Enabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR- Manually FieldOverride a method
        private void FieldOverrideManually(string value, int docID, int fID, CheckBox checkBox, Control control)
        {
            try
            {
                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                var field = (from fQuery in _AppEntity.vFields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - get the field info from fields table
                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data

                MaskedTextBox maskTextBox;
                ComboBox comboBox;
                CheckBox chkBox;
                if (checkBox.Checked)
                {
                    //DR- it was overriden before and it might've been with a different value, remove it so we can override it with the new value
                    if (LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, docID, fID, 2, workflowID))
                    {
                        LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, 2, workflowID);
                    }
                    //DR - do textbox specific logic if the control is a textbox
                    if (control.Name.ToLower().Contains("textbox"))
                    {
                        maskTextBox = (MaskedTextBox)control;

                        switch (field.Client_Data_Type.ToLower())
                        {
                            case "double":

                                break;
                            default:
                                //DR - set the mask to type string so we can set the value of the textbox
                                maskTextBox.Mask = GetMask("string");
                                break;
                        }
                        maskTextBox.Text = "";
                        maskTextBox = null;
                    }
                    else if (control.Name.ToLower().Contains("combobox"))
                    {
                        comboBox = (ComboBox)control;
                        comboBox.SelectedIndex = -1;
                        comboBox = null;

                    }
                    else if (control.Name.ToLower().Contains("checkbox"))
                    {
                        chkBox = (CheckBox)control;
                        chkBox.Checked = false;
                        chkBox = null;
                    }

                    control.Enabled = false;
                    DocumentListDictionary[docID][fID].Enabled = false;
                    //DR - commentcodes must always be escalated
                    if (field.Client_Data_Type.ToLower().Trim().Equals("commentcode") | field.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                        //DR - increments so final submit button knows loan must be escalated
                        UnmatchedCount++;
                    }
                    //DR - if field in database is null or empty string, unmatchedcount is not incremented because the loan won't need to escalate
                    else if (fieldValue == null)
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }
                    else if (fieldValue.ToString().Trim().Equals(""))
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                    }
                    //DR - field has value in database, loan must be escalated
                    else
                    {
                        LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                        //DR - increments so final submit button knows loan must be escalated
                        UnmatchedCount++;
                    }
                    //DR - if the document doesn't have an entry return
                    if (!FieldDependencyDictionary.ContainsKey(docID))
                    {
                        return;
                    }
                    //DR - field override the control's children if any
                    if (FieldDependencyDictionary[docID].ContainsKey(control))
                    {
                        foreach (Control c in FieldDependencyDictionary[docID][control])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - check the field override
                            fieldCheckDictionary[docID][childID].Checked = true;
                            fieldCheckDictionary[docID][childID].Enabled = false;

                            FieldOverrideManually(value, docID, childID, fieldCheckDictionary[docID][childID], c);
                        }
                    }
                }
                else
                {
                    //DR - do textbox specific logic if the control is a textbox
                    if (control.Name.ToLower().Contains("textbox"))
                    {
                        maskTextBox = (MaskedTextBox)control;
                        maskTextBox.Text = null;


                        switch (field.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                        {
                            case "double":

                                break;
                            default:
                                maskTextBox.Mask = GetMask(field.Client_Data_Type.ToLower()); //DR - passes in the type to get the mask
                                break;
                        }
                        maskTextBox = null;
                    }
                    control.Enabled = true;
                    DocumentListDictionary[docID][fID].Enabled = true;
                    if (field.Client_Data_Type.ToLower().Trim().Equals("commentcode") | field.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                    {
                        UnmatchedCount--;
                    }
                    //DR - unmatched count wasn't incremented before so no need to unincrement it
                    else if (fieldValue == null)
                    {

                    }
                    else if (fieldValue.ToString().Trim().Equals(""))
                    {

                    }
                    else //DR - unincrement unmatchedcount 
                    {
                        UnmatchedCount--;
                    }

                    LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, 2, workflowID);

                    //DR - if the document doesn't have an entry return
                    if (!FieldDependencyDictionary.ContainsKey(docID))
                    {
                        return;
                    }
                    //DR - field override the control's children if any
                    if (FieldDependencyDictionary[docID].ContainsKey(control))
                    {
                        foreach (Control c in FieldDependencyDictionary[docID][control])
                        {
                            int childID = GetFieldID(c.Name);
                            //DR - uncheck the field override
                            fieldCheckDictionary[docID][childID].Checked = false;

                            FieldOverrideManually(value, docID, childID, fieldCheckDictionary[docID][childID], c);
                            c.Enabled = false;
                            DocumentListDictionary[docID][childID].Enabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        
        /// <summary>
        /// MA - Focuses the cursor on the first textbox in the tab page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabPage_Click(object sender, EventArgs e)
        {
            try
            {
                //DR - if selected index is less than 0, it means controls don't exist so we must return must be first
                if (tabControl_Doc_Control.SelectedIndex < 0)
                {
                    return;
                }

                //DR - Store the active tab
                TabPage t = tabControl_Doc_Control.SelectedTab;

                //DR - If the user is in Tier 1
                if (_AppUser.ActiveRole == 1)
                {

                    ViewWarningStripStatusLabel.Visible = false;

                    //MA - Check if the Tab contains warnings
                    if (TabPageContainsWarningDictionary[t.Text] == true)
                    {
                        //MA - enable the warning link
                        ViewWarningStripStatusLabel.Visible = true;

                    }

                    //DR - If the tab page has already been checked
                    if (checkedDictionary.ContainsKey(t.Text))
                    {
                        //DR - Disable the check answers button
                        btnCheckAnswers.Enabled = false;
                    }
                    else
                    {
                        //DR - Enable the check answers button
                        btnCheckAnswers.Enabled = true;
                    }
                }
                //DR - pause any timers if they are runing
                PauseDocTimers();
                //DR - get the document name of the tab
                string docName = tabControl_Doc_Control.SelectedTab.Text;
                //DR - create the timer for document if it hasn't been created
                if (DocTimerDictionary[docName] == null)
                {
                    InitDocTimer(docName);
                }
                //DR - start the timer
                if (DocTimerDictionary.ContainsKey(docName))
                {
                    DocTimerDictionary[docName].Start();
                }
                //MA - handles different scenarios of user input so that the focus goes to the first empty txtbx or the first one if all the txtboxes are filled
                foreach (TableLayoutPanel tp in t.Controls.OfType<TableLayoutPanel>())
                {
                    foreach (CheckBox m in tp.Controls.OfType<CheckBox>())
                    {
                        if (m.Enabled == true)
                        {
                            m.Focus();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// MA - When a document is clicked in the Unchecked listbox, the tab containing that 
        /// document will become the active tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UncheckedListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                if (listBox_DocumentNav_Unchecked.SelectedItem == null) //DR - return if nothing was selected
                {
                    return;
                }

                //DR - Get the name of the selected document
                string docName = listBox_DocumentNav_Unchecked.SelectedItem.ToString();

                //DR - Make the selected document the active tab
                tabControl_Doc_Control.SelectedTab = uncheckedDictionary[docName];

                listBox_DocumentNav_Unchecked.SelectedItem = null;
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///  MA - When a document is clicked in the Checked listbox, the tab containing that 
        /// document will become the active tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckedListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                if (listBox_DocumentNav_Checked.SelectedItem == null)//DR - return if nothing was selected
                {
                    return;
                }

                //DR - Get the name of the selected document
                string docName = listBox_DocumentNav_Checked.SelectedItem.ToString();

                //DR - Make the selected document the active tab
                tabControl_Doc_Control.SelectedTab = checkedDictionary[docName];

                listBox_DocumentNav_Checked.SelectedItem = null;
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void RadioButtonGroupbox_Click(object s, EventArgs e, vDocuments_Fields field, TableLayoutPanel tablePanel, int documentId, GroupBox groupBox, RadioButton radiobtn)
        {
            var Field_DocDependencies = (from DD in _AppEntity.Fields_CrossDocument_Dependency.AsNoTracking()
                                         where DD.DocID == documentId
                                         && DD.FieldID == field.FieldId
                                         select DD).ToList();

            foreach (var DocumentDependencies in Field_DocDependencies)
            {
                switch (DocumentDependencies.EventCode)
                {
                    case 1:
                        if (radiobtn.Text.ToLower() != DocumentDependencies.ConditionalValue.ToLower())
                        {
                            foreach (TabPage tp in tabControl_Doc_Control.TabPages)
                            {
                                if (tp.Text.ToLower() == GetDocumentName(DocumentDependencies.ChildDocID).ToLower())
                                {
                                    TabPage tpage = tp;

                                    if (docCheckDictionary[GetDocumentName(DocumentDependencies.ChildDocID)].Checked == false)
                                    {
                                        docCheckDictionary[GetDocumentName(DocumentDependencies.ChildDocID)].Checked = true;

                                        DocOverrideManually("NotApplicable", tpage, GetDocumentName(DocumentDependencies.ChildDocID), DocumentDependencies.ChildDocID);
                                    }


                                }
                            }

                        }
                        else if (radiobtn.Text.ToLower() == DocumentDependencies.ConditionalValue.ToLower())
                        {
                            foreach (TabPage tp in tabControl_Doc_Control.TabPages)
                            {
                                if (tp.Text.ToLower() == GetDocumentName(DocumentDependencies.ChildDocID).ToLower())
                                {
                                    TabPage tpage = tp;

                                    if (docCheckDictionary[GetDocumentName(DocumentDependencies.ChildDocID)].Checked == true)
                                    {
                                        docCheckDictionary[GetDocumentName(DocumentDependencies.ChildDocID)].Checked = false;
                                        DocOverrideManually(null, tpage, GetDocumentName(DocumentDependencies.ChildDocID), DocumentDependencies.ChildDocID);
                                    }



                                }
                            }
                        }
                        break;
                    case 2:
                        if (radiobtn.Text.ToLower() == DocumentDependencies.ConditionalValue.ToLower())
                        {
                            foreach (TabPage tp in tabControl_Doc_Control.TabPages)
                            {
                                if (tp.Text.ToLower() == GetDocumentName(DocumentDependencies.ChildDocID).ToLower())
                                {
                                    TabPage tpage = tp;
                                    foreach (Control ctrl in tp.Controls.OfType<Control>())
                                    {

                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        
        private void DocCheck_Clicked(object sender, System.EventArgs e, TabPage currentTab, string docName, int docID) //DR - if docCheck was clicked, prompt user if they're sure and if so disable the document
        {
            try
            {
                int fID;
                int InputID = 0;

                switch (_AppUser.ActiveRole)
                {
                    case 1:
                        InputID = 2;
                        break;

                    case 2:
                        InputID = 3;
                        break;

                    case 3:
                        InputID = 4;
                        break;
                }

                if (docCheckDictionary[docName].Checked)
                {

                    ComboBox ddl = new ComboBox()
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    if (workflowID == 2)
                    {
                        ddl.Items.Add("Document Override");
                    }
                    if (workflowID == 1 && _AppUser.ActiveRole != 3)
                    {
                        ddl.Items.Add("Image Validation Task");
                    }
                    var docQuery = (from doc in _AppEntity.vDocuments.AsNoTracking() where doc.Name == docName select doc.IsNotRequired).Single();
                    if (docQuery == true)
                    {
                        ddl.Items.Add("Not Applicable");
                    }
                    string value = Form_Message_Box.Show("Document Override", "Select a reason to Document Override", ddl, "Proceed", "Cancel");
                    ddl.Items.Clear();
                    ddl.Dispose();

                    //DR - if they don't want to override, return and uncheck the checkbox
                    if (value == "Cancel" | value == null)
                    {
                        docCheckDictionary[docName].CheckState = CheckState.Unchecked;
                        return;
                    }
                    this.Refresh();
                    LoadingStartProgress("Overriding Document...");

                    foreach (TableLayoutPanel tableLayPanel in currentTab.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR- disable field textbox and add input to userinput table                        
                        foreach (Control c in tableLayPanel.Controls)
                        {
                            if (c.Name.Contains("FieldControl"))
                            {
                                fID = GetFieldID(c.Name);

                                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data

                                var fieldQuery = (from fQuery in _AppEntity.Fields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - declare and initilize our fieldquery variable

                                if (LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, docID, GetFieldID(c.Name), InputID, workflowID)) //DR - DocumentOverride needs to overwrite FieldOverride in userinputtable
                                {
                                    LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, InputID, workflowID);
                                }

                                switch (_AppUser.ActiveRole)
                                {
                                    case 1:
                                        //DR - commentcodes must always be escalated
                                        if (fieldQuery.Client_Data_Type.ToLower().Trim().Equals("commentcode") | fieldQuery.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            //DR - increments so final submit button knows loan must be escalated
                                            UnmatchedCount++;
                                        }
                                        //DR - if field in database is null or empty string, unmatchedcount is not incremented because the loan won't need to escalate                            
                                        //DR - checking for null must come first or null exception error will be thrown
                                        else if (fieldValue == null)
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                        }
                                        else //DR - field has value in database, loan must be escalated
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            UnmatchedCount++; //DR - increments so final submit button knows loan must be escalated
                                        }
                                        break;
                                    case 2:
                                    case 3:
                                        //DR - mark the row as matching so it does not escalate the loan
                                        if (value.Equals("ImageValidationTask"))
                                        {
                                            //DR - update the row if it exists to preserve the original timestamp
                                            if (LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, InputID, workflowID) == true)
                                            {
                                                //DR - if the row wasn't matching before, unincrement unmatchedCount
                                                if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, docID, fID, InputID, workflowID))
                                                {
                                                    UnmatchedCount--;
                                                }
                                                LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID);
                                            }
                                            else
                                            {
                                                LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            }
                                            if (!imageValList.Contains(docID))
                                            {
                                                imageValList.Add(docID);
                                            }
                                        }
                                        //DR - update the row if it exists to preserve the original timestamp
                                        else if (LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, InputID, workflowID) == true)
                                        {
                                            //DR - if the row was matching before, increment unmatchedCount
                                            if (LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, docID, fID, InputID, workflowID))
                                            {
                                                UnmatchedCount++;
                                            }
                                            LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID);
                                        }
                                        else
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            UnmatchedCount++; //DR - increments so final submit button knows loan must be escalated
                                        }
                                        break;
                                }

                                //DR - do textbox specific logic if the control is a textbox
                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    MaskedTextBox maskTextBox = (MaskedTextBox)c;

                                    switch (fieldQuery.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                                    {
                                        case "double":

                                            break;
                                        default:
                                            maskTextBox.Mask = GetMask("string"); //DR - set the mask to type string so we can set the value of the textbox to DocumentOverride
                                            break;

                                    }
                                    //maskTextBox.Text = value;
                                    maskTextBox.Text = "";
                                    maskTextBox = null;
                                }
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    ComboBox comboBox = (ComboBox)c;
                                    comboBox.SelectedIndex = -1;
                                    comboBox = null;

                                }
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    CheckBox chkBox = (CheckBox)c;
                                    chkBox.Checked = false;
                                    chkBox = null;
                                }
                                c.Enabled = false;
                            }
                        }

                        foreach (CheckBox chkbox in tableLayPanel.Controls.OfType<CheckBox>()) //DR - unchecks all field override checkboxes
                        {
                            //DR - if checkbox doesn't contain documentoverride in its name, disable it and uncheck it
                            if (!chkbox.Name.Contains("DocumentOverride"))
                            {
                                chkbox.CheckState = CheckState.Unchecked;
                                chkbox.Enabled = false;
                            }

                        }
                        //DR - disable comboxes
                        foreach (ComboBox cb in tableLayPanel.Controls.OfType<ComboBox>())
                        {
                            cb.Enabled = false;
                        }

                        foreach (Button btn in tableLayPanel.Controls.OfType<Button>())
                        {
                            btn.Enabled = false;
                        }

                        btnCheckAnswers.Enabled = false;


                    }
                    //DR - add to checked and remove from unchecked list
                    if (!checkedDictionary.ContainsKey(docName))
                    {
                        checkedDictionary.Add(docName, uncheckedDictionary[docName]);
                        uncheckedDictionary.Remove(docName);
                    }

                    LoadListDictionaries();
                }
                else //DR - remove the data added from the userinput table and enable the appropiate controls
                {
                    this.Refresh();
                    LoadingStartProgress("Removing Overrides...");
                    foreach (TableLayoutPanel tableLayPanel in currentTab.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR- enable field textbox and remove input from userinput table
                        foreach (Control c in tableLayPanel.Controls)
                        {
                            //DR - flag to see if our control is dependant on another
                            bool isChild = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    if (list.Value.Contains(c))
                                    {
                                        isChild = true;
                                    }
                                }
                            }
                            if (c.Name.Contains("FieldControl"))
                            {
                                fID = GetFieldID(c.Name);
                                LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, InputID, workflowID);

                                //DR - do textbox specific logic if the control is a textbox
                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    MaskedTextBox maskTextBox = (MaskedTextBox)c;
                                    maskTextBox.Text = null; //DR - set textbox to null
                                    var fieldQuery = (from fQuery in _AppEntity.Fields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - declare and initilize our fieldquery variable


                                    switch (fieldQuery.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                                    {
                                        case "double":

                                            break;
                                        default:
                                            maskTextBox.Mask = GetMask(fieldQuery.Client_Data_Type); //DR - passes in the type to get the mask
                                            break;
                                    }
                                }

                                c.Enabled = true;

                                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data
                                switch (_AppUser.ActiveRole)
                                {
                                    case 1:
                                        string clientDataType = (from f in _AppEntity.vFields.AsNoTracking() where f.Id == fID select f.Client_Data_Type).Single();
                                        //DR - commentcodes must always be escalated
                                        if (clientDataType.ToLower().Trim().Equals("commentcode") | clientDataType.ToLower().Trim().Equals("prepaymentpenalty"))
                                        {
                                            UnmatchedCount--;
                                        }
                                        //DR - unmatched count wasn't incremented before if fieldValue is equal to null or empty string so no need to unincrement it
                                        //DR - checking for null must come first or null exception error will be thrown
                                        else if (fieldValue == null)
                                        {

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {

                                        }
                                        else if (imageValList.Contains(docID))
                                        {

                                        }
                                        else //DR - unincrement unmatchedcount 
                                        {
                                            UnmatchedCount--;
                                        }
                                        break;
                                    case 2:
                                    case 3:
                                        //DR - unmatched count wasn't incremented before if fieldValue is equal to null or empty string so no need to unincrement it
                                        //DR - checking for null must come first or null exception error will be thrown
                                        if (fieldValue == null)
                                        {

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {

                                        }
                                        else if (imageValList.Contains(docID))
                                        {

                                        }
                                        else //DR - unincrement unmatchedcount 
                                        {
                                            UnmatchedCount--;
                                        }
                                        break;
                                }
                            }
                            //DR - disable the control
                            if (isChild)
                            {
                                c.Enabled = false;
                            }
                        }

                        foreach (CheckBox chkbox in tableLayPanel.Controls.OfType<CheckBox>()) //DR - enables checkboxes
                        {
                            bool isChildOverride = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    foreach (Control childControl in list.Value)
                                    {
                                        if (GetFieldID(chkbox.Name).Equals(GetFieldID(childControl.Name)))
                                        {
                                            isChildOverride = true;
                                        }
                                    }
                                }
                            }
                            //DR - only gets enabled if it's not a child control's fieldoverride
                            if (!isChildOverride)
                            {
                                chkbox.Enabled = true;
                            }

                        }

                        //DR - enables comboboxes
                        foreach (ComboBox cb in tableLayPanel.Controls.OfType<ComboBox>())
                        {
                            bool isChildDocList = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    foreach (Control childControl in list.Value)
                                    {
                                        if (GetFieldID(cb.Name).Equals(GetFieldID(childControl.Name)))
                                        {
                                            isChildDocList = true;
                                        }
                                    }
                                }
                            }
                            //DR - only gets enabled if it's not a child control's docList
                            if (!isChildDocList)
                            {
                                cb.Enabled = true;
                            }

                        }

                        foreach (Button btn in tableLayPanel.Controls.OfType<Button>())
                        {
                            btn.Enabled = true;
                        }

                        if (_AppUser.ActiveRole == 1)
                        {
                            btnCheckAnswers.Enabled = true;
                        }
                        //DR - if Tier 2 or 3 fieldControls need to be disabled unless new value is selected
                        if (_AppUser.ActiveRole == 2 | _AppUser.ActiveRole == 3)
                        {
                            foreach (Control control in tableLayPanel.Controls)
                            {
                                if (control.Name.ToLower().Contains("fieldcontrol"))
                                {
                                    control.Enabled = false;
                                }
                            }
                        }

                    }

                    //DR-remove checked and add to unchecked list
                    uncheckedDictionary.Add(docName, checkedDictionary[docName]);
                    checkedDictionary.Remove(docName);
                    LoadListDictionaries();

                    if (imageValList.Contains(docID))
                    {
                        imageValList.Remove(docID);
                    }

                }
                LoadingCloseProgress();
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR- Manually DocOverride a Document
        public void DocOverrideManually(string value, TabPage currentTab, string docName, int docID)
        {
            try
            {
                int fID;
                int InputID = 0;

                switch (_AppUser.ActiveRole)
                {
                    case 1:
                        InputID = 2;
                        break;

                    case 2:
                        InputID = 3;
                        break;

                    case 3:
                        InputID = 4;
                        break;
                }

                if (docCheckDictionary[docName].Checked)
                {
                    this.Refresh();
                    LoadingStartProgress("Overriding Document...");

                    foreach (TableLayoutPanel tableLayPanel in currentTab.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR- disable field textbox and add input to userinput table                        
                        foreach (Control c in tableLayPanel.Controls)
                        {
                            if (c.Name.Contains("FieldControl"))
                            {
                                fID = GetFieldID(c.Name);

                                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data

                                var fieldQuery = (from fQuery in _AppEntity.Fields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - declare and initilize our fieldquery variable

                                if (LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, docID, GetFieldID(c.Name), InputID, workflowID)) //DR - DocumentOverride needs to overwrite FieldOverride in userinputtable
                                {
                                    LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, InputID, workflowID);
                                }

                                switch (_AppUser.ActiveRole)
                                {
                                    case 1:
                                        //DR - commentcodes must always be escalated
                                        if (fieldQuery.Client_Data_Type.ToLower().Trim().Equals("commentcode")|fieldQuery.Client_Data_Type.ToLower().Trim().Equals("prepaymentpenalty"))
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, 2, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            //DR - increments so final submit button knows loan must be escalated
                                            UnmatchedCount++;
                                        }
                                        //DR - if field in database is null or empty string, unmatchedcount is not incremented because the loan won't need to escalate                            
                                        //DR - checking for null must come first or null exception error will be thrown
                                        else if (fieldValue == null)
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                        }
                                        else //DR - field has value in database, loan must be escalated
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            UnmatchedCount++; //DR - increments so final submit button knows loan must be escalated
                                        }
                                        break;
                                    case 2:
                                    case 3:
                                        //DR - mark the row as matching so it does not escalate the loan
                                        if (value.Equals("ImageValidationTask"))
                                        {
                                            //DR - update the row if it exists to preserve the original timestamp
                                            if (LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, InputID, workflowID) == true)
                                            {
                                                //DR - if the row wasn't matching before, unincrement unmatchedCount
                                                if (!LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, docID, fID, InputID, workflowID))
                                                {
                                                    UnmatchedCount--;
                                                }
                                                LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID);
                                            }
                                            else
                                            {
                                                LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 1, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            }
                                            if (!imageValList.Contains(docID))
                                            {
                                                imageValList.Add(docID);
                                            }
                                        }
                                        //DR - update the row if it exists to preserve the original timestamp
                                        else if (LocalDatabase.LocalDoesExist(loanid, _AppUser.Id, docID, fID, InputID, workflowID) == true)
                                        {
                                            //DR - if the row was matching before, increment unmatchedCount
                                            if (LocalDatabase.LocalIsMatching(loanid, _AppUser.Id, docID, fID, InputID, workflowID))
                                            {
                                                UnmatchedCount++;
                                            }
                                            LocalDatabase.LocalUpdateUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID);
                                        }
                                        else
                                        {
                                            LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, value, InputID, 0, "Overriden", workflowID, GetCurrentMSPValue(docID, fID), 1);
                                            UnmatchedCount++; //DR - increments so final submit button knows loan must be escalated
                                        }
                                        break;
                                }

                                //DR - do textbox specific logic if the control is a textbox
                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    MaskedTextBox maskTextBox = (MaskedTextBox)c;

                                    switch (fieldQuery.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                                    {
                                        case "double":

                                            break;
                                        default:
                                            maskTextBox.Mask = GetMask("string"); //DR - set the mask to type string so we can set the value of the textbox to DocumentOverride
                                            break;

                                    }
                                    //maskTextBox.Text = value;
                                    maskTextBox.Text = "";
                                    maskTextBox = null;
                                }
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    ComboBox comboBox = (ComboBox)c;
                                    comboBox.SelectedIndex = -1;
                                    comboBox = null;

                                }
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    CheckBox chkBox = (CheckBox)c;
                                    chkBox.Checked = false;
                                    chkBox = null;
                                }
                                c.Enabled = false;
                            }
                        }

                        foreach (CheckBox chkbox in tableLayPanel.Controls.OfType<CheckBox>()) //DR - unchecks all field override checkboxes
                        {
                            //DR - if checkbox doesn't contain documentoverride in its name, disable it and uncheck it
                            if (!chkbox.Name.Contains("DocumentOverride"))
                            {
                                chkbox.CheckState = CheckState.Unchecked;
                                chkbox.Enabled = false;
                            }

                        }
                        //DR - disable comboxes
                        foreach (ComboBox cb in tableLayPanel.Controls.OfType<ComboBox>())
                        {
                            cb.Enabled = false;
                        }

                        foreach (Button btn in tableLayPanel.Controls.OfType<Button>())
                        {
                            btn.Enabled = false;
                        }

                        btnCheckAnswers.Enabled = false;
                    }
                    //DR - add to checked and remove from unchecked list
                    if (!checkedDictionary.ContainsKey(docName))
                    {
                        checkedDictionary.Add(docName, uncheckedDictionary[docName]);
                        uncheckedDictionary.Remove(docName);
                    }
                    LoadListDictionaries();
                }
                else //DR - remove the data added from the userinput table and enable the appropiate controls
                {
                    this.Refresh();
                    LoadingStartProgress("Removing Overrides...");
                    foreach (TableLayoutPanel tableLayPanel in currentTab.Controls.OfType<TableLayoutPanel>())
                    {
                        //DR- enable field textbox and remove input from userinput table
                        foreach (Control c in tableLayPanel.Controls)
                        {
                            //DR - flag to see if our control is dependant on another
                            bool isChild = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    if (list.Value.Contains(c))
                                    {
                                        isChild = true;
                                    }
                                }
                            }
                            if (c.Name.Contains("FieldControl"))
                            {
                                fID = GetFieldID(c.Name);
                                LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, InputID, workflowID);

                                //DR - do textbox specific logic if the control is a textbox
                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    MaskedTextBox maskTextBox = (MaskedTextBox)c;
                                    maskTextBox.Text = null; //DR - set textbox to null
                                    var fieldQuery = (from fQuery in _AppEntity.Fields.AsNoTracking() where fQuery.Id == fID select fQuery).Single(); //DR - declare and initilize our fieldquery variable


                                    switch (fieldQuery.Client_Data_Type.ToLower()) //DR- determines whether the maskedTextBox uses a mask and sets it
                                    {
                                        case "double":

                                            break;
                                        default:
                                            maskTextBox.Mask = GetMask(fieldQuery.Client_Data_Type); //DR - passes in the type to get the mask
                                            break;
                                    }
                                }

                                c.Enabled = true;

                                var loanDataQuery = (from loan in _AppEntity.vLoanDatas.AsNoTracking() where loan.ID == loanid select loan).Single();//DR-get the row data for the loan
                                var fieldValue = loanDataQuery.GetType().GetProperty("Field" + fID).GetValue(loanDataQuery);//DR-get the actual value stored for that field from loan data

                                switch (_AppUser.ActiveRole)
                                {
                                    case 1:
                                        string clientDataType = (from f in _AppEntity.vFields.AsNoTracking() where f.Id == fID select f.Client_Data_Type).Single();
                                        //DR - commentcodes must always be escalated
                                        if (clientDataType.ToLower().Trim().Equals("commentcode") | clientDataType.ToLower().Trim().Equals("prepaymentpenalty"))
                                        {
                                            UnmatchedCount--;
                                        }
                                        //DR - unmatched count wasn't incremented before if fieldValue is equal to null or empty string so no need to unincrement it
                                        //DR - checking for null must come first or null exception error will be thrown
                                        else if (fieldValue == null)
                                        {

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {

                                        }
                                        else if (imageValList.Contains(docID))
                                        {

                                        }
                                        else //DR - unincrement unmatchedcount 
                                        {
                                            UnmatchedCount--;
                                        }
                                        break;
                                    case 2:
                                    case 3:
                                        //DR - unmatched count wasn't incremented before if fieldValue is equal to null or empty string so no need to unincrement it
                                        //DR - checking for null must come first or null exception error will be thrown
                                        if (fieldValue == null)
                                        {

                                        }
                                        else if (fieldValue.ToString().Trim().Equals(""))
                                        {

                                        }
                                        else if (imageValList.Contains(docID))
                                        {

                                        }
                                        else //DR - unincrement unmatchedcount 
                                        {
                                            UnmatchedCount--;
                                        }
                                        break;
                                }
                            }
                            //DR - disable the control
                            if (isChild)
                            {
                                c.Enabled = false;
                            }
                        }

                        foreach (CheckBox chkbox in tableLayPanel.Controls.OfType<CheckBox>()) //DR - enables checkboxes
                        {
                            bool isChildOverride = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    foreach (Control childControl in list.Value)
                                    {
                                        if (GetFieldID(chkbox.Name).Equals(GetFieldID(childControl.Name)))
                                        {
                                            isChildOverride = true;
                                        }
                                    }
                                }
                            }
                            //DR - only gets enabled if it's not a child control's fieldoverride
                            if (!isChildOverride)
                            {
                                chkbox.Enabled = true;
                            }

                        }

                        //DR - enables comboboxes
                        foreach (ComboBox cb in tableLayPanel.Controls.OfType<ComboBox>())
                        {
                            bool isChildDocList = false;
                            if (FieldDependencyDictionary.ContainsKey(docID))
                            {
                                foreach (KeyValuePair<Control, ArrayList> list in FieldDependencyDictionary[docID])
                                {
                                    foreach (Control childControl in list.Value)
                                    {
                                        if (GetFieldID(cb.Name).Equals(GetFieldID(childControl.Name)))
                                        {
                                            isChildDocList = true;
                                        }
                                    }
                                }
                            }
                            //DR - only gets enabled if it's not a child control's docList
                            if (!isChildDocList)
                            {
                                cb.Enabled = true;
                            }

                        }

                        foreach (Button btn in tableLayPanel.Controls.OfType<Button>())
                        {
                            btn.Enabled = true;
                        }

                        if (_AppUser.ActiveRole == 1)
                        {
                            btnCheckAnswers.Enabled = true;
                        }

                        //DR - if Tier 2 or 3 fieldControls need to be disabled unless new value is selected
                        if (_AppUser.ActiveRole == 2 | _AppUser.ActiveRole == 3)
                        {
                            foreach (Control control in tableLayPanel.Controls)
                            {
                                if (control.Name.ToLower().Contains("fieldcontrol"))
                                {
                                    control.Enabled = false;
                                }
                            }
                        }

                    }

                    //DR-remove checked and add to unchecked list
                    uncheckedDictionary.Add(docName, checkedDictionary[docName]);
                    checkedDictionary.Remove(docName);
                    LoadListDictionaries();

                    if (imageValList.Contains(docID))
                    {
                        imageValList.Remove(docID);
                    }

                }
                LoadingCloseProgress();
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        /// <summary>
        /// MA - Stores a Warning Message with its corresponding document
        /// </summary>
        /// <param name="ActiveTab"></param>
        /// <param name="WarningMessage"></param>
        private void AddWarningLabel(TabPage ActiveTab, string WarningMessage)
        {
            TabPageContainsWarningDictionary[ActiveTab.Text] = true;
            TabPageWarningPromptDictionary[ActiveTab.Text] = WarningMessage;
            
        }

        /// <summary>
        /// MA - When warning label link is clicked, a message box will be displayed
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="ActiveTab"></param>
        /// <param name="WarningMessage"></param>
        private void WarningLabelClick(object s, EventArgs e, TabPage ActiveTab, string WarningMessage)
        {
            MessageBox.Show(WarningMessage, "Prompt Messages", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }


       
        /// <summary>
        /// MA - Compares user input with MSP data and allows user 1 retry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckAnswers(object sender, System.EventArgs e)
        {
            try
            {
                LoadingStartProgress("Validating Entries");

                TabPage currentTab = tabControl_Doc_Control.SelectedTab;

                int docId = GetDocumentID(currentTab.Text);
                Control control = new Control();
                Boolean isFirst = false;
                //checking through all the controls for the values on the text boxes
                foreach (TableLayoutPanel current in currentTab.Controls.OfType<TableLayoutPanel>())
                {
                    current.SuspendLayout();
                    //DR - declare placeholder variables
                    MaskedTextBox textBox;
                    CheckBox checkBox;
                    GroupBox groupBox;
                    ComboBox comboBox;

                    //DR - loop through each control and do the appropriate comparison based on its controltype
                    foreach (Control c in current.Controls)
                    {
                        //DR - the control must be have fieldcontrol in its name 
                        if (c.Name.ToLower().Contains("fieldcontrol"))
                        {
                            if (!isFirst) { isFirst = true; control = c; }

                            //DR - checkanswer if field is not overriden
                            if (!LocalDatabase.LocalIsOverriden(loanid, _AppUser.Id, docId, GetFieldID(c.Name), 2, workflowID))
                            {
                                //DR - if control is of type textbox
                                if (c.Name.ToLower().Contains("textbox"))
                                {
                                    textBox = (MaskedTextBox)c;
                                    CheckAnswerTextBox(currentTab, docId, textBox, 1);
                                    textBox = null;
                                }
                                //DR - if control is of type checkbox
                                else if (c.Name.ToLower().Contains("checkbox"))
                                {
                                    checkBox = (CheckBox)c;
                                    CheckAnswerCheckBox(currentTab, docId, checkBox, 1);
                                    checkBox = null;
                                }
                                //DR - if control is of type groupbox
                                else if (c.Name.ToLower().Contains("groupbox"))
                                {
                                    groupBox = (GroupBox)c;
                                    CheckAnswerGroupBox(currentTab, docId, groupBox, 1);
                                    groupBox = null;
                                }
                                //DR - if control is of type combobox
                                else if (c.Name.ToLower().Contains("combobox"))
                                {
                                    comboBox = (ComboBox)c;
                                    CheckAnswerComboBox(currentTab, docId, comboBox, 1);
                                    comboBox = null;
                                }
                            }
                        }
                    }

                    //DR - add to checked and remove from unchecked list
                    checkedDictionary.Add(currentTab.Text, uncheckedDictionary[currentTab.Text]);
                    uncheckedDictionary.Remove(currentTab.Text);
                    LoadListDictionaries();

                    //DR - disable the override checkbox because submit button was clicked
                    foreach (var currentCheckBox in current.Controls.OfType<CheckBox>())
                    {
                        if (currentCheckBox.Name.Contains("DocumentOverride"))
                        {
                            currentCheckBox.Enabled = false;
                        }
                    }
                    current.ResumeLayout();

                }
                //DR - If the current document contains warnings...
                if (checkAnswersBool)
                {
                    //MA - Close the loading form
                    LoadingCloseProgress();

                    //DR - Displays the warnings for current document
                    MessageBox.Show(checkAnswersPromptMessageString, "Prompt Messages", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    //MA - Store the warning label
                    AddWarningLabel(currentTab, checkAnswersPromptMessageString);

                    //MA - Display the warning label link
                    ViewWarningStripStatusLabel.Visible = true;

                    //DR - Reset for next document
                    checkAnswersPromptMessageCount = 0;
                    checkAnswersPromptMessageString = "";
                    checkAnswersBool = false;

                }
                else
                {
                    //MA - Close the loading form
                    LoadingCloseProgress();
                }

                //DR - Disable check answers for document
                btnCheckAnswers.Enabled = false;
                control.Focus();
            }
            catch (Exception ex)
            {
                LoadingCloseProgress();
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form_User_App_Load(object sender, EventArgs e)
        {

            switch (_AppUser.ActiveRole)
            {
                //DR - Tier 1
                case 1:
                    this.Text = "Loan Review - Tier I";
                    break;
                //DR - Tier 2
                case 2:
                    this.Text = "Loan Review - Tier II";
                    break;
                //DR - Tier 3
                case 3:
                    this.Text = "Loan Review - Tier III";
                    break;
            }

            //DR - Tiers 1, 2, and 3 dictionaries
            DatabaseDictionary = new Dictionary<string, Dictionary<int, object>>(); //DR - creates a fresh dictionary so we can add our tabs
            UserInputDictionary = new Dictionary<string, Dictionary<int, object>>();//MA - storing user data
            FieldDependencyDictionary = new Dictionary<int, Dictionary<Control, ArrayList>>(); //DR - creates a fresh dictionary for our field dependency controls
            uncheckedDictionary = new Dictionary<string, TabPage>();//DR - creates a fresh dictionary for our uncheckeddocs
            checkedDictionary = new Dictionary<string, TabPage>();//DR - creates a fresh dictionary for our checkeddocs
            DocTimerDictionary = new Dictionary<string, System.Windows.Forms.Timer>();//DR - creates a fresh dictionary for our document timers
            DocTickDictionary = new Dictionary<string, ulong>();//DR - creates a fresh dictionary for our document ticks
            shortcutDictionary = new Dictionary<String, TabPage>();//DR - creates a fresh dictionary for our tabpage shortcuts
            DocumentListDictionary = new Dictionary<int, Dictionary<int, ComboBox>>();//DR-creates a fresh dictionary for our documentlist comboboxes
            docCheckDictionary = new Dictionary<string, CheckBox>();//DR - creates a fresh dictionary for our doc checkboxes
            FieldLabelDictionary = new Dictionary<int, Dictionary<int, Label>>();
            FieldDependenciesDictionary = new Dictionary<int, Dictionary<int, ArrayList>>();
            //DR - Tier 1 dictionary(s)
            fieldCheckDictionary = new Dictionary<int, Dictionary<int, CheckBox>>();//DR- creates a fresh dictionary for our fieldOverride checkboxes
            FieldNAByDefaultDictionary = new Dictionary<int, ArrayList>();
            FieldHasPromptDictionary = new Dictionary<int, Dictionary<int, bool>>();
            FieldPromptMessageDictionary = new Dictionary<int, Dictionary<int, string>>();
            TabPageContainsWarningDictionary = new Dictionary<string, bool>();
            TabPageWarningPromptDictionary = new Dictionary<string, string>();
            RedundantFieldsDictionary = new Dictionary<int, Dictionary<int, ArrayList>>();
            //DR - Tier 2 dictionary(s)
            fieldCriticalCheckDictionary = new Dictionary<int, bool>(); //DR - creates a fresh dictionary for our field critical checks
            //DR- Tier 2 and 3 dictionaries
            UserTier1Dictionary = new Dictionary<string, Dictionary<int, object>>();
            fieldCountDictionary = new Dictionary<int, int>();//DR - creates a fresh dictionary for our fieldcounts
            //DR - Tier 3 dictionary(s)
            UserTier2Dictionary = new Dictionary<string, Dictionary<int, object>>();//will store the tier 2 inputs for a loan

            _AppUser = (from user in _AppEntity.vUsers.AsNoTracking() where user.Id == _AppUser.Id select user).Single();

            docList = new ArrayList();
            var docQuery = from doc in _AppEntity.vDocuments.AsNoTracking() select doc;
            //DR - add the document names to the doclist
            foreach (var doc in docQuery)
            {
                docList.Add(doc.Name);
            }

            imageValList = new ArrayList();
            tabControl_Doc_Control.Enabled = false;
            userID.Text = _AppUser.Name;
            lblUserID.Text = _AppUser.Id.ToString();
            lblRole.Text = _AppUser.ActiveRole.ToString();
            InitTimer();
            GetLastLogin();
            toolStripStatusDBpic.Image = LoanReview.Properties.Resources.Connection_Good;
            toolStripServerName.Text = Environment.MachineName;

            //MA - Implements dynamic labels of shorcut keys for tabs on the right panel
            List<string> shortcuts = new List<string>();
            var skeys = (from doc in _AppEntity.Documents.AsNoTracking() select new { doc.Name, doc.ShortcutKey }).ToList();
            StringBuilder shortcutval = new StringBuilder();
            foreach (var skey in skeys)
            {
                string[] order = skey.ShortcutKey.Split(',');
                for (int x = order.Length - 1; x >= 0; x--)
                {
                    if (x > 0)
                    {
                        shortcutval.Append(order[x].Trim() + "+");
                    }
                    else
                        shortcutval.Append(order[x].Trim());
                }

                shortcuts.Add(skey.Name + " : " + shortcutval);
                Label shortcutLabel = new Label()
                {
                    Text = skey.Name + " : " + shortcutval,
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Padding = new Padding(5, 5, 5, 5),
                };
                shortcutGB.Controls.Add(shortcutLabel);
                shortcutval.Clear();
            }
            Label fieldOverrideLabel = new Label()
            {
                Text = "Field Override" + " : " + "Control + F",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(fieldOverrideLabel);
            Label pauseLabel = new Label()
            {
                Text = "Pause Timer" + " : " + "Control + T",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(pauseLabel);
            Label minimizedLabel = new Label()
            {
                Text = "Minimize Window" + " : " + "Control + F1",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(minimizedLabel);
            Label normalLabel = new Label()
            {
                Text = "Normal Window" + " : " + "Control + F2",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(normalLabel);
            Label maximizeLabel = new Label()
            {
                Text = "Maximize Window" + " : " + "Control + F3",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(maximizeLabel);
            Label logoutLabel = new Label()
            {
                Text = "Logout" + " : " + "Control + L",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(logoutLabel);
            Label finalSubmitLabel = new Label()
            {
                Text = "Submit Loan" + " : " + "Control + F12",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(finalSubmitLabel);
            Label checkAnswersLabel = new Label()
            {
                Text = "Check Answers" + " : " + "Control + F11",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(checkAnswersLabel);
            Label getLoanLabel = new Label()
            {
                Text = "Get Loan" + " : " + "Control + G",
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 5),
            };
            shortcutGB.Controls.Add(getLoanLabel);
            
            WorkflowComboBox.SelectedIndex = 0;
            switch (_AppUser.PermissionRole)
            {
                case 1:
                    role1SwitchMenuItem.Enabled = true;
                    break;
                case 2:
                    role1SwitchMenuItem.Enabled = true;
                    role2SwitchMenuItem.Enabled = true;
                    break;
                default:
                    role1SwitchMenuItem.Enabled = true;
                    role2SwitchMenuItem.Enabled = true;
                    if (_AppUser.Department == 1)
                    {
                        role3SwitchMenuItem.Enabled = true;
                    }
                    break;
            }


        }

        /// <summary>
        /// MA - Logs the user of the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logoutBtn_Click(object sender, EventArgs e)
        {
            this.isLoggingOut = true;
            this.Close();
            this.isLoggingOut = false;
        }

        /// <summary>
        /// MA - Exits the user out of the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //DR and MA - checks to see if the keydata(our shortcuts) is found in the shortcutDictionary, if so it selects the tab, And included shortcuts for static buttons
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (shortcutDictionary.ContainsKey(keyData.ToString()))
            {
                tabControl_Doc_Control.SelectedTab = shortcutDictionary[keyData.ToString()];
                return true;
            }
            else
            {
                switch (keyData.ToString())
                {
                    case "F11, Control":
                        btnCheckAnswers.PerformClick();
                        break;
                    case "F12, Control":
                        finalSubmitbtn.PerformClick();
                        break;
                    case "G, Control":
                        Loanbtn.PerformClick();
                        return true;
                        break;
                    case "T, Control":
                        DocTimerbtn.PerformClick();
                        return true;
                        break;
                    case "F1, Control":
                        this.WindowState = FormWindowState.Minimized;
                        break;
                    case "F2, Control":
                        this.WindowState = FormWindowState.Normal;
                        break;
                    case "F3, Control":
                        this.WindowState = FormWindowState.Maximized;
                        break;
                    case "L, Control":
                        logoutBtn.PerformClick();
                        return true;
                        break;
                    case "F, Control":
                        if(_AppUser.ActiveRole != 1)
                        {
                            return true;
                        }
                        if(this.ActiveControl.Name.Contains("FieldControl"))
                        {
                            if(fieldCheckDictionary.ContainsKey(GetDocumentID(tabControl_Doc_Control.SelectedTab.Text)))
                            {
                                if (fieldCheckDictionary[GetDocumentID(tabControl_Doc_Control.SelectedTab.Text)].ContainsKey(GetFieldID(this.ActiveControl.Name)))
                                {
                                    //DR - forces the controls event to fire
                                    String methodName = "On" + "Click";

                                    System.Reflection.MethodInfo miOne = fieldCheckDictionary[GetDocumentID(tabControl_Doc_Control.SelectedTab.Text)][GetFieldID(this.ActiveControl.Name)].GetType().GetMethod(
                                          methodName,
                                          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                                    if (miOne == null)
                                        throw new ArgumentException("Cannot find event thrower named " + methodName);

                                    miOne.Invoke(fieldCheckDictionary[GetDocumentID(tabControl_Doc_Control.SelectedTab.Text)][GetFieldID(this.ActiveControl.Name)], new object[] { new EventArgs() });
                                }
                            }
                        }
                        return true;
                        break;
                }
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        //DR - Used to switch role to role 1
        private void role1SwitchMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //DR - only switch their role if they have appropiate permissions
                if (_AppUser.PermissionRole >= 1)
                {
                    //DR - they are assigned a loan. prompt them if they role switch that they will lose data
                    if (loanid > 0)
                    {
                        var dialogResult = MessageBox.Show("Are you sure you want to Role Switch? All data for the current loan will be lost.", "Role Switch", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (dialogResult == DialogResult.OK)
                        {
                            LoadingStartProgress("Processing");
                            //DR - Adds the loan dropped action to the Histories table
                            _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                            switch (_AppUser.ActiveRole)
                            {
                                //DR - Tier 1
                                case 1:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                    }
                                    break;
                                //DR - Tier 2
                                case 2:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                    }
                                    break;
                                //DR - Tier 3
                                case 3:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                    }
                                    break;
                            }

                            resetControls();
                            LoadingCloseProgress();
                        }
                        else
                        {
                            return;
                        }
                    }

                    LoadingStartProgress("Processing");
                    _AppEntity.UpdateActiveRole(_AppUser.Id, 1);
                    _AppUser.ActiveRole = 1;
                    _AppEntity.CreateHistoryEvent(_AppUser.Id, null, Convert.ToInt32(_AppUser.ActiveRole), 9, workflowID);
                    lblRole.Text = _AppUser.ActiveRole.ToString();
                    this.Text = "Loan Review - Tier I";
                    LoadingCloseProgress();
                    MessageBox.Show("Role successfully switched to Role 1", "Role Switch Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else
                {
                    MessageBox.Show("You do not have the appropiate permission to switch to Role 1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        //DR - Used to switch role to role 2
        private void role2SwitchMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //DR - only switch their role if they have appropiate permissions
                if (_AppUser.PermissionRole >= 2)
                {
                    //DR - they are assigned a loan. prompt them if they role switch that they will lose data
                    if (loanid > 0)
                    {
                        var dialogResult = MessageBox.Show("Are you sure you want to Role Switch? All data for the current loan will be lost.", "Role Switch", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (dialogResult == DialogResult.OK)
                        {
                            LoadingStartProgress("Processing");

                            //DR - Adds the loan dropped action to the Histories table
                            _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                            switch (_AppUser.ActiveRole)
                            {
                                //DR - Tier 1
                                case 1:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                    }
                                    break;
                                //DR - Tier 2
                                case 2:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                    }
                                    break;
                                //DR - Tier 3
                                case 3:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                    }
                                    break;
                            }

                            resetControls();
                            LoadingCloseProgress();
                        }
                        else
                        {

                            return;
                        }
                    }

                    LoadingStartProgress("Processing");
                    _AppEntity.UpdateActiveRole(_AppUser.Id, 2);
                    _AppUser.ActiveRole = 2;
                    _AppEntity.CreateHistoryEvent(_AppUser.Id, null, Convert.ToInt32(_AppUser.ActiveRole), 9, workflowID);
                    lblRole.Text = _AppUser.ActiveRole.ToString();
                    this.Text = "Loan Review - Tier II";
                    LoadingCloseProgress();
                    MessageBox.Show("Role successfully switched to Role 2", "Role Switch Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else
                {
                    MessageBox.Show("You do not have the appropiate permission to switch to Role 2.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        //DR - Used to switch role to role 3
        private void role3SwitchMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //DR - only switch their role if they have appropiate permissions
                if (_AppUser.PermissionRole >= 3 && _AppUser.Department == 1)
                {
                    //DR - they are assigned a loan. prompt them if they role switch that they will lose data
                    if (loanid > 0)
                    {
                        var dialogResult = MessageBox.Show("Are you sure you want to Role Switch? All data for the current loan will be lost.", "Role Switch", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (dialogResult == DialogResult.OK)
                        {
                            LoadingStartProgress("Processing");
                            //DR - Adds the loan dropped action to the Histories table
                            _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                            switch (_AppUser.ActiveRole)
                            {
                                //DR - Tier 1
                                case 1:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                    }
                                    break;
                                //DR - Tier 2
                                case 2:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                    }
                                    break;
                                //DR - Tier 3
                                case 3:
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                    }
                                    break;
                            }

                            resetControls();
                            LoadingCloseProgress();
                        }
                        else
                        {
                            return;
                        }
                    }

                    LoadingStartProgress("Processing");
                    _AppEntity.UpdateActiveRole(_AppUser.Id, 3);
                    _AppUser.ActiveRole = 3;
                    _AppEntity.CreateHistoryEvent(_AppUser.Id, null, Convert.ToInt32(_AppUser.ActiveRole), 9, workflowID);
                    lblRole.Text = _AppUser.ActiveRole.ToString();
                    this.Text = "Loan Review - Tier III";
                    LoadingCloseProgress();
                    MessageBox.Show("Role successfully switched to Role 3", "Role Switch Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else
                {
                    MessageBox.Show("You do not have the appropiate permission to switch to Role 3.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
#if DEBUG
        private void button1_Click(object sender, EventArgs e)
        {


            int count = 20;
            //int count = 0;

            try
            {
                if (_AppUser.ActiveRole == 1)
                {
                    while (count > 1)
                    {
                        Loanbtn.PerformClick();

                        foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                        {
                            string docName = tp.Text;
                            var docId =
                                (from doc in _AppEntity.Documents.AsNoTracking() where docName == doc.Name select doc.Id)
                                    .Single();
                            foreach (TableLayoutPanel panel in tp.Controls.OfType<TableLayoutPanel>())
                            {
                                if (docName == "asdf")
                                {
                                    foreach (Button c in panel.Controls.OfType<Button>())
                                    {
                                        if (c.Enabled == true)
                                        {
                                            c.Enabled = false;
                                        }
                                    }
                                    docCheckDictionary[docName].Checked = true;
                                    if (isMessageBoxOpen)
                                    {
                                        SendKeys.Send("{Enter}");
                                    }
                                    SendKeys.Send("{Enter}");
                                    DocCheck_Clicked(sender, e, tp, docName, Convert.ToInt32(docId));
                                    if (isMessageBoxOpen)
                                    {
                                        SendKeys.Send("{Enter}");
                                    }
                                    SendKeys.Send("{Enter}");
                                }
                                else
                                {
                                    foreach (Control c in panel.Controls)
                                    {

                                        if (c.GetType() == typeof(Button))
                                        {
                                            c.Enabled = false;
                                        }
                                        if (c.GetType() == typeof(CheckBox))
                                        {
                                            if (c.Name.Contains("DocumentOverride_checkbox_"))
                                            {
                                            }
                                            else if (c.Name.Contains("DocumentOverride_checkbox_"))
                                            {
                                            }
                                        }
                                        if (c.GetType() == typeof(MaskedTextBox))
                                        {
                                            if (c.Name.Contains("_FieldControlTextBox_"))
                                            {
                                                if (c.Enabled == true)
                                                {
                                                    //int fieldId = Convert.ToInt32(c.Name.Split('_')[c.Name.Split('_').Length - 1]);
                                                    int fieldId = GetFieldID(c.Name);

                                                    String clientType =
                                                        (from f in _AppEntity.Fields.AsNoTracking()
                                                         where f.Id == fieldId
                                                         select f.Client_Data_Type).Single();

                                                    List<string> cbnames = new List<string>();
                                                    Random randnum = new Random();

                                                    switch (clientType.ToLower())
                                                    {
                                                        case "string":
                                                            cbnames.Add("Bilbo Bagans");
                                                            cbnames.Add("Lucas SkyWalker");
                                                            cbnames.Add("Shrek Lopez");
                                                            cbnames.Add("Han Solo");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                        case "double":
                                                            cbnames.Add("23165.56");
                                                            cbnames.Add("9515.10");
                                                            cbnames.Add("9348224.00");
                                                            cbnames.Add("837.235");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                        case "int":
                                                            cbnames.Add("145313");
                                                            cbnames.Add("68133584");
                                                            cbnames.Add("28212");
                                                            cbnames.Add("934822");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                        case "currency":
                                                            cbnames.Add("$123.456");
                                                            cbnames.Add("$3642.496");
                                                            cbnames.Add("$65123.93");
                                                            cbnames.Add("$512923.856");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                        case "date":
                                                            cbnames.Add("35/12/9538");
                                                            cbnames.Add("08/06/1919");
                                                            cbnames.Add("02/65/1519");
                                                            cbnames.Add("01/25/7521");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                        case "ssn":
                                                            cbnames.Add("933-26-6490");
                                                            cbnames.Add("645-56-4552");
                                                            cbnames.Add("182-64-2974");
                                                            cbnames.Add("155-66-9108");
                                                            c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                            ;
                                                            cbnames.Clear();
                                                            continue;
                                                    }
                                                }

                                            }

                                        }
                                    }
                                    SendKeys.Send("{Enter}");
                                    CheckAnswers(sender, e, tp, Convert.ToInt32(docId));
                                }
                            }
                        }
                        SendKeys.Send("{Enter}");
                        finalSubmitbtn_Click(sender, e);
                        SendKeys.Send("{Enter}");
                        count--;
                    }
                    button1.Enabled = true;
                }
                else if (_AppUser.ActiveRole == 2)
                {
                    while (count > 1)
                    {
                        Loanbtn.PerformClick();


                        foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
                        {
                            string DocumentKey = tp.Text;

                            foreach (TableLayoutPanel tablePanel in tp.Controls.OfType<TableLayoutPanel>())
                            {

                                int rows = (from UI in _AppEntity.UserInputs.AsNoTracking()
                                            join doc in _AppEntity.Documents.AsNoTracking()
                                                on UI.DocID equals doc.Id
                                            where UI.LoanID == loanid
                                                  && UI.InputID == 2
                                                  && UI.isMatching == false
                                                  && doc.Name == DocumentKey
                                            select new { UI }).Count();
                                rows = rows + 1;

                                for (int i = 0; i < rows; i++)
                                {
                                    string randomCheckbox = Randomize();
                                    string randomTextbox;
                                    if (randomCheckbox == "_MSPcheckbox_")
                                    {
                                        randomTextbox = "_MSPtextBox_";
                                    }
                                    else if (randomCheckbox == "_Tier1Entrycheckbox_")
                                    {
                                        randomTextbox = "_Tier1EntrytextBox_";
                                    }
                                    else
                                    {
                                        randomTextbox = "_textBox_";
                                    }

                                    for (int columnIndex = 0; columnIndex < 11; columnIndex++)
                                    {
                                        var control = tablePanel.GetControlFromPosition(columnIndex, i);
                                        if (control != null)
                                        {
                                            if (control.Name.Contains(randomCheckbox))
                                            {
                                                if (control.GetType() == typeof(ComboBox))
                                                {
                                                    ComboBox cbox = (ComboBox)control;
                                                    cbox.SelectedIndex = 0;

                                                }
                                                if (control.GetType() == typeof(CheckBox))
                                                {
                                                    CheckBox checkbox = (CheckBox)control;
                                                    if (control.Name.Contains("FieldOverride"))
                                                    {
                                                        return;
                                                    }
                                                    //DR - forces the controls event to fire
                                                    String methodName = "On" + "Click";

                                                    System.Reflection.MethodInfo mi = checkbox.GetType().GetMethod(
                                                        methodName,
                                                        System.Reflection.BindingFlags.Instance |
                                                        System.Reflection.BindingFlags.NonPublic);

                                                    if (mi == null)
                                                        throw new ArgumentException("Cannot find event thrower named " +
                                                                                    methodName);

                                                    mi.Invoke(checkbox, new object[] { e });
                                                }
                                            }
                                            else if (control.Name.Contains(randomTextbox))
                                            {
                                                List<string> cbnames = new List<string>();
                                                Random randnum = new Random();
                                                if (control.GetType() == typeof(MaskedTextBox))
                                                {
                                                    if (control.Enabled == true)
                                                    {
                                                        MaskedTextBox c = (MaskedTextBox)control;

                                                        //int fieldId = Convert.ToInt32(c.Name.Split('_')[c.Name.Split('_').Length - 1]);
                                                        int fieldId = GetFieldID(c.Name);

                                                        var clientType =
                                                            (from f in _AppEntity.Fields.AsNoTracking()
                                                             where f.Id == fieldId
                                                             select f.Client_Data_Type).Single();


                                                        switch (clientType.ToLower())
                                                        {
                                                            case "string":
                                                                cbnames.Add("Bilbo Bagans");
                                                                cbnames.Add("Lucas SkyWalker");
                                                                cbnames.Add("Shrek Lopez");
                                                                cbnames.Add("Han Solo");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                            case "double":
                                                                cbnames.Add("23165.56");
                                                                cbnames.Add("9515.10");
                                                                cbnames.Add("9348224.00");
                                                                cbnames.Add("837.235");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                            case "int":
                                                                cbnames.Add("145313");
                                                                cbnames.Add("68133584");
                                                                cbnames.Add("28212");
                                                                cbnames.Add("934822");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                            case "currency":
                                                                cbnames.Add("$123.456");
                                                                cbnames.Add("$3642.496");
                                                                cbnames.Add("$65123.93");
                                                                cbnames.Add("$512923.856");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                            case "date":
                                                                cbnames.Add("35/12/9538");
                                                                cbnames.Add("08/06/1919");
                                                                cbnames.Add("02/65/1519");
                                                                cbnames.Add("01/25/7521");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                            case "ssn":
                                                                cbnames.Add("933-26-6490");
                                                                cbnames.Add("645-56-4552");
                                                                cbnames.Add("182-64-2974");
                                                                cbnames.Add("155-66-9108");
                                                                c.Text = cbnames[randnum.Next(cbnames.Count())].ToString();
                                                                ;
                                                                cbnames.Clear();
                                                                continue;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                SendKeys.Send("{Enter}");
                            }

                            finalSubmitbtn_Click(sender, e);
                            SendKeys.Send("{Enter}");
                            count--;
                            //break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private string Randomize()
        {
            List<string> cbnames = new List<string>();
            cbnames.Add("_MSPcheckbox_");
            cbnames.Add("_Tier1Entrycheckbox_");
            cbnames.Add("_UserTier2checkbox_");
            Random randnum = new Random();
            return cbnames[randnum.Next(cbnames.Count())].ToString();
        }
#endif
        
        /// <summary>
        /// MA - displays shortcuts for each tab from DB and shorcuts for app buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> shortcuts = new List<string>();
            var skeys = (from doc in _AppEntity.Documents.AsNoTracking() select new { doc.Name, doc.ShortcutKey }).ToList();
            StringBuilder shortcutval = new StringBuilder();

            shortcuts.Add("Get Loan" + " : " + "Control + G");
            if (_AppUser.ActiveRole == 1)
            {
                shortcuts.Add("Check Answers" + " : " + "Control, F11");
                shortcuts.Add("Field Override" + " : " + "Control, F");
            }
            shortcuts.Add("Submit Loan" + " : " + "Control + F12");

            foreach (var d in skeys)
            {
                string[] order = d.ShortcutKey.Split(',');
                for (int x = order.Length - 1; x >= 0; x--)
                {
                    if (x > 0)
                    {
                        shortcutval.Append(order[x].Trim() + "+");
                    }
                    else
                        shortcutval.Append(order[x].Trim());
                }

                shortcuts.Add(d.Name + " : " + shortcutval);
                shortcutval.Clear();
            }
            shortcuts.Add("Field Override" + " : " + "Control + F");
            shortcuts.Add("Pause Timer" + " : " + "Control + T");
            shortcuts.Add("Minimize Window" + " : " + "Control + F1");
            shortcuts.Add("Normal Window" + " : " + "Control + F2");
            shortcuts.Add("Maximize Window" + " : " + "Control + F3");
            shortcuts.Add("Logout" + " : " + "Control + L");

            MessageBox.Show(shortcuts.Aggregate((str, val) => str + Environment.NewLine + val), "Shortcut keys");
        }

        /// <summary>
        /// MA - Allows the user to pause/resume the timer on the loan 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocTimerbtn_Click(object sender, EventArgs e)
        {
            try
            {
                //MA - Get the text of the Document timer button
                switch (DocTimerbtn.Text)
                {
                    //MA - If it says pause
                    case "Pause":

                        //MA - Change the text to resume 
                        DocTimerbtn.Text = "Resume";

                        //MA - Pause all the timers
                        PauseDocTimers();

                        //MA - Hide the table pages
                        tabControl_Doc_Control.Visible = false;

                        //MA - Disable the check answers and final submit button
                        btnCheckAnswers.Enabled = false;
                        finalSubmitbtn.Enabled = false;

                        //Create a label to display the pause message
                        Label WarningLabel = new Label()
                        {
                            Font = new Font("Arial", 19),
                            Text = "Timer has been Paused. \nPlease Click Resume to \ncontinue processing.",
                            //AutoSize = true,
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter,

                        };

                        //MA - Add the label to the Tab control panel
                        panel_Tab_Control.Controls.Add(WarningLabel);

                        //MA - Create an event in the histories table, for a user pause event
                        _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 10, workflowID);
                        break;

                    //MA - If it says resume
                    case "Resume":

                        //MA - Change the text to pause 
                        DocTimerbtn.Text = "Pause";

                        //MA - Get the current tab page
                        TabPage tabPage = tabControl_Doc_Control.SelectedTab;

                        //MA - Remove the pause label from the panel
                        foreach (Control ctrl in panel_Tab_Control.Controls)
                        {
                            if (ctrl.GetType() == typeof(Label))
                            {
                                panel_Tab_Control.Controls.Remove(ctrl);
                            }
                        }

                        //MA - Start the timers for the loan
                        DocTimerDictionary[tabPage.Text].Start();

                        //MA - Show the tab control 
                        tabControl_Doc_Control.Visible = true;

                        //MA - If the user is in Tier 1
                        if (_AppUser.ActiveRole == 1)
                        {
                            //MA - If the user already checked answers then disable check answers button for that tab
                            if (checkedDictionary.ContainsKey(tabPage.Text))
                            {
                                btnCheckAnswers.Enabled = false;
                            }
                            //MA - Otherwise enable the check answers button
                            else
                            {
                                btnCheckAnswers.Enabled = true;
                            }
                        }

                        //MA - Enable the final submit button
                        finalSubmitbtn.Enabled = true;

                        //MA - Create a user event in the histories table that the user resumed the loan
                        _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 11, workflowID);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// MA - Sets the workflow id based on the index of the value selected in WorkflowComboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorkflowComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //MA - If Normal workflow was selected
            if (WorkflowComboBox.SelectedIndex == 0)
            {
                workflowID = 1;
            }
            //MA - If IMAVAL workflow was selected
            else if (WorkflowComboBox.SelectedIndex == 1)
            {
                workflowID = 2;
            }
            LoanTimer_Tick(null, null);
        }

        private void finalSubmitbtn_Click(object sender, EventArgs e) //MA - confirms retries by user, validates tabs, and escalates loans accordingly
        {
            try
            {
                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier 1
                    case 1:
                        FinalSubmit();
                        break;
                    //DR - Tier 2
                    case 2:
                        FinalSubmit2();
                        break;
                    //DR - Tier 3
                    case 3:
                        FinalSubmit3();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR - event handler for the tablelayoutpanel mousemove event. Displays the controls tooltip if it has one
        private void toolTips_MouseMove(object sender, MouseEventArgs e, TableLayoutPanel tablePanel)
        {
            Control control = tablePanel.GetChildAtPoint(e.Location);
            //DR - if control is not null
            if (control != null)
            {
                //DR - if control is disabled and currenttooltip isn't displaying anything
                if (!control.Enabled && _currentToolTipControl == null)
                {
                    string toolTipString = toolTips.GetToolTip(control);
                    //DR - trigger the tooltip with no delay and some basic positioning just to give you an idea
                    toolTips.Show(toolTipString, control, control.Width / 2, control.Height / 2);
                    _currentToolTipControl = control;
                }
            }
            else
            {
                if (_currentToolTipControl != null) toolTips.Hide(_currentToolTipControl);
                _currentToolTipControl = null;
            }
        }

        /// <summary>
        /// MA - Loads Loan into Application, assigns User and allows for data entry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Loanbtn_Click(object sender, EventArgs e)
        {
            Loanbtn.Enabled = false;

        RetryGetLoan:
            try
            {
                this.Refresh();
                var loanQuery = from temp in _AppEntity.vLoanDatas.AsNoTracking() where temp.OwnedByUserID == _AppUser.Id select temp.ID;

                loanTimer.Stop();

                //DR - if user is assigned to more than one loan, remove them from each
                if (loanQuery.Count() >= 1)
                {
                    foreach (int loan in loanQuery)
                    {
                        int lN = Convert.ToInt32(loan);
                        int tier = Convert.ToInt32((from L in _AppEntity.vLoanDatas.AsNoTracking() where L.ID == lN select L.Tier).Single());
                        switch (tier)
                        {
                            //DR - Loan is Tier 1
                            case 1:
                                _AppEntity.RemoveLoanUser((int)loan, _AppUser.Id, 1);
                                _AppEntity.RemoveLoanUser((int)loan, _AppUser.Id, 2);
                                break;
                            //DR - Loan is Tier 2
                            case 2:
                                _AppEntity.RemoveLoanUser((int)loan, _AppUser.Id, 3);
                                break;
                            //DR - Loan is Tier 3
                            case 3:
                                _AppEntity.RemoveLoanUser((int)loan, _AppUser.Id, 4);
                                break;
                            //DR - Loan is Tier 4
                            case 4:
                                _AppEntity.RemoveLoanUser((int)loan, _AppUser.Id, 5);
                                break;
                        }
                    }

                }

                var loanQueryIVT = (from temp in _AppEntity.WorkflowTrackings.AsNoTracking() where temp.OwnedByUserID == _AppUser.Id select temp.LoanID).Distinct();

                //DR - if user is assigned to more than one IVTloan, remove them from each
                if (loanQueryIVT.Count() >= 1)
                {
                    foreach (int loan in loanQueryIVT)
                    {
                        int lN = Convert.ToInt32(loan);
                        int tier = Convert.ToInt32((from L in _AppEntity.WorkflowTrackings.AsNoTracking() where L.LoanID == lN select L.WorkflowTier).Distinct().Single());
                        switch (tier)
                        {
                            //DR - Loan is Tier 1
                            case 1:
                                _AppEntity.IVTRemoveLoanUser((int)loan, _AppUser.Id, 1);
                                _AppEntity.IVTRemoveLoanUser((int)loan, _AppUser.Id, 2);
                                break;
                            //DR - Loan is Tier 2
                            case 2:
                                _AppEntity.IVTRemoveLoanUser((int)loan, _AppUser.Id, 3);
                                break;
                            //DR - Loan is Tier 3
                            case 3:
                                _AppEntity.IVTRemoveLoanUser((int)loan, _AppUser.Id, 4);
                                break;
                            //DR - Loan is Tier 4
                            case 4:
                                _AppEntity.IVTRemoveLoanUser((int)loan, _AppUser.Id, 5);
                                break;
                        }
                    }

                }
                if (!LoanAvailabilityCheck())
                {
                    MessageBox.Show("Unable to get a loan because there are no available loans or you have worked on the available loans previously.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    resetControls();
                    return;
                }

                if (workflowID == 1)
                {
                    var newLoanID = _AppEntity.TestingGetLoans(_AppUser.Id).Single();
                    loanid = (int)newLoanID;
                }
                else if (workflowID == 2)
                {
                    var currentLoan = _AppEntity.GetIVTLoans(_AppUser.Id).Single();
                    loanid = (int)currentLoan;
                }

                WorkflowComboBox.Enabled = false;

                //DR - Adds the loan pulled action to the Histories table
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 6, workflowID);
                this.loanNum = Convert.ToInt32((from L in _AppEntity.vLoanDatas.AsNoTracking() where L.ID == loanid select L.LoanNumber).Single());
                PaymentDueDate = Convert.ToDateTime((from L in _AppEntity.vLoanDatas.AsNoTracking() where L.ID == loanid select L.PaymentDueDate).Single()).ToShortDateString();
                if(PaymentDueDate.Equals("1/1/0001"))
                {
                    PaymentDueDate = "Not Available";
                }
                DealID = (from L in _AppEntity.vLoanDatas.AsNoTracking() where L.ID == loanid select L.DealID).Single();
                System.Windows.Forms.Clipboard.SetText(loanNum.ToString());
                LoadingStartProgress("Loading Loan: " + this.loanNum.ToString());

                //DR - form closing knows to delete userinput values if they close the application   
                finalSubmitClicked = false;

                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier 1
                    case 1:
                        TryPopulateDocumentTabs();
                        break;
                    //DR - Tier 2
                    case 2:
                        TryPopulateDocumentTabs2();
                        break;
                    //DR - Tier 3
                    case 3:
                        TryPopulateDocumentTabs3();
                        break;
                }

                TryLoadDBDictionary(loanid);
                LocalDatabase = new LocalDatabase(_AppUser, _AppEntity, loanid, workflowID);
                LocalDatabase.LocalDBSetup();

                //DR - Manually override fields that are NA by default
                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier 1
                    case 1:
                        foreach (KeyValuePair<int, ArrayList> dictionary in FieldNAByDefaultDictionary)
                        {
                            foreach (Control c in dictionary.Value)
                            {
                                int fID = GetFieldID(c.Name);
                                fieldCheckDictionary[dictionary.Key][fID].Checked = true;
                                FieldOverrideManually("NotApplicable", dictionary.Key, fID, fieldCheckDictionary[dictionary.Key][fID], c);
                            }
                        }
                        //DR - enable the checkanswers button
                        btnCheckAnswers.Enabled = true;
                        break;
                }

                //DR - NA by default logic may skew unmatchedcount so we must correct it
                UnmatchedCount = LocalDatabase.LocalMatchingCount(2, 0);

                tabControl_Doc_Control.Visible = true;
                finalSubmitbtn.Enabled = true;
                DocTimerbtn.Visible = true;

                //DR - forces the selected index change event to fire
                //tabPage_Click(null, null);
                for (int i = 1; i < tabControl_Doc_Control.TabCount; i++)
                {
                        tabControl_Doc_Control.SelectedIndex = i;
                }
                tabControl_Doc_Control.SelectedIndex = 0;

                loanTimer.Start();
                LoadingCloseProgress();
            }
            catch (Exception ex)
            {
                loanTimer.Start();
                LoadingCloseProgress();

                var dialogResult = MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier 1
                    case 1:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                        }
                        break;
                    //DR - Tier 2
                    case 2:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                        }
                        break;
                    //DR - Tier 3
                    case 3:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                        }
                        break;
                }
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                resetControls();
                if (dialogResult == DialogResult.Retry)
                    goto RetryGetLoan;
            }
        }

        /// <summary>
        /// MA - Handles all cases of Form closing (eg User closed app, logging off)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_User_App_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //DR - stop the timer so an exception is not thrown on form close
                loanTimer.Stop();

                if (e.CloseReason == CloseReason.ApplicationExitCall) { Application.Exit(); }
                else
                {
                    //MA - Prompts user to verify closing form
                    var ExitResult = MessageBox.Show("Are You sure You Want To Close This Program? Your Data will not be Saved...", "Confirmation", MessageBoxButtons.YesNo);
                    if (ExitResult == DialogResult.No)
                    {
                        //DR - start the timer back up again if they decided not close the program
                        loanTimer.Start();
                        e.Cancel = true;
                    }
                    else
                    {
                        if (finalSubmitClicked == true) //DR - exit without removing userinput
                        {
                            //MA - Sets the active role to null
                            _AppEntity.UpdateActiveRole(_AppUser.Id, null);
                            
                            //MA - Sets the active server to null
                            _AppEntity.UpdateActiveServer(_AppUser.Id, "");

                            //MA - Sets the current session off
                            _AppEntity.UpdateSession(_AppUser.Id, false);

                            //MA - If the user is logging out, restart the application
                            if (e.CloseReason == CloseReason.UserClosing && this.isLoggingOut)
                            {
                                e.Cancel = true;
                                Application.Restart();
                            }

                            //MA - If the user is exiting the application, then terminate the application.
                            if (e.CloseReason == CloseReason.UserClosing && !this.isLoggingOut)
                            {
                                e.Cancel = true;
                                this.Dispose();
                                Application.Exit();
                            }
                        }
                        else //DR - remove userinput
                        {
                            switch (_AppUser.ActiveRole)
                            {
                                //DR - Tier 1
                                case 1:
                                    if (loanid > 0)
                                    {
                                        //DR - Adds the loan dropped action to the Histories table
                                        _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                                    }

                                    //MA - Determine current workflow
                                    switch (workflowID)
                                    {
                                        //MA - Removes Userinput if normal workflow
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;

                                        //MA - Removes Userinput if IMAVAL workflow
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                            break;
                                    }

                                    //MA - Sets the active role to null
                                    _AppEntity.UpdateActiveRole(_AppUser.Id, null);

                                    //MA - Sets the active server to null
                                    _AppEntity.UpdateActiveServer(_AppUser.Id, "");

                                    //MA - Sets the current session off
                                    _AppEntity.UpdateSession(_AppUser.Id, false);

                                    //MA - If the user is logging out, restart the application
                                    if (e.CloseReason == CloseReason.UserClosing && this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        Application.Restart();
                                    }

                                    //MA - If the user is exiting the application, then terminate the application.
                                    if (e.CloseReason == CloseReason.UserClosing && !this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        this.Dispose();
                                        Application.Exit();
                                    }
                                    break;
                                //DR - Tier 2
                                case 2:
                                    if (loanid > 0)
                                    {
                                        //DR - Adds the loan dropped action to the Histories table
                                        _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                                    }

                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                            break;
                                    }

                                    //MA - Sets the active role to null
                                    _AppEntity.UpdateActiveRole(_AppUser.Id, null);

                                    //MA - Sets the active server to null
                                    _AppEntity.UpdateActiveServer(_AppUser.Id, "");

                                    //MA - Sets the current session off
                                    _AppEntity.UpdateSession(_AppUser.Id, false);

                                    //MA - If the user is logging out, restart the application
                                    if (e.CloseReason == CloseReason.UserClosing && this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        Application.Restart();
                                    }

                                    //MA - If the user is exiting the application, then terminate the application.
                                    if (e.CloseReason == CloseReason.UserClosing && !this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        this.Dispose();
                                        Application.Exit();
                                    }

                                    break;
                                //DR - Tier 3
                                case 3:
                                    if (loanid > 0)
                                    {
                                        //DR - Adds the loan dropped action to the Histories table
                                        _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 8, workflowID);
                                    }
                                    switch (workflowID)
                                    {
                                        case 1:
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                            break;
                                    }

                                    //MA - Sets the active role to null
                                    _AppEntity.UpdateActiveRole(_AppUser.Id, null);

                                    //MA - Sets the active server to null
                                    _AppEntity.UpdateActiveServer(_AppUser.Id, "");

                                    //MA - Sets the current session off
                                    _AppEntity.UpdateSession(_AppUser.Id, false);

                                    //MA - If the user is logging out, restart the application
                                    if (e.CloseReason == CloseReason.UserClosing && this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        Application.Restart();
                                    }

                                    //MA - If the user is exiting the application, then terminate the application.
                                    if (e.CloseReason == CloseReason.UserClosing && !this.isLoggingOut)
                                    {
                                        e.Cancel = true;
                                        this.Dispose();
                                        Application.Exit();
                                    }
                                    break;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //DR - displays the available loans and active users, checks DB connection, checks session status
        private void LoanTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Invoke((MethodInvoker)(() => CheckDBConnection()));
                Invoke((MethodInvoker)(() => CheckSessionStatus()));
                CountGUI();
            }
            catch (Exception ex)
            {
                LogFile.LogFileWrite(LogFile.CreateErrorMessage(ex, "LoanID: " + loanid + " UserID: " + _AppUser.Id.ToString()));
                MessageBox.Show("An error occurred in the application. If the error persists, please contact your administrator. Error message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        //DR - increments the DocTicks
        private void DocTimer_Tick(object sender, EventArgs e, String docName)
        {
            DocTickDictionary[docName]++;
            lblDocTime.Text = DocTickDictionary[docName].ToString();
        }

        /// <summary>
        /// MA - Displays the warning message for the current tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewWarningStripStatusLabel_Click(object sender, EventArgs e)
        {
            MessageBox.Show(TabPageWarningPromptDictionary[tabControl_Doc_Control.SelectedTab.Text].ToString(), "Prompt Messages", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //DR - removes loan from the queue and flags it for manual review
        private void manualReviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(loanid == 0 | Loanbtn.Enabled == true)
            {
                //DR - return without doing anything since they don't have a loan yet
                MessageBox.Show("You can not flag a loan for Manual Review without being assigned a loan!", "No loan assigned",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }

            var dialogResult = MessageBox.Show("Are you sure you wish to flag this loan for Manual Review? If you click yes, this loan will be removed from the queue and cannot be re-added without Administrator action.","Manual Review Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (dialogResult == DialogResult.Yes)
            {
            RedoInput:
                string input = "";
                input = ShowDialog("You have flagged this loan for Manual Review. Please enter a comment explaining why: ", "Enter a reason for Manual Review");

                if (input == null)
                {
                    goto RedoInput;
                }
                else if (input.Trim() == "")
                {
                    goto RedoInput;
                }

                switch (_AppUser.ActiveRole)
                {
                    //DR - Tier 1
                    case 1:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 1);
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 2);
                                break;
                        }
                        break;
                    //DR - Tier 2
                    case 2:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 3);
                                break;
                        }
                        break;
                    //DR - Tier 3
                    case 3:
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 4);
                                break;
                        }
                        break;
                }

                //DR - add comment to table
                _AppEntity.InsertManualReviewComment(loanid, _AppUser.Id, input);
                //DR - Adds the Manual Review Opened action to the Histories table
                _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 13, workflowID);

                resetControls();
            }
        }

        #endregion EVENT_HANDLERS

       
    }
}