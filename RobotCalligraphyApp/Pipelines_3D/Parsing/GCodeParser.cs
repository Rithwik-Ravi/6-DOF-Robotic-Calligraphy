using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using RobotCalligraphyApp.Pipelines_3D.Core;

namespace RobotCalligraphyApp.Pipelines_3D.Parsing
{
    public class GCodeParser
    {
        private float lastX = 0f;
        private float lastY = 0f;
        private float lastZ = 0f;
        private float lastE = 0f;
        private float lastF = 3000f;
        
        // Robot Constants
        private const float BaseZ = 118.00f;
        private const float BaseX = -506.59f;
        private const float BaseY = 873.48f;
        
        // Scale to analog Extrusion (0-5v)
        private const float MaxEDelta = 0.5f; 

        public IEnumerable<RoboticWaypoint6DOF> Parse(string gcodePath)
        {
            using (var reader = new StreamReader(gcodePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    int commentIdx = line.IndexOf(';');
                    if (commentIdx >= 0)
                    {
                        line = line.Substring(0, commentIdx).Trim();
                    }

                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("G1 ") || line.StartsWith("G0 "))
                    {
                        yield return ParseMove(line);
                    }
                    else if (line.StartsWith("G92")) // Reset E
                    {
                        float? e = ParseCoord(line, 'E');
                        if (e.HasValue) lastE = e.Value;
                    }
                }
            }
        }

        private RoboticWaypoint6DOF ParseMove(string line)
        {
            float? x = ParseCoord(line, 'X');
            float? y = ParseCoord(line, 'Y');
            float? z = ParseCoord(line, 'Z');
            float? e = ParseCoord(line, 'E');
            float? f = ParseCoord(line, 'F');

            if (x.HasValue) lastX = x.Value;
            if (y.HasValue) lastY = y.Value;
            if (z.HasValue) lastZ = z.Value;
            if (f.HasValue) lastF = f.Value;

            bool isExtruding = false;
            float analogE = 0f;

            if (e.HasValue)
            {
                float deltaE = e.Value - lastE;
                if (deltaE > 0)
                {
                    isExtruding = true;
                    // Linear mapping to 0-5V based on delta E
                    analogE = (deltaE / MaxEDelta) * 5.0f;
                    if (analogE > 5.0f) analogE = 5.0f;
                }
                lastE = e.Value;
            }

            // Map Slicer coordinates to Robot Coordinates
            // Base vectors found in analysis:
            // Slicer Width mapped to Robot Y axis negatively
            // Slicer Height mapped to Robot X axis negatively
            // XRobot = -506.59 - SlicerY
            // YRobot = 873.48 - SlicerX
            
            float targetX = BaseX - lastY;
            float targetY = BaseY - lastX;
            float targetZ = BaseZ + lastZ;

            // Maintain same posture
            float a = 179.47f;
            float b = 0.04f;
            float c = 127.18f;

            PointF uv = new PointF(lastX / 200f, lastY / 150f);

            return new RoboticWaypoint6DOF(
                targetX, targetY, targetZ, 
                a, b, c, 
                lastF, isExtruding, analogE, 
                uv);
        }

        private float? ParseCoord(string line, char axis)
        {
            int idx = line.IndexOf(axis);
            if (idx == -1) return null;

            int spaceIdx = line.IndexOf(' ', idx);
            if (spaceIdx == -1) spaceIdx = line.Length;

            string valStr = line.Substring(idx + 1, spaceIdx - idx - 1);
            if (float.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
            {
                return val;
            }
            return null;
        }
    }
}
