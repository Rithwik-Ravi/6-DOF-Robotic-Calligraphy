using System.Drawing;

namespace RobotCalligraphyApp.ToolpathEngine
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
}
