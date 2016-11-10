using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoanReview
{
    public partial class Form_User_App_4 : Form
    {
        # region Private_Members
        private vUser _AppUser { get; set; }
        private LoanReviewEntities _AppEntity { get; set; }
        private Dictionary<String, TabPage> uncheckedDictionary; //DR- list of docs which haven't had one checkbox per row selected
        private Dictionary<String, TabPage> checkedDictionary; //DR -  list of docs which have had one checkbox per row selected
        private Dictionary<String, TabPage> shortcutDictionary; //DR - list of shortcuts with their docs
        private Dictionary<int, int> fieldCountDictionary; //DR - list of field counts per document
        private int loanid; //MA - Stores the Loan ID
        private int loanNum; //MA - Stores the Loan Number
        private int workflowID = 1; //MA - determines the workflow for userinputs
        private int loanCompletedCount; //DR- counts the amount of loans completed for the session
        private bool nonNumberEntered = false; //DR - used for number value textboxes
        private System.Timers.Timer loanTimer; //DR - to run a check on loan availability every X amount in time
        private bool isLoggingOut = false;
        private bool finalSubmitClicked = false; //DR - to check if a loan was submitted
        private bool isTerminated = false; //DR - gets set to true when application will terminate
        private SQLiteConnection dbConnection;
        private SQLiteCommand command; //MA - References a query used on the local database
        private Form_Progress_Loading objfrmShowProgress;  //MA- Used to reference the loading form
        private ToolTip toolTips; //DR - holds tooltips
        private Control _currentToolTipControl = null;
        private bool checkDBConnection = false; //MA - used for CheckDBConnection
        private LocalDatabase LocalDatabase; //MA- Used to reference the local sqlite database
        #endregion

        # region Constructors
        public Form_User_App_4(vUser userLoginObject, LoanReviewEntities entityLoginObject)
        {
            //MA - Stores the User information from the login.
            _AppUser = userLoginObject;

            //MA - References and stores the tables, views and stored procedures for Imaging Apps database
            _AppEntity = entityLoginObject;
            InitializeComponent();
        }
        #endregion

        #region Methods

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


        //AO made for faster debugging. Selects MSP checkboxes only
        private void selectAllbtn_Clicked(object sender, System.EventArgs e, TabPage currentTab, int docId)
        {
            
            this.Refresh();
            LoadingStartProgress("Checking all");

            TableLayoutPanel tablePanel = new TableLayoutPanel();
            foreach (var current in currentTab.Controls.OfType<TableLayoutPanel>())
            {
                tablePanel = (TableLayoutPanel)current;
                foreach (CheckBox chkbx in tablePanel.Controls.OfType<CheckBox>())
                {

                    chkbx.CheckState = CheckState.Checked;

                }
            }
            LoadingCloseProgress();
        }

        private string GetMask(string type)
        {
            switch (type.ToLower())
            {
                case "triple":
                    return "######.###";

                case "double":
                    return "######.##";

                case "int":
                    return "######";

                case "phone":
                    return "(000)-000-0000";

                case "currency":
                    return "$#######.##";

                case "date":
                    return "00/00/0000";

                case "ssn":
                    return "000-00-0000";

                default:
                    return "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
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

        /// <summary>
        /// MA - Creates a tab that will display the all the fields for each document
        /// that will require an update on MSP
        /// </summary>
        /// <returns></returns>
        private bool TryPopulateDocumentTabs()
        {
        RetryPopulateDocumentTabs:
            try
            {
                uncheckedDictionary = new Dictionary<string, TabPage>();//DR - creates a fresh dictionary for our uncheckeddocs
                checkedDictionary = new Dictionary<string, TabPage>();//DR - creates a fresh dictionary for our checkeddocs
                fieldCountDictionary = new Dictionary<int, int>();//DR - creates a fresh dictionary for our fieldcounts
                toolTips = new ToolTip();

                //MA - Gets all the documents that contain an mismatching value from tier 2
                var subquery = (from doc in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                where doc.LoanID == loanid
                                && doc.isMatching == false
                                && doc.Reportable == true
                                && doc.InputID == 3
                                && doc.Workflow == workflowID
                                select doc.DocID).ToList();

                //MA - Gets all the fields from tier 2 that were mismatching in each document in the subquery, for every document
                var fieldQuery = (from fields in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                  where fields.LoanID == loanid
                                  && fields.isMatching == false
                                  && fields.Reportable == true
                                  && fields.InputID == 3
                                  && fields.Workflow == workflowID
                                  && subquery.Contains(fields.DocID)
                                  select fields).OrderBy(x => x.MspWorkstation).ToList();

                //MA - Creates a tab page
                TabPage tabPage = new TabPage()
                {
                    Text = "Update Fields",
                    //ToolTipText = doc.Description
                    AutoScroll = true,
                };

                //MA - Add the tab page to the tab control
                tabControl_Doc_Control.Controls.Add(tabPage);

                //MA -Create a table layout panel
                ModifiedTablePanel tablePanel = new ModifiedTablePanel()
                {
                    Name = tabPage.Name + "_panel_",
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Enabled = true,
                    
                };

                //MA - Prevents the table layout panel from drawing
                tablePanel.SuspendLayout();

                //MA - Add the table layout to the tabpage
                tabPage.Controls.Add(tablePanel);

                //MA - Create static controls 
                Label CopyLabel = new Label()
                {
                    Name = tablePanel.Name + "_Copylabel_",
                    Text = "Copy to Clipboard",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    Enabled = true
                };

                Label CompletedLabel = new Label()
                {
                    Name = tablePanel.Name + "_Completedlabel_",
                    Text = "Completed?",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    Enabled = true
                };

                Label MSPLabel = new Label()
                {
                    Name = tablePanel.Name + "_MSPlabel_",
                    Text = "New MSP Value",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    Enabled = true
                };

                Label MSPScreenLabel = new Label()
                {
                    Name = tablePanel.Name + "_MSPlabel_",
                    Text = "MSP Screen",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0),
                    Dock = DockStyle.Left,
                    Enabled = true
                };

                //MA - Add the static controls to the table layout panel
                tablePanel.Controls.Add(CompletedLabel, 1, 0);
                tablePanel.Controls.Add(CopyLabel, 2, 0);
                tablePanel.Controls.Add(MSPLabel, 4, 0);
                tablePanel.Controls.Add(MSPScreenLabel, 5, 0);

                //MA - Sets the row to start inserting field controls in the table layout panel
                int row = 2;

              //MA - Restarts the field population
              RetryPopulateDocumentFields:
                try
                {
                    //MA - Adds an starts the count of fields in the tab
                    this.fieldCountDictionary.Add(1, 0);

                    //MA - Iterates through each field
                    foreach (var field in fieldQuery)
                    {
                        //MA - will store the value of the field
                        string val = "";
                        bool CriticalField = false;

                        //MA - Check if the field is critical
                        if (field.IsCritical == true)
                        {
                            //MA - If field critical, then grab value from tier 3
                            var FieldValue = (from vDF in _AppEntity.vDocuments_Fields_UserInputs.AsNoTracking()
                                              where vDF.LoanID == loanid &&
                                              vDF.InputID == 4 &&
                                              vDF.DocID == field.DocID &&
                                              vDF.FieldID == field.FieldID
                                              && vDF.Workflow == workflowID
                                              select vDF);

                            //DR - use tier 2 value if tier 3 value doesn't exist
                            if (FieldValue.Count() == 0)
                            {
                                if (row == 1)
                                {
                                    row = 2;
                                }

                                val = field.Value;
                            }
                            else
                            {
                                //DR - if tier 3 value is matching, the field doesn't need to be changed in msp so skip it
                                if (FieldValue.Single().isMatching == true)
                                {
                                    continue;
                                }

                                //MA - Stores the value of the field
                                val = FieldValue.Single().Value;
                                CriticalField = true;
                            }
                            
                        }
                        else
                        {
                            if (row == 1)
                            {
                                row = 2;
                            }
                            
                            val = field.Value;
                        }

                        //DR - Gets the information to Transform the data to something readable by the user
                        var DataTranslation = (from DT in _AppEntity.vDataTranslations.AsNoTracking()
                                               where DT.DocID == field.DocID &&
                                               DT.FieldID == field.FieldID
                                               select DT);

                        //DR - if the field has any DataTranslation, it's value may need to be translated
                        if (DataTranslation.Count() > 0)
                        {
                            foreach (var DT in DataTranslation)
                            {
                                //DR - if fieldvalue matches pretranslation, translate it into its actual MSPvalue
                                if (DT.PreTranslation.Trim().Equals(val.Trim()))
                                {
                                    val = DT.PostTranslation;
                                }
                            }
                        }

                        //DR - remove the legends from combobox values
                        vField vField = (from f in _AppEntity.vFields.AsNoTracking() where f.Id == field.FieldID select f).Single();
                        if(vField.Client_Data_Type.Trim().ToLower().Equals("combobox"))
                        {
                            val = val.Split('|')[0];
                        }

                        //MA- Create a label for the field
                        Label fieldLabel = new Label()
                        {
                            Name = tablePanel.Name + "_label_" + field.FieldName,
                            Text = RemoveCamelCasing(field.FieldName) + @": ",
                            AutoSize = true,
                            Padding = new Padding(0, 6, 0, 0),
                            Dock = DockStyle.Left,
                            Height = 20
                        };

                        //MA - Creates a textbox to display the value of the field
                        TextBox MSPTextbox = new TextBox()
                        {
                            Name = field.DocID + "_MSPtextBox_" + field.FieldID,
                            Visible = true,
                            BackColor = Color.White,
                            Enabled = false,
                            Dock = DockStyle.Left,
                            Text = val,
                            Width = 150,
                        };

                        //MA - Creates a textbox to display the MSP screens of the field
                        TextBox MSPscreenTextbox = new TextBox()
                        {
                            Name = field.DocID + "_MSPscreentextBox_" + field.FieldID,
                            Visible = true,
                            BackColor = Color.White,
                            Enabled = false,
                            Dock = DockStyle.Left,
                            Text = field.MspWorkstation,
                            Width = 150,
                        };

                        //MA - Creates a checkbox to allow user to check when the field has been updated
                        CheckBox MSPCheckBox = new CheckBox()
                        {
                            Name = tablePanel.Name + "_MSPcheckbox_" + field.FieldName,
                            Visible = true,
                            Enabled = false,
                            AutoSize = true,
                            Anchor = AnchorStyles.Right,
                            //Dock = DockStyle.Left,
                            Padding = new Padding(0, 3, 0, 0),
                            BackColor = Color.Transparent
                        };
                                                
                        //DR - button to copy the new MSP value to clipboard
                        Button btnCopy = new Button()
                        {
                            Name = tablePanel.Name + "_btnCopy_" + field.FieldID,
                            Visible = true,
                            Enabled = true,
                            Anchor = AnchorStyles.Left,
                            AutoSize = true,
                            Text = "Copy MSP Value"
                        };

                        //MA - Assigns click event to the copy button
                        btnCopy.Click += (s, e) => btnCopy_Clicked(s, e, MSPCheckBox, MSPTextbox.Text);
                        //selectAllbtn.Click += new EventHandler((s, e) => selectAllbtn_Clicked(s, e, tabPage, doc.Id));//AO - event handler for button
                        
                        //MA - Adds the controls to the table layout panel
                        tablePanel.Controls.Add(MSPCheckBox, 1, row);
                        tablePanel.Controls.Add(btnCopy, 2, row);
                        tablePanel.Controls.Add(fieldLabel, 3, row);
                        tablePanel.Controls.Add(MSPTextbox, 4, row);
                        tablePanel.Controls.Add(MSPscreenTextbox, 5, row);
                        row++;

                        //DR - adds tooltips for the specified textboxes and labels
                        toolTips.SetToolTip(fieldLabel, field.FieldDescription);

                        //MA - Assigns click event to the checkbox
                        MSPCheckBox.CheckStateChanged += new EventHandler((s, e) => MSPCheckBox_CheckedStateChanged(s, e, tabPage, MSPCheckBox, MSPTextbox, field.DocID, field.FieldID));
                        
                        //MA - Loads the context menu for current field
                        LoadMenu(fieldLabel, field.DocID, field.FieldID, CriticalField);
                        
                        //MA - Increase the count of fields
                        fieldCountDictionary[1]++;

                    }

#if debug
 tablePanel.Controls.Add(selectAllbtn, 0, ++row);//AO adds select all checkboxes button to controls
#endif

                }
                catch (Exception ex)
                {
                    var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                    if (dialogResult == DialogResult.Retry)
                        goto RetryPopulateDocumentFields;

                    return false;
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

                //MA - Allow the drawing of the controls to continue
                tablePanel.ResumeLayout();
                tablePanel.MouseDown += new MouseEventHandler((s, e) => tablePanel_MouseDown(s, e, tablePanel));
            }
            catch (Exception ex)
            {
                var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (dialogResult == DialogResult.Retry)
                    goto RetryPopulateDocumentTabs;

                return false;
            }

            return true;
        }

        /// <summary>
        /// MA - Creates a context menu for labels depending on current tierMA - Creates a context menu for labels depending on current tier
        /// </summary>
        /// <param name="FieldControl"></param>
        /// <param name="documentId"></param>
        /// <param name="fid"></param>
        /// <param name="isCritical"></param>
        private void LoadMenu(Control FieldControl, int documentId, int fid, bool isCritical)
        {
            //MA - Creates a context menu
            ContextMenuStrip menu = new ContextMenuStrip();

            //MA - Create a sub menu for the context menu
            ToolStripMenuItem submenu = new ToolStripMenuItem();
            submenu.Text = "Selected Document";

            //MA - Gets the selected document in tier 1 from Userinput table in Imaging Apps
            var TierSelecteddocument = (from UI in _AppEntity.UserInputs.AsNoTracking()
                                        where UI.LoanID == loanid
                                        && UI.DocID == documentId
                                        && UI.FieldID == fid
                                        && UI.InputID == 2
                                        && UI.Workflow == workflowID
                                        select UI.SelectedDocument).Single();

            //MA - Create a sub menu for the context menu
            ToolStripMenuItem subitem = new ToolStripMenuItem();
            subitem.Text = "Tier 1: " + TierSelecteddocument.ToString();
            submenu.DropDownItems.Add(subitem);

            //MA - Gets the selected document in tier 2 from Userinput table in Imaging Apps
            var Tier2selecteddocument = (from UI in _AppEntity.UserInputs.AsNoTracking()
                                         where UI.LoanID == loanid
                                         && UI.DocID == documentId
                                         && UI.FieldID == fid
                                         && UI.InputID == 3
                                         && UI.Workflow == workflowID
                                         select UI.SelectedDocument).Single();

            //MA - Create a sub menu for the context menu
            ToolStripMenuItem subitem2 = new ToolStripMenuItem();
            subitem2.Text = "Tier 2: " + Tier2selecteddocument.ToString();
            submenu.DropDownItems.Add(subitem2);

            //MA - Check if the field is critical
            if (isCritical)
            {
                //MA - Gets the selected document in tier 3 from Userinput table in Imaging Apps
                var Tier3SelectedDocument = (from UI in _AppEntity.UserInputs.AsNoTracking()
                                             where UI.LoanID == loanid
                                             && UI.DocID == documentId
                                             && UI.FieldID == fid
                                             && UI.InputID == 4
                                             && UI.Workflow == workflowID
                                             select UI.SelectedDocument).Single();


                ToolStripMenuItem subitem3 = new ToolStripMenuItem();
                subitem3.Text = "Tier 3: " + Tier3SelectedDocument.ToString();
                submenu.DropDownItems.Add(subitem3);
            }

            //MA - Create a sub menu for the context menu
            ToolStripMenuItem submenu2 = new ToolStripMenuItem();
            submenu2.Text = "Information";

            //MA - Gets the Comment, Operator Note and Description for the field from Fields table in Imaging Apps
            var query = (from Fields in _AppEntity.Fields.AsNoTracking()
                         join DF in _AppEntity.Documents_Fields.AsNoTracking()
                         on Fields.Id equals DF.FieldId
                         join D in _AppEntity.Documents.AsNoTracking()
                         on DF.DocumentId equals D.Id
                         where D.Id == documentId && DF.FieldId == fid
                         select new { Fields }).ToList();

            foreach (var info in query)
            {
                //MA - If the statements are null then add them to the context menu
                if (info.Fields.Comment != null)
                {
                    //MA - Create a sub menu for the context menu
                    ToolStripMenuItem subitem4 = new ToolStripMenuItem();
                    subitem4.Text = "MSP Comment: " + info.Fields.Comment;
                    submenu2.DropDownItems.Add(subitem4);

                }

                if (info.Fields.OperatorNote != null)
                {
                    //MA - Create a sub menu for the context menu
                    ToolStripMenuItem subitem5 = new ToolStripMenuItem();
                    subitem5.Text = "MSP Operator Note: " + info.Fields.OperatorNote;
                    submenu2.DropDownItems.Add(subitem5);
                }

                if (info.Fields.Description != null)
                {
                    //MA - Create a sub menu for the context menu
                    ToolStripMenuItem subitem6 = new ToolStripMenuItem();
                    subitem6.Text = "MSP Description: " + info.Fields.Description;
                    submenu2.DropDownItems.Add(subitem6);
                }

            }

            //MA - Add the toolstrip to the submenu
            menu.Items.Add(submenu);
            menu.Items.Add(submenu2);

            //MA - Assign the context menu to the corresponding control
            FieldControl.ContextMenuStrip = menu;
            FieldControl.ContextMenuStrip.Opened += ((s, e) => ContextMenuStrip_Opened(s, e, FieldControl));
            FieldControl.ContextMenuStrip.Closed += ((s, e) => ContextMenuStrip_Closed(s, e, FieldControl));

        }

        /// <summary>
        /// MA - When the Context menu is closed the background color of the label will be default
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="FieldControl"></param>
        private void ContextMenuStrip_Closed(object s, ToolStripDropDownClosedEventArgs e, Control FieldControl)
        {
            //MA - set the backcolor of control to default
            FieldControl.BackColor = SystemColors.Control;
        }

        /// <summary>
        /// MA - When the Context menu is opened the background color of the label will be light blue
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="FieldControl"></param>
        private void ContextMenuStrip_Opened(object s, EventArgs e, Control FieldControl)
        {
            //MA - set the backcolor of control to light blue
            FieldControl.BackColor = Color.LightBlue;
        }

        //DR - event to copy the new MSP value to clipboard and enables the completed checkbox
        private void btnCopy_Clicked(object s, EventArgs e, CheckBox completed, string str)
        {
            if (str != "")
            {
                System.Windows.Forms.Clipboard.SetText(str);
            }
            //System.Windows.Forms.Clipboard.SetText(str);
            completed.Enabled = true;
        }


        #endregion Methods

        #region Events

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

        //DR - event handler for the tablelayoutpanel mousedown event. Displays the control's text
        private void tablePanel_MouseDown(object sender, MouseEventArgs e, TableLayoutPanel tablePanel)
        {
            Control control = tablePanel.GetChildAtPoint(e.Location);
            //DR - if control is not null
            if (control != null)
            {
                //DR - if control is disabled
                if (!control.Enabled)
                {

                    string controlText = control.Text.ToString();
                    //DR - if control's text is equal to empty string or null return without doing anything
                    if (controlText.Equals("") | controlText.Equals(null))
                    {
                        if (control.GetType().ToString() == "Textbox")
                        {
                            return;
                        }
                    }

                    if (!control.Name.Contains("_MSPscreentextBox_"))
                    {
                        if (Convert.ToInt32(ExtractID(control.Name, "Document")) == 17)
                        {
                            string fieldid = ExtractID(control.Name, "Control").ToString();
                            string ControlDateText = _AppEntity.GetControlDateForField(loanNum, fieldid, "Payment Line").Single();

                            if ((ControlDateText ?? "") != "")
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
                    }
                    //MessageBox.Show(controlText);

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
                    if (names.Count() == 3)
                    {
                        id = names[0].ToString();
                    }
                    if (names.Count() == 5)
                    {
                        id = "0";
                    }
                    break;
                case "Control":
                    if (names.Count() == 3)
                    {
                        id = names[2].ToString();
                    }

                    break;
            }

            return id;
        }

        //DR and MA - Loads Loan into Application, assigns User and allows for data entry
        private void Loanbtn_Click(object sender, EventArgs e)
        {
            //MA - Button to get loan
            Loanbtn.Enabled = false;

        RetryGetLoan:

            try
            {

                this.Refresh();

                var loanQuery = from temp in _AppEntity.LoanDatas where temp.OwnedByUserID == _AppUser.Id select temp.ID;

                //DR - if user is assigned to more than one loan, remove them from each
                if (loanQuery.Count() >= 1)
                {
                    foreach (int loan in loanQuery)
                    {
                        int lN = Convert.ToInt32(loan);
                        int tier = Convert.ToInt32((from L in _AppEntity.LoanDatas.AsNoTracking() where L.ID == lN select L.Tier).Single());
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
                this.loanNum = Convert.ToInt32((from L in _AppEntity.LoanDatas.AsNoTracking() where L.ID == loanid select L.LoanNumber).Single());
                lblLoanID.Text = loanid.ToString();
                lblLoanNumber.Text = this.loanNum.ToString();
                LoadingStartProgress("Loading Loan: " + this.loanNum.ToString());
                //DR - form closing knows to delete userinput values if they close the application   
                finalSubmitClicked = false;
                TryPopulateDocumentTabs();
                //TryLoadDBDictionary(loanid);

                tabControl_Doc_Control.Visible = true;
                tabControl_Doc_Control.Enabled = true;
                finalSubmitbtn.Enabled = true;

                //AO Sets focus to first txtbox of default tab
                TabPage tp = tabControl_Doc_Control.SelectedTab;
                bool isFirstTxtBx = true;
                if (tp != null)
                {
                    foreach (TableLayoutPanel t in tp.Controls.OfType<TableLayoutPanel>())
                    {
                        foreach (CheckBox m in t.Controls.OfType<CheckBox>())
                        {
                            if (isFirstTxtBx) { m.Focus(); }
                            isFirstTxtBx = false;
                        }
                    }
                }
                LocalDatabase = new LocalDatabase(_AppUser, _AppEntity, loanid, workflowID);
                LocalDatabase.LocalDBSetup();
                LoadingCloseProgress();
            }
            catch (Exception ex)
            {
                LoadingCloseProgress();
                var dialogResult = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                var UserRemovedStatus = (_AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 5));
                resetControls();
                if (dialogResult == DialogResult.Retry)
                    goto RetryGetLoan;
            }
        }

        /// <summary>
        /// MA - Resets all the controls in the form to accept new loan
        /// </summary>
        private void resetControls()
        {
            loanid = 0;
            this.loanNum = 0;
            lblSessionCompleted.Text = loanCompletedCount.ToString();
            LoanTimer_Tick(null, null);
            Loanbtn.Enabled = true;
            lblLoanID.Text = "";
            tabControl_Doc_Control.Visible = false;
            finalSubmitbtn.Enabled = false;
            tabControl_Doc_Control.Controls.Clear();
            //listBox_DocumentNav_Checked.Items.Clear();
            //listBox_DocumentNav_Unchecked.Items.Clear();
            WorkflowComboBox.Enabled = true;
            //DR - this line must come before closing the DB connection
            //DeleteLocalDBUserInput();
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection.Dispose();
            }
            shortcutDictionary.Clear();
            GC.Collect();
        }

        //DR - checks to see if there are available loans for the user
        private bool LoanAvailabilityCheck()
        {
            int count = 0;
            switch (workflowID)
            {
                case 1:
                    count = (from LD in _AppEntity.LoanDatas.AsNoTracking()
                             where
                                 (LD.Tier == _AppUser.ActiveRole) && (LD.NormalWorkflowCompleted == false) && (LD.OwnedByUserID == null)
                             select LD.ID).Count();
                    break;
                case 2:
                    count = (from WT in _AppEntity.vWorkflowTracking_Loandata.AsNoTracking()
                             where (WT.TaskStatus == 2)
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
        ///  MA - Logs the user of the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logoutBtn_Click(object sender, EventArgs e)
        {
            this.isLoggingOut = true;
            this.Close();
            this.isLoggingOut = false;
        }

       
        private void Form_User_App_4_Load(object sender, EventArgs e)
        {
            tabControl_Doc_Control.Enabled = false;
            shortcutDictionary = new Dictionary<String, TabPage>();//DR - creates a fresh dictionary for our tabpage shortcuts
            userID.Text = _AppUser.Name;
            lblUserID.Text = _AppUser.Id.ToString();
            lblRole.Text = _AppUser.ActiveRole.ToString();
            InitTimer();
            GetLastLogin();
            toolStripSplitButton1.Image = LoanReview.Properties.Resources.Connection_Good;
            toolStripServerName.Text = Environment.MachineName;

            //MA - Implements dynamic labels of shorcut keys for tabs on the right panel
            List<string> shortcuts = new List<string>();
            var skeys = (from doc in _AppEntity.Documents.AsNoTracking() select new { doc.Name, doc.ShortcutKey }).ToList();
            StringBuilder shortcutval = new StringBuilder();
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
                Label shortcutLabel = new Label()
                {
                    Text = d.Name + " : " + shortcutval,
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 15, 0, 0),
                };
                shortcutGB.Controls.Add(shortcutLabel);
                shortcutval.Clear();
            }
            WorkflowComboBox.SelectedIndex = 0;
        }


        private void GetLastLogin() //DR- gets the last login of the user
        {
            int id = _AppUser.Id;
            var query = from user in _AppEntity.Users where user.Id == id select user;

            foreach (var user in query)
            {
                toolStripStatusLogin.Text = user.LastLogin.ToString();
            }
        }


        public void InitTimer() //DR - initilializes the loan timer
        {
            loanTimer = new System.Timers.Timer(15000);
            loanTimer.Elapsed += LoanTimer_Tick;
            loanTimer.Start();
            LoanTimer_Tick(null, null);
        }

        /// <summary>
        /// MA - Displays the available loans and active users, checks DB connection, checks session status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoanTimer_Tick(object sender, EventArgs e) //DR - displays the available loans and active users
        {
            Invoke((MethodInvoker)(() => CheckDBConnection()));
            Invoke((MethodInvoker)(() => CheckSessionStatus()));
            CountGUI();
        }

        //DR - Sets tier counts and role counts for GUI labels
        private void CountGUI()
        {
            int tier1Count = 0;
            int tier2Count = 0;
            int tier3Count = 0;
            int tier4Count = 0;

            int role1Count = 0;
            int role2Count = 0;
            int role3Count = 0;
            int role4Count = 0;

            //MA - Determine which workflow the user is on
            switch (workflowID)
            {
                case 1:

                    //MA - Gets the amount of loan in normal workflow
                    var tq = (from loan in _AppEntity.LoanDatas
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
                        else
                            tier4Count = count.Count;


                    }

                    break;
            }

            //MA - Gets the amount of users logged in into the 
            var roleQuery = from users in _AppEntity.vUsers.AsNoTracking() where users.IsActive == true group users by users.ActiveRole;

            var userQuery = from users in roleQuery select users;

            //MA - Stores the amount of users in each tier
            foreach (var user in userQuery)
            {
                if (user.Key == 1)
                {
                    role1Count = user.Count();
                }
                else if (user.Key == 2)
                {
                    role2Count = user.Count();
                }
                else if (user.Key == 3)
                {
                    role3Count = user.Count();
                }
                else
                    role4Count = user.Count();

            }

            //MA - Forces the labels for the amounts to change despite the thread currently active
            Invoke((MethodInvoker)(() => ChangeGUI(tier1Count, tier2Count, tier3Count, tier4Count, role1Count, role2Count, role3Count)));
        }

        /// <summary>
        /// MA - Sets the values for the labels indicating the amount 
        /// loans in each tier for each workflow as well as the amount
        /// of users currently logged on each tier
        /// </summary>
        /// <param name="tier1Count"></param>
        /// <param name="tier2Count"></param>
        /// <param name="tier3Count"></param>
        /// <param name="tier4Count"></param>
        /// <param name="role1Count"></param>
        /// <param name="role2Count"></param>
        /// <param name="role3Count"></param>
        private void ChangeGUI(int tier1Count, int tier2Count, int tier3Count, int tier4Count, int role1Count, int role2Count, int role3Count)
        {
            if (workflowID == 2)
            {
                Tier1Label.Text = "IVT Tier I:";
                Tier2Label.Text = "IVT Tier II:";
                Tier3Label.Text = "IVT Tier III:";
                Tier4Label.Text = "IVT Tier IV:";
            }
            else
            {
                Tier1Label.Text = "Tier I:";
                Tier2Label.Text = "Tier II:";
                Tier3Label.Text = "Tier III:";
                Tier4Label.Text = "Tier IV:";
            }
            lblTierI.Text = tier1Count.ToString();
            lblTierII.Text = tier2Count.ToString();
            lblTierIII.Text = tier3Count.ToString();
            lblTierIIII.Text = tier4Count.ToString();

            if (tier1Count == 0) { lblTierI.ForeColor = Color.Red; } else lblTierI.ForeColor = Color.Black;
            lblTierII.Text = tier2Count.ToString();
            if (tier2Count == 0) { lblTierII.ForeColor = Color.Red; } else lblTierII.ForeColor = Color.Black;
            lblTierIII.Text = tier3Count.ToString();
            if (tier3Count == 0) { lblTierIII.ForeColor = Color.Red; } else lblTierIII.ForeColor = Color.Black;
            lblTierIIII.Text = tier4Count.ToString();
            if (tier4Count == 0) { lblTierIIII.ForeColor = Color.Red; } else lblTierIIII.ForeColor = Color.Black;

            lblRoleI.Text = role1Count.ToString();
            lblRoleII.Text = role2Count.ToString();
            lblRoleIII.Text = role3Count.ToString();
        }


        /// <summary>
        /// MA - checks the session status of the user
        /// </summary>
        private void CheckSessionStatus()
        {
            //MA - Check if the server is active on a server
            var Server = (from Users in _AppEntity.Users.AsNoTracking()
                          where Users.Id == _AppUser.Id
                          select new { Users.ActiveServer, Users.IsActive }).Single();

            //MA - If the active server doesnt match the current server or the session is
            //inactive then log the user off
            if (Server.IsActive == false | Server.ActiveServer != Environment.MachineName)
            {
                //DR - if statement is required so terminated messagebox only appears once
                if (!isTerminated)
                {
                    isTerminated = true;

                    if (loanid > 0)
                    {
                        switch (workflowID)
                        {
                            case 1:
                                _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 5);
                                break;
                            case 2:
                                _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 5);
                                break;
                        }

                    }
                    //MessageBox.Show("Your Session has been terminated. This Application will be closed.", "Session Terminated", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    Application.Exit();
                }


            }

        }

        /// <summary>
        ///MA - pinghost returns true if server is pingable. returns false for anything else
        /// </summary>
        /// <param name="nameOrAddress"></param>
        /// <returns></returns>
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
                //MA - Discard PingExceptions and return false;
                return false;
            }
            return pingable;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CheckDBConnection()
        {
            if (!PingHost("10.10.53.19"))
            {

                //DR - checkDBconnection variable is always false so it will run once
                if (!checkDBConnection)
                {
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
            

        }

        
        /// <summary>
        /// MA - Handles all cases of Form closing (eg User closed app, logging off)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_User_App_4_FormClosing(object sender, FormClosingEventArgs e)
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
                                //DR - Tier 4
                                case 4:
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
                                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 5);
                                            break;

                                        //MA - Removes Userinput if IMAVAL workflow    
                                        case 2:
                                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 5);
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

        //DR - store or remove userinput depending on the state of the checkbox
        private void MSPCheckBox_CheckedStateChanged(object sender, System.EventArgs e, TabPage currentTab, CheckBox itself, TextBox textBox, int docID, int fID)
        {
            if (itself.CheckState == CheckState.Checked)
            {
                LocalDatabase.LocalSaveUserInput(loanid, _AppUser.Id, docID, fID, textBox.Text.ToString(), 5, 1, "", workflowID, "", 1);

                string sql = "select count(DocID) from UserInputTable where DocID = '" + docID.ToString() + "'";
                command = new SQLiteCommand(sql, LocalDatabase.dbConnection);
                //DR - gets the count of userinputs for a document
                int count = int.Parse(command.ExecuteScalar().ToString());
                //DR - if the userinput count matches the fieldCount for a document, move the document to the checkedDictionary
                if (fieldCountDictionary[1] == count)
                {
                    if (uncheckedDictionary.ContainsKey(currentTab.Text.ToString()))
                    {
                        //DR - add to checked and remove from unchecked list
                        checkedDictionary.Add(currentTab.Text.ToString(), uncheckedDictionary[currentTab.Text.ToString()]);
                        uncheckedDictionary.Remove(currentTab.Text.ToString());
                        //LoadListDictionaries();
                    }
                }
            }
            else
            {
                LocalDatabase.LocalRemoveUserInput(loanid, _AppUser.Id, docID, fID, 5, workflowID);

                if (checkedDictionary.ContainsKey(currentTab.Text.ToString()))
                {
                    //DR - add to unchecked and remove from checked list
                    uncheckedDictionary.Add(currentTab.Text.ToString(), checkedDictionary[currentTab.Text.ToString()]);
                    checkedDictionary.Remove(currentTab.Text.ToString());
                    //LoadListDictionaries();
                }
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


        /// <summary>
        /// MA - Logs the user out of the application by using the menu strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loanid == 0)
            {
                this.Close();
            }
            else
                logoutBtn_Click(sender, e);
        }

        /// <summary>
        /// MA - Exits the user out of the application by using the menu strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FormClosingEventArgs e2 = new FormClosingEventArgs(CloseReason.UserClosing, false);
            Form_User_App_4_FormClosing(null, e2);
        }

        //DR - checks to see if the keydata(our shortcuts) is found in the shortcutDictionary, if so it selects the tab
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (shortcutDictionary.ContainsKey(keyData.ToString()))
            {
                tabControl_Doc_Control.SelectedTab = shortcutDictionary[keyData.ToString()];
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// MA - Submits the Users checkbox entries to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void finalSubmitbtn_Click(object sender, EventArgs e)
        {
            this.Refresh();
            LoadingStartProgress("Submitting");

            TableLayoutPanel tablePanel = new TableLayoutPanel();
            int checkBoxCheckedCount = 0;

            //MA - Iterate through each tab.
            foreach (var tp in tabControl_Doc_Control.Controls.OfType<TabPage>())
            {
                string DocumentKey = @tp.Text;

                //MA: Iterate through each Panel in tab.
                foreach (var Panel in tp.Controls.OfType<TableLayoutPanel>())
                {
                    tablePanel = (TableLayoutPanel)Panel;

                    //MA: Iterate through all checkboxes in a Panel.
                    foreach (var checkbox in tablePanel.Controls.OfType<CheckBox>())
                    {
                        //MA: Check whether the checkbox is checked.
                        if (checkbox.Checked == true)
                        {
                            checkBoxCheckedCount++;
                        }
                    }
                }
                            
                if (checkBoxCheckedCount != fieldCountDictionary[1])
                {
                    LoadingCloseProgress();
                    MessageBox.Show("All fields must be checked for each document.", "Submit Error", MessageBoxButtons.OK);
                    return;
                }
                checkBoxCheckedCount = 0;

            }

            //MA - Calls the custom dialog box for the user to enter the information for closing the task
            LoadingCloseProgress();
            string reval = Form_Task_DialogBox.Show(_AppEntity, loanNum, loanid, workflowID, _AppUser.Id);
            if (reval == "0")
            {
                return;
            }

            LoadingStartProgress("Submitting");
            LocalDatabase.TransferDataToLiveDB();

            //DR - a check to see if all inputs made it to liveDB
            if (!LocalDatabase.WasUserInputTransferSuccessful(5))//DR - transfer failed 1st time
            {
                //DR - remove userinputs from liveDB if there were any and try transfer again
                _AppEntity.RemoveUserInput(loanid, _AppUser.Id, 5, workflowID);
                LocalDatabase.TransferDataToLiveDB();
                if (!LocalDatabase.WasUserInputTransferSuccessful(5)) //DR - transfer failed 2nd time remove userinput again if there were any
                {
                    switch (workflowID)
                    {
                        case 1:
                            _AppEntity.RemoveLoanUser(loanid, _AppUser.Id, 5);
                            break;
                        case 2:
                            _AppEntity.IVTRemoveLoanUser(loanid, _AppUser.Id, 5);
                            break;
                    }
                    var email = (from Users in _AppEntity.vUsers
                                 where Users.Name == _AppUser.Name
                                 select Users.Email).Single();
                   
                    EmailMessage emailMessage = new EmailMessage(email.ToString(), "Loan Failed to Submit", " LoanID: " + loanid.ToString() + " UserName: " + _AppUser.Name + " UserID: " + _AppUser.Id.ToString() + " Tier: " + _AppUser.ActiveRole.ToString() + " Workflow: " + workflowID + " Time tried to submit: " + DateTime.Now.ToString());
                    LoadingCloseProgress();
                    MessageBox.Show("Loan failed to submit. An email was sent to your supervisor", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    resetControls();
                    return;
                }

            }

            switch (workflowID)
            {
                case 1:
                    _AppEntity.LoanEscalation(1, _AppUser.ActiveRole.Value, _AppUser.Id, loanid);
                    break;
                case 2:
                    _AppEntity.IVTLoanEscalation(loanid, _AppUser.Id, 2, _AppUser.ActiveRole, 1);
                    break;
            }
            //DR - Adds the loan submitted action to the Histories table
            _AppEntity.CreateHistoryEvent(_AppUser.Id, loanid, _AppUser.ActiveRole, 7, workflowID);

            LoadingCloseProgress();

            var t = MessageBox.Show("Loan Successfully Submitted!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            loanCompletedCount++;

            finalSubmitClicked = true;//DR - lets the formclosing know not to delete userinput

            resetControls();
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

        #endregion Events

        


    }
}

