using System;
using System.Collections.Generic;
using System.Drawing;
using RobotCalligraphyApp.ToolpathEngine;

namespace RobotCalligraphyApp.Pipelines_2D
{
    public class TextCalligraphyPipeline2D : IToolpathGenerator
    {
        public string Text { get; set; } = "";
        public float LetterSizeMm { get; set; } = 8f;
        public string FontName { get; set; } = "Block";

        public List<PointF> PreviewPoints { get; private set; } = new List<PointF>();
        public List<byte> PreviewTypes { get; private set; } = new List<byte>();

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
            {' ', new List<PointF[]> { } }
        };

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
            {' ', new List<PointF[]> { } }
        };

        public TextCalligraphyPipeline2D()
        {
            // Copy shared characters (numbers, punctuation) into roundedFont
            foreach (var kvp in singleStrokeFont)
            {
                if (!roundedFont.ContainsKey(kvp.Key))
                {
                    roundedFont[kvp.Key] = kvp.Value;
                }
            }
        }

        public List<RoboticWaypoint> Generate()
        {
            var waypoints = new List<RoboticWaypoint>();
            PreviewPoints.Clear();
            PreviewTypes.Clear();

            string text = Text.ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return waypoints;
            }

            float drawZ = 118.00f;
            float transitZ = 123.00f;
            
            PointF p1a = new PointF(-506.59f, 873.48f);
            PointF p2a = new PointF(-506.59f, 673.48f);
            PointF p4a = new PointF(-656.59f, 873.48f);

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

            float charU = LetterSizeMm / magW;
            float charV = LetterSizeMm / magH;
            float charSpaceU = 0.3f * charU;
            float lineSpaceV = 0.6f * charV;
            float margin = 0.03f;

            float availableU = 1.0f - 2 * margin;
            float availableV = 1.0f - 2 * margin;
            float defaultWordSpaceU = 0.8f * charU;

            bool isItalic = (FontName == "Italic");
            float italicShear = isItalic ? 0.2f : 0f;
            float effectiveCharU = charU * (1.0f + italicShear);

            var activeFont = FontName == "Rounded" ? roundedFont : singleStrokeFont;

            string[] words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return waypoints;

            float[] wordWidths = new float[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                int n = 0;
                foreach (char c in words[i]) { if (activeFont.ContainsKey(c)) n++; }
                wordWidths[i] = n * effectiveCharU + Math.Max(0, n - 1) * charSpaceU;
            }

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

            float totalTextHeight = lines.Count * charV + (lines.Count - 1) * lineSpaceV;
            float startV = margin + Math.Max(0, (availableV - totalTextHeight) / 2.0f);

            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                var line = lines[lineIdx];
                float lineV = startV + lineIdx * (charV + lineSpaceV);

                float lineWordsWidth = 0;
                foreach (int wi in line) lineWordsWidth += wordWidths[wi];

                bool isLastLine = (lineIdx == lines.Count - 1);
                float actualWordSpaceU;
                float lineStartU;

                if (!isLastLine && line.Count > 1)
                {
                    float extraSpace = availableU - lineWordsWidth;
                    actualWordSpaceU = extraSpace / (line.Count - 1);
                    lineStartU = margin;
                }
                else
                {
                    actualWordSpaceU = defaultWordSpaceU;
                    float lineWidth = lineWordsWidth + (line.Count - 1) * defaultWordSpaceU;
                    lineStartU = margin + (availableU - lineWidth) / 2.0f;
                }

                float cursorU = lineStartU;
                for (int wi = 0; wi < line.Count; wi++)
                {
                    string word = words[line[wi]];

                    for (int ci = 0; ci < word.Length; ci++)
                    {
                        char c = word[ci];
                        if (!activeFont.ContainsKey(c))
                        {
                            cursorU += effectiveCharU;
                            if (ci < word.Length - 1) cursorU += charSpaceU;
                            continue;
                        }

                        float charStartU = cursorU;
                        float charStartV = lineV;

                        foreach (var stroke in activeFont[c])
                        {
                            for (int pt = 0; pt < stroke.Length; pt++)
                            {
                                float sx = stroke[pt].X;
                                float sy = stroke[pt].Y;
                                if (isItalic) sx += italicShear * (1.0f - sy);

                                float u = charStartU + sx * charU;
                                float v = charStartV + sy * charV;

                                float xRobot = p1a.X + u * dxW + v * dxH;
                                float yRobot = p1a.Y + u * dyW + v * dyH;

                                if (pt == 0)
                                {
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, transitZ, new PointF(u, v)));
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                    PreviewTypes.Add(0);
                                }
                                else
                                {
                                    waypoints.Add(new RoboticWaypoint(xRobot, yRobot, drawZ, new PointF(u, v)));
                                    PreviewTypes.Add(1);
                                }

                                PreviewPoints.Add(new PointF(u, v));
                            }

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

                        cursorU += effectiveCharU;
                        if (ci < word.Length - 1) cursorU += charSpaceU;
                    }

                    if (wi < line.Count - 1)
                    {
                        cursorU += actualWordSpaceU;
                    }
                }
            }

            return waypoints;
        }
    }
}
