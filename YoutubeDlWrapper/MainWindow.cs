﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace YoutubeDlWrapper
{
    public partial class MainWindow : Form
    {
        static string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + @"\From YouTube";
        string _videoArgs = "--add-metadata -c -o \"" + _folderPath + "\\%(title)s.%(ext)s\" --merge-output-format mkv";
        string _audioArgs = "--add-metadata -c -o %(title)s.%(ext)s -x --audio-format";

        Dictionary<string, string> _audioFormats;
        SuccessMessageBox _success;

        public MainWindow()
        {
            InitializeComponent();
            _success = new SuccessMessageBox(_folderPath);
            tbOutput.Text = "Checking for updates...";
            lblFormat.Visible = false;
            cbFormat.Visible = false;
            pBar.Visible = false;

            if(!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            _audioFormats = new Dictionary<string, string>();
            _audioFormats.Add("AAC", "aac");
            _audioFormats.Add("MP3", "mp3");
            _audioFormats.Add("Ogg Vorbis", "vorbis");
            _audioFormats.Add("Wave", "wav");

            foreach(KeyValuePair<string, string> format in _audioFormats)
            {
                cbFormat.Items.Add(format.Key);
            }

            Process process = new Process();
            process.StartInfo.FileName = "youtube-dl.exe";
            process.StartInfo.Arguments = "--update";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += new DataReceivedEventHandler(ytdlOutputHandler);
            process.Start();
            process.BeginOutputReadLine();
        }

        bool CheckForm(out string message)
        {
            if (string.IsNullOrWhiteSpace(tbAddress.Text))
            {
                message = "No address!";
                return false;
            }

            if (!Regex.Match(tbAddress.Text, @"^(https:\/\/www\.youtube\.com\/watch\?v=).+").Success)
            {
                message = "Invalid address!";
                return false;
            }

            if(cbAudio.Checked)
            {
                if(cbFormat.SelectedItem == null)
                {
                    message = "Select audio format";
                    return false;
                }
            }

            message = "Success!";
            return true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if(!CheckForm(out string message))
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string format = "";
            if (cbAudio.Checked) _audioFormats.TryGetValue(cbFormat.SelectedItem.ToString(), out format);

            tbOutput.Text = string.Empty;
            DisableControls();
            btnStart.Text = "Wait...";
            Process process = new Process();
            process.StartInfo.FileName = "youtube-dl.exe";
            process.StartInfo.Arguments = cbAudio.Checked ? _audioArgs + " " + format + " " + tbAddress.Text : _videoArgs + " " + tbAddress.Text;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += new DataReceivedEventHandler(ytdlOutputHandler);
            process.ErrorDataReceived += (obj, args) => 
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    MessageBox.Show(args.Data, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            process.ErrorDataReceived += new DataReceivedEventHandler(ytdlErrorHandler);
            process.Exited += (obj, args) => 
            {
                if (process.ExitCode == 0)
                {
                    _success.ShowDialog();
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.EnableRaisingEvents = true;
            process.Exited += ytdlExitHandler;
        }

        void ytdlOutputHandler(object sender, DataReceivedEventArgs e)
        {
            Trace.WriteLine(e.Data);
            BeginInvoke(new MethodInvoker(() =>
            {
                tbOutput.Text = string.IsNullOrWhiteSpace(e.Data) ? tbOutput.Text : e.Data;
            }));
        }

        void ytdlErrorHandler(object sender, DataReceivedEventArgs e)
        {
            Trace.WriteLine(e.Data);
            BeginInvoke(new MethodInvoker(() =>
            {
                tbOutput.Text = string.IsNullOrWhiteSpace(e.Data) ? tbOutput.Text : e.Data;
            }));
        }

        void ytdlExitHandler(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                tbOutput.Text = "Ready.";
                btnStart.Text = "Start";
                EnableControls();
                tbAddress.Focus();
            }));
        }

        private void cbAudio_CheckedChanged(object sender, EventArgs e)
        {
            lblFormat.Visible = cbAudio.Checked;
            cbFormat.Visible = cbAudio.Checked;
        }

        void DisableControls()
        {
            btnStart.Enabled = false;
            cbAudio.Enabled = false;
            cbFormat.Enabled = false;
            tbAddress.Enabled = false;
            pBar.Visible = true;
        }

        void EnableControls()
        {
            btnStart.Enabled = true;
            cbAudio.Enabled = true;
            cbFormat.Enabled = true;
            tbAddress.Enabled = true;
            pBar.Visible = false;
        }

        private void pbLogo_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog(this);
        }
    }
}
