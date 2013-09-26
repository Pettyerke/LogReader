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
        private string LogPath = ConfigurationSettings.AppSettings["LogPath"] ?? "C:\\Temp\\";
        List<LogInfo> logs = new List<LogInfo>();
        List<LogInfo> dataTable;

        public Form1()
        {
            InitializeComponent();

            //Fill SearchParam ComboBox
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                cbSearchParam.Items.Add(column.Name);
            }
            cbSearchParam.SelectedIndex = 2;

            try
            {
                //Get the files from directory
                string[] extensions = new[] { ".txt", ".log" };
                FileInfo[] files = new DirectoryInfo(LogPath).GetFiles()
                                                                  .Where(p => extensions.Contains(p.Extension.ToLower()))
                                                                  .OrderByDescending(p => p.LastWriteTime)
                                                                  .ToArray();
                
                //Open the newest file
                OpenFile(Path.GetFileName(files.First().FullName));

                //Add filenames to ListBox
                foreach (FileInfo file in files)
                {
                    filesListBox.Items.Add(Path.GetFileName(file.FullName));
                }
            }
            catch
            {
                MessageBox.Show("Érvénytelen elérési út");
            }
        }

        private void Search()
        {
            if (!string.IsNullOrEmpty(tbSearchValue.Text))
            {
                int col = cbSearchParam.SelectedIndex;
                string val = tbSearchValue.Text;


                switch (col)
                {
                    case 0: dataTable = new List<LogInfo>(logs.Where(p => p.Error.Contains(val)));
                        break;
                    case 1: dataTable = new List<LogInfo>(logs.Where(p => p.Time.Contains(val)));
                        break;
                    case 2: dataTable = new List<LogInfo>(logs.Where(p => p.ProcessId.Contains(val)));
                        break;
                    case 3: dataTable = new List<LogInfo>(logs.Where(p => p.Namespace.Contains(val)));
                        break;
                    case 4: dataTable = new List<LogInfo>(logs.Where(p => p.Message.Contains(val)));
                        break;
                    case 5: dataTable = new List<LogInfo>(logs.Where(p => p.Stacktrace.Contains(val)));
                        break;
                    case 6: dataTable = new List<LogInfo>(logs.Where(p => p.InnerExceptionMessage.Contains(val)));
                        break;
                    case 7: dataTable = new List<LogInfo>(logs.Where(p => p.InnerExceptionStacktrace.Contains(val)));
                        break;
                }
            }
            else
            {
                dataGridView1.DataSource = logs;
            }
            dataGridView1.Refresh();
        }

        private void OpenFile(string fileName)
        {
            try
            {
                //dataGridView1.ScrollBars = ScrollBars.None;
                //dataGridView1.Rows.Clear();
                //dataGridView1.ScrollBars = ScrollBars.Both;

                logs.Clear();

                //Open the Log file for reading
                try
                {
                    TextReader reader = new StreamReader(File.OpenRead(LogPath + fileName));
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
                            log.Time = node.Attributes["Time"].Value;
                            log.ProcessId = node.Attributes["ProcessID"] != null ? node.Attributes["ProcessID"].Value : string.Empty;
                            //There could be either Entry or Message in a LogInfo
                            log.Message = node.Attributes["Message"] != null ? node.Attributes["Message"].Value : string.Empty;
                            log.Message = node.Attributes["Entry"] != null ? node.Attributes["Entry"].Value : string.Empty;

                            logs.Add(log);
                        }

                        //Process LogErrorInfo
                        foreach (XmlNode node in listerror)
                        {
                            LogInfo log = new LogInfo();
                            log.Error = "ERROR";
                            log.Time = node.Attributes["Time"].Value;
                            log.ProcessId = node.Attributes["ProcessID"] != null ? node.Attributes["ProcessID"].Value : string.Empty;
                            log.Namespace = node.Attributes["Namespace"] != null ? node.Attributes["Namespace"].Value : string.Empty;
                            log.Message = node.Attributes["Message"] != null ? node.Attributes["Message"].Value : string.Empty;
                            log.Message = node.Attributes["Entry"] != null ? node.Attributes["Entry"].Value : string.Empty;
                            log.Stacktrace = node.Attributes["Stacktrace"] != null ? node.Attributes["Stacktrace"].Value : string.Empty;
                            if (node.SelectSingleNode("InnerException") != null)
                            {
                                XmlNode iex = node.SelectSingleNode("InnerException");
                                log.InnerExceptionMessage = node.Attributes["Message"] != null ? node.Attributes["Message"].Value : string.Empty;
                                log.InnerExceptionStacktrace = node.Attributes["Stacktrace"] != null ? node.Attributes["Stacktrace"].Value : string.Empty;
                            }
                            logs.Add(log);
                        }

                        dataTable = new List<LogInfo>(logs.OrderBy(p => p.Time).ToList());

                        dataGridView1.DataSource = dataTable;

                        //Sort log
                        //dataGridView1.Sort(dataGridView1.Columns["Time"], ListSortDirection.Ascending);
                        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                        //Set Statusbar
                        LogCountValue.Text = (list.Count + listerror.Count).ToString();
                        ErrorCountValue.Text = listerror.Count.ToString();

                        //Close log file
                        reader.Close();
                        lblFileName.Text = fileName;
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

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenFile(Path.GetFileName(openFileDialog1.FileName));
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 240;
            dataGridView1.Height = this.Height - 105;
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60,142,255);
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].Value != null && dataGridView1.Rows[i].Cells[0].Value.Equals("ERROR"))
                {
                    //Highlight errors
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255,126,126);
                    dataGridView1.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
                }
            }
        }

        private void filesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (filesListBox.SelectedIndex != -1)
                OpenFile(filesListBox.SelectedItem.ToString());
        }

        private void tbSearchValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Search();
        }
    }

    public class LogInfo
    {
        //LogNodes for LogInfo and LogErrorInfo
        public string Error { get { if (this.Error == null) return ""; else return this.Error; } set; }
        public string Time { get { if (this.Time == null) return ""; else return this.Time; } set; }
        public string ProcessId { get { if (this.ProcessId == null) return ""; else return this.ProcessId; } set; }
        public string Namespace { get { if (this.Namespace == null) return ""; else return this.Namespace; } set; }
        public string Message { get { if (this.Message == null) return ""; else return this.Message; } set; }
        public string Name { get { if (this.Name == null) return ""; else return this.Name; } set; }
        public string Stacktrace { get { if (this.Stacktrace == null) return ""; else return this.Stacktrace; } set; }
        public string InnerExceptionMessage { get { if (this.InnerExceptionMessage == null) return ""; else return this.InnerExceptionMessage; } set; }
        public string InnerExceptionStacktrace { get { if (this.InnerExceptionStacktrace == null) return ""; else return this.InnerExceptionStacktrace; } set; }
    }


}
