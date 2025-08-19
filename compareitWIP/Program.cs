using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace compareitWIP
{
    public class MainForm : Form
    {
        private ComboBox cmbPath1;
        private ComboBox cmbPath2;
        private Button btnBrowse1;
        private Button btnBrowse2;
        private Button btnCompare;
        private Button btnCopyActions;
        private TextBox txtResults;

        public MainForm()
        {
            this.Text = "File Comparer";
            this.Width = 700;
            this.Height = 550;

            Label lbl1 = new Label() { Text = "Folder 1:", Top = 20, Left = 10, Width = 60 };
            cmbPath1 = new ComboBox() { Top = 20, Left = 80, Width = 500, DropDownStyle = ComboBoxStyle.DropDown };
            btnBrowse1 = new Button() { Text = "Browse", Top = 20, Left = 590 };
            btnBrowse1.Click += (s, e) => BrowseFolder(cmbPath1);

            Label lbl2 = new Label() { Text = "Folder 2:", Top = 60, Left = 10, Width = 60 };
            cmbPath2 = new ComboBox() { Top = 60, Left = 80, Width = 500, DropDownStyle = ComboBoxStyle.DropDown };
            btnBrowse2 = new Button() { Text = "Browse", Top = 60, Left = 590 };
            btnBrowse2.Click += (s, e) => BrowseFolder(cmbPath2);

            btnCompare = new Button() { Text = "Compare", Top = 100, Left = 80, Width = 100 };
            btnCompare.Click += (s, e) => CompareFolders();

            btnCopyActions = new Button() { Text = "Copy/Sync Options", Top = 100, Left = 200, Width = 150 };
            btnCopyActions.Click += (s, e) => ShowCopyActionsPopup();

            txtResults = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Top = 140, Left = 10, Width = 660, Height = 350, ReadOnly = true };

            this.Controls.Add(lbl1);
            this.Controls.Add(cmbPath1);
            this.Controls.Add(btnBrowse1);
            this.Controls.Add(lbl2);
            this.Controls.Add(cmbPath2);
            this.Controls.Add(btnBrowse2);
            this.Controls.Add(btnCompare);
            this.Controls.Add(btnCopyActions);
            this.Controls.Add(txtResults);
        }

        private void BrowseFolder(ComboBox target)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    target.Text = fbd.SelectedPath;
                    AddToHistory(target, fbd.SelectedPath);
                }
            }
        }

        private void AddToHistory(ComboBox comboBox, string path)
        {
            if (!comboBox.Items.Contains(path))
            {
                comboBox.Items.Insert(0, path);
            }
        }

        private (string[] onlyIn1, string[] onlyIn2) GetDifferences(string path1, string path2)
        {
            var files1 = Directory.GetFiles(path1, "*", SearchOption.AllDirectories)
                                   .Select(f => f.Substring(path1.Length).TrimStart('\\'))
                                   .ToHashSet();

            var files2 = Directory.GetFiles(path2, "*", SearchOption.AllDirectories)
                                   .Select(f => f.Substring(path2.Length).TrimStart('\\'))
                                   .ToHashSet();

            var onlyIn1 = files1.Except(files2).ToArray();
            var onlyIn2 = files2.Except(files1).ToArray();

            return (onlyIn1, onlyIn2);
        }

        private void CompareFolders()
        {
            string path1 = cmbPath1.Text;
            string path2 = cmbPath2.Text;

            if (!Directory.Exists(path1) || !Directory.Exists(path2))
            {
                MessageBox.Show("Please enter valid folder paths.");
                return;
            }

            AddToHistory(cmbPath1, path1);
            AddToHistory(cmbPath2, path2);

            var (onlyIn1, onlyIn2) = GetDifferences(path1, path2);

            txtResults.Clear();
            if (!onlyIn1.Any() && !onlyIn2.Any())
            {
                txtResults.AppendText("The folders are identical.\r\n");
            }
            else
            {
                if (onlyIn1.Any())
                {
                    txtResults.AppendText("Files only in Folder 1:\r\n");
                    foreach (var f in onlyIn1)
                        txtResults.AppendText(" - " + f + "\r\n");
                }

                if (onlyIn2.Any())
                {
                    txtResults.AppendText("\r\nFiles only in Folder 2:\r\n");
                    foreach (var f in onlyIn2)
                        txtResults.AppendText(" - " + f + "\r\n");
                }
            }
        }

        private void ShowCopyActionsPopup()
        {
            Form popup = new Form()
            {
                Text = "Copy/Sync Options",
                Width = 400,
                Height = 200,
                StartPosition = FormStartPosition.CenterParent
            };

            Button btnCopy1to2 = new Button() { Text = "Copy Folder1 → Folder2", Top = 20, Left = 50, Width = 280 };
            Button btnCopy2to1 = new Button() { Text = "Copy Folder2 → Folder1", Top = 60, Left = 50, Width = 280 };
            Button btnSyncBoth = new Button() { Text = "Sync Both Ways", Top = 100, Left = 50, Width = 280 };

            btnCopy1to2.Click += (s, e) => { ExecuteCopy(1); popup.Close(); };
            btnCopy2to1.Click += (s, e) => { ExecuteCopy(2); popup.Close(); };
            btnSyncBoth.Click += (s, e) => { ExecuteCopy(3); popup.Close(); };

            popup.Controls.Add(btnCopy1to2);
            popup.Controls.Add(btnCopy2to1);
            popup.Controls.Add(btnSyncBoth);

            popup.ShowDialog();
        }

        private void ExecuteCopy(int mode)
        {
            string path1 = cmbPath1.Text;
            string path2 = cmbPath2.Text;

            if (!Directory.Exists(path1) || !Directory.Exists(path2))
            {
                MessageBox.Show("Please enter valid folder paths.");
                return;
            }

            var (onlyIn1, onlyIn2) = GetDifferences(path1, path2);

            if (mode == 1)
            {
                CopyFiles(path1, path2, onlyIn1);
                MessageBox.Show("Copied missing files from Folder 1 to Folder 2");
            }
            else if (mode == 2)
            {
                CopyFiles(path2, path1, onlyIn2);
                MessageBox.Show("Copied missing files from Folder 2 to Folder 1");
            }
            else if (mode == 3)
            {
                CopyFiles(path1, path2, onlyIn1);
                CopyFiles(path2, path1, onlyIn2);
                MessageBox.Show("Synchronized both folders");
            }
        }

        private void CopyFiles(string sourceRoot, string targetRoot, string[] relativePaths)
        {
            foreach (var relativePath in relativePaths)
            {
                string sourcePath = Path.Combine(sourceRoot, relativePath);
                string targetPath = Path.Combine(targetRoot, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(sourcePath, targetPath, true);
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }
}
