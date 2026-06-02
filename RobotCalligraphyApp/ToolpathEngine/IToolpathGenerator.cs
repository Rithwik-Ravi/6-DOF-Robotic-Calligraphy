using System.Collections.Generic;
using System.Drawing;

namespace RobotCalligraphyApp.ToolpathEngine
{
    public interface IToolpathGenerator
    {
        // Generates the sequence of waypoints for the robot
        List<RoboticWaypoint> Generate();

        // Optional: Properties for the UI to read back for 2D visualization
        List<PointF> PreviewPoints { get; }
        List<byte> PreviewTypes { get; }
    }
}
