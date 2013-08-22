﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace File_Searcher
{
    public partial class MainForm : Form
    {
        private Thread searchThread = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;

            //btnSearch.BackgroundImage = Image.FromFile("C:\\Users\\Jasper\\Downloads\\document_search.ico");
            //btnSearch.BackgroundImageLayout = ImageLayout.Center;

            listViewResults.View = View.Details;
            ColumnHeader headerExt = listViewResults.Columns.Add("Extension", 1, HorizontalAlignment.Right);
            ColumnHeader headerName = listViewResults.Columns.Add("Name", 1, HorizontalAlignment.Left);
            ColumnHeader headerSize = listViewResults.Columns.Add("Size (KB)", 1, HorizontalAlignment.Right);
            ColumnHeader headerLastModified = listViewResults.Columns.Add("Last Modified", 1, HorizontalAlignment.Right);
            headerExt.Width = 60;
            headerName.Width = 430;
            headerSize.Width = 85;
            headerLastModified.Width = 161; //! -4 becuase else we get a scrollbar
        }

        private void button2_Click(object sender, EventArgs e)
        {
            searchThread = new Thread(new ThreadStart(StartSearching));
            searchThread.Start();
        }

        private void StartSearching()
        {
            string searchDirectory = txtBoxDirectorySearch.Text;
            string searchFilename = txtBoxFilenameSearch.Text;

            if (searchDirectory == "" || searchDirectory == String.Empty)
            {
                MessageBox.Show("The search directory field was left empty!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(searchDirectory))
            {
                MessageBox.Show("The directory to search for could not be found!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (Path.HasExtension(searchDirectory))
            {
                MessageBox.Show("The search directory field contains an extension!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (searchFilename != "" && searchFilename != String.Empty && Directory.Exists(searchFilename))
            {
                MessageBox.Show("The field for filename contains a directory!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetEnabledOfControl(btnSearch, false);
            SetEnabledOfControl(btnStopSearching, true);

            string allFiles = "";

            ClearListViewResultsCrossThread(listViewResults);

            //! Function also fills up the listbox on the fly
            GetAllFilesFromDirectoryAndFillResults(searchDirectory, checkBoxIncludeSubDirs.Checked, ref allFiles);

            if (allFiles == string.Empty)
            {
                MessageBox.Show("The searched directory contains no files at all.", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetEnabledOfControl(btnSearch, true);
            SetEnabledOfControl(btnStopSearching, false);
        }

        private void comboBoxSearchDir_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void GetAllFilesFromDirectoryAndFillResults(string directorySearch, bool includingSubDirs, ref string allFiles)
        {
            try
            {
                string[] directories = Directory.GetDirectories(directorySearch);
                string[] files = Directory.GetFiles(directorySearch);

                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i] != "")
                    {
                        if (txtBoxFilenameSearch.Text == "" || (txtBoxFilenameSearch.Text != "" && files[i].Contains(txtBoxFilenameSearch.Text)))
                        {
                            if ((File.GetAttributes(files[i]) & FileAttributes.Hidden) != FileAttributes.Hidden)
                            {
                                allFiles += files[i] + "\n"; //! Need to fill up the reference...

                                if (Path.HasExtension(files[i]))
                                {
                                    string fileName = files[i];
                                    string extension = Path.GetExtension(files[i]);
                                    string fileSize = new FileInfo(files[i]).Length.ToString();

                                    if (!checkBoxShowDir.Checked)
                                        fileName = Path.GetFileName(fileName);

                                    AddItemToListView(listViewResults, new ListViewItem(new[] { extension, fileName, fileSize, new FileInfo(files[i]).LastWriteTime.ToString() }));
                                }
                            }
                        }
                    }
                }

                //! If we include sub directories, recursive call this function up to every single directory.
                if (includingSubDirs)
                    for (int i = 0; i < directories.Length; i++)
                        GetAllFilesFromDirectoryAndFillResults(directories[i], true, ref allFiles);
            }
            catch (Exception) { }; //! No need to do anything with the error.
        }

        private void btnSearchDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select a directory in which you want to search for files and directories.";

            if (txtBoxDirectorySearch.Text != "" && Directory.Exists(txtBoxDirectorySearch.Text))
                fbd.SelectedPath = txtBoxDirectorySearch.Text;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtBoxDirectorySearch.Text = fbd.SelectedPath;
        }

        private void btnStopSearching_Click(object sender, EventArgs e)
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                searchThread.Abort();
                searchThread = null;

                SetEnabledOfControl(btnSearch, true);
                SetEnabledOfControl(btnStopSearching, false);
            }
        }

        private void checkBoxShowDir_CheckedChanged(object sender, EventArgs e)
        {
            //! Update current results with path
            foreach (ListViewItem item in listViewResults.Items)
                item.SubItems[1].Text = checkBoxShowDir.Checked ? Path.GetFullPath(item.SubItems[1].Text) : Path.GetFileName(item.SubItems[1].Text);
        }

        //! Cross-thread functions:
        private delegate void SetEnabledOfControlDelegate(Control control, bool enable);

        private void SetEnabledOfControl(Control control, bool enable)
        {
            if (control.InvokeRequired)
            {
                Invoke(new SetEnabledOfControlDelegate(SetEnabledOfControl), new object[] { control, enable });
                return;
            }

            control.Enabled = enable;
        }

        private delegate void AddItemToListViewDelegate(ListView listView, ListViewItem item);

        private void AddItemToListView(ListView listView, ListViewItem item)
        {
            if (listView.InvokeRequired)
            {
                Invoke(new AddItemToListViewDelegate(AddItemToListView), new object[] { listView, item });
                return;
            }

            listView.Items.Add(item);
        }

        private delegate void ClearListViewResultsCrossThreadDelegate(ListView listView);

        private void ClearListViewResultsCrossThread(ListView listView)
        {
            if (listView.InvokeRequired)
            {
                Invoke(new ClearListViewResultsCrossThreadDelegate(ClearListViewResultsCrossThread), new object[] { listView });
                return;
            }

            listView.Items.Clear();
        }
    }
}