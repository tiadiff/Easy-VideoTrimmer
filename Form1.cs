using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace VideoTrimmer;

public partial class Form1 : Form
{
    private Panel leftPanel;
    private Panel rightPanel;
    private Panel bottomPlayerPanel;

    private Label lblTitle;
    private Button btnSelectFile;
    private TextBox txtFilePath;
    private Label lblStartTime;
    private MaskedTextBox txtStartTime;
    private Button btnSetStart;
    private Label lblEndTime;
    private MaskedTextBox txtEndTime;
    private Button btnSetEnd;
    private Button btnTrim;
    private Label lblStatus;
    private ProgressBar progressBar;
    
    private VideoView videoView;
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private Button btnPlayOriginal;
    private Button btnPlayTrimmed;
    private TrackBar tbSeek;
    private Label lblTime;
    private Button btnPlayPause;
    private Button btnStop;
    private System.Windows.Forms.Timer updateTimer;
    private bool _isDragging = false;
    
    private string lastTrimmedVideoPath = "";

    // Colors
    private Color bgDark = Color.FromArgb(30, 30, 30);
    private Color bgPanel = Color.FromArgb(45, 45, 48);
    private Color textLight = Color.FromArgb(240, 240, 240);
    private Color accentPrimary = Color.FromArgb(100, 149, 237); // CornflowerBlue
    private Color accentSecondary = Color.FromArgb(232, 65, 24); // Alizarin
    private Color btnBg = Color.FromArgb(62, 62, 66);
    private Color btnHover = Color.FromArgb(85, 85, 90);

    public Form1()
    {
        InitializeComponentManual();
        Core.Initialize();
        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        videoView.MediaPlayer = _mediaPlayer;

        updateTimer = new System.Windows.Forms.Timer { Interval = 250 };
        updateTimer.Tick += UpdateTimer_Tick;
        updateTimer.Start();
    }

    private void StyleButton(Button btn, Color? backColor = null, Color? foreColor = null)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = backColor ?? btnBg;
        btn.ForeColor = foreColor ?? textLight;
        btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        
        btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = btnHover; };
        btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = backColor ?? btnBg; };
        btn.EnabledChanged += (s, e) => { 
            if (!btn.Enabled) btn.BackColor = Color.FromArgb(80, 80, 80); 
            else btn.BackColor = backColor ?? btnBg; 
        };
    }

    private void StyleTextBox(TextBoxBase txt)
    {
        txt.BackColor = bgDark;
        txt.ForeColor = textLight;
        txt.BorderStyle = BorderStyle.FixedSingle;
        txt.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
    }

    private void InitializeComponentManual()
    {
        this.Text = "Video Trimmer Pro";
        this.Size = new Size(1100, 680);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = bgDark;
        this.ForeColor = textLight;
        this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        this.FormClosed += Form1_FormClosed;

        // 1. Right Panel (Fill) - MUST be added first so Left Panel can dock properly beside it
        rightPanel = new Panel() { Dock = DockStyle.Fill, BackColor = Color.Black };
        this.Controls.Add(rightPanel);

        // 2. Left Panel (Left) - Added second so it docks to the left edge of the Form
        leftPanel = new Panel() { Dock = DockStyle.Left, Width = 380, BackColor = bgPanel, Padding = new Padding(20) };
        this.Controls.Add(leftPanel);

        // Left Panel Controls
        lblTitle = new Label() { Text = "✨ Video Trimmer", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = accentPrimary, AutoSize = true, Location = new Point(20, 20) };
        leftPanel.Controls.Add(lblTitle);

        Label lblStep1 = new Label() { Text = "1. File Sorgente", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 80) };
        leftPanel.Controls.Add(lblStep1);

        btnSelectFile = new Button() { Text = "Sfoglia...", Location = new Point(20, 115), Size = new Size(90, 28) };
        StyleButton(btnSelectFile);
        btnSelectFile.Click += BtnSelectFile_Click;
        leftPanel.Controls.Add(btnSelectFile);

        txtFilePath = new TextBox() { Location = new Point(120, 116), Size = new Size(240, 25), ReadOnly = true };
        StyleTextBox(txtFilePath);
        leftPanel.Controls.Add(txtFilePath);

        Label lblStep2 = new Label() { Text = "2. Impostazioni Taglio", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 175) };
        leftPanel.Controls.Add(lblStep2);

        lblStartTime = new Label() { Text = "Inizio", Location = new Point(20, 215), Size = new Size(60, 20) };
        leftPanel.Controls.Add(lblStartTime);
        txtStartTime = new MaskedTextBox("00:00:00") { Location = new Point(80, 212), Size = new Size(70, 25) };
        StyleTextBox(txtStartTime);
        txtStartTime.Text = "00:00:00";
        leftPanel.Controls.Add(txtStartTime);
        
        btnSetStart = new Button() { Text = "Prendi da Video", Location = new Point(160, 211), Size = new Size(200, 27) };
        StyleButton(btnSetStart);
        btnSetStart.Click += BtnSetStart_Click;
        leftPanel.Controls.Add(btnSetStart);

        lblEndTime = new Label() { Text = "Fine", Location = new Point(20, 260), Size = new Size(60, 20) };
        leftPanel.Controls.Add(lblEndTime);
        txtEndTime = new MaskedTextBox("00:00:00") { Location = new Point(80, 257), Size = new Size(70, 25) };
        StyleTextBox(txtEndTime);
        txtEndTime.Text = "00:00:10";
        leftPanel.Controls.Add(txtEndTime);
        
        btnSetEnd = new Button() { Text = "Prendi da Video", Location = new Point(160, 256), Size = new Size(200, 27) };
        StyleButton(btnSetEnd);
        btnSetEnd.Click += BtnSetEnd_Click;
        leftPanel.Controls.Add(btnSetEnd);

        btnTrim = new Button() { Text = "✂ RITAGLIA", Location = new Point(20, 320), Size = new Size(340, 50), Font = new Font("Segoe UI", 13F, FontStyle.Bold) };
        StyleButton(btnTrim, accentSecondary, Color.White);
        btnTrim.Click += BtnTrim_Click;
        leftPanel.Controls.Add(btnTrim);

        progressBar = new ProgressBar() { Location = new Point(20, 390), Size = new Size(340, 10), Style = ProgressBarStyle.Continuous };
        leftPanel.Controls.Add(progressBar);

        lblStatus = new Label() { Text = "Pronto", Location = new Point(20, 410), Size = new Size(340, 40), ForeColor = Color.LightGray };
        leftPanel.Controls.Add(lblStatus);

        Label lblStep3 = new Label() { Text = "3. Azioni Rapide", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 470) };
        leftPanel.Controls.Add(lblStep3);

        btnPlayOriginal = new Button() { Text = "▶ Video Originale", Location = new Point(20, 510), Size = new Size(165, 40) };
        StyleButton(btnPlayOriginal);
        btnPlayOriginal.Click += BtnPlayOriginal_Click;
        leftPanel.Controls.Add(btnPlayOriginal);

        btnPlayTrimmed = new Button() { Text = "▶ Video Ritagliato", Location = new Point(195, 510), Size = new Size(165, 40), Enabled = false };
        StyleButton(btnPlayTrimmed, Color.SeaGreen, Color.White);
        btnPlayTrimmed.Click += BtnPlayTrimmed_Click;
        leftPanel.Controls.Add(btnPlayTrimmed);

        // Right Panel Structure
        // 1. videoView (Fill) - MUST be added first to rightPanel
        videoView = new VideoView() { Dock = DockStyle.Fill, BackColor = Color.Black };
        rightPanel.Controls.Add(videoView);

        // 2. bottomPlayerPanel (Bottom) - Added second so it docks to the bottom edge of rightPanel
        bottomPlayerPanel = new Panel() { Dock = DockStyle.Bottom, Height = 90, BackColor = bgDark };
        rightPanel.Controls.Add(bottomPlayerPanel);

        // Controls inside bottomPlayerPanel
        tbSeek = new TrackBar() { Location = new Point(15, 5), Width = 670, Maximum = 10000, TickStyle = TickStyle.None, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        tbSeek.MouseDown += (s, e) => _isDragging = true;
        tbSeek.MouseUp += (s, e) => {
            _isDragging = false;
            if (_mediaPlayer != null) {
                _mediaPlayer.Position = (float)tbSeek.Value / tbSeek.Maximum;
            }
        };
        bottomPlayerPanel.Controls.Add(tbSeek);

        btnPlayPause = new Button() { Text = "⏸ Pausa", Location = new Point(20, 40), Size = new Size(100, 35) };
        StyleButton(btnPlayPause, accentPrimary, Color.White);
        btnPlayPause.Click += BtnPlayPause_Click;
        bottomPlayerPanel.Controls.Add(btnPlayPause);

        btnStop = new Button() { Text = "⏹ Stop", Location = new Point(130, 40), Size = new Size(100, 35) };
        StyleButton(btnStop);
        btnStop.Click += (s, e) => _mediaPlayer.Stop();
        bottomPlayerPanel.Controls.Add(btnStop);

        lblTime = new Label() { Text = "00:00:00 / 00:00:00", Location = new Point(250, 47), Size = new Size(220, 20), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        bottomPlayerPanel.Controls.Add(lblTime);
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaPlayer == null) return;

        if (_mediaPlayer.IsPlaying)
        {
            btnPlayPause.Text = "⏸ Pausa";
        }
        else
        {
            btnPlayPause.Text = "▶ Play";
        }

        long time = _mediaPlayer.Time;
        long length = _mediaPlayer.Length;

        if (length > 0)
        {
            if (!_isDragging)
            {
                float pos = _mediaPlayer.Position;
                if (pos >= 0 && pos <= 1)
                {
                    tbSeek.Value = (int)(pos * tbSeek.Maximum);
                }
            }

            TimeSpan tsTime = TimeSpan.FromMilliseconds(time >= 0 ? time : 0);
            TimeSpan tsLen = TimeSpan.FromMilliseconds(length);
            lblTime.Text = $"{tsTime:hh\\:mm\\:ss} / {tsLen:hh\\:mm\\:ss}";
        }
    }

    private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
    {
        updateTimer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }

    private void BtnSelectFile_Click(object? sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Video MP4 (*.mp4)|*.mp4|Tutti i file (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                PlayVideo(txtFilePath.Text);
            }
        }
    }

    private void BtnPlayOriginal_Click(object? sender, EventArgs e)
    {
        if (File.Exists(txtFilePath.Text))
        {
            PlayVideo(txtFilePath.Text);
        }
    }

    private void BtnPlayTrimmed_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(lastTrimmedVideoPath) && File.Exists(lastTrimmedVideoPath))
        {
            PlayVideo(lastTrimmedVideoPath);
        }
    }

    private void BtnPlayPause_Click(object? sender, EventArgs e)
    {
        if (_mediaPlayer == null) return;
        
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    private void BtnSetStart_Click(object? sender, EventArgs e)
    {
        if (_mediaPlayer != null && _mediaPlayer.Time > 0)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            txtStartTime.Text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    private void BtnSetEnd_Click(object? sender, EventArgs e)
    {
        if (_mediaPlayer != null && _mediaPlayer.Time > 0)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            txtEndTime.Text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    private void PlayVideo(string path)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            using (var media = new Media(_libVLC, new Uri(path)))
            {
                _mediaPlayer.Play(media);
            }
        });
    }

    private async void BtnTrim_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
        {
            MessageBox.Show("Seleziona un file video valido.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TimeSpan.TryParse(txtStartTime.Text, out TimeSpan startTime) || 
            !TimeSpan.TryParse(txtEndTime.Text, out TimeSpan endTime))
        {
            MessageBox.Show("Formato tempo non valido.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (startTime >= endTime)
        {
            MessageBox.Show("Il tempo di inizio deve essere minore del tempo di fine.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string inputPath = txtFilePath.Text;
        string outputPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? "", Path.GetFileNameWithoutExtension(inputPath) + "_trimmed_" + DateTime.Now.Ticks + ".mp4");

        try
        {
            btnTrim.Enabled = false;
            progressBar.Value = 0;
            lblStatus.Text = "Scaricamento di FFmpeg (se necessario)...";

            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            FFmpeg.SetExecutablesPath(Environment.CurrentDirectory);

            lblStatus.Text = "Ritaglio in corso...";
            TimeSpan duration = endTime - startTime;

            IConversion conversion = await FFmpeg.Conversions.FromSnippet.Split(inputPath, outputPath, startTime, duration);
            
            conversion.OnProgress += (s, args) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    int percent = args.Percent;
                    if (percent > 100) percent = 100;
                    if (percent < 0) percent = 0;
                    progressBar.Value = percent;
                    lblStatus.Text = $"Ritaglio in corso... {percent}%";
                });
            };

            await conversion.Start();

            lblStatus.Text = "Completato!";
            lastTrimmedVideoPath = outputPath;
            btnPlayTrimmed.Enabled = true;
            PlayVideo(outputPath);
            MessageBox.Show($"Video salvato in:\n{outputPath}\nL'anteprima del video ritagliato è ora in riproduzione.", "Completato", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Errore";
            MessageBox.Show($"Errore durante il ritaglio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTrim.Enabled = true;
            progressBar.Value = 0;
        }
    }
}
