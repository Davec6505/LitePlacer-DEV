# Firmware Protocol Comparison

## Purpose
Compare communication protocols to understand differences and design proper abstraction.

---

## TinyG Protocol

### Connection
- **Baud Rate:** 115200
- **Line Ending:** `\n`
- **Format:** JSON-based commands and responses

### Commands (To Document)
```json
// Move command (TBD - document actual format from CNC.cs)
{"gc":"G0 X10.5 Y20.3 Z5.0 A0"}

// Status request
{"sr":null}

// Motor settings
{"1ma":1.4}  // Motor 1 current in amps
```

### Responses (To Document)
```json
// Command accepted
{"r":{"gc":0},"f":[1,0,0]}

// Status report
{"sr":{"posx":10.500,"posy":20.300,"posz":5.000,"posa":0.000}}
```

### Notes
- [ ] Document actual command format from code inspection
- [ ] Identify all JSON keys used
- [ ] Document error response format
- [ ] Map motor configuration parameters

---

## GRBL Protocol (PIC33MZ Target)

### Connection
- **Baud Rate:** 115200 (TBD - confirm with hardware)
- **Line Ending:** `\n`
- **Format:** Plain text G-code

### Standard GRBL Commands
```
G0 X10.5 Y20.3 Z5.0 A0    ; Rapid move (non-cutting)
G1 X10.5 Y20.3 F500        ; Linear move with feedrate
G28                        ; Return to home position
$H                         ; Run homing cycle
?                          ; Request status report
$$                         ; View all settings
$X                         ; Kill alarm lock
$C                         ; Check gcode mode
```

### Standard GRBL Responses
```
ok                         ; Command executed successfully
error:1                    ; Error with code number
<Idle|MPos:10.500,20.300,5.000,0.000|FS:500,8000>  ; Status report

Status report format:
<State|MPos:X,Y,Z,A|WPos:X,Y,Z,A|FS:Feed,Spindle|...>
```

### GRBL Error Codes (Standard)
```
error:1  - Expected command letter
error:2  - Bad number format
error:3  - Invalid statement
error:4  - Negative value
error:5  - Setting disabled
error:6  - Step pulse minimum
error:7  - EEPROM read fail
error:8  - Not idle (command requires idle state)
error:9  - G-code lock
error:10 - Homing not enabled
error:11 - Line overflow
// ... more error codes
```

### Custom Commands (User's PIC33MZ Firmware)
**To Be Documented:**
- [ ] Nozzle selection command (if different from standard)?
- [ ] Pump/vacuum control commands?
- [ ] Special homing modes?
- [ ] Tool change commands?
- [ ] Custom settings ($-commands)?

### Questions for User's Firmware
- [ ] What's the exact baud rate?
- [ ] Does it support 4th axis (A) natively?
- [ ] Are there custom $ commands for pick-and-place?
- [ ] How is vacuum/pump controlled?
- [ ] Status report frequency?

---

## Smoothie Protocol

### Connection
- **Baud Rate:** 115200
- **Line Ending:** `\n`
- **Format:** Plain text G-code (similar to GRBL)

### Commands (To Document)
```
G0 X10 Y20                 ; Rapid move
G1 X10 Y20 F500           ; Linear move
G28                        ; Home
M114                       ; Get current position
M999                       ; Reset
```

### Responses (To Document)
```
ok                         ; Command successful
// Position format TBD
```

---

## Protocol Comparison Matrix

| Feature | TinyG | GRBL | Smoothie |
|---------|-------|------|----------|
| **Format** | JSON | Plain G-code | Plain G-code |
| **Line End** | `\n` | `\n` | `\n` |
| **Baud Rate** | 115200 | 115200 | 115200 |
| **Success Response** | JSON with status | `ok` | `ok` |
| **Error Format** | JSON error object | `error:N` | TBD |
| **Status Query** | `{"sr":null}` | `?` | `M114` |
| **Homing** | `G28.2` | `$H` | `G28` |
| **4th Axis** | Native support | Depends on build | Native support |

## Command Translation Table

| Action | TinyG | GRBL | Smoothie |
|--------|-------|------|----------|
| Move XY | `{"gc":"G0 X_ Y_"}` | `G0 X_ Y_` | `G0 X_ Y_` |
| Home All | `{"gc":"G28.2 X0 Y0 Z0"}` | `$H` | `G28` |
| Get Status | `{"sr":null}` | `?` | `M114` |
| Reset | Soft reset byte | `Ctrl-X` | `M999` |

## Abstraction Requirements

### Must Support
1. Connection/disconnection with configurable baud rate
2. Move commands (XY, Z, A independently and combined)
3. Homing (all axes and individual)
4. Position reporting (machine and work coordinates)
5. Status query (idle, running, alarm, etc.)
6. Error handling and recovery
7. Motor configuration (speed, current)

### Nice to Have
1. Firmware version detection
2. Auto-detection of firmware type
3. Custom command extensions
4. Progress reporting during moves
5. Buffer status monitoring

---
*To be completed during protocol analysis phase*
*Update as actual commands are discovered in code*
