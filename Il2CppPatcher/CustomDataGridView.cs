using System;
using System.Windows.Forms;

public class CustomDataGridView : DataGridView
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter)
        {
            this.EndEdit();
            DataGridViewCell cell = this.CurrentCell;
            int currentCol = cell.ColumnIndex;
            int currentRow = cell.RowIndex;

            if (currentCol < this.ColumnCount - 1)
                this.CurrentCell = this[++currentCol, currentRow];
            else
            {
                bool isMain = this.Tag != null && this.Tag.ToString() == "1";
                bool valid = isMain
                    ? Functions.ValidateRow(ref global.keystone, this, currentRow)
                    : cell.Value != null && cell.Value.ToString().Trim() != "" && this[cell.ColumnIndex - 1, cell.RowIndex].Value != null && this[cell.ColumnIndex - 1, cell.RowIndex].Value.ToString().Trim() != "";

                if (valid)
                {
                    if (this.CurrentRow.Index == this.RowCount -1)
                        this.Rows.Add();
                    this.CurrentCell = this[Convert.ToInt32(isMain), ++currentRow];
                }
            }

            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}