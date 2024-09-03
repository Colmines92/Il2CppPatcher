using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IL2CPP_EASY_PATCHER
{
    public partial class ConstantsForm : Form
    {
        public ConstantsForm()
        {
            InitializeComponent();
        }

        private void ConstantsForm_Load(object sender, EventArgs e)
        {
            string strVal = "0x" + global.patchData.baseAddress.ToString("X");
            txtAddress.Text = Functions.ValidateInt(strVal, true, 8);

            foreach (KeyValuePair<string, string> pair in global.patchData.Constants)
            {
                string key = pair.Key;
                if (pair.Key.Trim() != "" && !pair.Key.StartsWith("@"))
                    key = "@" + key;
                gridView.Rows.Add(key, pair.Value);
            }

            if (gridView.RowCount == 0)
                gridView.Rows.Add();
            gridView.CurrentCell = gridView[0, 0];

            ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            // Title
            this.Text = global.lang.GetStr("define_constants_title");

            // File specifics
            gbFileSpecifics.Text = global.lang.GetStr("file_specific");
            lbBaseAddress.Text = global.lang.GetStr("base_address");

            // List buttons
            btnAdd.Text = global.lang.GetStr("add_entry");
            btnRemove.Text = global.lang.GetStr("remove_entry");
            btnClear.Text = global.lang.GetStr("clear_entries");

            // List columns
            gridView.Columns[0].HeaderText = global.lang.GetStr("name");
            gridView.Columns[1].HeaderText = global.lang.GetStr("value");
            
            // Buttons
            btOK.Text = global.lang.GetStr("ok");
            btCancel.Text = global.lang.GetStr("cancel");
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            txtAddress.Text = Functions.ValidateInt(txtAddress.Text, true, 8);
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            gridView.Rows.Add();
            gridView.CurrentCell = gridView[1, 0];
        }

        private void gridView_KeyDown(object sender, KeyEventArgs e)
        {
            CustomDataGridView dataGridView = sender as CustomDataGridView;
            if (dataGridView == null || dataGridView.SelectedCells.Count == 0)
                return;

            if (e.Control && e.KeyCode == Keys.C) // Copiar
            {
                DataObject dataObj = dataGridView.GetClipboardContent();
                if (dataObj != null)
                    Clipboard.SetDataObject(dataObj);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V) // Pegar
            {
                string clipboardText = Clipboard.GetText();
                string[] lines = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int rowIndex = dataGridView.SelectedCells[0].RowIndex;
                int colIndex = dataGridView.SelectedCells[0].ColumnIndex;

                foreach (string line in lines)
                {
                    string[] cells = line.Split('\t');
                    for (int i = 0; i < cells.Length; i++)
                    {
                        if (colIndex + i < dataGridView.ColumnCount)
                        {
                            if (rowIndex >= dataGridView.RowCount - 1)
                                dataGridView.Rows.Add();
                            dataGridView[colIndex + i, rowIndex].Value = cells[i];
                        }
                    }
                    rowIndex++;
                    if (rowIndex >= dataGridView.RowCount)
                        break;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete) // Eliminar
            {
                Functions.DeleteEntry(ref dataGridView);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back) // Borrar
            {
                dataGridView[dataGridView.SelectedCells[0].ColumnIndex, dataGridView.SelectedCells[0].RowIndex].Value = "";
                e.Handled = true;
            }
            else if (e.Shift && e.KeyCode == Keys.Tab)
            {
                int currentCol = dataGridView.CurrentCell.ColumnIndex;
                int currentRow = dataGridView.CurrentCell.RowIndex;
                if (currentRow > 0)
                    dataGridView.CurrentCell = dataGridView[0, --currentRow];
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                int currentCol = dataGridView.CurrentCell.ColumnIndex;
                int currentRow = dataGridView.CurrentCell.RowIndex;
                if (currentRow < dataGridView.RowCount - 1)
                    dataGridView.CurrentCell = dataGridView[0, ++currentRow];
                e.Handled = true;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Functions.AddEntry(ref gridView);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            Functions.DeleteEntry(ref gridView);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            Functions.ClearEntries(ref gridView);
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            textBox1_Leave(sender, e);
            global.patchData.BaseAddress = txtAddress.Text;
            global.patchData.baseAddress = (ulong)Functions.ParseInt(txtAddress.Text);

            gridView.EndEdit();
            global.patchData.Constants.Clear();

            foreach(DataGridViewRow row in gridView.Rows)
            {
                if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                {
                    string key = row.Cells[0].Value.ToString().Trim();
                    string value = row.Cells[1].Value.ToString().Trim();

                    if (key == "" || value == "")
                        continue;

                    if (global.patchData.Constants.ContainsKey(key))
                        global.patchData.Constants[key] = value;
                    else
                        global.patchData.Constants.Add(key, value);
                }
            }
        }

        private void gridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            CustomDataGridView gridView = (CustomDataGridView)sender;
            
            if (e.ColumnIndex == 0)
            {
                DataGridViewCell cell = gridView[e.ColumnIndex, e.RowIndex];
                if (cell.Value != null)
                {
                    string key = cell.Value.ToString().Trim();
                    if (key != "" && !key.StartsWith("@"))
                        cell.Value = "@" + key;
                }
            }
        }
    }
}
