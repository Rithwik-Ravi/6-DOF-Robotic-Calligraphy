' ========================================================
' TASK 2: MOTION EXECUTION (CONSUMER)
' ========================================================
MvTune 3
Spd 300

' ENABLE CONTINUOUS INTERPOLATION
CNT 1

*MOTIONLOOP
    ' Wait until there is a point in the buffer (Head != Tail)
    If M2 = M1 Then
        Dly 0.01
        GoTo *MOTIONLOOP
    EndIf

    ' Read the point from the Global Variables (P1-P10, M11-M20)
    If M2 = 1 Then 
        P11 = P1
        M21 = M11
    EndIf
    If M2 = 2 Then 
        P11 = P2
        M21 = M12
    EndIf
    If M2 = 3 Then 
        P11 = P3
        M21 = M13
    EndIf
    If M2 = 4 Then 
        P11 = P4
        M21 = M14
    EndIf
    If M2 = 5 Then 
        P11 = P5
        M21 = M15
    EndIf
    If M2 = 6 Then 
        P11 = P6
        M21 = M16
    EndIf
    If M2 = 7 Then 
        P11 = P7
        M21 = M17
    EndIf
    If M2 = 8 Then 
        P11 = P8
        M21 = M18
    EndIf
    If M2 = 9 Then 
        P11 = P9
        M21 = M19
    EndIf
    If M2 = 10 Then 
        P11 = P10
        M21 = M20
    EndIf

    ' Free the buffer slot immediately by advancing Tail
    M4 = M2 + 1
    If M4 > 10 Then M4 = 1
    M2 = M4

    ' Execute Motion
    If M21 = 0 Then
        Ovrd 10
        MOV P11
    Else
        Ovrd 100
        MVS P11
    EndIf

GoTo *MOTIONLOOP
END
