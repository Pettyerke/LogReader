using System;
using System.Collections.Generic;
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
        public Form1()
        {
            InitializeComponent();
            try
            {
                //Get the files from directory
                string[] extensions = new[] { ".txt", ".log" };
                FileInfo[] files = new DirectoryInfo("c:\\Temp\\").GetFiles()
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

        private void OpenFile(string fileName)
        {
            try
            {
                dataGridView1.ScrollBars = ScrollBars.None;
                dataGridView1.Rows.Clear();
                dataGridView1.ScrollBars = ScrollBars.Both;

                //Open the Log file for reading
                try
                {
                    TextReader reader = new StreamReader(File.OpenRead(@"c:\Temp\" + fileName));
                    string file = reader.ReadToEnd();
                    if (file.StartsWith("<"))
                    {
                        string xmlString = string.Join("", new String[] { "<Log>", file, "</Log>" });
                        XmlDocument xml = new XmlDocument();
                        List<LogInfo> logs = new List<LogInfo>();

                        //Get nodes
                        xml.LoadXml(xmlString);
                        XmlNodeList list = xml.SelectNodes("Log/LogInfo");
                        XmlNodeList listerror = xml.SelectNodes("Log/LogErrorInfo");

                        //Process LogInfo
                        foreach (XmlNode node in list)
                        {
                            LogInfo log = new LogInfo();
                            log.Time = node.Attributes["Time"].Value;
                            if (node.Attributes["ProcessID"] != null)
                                log.ProcessId = node.Attributes["ProcessID"].Value;
                            //There could be either Entry or Message in a LogInfo
                            if (node.SelectSingleNode("Message") != null)
                                log.Message = node.SelectSingleNode("Message").InnerText;
                            else if (node.SelectSingleNode("Entry") != null)
                                log.Message = node.SelectSingleNode("Entry").InnerText;
                            logs.Add(log);

                            dataGridView1.Rows.Add( "",
                                                    log.Time,
                                                    log.ProcessId,
                                                    log.Namespace,
                                                    log.Message,
                                                    log.Stacktrace,
                                                    "",
                                                    "");
                        }

                        //Process LogErrorInfo
                        foreach (XmlNode node in listerror)
                        {
                            LogInfo log = new LogInfo();
                            log.Time = node.Attributes["Time"].Value;
                            if (node.Attributes["ProcessID"] != null)
                                log.ProcessId = node.Attributes["ProcessID"].Value;
                            if (node.SelectSingleNode("Namespace") != null)
                                log.Namespace = node.SelectSingleNode("Namespace").InnerText;
                            //There could be either Entry or Message in a LogInfo
                            if (node.SelectSingleNode("Message") != null)
                                log.Message = node.SelectSingleNode("Message").InnerText;
                            else if (node.SelectSingleNode("Entry") != null)
                                log.Message = node.SelectSingleNode("Entry").InnerText;
                            if (node.SelectSingleNode("Stacktrace") != null)
                                log.Stacktrace = node.SelectSingleNode("Stacktrace").InnerText;
                            if (node.SelectSingleNode("InnerException") != null)
                            {
                                XmlNode iex = node.SelectSingleNode("InnerException");
                                if (iex.SelectSingleNode("Message") != null)
                                    log.InnerExceptionMessage = iex.SelectSingleNode("Message").InnerText;
                                if (iex.SelectSingleNode("Stacktrace") != null)
                                    log.InnerExceptionStacktrace = iex.SelectSingleNode("Stacktrace").InnerText;
                            }
                            logs.Add(log);

                            dataGridView1.Rows.Add("ERROR",
                                                    log.Time,
                                                    log.ProcessId,
                                                    log.Namespace,
                                                    log.Message,
                                                    log.Stacktrace,
                                                    log.InnerExceptionMessage,
                                                    log.InnerExceptionStacktrace);
                        }

                        //Sort log
                        dataGridView1.Sort(dataGridView1.Columns["Time"], ListSortDirection.Ascending);
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
    }

    public class LogInfo
    {
        //LogNodes for LogInfo and LogErrorInfo
        public string Time = string.Empty;
        public string ProcessId = string.Empty;
        public string Namespace = string.Empty;
        public string Message = string.Empty;
        public string Name = string.Empty;
        public string Stacktrace = string.Empty;
        public string InnerExceptionMessage = string.Empty;
        public string InnerExceptionStacktrace = string.Empty;
    }


}
