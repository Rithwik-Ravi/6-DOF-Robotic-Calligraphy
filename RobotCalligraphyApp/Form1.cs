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

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Robot Calligraphy";
            this.Size = new Size(1200, 820);
            this.StartPosition = FormStartPosition.CenterScreen;

            // === ROW 1: Text Input + Letter Size + Generate ===
            Label lblInput = new Label() { Text = "Text:", Location = new Point(10, 15), AutoSize = true };
            txtInput = new TextBox() 
            { 
                Location = new Point(50, 10), 
                Width = 500, 
                Height = 55, 
                Multiline = true, 
                ScrollBars = ScrollBars.Vertical,
                Text = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG" 
            };

            Label lblLetterSize = new Label() { Text = "Letter Size (mm):", Location = new Point(570, 15), AutoSize = true };
            numLetterSize = new NumericUpDown() 
            { 
                Location = new Point(690, 12), 
                Width = 60, 
                DecimalPlaces = 1, 
                Increment = 1m,
                Minimum = 3m,
                Maximum = 50m,
                Value = 8m 
            };

            Label lblFont = new Label() { Text = "Font:", Location = new Point(760, 15), AutoSize = true };
            cmbFont = new ComboBox()
            {
                Location = new Point(795, 12),
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFont.Items.AddRange(new object[] { "Block", "Rounded", "Italic" });
            cmbFont.SelectedIndex = 0;

            btnGenerate = new Button() { Text = "Generate", Location = new Point(895, 25), Width = 90 };
            btnGenerate.Click += BtnGenerate_Click;

            // === ROW 2: Network + Execution Controls ===
            Label lblIp = new Label() { Text = "IP:", Location = new Point(10, 78), AutoSize = true };
            txtIpAddress = new TextBox() { Location = new Point(30, 75), Width = 110, Text = "192.168.0.20" };
            
            Label lblPort = new Label() { Text = "Port:", Location = new Point(150, 78), AutoSize = true };
            txtPort = new TextBox() { Location = new Point(185, 75), Width = 55, Text = "10003" };

            btnConnect = new Button() { Text = "Connect", Location = new Point(250, 73), Width = 75 };
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = new Button() { Text = "Disconnect", Location = new Point(330, 73), Width = 85, Enabled = false };
            btnDisconnect.Click += BtnDisconnect_Click;

            btnExecute = new Button() { Text = "Execute", Location = new Point(420, 73), Width = 75, Enabled = false };
            btnExecute.Click += BtnExecute_Click;

            btnPause = new Button() { Text = "Pause", Location = new Point(500, 73), Width = 70, Enabled = false };
            btnPause.Click += BtnPause_Click;

            btnStop = new Button() 
            { 
                Text = "STOP", 
                Location = new Point(575, 73), 
                Width = 60, 
                Enabled = false,
                BackColor = Color.FromArgb(255, 180, 180),
                ForeColor = Color.DarkRed,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            btnStop.Click += BtnStop_Click;

            Button btnExport = new Button() { Text = "Export to .prg", Location = new Point(645, 73), Width = 100 };
            btnExport.Click += BtnExport_Click;

            lblConnectionStatus = new Label() { Text = "Disconnected", Location = new Point(755, 78), AutoSize = true, ForeColor = Color.Red };
            lblProgress = new Label() { Text = "Progress: 0%", Location = new Point(850, 78), AutoSize = true, ForeColor = Color.Blue };
            lblETA = new Label() { Text = "ETA: --:--", Location = new Point(980, 78), AutoSize = true, ForeColor = Color.Blue };

            // === ROW 3: Image Vectorization ===
            btnLoadImage = new Button() { Text = "Load Image", Location = new Point(10, 105), Width = 100 };
            btnLoadImage.Click += BtnLoadImage_Click;

            txtImagePath = new TextBox() { Location = new Point(120, 107), Width = 300, ReadOnly = true };

            Label lblImageWidth = new Label() { Text = "Target Width (mm):", Location = new Point(430, 110), AutoSize = true };
            numImageWidth = new NumericUpDown()
            {
                Location = new Point(540, 107),
                Width = 70,
                DecimalPlaces = 1,
                Minimum = 10m,
                Maximum = 200m,
                Value = 150m
            };

            btnVectorize = new Button() { Text = "Vectorize Image", Location = new Point(620, 105), Width = 120, Enabled = false };
            btnVectorize.Click += BtnVectorize_Click;

            lblDetail = new Label() { Text = "Detail Level:", Location = new Point(750, 110), AutoSize = true };
            tbDetail = new TrackBar()
            {
                Location = new Point(830, 105),
                Width = 150,
                Minimum = 1,
                Maximum = 100,
                Value = 20, // Value of 20 = 0.0005 epsilon (original value)
                TickFrequency = 10
            };
            // Dynamically re-vectorize when slider changes to provide real-time preview
            tbDetail.Scroll += (s, e) => { if (btnVectorize.Enabled && !string.IsNullOrEmpty(txtImagePath.Text)) BtnVectorize_Click(null, EventArgs.Empty); };

            // === ORIGINAL IMAGE PICTUREBOX ===
            picOriginal = new PictureBox()
            {
                Location = new Point(10, 140),
                Size = new Size(570, 625),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // === PREVIEW PICTUREBOX ===
            picPreview = new PictureBox() 
            { 
                Location = new Point(590, 140), 
                Size = new Size(580, 625), 
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            picPreview.Paint += PicPreview_Paint;

            // === ADD CONTROLS TO FORM ===
            this.Controls.Add(lblInput);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblLetterSize);
            this.Controls.Add(numLetterSize);
            this.Controls.Add(lblFont);
            this.Controls.Add(cmbFont);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(lblIp);
            this.Controls.Add(txtIpAddress);
            this.Controls.Add(lblPort);
            this.Controls.Add(txtPort);
            this.Controls.Add(btnConnect);
            this.Controls.Add(btnDisconnect);
            this.Controls.Add(btnExecute);
            this.Controls.Add(btnPause);
            this.Controls.Add(btnStop);
            this.Controls.Add(btnExport);
            this.Controls.Add(lblConnectionStatus);
            this.Controls.Add(lblProgress);
            this.Controls.Add(lblETA);
            this.Controls.Add(btnLoadImage);
            this.Controls.Add(txtImagePath);
            this.Controls.Add(lblImageWidth);
            this.Controls.Add(numImageWidth);
            this.Controls.Add(btnVectorize);
            this.Controls.Add(lblDetail);
            this.Controls.Add(tbDetail);
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
                sw.WriteLine("P3 = (-581.59, +773.48, +150.00, +179.47, +0.04, +127.18)(7,1048576)");
                sw.WriteLine("MOV P3");
                sw.WriteLine("P1 = (-506.59, +873.48, +118.00, +179.47, +0.04, +127.18)(7,1048576)");
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
            if (!robotClient.IsConnected || waypoints.Count == 0) return;

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
                
                double emaMsPerPoint = 50.0; // Default assumption to start
                System.Diagnostics.Stopwatch pointSw = new System.Diagnostics.Stopwatch();

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

                    pointSw.Restart();
                    resp = await robotClient.SendWaypointAsync(wp, isFirstMove);
                    pointSw.Stop();
                    
                    if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot streaming interrupted. Response: {resp}");
                    
                    pointsExecuted++;
                    
                    // Update Exponential Moving Average (ignore the first point as it gets an instant ACK)
                    if (pointsExecuted > 1)
                    {
                        double currentMs = pointSw.ElapsedMilliseconds;
                        emaMsPerPoint = (emaMsPerPoint * 0.9) + (currentMs * 0.1);
                    }

                    isFirstMove = false;

                    // Update UI (throttled to ~10 FPS)
                    if (uiSw.ElapsedMilliseconds > 100 || pointsExecuted == totalPoints)
                    {
                        currentRobotWaypoint = wp;
                        uiSw.Restart();

                        double msRemaining = emaMsPerPoint * (totalPoints - pointsExecuted);
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
            float workingAreaHeight = workingAreaWidth * (150f / 200f);
            
            if (workingAreaHeight > picPreview.Height - 40)
            {
                workingAreaHeight = picPreview.Height - 40;
                workingAreaWidth = workingAreaHeight * (200f / 150f);
            }

            float offsetX = (picPreview.Width - workingAreaWidth) / 2f;
            float offsetY = (picPreview.Height - workingAreaHeight) / 2f;
            
            // Draw physical boundaries
            g.DrawRectangle(Pens.Black, offsetX, offsetY, workingAreaWidth, workingAreaHeight);
            g.DrawString("Physical Working Area (200x150mm)", this.Font, Brushes.Gray, offsetX, offsetY - 15);

            if (previewPoints.Count == 0) return;

            Pen drawPen = new Pen(Color.Blue, 2f);
            Pen transitPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
            Brush startNodeBrush = Brushes.Green;
            Brush drawNodeBrush = Brushes.Red;

            PointF? lastDrawPoint = null;
            PointF? lastAbsolutePoint = null;

            for (int i = 0; i < previewPoints.Count; i++)
            {
                // Scale u, v directly into the working area bounds
                float u = previewPoints[i].X;
                float v = previewPoints[i].Y;
                
                PointF p = new PointF(offsetX + u * workingAreaWidth, offsetY + v * workingAreaHeight);
                byte type = previewTypes[i];

                if (type == 0) // Start
                {
                    if (lastAbsolutePoint.HasValue)
                    {
                        // Draw a transit line from the previous stroke's end to this new start
                        g.DrawLine(transitPen, lastAbsolutePoint.Value, p);
                    }
                    
                    // Draw start node
                    g.FillEllipse(startNodeBrush, p.X - 4, p.Y - 4, 8, 8);
                    lastDrawPoint = p;
                }
                else if (type == 1) // Line
                {
                    if (lastDrawPoint.HasValue)
                    {
                        // Draw drawing segment
                        g.DrawLine(drawPen, lastDrawPoint.Value, p);
                    }
                    
                    // Draw segment node
                    g.FillEllipse(drawNodeBrush, p.X - 2, p.Y - 2, 4, 4);
                    lastDrawPoint = p;
                }

                lastAbsolutePoint = p;
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
    }
}
