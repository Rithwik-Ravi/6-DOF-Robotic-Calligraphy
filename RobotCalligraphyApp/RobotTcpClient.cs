using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RobotCalligraphyApp
{
    public class RobotTcpClient : IDisposable
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;

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
        }

        public async Task<string?> SendAsync(string command)
        {
            if (_writer == null || _reader == null || !IsConnected)
                throw new InvalidOperationException("Not connected to the robot.");

            await _writer.WriteLineAsync(command);
            return await _reader.ReadLineAsync();
        }

        public void Disconnect()
        {
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
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
