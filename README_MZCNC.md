# LitePlacer - MZ_CNC Integration Update

## ?? **NEW: PIC32MZ GRBL Controller Support!**

LitePlacer now supports **three CNC controller types**:
- ? **TinyG** (original)
- ? **SKR3** (grblHAL)
- ? **MZ_CNC** (PIC32MZ GRBL v1.1) **? NEW!**

---

## What's New in This Release

### MZ_CNC Controller Integration (February 2026)

**Full GRBL v1.1 support** for PIC32MZ-based controllers:

#### ? **Automatic Detection**
- Auto-detects "Grbl 1.1h" firmware on connection
- Downloads all 34 GRBL `$$` settings automatically
- Seamlessly switches UI between controller types

#### ? **Motor Settings UI**
- **New MZ_CNC Settings tabs** in Basic Setup
- Configure all 4 axes (X, Y, Z, A) independently
- Real-time settings changes via GRBL `$xxx=value` commands
- Settings:
  - Speed (max rate) - `$110`, `$111`, `$112`, `$113`
  - Acceleration - `$120`, `$121`, `$122`, `$123`
  - Steps/mm calculation and display
  - Homing speed (seek rate) - `$26`
  - Homing backoff (pull-off) - `$30`
  - Motor current, microsteps, interpolation (stored in AppSettings)

#### ? **Movement & Control**
- Standard GRBL G-code movement commands
- Jogging with feed hold (`!`) support
- Coordinated multi-axis moves (XY, XYA)
- Position tracking and work coordinate system (G92)

#### ? **Homing & Probing**
- GRBL `$H` homing cycle
- G38.2 probing for Z-height measurement
- Probe result parsing: `[PRB:x,y,z,a:1]`

#### ? **Hardware Features**
- Motor enable/disable (M17/M18)
- Vacuum control via M7 (mist coolant)
- Pump control via M8 (flood coolant)

---

## Controller Comparison

| Feature | TinyG | SKR3 (grblHAL) | MZ_CNC (GRBL v1.1) |
|---------|-------|----------------|---------------------|
| **Protocol** | JSON | GRBL | GRBL |
| **Settings** | JSON objects | `$xxx=value` | `$xxx=value` |
| **Homing** | G28.3 | `$H` | `$H` |
| **Probing** | G28.4 | G38.2 | G38.2 |
| **Vacuum** | Digital I/O | M7 (mist) | M7 (mist) |
| **Pump** | Digital I/O | M8 (flood) | M8 (flood) |
| **Motor Power** | Always on | M17/M18 | M17/M18 |
| **UI Support** | ? Complete | ? Complete | ? Complete |

---

## Installation

### Prerequisites

Same as before, plus:

* **For MZ_CNC users:** PIC32MZ GRBL v1.1 firmware (tested with Pic32mzCNC_V3)

### Building from Source

1. Follow original LitePlacer build instructions
2. Install AForge.NET
3. Install MathNet.Numerics via NuGet
4. Add HomographyEstimation.dll reference
5. Build solution (supports all three controllers automatically)

---

## Using MZ_CNC Controller

### First Connection

1. **Connect your MZ_CNC board** to USB serial port
2. **Select the port** in LitePlacer's Basic Setup tab
3. **Click "Connect"**
4. LitePlacer will:
   - Auto-detect GRBL firmware
   - Download all settings (`$$` command)
   - Populate motor settings UI
   - Display "MZ_CNC" in the Board label

### Configuring Motors

1. Go to **Basic Setup** tab
2. Find the **MZ_CNC Motors** tab control (visible when connected)
3. Select axis tab: **X**, **Y**, **Z**, or **A**
4. **Edit values** in textboxes
5. **Press Enter** to send changes to controller
6. Settings are immediately applied to GRBL firmware

**Example:**
- Change X-axis speed from `5000.0` to `6000.0`
- Press **Enter**
- Command sent: `$110=6000.0`
- Controller responds: `ok`
- UI shows: "X max rate set to 6000 mm/min"

### Settings Stored

**In GRBL Firmware (persistent):**
- Speed, acceleration, steps/mm
- Homing speed, homing backoff
- Limit switch configuration
- Work coordinate offsets

**In LitePlacer AppSettings:**
- Motor current (not supported by GRBL)
- Microsteps display (calculated from steps/mm)
- Step angle (1.8° or 0.9°)
- Interpolation enable/disable

**Settings File Management:**
- **Separate settings files** per controller: `TinyG_Settings.json`, `MZ_CNC_Settings.json`
- **Board Settings Save** button: Saves current MZ_CNC configuration to file
- **Board Settings Load** button: Loads configuration from file and writes to controller
- **Board Built-In Settings** button: Resets MZ_CNC to default values
- Settings auto-saved on application exit (if enabled in preferences)

---

## PIC32MZ Firmware Notes

### Known Quirks

**Homing Pull-Off Units (`$30`):**
- PIC32MZ firmware reports `$30` in **firmware-specific units** (not mm!)
- Conversion factor: **÷12000** to get mm value
- Example: `$30=12000` displayed as `1.000` mm in UI
- When sending updates: multiply mm × 12000 for firmware value

**GRBL Settings Format:**
- Always use `CultureInfo.InvariantCulture` for decimal point (`.` not `,`)
- Example: `$110=5000.0` NOT `$110=5000,0`

---

## Files Added/Modified

### New Files
- `LitePlacer\MZCNCSettings.cs` - Settings UI and event handlers

### Modified Files
- `LitePlacer\CNC.cs` - Added MZ_CNC routing to 21 methods
- `LitePlacer\MZ_CNCControl.cs` - Complete GRBL command implementation
- `LitePlacer\MainForm.cs` - State machine for controller type switching
- `LitePlacer\MainForm.Designer.cs` - MZ_CNC motor tab controls
- `STATUS.md` - Complete development documentation
- `.github\copilot-instructions.md` - GRBL command reference

---

## Troubleshooting

### MZ_CNC Not Detected
1. Check that firmware responds to `\x18` (Ctrl-X soft reset)
2. Verify response contains "Grbl" string
3. Check serial monitor for actual banner string
4. Update detection logic if needed (see STATUS.md)

### Settings Not Updating
1. Ensure you're **pressing Enter** after typing value
2. Check serial monitor for command echo: `==> $110=6000.0`
3. Verify response is `ok` not `error:xx`
4. Some settings require homing cycle or reset to apply

### Position Not Tracking
1. Send `?` status query manually
2. Check for `<Idle|MPos:x,y,z,a|...>` response format
3. Verify position updates in UI
4. Check that position parsing is working

---

## Documentation

**For detailed implementation notes, see:**
- `STATUS.md` - Complete development history and testing notes
- `.github/copilot-instructions.md` - GRBL command reference for developers
- `Plan/*.md` - Architecture and planning documents

---

## Contributing

When modifying MZ_CNC support:

1. **Read `STATUS.md` first** - Check what's been tried
2. **Follow GRBL protocol** - Use standard GRBL v1.1 commands
3. **Test on hardware** - Verify changes work with actual board
4. **Update documentation** - Add entry to STATUS.md
5. **Use InvariantCulture** - All decimal formatting for GRBL commands

---

## License

Same as original LitePlacer project.

---

## Acknowledgments

**Original LitePlacer:** Juha Kuusama (JuKu)  
**MZ_CNC Integration:** Dave C. (2026)  
**Testing:** Community contributors

---

## Version History

**v2.0.0 (February 2026) - Multi-Controller Support**
- ? Added MZ_CNC (GRBL v1.1) controller support
- ? Fixed concurrency issues in all controllers
- ? Enhanced UI with controller-specific settings
- ? Improved thread safety and error handling

**v1.x (Historical)**
- Original TinyG-only version
- Partial SKR3 integration (completed in v2.0)

---

*Last Updated: February 11, 2026*
