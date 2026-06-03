using System.Drawing;

namespace RobotCalligraphyApp.Pipelines_3D.Core
{
    public struct RoboticWaypoint6DOF
    {
        public float X;
        public float Y;
        public float Z;
        public float A;
        public float B;
        public float C;
        
        public float Feedrate;
        public bool IsExtruding;
        public float AnalogExtrusionScalar;
        
        // UV is stored for mapping back to the UI visualizer
        public PointF UV;

        public RoboticWaypoint6DOF(float x, float y, float z, float a, float b, float c, float feedrate, bool isExtruding, float analogExtrusionScalar, PointF uv)
        {
            X = x;
            Y = y;
            Z = z;
            A = a;
            B = b;
            C = c;
            Feedrate = feedrate;
            IsExtruding = isExtruding;
            AnalogExtrusionScalar = analogExtrusionScalar;
            UV = uv;
        }

        public override string ToString()
        {
            return $"X:{X:F2}, Y:{Y:F2}, Z:{Z:F2}, E:{AnalogExtrusionScalar:F2}";
        }
    }
}
