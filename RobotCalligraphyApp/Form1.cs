using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace RobotCalligraphyApp
{
    public struct RoboticWaypoint
    {
        public float X;
        public float Y;
        public float Z;
        public PointF UV;

        public RoboticWaypoint(float x, float y, float z, PointF uv)
        {
            X = x;
            Y = y;
            Z = z;
            UV = uv;
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

        // Single-stroke font: A-Z, 0-9, punctuation, and space
        private Dictionary<char, List<PointF[]>> singleStrokeFont = new Dictionary<char, List<PointF[]>>()
        {
            // === UPPERCASE LETTERS ===
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

            // === NUMBERS ===
            {'0', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(1,1), new PointF(0,1), new PointF(0,0) } }},
            {'1', new List<PointF[]> { new PointF[] { new PointF(0.3f,0.2f), new PointF(0.5f,0), new PointF(0.5f,1) }, new PointF[] { new PointF(0.2f,1), new PointF(0.8f,1) } }},
            {'2', new List<PointF[]> { new PointF[] { new PointF(0,0.2f), new PointF(0.2f,0), new PointF(0.8f,0), new PointF(1,0.2f), new PointF(1,0.4f), new PointF(0,1), new PointF(1,1) } }},
            {'3', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(1,0.5f), new PointF(0.3f,0.5f) }, new PointF[] { new PointF(1,0.5f), new PointF(1,1), new PointF(0,1) } }},
            {'4', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,0.5f), new PointF(1,0.5f) }, new PointF[] { new PointF(0.75f,0), new PointF(0.75f,1) } }},
            {'5', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,0.5f), new PointF(0.8f,0.5f), new PointF(1,0.7f), new PointF(0.8f,1), new PointF(0,1) } }},
            {'6', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,0), new PointF(0,1), new PointF(1,1), new PointF(1,0.5f), new PointF(0,0.5f) } }},
            {'7', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(0.3f,1) } }},
            {'8', new List<PointF[]> { new PointF[] { new PointF(0.5f,0.5f), new PointF(0,0.3f), new PointF(0,0), new PointF(1,0), new PointF(1,0.3f), new PointF(0.5f,0.5f), new PointF(0,0.7f), new PointF(0,1), new PointF(1,1), new PointF(1,0.7f), new PointF(0.5f,0.5f) } }},
            {'9', new List<PointF[]> { new PointF[] { new PointF(1,0.5f), new PointF(0,0.5f), new PointF(0,0), new PointF(1,0), new PointF(1,1), new PointF(0,1) } }},

            // === PUNCTUATION ===
            {'.', new List<PointF[]> { new PointF[] { new PointF(0.5f,0.85f), new PointF(0.5f,1.0f) } }},
            {',', new List<PointF[]> { new PointF[] { new PointF(0.5f,0.85f), new PointF(0.35f,1.0f) } }},
            {'!', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.5f,0.7f) }, new PointF[] { new PointF(0.5f,0.85f), new PointF(0.5f,1.0f) } }},
            {'?', new List<PointF[]> { new PointF[] { new PointF(0,0.2f), new PointF(0.2f,0), new PointF(0.8f,0), new PointF(1,0.2f), new PointF(0.5f,0.5f), new PointF(0.5f,0.7f) }, new PointF[] { new PointF(0.5f,0.85f), new PointF(0.5f,1.0f) } }},
            {'-', new List<PointF[]> { new PointF[] { new PointF(0.2f,0.5f), new PointF(0.8f,0.5f) } }},
            {':', new List<PointF[]> { new PointF[] { new PointF(0.5f,0.25f), new PointF(0.5f,0.35f) }, new PointF[] { new PointF(0.5f,0.7f), new PointF(0.5f,0.85f) } }},
            {';', new List<PointF[]> { new PointF[] { new PointF(0.5f,0.25f), new PointF(0.5f,0.35f) }, new PointF[] { new PointF(0.5f,0.7f), new PointF(0.35f,0.85f) } }},
            {'\'', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.45f,0.15f) } }},
            {'(', new List<PointF[]> { new PointF[] { new PointF(0.7f,0), new PointF(0.3f,0.5f), new PointF(0.7f,1) } }},
            {')', new List<PointF[]> { new PointF[] { new PointF(0.3f,0), new PointF(0.7f,0.5f), new PointF(0.3f,1) } }},
            {'/', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0,1) } }},

            // === SPACE ===
            {' ', new List<PointF[]> { } }
        };

        // Rounded font: softer, curved shapes using intermediate points
        private Dictionary<char, List<PointF[]>> roundedFont = new Dictionary<char, List<PointF[]>>()
        {
            {'A', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0.15f,0.4f), new PointF(0.5f,0), new PointF(0.85f,0.4f), new PointF(1,1) }, new PointF[] { new PointF(0.2f,0.6f), new PointF(0.8f,0.6f) } }},
            {'B', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0,0), new PointF(0.6f,0), new PointF(0.85f,0.1f), new PointF(0.85f,0.4f), new PointF(0.6f,0.5f), new PointF(0,0.5f) }, new PointF[] { new PointF(0,0.5f), new PointF(0.6f,0.5f), new PointF(0.9f,0.6f), new PointF(0.9f,0.9f), new PointF(0.6f,1), new PointF(0,1) } }},
            {'C', new List<PointF[]> { new PointF[] { new PointF(1,0.15f), new PointF(0.7f,0), new PointF(0.3f,0), new PointF(0,0.15f), new PointF(0,0.85f), new PointF(0.3f,1), new PointF(0.7f,1), new PointF(1,0.85f) } }},
            {'D', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0,0), new PointF(0.5f,0), new PointF(0.85f,0.15f), new PointF(1,0.5f), new PointF(0.85f,0.85f), new PointF(0.5f,1), new PointF(0,1) } }},
            {'E', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0.3f,0), new PointF(0,0.15f), new PointF(0,0.85f), new PointF(0.3f,1), new PointF(1,1) }, new PointF[] { new PointF(0,0.5f), new PointF(0.7f,0.5f) } }},
            {'F', new List<PointF[]> { new PointF[] { new PointF(1,0), new PointF(0.3f,0), new PointF(0,0.15f), new PointF(0,1) }, new PointF[] { new PointF(0,0.5f), new PointF(0.7f,0.5f) } }},
            {'G', new List<PointF[]> { new PointF[] { new PointF(1,0.15f), new PointF(0.7f,0), new PointF(0.3f,0), new PointF(0,0.15f), new PointF(0,0.85f), new PointF(0.3f,1), new PointF(0.7f,1), new PointF(1,0.85f), new PointF(1,0.5f), new PointF(0.5f,0.5f) } }},
            {'H', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(1,0), new PointF(1,1) }, new PointF[] { new PointF(0,0.5f), new PointF(1,0.5f) } }},
            {'I', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.5f,1) }, new PointF[] { new PointF(0.2f,0), new PointF(0.8f,0) }, new PointF[] { new PointF(0.2f,1), new PointF(0.8f,1) } }},
            {'J', new List<PointF[]> { new PointF[] { new PointF(0.8f,0), new PointF(0.8f,0.75f), new PointF(0.6f,0.95f), new PointF(0.3f,0.95f), new PointF(0.1f,0.75f), new PointF(0.1f,0.5f) } }},
            {'K', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,1) }, new PointF[] { new PointF(0.9f,0), new PointF(0.1f,0.5f) }, new PointF[] { new PointF(0.25f,0.45f), new PointF(0.9f,1) } }},
            {'L', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,0.85f), new PointF(0.15f,1), new PointF(1,1) } }},
            {'M', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(0.5f,0.45f), new PointF(1,0), new PointF(1,1) } }},
            {'N', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(1,1), new PointF(1,0) } }},
            {'O', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.85f,0.1f), new PointF(1,0.5f), new PointF(0.85f,0.9f), new PointF(0.5f,1), new PointF(0.15f,0.9f), new PointF(0,0.5f), new PointF(0.15f,0.1f), new PointF(0.5f,0) } }},
            {'P', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(0.6f,0), new PointF(0.85f,0.1f), new PointF(0.85f,0.4f), new PointF(0.6f,0.5f), new PointF(0,0.5f) } }},
            {'Q', new List<PointF[]> { new PointF[] { new PointF(0.5f,0), new PointF(0.85f,0.1f), new PointF(1,0.5f), new PointF(0.85f,0.9f), new PointF(0.5f,1), new PointF(0.15f,0.9f), new PointF(0,0.5f), new PointF(0.15f,0.1f), new PointF(0.5f,0) }, new PointF[] { new PointF(0.6f,0.7f), new PointF(1,1) } }},
            {'R', new List<PointF[]> { new PointF[] { new PointF(0,1), new PointF(0,0), new PointF(0.6f,0), new PointF(0.85f,0.1f), new PointF(0.85f,0.4f), new PointF(0.6f,0.5f), new PointF(0,0.5f) }, new PointF[] { new PointF(0.4f,0.5f), new PointF(1,1) } }},
            {'S', new List<PointF[]> { new PointF[] { new PointF(0.85f,0.15f), new PointF(0.6f,0), new PointF(0.3f,0), new PointF(0.1f,0.15f), new PointF(0.1f,0.35f), new PointF(0.5f,0.5f), new PointF(0.9f,0.65f), new PointF(0.9f,0.85f), new PointF(0.7f,1), new PointF(0.3f,1), new PointF(0.15f,0.85f) } }},
            {'T', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0) }, new PointF[] { new PointF(0.5f,0), new PointF(0.5f,1) } }},
            {'U', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0,0.75f), new PointF(0.15f,0.95f), new PointF(0.5f,1), new PointF(0.85f,0.95f), new PointF(1,0.75f), new PointF(1,0) } }},
            {'V', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.5f,1), new PointF(1,0) } }},
            {'W', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.2f,1), new PointF(0.5f,0.4f), new PointF(0.8f,1), new PointF(1,0) } }},
            {'X', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.45f,0.45f), new PointF(1,1) }, new PointF[] { new PointF(1,0), new PointF(0.55f,0.45f), new PointF(0,1) } }},
            {'Y', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(0.5f,0.5f), new PointF(1,0) }, new PointF[] { new PointF(0.5f,0.5f), new PointF(0.5f,1) } }},
            {'Z', new List<PointF[]> { new PointF[] { new PointF(0,0), new PointF(1,0), new PointF(0,1), new PointF(1,1) } }},
            // Rounded font shares numbers + punctuation from block font (loaded at runtime)
            {' ', new List<PointF[]> { } }
        };

        public Form1()
        {
            // Copy shared characters (numbers, punctuation) into roundedFont
            foreach (var kvp in singleStrokeFont)
            {
                if (!roundedFont.ContainsKey(kvp.Key))
                {
                    roundedFont[kvp.Key] = kvp.Value;
                }
            }

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

            // Tuning variables
            double minContourArea = 5.0; // Increased to filter out small noise specks
            double epsilonFactor = 0.0005; // Drastically reduced to preserve smooth curves (mustache fix)

            waypoints.Clear();
            previewPoints.Clear();
            previewTypes.Clear();

            // 1. Ingest image with Alpha channel (Bgra)
            using (Image<Bgra, byte> originalImg = new Image<Bgra, byte>(txtImagePath.Text))
            {
                // 2. Alpha Compositing: Flatten onto a solid White background
                using (Image<Bgr, byte> flattenedImg = new Image<Bgr, byte>(originalImg.Width, originalImg.Height))
                {
                    for (int y = 0; y < originalImg.Height; y++)
                    {
                        for (int x = 0; x < originalImg.Width; x++)
                        {
                            Bgra pixel = originalImg[y, x];
                            double alpha = pixel.Alpha / 255.0;
                            
                            // Blend foreground with white background
                            byte b = (byte)(pixel.Blue * alpha + 255 * (1 - alpha));
                            byte g = (byte)(pixel.Green * alpha + 255 * (1 - alpha));
                            byte r = (byte)(pixel.Red * alpha + 255 * (1 - alpha));
                            
                            flattenedImg[y, x] = new Bgr(b, g, r);
                        }
                    }

                    // 3. Convert to Grayscale
                    using (Image<Gray, byte> grayImg = flattenedImg.Convert<Gray, byte>())
                    using (Image<Gray, byte> blurredImg = new Image<Gray, byte>(grayImg.Width, grayImg.Height))
                    {
                        // 3.5 Pre-processing: Gaussian Blur for noise reduction
                        CvInvoke.GaussianBlur(grayImg, blurredImg, new Size(5, 5), 0);

                        // 4. Adaptive Thresholding (Pencil Sketch Algorithm)
                        using (Image<Gray, byte> edges = new Image<Gray, byte>(blurredImg.Width, blurredImg.Height))
                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        using (Mat hierarchy = new Mat())
                        {
                            // Create a binary image where lines are white (255)
                            CvInvoke.AdaptiveThreshold(blurredImg, edges, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 21, 5);

                            // Apply Zhang-Suen morphological thinning to reduce strokes to a 1-pixel skeleton
                            ZhangSuenThinning(edges);

                            // 5. Find Contours (now tracing a 1-pixel skeleton instead of thick edges)
                            CvInvoke.FindContours(edges, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                            // Extract to C# arrays, approximate polygons (reduce point density), and apply contour area filter
                            List<Point[]> allContours = new List<Point[]>();
                            for (int i = 0; i < contours.Size; i++)
                            {
                                double area = CvInvoke.ContourArea(contours[i], false);
                                
                                // Point reduction via Ramer-Douglas-Peucker algorithm
                                double perimeter = CvInvoke.ArcLength(contours[i], false);
                                using (VectorOfPoint approxContour = new VectorOfPoint())
                                {
                                    CvInvoke.ApproxPolyDP(contours[i], approxContour, perimeter * epsilonFactor, false);
                                    
                                    var pts = approxContour.ToArray();
                                    if (pts.Length > 1 && area >= minContourArea)
                                    {
                                        allContours.Add(pts);
                                    }
                                }
                            }

                            if (allContours.Count == 0)
                            {
                                MessageBox.Show("No contours found in the image (or all filtered out).", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                picPreview.Invalidate();
                                return;
                            }

                            // 6. Nearest Neighbor Path Optimization
                            List<Point[]> optimizedContours = OptimizePath(allContours);

                            // 7. Scaling Math
                            float imgWidth = originalImg.Width;
                            float imgHeight = originalImg.Height;
                            
                            float targetWidthMm = (float)numImageWidth.Value;
                            float scaleU = targetWidthMm / imgWidth;
                            
                            // Physical Constraints
                            float drawZ = 118.00f;  // Pen-down height
                            float transitZ = 123.00f;  // Pen-up height
                            
                            // Bounding Box setup (same as text, centered)
                            PointF p1a = new PointF(-506.59f, 873.48f);   // Top-left
                            PointF p2a = new PointF(-506.59f, 673.48f);   // Top-right
                            PointF p4a = new PointF(-656.59f, 873.48f);   // Bottom-left

                            float dxW = p2a.X - p1a.X;
                            float dyW = p2a.Y - p1a.Y;
                            float magW = (float)Math.Sqrt(dxW * dxW + dyW * dyW);
                            
                            float dxH_raw = p4a.X - p1a.X;
                            float dyH_raw = p4a.Y - p1a.Y;
                            float magH = (float)Math.Sqrt(dxH_raw * dxH_raw + dyH_raw * dyH_raw);

                            float perpX = dyW;
                            float perpY = -dxW;
                            float magPerp = (float)Math.Sqrt(perpX * perpX + perpY * perpY);
                            
                            float dxH = (perpX / magPerp) * magH;
                            float dyH = (perpY / magPerp) * magH;

                            // Compute physical target dimensions
                            float targetHeightMm = imgHeight * scaleU;
                            float targetU_Span = targetWidthMm / magW;
                            float targetV_Span = targetHeightMm / magH;
                            
                            // Center the image in the drawing area
                            float margin = 0.03f;
                            float availableU = 1.0f - 2 * margin;
                            float availableV = 1.0f - 2 * margin;
                            
                            float startU = margin + (availableU - targetU_Span) / 2.0f;
                            float startV = margin + (availableV - targetV_Span) / 2.0f;

                            // Ensure it doesn't exceed bounds
                            if (targetU_Span > availableU || targetV_Span > availableV)
                            {
                                MessageBox.Show("Image target size exceeds drawing area. It will be scaled to fit.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                // Fit to bounds proportionally
                                float scaleFactor = Math.Min(availableU / targetU_Span, availableV / targetV_Span);
                                targetU_Span *= scaleFactor;
                                targetV_Span *= scaleFactor;
                                startU = margin + (availableU - targetU_Span) / 2.0f;
                                startV = margin + (availableV - targetV_Span) / 2.0f;
                            }

                            // 8. Populate waypoints
                            foreach (var contour in optimizedContours)
                            {
                                for (int pt = 0; pt < contour.Length; pt++)
                                {
                                    // Map pixel (x,y) to (u,v) [0, 1] relative to image dimensions
                                    float normX = contour[pt].X / imgWidth;
                                    float normY = contour[pt].Y / imgHeight;

                                    float u = startU + normX * targetU_Span;
                                    float v = startV + normY * targetV_Span;

                                    // Map UV to physical robot coordinates
                                    float xRobot = p1a.X + u * dxW + v * dxH;
                                    float yRobot = p1a.Y + u * dyW + v * dyH;

                                    if (pt == 0) // Start of contour
                                    {
                                        waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ, new PointF(u, v)));
                                        waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                        previewTypes.Add(0); // Start marker
                                    }
                                    else
                                    {
                                        waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                        previewTypes.Add(1); // Line marker
                                    }
                                    
                                    // 2D preview (saved as unscaled u, v)
                                    previewPoints.Add(new PointF(u, v));
                                }
                                
                                // Lift pen at end of contour
                                if (contour.Length > 0)
                                {
                                    var lastPt = contour[contour.Length - 1];
                                    float normX = lastPt.X / imgWidth;
                                    float normY = lastPt.Y / imgHeight;
                                    float u = startU + normX * targetU_Span;
                                    float v = startV + normY * targetV_Span;
                                    float xRobot = p1a.X + u * dxW + v * dxH;
                                    float yRobot = p1a.Y + u * dyW + v * dyH;
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ, new PointF(u, v)));
                                }
                            }

                            picPreview.Invalidate();
                        }
                    }
                }
            }
        }

        private unsafe void ZhangSuenThinning(Image<Gray, byte> img)
        {
            int width = img.Width;
            int height = img.Height;
            int stride = img.Mat.Step;
            byte* data = (byte*)img.Mat.DataPointer;

            bool hasChanged = true;
            List<Point> toRemove = new List<Point>();

            while (hasChanged)
            {
                hasChanged = false;

                // Two sub-iterations
                for (int step = 0; step < 2; step++)
                {
                    toRemove.Clear();
                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            if (data[y * stride + x] == 255) // foreground pixel
                            {
                                int p2 = data[(y - 1) * stride + x] == 255 ? 1 : 0;
                                int p3 = data[(y - 1) * stride + (x + 1)] == 255 ? 1 : 0;
                                int p4 = data[y * stride + (x + 1)] == 255 ? 1 : 0;
                                int p5 = data[(y + 1) * stride + (x + 1)] == 255 ? 1 : 0;
                                int p6 = data[(y + 1) * stride + x] == 255 ? 1 : 0;
                                int p7 = data[(y + 1) * stride + (x - 1)] == 255 ? 1 : 0;
                                int p8 = data[y * stride + (x - 1)] == 255 ? 1 : 0;
                                int p9 = data[(y - 1) * stride + (x - 1)] == 255 ? 1 : 0;

                                int A = (p2 == 0 && p3 == 1 ? 1 : 0) +
                                        (p3 == 0 && p4 == 1 ? 1 : 0) +
                                        (p4 == 0 && p5 == 1 ? 1 : 0) +
                                        (p5 == 0 && p6 == 1 ? 1 : 0) +
                                        (p6 == 0 && p7 == 1 ? 1 : 0) +
                                        (p7 == 0 && p8 == 1 ? 1 : 0) +
                                        (p8 == 0 && p9 == 1 ? 1 : 0) +
                                        (p9 == 0 && p2 == 1 ? 1 : 0);

                                int B = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;

                                int m1 = step == 0 ? (p2 * p4 * p6) : (p2 * p4 * p8);
                                int m2 = step == 0 ? (p4 * p6 * p8) : (p2 * p6 * p8);

                                if (A == 1 && (B >= 2 && B <= 6) && m1 == 0 && m2 == 0)
                                {
                                    toRemove.Add(new Point(x, y));
                                }
                            }
                        }
                    }

                    if (toRemove.Count > 0)
                    {
                        hasChanged = true;
                        foreach (Point p in toRemove)
                        {
                            data[p.Y * stride + p.X] = 0;
                        }
                    }
                }
            }
        }

        private List<Point[]> OptimizePath(List<Point[]> contours)
        {
            if (contours.Count == 0) return new List<Point[]>();

            List<Point[]> optimized = new List<Point[]>(contours.Count);
            List<Point[]> unvisited = new List<Point[]>(contours);

            // Start with the first contour
            Point[] current = unvisited[0];
            optimized.Add(current);
            unvisited.RemoveAt(0);

            while (unvisited.Count > 0)
            {
                Point currentEnd = current[current.Length - 1];
                
                int bestIdx = -1;
                bool needsReverse = false;
                double minDistance = double.MaxValue;

                for (int i = 0; i < unvisited.Count; i++)
                {
                    Point start = unvisited[i][0];
                    Point end = unvisited[i][unvisited[i].Length - 1];

                    double dStart = Math.Pow(start.X - currentEnd.X, 2) + Math.Pow(start.Y - currentEnd.Y, 2);
                    double dEnd = Math.Pow(end.X - currentEnd.X, 2) + Math.Pow(end.Y - currentEnd.Y, 2);

                    if (dStart < minDistance)
                    {
                        minDistance = dStart;
                        bestIdx = i;
                        needsReverse = false;
                    }

                    if (dEnd < minDistance)
                    {
                        minDistance = dEnd;
                        bestIdx = i;
                        needsReverse = true;
                    }
                }

                Point[] next = unvisited[bestIdx];
                if (needsReverse)
                {
                    Array.Reverse(next);
                }

                optimized.Add(next);
                unvisited.RemoveAt(bestIdx);
                current = next;
            }

            return optimized;
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
                string pHomeStr = "MVS; -581.59;  773.48;  150.00";
                string? resp = await robotClient.SendAsync(pHomeStr);
                if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot did not acknowledge home move. Response: {resp}");

                bool isFirstMove = true;
                int totalPoints = waypoints.Count;
                int pointsExecuted = 0;
                System.Diagnostics.Stopwatch uiSw = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Stopwatch totalSw = System.Diagnostics.Stopwatch.StartNew();

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

                    string commandType = isFirstMove ? "MOV" : "MVS";
                    string pt = $"{commandType};{wp.X,8:F2};{wp.Y,8:F2};{wp.Z,8:F2}";
                    
                    resp = await robotClient.SendAsync(pt);
                    if (resp == null || !resp.Trim().StartsWith("ACK")) throw new Exception($"Robot streaming interrupted. Response: {resp}");
                    
                    pointsExecuted++;
                    isFirstMove = false;

                    // Update UI (throttled to ~10 FPS)
                    if (uiSw.ElapsedMilliseconds > 100 || pointsExecuted == totalPoints)
                    {
                        currentRobotWaypoint = wp;
                        uiSw.Restart();

                        double elapsedMs = totalSw.ElapsedMilliseconds;
                        double msPerPoint = elapsedMs / pointsExecuted;
                        double msRemaining = msPerPoint * (totalPoints - pointsExecuted);
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
                await robotClient.SendAsync(pHomeStr);
                
                MessageBox.Show("Calligraphy sequence finished!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                // Try to send robot home safely after stop
                try
                {
                    if (robotClient.IsConnected)
                    {
                        string pHomeStr = "MVS; -581.59;  773.48;  150.00";
                        await robotClient.SendAsync(pHomeStr);
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
            waypoints.Clear();
            previewPoints.Clear();
            previewTypes.Clear();

            string text = txtInput.Text.ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                picPreview.Invalidate();
                return;
            }

            // Physical Constraints for RV-8CRL Physical Robot
            float drawZ = 118.00f;  // Pen-down height (touching surface)
            float transitZ = 123.00f;  // Pen-up height (minimal 5mm lift for speed)
            
            // Drawing area near the robot's home position
            // 200mm wide x 150mm tall rectangle
            PointF p1a = new PointF(-506.59f, 873.48f);   // Top-left corner
            PointF p2a = new PointF(-506.59f, 673.48f);   // Top-right corner (width direction along -Y)
            PointF p4a = new PointF(-656.59f, 873.48f);   // Bottom-left corner (height direction along -X)

            // Orthogonal Vector Math (Fixes the slant!)
            float dxW = p2a.X - p1a.X;
            float dyW = p2a.Y - p1a.Y;
            float magW = (float)Math.Sqrt(dxW * dxW + dyW * dyW);
            
            float dxH_raw = p4a.X - p1a.X;
            float dyH_raw = p4a.Y - p1a.Y;
            float magH = (float)Math.Sqrt(dxH_raw * dxH_raw + dyH_raw * dyH_raw);

            // Create a perfectly perpendicular Height vector from the Width vector
            float perpX = dyW;
            float perpY = -dxW;
            float magPerp = (float)Math.Sqrt(perpX * perpX + perpY * perpY);
            
            float dxH = (perpX / magPerp) * magH;
            float dyH = (perpY / magPerp) * magH;

            // ========================================
            // Multi-line text layout with justification
            // ========================================

            float letterMm = (float)numLetterSize.Value; // Physical height of each letter in mm

            // UV space dimensions (each letter is square in physical mm)
            float charU = letterMm / magW;   // UV width of one letter
            float charV = letterMm / magH;   // UV height of one letter
            float charSpaceU = 0.3f * charU; // Inter-character spacing
            float lineSpaceV = 0.6f * charV; // Extra vertical spacing between lines
            float margin = 0.03f;            // Margin from edges in UV space

            float availableU = 1.0f - 2 * margin;  // Usable width in UV
            float availableV = 1.0f - 2 * margin;  // Usable height in UV
            float defaultWordSpaceU = 0.8f * charU; // Default word spacing (for last/centered line)

            // Font selection
            string fontName = cmbFont.SelectedItem?.ToString() ?? "Block";
            bool isItalic = (fontName == "Italic");
            float italicShear = isItalic ? 0.2f : 0f;
            // For italic, characters are wider due to shear offset
            float effectiveCharU = charU * (1.0f + italicShear);

            // Select active font dictionary (Italic uses Block data + shear transform)
            var activeFont = fontName == "Rounded" ? roundedFont : singleStrokeFont;

            // Split text into words (all whitespace = word boundary)
            string[] words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) { picPreview.Invalidate(); return; }

            // Calculate each word's width in UV units
            float[] wordWidths = new float[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                int n = 0;
                foreach (char c in words[i]) { if (activeFont.ContainsKey(c)) n++; }
                wordWidths[i] = n * effectiveCharU + Math.Max(0, n - 1) * charSpaceU;
            }

            // Word-wrap into lines (greedy algorithm)
            List<List<int>> lines = new List<List<int>>();
            List<int> currentLine = new List<int>();
            float currentLineWidth = 0;

            for (int i = 0; i < words.Length; i++)
            {
                float neededWidth = currentLine.Count > 0 
                    ? defaultWordSpaceU + wordWidths[i] 
                    : wordWidths[i];
                
                if (currentLine.Count > 0 && currentLineWidth + neededWidth > availableU)
                {
                    // Current word doesn't fit - start a new line
                    lines.Add(currentLine);
                    currentLine = new List<int> { i };
                    currentLineWidth = wordWidths[i];
                }
                else
                {
                    currentLine.Add(i);
                    currentLineWidth += neededWidth;
                }
            }
            if (currentLine.Count > 0) lines.Add(currentLine);

            // Calculate total height and vertical centering offset
            float totalTextHeight = lines.Count * charV + (lines.Count - 1) * lineSpaceV;
            float startV = margin + Math.Max(0, (availableV - totalTextHeight) / 2.0f);

            // Render each line
            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                var line = lines[lineIdx];
                float lineV = startV + lineIdx * (charV + lineSpaceV);

                // Sum of all word widths on this line
                float lineWordsWidth = 0;
                foreach (int wi in line) lineWordsWidth += wordWidths[wi];

                bool isLastLine = (lineIdx == lines.Count - 1);
                float actualWordSpaceU;
                float lineStartU;

                if (!isLastLine && line.Count > 1)
                {
                    // JUSTIFIED: distribute extra space evenly between words
                    float extraSpace = availableU - lineWordsWidth;
                    actualWordSpaceU = extraSpace / (line.Count - 1);
                    lineStartU = margin;
                }
                else
                {
                    // CENTERED: last line or single-word line
                    actualWordSpaceU = defaultWordSpaceU;
                    float lineWidth = lineWordsWidth + (line.Count - 1) * defaultWordSpaceU;
                    lineStartU = margin + (availableU - lineWidth) / 2.0f;
                }

                // Render each word in the line
                float cursorU = lineStartU;
                for (int wi = 0; wi < line.Count; wi++)
                {
                    string word = words[line[wi]];

                    // Render each character in the word
                    for (int ci = 0; ci < word.Length; ci++)
                    {
                        char c = word[ci];
                        if (!activeFont.ContainsKey(c))
                        {
                            // Skip unknown characters but still advance cursor
                            cursorU += effectiveCharU;
                            if (ci < word.Length - 1) cursorU += charSpaceU;
                            continue;
                        }

                        float charStartU = cursorU;
                        float charStartV = lineV;

                        // Render all strokes for this character
                        foreach (var stroke in activeFont[c])
                        {
                            for (int pt = 0; pt < stroke.Length; pt++)
                            {
                                // Apply italic shear: shift X right at top, straight at bottom
                                float sx = stroke[pt].X;
                                float sy = stroke[pt].Y;
                                if (isItalic) sx += italicShear * (1.0f - sy);

                                float u = charStartU + sx * charU;
                                float v = charStartV + sy * charV;

                                // Map UV to physical robot coordinates (orthogonal projection)
                                float xRobot = p1a.X + u * dxW + v * dxH;
                                float yRobot = p1a.Y + u * dyW + v * dyH;

                                if (pt == 0) // Start of stroke: transit down
                                {
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ, new PointF(u, v)));
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                    previewTypes.Add(0); // Start marker
                                }
                                else // Continue drawing
                                {
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                    previewTypes.Add(1); // Line marker
                                }

                                // 2D preview point (saved as unscaled u, v)
                                previewPoints.Add(new PointF(u, v));
                            }

                            // Lift pen after each stroke
                            if (stroke.Length > 0)
                            {
                                var lastPt = stroke[stroke.Length - 1];
                                float lsx = lastPt.X;
                                float lsy = lastPt.Y;
                                if (isItalic) lsx += italicShear * (1.0f - lsy);

                                float u = charStartU + lsx * charU;
                                float v = charStartV + lsy * charV;
                                float xRobot = p1a.X + u * dxW + v * dxH;
                                float yRobot = p1a.Y + u * dyW + v * dyH;
                                waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ, new PointF(u, v)));
                            }
                        }

                        // Advance cursor past this character
                        cursorU += effectiveCharU;
                        if (ci < word.Length - 1) cursorU += charSpaceU;
                    }

                    // Advance cursor by word spacing (to next word)
                    if (wi < line.Count - 1)
                    {
                        cursorU += actualWordSpaceU;
                    }
                }
            }

            picPreview.Invalidate();
        }

        // =====================================================================
        // PREVIEW: Paint handler for 2D path visualization
        // =====================================================================

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
