using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows.Forms;

namespace LogReader
{
    public partial class Form1 : Form
    {
        #region Parameters

        private string LogPath = ConfigurationSettings.AppSettings["LogPath"] ?? "C:\\Temp\\";
        private string currentFileName;
        List<LogInfo> logs = new List<LogInfo>();

        #endregion

        #region Functions

        public Form1()
        {
            Init();

            try
            {
                //Get the files from directory
                string[] extensions = new[] { ".txt", ".log" };
                FileInfo[] files = new DirectoryInfo(LogPath).GetFiles()
                                                                  .Where(p => extensions.Contains(p.Extension.ToLower()))
                                                                  .OrderByDescending(p => p.LastWriteTime)
                                                                  .ToArray();

                //Open the newest file
                OpenFile(files.First().FullName);

                //Fill SearchParam ComboBox
                cbSearchParam.Items.Clear();
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    cbSearchParam.Items.Add(column.Name);
                }
                cbSearchParam.SelectedIndex = 2;
            }
            catch
            {
                MessageBox.Show("Érvénytelen elérési út");
            }
        }

        private void Init()
        {
            InitializeComponent();

            //Init OpenFileDialog
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Log Files (.txt, .log)|*.txt;*.log|All Files (*.*)|*.*";

            //Get the files from directory
            string[] extensions = new[] { ".txt", ".log" };
            FileInfo[] files = new DirectoryInfo(LogPath).GetFiles()
                                                              .Where(p => extensions.Contains(p.Extension.ToLower()))
                                                              .OrderByDescending(p => p.LastWriteTime)
                                                              .ToArray();

            //Add filenames to ListBox
            foreach (FileInfo file in files)
            {
                filesListBox.Items.Add(Path.GetFileName(file.FullName));
            }

            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 142, 255);
        }

        private void Search()
        {
            List<LogInfo> templogs = new List<LogInfo>();
            if (!string.IsNullOrEmpty(tbSearchValue.Text))
            {
                int col = cbSearchParam.SelectedIndex;
                string val = tbSearchValue.Text;

                switch (col)
                {
                    case 0: templogs = new List<LogInfo>(logs.Where(p => p.Error.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 1: templogs = new List<LogInfo>(logs.Where(p => p.Time.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 2: templogs = new List<LogInfo>(logs.Where(p => p.ProcessId.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 3: templogs = new List<LogInfo>(logs.Where(p => p.Namespace.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 4: templogs = new List<LogInfo>(logs.Where(p => p.Message.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 5: templogs = new List<LogInfo>(logs.Where(p => p.Stacktrace.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 6: templogs = new List<LogInfo>(logs.Where(p => p.InnerExceptionMessage.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                    case 7: templogs = new List<LogInfo>(logs.Where(p => p.InnerExceptionStacktrace.Contains(val)).OrderBy(p => p.Time).ToList());
                        break;
                }
                LogCountValue.Text = string.Format("{0} ({1})", templogs.Count, logs.Count);
                ErrorCountValue.Text = string.Format("{0} ({1})", templogs.Where(p => p.Error.Equals("ERROR")).Count(), logs.Where(p => p.Error.Equals("ERROR")).Count());
            }
            else
            {
                templogs = new List<LogInfo>(logs.OrderBy(p => p.Time).ToList());
                LogCountValue.Text = logs.Count.ToString();
                ErrorCountValue.Text = logs.Where(p => p.Error.Equals("ERROR")).Count().ToString();
            }
            dataGridView1.DataSource = templogs;
            dataGridView1.Update();
        }

        private void OpenFile(string fullFileName)
        {
            try
            {
                logs.Clear();

                //Open the Log file for reading
                try
                {
                    TextReader reader = new StreamReader(File.OpenRead(fullFileName));
                    string file = reader.ReadToEnd();
                    if (file.StartsWith("<"))
                    {
                        string xmlString = string.Join("", new String[] { "<Log>", file, "</Log>" });
                        XmlDocument xml = new XmlDocument();

                        //Get nodes
                        xml.LoadXml(xmlString);
                        XmlNodeList list = xml.SelectNodes("Log/LogInfo");
                        XmlNodeList listerror = xml.SelectNodes("Log/LogErrorInfo");

                        //Process LogInfo
                        foreach (XmlNode node in list)
                        {
                            LogInfo log = new LogInfo();
                            log.Error = "OK";
                            log.Time = node.Attributes["Time"].Value;
                            log.ProcessId = node.Attributes["ProcessID"] != null ? node.Attributes["ProcessID"].Value : string.Empty;
                            log.Namespace = node.SelectSingleNode("Namespace") != null ? node.SelectSingleNode("Namespace").InnerText : string.Empty;
                            if (node.SelectSingleNode("Entry") != null)
                                log.Message = node.SelectSingleNode("Entry") != null ? node.SelectSingleNode("Entry").InnerText : string.Empty;
                            else
                                log.Message = node.SelectSingleNode("Message") != null ? node.SelectSingleNode("Message").InnerText : string.Empty;
                            log.Stacktrace = node.SelectSingleNode("Stacktrace") != null ? node.SelectSingleNode("Stacktrace").InnerText : string.Empty;
                            if (node.SelectSingleNode("InnerException") != null)
                            {
                                XmlNode iex = node.SelectSingleNode("InnerException");
                                log.InnerExceptionMessage = node.SelectSingleNode("Message") != null ? node.SelectSingleNode("Message").InnerText : string.Empty;
                                log.InnerExceptionStacktrace = node.SelectSingleNode("Stacktrace") != null ? node.SelectSingleNode("Stacktrace").InnerText : string.Empty;
                            }
                            if (log.Message.Contains("was thrown an Exception"))
                                log.Error = "ERROR";
                            logs.Add(log);
                        }

                        //Process LogErrorInfo
                        foreach (XmlNode node in listerror)
                        {
                            LogInfo log = new LogInfo();
                            log.Error = "ERROR";
                            log.Time = node.Attributes["Time"].Value;
                            log.ProcessId = node.Attributes["ProcessID"] != null ? node.Attributes["ProcessID"].Value : string.Empty;
                            log.Namespace = node.SelectSingleNode("Namespace") != null ? node.SelectSingleNode("Namespace").InnerText : string.Empty;
                            if (node.SelectSingleNode("Entry") != null)
                                log.Message = node.SelectSingleNode("Entry") != null ? node.SelectSingleNode("Entry").InnerText : string.Empty;
                            else
                                log.Message = node.SelectSingleNode("Message") != null ? node.SelectSingleNode("Message").InnerText : string.Empty;
                            log.Stacktrace = node.SelectSingleNode("Stacktrace") != null ? node.SelectSingleNode("Stacktrace").InnerText : string.Empty;
                            if (node.SelectSingleNode("InnerException") != null)
                            {
                                XmlNode iex = node.SelectSingleNode("InnerException");
                                log.InnerExceptionMessage = node.SelectSingleNode("Message") != null ? node.SelectSingleNode("Message").InnerText : string.Empty;
                                log.InnerExceptionStacktrace = node.SelectSingleNode("Stacktrace") != null ? node.SelectSingleNode("Stacktrace").InnerText : string.Empty;
                            }
                            logs.Add(log);
                        }

                        dataGridView1.DataSource = new List<LogInfo>(logs.OrderBy(p => p.Time).ToList());

                        //Sort log
                        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                        //Set Statusbar
                        LogCountValue.Text = (list.Count + listerror.Count).ToString();
                        ErrorCountValue.Text = logs.Where(p => p.Error.Equals("ERROR")).Count().ToString();

                        //Close log file
                        reader.Close();
                        currentFileName = fullFileName;
                        lblFileName.Text = Path.GetFileName(fullFileName);
                    }
                    else
                    {
                        MessageBox.Show("A fájl felépítése nem megfelelő. Valószínűleg nem XML fájl.");
                    }
                }
                catch (FileNotFoundException fex)
                {
                    MessageBox.Show("A fájl nem található");
                }
            }
            catch (NullReferenceException nrefex)
            {
                MessageBox.Show(nrefex.Message);
                MessageBox.Show(nrefex.StackTrace);
            }
        }

        private void SaveFile(string pFileName)
        {
            StreamWriter file = new StreamWriter(pFileName);

            foreach (LogInfo log in logs)
            {
                string xml = String.Empty;
                StringBuilder sbuilder = new StringBuilder();
                using (StringWriter sw = new StringWriter(sbuilder))
                {
                    using (XmlTextWriter w = new XmlTextWriter(sw))
                    {
                        w.Formatting = Formatting.Indented;
                        if (log.Error.Equals("ERROR")) {//ERROR

                            w.WriteStartElement("LogErrorInfo");
                            w.WriteAttributeString("Time", log.Time);
                            if (!string.IsNullOrEmpty(log.ProcessId)) w.WriteAttributeString("ProcessID", log.ProcessId);
                            w.WriteElementString("Namespace", log.Namespace);
                            w.WriteElementString("Name", log.Name);
                            w.WriteStartElement("Message");
                            w.WriteRaw(log.Message);
                            w.WriteEndElement();	//Message
                            w.WriteElementString("Stacktrace", log.Stacktrace);
                            if (log.InnerExceptionMessage != null)
                            {
                                w.WriteStartElement("InnerException");
                                w.WriteElementString("Message", log.InnerExceptionMessage);
                                w.WriteElementString("Stacktrace", log.InnerExceptionStacktrace);
                                w.WriteEndElement();  //InnerException
                            }
                            w.WriteEndElement();  //LogErrorInfo

                        }else {//OK

                            w.WriteStartElement("LogInfo");
                            w.WriteAttributeString("Time", log.Time);
                            if (!string.IsNullOrEmpty(log.ProcessId)) w.WriteAttributeString("ProcessID", log.ProcessId);
                            w.WriteStartElement("Message");
                            w.WriteRaw(log.Message);
                            w.WriteEndElement();	//Message
                            w.WriteEndElement();	//LogInfo

                        }
                    }
                }
                file.WriteLine(sbuilder.ToString());

            }
            file.Flush();
            file.Close();

            Init();
        }

        #endregion

        #region EventHandlers

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 240;
            dataGridView1.Height = this.Height - 105;
            filesListBox.Height = this.Height - 100;
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].Value != null && dataGridView1.Rows[i].Cells[0].Value.Equals("ERROR"))
                {
                    //Highlight errors
                    dataGridView1.Rows[i].ErrorText = "ERROR";
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255,200,200);

                }
            }
            dataGridView1.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
        }

        private void filesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (filesListBox.SelectedIndex != -1)
                OpenFile(LogPath + filesListBox.SelectedItem.ToString());
        }

        private void tbSearchValue_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
                Search();
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openFileDialog1.FileName);
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is simple log reader application for some xml structured log files, mainly for our project.\r\n" +
                            "The application was written in Visual Studio 2008 with .NET 3.5 by\r\npberezvay");
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(currentFileName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Log Files (.txt, .log)|*.txt;*.log|All Files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SaveFile(saveFileDialog1.FileName);
            }
        }

        #endregion
    }

    public class LogInfo
    {
        #region LogInfo

        //LogNodes for LogInfo and LogErrorInfo
        public string Error { get; set; }
        public string Time { get; set; }
        public string ProcessId { get; set; }
        public string Namespace { get; set; }
        public string Message { get; set; }
        public string Name { get; set; }
        public string Stacktrace { get; set; }
        public string InnerExceptionMessage { get; set; }
        public string InnerExceptionStacktrace { get; set; }

        #endregion
    }


}
