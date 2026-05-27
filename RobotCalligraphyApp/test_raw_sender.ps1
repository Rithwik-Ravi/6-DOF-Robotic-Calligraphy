$server = "192.168.0.20"
$port = 10003

Write-Host "Connecting to Virtual Robot on $server : $port..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    $stream = $tcpClient.GetStream()
    $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
    
    Write-Host "Connected!"
    Write-Host "Type exactly what you want to send and press Enter."
    Write-Host "Type exactly what you want to send and press Enter. (No quotes needed)"
    Write-Host "The script will automatically add the Carriage Return."
    Write-Host "Example: MOV;  500.00;  100.00;  800.00"
    Write-Host "Type 'exit' to quit."
    
    while ($tcpClient.Connected) {
        $userInput = Read-Host "Send to Robot"
        if ($userInput -eq "exit") { break }
        
        # Test WITHOUT quotes, just the raw string + CRLF
        $processed = $userInput + "`r`n"
        
        $writer.Write($processed)
        Write-Host "-> Sent payload."

        # Wait for the robot to move and reply
        Write-Host "Waiting for response..."
        $response = ""
        
        # Robust read that doesn't drop characters if they arrive slowly
        $timeout = 20
        while ($tcpClient.Connected -and $timeout -gt 0) {
            if ($stream.DataAvailable) {
                while ($stream.DataAvailable) {
                    $char = $reader.Read()
                    if ($char -eq -1) { break }
                    if ($char -eq 13 -or $char -eq 10) { 
                        if ($response.Length -gt 0) { break }
                        continue
                    }
                    $response += [char]$char
                }
                if ($response.Length -gt 0) { break } # We got a full line
            } else {
                Start-Sleep -Milliseconds 100
                $timeout--
            }
        }
        
        if ($response) {
            Write-Host "Robot says: $response" -ForegroundColor Green
        } else {
            Write-Host "No response received." -ForegroundColor Red
        }
    }
    
    $tcpClient.Close()
} catch {
    Write-Host "Failed to connect. Is the robot script running and waiting at WAITCONN?"
}
