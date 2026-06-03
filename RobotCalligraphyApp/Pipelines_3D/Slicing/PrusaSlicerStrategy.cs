using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RobotCalligraphyApp.Pipelines_3D.Slicing
{
    public class PrusaSlicerStrategy
    {
        private string prusaSlicerPath = @"C:\Program Files\Prusa3D\PrusaSlicer\prusa-slicer-console.exe";

        public PrusaSlicerStrategy(string? path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                prusaSlicerPath = path;
            }
        }

        public async Task<string> SliceAsync(string stlPath)
        {
            if (!File.Exists(stlPath))
            {
                throw new FileNotFoundException($"STL file not found: {stlPath}");
            }

            string outputGcode = Path.ChangeExtension(stlPath, ".gcode");

            var tcs = new TaskCompletionSource<bool>();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = prusaSlicerPath,
                Arguments = $"-g \"{stlPath}\" --center 100,75 -o \"{outputGcode}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) => { /* Optional: Log stdout */ };
                process.ErrorDataReceived += (sender, e) => { /* Optional: Log stderr */ };
                process.Exited += (sender, e) =>
                {
                    if (process.ExitCode == 0)
                    {
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception($"Slicer failed with exit code {process.ExitCode}"));
                    }
                };
                process.EnableRaisingEvents = true;

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await tcs.Task;

                    if (File.Exists(outputGcode))
                    {
                        return outputGcode;
                    }
                    else
                    {
                        throw new Exception("Slicing completed but no output file was found.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to start or run PrusaSlicer: {ex.Message}");
                }
            }
        }
    }
}
