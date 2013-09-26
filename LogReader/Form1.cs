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
            List<FileInfo> files = new DirectoryInfo("c:\\Temp\\").GetFiles("*.txt").OrderBy(p => p.CreationTime).ToList();
            DateTime latest = DateTime.MinValue;
            string defaultfile = string.Empty;
            foreach (FileInfo file in files)
            {
                string filePath = file.FullName;
                filesListBox.Items.Add(Path.GetFileNameWithoutExtension(filePath));
                DateTime time = File.GetLastWriteTime(filePath);
                if (time > latest)
                {
                    latest = time;
                    defaultfile = filePath;
                }
            }
            OpenFile(Path.GetFileNameWithoutExtension(defaultfile));
        }

        private void OpenFile(string fileName)
        {
            string line = string.Empty;
            dataGridView1.Rows.Clear();

            //open the Log file for reading
            TextReader reader = new StreamReader(File.OpenRead(@"c:\Temp\" + fileName + ".txt"));
            string file = reader.ReadToEnd();
            if (file.StartsWith("<"))
            {
                string xmlString = string.Join("", new String[] { "<Log>", file, "</Log>" });
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlString);
                XmlNodeList list = xml.SelectNodes("Log/LogInfo");
                XmlNodeList listerror = xml.SelectNodes("Log/LogErrorInfo");


                List<LogInfo> logs = new List<LogInfo>();

                foreach (XmlNode node in list)
                {
                    LogInfo log = new LogInfo();
                    //log.Time = DateTime.Parse(node.Attributes["Time"].Value);
                    log.Time = node.Attributes["Time"].Value;
                    if (node.Attributes["ProcessID"] != null)
                        log.ProcessId = node.Attributes["ProcessID"].Value;
                    if (node.SelectSingleNode("Message") != null)
                        log.Message = node.SelectSingleNode("Message").InnerText;
                    else if (node.SelectSingleNode("Entry") != null)
                        log.Message = node.SelectSingleNode("Entry").InnerText;
                    logs.Add(log);

                    dataGridView1.Rows.Add("", log.Time, log.ProcessId, log.Namespace, log.Message, log.Stacktrace);
                }

                foreach (XmlNode node in listerror)
                {
                    LogInfo log = new LogInfo();
                    log.Time = node.Attributes["Time"].Value;
                    if (node.Attributes["ProcessID"] != null)
                        log.ProcessId = node.Attributes["ProcessID"].Value;
                    if (node.SelectSingleNode("Namespace") != null)
                        log.Namespace = node.SelectSingleNode("Namespace").InnerText;
                    log.Message = node.SelectSingleNode("Message").InnerText;
                    if (node.SelectSingleNode("Stacktrace") != null)
                        log.Stacktrace = node.SelectSingleNode("Stacktrace").InnerText;
                    logs.Add(log);

                    dataGridView1.Rows.Add("ERROR", log.Time, log.ProcessId, log.Namespace, log.Message, log.Stacktrace);
                }

                dataGridView1.Sort(dataGridView1.Columns[1],ListSortDirection.Ascending);
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                LogCountValue.Text = (list.Count + listerror.Count).ToString();
                ErrorCountValue.Text = listerror.Count.ToString();

                //Close log file
                reader.Close();
                lblFileName.Text = fileName;
            }
            else
            {
                MessageBox.Show("A fájl felépítése nem megfelelő");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenFile(Path.GetFileNameWithoutExtension(openFileDialog1.FileName));
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 240;
            dataGridView1.Height = this.Height - 80;
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60,142,255);
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].Value != null && dataGridView1.Rows[i].Cells[0].Value.Equals("ERROR"))
                {
                    //dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Red;
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
        public string Time = string.Empty;
        public string ProcessId = string.Empty;
        public string Namespace = string.Empty;
        public string Message = string.Empty;
        public string Name = string.Empty;
        public string Stacktrace = string.Empty;
    }


}
