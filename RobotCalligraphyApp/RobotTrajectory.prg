' Initialize Safe Position using standard P1 variable
P1 = (+400.00, +0.00, +50.00, +180.00, +0.00, +180.00)
MOV P1
OPEN "COM13:" AS #1
CNT 1

*LOOP
INPUT #1, C1$
IF C1$ = "STOP" THEN GOTO *ENDPROG

' Find first semicolon position -> M11
M10 = LEN(C1$)
M13 = 1
*F1
  C2$ = MID$(C1$, M13, 1)
  IF C2$ = ";" THEN GOTO *F1END
  M13 = M13 + 1
  IF M13 <= M10 THEN GOTO *F1
*F1END
M11 = M13

' Find second semicolon position -> M12
M13 = M11 + 1
*F2
  C2$ = MID$(C1$, M13, 1)
  IF C2$ = ";" THEN GOTO *F2END
  M13 = M13 + 1
  IF M13 <= M10 THEN GOTO *F2
*F2END
M12 = M13

' Extract values into standard M variables
C3$ = MID$(C1$, 1, M11 - 1)
M1 = VAL(C3$)

C4$ = MID$(C1$, M11 + 1, M12 - M11 - 1)
M2 = VAL(C4$)

C5$ = MID$(C1$, M12 + 1, M10 - M12)
M3 = VAL(C5$)

' Move to target using standard P2 variable
P2 = P1
P2.X = M1
P2.Y = M2
P2.Z = M3
MVS P2
PRINT #1, "ACK"
GOTO *LOOP

*ENDPROG
CLOSE #1
MOV P1
END
