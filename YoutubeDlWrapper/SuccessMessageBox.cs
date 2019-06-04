using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Media;

namespace YoutubeDlWrapper
{
    public partial class SuccessMessageBox : Form
    {
        string _folderPath;

        public SuccessMessageBox(string folderPath)
        {
            InitializeComponent();
            _folderPath = folderPath;
            label1.Text = "Download successful!\n\nFile saved to " + folderPath;
            Width = label1.Width + pictureBox1.Width + 51;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", _folderPath);
            Close();
        }

        private void SuccessMessageBox_Load(object sender, EventArgs e)
        {
            SoundPlayer sp = new SoundPlayer("Resources\\done.wav");
            sp.Play();
        }
    }
}
