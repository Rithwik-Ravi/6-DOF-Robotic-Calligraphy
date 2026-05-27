$server = "127.0.0.1"
$port = 10003

Write-Host "Connecting to Virtual Robot on $server : $port..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    $stream = $tcpClient.GetStream()
    $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
    
    Write-Host "Connected! Listening for PINGs..."
    
    while ($tcpClient.Connected) {
        if ($stream.DataAvailable) {
            $line = ""
            while ($tcpClient.Connected) {
                $char = $reader.Read()
                if ($char -eq -1) { break }
                if ($char -eq 13 -or $char -eq 10) { break }
                $line += [char]$char
            }
            if ($line) {
                Write-Host "Robot says: $line"
            }
        }
        Start-Sleep -Milliseconds 50
    }
} catch {
    Write-Host "Failed to connect. Make sure the robot is running and waiting at WAITCONN."
}
