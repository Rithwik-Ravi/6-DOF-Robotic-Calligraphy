using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RobotCalligraphyApp.ToolpathEngine;

namespace RobotCalligraphyApp.CoreNetworking
{
    public class RobotTcpClient : IDisposable
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        
        // 5-point lookahead buffer for responsive pausing
        public const int LookaheadBufferSize = 5;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(LookaheadBufferSize, LookaheadBufferSize);
        private CancellationTokenSource? _receiveCts;

        public bool IsConnected => _client?.Connected ?? false;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            Disconnect(); // Ensure previous connection is closed

            _client = new TcpClient();
            _client.NoDelay = true; // Disable Nagle Algorithm

            await _client.ConnectAsync(ipAddress, port);

            var stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.ASCII);
            _writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Start background listening task for ACKs
            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveAcksAsync(_receiveCts.Token));
        }

        private async Task ReceiveAcksAsync(CancellationToken token)
        {
            if (_reader == null) return;
            
            try
            {
                var sb = new StringBuilder();
                char[] buffer = new char[1];
                
                while (!token.IsCancellationRequested && IsConnected)
                {
                    int bytesRead = await _reader.ReadAsync(buffer, 0, 1);
                    if (bytesRead > 0)
                    {
                        if (buffer[0] == '\r')
                        {
                            string response = sb.ToString();
                            sb.Clear();
                            
                            if (response.StartsWith("ACK"))
                            {
                                // Release one slot in our sliding window pipeline
                                _semaphore.Release();
                            }
                        }
                        else if (buffer[0] != '\n')
                        {
                            sb.Append(buffer[0]);
                        }
                    }
                    else
                    {
                        // Connection lost
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle disconnects/disposed streams in background task
            }
        }

        public async Task<string?> SendAsync(string command)
        {
            if (_writer == null || !IsConnected)
                throw new InvalidOperationException("Not connected to the robot.");

            // Wait until the robot has space in its 50-point buffer
            await _semaphore.WaitAsync();

            try
            {
                // Send command terminated by pure CR
                await _writer.WriteAsync(command + "\r");
                await _writer.FlushAsync();
                
                return "ACK_QUEUED"; // Return pseudo-ACK to satisfy legacy pipeline logic in Form1.cs
            }
            catch
            {
                // If writing fails, release the semaphore to prevent deadlock on disconnects
                _semaphore.Release();
                throw;
            }
        }

        public async Task<string?> SendHomeAsync()
        {
            string pt = $"MOV;{470.00f,8:F2};{-945.00f,8:F2};{200.00f,8:F2}";
            return await SendAsync(pt);
        }

        public async Task<string?> SendWaypointAsync(RoboticWaypoint wp, bool isFirstMove)
        {
            string commandType = isFirstMove ? "MOV" : "MVS";
            string pt = $"{commandType};{wp.X,8:F2};{wp.Y,8:F2};{wp.Z,8:F2}";
            return await SendAsync(pt);
        }

        public async Task<string?> SendWaypoint6DOFAsync(RobotCalligraphyApp.Pipelines_3D.Core.RoboticWaypoint6DOF wp, bool isFirstMove)
        {
            string commandType = isFirstMove ? "MOV" : "MVS";
            string pt = $"{commandType};{wp.X,8:F2};{wp.Y,8:F2};{wp.Z,8:F2}";
            return await SendAsync(pt);
        }

        public void Disconnect()
        {
            _receiveCts?.Cancel();

            if (_writer != null)
            {
                try { _writer.Dispose(); } catch { }
                _writer = null;
            }

            if (_reader != null)
            {
                try { _reader.Dispose(); } catch { }
                _reader = null;
            }

            if (_client != null)
            {
                try { _client.Close(); } catch { }
                _client = null;
            }

            // Reset semaphore to 50
            _semaphore.Dispose();
            _semaphore = new SemaphoreSlim(50, 50);
        }

        public void Dispose()
        {
            Disconnect();
            _receiveCts?.Dispose();
        }
    }
}
