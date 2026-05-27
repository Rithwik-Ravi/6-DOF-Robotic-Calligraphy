Ovrd 20
Spd 100
P3 = (-586.18, +783.00, +182.01, +177.94, +0.35, +119.72)(7,1048576)
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
    
    If C2$ = "MOV" Then
        MOV P2
    ElseIf C2$ = "MVS" Then
        MVS P2
    EndIf

    Print #1, "ACK"
GoTo *LOOP

*QUIT
CLOSE #1
END
