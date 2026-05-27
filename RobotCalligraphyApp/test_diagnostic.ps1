$server = "192.168.0.20"
$port = 10003

Write-Host "============================================"
Write-Host "  DIAGNOSTIC TEST for CR800 Communication"
Write-Host "============================================"
Write-Host ""
Write-Host "Connecting to $server : $port ..."

try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    $stream = $tcpClient.GetStream()
    $stream.ReadTimeout = 5000
    $stream.WriteTimeout = 5000

    Write-Host "[OK] Connected!" -ForegroundColor Green
    Write-Host ""

    # ==========================================
    # PHASE 1: Drain any bytes sent on connect
    # ==========================================
    Write-Host "--- PHASE 1: Reading initial bytes (waiting 1 second) ---" -ForegroundColor Cyan
    Start-Sleep -Milliseconds 1000
    $initResponse = ""
    $initRaw = @()
    while ($stream.DataAvailable) {
        $b = $stream.ReadByte()
        $initRaw += $b
        $initResponse += [char]$b
    }
    if ($initRaw.Count -gt 0) {
        Write-Host "  Received $($initRaw.Count) initial bytes" -ForegroundColor Yellow
        Write-Host "  Raw bytes: $($initRaw -join ', ')" -ForegroundColor Yellow
        Write-Host "  As text:   [$initResponse]" -ForegroundColor Yellow
    } else {
        Write-Host "  No initial bytes received." -ForegroundColor Gray
    }
    Write-Host ""

    # ==========================================
    # PHASE 2: Send the coordinate string
    # ==========================================
    Write-Host "--- PHASE 2: Sending coordinate string ---" -ForegroundColor Cyan
    $command = "MOV; -586.18;  783.00;  182.01"
    
    # Send with CR only (matching OPT12 Packet Type: CR)
    $payload = $command + "`r"
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($payload)
    
    Write-Host "  Payload text: [$command\r]"
    Write-Host "  Payload bytes ($($bytes.Length)): $($bytes -join ', ')"
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
    Write-Host "  [SENT]" -ForegroundColor Green
    Write-Host ""

    # ==========================================
    # PHASE 3: Read ALL responses for 10 seconds
    # ==========================================
    Write-Host "--- PHASE 3: Reading ALL responses for 10 seconds ---" -ForegroundColor Cyan
    Write-Host "  (This captures everything the robot sends back)" -ForegroundColor Gray
    Write-Host ""
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $allBytes = @()
    $lineBuffer = ""
    $lineCount = 0
    
    while ($stopwatch.Elapsed.TotalSeconds -lt 10) {
        if ($stream.DataAvailable) {
            $b = $stream.ReadByte()
            if ($b -eq -1) { break }
            $allBytes += $b
            
            if ($b -eq 13 -or $b -eq 10) {
                # End of line
                if ($lineBuffer.Length -gt 0) {
                    $lineCount++
                    $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
                    Write-Host "  Response #$lineCount (at ${elapsed}s): [$lineBuffer]" -ForegroundColor Green
                    $lineBuffer = ""
                }
            } else {
                $lineBuffer += [char]$b
            }
        } else {
            Start-Sleep -Milliseconds 50
        }
    }
    
    # Flush any remaining buffer
    if ($lineBuffer.Length -gt 0) {
        $lineCount++
        Write-Host "  Response #$lineCount (final): [$lineBuffer]" -ForegroundColor Green
    }
    
    $stopwatch.Stop()
    Write-Host ""
    Write-Host "--- RESULTS ---" -ForegroundColor Cyan
    Write-Host "  Total bytes received: $($allBytes.Count)"
    Write-Host "  Total response lines: $lineCount"
    if ($allBytes.Count -gt 0) {
        Write-Host "  All raw bytes: $($allBytes -join ', ')"
        $fullText = [System.Text.Encoding]::ASCII.GetString([byte[]]$allBytes)
        Write-Host "  Full text: [$fullText]"
    }
    
    $tcpClient.Close()
    Write-Host ""
    Write-Host "[DONE] Connection closed." -ForegroundColor Gray
    
} catch {
    Write-Host "[ERROR] $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
