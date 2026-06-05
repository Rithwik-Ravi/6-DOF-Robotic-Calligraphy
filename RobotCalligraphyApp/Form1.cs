using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RobotCalligraphyApp.ToolpathEngine;
using RobotCalligraphyApp.CoreNetworking;
using RobotCalligraphyApp.Pipelines_2D;

namespace RobotCalligraphyApp
{
    public partial class Form1 : Form
    {
        // UI Controls
        private TextBox txtInput = null!;
        private NumericUpDown numLetterSize = null!;
        private ComboBox cmbFont = null!;
        private Button btnGenerate = null!;
        private PictureBox picPreview = null!;

        // Network Controls
        private TextBox txtIpAddress = null!;
        private TextBox txtPort = null!;
        private Button btnConnect = null!;
        private Button btnDisconnect = null!;
        private Button btnExecute = null!;
        private Button btnPause = null!;
        private Button btnStop = null!;
        private Label lblConnectionStatus = null!;
        private RobotTcpClient robotClient = new RobotTcpClient();

        // Image Vectorization Controls
        private Button btnLoadImage = null!;
        private Button btnVectorize = null!;
        private TextBox txtImagePath = null!;
        private NumericUpDown numImageWidth = null!;
        private PictureBox picOriginal = null!;
        private TrackBar tbDetail = null!;
        private Label lblDetail = null!;

        // Architecture
        private ToolpathCoordinator coordinator = new ToolpathCoordinator();

        // Execution Control
        private CancellationTokenSource? executionCts;
        private bool isPaused = false;
        private RoboticWaypoint? currentRobotWaypoint = null;
        private Label lblProgress = null!;
        private Label lblETA = null!;
        
        // Data Storage
        private List<RoboticWaypoint> waypoints = new List<RoboticWaypoint>();
        private List<PointF> previewPoints = new List<PointF>();
        private List<byte> previewTypes = new List<byte>();

        // 3D Pipeline Data Storage
        private List<RobotCalligraphyApp.Pipelines_3D.Core.RoboticWaypoint6DOF> waypoints3D = new List<RobotCalligraphyApp.Pipelines_3D.Core.RoboticWaypoint6DOF>();
        private int currentLayerZIndex = 0;
        private List<float> distinctZLayers = new List<float>();

        // 3D Pipeline UI Controls
        private Button btnLoad3D = null!;
        private Button btnSlice3D = null!;
        private TextBox txtStlPath = null!;
        private TrackBar tbLayer3D = null!;
        private Label lblLayer3D = null!;

        // 3D Viewer Controls
        private float orbitYaw = 45f;
        private float orbitPitch = 30f;
        private bool isOrbiting = false;
        private bool isPanning = false;
        private float panOffsetX = 0f;
        private float panOffsetY = 0f;
        private float zoomScale = 3.0f;
        private Point lastMousePos;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                executionCts?.Cancel();
                robotClient?.Disconnect();
            }
            catch { }
            Environment.Exit(0);
            base.OnFormClosing(e);
        }

        private void StyleButton(Button btn, Color backColor, Color foreColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Font = new Font(btn.Font, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        private void InitializeComponent()
        {
            this.Text = "Robot Calligraphy - Industrial Control";
            this.Size = new Size(1200, 900);
            this.MinimumSize = new Size(1200, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.ForeColor = Color.Black;

            Color ctrlBg = Color.White;

            // === TOP PANEL: Network & Global Execution ===
            Panel pnlTop = new Panel() { Location = new Point(0, 0), Size = new Size(1200, 60), BackColor = Color.FromArgb(230, 230, 230), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            
            Label lblIp = new Label() { Text = "IP:", Location = new Point(10, 22), AutoSize = true };
            txtIpAddress = new TextBox() { Location = new Point(35, 19), Width = 110, Text = "192.168.3.20", BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblPort = new Label() { Text = "Port:", Location = new Point(155, 22), AutoSize = true };
            txtPort = new TextBox() { Location = new Point(195, 19), Width = 55, Text = "10003", BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };

            btnConnect = new Button() { Text = "Connect", Location = new Point(260, 14), Width = 80, Height = 30 };
            StyleButton(btnConnect, Color.FromArgb(0, 122, 204), Color.White);
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = new Button() { Text = "Disconnect", Location = new Point(350, 14), Width = 90, Height = 30, Enabled = false };
            StyleButton(btnDisconnect, ctrlBg, Color.Black);
            btnDisconnect.Click += BtnDisconnect_Click;

            btnExecute = new Button() { Text = "Execute", Location = new Point(460, 14), Width = 90, Height = 30, Enabled = false };
            StyleButton(btnExecute, Color.FromArgb(40, 167, 69), Color.White);
            btnExecute.Click += BtnExecute_Click;

            btnPause = new Button() { Text = "Pause", Location = new Point(560, 14), Width = 80, Height = 30, Enabled = false };
            StyleButton(btnPause, Color.FromArgb(255, 193, 7), Color.Black);
            btnPause.Click += BtnPause_Click;

            btnStop = new Button() { Text = "STOP", Location = new Point(650, 14), Width = 70, Height = 30, Enabled = false };
            StyleButton(btnStop, Color.FromArgb(220, 53, 69), Color.White);
            btnStop.Click += BtnStop_Click;

            Button btnExport = new Button() { Text = "Export .prg", Location = new Point(730, 14), Width = 100, Height = 30 };
            StyleButton(btnExport, ctrlBg, Color.Black);
            btnExport.Click += BtnExport_Click;

            lblConnectionStatus = new Label() { Text = "Disconnected", Location = new Point(850, 22), AutoSize = true, ForeColor = Color.FromArgb(220, 53, 69) };
            lblProgress = new Label() { Text = "Progress: 0%", Location = new Point(950, 22), AutoSize = true, ForeColor = Color.FromArgb(0, 122, 204) };
            lblETA = new Label() { Text = "ETA: --:--", Location = new Point(1100, 22), AutoSize = true, ForeColor = Color.FromArgb(0, 122, 204), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            pnlTop.Controls.AddRange(new Control[] { lblIp, txtIpAddress, lblPort, txtPort, btnConnect, btnDisconnect, btnExecute, btnPause, btnStop, btnExport, lblConnectionStatus, lblProgress, lblETA });

            // === 2D CALLIGRAPHY GROUP ===
            GroupBox grp2D = new GroupBox() { Text = "2D Calligraphy & Vectorization", Location = new Point(10, 70), Size = new Size(570, 140), ForeColor = Color.Black };
            
            Label lblInput = new Label() { Text = "Text:", Location = new Point(10, 30), AutoSize = true };
            txtInput = new TextBox() { Location = new Point(50, 27), Width = 230, Height = 40, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG", BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblLetterSize = new Label() { Text = "Size (mm):", Location = new Point(290, 30), AutoSize = true };
            numLetterSize = new NumericUpDown() { Location = new Point(360, 28), Width = 50, DecimalPlaces = 1, Increment = 1m, Minimum = 3m, Maximum = 50m, Value = 8m, BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblFont = new Label() { Text = "Font:", Location = new Point(290, 55), AutoSize = true };
            cmbFont = new ComboBox() { Location = new Point(330, 53), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ctrlBg, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            cmbFont.Items.AddRange(new object[] { "Block", "Rounded", "Italic" }); cmbFont.SelectedIndex = 0;

            btnGenerate = new Button() { Text = "Generate\nPath", Location = new Point(430, 27), Width = 120, Height = 48 };
            StyleButton(btnGenerate, ctrlBg, Color.Black);
            btnGenerate.Click += BtnGenerate_Click;

            btnLoadImage = new Button() { Text = "Load Image", Location = new Point(10, 80), Width = 90, Height = 25 };
            StyleButton(btnLoadImage, ctrlBg, Color.Black);
            btnLoadImage.Click += BtnLoadImage_Click;

            txtImagePath = new TextBox() { Location = new Point(110, 82), Width = 170, ReadOnly = true, BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };

            Label lblImageWidth = new Label() { Text = "Width:", Location = new Point(290, 84), AutoSize = true };
            numImageWidth = new NumericUpDown() { Location = new Point(340, 82), Width = 70, DecimalPlaces = 1, Minimum = 10m, Maximum = 200m, Value = 150m, BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };

            btnVectorize = new Button() { Text = "Vectorize Image", Location = new Point(430, 80), Width = 120, Height = 25, Enabled = false };
            StyleButton(btnVectorize, ctrlBg, Color.Black);
            btnVectorize.Click += BtnVectorize_Click;

            lblDetail = new Label() { Text = "Detail:", Location = new Point(10, 110), AutoSize = true };
            tbDetail = new TrackBar() { Location = new Point(60, 110), Width = 350, Height = 20, Minimum = 1, Maximum = 100, Value = 20, TickFrequency = 10 };
            tbDetail.Scroll += (s, e) => { if (btnVectorize.Enabled && !string.IsNullOrEmpty(txtImagePath.Text)) BtnVectorize_Click(null, EventArgs.Empty); };

            grp2D.Controls.AddRange(new Control[] { lblInput, txtInput, lblLetterSize, numLetterSize, lblFont, cmbFont, btnGenerate, btnLoadImage, txtImagePath, lblImageWidth, numImageWidth, btnVectorize, lblDetail, tbDetail });

            // === 3D PRINTING GROUP ===
            GroupBox grp3D = new GroupBox() { Text = "3D Printing & Slicing", Location = new Point(590, 70), Size = new Size(580, 140), ForeColor = Color.Black, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            btnLoad3D = new Button() { Text = "Load 3D STL", Location = new Point(15, 30), Width = 100, Height = 30 };
            StyleButton(btnLoad3D, ctrlBg, Color.Black);
            btnLoad3D.Click += BtnLoad3D_Click;

            txtStlPath = new TextBox() { Location = new Point(125, 35), Width = 320, ReadOnly = true, BackColor = ctrlBg, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            btnSlice3D = new Button() { Text = "Slice & Parse", Location = new Point(455, 30), Width = 110, Height = 30, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            StyleButton(btnSlice3D, Color.FromArgb(0, 122, 204), Color.White);
            btnSlice3D.Click += BtnSlice3D_Click;

            lblLayer3D = new Label() { Text = "Preview Layer:", Location = new Point(15, 75), AutoSize = true };
            tbLayer3D = new TrackBar() { Location = new Point(15, 95), Width = 550, Minimum = 0, Maximum = 0, Value = 0, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            tbLayer3D.Scroll += (s, e) => { currentLayerZIndex = tbLayer3D.Value; picPreview.Invalidate(); };

            grp3D.Controls.AddRange(new Control[] { btnLoad3D, txtStlPath, btnSlice3D, lblLayer3D, tbLayer3D });

            // === VISUALIZERS ===
            picOriginal = new PictureBox()
            {
                Location = new Point(10, 220),
                Size = new Size(570, 630),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            picOriginal.Paint += PicOriginal_Paint;
            picOriginal.MouseDown += PicOriginal_MouseDown;
            picOriginal.MouseMove += PicOriginal_MouseMove;
            picOriginal.MouseUp += PicOriginal_MouseUp;
            picOriginal.MouseWheel += PicOriginal_MouseWheel;
            picOriginal.MouseDoubleClick += PicOriginal_MouseDoubleClick;
            picOriginal.MouseEnter += PicOriginal_MouseEnter;

            picPreview = new PictureBox() 
            { 
                Location = new Point(590, 220), 
                Size = new Size(580, 630), 
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            picPreview.Paint += PicPreview_Paint;

            // === ADD CONTROLS TO FORM ===
            this.Controls.Add(pnlTop);
            this.Controls.Add(grp2D);
            this.Controls.Add(grp3D);
            this.Controls.Add(picOriginal);
            this.Controls.Add(picPreview);

            this.FormClosing += Form1_FormClosing;
        }

        // =====================================================================
        // NETWORK: Connect / Disconnect
        // =====================================================================

        private void BtnLoadImage_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtImagePath.Text = ofd.FileName;
                    picOriginal.ImageLocation = ofd.FileName;
                    btnVectorize.Enabled = true;

                    // Clear 3D State
                    waypoints3D.Clear();
                    distinctZLayers.Clear();
                    tbLayer3D.Enabled = false;
                    picOriginal.Invalidate();
                    picPreview.Invalidate();
                }
            }
        }

        private void BtnVectorize_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtImagePath.Text) || !System.IO.File.Exists(txtImagePath.Text))
            {
                MessageBox.Show("Please load a valid image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pipeline = new ImageVectorizationPipeline2D
            {
                ImagePath = txtImagePath.Text,
                TargetWidthMm = (float)numImageWidth.Value,
                DetailLevel = tbDetail.Value
            };

            coordinator.SetPipeline(pipeline);
            waypoints = coordinator.GenerateToolpath();

            previewPoints.Clear();
            previewPoints.AddRange(pipeline.PreviewPoints);
            previewTypes.Clear();
            previewTypes.AddRange(pipeline.PreviewTypes);

            picPreview.Invalidate();
            
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate {
                    lblProgress.Text = $"Points: {waypoints.Count} | Estimated Time: {(waypoints.Count * 0.025f):F1}s";
                    lblProgress.ForeColor = Color.DarkViolet;
                });
            }
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;
                lblConnectionStatus.Text = "Connecting...";
                lblConnectionStatus.ForeColor = Color.Orange;

                await robotClient.ConnectAsync(txtIpAddress.Text, int.Parse(txtPort.Text));

                lblConnectionStatus.Text = "Connected";
                lblConnectionStatus.ForeColor = Color.Green;
                btnDisconnect.Enabled = true;
                btnExecute.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblConnectionStatus.Text = "Disconnected";
                lblConnectionStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
            }
        }

        private async void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            await DisconnectRobotAsync();
        }

        private async Task DisconnectRobotAsync()
        {
            if (robotClient.IsConnected)
            {
                try
                {
                    await robotClient.SendAsync("STOP");
                }
                catch { /* Ignore send errors on disconnect */ }
                
                robotClient.Disconnect();
            }

            lblConnectionStatus.Text = "Disconnected";
            lblConnectionStatus.ForeColor = Color.Red;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnExecute.Enabled = false;
        }

        // =====================================================================
        // EXPORT: Generate offline .prg file
        // =====================================================================

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            if (waypoints.Count == 0)
            {
                MessageBox.Show("Please generate a path first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimulationProgram.prg");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine("' Robot Calligraphy Offline Program");
                sw.WriteLine("Ovrd 50"); // Global speed override (fast writing)
                sw.WriteLine("Spd 300"); // Linear speed (300 mm/s)
                // Use standard P3 variable for Home Position to avoid declaration errors
                // Restored the exact physical coordinates and posture flags (7,1048576) 
                // so the real robot doesn't violently unwind its wrist!
                sw.WriteLine("P3 = (+470.00, -945.00, +200.00, +3.13, +0.53, -36.52)(7,0)");
                sw.WriteLine("MOV P3");
                sw.WriteLine("P1 = (+340.00, -1030.00, +164.64, +3.13, +0.53, -36.52)(7,0)");
                sw.WriteLine("CNT 1");
                bool isFirstMove = true;
                foreach (var wp in waypoints)
                {
                    sw.WriteLine("P2 = P1");
                    sw.WriteLine($"P2.X = {wp.X:F2}");
                    sw.WriteLine($"P2.Y = {wp.Y:F2}");
                    sw.WriteLine($"P2.Z = {wp.Z:F2}");
                    
                    if (isFirstMove)
                    {
                        sw.WriteLine("MOV P2"); // Use Joint interpolation for the long transit from home
                        isFirstMove = false;
                    }
                    else
                    {
                        sw.WriteLine("MVS P2");
                    }
                }
                sw.WriteLine("MOV P3");
                sw.WriteLine("END");
            }
            MessageBox.Show($"Exported successfully to:\n{filePath}\n\nYou can copy-paste the contents of this file directly into RT ToolBox3 to see the robot draw everything offline in the 3D Simulator!", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================================
        // EXECUTION: Stream waypoints to robot with Stop/Pause support
        // =====================================================================

        private async void BtnExecute_Click(object? sender, EventArgs e)
        {
            if (!robotClient.IsConnected) return;

            if (waypoints3D.Count > 0)
            {
                await Execute3DPipelineAsync();
            }
            else if (waypoints.Count > 0)
            {
                await Execute2DPipelineAsync();
            }
        }

        private async Task Execute2DPipelineAsync()
        {
            executionCts = new CancellationTokenSource();
            isPaused = false;

            btnExecute.Enabled = false;
            btnGenerate.Enabled = false;
            btnConnect.Enabled = false;
            btnStop.Enabled = true;
            btnPause.Enabled = true;
            btnPause.Text = "Pause";
            
            try
            {
                var token = executionCts.Token;

                // Home Position (matches P3 in RobotListener.prg)
                string? resp = await robotClient.SendHomeAsync();
                if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot did not acknowledge home move. Response: {resp}");

                bool isFirstMove = true;
                int totalPoints = waypoints.Count;
                int pointsExecuted = 0;
                System.Diagnostics.Stopwatch uiSw = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Stopwatch globalSw = System.Diagnostics.Stopwatch.StartNew();

                foreach (var wp in waypoints)
                {
                    // Check for cancellation
                    token.ThrowIfCancellationRequested();

                    // Wait while paused
                    while (isPaused)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(100);
                    }

                    resp = await robotClient.SendWaypointAsync(wp, isFirstMove);
                    
                    if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot streaming interrupted. Response: {resp}");
                    
                    pointsExecuted++;
                    isFirstMove = false;

                    // Update UI (throttled to ~100 FPS)
                    if (uiSw.ElapsedMilliseconds > 100 || pointsExecuted == totalPoints)
                    {
                        currentRobotWaypoint = wp;
                        uiSw.Restart();

                        int bufferSize = RobotCalligraphyApp.CoreNetworking.RobotTcpClient.LookaheadBufferSize;
                        int pointsPhysicallyCompleted = Math.Max(0, pointsExecuted - bufferSize);
                        
                        double msRemaining;
                        if (pointsPhysicallyCompleted > 0)
                        {
                            double avgMsPerPoint = (double)globalSw.ElapsedMilliseconds / pointsPhysicallyCompleted;
                            msRemaining = avgMsPerPoint * (totalPoints - pointsExecuted);
                        }
                        else
                        {
                            msRemaining = 50.0 * (totalPoints - pointsExecuted); // default estimate before buffer fills
                        }

                        TimeSpan timeRemaining = TimeSpan.FromMilliseconds(msRemaining);
                        int pct = (int)((pointsExecuted / (float)totalPoints) * 100);

                        // Ensure we update on the UI thread
                        if (this.IsHandleCreated)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                lblProgress.Text = $"Progress: {pct}% ({pointsExecuted}/{totalPoints})";
                                lblETA.Text = $"ETA: {timeRemaining.ToString(@"mm\:ss")}";
                                picPreview.Invalidate();
                            });
                        }
                    }
                }

                // Return Home
                await robotClient.SendHomeAsync();
                
                MessageBox.Show("Calligraphy sequence finished!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                // Try to send robot home safely after stop
                try
                {
                    if (robotClient.IsConnected)
                    {
                await robotClient.SendHomeAsync();
                    }
                }
                catch { /* Best effort to return home */ }

                MessageBox.Show("Execution stopped by user. Robot returning home.", "Stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Streaming failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                executionCts?.Dispose();
                executionCts = null;
                isPaused = false;
                currentRobotWaypoint = null;
                
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate {
                        lblProgress.Text = "Progress: 0%";
                        lblETA.Text = "ETA: --:--";
                        picPreview.Invalidate();
                    });
                }

                btnStop.Enabled = false;
                btnPause.Enabled = false;
                btnPause.Text = "Pause";
                btnGenerate.Enabled = true;
                btnConnect.Enabled = !robotClient.IsConnected;
                if (robotClient.IsConnected) btnExecute.Enabled = true;
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            executionCts?.Cancel();
            isPaused = false; // Unpause so the execute loop can exit cleanly
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            isPaused = !isPaused;
            btnPause.Text = isPaused ? "Resume" : "Pause";
        }

        // =====================================================================
        // FORM CLOSING
        // =====================================================================

        private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (robotClient.IsConnected)
            {
                e.Cancel = true; // Cancel close to await the disconnect
                await DisconnectRobotAsync();
                
                this.FormClosing -= Form1_FormClosing; // Prevent infinite loop
                this.Close(); // Close again
            }
        }

        // =====================================================================
        // TEXT GENERATION: Multi-line, word-wrapped, justified layout
        // =====================================================================

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            string text = txtInput.Text.ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                picPreview.Invalidate();
                return;
            }

            // Clear 3D State
            waypoints3D.Clear();
            distinctZLayers.Clear();
            tbLayer3D.Enabled = false;
            picOriginal.Image = null;
            picOriginal.Invalidate();

            var pipeline = new TextCalligraphyPipeline2D
            {
                Text = text,
                LetterSizeMm = (float)numLetterSize.Value,
                FontName = cmbFont.SelectedItem?.ToString() ?? "Block"
            };

            coordinator.SetPipeline(pipeline);
            waypoints = coordinator.GenerateToolpath();

            previewPoints.Clear();
            previewPoints.AddRange(pipeline.PreviewPoints);
            previewTypes.Clear();
            previewTypes.AddRange(pipeline.PreviewTypes);

            picPreview.Invalidate();
        }

        private void PicPreview_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Draw working area box (4:3 aspect ratio to match 200x150mm physical workspace)
            float workingAreaWidth = picPreview.Width - 40; // 20px padding on each side
            float workingAreaHeight = workingAreaWidth * (170f / 260f);
            
            if (workingAreaHeight > picPreview.Height - 40)
            {
                workingAreaHeight = picPreview.Height - 40;
                workingAreaWidth = workingAreaHeight * (260f / 170f);
            }

            float offsetX = (picPreview.Width - workingAreaWidth) / 2f;
            float offsetY = (picPreview.Height - workingAreaHeight) / 2f;
            
            // Draw physical boundaries
            g.DrawRectangle(Pens.Black, offsetX, offsetY, workingAreaWidth, workingAreaHeight);
            g.DrawString("Physical Working Area (260x170mm)", this.Font, Brushes.Gray, offsetX, offsetY - 15);

            // Draw 2D Points
            if (previewPoints.Count > 0)
            {
                Pen drawPen = new Pen(Color.Blue, 2f);
                Pen transitPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
                Brush startNodeBrush = Brushes.Green;
                Brush drawNodeBrush = Brushes.Red;

                PointF? lastDrawPoint = null;
                PointF? lastAbsolutePoint = null;

                for (int i = 0; i < previewPoints.Count; i++)
                {
                    float u = previewPoints[i].X;
                    float v = previewPoints[i].Y;
                    PointF p = new PointF(offsetX + u * workingAreaWidth, offsetY + v * workingAreaHeight);
                    byte type = previewTypes[i];

                    if (type == 0) // Start
                    {
                        if (lastAbsolutePoint.HasValue)
                        {
                            g.DrawLine(transitPen, lastAbsolutePoint.Value, p);
                        }
                        g.FillEllipse(startNodeBrush, p.X - 4, p.Y - 4, 8, 8);
                        lastDrawPoint = p;
                    }
                    else if (type == 1) // Line
                    {
                        if (lastDrawPoint.HasValue)
                        {
                            g.DrawLine(drawPen, lastDrawPoint.Value, p);
                        }
                        g.FillEllipse(drawNodeBrush, p.X - 2, p.Y - 2, 4, 4);
                        lastDrawPoint = p;
                    }

                    lastAbsolutePoint = p;
                }
            }

            // Draw 3D Layer
            if (waypoints3D.Count > 0 && distinctZLayers.Count > 0)
            {
                float targetZ = distinctZLayers[currentLayerZIndex];
                lblLayer3D.Text = $"Layer: {currentLayerZIndex + 1}/{distinctZLayers.Count} (Z: {targetZ:F2}mm)";
                
                Pen extPen = new Pen(Color.DarkOrchid, 2f);
                Pen travelPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
                
                PointF? lastP = null;

                foreach (var wp in waypoints3D)
                {
                    float px = offsetX + wp.UV.X * workingAreaWidth;
                    float py = offsetY + wp.UV.Y * workingAreaHeight;
                    PointF p = new PointF(px, py);

                    if (Math.Abs(wp.Z - targetZ) < 0.05f) // Render points on this layer
                    {
                        if (lastP.HasValue)
                        {
                            if (wp.IsExtruding) g.DrawLine(extPen, lastP.Value, p);
                            else g.DrawLine(travelPen, lastP.Value, p);
                        }
                    }
                    
                    lastP = p;
                }
            }

            // Draw Real-time position tracking indicator
            if (currentRobotWaypoint.HasValue)
            {
                float u = currentRobotWaypoint.Value.UV.X;
                float v = currentRobotWaypoint.Value.UV.Y;
                float px = offsetX + u * workingAreaWidth;
                float py = offsetY + v * workingAreaHeight;

                // Draw a prominent orange circle
                g.DrawEllipse(new Pen(Color.DarkOrange, 3f), px - 8, py - 8, 16, 16);
                g.FillEllipse(Brushes.Orange, px - 4, py - 4, 8, 8);
            }
        }

        // =====================================================================
        // 3D PIPELINE: LOAD, SLICE, PRINT
        // =====================================================================

        private void BtnLoad3D_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "STL Files|*.stl;*.obj";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtStlPath.Text = ofd.FileName;
                    btnSlice3D.Enabled = true;
                }
            }
        }

        private async void BtnSlice3D_Click(object? sender, EventArgs e)
        {
            string stlPath = txtStlPath.Text;
            if (string.IsNullOrEmpty(stlPath) || !System.IO.File.Exists(stlPath)) return;

            btnSlice3D.Enabled = false;
            btnLoad3D.Enabled = false;
            lblProgress.Text = "Slicing 3D object...";

            try
            {
                // 1. Slice Headlessly
                var slicer = new RobotCalligraphyApp.Pipelines_3D.Slicing.PrusaSlicerStrategy();
                string gcodePath = await slicer.SliceAsync(stlPath);

                // 2. Parse G-Code statefully
                var parser = new RobotCalligraphyApp.Pipelines_3D.Parsing.GCodeParser();
                waypoints3D.Clear();
                distinctZLayers.Clear();
                
                await Task.Run(() => 
                {
                    foreach (var wp in parser.Parse(gcodePath))
                    {
                        waypoints3D.Add(wp);
                        if (distinctZLayers.Count == 0 || Math.Abs(distinctZLayers[distinctZLayers.Count - 1] - wp.Z) > 0.05f)
                        {
                            if (!distinctZLayers.Contains(wp.Z))
                            {
                                distinctZLayers.Add(wp.Z);
                            }
                        }
                    }
                    distinctZLayers.Sort();
                });

                if (waypoints3D.Count > 0 && distinctZLayers.Count > 0)
                {
                    tbLayer3D.Maximum = distinctZLayers.Count - 1;
                    tbLayer3D.Value = 0;
                    currentLayerZIndex = 0;
                    tbLayer3D.Enabled = true;
                    
                    // Clear 2D waypoints to prioritize 3D
                    waypoints.Clear();
                    previewPoints.Clear();
                    previewTypes.Clear();
                    picOriginal.Image = null; // Clear image if one was loaded

                    picPreview.Invalidate();
                    picOriginal.Invalidate();
                    lblProgress.Text = $"Parsed {waypoints3D.Count} 3D waypoints over {distinctZLayers.Count} layers.";
                    if (robotClient.IsConnected) btnExecute.Enabled = true;
                }
                else
                {
                    lblProgress.Text = "Slicing resulted in 0 points.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"3D Pipeline failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblProgress.Text = "Error during slicing.";
            }
            finally
            {
                btnSlice3D.Enabled = true;
                btnLoad3D.Enabled = true;
            }
        }

        private async Task Execute3DPipelineAsync()
        {
            executionCts = new CancellationTokenSource();
            isPaused = false;

            btnLoad3D.Enabled = false;
            btnSlice3D.Enabled = false;
            btnGenerate.Enabled = false;
            btnVectorize.Enabled = false;
            btnConnect.Enabled = false;
            btnExecute.Enabled = false;
            btnStop.Enabled = true;
            btnPause.Enabled = true;
            btnPause.Text = "Pause";
            
            try
            {
                var token = executionCts.Token;

                string? resp = await robotClient.SendHomeAsync();
                if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot did not acknowledge home move. Response: {resp}");

                bool isFirstMove = true;
                int totalPoints = waypoints3D.Count;
                int pointsExecuted = 0;
                System.Diagnostics.Stopwatch uiSw = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Stopwatch globalSw = System.Diagnostics.Stopwatch.StartNew();

                foreach (var wp in waypoints3D)
                {
                    token.ThrowIfCancellationRequested();

                    while (isPaused)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(100);
                    }

                    // Using our extended TCP Client method
                    resp = await robotClient.SendWaypoint6DOFAsync(wp, isFirstMove);
                    
                    if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot streaming interrupted. Response: {resp}");
                    
                    pointsExecuted++;
                    isFirstMove = false;

                    if (uiSw.ElapsedMilliseconds > 100 || pointsExecuted == totalPoints)
                    {
                        // Re-use currentRobotWaypoint for visualizer marker (downcast to 2D for UV)
                        currentRobotWaypoint = new RoboticWaypoint(wp.X, wp.Y, wp.Z, wp.UV);
                        uiSw.Restart();

                        int bufferSize = RobotCalligraphyApp.CoreNetworking.RobotTcpClient.LookaheadBufferSize;
                        int pointsPhysicallyCompleted = Math.Max(0, pointsExecuted - bufferSize);
                        
                        double msRemaining;
                        if (pointsPhysicallyCompleted > 0)
                        {
                            double avgMsPerPoint = (double)globalSw.ElapsedMilliseconds / pointsPhysicallyCompleted;
                            msRemaining = avgMsPerPoint * (totalPoints - pointsExecuted);
                        }
                        else
                        {
                            msRemaining = 50.0 * (totalPoints - pointsExecuted);
                        }

                        TimeSpan timeRemaining = TimeSpan.FromMilliseconds(msRemaining);
                        int pct = (int)((pointsExecuted / (float)totalPoints) * 100);

                        if (this.IsHandleCreated)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                lblProgress.Text = $"3D Print: {pct}% ({pointsExecuted}/{totalPoints})";
                                lblETA.Text = $"ETA: {timeRemaining.ToString(@"mm\:ss")}";
                                
                                // Automatically sync layer visualizer with execution
                                int zIndex = distinctZLayers.BinarySearch(wp.Z);
                                if (zIndex >= 0 && zIndex != currentLayerZIndex)
                                {
                                    currentLayerZIndex = zIndex;
                                    tbLayer3D.Value = zIndex;
                                }

                                picPreview.Invalidate();
                            });
                        }
                    }
                }

                await robotClient.SendHomeAsync();
                MessageBox.Show("3D Print finished!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                try { if (robotClient.IsConnected) await robotClient.SendHomeAsync(); } catch { }
                MessageBox.Show("Execution stopped by user. Robot returning home.", "Stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Streaming failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                executionCts?.Dispose();
                executionCts = null;
                isPaused = false;
                currentRobotWaypoint = null;
                
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate {
                        lblProgress.Text = "Progress: 0%";
                        lblETA.Text = "ETA: --:--";
                        picPreview.Invalidate();
                    });
                }

                btnStop.Enabled = false;
                btnPause.Enabled = false;
                btnPause.Text = "Pause";
                
                btnGenerate.Enabled = true;
                if (!string.IsNullOrEmpty(txtImagePath.Text)) btnVectorize.Enabled = true;
                btnLoad3D.Enabled = true;
                btnSlice3D.Enabled = !string.IsNullOrEmpty(txtStlPath.Text);
                
                btnConnect.Enabled = !robotClient.IsConnected;
                if (robotClient.IsConnected)
                {
                    btnExecute.Enabled = waypoints.Count > 0 || waypoints3D.Count > 0;
                }
            }
        }

        // =====================================================================
        // 3D VIEWER LOGIC (Orbit Viewer)
        // =====================================================================
        
        private void PicOriginal_MouseDown(object? sender, MouseEventArgs e)
        {
            if (waypoints3D.Count == 0) return;
            if (e.Button == MouseButtons.Left)
            {
                isOrbiting = true;
                lastMousePos = e.Location;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                lastMousePos = e.Location;
            }
        }

        private void PicOriginal_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isOrbiting)
            {
                int dx = e.X - lastMousePos.X;
                int dy = e.Y - lastMousePos.Y;
                
                orbitYaw += dx * 0.5f;
                orbitPitch -= dy * 0.5f;
                
                if (orbitPitch > 89f) orbitPitch = 89f;
                if (orbitPitch < -89f) orbitPitch = -89f;

                lastMousePos = e.Location;
                picOriginal.Invalidate();
            }
            else if (isPanning)
            {
                int dx = e.X - lastMousePos.X;
                int dy = e.Y - lastMousePos.Y;
                
                panOffsetX += dx;
                panOffsetY += dy;
                
                lastMousePos = e.Location;
                picOriginal.Invalidate();
            }
        }

        private void PicOriginal_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) isOrbiting = false;
            if (e.Button == MouseButtons.Middle) isPanning = false;
        }

        private void PicOriginal_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (waypoints3D.Count == 0) return;
            if (e.Delta > 0) zoomScale *= 1.15f;
            else if (e.Delta < 0) zoomScale *= 0.85f;
            
            if (zoomScale < 0.1f) zoomScale = 0.1f;
            if (zoomScale > 50.0f) zoomScale = 50.0f;
            picOriginal.Invalidate();
        }

        private void PicOriginal_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                zoomScale = 3.0f;
                panOffsetX = 0f;
                panOffsetY = 0f;
                orbitYaw = 45f;
                orbitPitch = 30f;
                picOriginal.Invalidate();
            }
        }

        private void PicOriginal_MouseEnter(object? sender, EventArgs e)
        {
            picOriginal.Focus();
        }

        private void PicOriginal_Paint(object? sender, PaintEventArgs e)
        {
            if (waypoints3D.Count == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            // Basic 3D projection parameters
            float centerX = picOriginal.Width / 2f;
            float centerY = picOriginal.Height / 2f;

            // Bounding box center based on robot workspace (adjust based on max bounds)
            float cx = 470.00f;  // Center of X: (340 to 600)
            float cy = -945.00f; // Center of Y: (-1030 to -860)
            float cz = 164.640f; // Base Z

            float yawRad = orbitYaw * (float)Math.PI / 180f;
            float pitchRad = orbitPitch * (float)Math.PI / 180f;

            float cosY = (float)Math.Cos(yawRad);
            float sinY = (float)Math.Sin(yawRad);
            float cosP = (float)Math.Cos(pitchRad);
            float sinP = (float)Math.Sin(pitchRad);

            Pen extPen = new Pen(Color.DeepSkyBlue, 1.5f);
            Pen travelPen = new Pen(Color.FromArgb(100, 150, 150, 150), 1f); // Faint gray

            PointF? lastScreenP = null;

            foreach (var wp in waypoints3D)
            {
                // Translate to center
                float dx = wp.X - cx;
                float dy = wp.Y - cy;
                float dz = wp.Z - cz;

                // Rotate Yaw (around Z axis)
                float x1 = dx * cosY - dy * sinY;
                float y1 = dx * sinY + dy * cosY;
                float z1 = dz;

                // Rotate Pitch (around X axis)
                float x2 = x1;
                float y2 = y1 * cosP - z1 * sinP;
                // float z2 = y1 * sinP + z1 * cosP; // Depth not used for simple orthogonal projection

                // Project to screen
                float sx = centerX + panOffsetX + x2 * zoomScale;
                float sy = centerY + panOffsetY + y2 * zoomScale;
                PointF p = new PointF(sx, sy);

                if (lastScreenP.HasValue)
                {
                    if (wp.IsExtruding) g.DrawLine(extPen, lastScreenP.Value, p);
                    else g.DrawLine(travelPen, lastScreenP.Value, p);
                }

                lastScreenP = p;
            }

            g.DrawString($"Orbit: {orbitYaw:F1}°, {orbitPitch:F1}°", this.Font, Brushes.Black, 10, 10);
        }
    }
}
