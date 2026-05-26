$port = 5555
$endpoint = new-object System.Net.IPEndPoint([system.net.ipaddress]::Any, $port)
$listener = new-object System.Net.Sockets.TcpListener($endpoint)
$listener.start()
Write-Host "Mock Robot Server is running on port $port."
Write-Host "Waiting for the WinForms app to connect..."

$client = $listener.AcceptTcpClient()
Write-Host "App Connected!"

$stream = $client.GetStream()
$reader = new-object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
$writer = new-object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

while ($client.Connected) {
    if ($stream.DataAvailable) {
        $line = ""
        while ($client.Connected) {
            $char = $reader.Read()
            if ($char -eq -1) { break }
            if ($char -eq 13 -or $char -eq 10) { break }
            $line += [char]$char
        }
        
        if ($line -eq "STOP") {
            Write-Host "Received STOP command. Closing..."
            break
        }
        if ($line) {
            Write-Host "Received from App: $line"
            # Crucial: Send ACK back so the C# app can proceed with the next point!
            $writer.WriteLine("ACK")
        }
    }
    Start-Sleep -Milliseconds 10
}
$client.Close()
$listener.Stop()
