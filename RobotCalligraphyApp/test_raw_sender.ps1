$server = "127.0.0.1"
$port = 10003

Write-Host "Connecting to Virtual Robot on $server : $port..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    $stream = $tcpClient.GetStream()
    $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
    $writer.AutoFlush = $true
    
    Write-Host "Connected!"
    Write-Host "Type exactly what you want to send and press Enter."
    Write-Host "Note: \r will be converted to Carriage Return, \n to Line Feed."
    Write-Host "Example 1 (Standard): MOV;  400.00;    0.00;  300.00\r"
    Write-Host "Example 2 (Quoted): `"MOV;  400.00;    0.00;  300.00`"\r"
    Write-Host "Type 'exit' to quit."
    
    while ($tcpClient.Connected) {
        $userInput = Read-Host "Send to Robot"
        if ($userInput -eq "exit") { break }
        
        # Manually parse the escape characters so you can control exact terminators
        $processed = $userInput.Replace("\r", "`r").Replace("\n", "`n")
        
        $writer.Write($processed)
        Write-Host "-> Sent payload."

        # Wait a moment for the robot to move and reply
        Start-Sleep -Milliseconds 100
        
        # Read available responses
        $stream = $tcpClient.GetStream()
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
        
        while ($stream.DataAvailable) {
            $response = $reader.ReadLine()
            Write-Host "Robot says: $response" -ForegroundColor Green
        }
    $tcpClient.Close()
} catch {
    Write-Host "Failed to connect. Is the robot script running and waiting at WAITCONN?"
}
