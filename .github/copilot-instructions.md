# GitHub Copilot Instructions for LitePlacer-DEV

## Project Documentation

### Required Reading
Before making any changes to the codebase, **ALWAYS** consult:

1. **STATUS.md** - Development status, change log, and lessons learned
   - Contains comprehensive documentation of all changes made
   - Documents known issues and their root causes
   - Lists attempted solutions and their outcomes
   - **UPDATE THIS FILE** whenever you make code changes

2. **Plan/*.md** - Project planning and architecture documents
   - System architecture overview
   - Feature specifications
   - Design decisions

## Making Changes

### Change Documentation Protocol

When making ANY code change, you **MUST**:

1. **Read STATUS.md first** to understand:
   - What has already been tried
   - Why previous approaches failed
   - Current state of the codebase

2. **Update STATUS.md** immediately after making changes:
   - Add entry to "Changes Made" section
   - Document: File, Line numbers, Function name
   - Explain: What changed and WHY
   - Add entry to "Change Log" table
   - Update "Testing Status" if applicable

3. **Update the "Last Updated" date** at bottom of STATUS.md

### Change Log Entry Format

```markdown
#### Change N: [Brief Description]
**Location:** `FileName.cs`, [MethodName()], lines X-Y
**Commit:** [commit hash or "Uncommitted"]
**Why:** [Reason for change]

**What Changed:**
- Bullet list of specific changes
- Be detailed and specific

**Code Sample:**
```csharp
// Show key code changes
```

**Status:** ? Complete / ? In Progress / ? Failed
```

## Z-Axis Probing Context

**CRITICAL UNDERSTANDING:** 
- Z=0 is HOME (nozzle UP, away from PCB)
- Z=positive is DOWN (toward PCB)  
- Probing DOWN = moving to POSITIVE Z (e.g., G0 Z79)
- Z-max switch is at PCB surface (positive Z position)
- Z-min switch is at home (Z=0 position)

**Always verify coordinate system understanding before modifying Z-axis code!**

## TinyG JSON Commands

**CRITICAL:** TinyG uses **NON-STANDARD JSON** format with **COMMAS** instead of colons!

? **CORRECT for TinyG:** `{\"zsn\",3}`  
? **WRONG for TinyG:** `{\"zsn\":3}` (this is standard JSON but TinyG does NOT accept it)

**Important Notes:**
- This is NOT standard JSON syntax, but TinyG firmware accepts it
- **DO NOT "fix" these to use colons** - it will break TinyG communication
- All existing code uses comma syntax: `{\"param\",value}`
- Keep this format consistent throughout the codebase

**Examples of correct TinyG commands:**
```csharp
Write_m("{\"zsn\",3}", 150);      // Set Z-min switch mode
Write_m("{\"zsx\",2}", 150);      // Set Z-max switch mode
Write_m("{\"zzb\",2.0}", 150);    // Set zero backoff
Write_m("{\"st\",0}", 150);       // Set switch type
```

## MZ_CNC (GRBL) Commands

**CRITICAL:** MZ_CNC uses **STANDARD GRBL v1.1** protocol!

? **CORRECT for GRBL:** `$110=5000.0` (Settings)  
? **CORRECT for GRBL:** `G0 X10 Y20` (Movement)  
? **CORRECT for GRBL:** `G38.2 Z10` (Probing)  
? **CORRECT for GRBL:** `$H` (Homing)  

**Important Notes:**
- GRBL uses `$xxx=value` for settings (not JSON!)
- Settings use `CultureInfo.InvariantCulture` for decimal formatting (always `.` not `,`)
- PIC32MZ firmware has quirk: `$30` (homing pull-off) reported in firmware units, divide by 12000 for mm
- Use `$30` for homing pull-off (applies to all axes)
- Use `$26` for homing seek rate (applies to all axes)

**Examples of correct GRBL commands:**
```csharp
Write_m("$110=5000.0");           // Set X max rate
Write_m("$120=500.0");            // Set X acceleration
Write_m("$26=1000.0");            // Set homing seek rate
Write_m("$30=1.0");               // Set homing pull-off (in mm * 12000 for PIC32MZ)
Write_m("$H");                    // Start homing cycle
Write_m("G38.2 Z10 F100");       // Probe down to Z10 at 100mm/min
Write_m("M7");                    // Vacuum ON (mist coolant)
Write_m("M9");                    // Vacuum/Pump OFF (all coolant)
```

**GRBL Response Parsing:**
- Status reports: `<Idle|MPos:0.000,0.000,79.000,0.000|...>`
- Probe results: `[PRB:10.123,20.456,5.789,0.000:1]`
- Settings echo: `$110=5000.000` (confirms new value)
- Acknowledgment: `ok` (command accepted)

**CRITICAL - PIC32MZ Homing Settings:**
- `$25` = Homing seek rate (mm/min) - Standard GRBL
- `$27` = Homing pull-off (mm) - Standard GRBL  
- `$30` = Homing pull-off (firmware units) - **PIC32MZ QUIRK!**
  - Conversion: `$30 value ÷ 12000 = mm`
  - Example: `$30=24000` ? `24000 ÷ 12000 = 2.0 mm`
  - **Use $25 for homing seek, $30 for pull-off** (both exist!)

**MZ_CNC Settings UI:**
- Event handlers use **KeyPress** with **Enter key** to apply changes
- All event handlers send `$xxx=value` commands via `Cnc.MZ_CNC.Write_m()`
- Handlers validate input before sending to controller
- Success/failure logged with color-coded messages

**MZ_CNC Settings Files:**
- **Separate settings files** for each controller type (TinyG, SKR3, MZ_CNC)
- **Board Settings Save/Load** buttons repurposed for all controllers
- MZ_CNC uses `MZ_CNC_Settings.json` file format (like TinyG pattern)
- Settings include GRBL-writable values + AppSettings-only values
- Files stored in application directory (same as TinyG settings)

**Settings File Format:**
```
MZ_CNC  \n\r
{
  "XSpeed": 5000.0,
  "XAccel": 500.0,
  ...
}
```

## Code Style

### Comments
- Add comments ONLY when necessary to explain WHY, not WHAT
- Match existing comment style in the file
- Reference STATUS.md entry numbers in complex changes

### Logging
- Use color-coded logging for different severity:
  - `KnownColor.DarkRed` - Errors
  - `KnownColor.DarkOrange` - Warnings
  - `KnownColor.DarkGreen` - Success
  - `KnownColor.DarkCyan` - Informational

### Error Handling
- Always check return values from TinyG commands
- Provide clear error messages to user
- Log failures with context

## Testing Requirements

Before marking a change as complete:

1. ? Build succeeds with no errors
2. ? Code follows existing patterns in file
3. ? Change is documented in STATUS.md
4. ? If hardware-related, describe test procedure in STATUS.md
5. ? Update "Testing Status" section

## Git Workflow

### Current State
- Project is in detached HEAD state
- Need to create proper feature branch before committing

### Recommended Workflow
```bash
git checkout -b feature/descriptive-name
git add [files]
git commit -m "Descriptive commit message

Refs: STATUS.md changes section X"
```

### Commit Message Format
```
Short summary (50 chars or less)

Detailed explanation of what changed and why.
Reference STATUS.md change entry number.

Refs: STATUS.md Change #N
```

## File Organization

### Core Files
- `LitePlacer/TinyGControl.cs` - TinyG communication and control
- `LitePlacer/CNC.cs` - Generic CNC abstraction
- `LitePlacer/MainForm.cs` - UI and application logic
- `STATUS.md` - **Change documentation (ALWAYS UPDATE!)**

### When Modifying TinyGControl.cs
- Understand that this handles ALL TinyG communication
- Changes here affect machine safety - test carefully
- Document expected TinyG responses
- Always include timeout handling

## Safety Considerations

### Z-Axis Changes
- **ALWAYS** ensure limit switches are enabled after operations
- **NEVER** disable both Z-switches simultaneously during normal operation
- Test switch response before running full operations
- Add safety checks for `zzb` value (must be >0 for homing to work)

### Switch Configuration
Before changing switch settings:
1. Document current state
2. Explain why change is needed
3. Test on actual hardware if possible
4. Provide rollback procedure

## Questions to Ask Before Changing Code

1. **Has this been tried before?** ? Check STATUS.md "Solutions Attempted"
2. **Why did it fail last time?** ? Read failure analysis in STATUS.md
3. **What's the root cause?** ? Check "Root Cause Analysis" section
4. **Will this break existing functionality?** ? Review "Key Learnings"
5. **How will I test this?** ? Add to "Testing Status" section

## Emergency Procedures

If changes cause machine malfunction:

1. **Document in STATUS.md immediately:**
   - What was attempted
   - What went wrong
   - Current machine state
   - Recovery steps taken

2. **Update "Solutions Attempted" with ? Failed status**

3. **Add to "Known Issues" section**

4. **Consider reverting to last known good state:**
   ```bash
   git checkout [last-good-commit]
   ```

## AI Assistant Guidelines

### When Providing Solutions

1. **Read STATUS.md first** - Check what's already been tried
2. **Explain your reasoning** - Reference sections from STATUS.md
3. **Show what's different** - How is this different from failed attempts?
4. **Document in STATUS.md** - Add new entry for your change
5. **Update change log** - Add to table at bottom

### When User Reports Issue

1. **Check STATUS.md** - Is this a known issue?
2. **Review previous attempts** - What's been tried already?
3. **Analyze root cause** - Is it in "Root Cause Analysis"?
4. **Propose solution** - Explain how it differs from previous attempts
5. **Update documentation** - Add to STATUS.md

## Resources

### TinyG Documentation
- Configuration: https://github.com/synthetos/TinyG/wiki/TinyG-Configuration
- Switch Setup: https://github.com/synthetos/TinyG/wiki/TinyG-Configuration#switches
- Homing: https://github.com/synthetos/TinyG/wiki/TinyG-Homing
- Probing: https://github.com/synthetos/TinyG/wiki/TinyG-Probing

### LitePlacer Resources
- Main Repo: https://github.com/Davec6505/LitePlacer-DEV
- STATUS.md: [Root of repository]

---

**Remember:** STATUS.md is the single source of truth for what has been tried, what worked, and what didn't. **ALWAYS update it when making changes!**

---

*Last Updated: 2024-01-XX*
