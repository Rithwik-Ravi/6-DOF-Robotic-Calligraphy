$server = "192.168.0.20"
$port = 10003

Write-Host "Connecting to Robot on $server : $port..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    $stream = $tcpClient.GetStream()
    $stream.ReadTimeout = 5000
    $stream.WriteTimeout = 5000
    
    Write-Host "Connected!" -ForegroundColor Green
    
    # Drain any initial bytes the robot sends upon connection (e.g. INPUT prompt)
    Start-Sleep -Milliseconds 500
    $initBytes = ""
    while ($stream.DataAvailable) {
        $b = $stream.ReadByte()
        $initBytes += "[" + $b.ToString() + "=" + [char]$b + "] "
    }
    if ($initBytes) {
        Write-Host "Initial bytes from robot: $initBytes" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Type your command and press Enter."
    Write-Host "Example: MOV; -586.18;  783.00;  182.01"
    Write-Host "Type 'exit' to quit."
    Write-Host ""
    
    while ($tcpClient.Connected) {
        $userInput = Read-Host "Send"
        if ($userInput -eq "exit") { break }
        
        # Build the raw byte payload: the string + CR only (no LF)
        # OPT12 Packet Type is 0:CR, so the controller expects CR as delimiter
        $payload = $userInput + "`r"
        $bytes = [System.Text.Encoding]::ASCII.GetBytes($payload)
        
        Write-Host "-> Sending $($bytes.Length) bytes: [$userInput\r]"
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()

        # Wait for response
        Write-Host "Waiting for response..."
        $response = ""
        $timeout = 50  # 5 seconds
        while ($tcpClient.Connected -and $timeout -gt 0) {
            if ($stream.DataAvailable) {
                $b = $stream.ReadByte()
                if ($b -eq -1) { break }
                if ($b -eq 13 -or $b -eq 10) { 
                    if ($response.Length -gt 0) { break }
                    continue
                }
                $response += [char]$b
                $timeout = 50  # reset on each byte received
            } else {
                Start-Sleep -Milliseconds 100
                $timeout--
            }
        }
        
        if ($response) {
            Write-Host "Robot says: $response" -ForegroundColor Green
        } else {
            Write-Host "No response within 5 seconds." -ForegroundColor Red
        }
    }
    
    $tcpClient.Close()
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
