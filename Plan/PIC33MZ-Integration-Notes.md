# PIC33MZ CNC Firmware Integration Notes

## Purpose
Document specific details about the PIC33MZ GRBL-based firmware for LitePlacer integration.

---

## Hardware Specifications

### Microcontroller
- **Model:** PIC33MZ (exact part number TBD)
- **Clock Speed:** TBD
- **Flash/RAM:** TBD
- **Peripherals:** UART, SPI, I2C, PWM, etc.

### Development Environment
- **IDE:** VSCode (primary)
- **Compiler:** XC32
- **Framework:** MPLAB Harmony / MCC (MPLAB Code Configurator)
- **Abstraction Layer:** Custom layer over MCC-generated HAL
- **Build System:** Custom Makefile / tasks.json
- **Programmer:** PICkit 4 (or similar)

### Repository
- **Location:** Separate Git repository (path TBD)
- **Structure:**
  ```
  PIC33MZ_CNC/
  ??? src/
  ?   ??? main.c
  ?   ??? grbl_port.c
  ?   ??? cnc_control.c
  ?   ??? mcc_abstraction/
  ??? mcc_generated_files/
  ??? .vscode/
  ?   ??? tasks.json
  ?   ??? c_cpp_properties.json
  ??? Makefile
  ```

---

## Firmware Protocol

### Base Protocol: GRBL
- **Version:** GRBL 1.1 compatible (TBD - confirm version)
- **Standard Commands:** Supported
- **4-Axis Support:** Yes (X, Y, Z, A)

### Connection Parameters
- **Baud Rate:** 115200 (TBD - confirm)
- **Data Bits:** 8
- **Stop Bits:** 1
- **Parity:** None
- **Flow Control:** None
- **Line Ending:** `\n` (LF only, standard GRBL)

### Custom Commands (To Document)

#### Pump/Vacuum Control
```
// TBD - Document custom commands for:
// - Vacuum pump on/off
// - Pressure sensing?
// - Valve control?
```

#### Nozzle Selection
```
// TBD - If multiple nozzles supported:
// - Nozzle select command
// - Tool change sequence
```

#### Custom Settings
```
// TBD - Document custom $-commands:
// - $130-133 = axis travel (standard GRBL)
// - $XXX = custom PnP settings?
```

### Response Format

#### Status Report
Standard GRBL format with possible extensions:
```
<Idle|MPos:10.500,20.300,5.000,0.000|WPos:0.000,0.000,0.000,0.000|FS:500,0>

Fields:
- State: Idle, Run, Hold, Alarm, Home, Check
- MPos: Machine position (X, Y, Z, A)
- WPos: Work position (relative to G54/G55/etc)
- FS: Feed rate, Spindle speed (spindle = 0 for PnP)

Possible custom fields:
- Pn: Pin state (limit switches, probe)
- Ov: Override values
- A: Accessory state (vacuum, pump?)
```

#### Error Codes
Standard GRBL errors plus custom codes:
```
Standard GRBL:
error:1  - Expected command letter
error:2  - Bad number format
error:3  - Invalid statement
...

Custom codes (if any):
error:100 - Vacuum failure?
error:101 - Nozzle not selected?
// TBD - document custom error codes
```

---

## Hardware Features

### Axes Configuration
- **X-Axis:** Gantry (belt/leadscrew?)
  - Travel: TBD mm
  - Steps/mm: TBD
  - Max speed: TBD mm/min
  - Max acceleration: TBD mm/s²
  
- **Y-Axis:** Gantry
  - Travel: TBD mm
  - Steps/mm: TBD
  - Max speed: TBD mm/min
  - Max acceleration: TBD mm/s²
  
- **Z-Axis:** Nozzle vertical
  - Travel: TBD mm
  - Steps/mm: TBD
  - Max speed: TBD mm/min
  - Max acceleration: TBD mm/s²
  
- **A-Axis:** Nozzle rotation
  - Steps/degree: TBD
  - Max speed: TBD deg/s
  - Max acceleration: TBD deg/s²

### Limit Switches
- **X-Axis:** Min: ? Max: ?
- **Y-Axis:** Min: ? Max: ?
- **Z-Axis:** Min: ? Max: ?
- **A-Axis:** N/A (continuous rotation?)

### Homing Configuration
- **Homing Enabled:** Yes/No (TBD)
- **Homing Direction:** +X/+Y or -X/-Y? (TBD)
- **Homing Speed:** Fast seek + slow feed (TBD values)
- **Homing Pull-off:** Distance after switch hit (TBD)

---

## Pick-and-Place Specific Features

### Vacuum System
- **Control Method:** GPIO pin? PWM? Custom command?
- **Pin Assignment:** TBD
- **Feedback:** Pressure sensor? Yes/No
- **Commands:**
  ```
  // TBD - document commands:
  // M3 / M5 for spindle ? repurposed for vacuum?
  // Custom M-codes?
  ```

### Pump Control
- **Control Method:** GPIO? PWM?
- **Pin Assignment:** TBD
- **Commands:** TBD

### Lighting Control
- **LED Ring:** GPIO control?
- **Brightness:** PWM?
- **Commands:** TBD

---

## Differences from Standard GRBL

### Features Added
- [ ] 4th axis (A) support
- [ ] Vacuum pump control
- [ ] Pressure sensing
- [ ] Custom homing sequences
- [ ] Tool change macros
- [ ] Other (TBD)

### Features Removed/Disabled
- [ ] Spindle control (repurposed?)
- [ ] Coolant control (repurposed?)
- [ ] Laser mode
- [ ] Other (TBD)

### Modified Behaviors
- [ ] Feed rate handling for rotation axis
- [ ] Soft limits configuration
- [ ] Homing cycle order
- [ ] Other (TBD)

---

## Testing Plan

### Hardware Tests
- [ ] Serial communication at 115200 baud
- [ ] All axes movement (X, Y, Z, A)
- [ ] Homing cycle
- [ ] Limit switch detection
- [ ] Vacuum pump on/off
- [ ] Position reporting accuracy
- [ ] Emergency stop handling
- [ ] Coordinate system switching (G54/G55)

### Software Integration Tests
- [ ] Connect from LitePlacer
- [ ] Send movement commands
- [ ] Receive status reports
- [ ] Parse error messages
- [ ] Handle connection loss/recovery
- [ ] Timeout handling
- [ ] Long-running job stability

---

## Known Limitations

### Current Constraints
- TBD (document after testing)

### Future Enhancements
- TBD (feature wishlist)

---

## Firmware Update Procedure

### How to Flash PIC33MZ
1. Connect PICkit 4 programmer to ICSP header
2. Open MPLAB X (or use command-line tools)
3. Build firmware in VSCode (generates .hex file)
4. Flash using: `[command TBD]`
5. Reset board
6. Test serial communication

### Version Management
- **Current Version:** TBD
- **Version Query:** `$$` should show version info
- **Changelog Location:** TBD (firmware repo?)

---

## Questions for User (To Be Answered)

### Connection
- [ ] Exact baud rate? (115200 assumed)
- [ ] Hardware flow control needed?
- [ ] Reset on connect? (DTR/RTS behavior)

### Commands
- [ ] What custom commands exist?
- [ ] How is vacuum controlled?
- [ ] How is pump controlled?
- [ ] Any special initialization sequence?

### Status
- [ ] Status report frequency? (on-demand via `?` only?)
- [ ] Automatic status updates?
- [ ] What fields are in status report?

### Configuration
- [ ] How to set motor currents?
- [ ] How to configure axis limits?
- [ ] EEPROM settings supported?
- [ ] Settings reset command?

### Error Handling
- [ ] What custom error codes exist?
- [ ] How to clear alarm state?
- [ ] Recovery procedure from errors?

---

*This document will be updated as firmware details are discovered*
*User should fill in TBD sections during integration phase*
