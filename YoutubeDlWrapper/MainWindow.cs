using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace YoutubeDlWrapper
{
    public partial class MainWindow : Form
    {
        static string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + @"\From YouTube";
        string _videoArgs = "--add-metadata -c -o \"" + _folderPath + "\\%(title)s.%(ext)s\" --merge-output-format mkv";
        string _audioArgs = "--add-metadata -c -o \"" + _folderPath + "\\%(title)s.%(ext)s\" -x --audio-format";

        Dictionary<string, string> _audioFormats;
        SuccessMessageBox _success;
        WebClient webClient;

        public MainWindow()
        {
            InitializeComponent();
            _success = new SuccessMessageBox(_folderPath);
            webClient = new WebClient();

            tbOutput.Text = "Checking for updates...";
            lblFormat.Visible = false;
            cbFormat.Visible = false;
            pBar.Visible = false;

            if(!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            // {GUI name, youtube-dl name}
            _audioFormats = new Dictionary<string, string>
            {
                { "AAC (.aac)", "aac" },
                { "MP3 (.mp3)", "mp3" },
                { "Ogg Vorbis (.ogg)", "vorbis" },
                { "Wave (.wav)", "wav" }
            };

            // Build the drop down menu
            foreach (KeyValuePair<string, string> format in _audioFormats)
            {
                cbFormat.Items.Add(format.Key);
            }

            // Check for youtube-dl updates
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
            DisableControls();
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
            // 2nd handler for Exited, to avoid cross-thread ops
            process.Exited += ytdlExitHandler;
        }

        void ytdlOutputHandler(object sender, DataReceivedEventArgs e)
        {
            Trace.WriteLine(e.Data);
            BeginInvoke(new MethodInvoker(() =>
            {
                tbOutput.Text = string.IsNullOrWhiteSpace(e.Data) ? tbOutput.Text : e.Data;
                if(tbOutput.Text.Contains("[download]"))
                {
                    string wat = Regex.Match(tbOutput.Text, "[0-9]{3}|[0-9]{2}").Value;
                    int.TryParse(wat, out int lol);
                    pBar.Style = ProgressBarStyle.Continuous;
                    if (lol > 100)
                        lol = 100;
                    pBar.Value = lol;
                }
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

        // When the youtube-dl process exits
        void ytdlExitHandler(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                tbOutput.Text = "Ready.";
                btnStart.Text = "Start";
                EnableControls();
            }));
        }

        private void cbAudio_CheckedChanged(object sender, EventArgs e)
        {
            lblFormat.Visible = cbAudio.Checked;
            cbFormat.Visible = cbAudio.Checked;
            if(cbFormat.SelectedIndex < 0)
            {
                cbFormat.SelectedIndex = 1;
            }
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
            tbAddress.Focus();
        }

        private void pbLogo_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            _ = about.ShowDialog(this);
        }

        // Enable controls once youtube-dl updating has finished
        private void timer1_Tick(object sender, EventArgs e)
        {
            if(tbOutput.Text.ToLower().Contains("updated")
                || tbOutput.Text.ToLower().Contains("up-to-date")
                || tbOutput.Text.ToLower().Contains("ready"))
            {
                EnableControls();
                timer1.Stop();
            }
        }

        // Open the 'Videos\From YouTube' folder
        private void btnFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", _folderPath);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            try
            {
                Stream msgStream = webClient.OpenRead("http://www.runedal.dk/test.txt");
                StreamReader msgReader = new StreamReader(msgStream);
                string[] content = msgReader.ReadToEnd().Replace(Environment.NewLine, "\n").Split('\n');
                if (content[0] == "1")
                    MessageBox.Show(content[1], "YouTube Download - Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { /* nothing to do here */ _ = ex; }

            try
            {
                Stream versionStream = webClient.OpenRead("https://raw.githubusercontent.com/balderi/YoutubeDlWrapper/master/YoutubeDlWrapper/version.txt");
                StreamReader versionReader = new StreamReader(versionStream);
                string version = versionReader.ReadToEnd();
                if (Assembly.GetExecutingAssembly().GetName().Version < Version.Parse(version))
                {
                    if (MessageBox.Show("There is a new version available!\nWould you like to download it now?", "YouTube Download - Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        Process.Start("https://github.com/balderi/YoutubeDlWrapper/releases");
                    }
                }
            }
            catch (Exception ex) { /* nothing to do here */ _ = ex; }
        }
    }
}
