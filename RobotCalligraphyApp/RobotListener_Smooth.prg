' ========================================================
' STREAMING LISTENER (Smooth Continuous Path)
' ========================================================
' Enable Continuous Path blending to prevent stuttering!
' This was the main reason the robot stuttered before (CNT 0)
CNT 1

P3 = (-581.59, +773.48, +150.00, +179.47, +0.04, +127.18)(7,1048576)

OPEN "COM2:" AS #1

' Wait endlessly until a client connects
*WAITCONN
If M_Open(1) = 0 Then GoTo *WAITCONN

*LOOP
    ' Blocking read from the C# application
    ' (C# will stream points continuously without waiting for ACKs)
    INPUT #1, C1$
    
    If C1$ = "STOP" Then GoTo *QUIT

    ' SAFETY CHECK: Ignore empty strings or malformed short strings 
    ' (These often occur due to leftover \r\n characters in the TCP buffer from previous runs)
    If Len(C1$) < 30 Then
        ' Just loop again and wait for the real string
        GoTo *LOOP
    EndIf

    ' Extract coordinate substrings safely now that we know the string is long enough
    C2$ = Mid$(C1$, 1, 3)
    C3$ = Mid$(C1$, 5, 8)
    C4$ = Mid$(C1$, 14, 8)
    C5$ = Mid$(C1$, 23, 8)

    P2 = P3
    P2.X = Val(C3$)
    P2.Y = Val(C4$)
    P2.Z = Val(C5$)

    ' The CR800 controller has a built-in read-ahead motion buffer (up to 32 points)
    ' Send ACK back to C# *before* blocking on the queue
    Print #1, "ACK"

    If C2$ = "MOV" Then
        Ovrd 10
        MOV P2
    ElseIf C2$ = "MVS" Then
        Ovrd 100
        MVS P2
    EndIf

GoTo *LOOP

*QUIT
CLOSE #1
END
