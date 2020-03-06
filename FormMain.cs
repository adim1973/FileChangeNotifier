using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
//using Access = Microsoft.Office.Interop.Access;


namespace FileChangeNotifier
{
    public partial class frmNotifier : Form
    {
        private bool m_bIsProcessing;
        private delegate void dgAddToList(string Message);
        bool DoClose;
        DataTable results = new DataTable();
        string fileNo = "";
        string dbString = "";
        string connString = "";
        //Access.Application oAccess = null;
        object oAccess = null;

        #region frmNotifier
        public frmNotifier()
        {
            InitializeComponent();
            m_bIsProcessing = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_SHOWME)
            {
                ShowMe();
            }
            base.WndProc(ref m);
        }

        private void ShowMe()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            // get our current "TopMost" value (ours will always be false though)
            bool top = TopMost;
            // make our form jump to the top of everything
            TopMost = true;
            // set it back to whatever it was
            TopMost = top;
            Visible = true;
        }

        private void frmNotifier_Load(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\database.txt"))
            {
                StreamReader file = new StreamReader(Application.StartupPath + "\\database.txt");
                txtFile.Text = file.ReadLine();
                dbString = txtFile.Text;
                txtNo.Text = file.ReadLine();
                fileNo = txtNo.Text;
                connString = "Provider=Microsoft.ACE.OLEDB.12.0;data source=" + dbString;
                file.Close();
            }
        }

        private void frmNotifier_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (!DoClose)
            {
                e.Cancel = true;
                Visible = false;
                WindowState = FormWindowState.Minimized;
            }
        }

        public void frmNotifier_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            NotifyIcon1.Dispose();
        }
        #endregion

        #region menus
        private void mnuShow_Click(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
        }

        private void mnuHide_Click(object sender, EventArgs e)
        {
            Visible = false;
            WindowState = FormWindowState.Minimized;
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                Visible = false;
                WindowState = FormWindowState.Minimized;
            }
            else
            {
                Visible = true;
                WindowState = FormWindowState.Normal;
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            DoClose = true;
            Close();
        }
        #endregion

        #region buttons
        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            DialogResult resDialog = dlgOpenFile.ShowDialog();
            if (resDialog.ToString() == "OK")
            {
                txtFile.Text = dlgOpenFile.FileName;
                dbString = txtFile.Text;
                fileNo = txtNo.Text;
                //connString = "Provider=Microsoft.ACE.OLEDB.12.0;data source=" + dbString;
                File.Delete(Application.StartupPath + "\\database.txt");
                StreamWriter file = new StreamWriter(Application.StartupPath + "\\database.txt");
                file.WriteLine(txtFile.Text);
                file.WriteLine(txtNo.Text);
                file.Close();
            }
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            dbString = txtFile.Text;
            fileNo = txtNo.Text;
            File.Delete(Application.StartupPath + "\\database.txt");
            StreamWriter file = new StreamWriter(Application.StartupPath + "\\database.txt");
            file.WriteLine(txtFile.Text);
            file.WriteLine(txtNo.Text);
            file.Close();
        }

        private void btnWatchFile_Click(object sender, EventArgs e)
        {
            if (m_bIsProcessing)
            {
                m_bIsProcessing = false;
                btnWatchFile.BackColor = Color.LightSkyBlue;
                btnWatchFile.Text = "Start Watching";
            }
            else
            {
                if (string.IsNullOrEmpty( txtFile.Text))
                {
                    MessageBox.Show("Choose the path to the database!");
                    return;
                }
                if (!File.Exists(txtFile.Text))
                {
                    MessageBox.Show("Specified database doesn't exist!");
                    return;
                }
                //GetAccessTable();
                GetCSVTable();
                m_bIsProcessing = true;
                btnWatchFile.BackColor = Color.Red;
                btnWatchFile.Text = "Stop Watching";
            }
            timer1.Enabled = m_bIsProcessing;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lstNotification.Items.Clear();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            DoClose = true;
            Close();
        }
        #endregion

        #region database
        private void GetAccessTable()
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                OleDbCommand cmd = new OleDbCommand("SELECT * FROM Files_Folders", conn);
                conn.Open();
                OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
                adapter.Fill(results);
                conn.Close();
            }
        }

        private void GetCSVTable()
        {
            var filename = Application.StartupPath + @"\files_folders.csv";
            var connString = string.Format(@"Provider=Microsoft.Jet.OleDb.4.0; Data Source={0};Extended Properties=""Text;HDR=YES;FMT=Delimited""", Path.GetDirectoryName(filename));
            using (var conn = new OleDbConnection(connString))
            {
                conn.Open();
                var query = "SELECT * FROM [" + Path.GetFileName(filename) + "]";
                using (var adapter = new OleDbDataAdapter(query, conn))
                {
                    var ds = new DataSet("CSV File");
                    adapter.Fill(results);
                }
            }
        }
        #endregion

        #region files processing
        private void ProcesssFiles()
        {
            try
            {
                timer1.Enabled = false;
                foreach (DataRow row in results.Rows)
                {
                    string Originating_folder = row["Originating_folder"].ToString();
                    if (Directory.Exists(Originating_folder))
                    {
                        string[] files = Directory.GetFiles(Originating_folder, row["File_name"].ToString(), SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            foreach (string strFile in files)
                            {
                                //exception for Ercot files
                                if (row["File_name"].ToString().Contains("ERCOT"))
                                {
                                    if (strFile.Contains("Small Comm"))
                                    {
                                        if (!row["File_name"].ToString().Contains("Small Comm"))
                                            break;
                                    }
                                }
                                //exception for Entrust files
                                if (row["File_name"].ToString().Contains("Entrust"))
                                {
                                    if (strFile.Contains("Index"))
                                    {
                                        if (!row["File_name"].ToString().Contains("Index"))
                                            break;
                                    }
                                }
                                //open database if closed
                                if (oAccess == null)
                                    OpenDB();
                                //process found file
                                string strOutputPath = row["Conversion_folder"].ToString();
                                lblMessage.Text = string.Format("Running code '{0}' on file '{1}'", row["Code_name"].ToString(), strFile);
                                bool retVal = RunFileMacro(Convert.ToInt16(row["ID"]), strFile, strOutputPath);
                                lblMessage.Text = "";
                                //move to Destination_folder
                                strOutputPath = Path.Combine(row["Destination_folder"].ToString(), Path.GetFileName(strFile));
                                if (!string.IsNullOrEmpty(row["Destination_folder"].ToString()))
                                {
                                    File.Delete(strOutputPath);
                                    File.Move(strFile, strOutputPath);
                                }
                                if (retVal)
                                    AddToList(string.Format("ERROR processing file: '{0}', using code '{1}'", strFile, row["Code_name"].ToString()));
                                else
                                    AddToList(string.Format("File processed: '{0}', using code '{1}'", strFile, row["Code_name"].ToString()));
                            }
                        }
                    }
                    else
                        AddToList(string.Format("Folder '{0}' does not exists!", Originating_folder));
                }
            }
            catch (Exception ex)
            {
                AddToList("ERROR in program: " + ex.Message);
            }
            finally
            {
                if (oAccess != null)
                    CloseDB();
                timer1.Enabled = true;
            }
        }

        private void OpenDB()
        {
            // Create an instance of Microsoft Access, make it visible and open database.
            //oAccess = new Access.Application();
            //oAccess.Visible = true;
            //oAccess.OpenCurrentDatabase(dbString, false, "");
            oAccess = Activator.CreateInstance(Type.GetTypeFromProgID("Access.Application"));
            object[] Parameters = new object[1];
            Parameters[0] = true;
            oAccess.GetType().InvokeMember("Visible", System.Reflection.BindingFlags.SetProperty,
            null, oAccess, Parameters);
            oAccess.GetType().InvokeMember(
                   "OpenCurrentDatabase",
                   System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod,
                   null,
                   oAccess,
                   new object[] { @dbString });
        }

        private void CloseDB()
        {
            // Quit Access and clean up.
            //oAccess.DoCmd.Quit(Access.AcQuitOption.acQuitSaveNone);
            oAccess.GetType().InvokeMember(
                   "CloseCurrentDatabase",
                   System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod,
                   null,
                   oAccess,
                   null);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(oAccess);
            oAccess = null;
        }

        private bool RunFileMacro(int fileType, string inputFile, string outputFile)
        {
            // Run the macro
            object oMissing = System.Reflection.Missing.Value;
            return RunMacro(oAccess, new object[] { "ConvertPriceFile", fileType, outputFile, fileNo, inputFile, oMissing });
            //return RunMacro(oAccess, new object[] { "ConvertPriceFile", fileType, outputFile, inputFile, outputFile });
        }

        private bool RunMacro(object oApp, object[] oRunArgs)
        {
            return (bool)oApp.GetType().InvokeMember("Run", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod, null, oApp, oRunArgs);
        }
        #endregion

        #region list
        private void AddToList(string Message)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new dgAddToList(AddToList), new object[] { Message });
            else
            {
                if (lstNotification.Items.Count > 10000)
                    lstNotification.Items.Clear();
                lstNotification.BeginUpdate();
                lstNotification.Items.Add(string.Format("{0:yyyy.MM.dd HH:mm:ss} - {1}", DateTime.Now, Message));
                lstNotification.EndUpdate();
            }
        }
        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            ProcesssFiles();
        }
    }
}