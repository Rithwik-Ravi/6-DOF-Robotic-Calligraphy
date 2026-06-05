using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using RobotCalligraphyApp.ToolpathEngine;

namespace RobotCalligraphyApp.Pipelines_2D
{
    public class ImageVectorizationPipeline2D : IToolpathGenerator
    {
        public string ImagePath { get; set; } = "";
        public float TargetWidthMm { get; set; } = 260f;
        public int DetailLevel { get; set; } = 20;

        public List<PointF> PreviewPoints { get; private set; } = new List<PointF>();
        public List<byte> PreviewTypes { get; private set; } = new List<byte>();

        public List<RoboticWaypoint> Generate()
        {
            if (string.IsNullOrEmpty(ImagePath) || !System.IO.File.Exists(ImagePath))
            {
                throw new InvalidOperationException("Valid image path is required.");
            }

            var waypoints = new List<RoboticWaypoint>();
            PreviewPoints.Clear();
            PreviewTypes.Clear();

            double minContourArea = 5.0; 
            double epsilonPixels = 0.5 + ((100 - DetailLevel) / 99.0) * 9.5;

            using (Image<Bgra, byte> originalImg = new Image<Bgra, byte>(ImagePath))
            using (Image<Bgr, byte> flattenedImg = new Image<Bgr, byte>(originalImg.Width, originalImg.Height))
            {
                for (int y = 0; y < originalImg.Height; y++)
                {
                    for (int x = 0; x < originalImg.Width; x++)
                    {
                        Bgra pixel = originalImg[y, x];
                        double alpha = pixel.Alpha / 255.0;
                        byte b = (byte)(pixel.Blue * alpha + 255 * (1 - alpha));
                        byte g = (byte)(pixel.Green * alpha + 255 * (1 - alpha));
                        byte r = (byte)(pixel.Red * alpha + 255 * (1 - alpha));
                        flattenedImg[y, x] = new Bgr(b, g, r);
                    }
                }

                using (Image<Gray, byte> grayImg = flattenedImg.Convert<Gray, byte>())
                using (Image<Gray, byte> blurredImg = new Image<Gray, byte>(grayImg.Width, grayImg.Height))
                {
                    CvInvoke.GaussianBlur(grayImg, blurredImg, new Size(5, 5), 0);

                    using (Image<Gray, byte> edges = new Image<Gray, byte>(blurredImg.Width, blurredImg.Height))
                    {
                        CvInvoke.AdaptiveThreshold(blurredImg, edges, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 21, 5);

                        ZhangSuenThinning(edges);

                        List<Point[]> rawPaths = ExtractSkeletonPaths(edges);

                        List<Point[]> allContours = new List<Point[]>();
                        foreach (var path in rawPaths)
                        {
                            using (VectorOfPoint vp = new VectorOfPoint(path))
                            using (VectorOfPoint approxContour = new VectorOfPoint())
                            {
                                double pathLength = CvInvoke.ArcLength(vp, false);
                                CvInvoke.ApproxPolyDP(vp, approxContour, epsilonPixels, false);
                                
                                var pts = approxContour.ToArray();
                                if (pts.Length > 1 && pathLength >= minContourArea)
                                {
                                    allContours.Add(pts);
                                }
                            }
                        }

                        if (allContours.Count == 0)
                        {
                            return waypoints;
                        }

                        List<Point[]> optimizedContours = OptimizePath(allContours);

                        float imgWidth = originalImg.Width;
                        float imgHeight = originalImg.Height;
                        
                        float scaleU = TargetWidthMm / imgWidth;
                        
                        float drawZ = 164.640f;
                        float transitZ = 169.640f;
                        
                        PointF p1a = new PointF(340.00f, -860.00f);
                        PointF p2a = new PointF(600.00f, -860.00f);
                        PointF p4a = new PointF(340.00f, -1030.00f);

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

                        float targetHeightMm = imgHeight * scaleU;
                        float targetU_Span = TargetWidthMm / magW;
                        float targetV_Span = targetHeightMm / magH;
                        
                        float margin = 0.03f;
                        float availableU = 1.0f - 2 * margin;
                        float availableV = 1.0f - 2 * margin;
                        
                        float startU = margin + (availableU - targetU_Span) / 2.0f;
                        float startV = margin + (availableV - targetV_Span) / 2.0f;

                        if (targetU_Span > availableU || targetV_Span > availableV)
                        {
                            float scaleFactor = Math.Min(availableU / targetU_Span, availableV / targetV_Span);
                            targetU_Span *= scaleFactor;
                            targetV_Span *= scaleFactor;
                            startU = margin + (availableU - targetU_Span) / 2.0f;
                            startV = margin + (availableV - targetV_Span) / 2.0f;
                        }

                        foreach (var contour in optimizedContours)
                        {
                            for (int pt = 0; pt < contour.Length; pt++)
                            {
                                float normX = contour[pt].X / imgWidth;
                                float normY = contour[pt].Y / imgHeight;

                                float u = startU + normX * targetU_Span;
                                float v = startV + normY * targetV_Span;

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
                    }
                }
            }

            return waypoints;
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

                for (int step = 0; step < 2; step++)
                {
                    toRemove.Clear();
                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            if (data[y * stride + x] == 255)
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

        private unsafe List<Point[]> ExtractSkeletonPaths(Image<Gray, byte> img)
        {
            int width = img.Width;
            int height = img.Height;
            int stride = img.Mat.Step;
            byte* data = (byte*)img.Mat.DataPointer;
            
            bool[,] visited = new bool[width, height];
            List<Point[]> paths = new List<Point[]>();

            List<Point> GetUnvisitedNeighbors(int cx, int cy)
            {
                List<Point> n = new List<Point>();
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (data[ny * stride + nx] > 0 && !visited[nx, ny])
                            {
                                n.Add(new Point(nx, ny));
                            }
                        }
                    }
                }
                return n;
            }

            List<Point> GetAllNeighbors(int cx, int cy)
            {
                List<Point> n = new List<Point>();
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (data[ny * stride + nx] > 0)
                            {
                                n.Add(new Point(nx, ny));
                            }
                        }
                    }
                }
                return n;
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (data[y * stride + x] > 0 && !visited[x, y])
                    {
                        var allN = GetAllNeighbors(x, y);
                        if (allN.Count == 1)
                        {
                            List<Point> stroke = new List<Point>();
                            int cx = x, cy = y;
                            
                            while (true)
                            {
                                stroke.Add(new Point(cx, cy));
                                visited[cx, cy] = true;

                                var unvisitedN = GetUnvisitedNeighbors(cx, cy);
                                if (unvisitedN.Count > 0)
                                {
                                    cx = unvisitedN[0].X;
                                    cy = unvisitedN[0].Y;
                                }
                                else
                                {
                                    var allNeighbors = GetAllNeighbors(cx, cy);
                                    foreach(var n in allNeighbors)
                                    {
                                        if (visited[n.X, n.Y] && stroke.Count > 1 && (n.X != stroke[stroke.Count - 2].X || n.Y != stroke[stroke.Count - 2].Y))
                                        {
                                            stroke.Add(new Point(n.X, n.Y));
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            if (stroke.Count > 1) paths.Add(stroke.ToArray());
                        }
                    }
                }
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (data[y * stride + x] > 0 && !visited[x, y])
                    {
                        List<Point> stroke = new List<Point>();
                        int cx = x, cy = y;

                        while (true)
                        {
                            stroke.Add(new Point(cx, cy));
                            visited[cx, cy] = true;

                            var unvisitedN = GetUnvisitedNeighbors(cx, cy);
                            if (unvisitedN.Count > 0)
                            {
                                cx = unvisitedN[0].X;
                                cy = unvisitedN[0].Y;
                            }
                            else
                            {
                                var allNeighbors = GetAllNeighbors(cx, cy);
                                foreach(var n in allNeighbors)
                                {
                                    if (visited[n.X, n.Y] && stroke.Count > 1 && (n.X != stroke[stroke.Count - 2].X || n.Y != stroke[stroke.Count - 2].Y))
                                    {
                                        stroke.Add(new Point(n.X, n.Y));
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        if (stroke.Count > 1) paths.Add(stroke.ToArray());
                    }
                }
            }

            return paths;
        }

        private List<Point[]> OptimizePath(List<Point[]> contours)
        {
            if (contours.Count == 0) return new List<Point[]>();

            List<Point[]> optimized = new List<Point[]>(contours.Count);
            List<Point[]> unvisited = new List<Point[]>(contours);

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
    }
}
