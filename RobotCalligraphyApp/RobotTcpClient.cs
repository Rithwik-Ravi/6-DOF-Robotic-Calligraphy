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

            // 1. Send command terminated by pure CR (Data Link mode, Packet Type: CR)
            await _writer.WriteAsync(command + "\r");
            await _writer.FlushAsync();

            // 2. Read response manually until CR (bypassing Windows ReadLine \n requirement)
            var sb = new System.Text.StringBuilder();
            char[] buffer = new char[1];
            while (await _reader.ReadAsync(buffer, 0, 1) > 0)
            {
                if (buffer[0] == '\r') break;
                if (buffer[0] != '\n') sb.Append(buffer[0]);
            }
            return sb.ToString();
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
