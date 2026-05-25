using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobotCalligraphyApp
{
    public struct RoboticWaypoint
    {
        public float X;
        public float Y;
        public float Z;

        public RoboticWaypoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"X:{X:F2}, Y:{Y:F2}, Z:{Z:F2}";
        }
    }

    public partial class Form1 : Form
    {
        // UI Controls
        private TextBox txtInput = null!;
        private NumericUpDown numScale = null!;
        private NumericUpDown numFlatness = null!;
        private Button btnGenerate = null!;
        private PictureBox picPreview = null!;

        // Network Controls
        private TextBox txtIpAddress = null!;
        private TextBox txtPort = null!;
        private Button btnConnect = null!;
        private Button btnDisconnect = null!;
        private Button btnExecute = null!;
        private Label lblConnectionStatus = null!;
        private RobotTcpClient robotClient = new RobotTcpClient();
        
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
            this.Text = "Robot Calligraphy PoC";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Input TextBox
            Label lblInput = new Label() { Text = "Text:", Location = new Point(10, 15), AutoSize = true };
            txtInput = new TextBox() { Location = new Point(50, 12), Width = 300, Text = "HELLO" };

            // Scale NumericUpDown
            Label lblScale = new Label() { Text = "Scale:", Location = new Point(370, 15), AutoSize = true };
            numScale = new NumericUpDown() 
            { 
                Location = new Point(410, 12), 
                Width = 80, 
                DecimalPlaces = 2, 
                Increment = 0.1m,
                Minimum = 0.01m,
                Maximum = 100m,
                Value = 0.5m 
            };

            // Flatness Tolerance NumericUpDown
            Label lblFlatness = new Label() { Text = "Flatness Tolerance:", Location = new Point(510, 15), AutoSize = true };
            numFlatness = new NumericUpDown() 
            { 
                Location = new Point(620, 12), 
                Width = 80, 
                DecimalPlaces = 2, 
                Increment = 0.05m,
                Minimum = 0.1m, 
                Maximum = 10m,
                Value = 0.25m 
            };

            // Generate Button
            btnGenerate = new Button() { Text = "Generate", Location = new Point(720, 10), Width = 100 };
            btnGenerate.Click += BtnGenerate_Click;

            // Network Controls
            Label lblIp = new Label() { Text = "IP:", Location = new Point(10, 48), AutoSize = true };
            txtIpAddress = new TextBox() { Location = new Point(40, 45), Width = 120, Text = "192.168.0.20" };
            
            Label lblPort = new Label() { Text = "Port:", Location = new Point(170, 48), AutoSize = true };
            txtPort = new TextBox() { Location = new Point(210, 45), Width = 60, Text = "10003" };

            btnConnect = new Button() { Text = "Connect", Location = new Point(280, 43), Width = 80 };
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = new Button() { Text = "Disconnect", Location = new Point(370, 43), Width = 80, Enabled = false };
            btnDisconnect.Click += BtnDisconnect_Click;

            btnExecute = new Button() { Text = "Execute", Location = new Point(460, 43), Width = 80, Enabled = false };
            btnExecute.Click += BtnExecute_Click;

            Button btnExport = new Button() { Text = "Export to .prg", Location = new Point(550, 43), Width = 100 };
            btnExport.Click += BtnExport_Click;

            lblConnectionStatus = new Label() { Text = "Disconnected", Location = new Point(660, 48), AutoSize = true, ForeColor = Color.Red };

            // Preview PictureBox
            picPreview = new PictureBox() 
            { 
                Location = new Point(10, 80), 
                Size = new Size(1160, 670), 
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            picPreview.Paint += PicPreview_Paint;

            // Add Controls to Form
            this.Controls.Add(lblInput);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblScale);
            this.Controls.Add(numScale);
            this.Controls.Add(lblFlatness);
            this.Controls.Add(numFlatness);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(lblIp);
            this.Controls.Add(txtIpAddress);
            this.Controls.Add(lblPort);
            this.Controls.Add(txtPort);
            this.Controls.Add(btnConnect);
            this.Controls.Add(btnDisconnect);
            this.Controls.Add(btnExecute);
            this.Controls.Add(btnExport);
            this.Controls.Add(lblConnectionStatus);
            this.Controls.Add(picPreview);

            this.FormClosing += Form1_FormClosing;
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

        private Dictionary<char, List<PointF[]>> singleStrokeFont = new Dictionary<char, List<PointF[]>>()
        {
            {'A', new List<PointF[]> { new PointF[] { new PointF(0, 1), new PointF(0.5f, 0), new PointF(1, 1) }, new PointF[] { new PointF(0.25f, 0.5f), new PointF(0.75f, 0.5f) } }},
            {'B', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0,0), new PointF(0.8f,0), new PointF(1,0.25f), new PointF(0.8f,0.5f), new PointF(0,0.5f) }, new PointF[] { new PointF(0.8f,0.5f), new PointF(1,0.75f), new PointF(0.8f,1), new PointF(0,1) } }},
            {'C', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,1), new PointF(1,1) } }},
            {'D', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0,0), new PointF(0.8f,0), new PointF(1,0.5f), new PointF(0.8f,1), new PointF(0,1) } }},
            {'E', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,1), new PointF(1,1) }, new PointF[] { new PointF(0,0.5f), new PointF(0.8f,0.5f) } }},
            {'F', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0,0.5f), new PointF(0.8f,0.5f) } }},
            {'G', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,1), new PointF(1,1), new PointF(1,0.5f), new PointF(0.5f,0.5f) } }},
            {'H', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(1,0), new PointF(1,1) }, new PointF[] { new PointF(0,0.5f), new PointF(1,0.5f) } }},
            {'I', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.5f,1) }, new PointF[] { new PointF(0,0), new PointF(1,0) }, new PointF[] { new PointF(0,1), new PointF(1,1) } }},
            {'J', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(1,0.8f), new PointF(0.8f,1), new PointF(0.2f,1), new PointF(0,0.8f), new PointF(0,0.5f) } }},
            {'K', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(1,0), new PointF(0,0.5f) }, new PointF[] { new PointF(0.3f,0.5f), new PointF(1,1) } }},
            {'L', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1), new PointF(1,1) } }},
            {'M', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(0.5f,0.5f), new PointF(1,0), new PointF(1,1) } }},
            {'N', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(1,1), new PointF(1,0) } }},
            {'O', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(1,1), new PointF(0,1), new PointF(0,0) } }},
            {'P', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(1,0), new PointF(1,0.5f), new PointF(0,0.5f) } }},
            {'Q', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(1,1), new PointF(0,1), new PointF(0,0) }, new PointF[] { new PointF(0.5f,0.5f), new PointF(1,1) } }},
            {'R', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(1,0), new PointF(1,0.5f), new PointF(0,0.5f) }, new PointF[] { new PointF(0.3f,0.5f), new PointF(1,1) } }},
            {'S', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,0.5f), new PointF(1,0.5f), new PointF(1,1), new PointF(0,1) } }},
            {'T', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0) }, new PointF[] { new PointF(0.5f,0), new PointF(0.5f,1) } }},
            {'U', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1), new PointF(1,1), new PointF(1,0) } }},
            {'V', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.5f,1), new PointF(1,0) } }},
            {'W', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.2f,1), new PointF(0.5f,0.5f), new PointF(0.8f,1), new PointF(1,0) } }},
            {'X', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,1) }, new PointF[] { new PointF(1,0), new PointF(0,1) } }},
            {'Y', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.5f,0.5f), new PointF(1,0) }, new PointF[] { new PointF(0.5f,0.5f), new PointF(0.5f,1) } }},
            {'Z', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(0,1), new PointF(1,1) } }},
            {' ', new List<PointF[]> { } }
        };

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
                sw.WriteLine("Ovrd 20"); // Global speed override
                sw.WriteLine("Spd 100"); // Linear speed (100 mm/s)
                // Use standard P3 variable for Home Position to avoid declaration errors
                // Restored the exact physical coordinates and posture flags (7,1048576) 
                // so the real robot doesn't violently unwind its wrist!
                sw.WriteLine("P3 = (-586.18, +783.00, +182.01, +177.94, +0.35, +119.72)(7,1048576)");
                sw.WriteLine("MOV P3");
                sw.WriteLine("P1 = (-643.160, +866.620, +117.830, +177.940, +0.350, +119.720)(7,1048576)");
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

        private async void BtnExecute_Click(object? sender, EventArgs e)
        {
            if (!robotClient.IsConnected || waypoints.Count == 0) return;
            btnExecute.Enabled = false;
            foreach (var wp in waypoints)
            {
                string pt = $"{wp.X:F2};{wp.Y:F2};{wp.Z:F2}";
                try
                {
                    string? resp = await robotClient.SendAsync(pt);
                    if (resp?.Trim() != "ACK") break;
                }
                catch
                {
                    break;
                }
            }
            if (robotClient.IsConnected) btnExecute.Enabled = true;
        }

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

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            waypoints.Clear();
            previewPoints.Clear();
            previewTypes.Clear();

            string text = txtInput.Text.ToUpper();
            if (string.IsNullOrWhiteSpace(text))
            {
                picPreview.Invalidate();
                return;
            }

            // Physical Constraints
            float drawZ = 126.55f; // Average of the 4 Z heights
            float transitZ = 160.0f; // Safe height
            
            // Quad Corners (p1a is TopLeft, p2a is TopRight, p4a is BottomLeft)
            // Restored the exact physical coordinates for the real robot!
            PointF p1a = new PointF(-643.160f, 866.620f);
            PointF p2a = new PointF(-500.220f, 891.050f);
            PointF p4a = new PointF(-658.410f, 677.510f);

            // Text Layout Config
            float letterWidth = 1.0f;
            float letterSpacing = 0.4f;
            float totalWidth = (text.Length * letterWidth) + ((text.Length - 1) * letterSpacing);

            // Aspect Ratio Preservation Math
            float magW = (float)Math.Sqrt(Math.Pow(p2a.X - p1a.X, 2) + Math.Pow(p2a.Y - p1a.Y, 2));
            float magH = (float)Math.Sqrt(Math.Pow(p4a.X - p1a.X, 2) + Math.Pow(p4a.Y - p1a.Y, 2));
            float S = Math.Min(magW / totalWidth, magH / 1.0f);
            
            float scaleU = (totalWidth * S) / magW;
            float scaleV = (1.0f * S) / magH;
            
            // Center the text in the unused space of the box
            float offsetU = (1.0f - scaleU) / 2.0f;
            float offsetV = (1.0f - scaleV) / 2.0f;

            // Iterate over characters and build normalized [0,1] bounding box strokes
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!singleStrokeFont.ContainsKey(c)) continue;

                float offsetX = i * (letterWidth + letterSpacing);
                
                foreach (var stroke in singleStrokeFont[c])
                {
                    for (int pt = 0; pt < stroke.Length; pt++)
                    {
                        // Normalize X and Y to [0,1] where 1 is the full width of the word
                        float rawU = (offsetX + stroke[pt].X * letterWidth) / totalWidth;
                        float rawV = stroke[pt].Y; // Y is already 0 to 1

                        // Apply Aspect Ratio Scaling
                        float u = offsetU + (rawU * scaleU);
                        float v = offsetV + (rawV * scaleV);

                        // Affine Mapping to physical table
                        float xRobot = p1a.X + u * (p2a.X - p1a.X) + v * (p4a.X - p1a.X);
                        float yRobot = p1a.Y + u * (p2a.Y - p1a.Y) + v * (p4a.Y - p1a.Y);

                        if (pt == 0) // Start of stroke (Transit down)
                        {
                            waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ));
                            waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ));
                            previewTypes.Add(0); // Start
                        }
                        else
                        {
                            waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ));
                            previewTypes.Add(1); // Line
                        }
                        
                        // For 2D preview (just standard scaling for visualization)
                        previewPoints.Add(new PointF(u * 800f, v * 800f));
                    }
                    
                    // After stroke finishes, lift up
                    if (stroke.Length > 0)
                    {
                        var lastPt = stroke[stroke.Length - 1];
                        float rawU = (offsetX + lastPt.X * letterWidth) / totalWidth;
                        float rawV = lastPt.Y;
                        float u = offsetU + (rawU * scaleU);
                        float v = offsetV + (rawV * scaleV);
                        float xRobot = p1a.X + u * (p2a.X - p1a.X) + v * (p4a.X - p1a.X);
                        float yRobot = p1a.Y + u * (p2a.Y - p1a.Y) + v * (p4a.Y - p1a.Y);
                        waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ));
                    }
                }
            }

            picPreview.Invalidate();
        }

        private void PicPreview_Paint(object? sender, PaintEventArgs e)
        {
            if (previewPoints.Count == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            Pen drawPen = new Pen(Color.Blue, 2f);
            Pen transitPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
            Brush startNodeBrush = Brushes.Green;
            Brush drawNodeBrush = Brushes.Red;

            // Offset to draw text comfortably within the PictureBox
            float offsetX = 50f;
            float offsetY = 100f;

            PointF? lastDrawPoint = null;
            PointF? lastAbsolutePoint = null;

            for (int i = 0; i < previewPoints.Count; i++)
            {
                // Offset the pixel points to center them roughly
                PointF p = new PointF(previewPoints[i].X + offsetX, previewPoints[i].Y + offsetY);
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
        }
    }
}
