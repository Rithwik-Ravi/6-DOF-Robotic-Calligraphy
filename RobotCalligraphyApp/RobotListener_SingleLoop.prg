' ========================================================
' LEGACY TASK 1: SINGLE-LOOP NETWORK LISTENER
' ========================================================
Spd 300
CNT 0
P3 = (-581.59, +773.48, +150.00, +179.47, +0.04, +127.18)(7,1048576)
OPEN "COM2:" AS #1

' Wait endlessly until a client actually connects!
*WAITCONN
If M_Open(1) = 0 Then GoTo *WAITCONN

*LOOP
    INPUT #1, C1$
    If C1$ = "STOP" Then GoTo *QUIT

    C2$ = Mid$(C1$, 1, 3)
    C3$ = Mid$(C1$, 5, 8)
    C4$ = Mid$(C1$, 14, 8)
    C5$ = Mid$(C1$, 23, 8)

    P2 = P3
    P2.X = Val(C3$)
    P2.Y = Val(C4$)
    P2.Z = Val(C5$)
    
    ' Send ACK *before* moving to hide network latency!
    ' The next point will buffer while the robot is moving.
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
