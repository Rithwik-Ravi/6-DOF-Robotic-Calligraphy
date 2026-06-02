using System;
using System.Collections.Generic;
using System.Drawing;
using RobotCalligraphyApp.ToolpathEngine;

namespace RobotCalligraphyApp.Pipelines_3D
{
    public class Slicer3D_Placeholder : IToolpathGenerator
    {
        public List<PointF> PreviewPoints { get; } = new List<PointF>();
        public List<byte> PreviewTypes { get; } = new List<byte>();

        public List<RoboticWaypoint> Generate()
        {
            // Placeholder: Future 3D printing slicing logic will go here.
            throw new NotImplementedException("3D slicing mode is not yet implemented.");
        }
    }
}
