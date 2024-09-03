using IL2CPP_EASY_PATCHER.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IL2CPP_EASY_PATCHER
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.icon;
            ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            this.Text = global.lang.GetStr("about");
            button1.Text = global.lang.GetStr("ok");
            groupBox1.Text = global.lang.GetStr("this_program_uses");
            label1.Text = Functions.GetTitle();
            label2.AutoSize = true;
            linkLabel1.Text = "Colmines92";
            linkLabel1.Top = label2.Top;
            linkLabel1.Left = label2.Left + label2.Size.Width - label2.Margin.Right;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Functions.OpenUrl(global.myGithub);
        }

        private void listView1_SizeChanged(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = listView1.Width;
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;
            ListViewItem item = listView1.SelectedItems[0];
            Functions.OpenUrl(item.ToolTipText);
        }
    }
}
