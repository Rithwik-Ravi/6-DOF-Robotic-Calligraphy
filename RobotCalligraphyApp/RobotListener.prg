' ========================================================
' TASK 1: NETWORK LISTENER (PRODUCER)
' ========================================================
M1 = 1 ' Head Index (1 to 10)
M2 = 1 ' Tail Index (1 to 10)

P3 = (+470.00, -945.00, +200.00, +3.13, +0.53, -36.52)(7,0)
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

    P11 = P3
    P11.X = Val(C3$)
    P11.Y = Val(C4$)
    P11.Z = Val(C5$)
    
    If C2$ = "MOV" Then
        M21 = 0
    Else
        M21 = 1
    EndIf
    
    ' Calculate Next Head
    M3 = M1 + 1
    If M3 > 10 Then M3 = 1
    
    ' Flow Control: Wait if buffer is full!
    *WAITSPACE
    If M3 = M2 Then GoTo *WAITSPACE

    ' Store Coordinate and Command in Global Variables (P1-P10, M11-M20)
    If M1 = 1 Then 
        P1 = P11
        M11 = M21
    EndIf
    If M1 = 2 Then 
        P2 = P11
        M12 = M21
    EndIf
    If M1 = 3 Then 
        P3 = P11
        M13 = M21
    EndIf
    If M1 = 4 Then 
        P4 = P11
        M14 = M21
    EndIf
    If M1 = 5 Then 
        P5 = P11
        M15 = M21
    EndIf
    If M1 = 6 Then 
        P6 = P11
        M16 = M21
    EndIf
    If M1 = 7 Then 
        P7 = P11
        M17 = M21
    EndIf
    If M1 = 8 Then 
        P8 = P11
        M18 = M21
    EndIf
    If M1 = 9 Then 
        P9 = P11
        M19 = M21
    EndIf
    If M1 = 10 Then 
        P10 = P11
        M20 = M21
    EndIf
    
    ' Advance Head
    M1 = M3
    
    ' Send ACK back to C# Client
    Print #1, "ACK"
GoTo *LOOP

*QUIT
CLOSE #1
END
