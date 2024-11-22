using KeystoneNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IL2CPP_EASY_PATCHER
{
    public partial class MainForm : Form
    {
        private static bool patching = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            cbAsmLang.SelectedIndex = 0;
            dataGridView1.Rows.Add();
            dataGridView1[0, 0].Value = true;
            dataGridView1.CurrentCell = dataGridView1[1, 0];
        }

        public void ApplyLanguage()
        {
            // File menu
            tsmiFile.Text = global.lang.GetStr("file");
            tsmiSelectExe.Text = global.lang.GetStr("select_executable");
            tsmiRecents.Text = global.lang.GetStr("recents");
            tsmiExit.Text = global.lang.GetStr("exit");
            tsmiClear.Text = global.lang.GetStr("clear");

            // Settings menu
            tsmiSettings.Text = global.lang.GetStr("settings");
            tsmiMakeBackups.Text = global.lang.GetStr("make_backups");
            tsmiLanguage.Text = global.lang.GetStr("language");
            tsmiDefineConstants.Text = global.lang.GetStr("define_constants");

            // About menu
            tsmiAbout.Text = global.lang.GetStr("about");

            // List buttons
            btnAdd.Text = global.lang.GetStr("add_entry");
            btnRemove.Text = global.lang.GetStr("remove_entry");
            btnClear.Text = global.lang.GetStr("clear_entries");
            btnOpen.Text = global.lang.GetStr("load_patches");
            btnSave.Text = global.lang.GetStr("save_patches");
            btnSaveAs.Text = global.lang.GetStr("save_patches_as");
            btnPatch.Text = global.lang.GetStr("apply_patches");
            lbArch.Text = global.lang.GetStr("architecture");

            // List columns
            dataGridView1.Columns[1].HeaderText = global.lang.GetStr("name");
            dataGridView1.Columns[2].HeaderText = global.lang.GetStr("offset");
            dataGridView1.Columns[3].HeaderText = global.lang.GetStr("return_value");
        }

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            CustomDataGridView gridView = sender as CustomDataGridView;
            if (gridView == null || gridView.SelectedCells.Count == 0)
                return;

            if (e.Control && e.KeyCode == Keys.C) // Copiar
            {
                DataObject dataObj = gridView.GetClipboardContent();
                if (dataObj != null)
                    Clipboard.SetDataObject(dataObj);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V) // Pegar
            {
                string clipboardText = Clipboard.GetText();
                int rowIndex = gridView.SelectedCells[0].RowIndex;
                int colIndex = gridView.SelectedCells[0].ColumnIndex;
                gridView[colIndex, rowIndex].Value = clipboardText;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete) // Eliminar
            {
                Functions.DeleteEntry(ref gridView);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back) // Borrar
            {
                gridView[gridView.SelectedCells[0].ColumnIndex, gridView.SelectedCells[0].RowIndex].Value = null;
                e.Handled = true;
            }
            else if (e.Shift && e.KeyCode == Keys.Tab)
            {
                int currentCol = gridView.CurrentCell.ColumnIndex;
                int currentRow = gridView.CurrentCell.RowIndex;
                if (currentRow > 0)
                    gridView.CurrentCell = gridView[1, --currentRow];
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                int currentCol = gridView.CurrentCell.ColumnIndex;
                int currentRow = gridView.CurrentCell.RowIndex;
                if (currentRow < gridView.RowCount - 1)
                    gridView.CurrentCell = gridView[1, ++currentRow];
                e.Handled = true;
            }
        }
        
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView gridView = (DataGridView)sender;
            if (gridView.CurrentCell == null)
                return;

            int currentCol = gridView.CurrentCell.ColumnIndex;
            int currentRow = gridView.CurrentCell.RowIndex;

            if (currentCol == 2)
            {
                if (gridView[currentCol, currentRow].Value != null)
                {
                    string strVal = gridView[currentCol, currentRow].Value.ToString().ToLowerInvariant();
                    gridView[currentCol, currentRow].Value = Functions.ValidateInt(strVal, true, 8);
                }
            }
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            DataGridView gridView = (DataGridView)sender;
            gridView[0, e.RowIndex].Value = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ApplyPatch();
        }

        private void ApplyPatch()
        {
            if (patching)
                return;

            patching = true;
            dataGridView1.Focus();
            DataGridViewCell cell = dataGridView1.CurrentCell;
            dataGridView1.CurrentCell = dataGridView1[1, cell.RowIndex];
            btnPatch.Enabled = false;

            if (global.settings.MakeBackups)
                Functions.MakeBackup(global.loadedFile.FileName, global.loadedFile.FileName + ".bak", false);

            ulong position = 0;

            try
            {
                using (BinaryWriter w = new BinaryWriter(File.Open(global.loadedFile.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)))
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if ((bool)row.Cells[0].Value)
                        {
                            if (row.Cells[3].Value == null)
                                continue;

                            Functions.ValidateRow(ref global.keystone, dataGridView1, row.Index);

                            bool validAddress = row.Cells[2].Value != null && row.Cells[2].Value.ToString().Trim() != "";
                            w.BaseStream.Position = Functions.ParseInt(validAddress ? row.Cells[2].Value.ToString() : "0x" + position.ToString("X"));
                            byte[] bytes = Functions.HelperBytes(ref global.keystone, row.Cells[3].Value.ToString(), (ulong)w.BaseStream.Position);
                            position = (ulong)(w.BaseStream.Position + bytes.Length);

                            if (bytes.Length != 0)
                            {
                                w.Write(bytes);
                                row.Cells[3].Style.BackColor = Color.PaleGreen;
                            }
                            else
                                row.Cells[3].Style.BackColor = Color.Salmon;
                        }
                    }
                    w.Flush();
                }
            }
            catch 
            {
                MessageBox.Show(global.lang.GetStr("error"), global.lang.GetStr("could_not_write_to_file"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnPatch.Enabled = true;
            patching = false;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Functions.AddEntry(ref dataGridView1);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            Functions.DeleteEntry(ref dataGridView1);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            Functions.ClearEntries(ref dataGridView1);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void selectExecutableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fo = new OpenFileDialog())
            {
                fo.Filter = global.lang.GetStr("filter_all_files") + "|*.*";
                fo.Title = global.lang.GetStr("select_executable");

                fo.FileName = global.loadedFile.ShortName;

                if (global.loadedFile.FileName != "")
                    fo.InitialDirectory = Path.GetDirectoryName(global.loadedFile.FileName);

                if (fo.ShowDialog() != DialogResult.OK)
                    return;

                if (global.loadedFile.FileName.ToLowerInvariant() == fo.FileName.ToLowerInvariant())
                    return;
                
                global.loadedFile.FileName = fo.FileName;
            }

            selectExecutable();
        }

        private void selectExecutable(string fileName = "")
        {
            if (fileName != "")
                global.loadedFile.FileName = fileName;

            global.loadedFile.ShortName = Path.GetFileName(global.loadedFile.FileName);
            this.Text = Functions.GetTitle(false, true, true);

            ToolStripDropDownItem item = new ToolStripMenuItem()
            {
                Name = "tsmiRecent" + (0),
                Text = Path.GetFileName(global.loadedFile.ShortName),
                ToolTipText = global.loadedFile.FileName
            };
            item.Click += recentsToolStripMenuItem_Click;

            var match = tsmiRecents.DropDownItems.Cast<ToolStripItem>().Where(i => i.ToolTipText == item.ToolTipText);
            ToolStripItem[] items = match.ToArray();

            for (int i = 0; i < items.Length; i++)
                tsmiRecents.DropDownItems.Remove(items[i]);

            tsmiRecents.DropDownItems.Insert(0, item);

            match = tsmiRecents.DropDownItems.Cast<ToolStripItem>().Where(i => i.Name.StartsWith("tsmiRecent"));
            items = match.ToArray();
            int count = 1;
            for (int i = 0; i < items.Length; i++)
            {
                if (count > 10)
                    tsmiRecents.DropDownItems.Remove(items[i]);
                else
                    items[i].Name = "tsmiRecent" + (count++).ToString();
            }

            tsmiRecents.Enabled = tsmiRecents.DropDownItems.Count > 2;

            switch (Functions.DetectFileArch(global.loadedFile.FileName))
            {
                case KeystoneMode.KS_MODE_V8:
                    cbAsmLang.SelectedIndex = 0; break;
                case KeystoneMode.KS_MODE_32:
                    cbAsmLang.SelectedIndex = 1; break;
                case KeystoneMode.KS_MODE_64:
                    cbAsmLang.SelectedIndex = 1; break;
                default:
                    cbAsmLang.SelectedIndex = 0; break;
            }

            btnPatch.Enabled = Enabled = true;
            this.Text = Functions.GetTitle(false, true, true);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(cbAsmLang.SelectedIndex)
            {
                case 0:
                    global.keystone = new Keystone(KeystoneArchitecture.KS_ARCH_ARM, KeystoneMode.KS_MODE_ARM);
                    break;
                case 1:
                    global.keystone = new Keystone(KeystoneArchitecture.KS_ARCH_X86, KeystoneMode.KS_MODE_64);
                    break;
                default:
                    break;
            }
        }

        private void applyPatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void defineConstantsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ConstantsForm frm = new ConstantsForm())
            {
                if (frm.ShowDialog() != DialogResult.OK)
                    return;
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            LoadPatch();
        }

        public void LoadPatch()
        {
            string fileName = global.loadedFile.PatchFileName;
            using (OpenFileDialog fo = new OpenFileDialog())
            {
                fo.Filter = global.lang.GetStr("filter_patch_file") + "|*.json";
                fo.Title = global.lang.GetStr("load_patches");
                fo.FileName = global.loadedFile.PatchShortName;

                if (global.loadedFile.PatchFileName != "")
                    fo.InitialDirectory = Path.GetDirectoryName(global.loadedFile.PatchFileName);

                if (fo.ShowDialog() != DialogResult.OK)
                    return;
                fileName = fo.FileName;
            }

            LoadJsonPatch(fileName);
        }

        public bool LoadJsonPatch(string fileName)
        {
            string json = null;
            try
            {
                json = File.ReadAllText(fileName);
                global.patchData = JsonConvert.DeserializeObject<PatchData>(json);

                dataGridView1.Rows.Clear();

                dataGridView1.RowsAdded -= dataGridView1_RowsAdded;
                foreach (Patch patch in global.patchData.Patches)
                    dataGridView1.Rows.Add(patch.Enabled, patch.Name, patch.Address, patch.Value);
                dataGridView1.RowsAdded += dataGridView1_RowsAdded;

                if (dataGridView1.RowCount == 0)
                    dataGridView1.Rows.Add();
                dataGridView1.CurrentCell = dataGridView1[1, 0];

                if (global.patchData.Architecture < cbAsmLang.Items.Count)
                    cbAsmLang.SelectedIndex = global.patchData.Architecture;

                global.patchData.baseAddress = (ulong)Functions.ParseInt(global.patchData.BaseAddress);
            }
            catch
            {
                return false;
            }

            global.loadedFile.PatchFileName = fileName;
            global.loadedFile.PatchShortName = Path.GetFileNameWithoutExtension(fileName);
            this.Text = Functions.GetTitle(false, true, true);
            return true;
        }

        public void SavePatch(bool forceDialog = false)
        {
            dataGridView1.EndEdit();
            string fileName = global.loadedFile.PatchFileName;

            if (forceDialog || global.loadedFile.PatchFileName == "")
            {
                using (SaveFileDialog fs = new SaveFileDialog())
                {
                    fs.Filter = global.lang.GetStr("filter_patch_file") + "|*.json";
                    fs.Title = global.lang.GetStr("save_patches_as");
                    fs.FileName = global.loadedFile.PatchShortName;

                    if (global.loadedFile.PatchFileName != "")
                        fs.InitialDirectory = Path.GetDirectoryName(global.loadedFile.PatchFileName);

                    if (fs.ShowDialog() != DialogResult.OK)
                        return;

                    fileName = fs.FileName;
                }
            }

            SaveJsonPatch(fileName);
        }

        public bool SaveJsonPatch(string fileName)
        {
            if (!Functions.CreateDirectory(Path.GetDirectoryName(fileName)))
                return false;

            List<Patch> tmpPatches = new List<Patch>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[3].Value != null)
                {
                    string address = row.Cells[2].Value != null ? row.Cells[2].Value.ToString().Trim() : "";
                    string value = row.Cells[3].Value.ToString().Trim();

                    if (value == "")
                        continue;

                    Patch newPatch = new Patch()
                    {
                        Enabled = (bool)row.Cells[0].Value,
                        Name = row.Cells[1].Value != null ? row.Cells[1].Value.ToString().Trim() : "",
                        Address = address,
                        Value = value
                    };

                    tmpPatches.Add(newPatch);
                }
            }

            global.patchData.Architecture = cbAsmLang.SelectedIndex;
            global.patchData.Patches = tmpPatches.ToArray();
            string json = JsonConvert.SerializeObject(global.patchData, Formatting.Indented);

            try
            {
                File.WriteAllText(fileName, json);
            }
            catch { return false; }

            global.loadedFile.PatchFileName = fileName;
            global.loadedFile.PatchShortName = Path.GetFileNameWithoutExtension(fileName);
            this.Text = Functions.GetTitle(false, true, true);
            return true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SavePatch();
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            SavePatch(true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutForm frm = new AboutForm())
                frm.ShowDialog();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Save();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var match = tsmiRecents.DropDownItems.Cast<ToolStripItem>().Where(i => i.Name.StartsWith("tsmiRecent"));
            ToolStripItem[] items = match.ToArray();
            for (int i = 0; i < items.Length; i++)
                tsmiRecents.DropDownItems.Remove(items[i]);
            tsmiRecents.Enabled = tsmiRecents.DropDownItems.Count > 2;
        }

        public void recentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            if (!File.Exists(item.ToolTipText))
                return;

            selectExecutable(item.ToolTipText);
        }

        private void tsmiLang_DropDownOpening(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in tsmiLanguage.DropDownItems)
                item.Checked = (item.Tag.ToString().ToLowerInvariant() == global.selectedLang);
        }

        private void ctxmilang_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            global.selectedLang = item.Tag.ToString();
            global.lang = Settings.LoadLanguage(global.selectedLang);
            ApplyLanguage();
        }

        private void tsmiMakeBackups_CheckedChanged(object sender, EventArgs e)
        {
            global.settings.MakeBackups = tsmiMakeBackups.Checked;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                selectExecutable(files[0]);
        }

        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView gridView = (DataGridView)sender;
            if (gridView[e.ColumnIndex, e.RowIndex].Style.BackColor != gridView.DefaultCellStyle.BackColor)
                gridView[e.ColumnIndex, e.RowIndex].Style.BackColor = gridView.DefaultCellStyle.BackColor;
        }
    }
}
