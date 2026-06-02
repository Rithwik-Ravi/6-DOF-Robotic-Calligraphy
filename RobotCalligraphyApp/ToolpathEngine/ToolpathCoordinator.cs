using System;
using System.Collections.Generic;

namespace RobotCalligraphyApp.ToolpathEngine
{
    public class ToolpathCoordinator
    {
        private IToolpathGenerator? _activePipeline;

        public IToolpathGenerator? ActivePipeline => _activePipeline;

        public void SetPipeline(IToolpathGenerator pipeline)
        {
            _activePipeline = pipeline;
        }

        public List<RoboticWaypoint> GenerateToolpath()
        {
            if (_activePipeline == null)
                throw new InvalidOperationException("No pipeline set in ToolpathCoordinator.");
                
            return _activePipeline.Generate();
        }
    }
}
