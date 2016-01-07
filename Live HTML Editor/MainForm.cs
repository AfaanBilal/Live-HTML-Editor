/*
 *  Live HTML Editor
 *  (c) 2012-2016 Afaan Bilal
 *
 */


using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Diagnostics;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

namespace Live_HTML_Editor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        int numTabs, maxTabs;
        string[] savedFileNames;
        Timer spt;
        About abt;


        private void MainForm_Load(object sender, EventArgs e)
        {
            /* 
             * FULL SCREEN VIEW
             * SEQUENCE => Optional[TopMost(true),] FormBorderStyle(None), WindowState(Max);
             * this.TopMost = true;
            
            
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
             */
            //this.WindowState = FormWindowState.Maximized;
            //this.MaximizeBox = false;
            
            /*
             * DISABLE EDITOR AND OTHER BUTTONS
            */

            EnableEditingButtons(false);

            /*
             * Tabs vars
           */
            numTabs = 0;
            maxTabs = 7;


            // savedfilenames
            savedFileNames = new string[10];

            //Mainform contextmenu
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add(new MenuItem("About", aboutToolStripMenuItem_Click));
            cm.MenuItems.Add(new MenuItem("-"));
            cm.MenuItems.Add(new MenuItem("Exit", exitToolStripMenuItem_Click));
            this.ContextMenu = cm;

            //WebMain Contextmenu
            ContextMenu cmwb = new ContextMenu();
            cmwb.MenuItems.Add(new MenuItem("About", aboutToolStripMenuItem_Click));
            cmwb.MenuItems.Add(new MenuItem("-"));
            cmwb.MenuItems.Add(new MenuItem("Refresh", browserRefresh));
            cmwb.MenuItems.Add(new MenuItem("-"));
            cmwb.MenuItems.Add(new MenuItem("Exit", exitToolStripMenuItem_Click));
            wbMain.ContextMenu = cmwb;

            //show about splash
            ShowSplash();

            mainstatus_lblCp.Text = "Copyright © " + DateTime.Now.Year + " Afaan Bilal, AMX Infinity!";

        }

        private void ShowSplash(bool dlg = false)
        {
            spt = new Timer();
            spt.Interval = 5500;
            spt.Tick += new EventHandler(spt_Tick);
            spt.Start();
            abt = new About();
            abt.TopMost = true;
            if (dlg)
                abt.ShowDialog();
            else
                abt.Show();
        }

        void spt_Tick(object sender, EventArgs e)
        {
            abt.Dispose();
            spt.Dispose();
        }

        private void SetContextMenuForTab(TabPage tb)
        {
            ContextMenu contextMenu = new ContextMenu();
            
            contextMenu.MenuItems.Add(new MenuItem("Cut", cutToolStripMenuItem1_Click));
            contextMenu.MenuItems.Add(new MenuItem("Copy", copyToolStripMenuItem1_Click));
            contextMenu.MenuItems.Add(new MenuItem("Paste", pasteToolStripMenuItem1_Click));
            contextMenu.MenuItems.Add(new MenuItem("-"));
            contextMenu.MenuItems.Add(new MenuItem("Find", menuItem6_Click));
            contextMenu.MenuItems.Add(new MenuItem("Replace", menuItem7_Click));
            contextMenu.MenuItems.Add(new MenuItem("-", aboutToolStripMenuItem_Click));
            contextMenu.MenuItems.Add(new MenuItem("About", aboutToolStripMenuItem_Click));
            contextMenu.MenuItems.Add(new MenuItem("-"));
            contextMenu.MenuItems.Add(new MenuItem("Close Tab", CloseTab));
            contextMenu.MenuItems.Add(new MenuItem("-"));
            contextMenu.MenuItems.Add(new MenuItem("Exit", exitToolStripMenuItem_Click));

            tb.ContextMenu = contextMenu;
        }

        void menuItem7_Click(object sender, EventArgs e)
        {
            getEditor().ShowReplaceDialog();
        }

        void menuItem6_Click(object sender, EventArgs e)
        {
            getEditor().ShowFindDialog();
        }

        void browserRefresh(object sender, EventArgs e)
        {
            updateBrowser();
        }
        
        private void AddTab(string tabName = "", string textToInsert = "")
        {
            //enable editor and toolstrip buttons and menustrip Edit
            EnableEditingButtons(true);

            numTabs++;

            if (numTabs > maxTabs)
            {
                MessageBox.Show("Max Tabs Reached. Can't add more, sorry", "Live HTML Editor");
                return;
            }

            // Add TabPage
            TabPage tabPage = new TabPage();
            tabPage.Name = "tab" + numTabs;
            
            if (tabName == "")
                tabPage.Text = "New" + numTabs + ".html";
            else
                tabPage.Text = tabName;

            SetContextMenuForTab(tabPage);

            //Add FTB
            FastColoredTextBox ftb = new FastColoredTextBox();
            ftb.Name = "ftb" + numTabs;
            ftb.Dock = DockStyle.Fill;
            ftb.Text = textToInsert;
            ftb.TextChanged += new EventHandler<TextChangedEventArgs>(ftb_TextChanged);
            ftb.AcceptsReturn = true;
            ftb.AcceptsTab = true;
            ftb.WordWrap = false;
            ftb.HighlightingRangeType = HighlightingRangeType.AllTextRange;
            ftb.AutoIndent = true;
            ftb.Language = Language.HTML;
            ftb.AutoIndentExistingLines = true;
            ftb.CaretColor = Color.Blue;
            ftb.OnTextChanged();

            //Add FTB to tabPage
            tabPage.Tag = ftb;

            // Add rtb to tab
            tabPage.Controls.Add(ftb);

            // Add tab to TabControl
            tabs_editor.TabPages.Add(tabPage);

            // put the current tab as selected
            tabs_editor.SelectedTab = tabPage;
        }

        private FastColoredTextBox getEditor()
        {
            return (FastColoredTextBox)tabs_editor.SelectedTab.Tag;
        }

        private void EnableEditingButtons(bool enable = true)
        {
            if (enable)
            {
                tabs_editor.Enabled = true;
                tsb_Save.Enabled = true;
                tsb_Close.Enabled = true;
                editToolStripMenuItem2.Enabled = true;
                saveAsToolStripMenuItem2.Enabled = true;
                saveToolStripMenuItem2.Enabled = true;
            }
            else
            {
                tabs_editor.Enabled = false;
                tsb_Save.Enabled = false;
                tsb_Close.Enabled = false;
                editToolStripMenuItem2.Enabled = false;
                saveAsToolStripMenuItem2.Enabled = false;
                saveToolStripMenuItem2.Enabled = false;
            }
        }

        void ftb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (FastColoredTextBox)sender;

            //highlight html
            tb.SyntaxHighlighter.HTMLSyntaxHighlight(tb.Range);
            
            //find Style fragments
            foreach (var r in tb.GetRanges(@"<style.*?</style>", RegexOptions.Singleline))
            {
                //remove HTML highlighting from this fragment
                r.ClearStyle(StyleIndex.All);
                //do STYLE highlighting
                tb.SyntaxHighlighter.HighlightSyntax(Language.Custom, r);
            }

            //find JS fragments
            foreach (var r in tb.GetRanges(@"<script.*?</script>", RegexOptions.Singleline))
            {
                //remove HTML highlighting from this fragment
                r.ClearStyle(StyleIndex.All);
                //do JS highlighting
                tb.SyntaxHighlighter.HighlightSyntax(Language.JS, r);
            }

            Save();
            updateBrowser();
        }

        private void CloseTab(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Live HTML Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                tabs_editor.TabPages.Remove(tabs_editor.SelectedTab);
                mainStatus_lblFileName.Text = "FileName";
                mainStatus_lblSaved.Text = "Not Saved Yet";
                wbMain.Navigate("about:blank");
                numTabs--;

                if (tabs_editor.TabCount < 1)
                {
                    //disable editor and toolstrip buttons if all tabs closed. 
                    EnableEditingButtons(false);
                }
            }
        }

        void rtb_TextChanged(object sender, EventArgs e)
        {
            Save();
            updateBrowser();
        }
                
        private void Save()
        {
            try
            {
                getEditor().SaveToFile(savedFileNames[tabs_editor.SelectedIndex], new ASCIIEncoding());
            }
            catch (Exception)
            {
                //nothing :P
            }
            mainStatus_lblSaved.Text = "Saved: " + DateTime.Now.ToString("hh:mm tt");
            updateBrowser();
        }

        private bool SaveAs()
        {
            SaveFileDialog dlgSave = new SaveFileDialog();
            dlgSave.InitialDirectory = Environment.SpecialFolder.Desktop.ToString();
            dlgSave.Title = "Save as. . .";
            dlgSave.FileName = tabs_editor.SelectedTab.Text;
            dlgSave.Filter = "HTML Files|*.htm;*.html|Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (dlgSave.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                savedFileNames[tabs_editor.SelectedIndex] = dlgSave.FileName;
                tabs_editor.SelectedTab.Text = Path.GetFileName(savedFileNames[tabs_editor.SelectedIndex]);
                mainStatus_lblFileName.Text = savedFileNames[tabs_editor.SelectedIndex];
                getEditor().SaveToFile(savedFileNames[tabs_editor.SelectedIndex], new ASCIIEncoding());
                mainStatus_lblSaved.Text = "Saved: " + DateTime.Now.ToString("hh:mm tt");
                return true;
            }
            else
                return false;
        }

        private void updateBrowser()
        {
            if (tabs_editor.TabCount > 0)
            {
                if (savedFileNames[tabs_editor.SelectedIndex] != null)
                {
                    string urli = new Uri(savedFileNames[tabs_editor.SelectedIndex]).AbsoluteUri;
                    wbMain.Url = new Uri(urli);
                }
            }
        }


        #region MenuStripFunctions

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddTab();
            if (SaveAs())
            {
                updateBrowser();
            }
            else
            {
                tabs_editor.TabPages.Remove(tabs_editor.SelectedTab);
                mainStatus_lblFileName.Text = "FileName";
                mainStatus_lblSaved.Text = "Not Saved Yet";
                wbMain.Navigate("about:blank");

                if (tabs_editor.TabCount < 1)
                {
                    //disable editor and toolstrip buttons if all tabs closed. 
                    EnableEditingButtons(false);
                }
                MessageBox.Show("Error: Please specify a filename to save.", "Live HTML Editor");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlgOpen = new OpenFileDialog();
            dlgOpen.DereferenceLinks = false;
            dlgOpen.InitialDirectory = Environment.SpecialFolder.Desktop.ToString();
            dlgOpen.Filter = "HTML Files|*.htm;*.html|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            dlgOpen.Title = "Open a file to edit";

            string fileName = "";
            int size = -1;

            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                fileName = dlgOpen.FileName;
                try
                {
                    string text = File.ReadAllText(fileName);
                    size = text.Length;
                    AddTab(Path.GetFileName(fileName), text);
                    mainStatus_lblFileName.Text = fileName;
                    savedFileNames[tabs_editor.SelectedIndex] = fileName;
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Error: " + ex.ToString(), "Live HTML Editor");
                }
            }

            updateBrowser();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                SaveAs();
                updateBrowser();
            }
        }
        
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                if (savedFileNames[tabs_editor.SelectedIndex] == null || savedFileNames[tabs_editor.SelectedIndex] == "")
                    SaveAs();
                else
                {
                    Save();
                }

                updateBrowser();
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void wbStatus_TabIndexChanged(object sender, EventArgs e)
        {
            updateBrowser();
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                getEditor().Cut();
            }
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                getEditor().Copy();
            }
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                getEditor().Paste();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                getEditor().SelectAll();
            }
        }

        private void tabs_editor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                updateBrowser();
                mainStatus_lblFileName.Text = savedFileNames[tabs_editor.SelectedIndex];
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                FontDialog dlgFont = new FontDialog();
                try
                {
                    if (dlgFont.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        getEditor().Font = dlgFont.Font;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Live HTML Editor");
                }
            }
        }

        private void textColourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                ColorDialog dlgColor = new ColorDialog();
                try
                {
                    if (dlgColor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        getEditor().ForeColor = dlgColor.Color;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Live HTML Editor");
                }
            }
        }

        private void editorBackgroundColourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabs_editor.TabCount > 0)
            {
                ColorDialog dlgColor = new ColorDialog();
                try
                {
                    if (dlgColor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        getEditor().BackColor = dlgColor.Color;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Live HTML Editor");
                }
            }
        }
        
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Live HTML Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                //ShowSplash(true);
                //System.Threading.Thread.Sleep(200);
                Application.Exit();
            }
        }

        private void autoIndentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getEditor().DoAutoIndent();
        }
        #endregion

        #region AboutMenuLinks
        
        private void mainstatus_lblCp_Click(object sender, EventArgs e)
        {
            Process.Start("http://amxinfinity.tk");
        }

        private void ourProjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://amxinfinity.tk/Store");
        }

        private void visitUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://amxinfinity.tk");
        }

        private void authorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://afaan.cu.cc");
        }

        #endregion
    }
}
