# MZ_CNC Integration Checklist

**Created**: February 11, 2026  
**Status**: 5/21 Complete (24%)

---

## ✅ Completed Methods

1. ✅ `CheckMZ_CNC()` - Board detection (GRBL v1.1 banner)
2. ✅ `LineReceived()` - Serial data routing
3. ✅ `Write_m()` - Command sending
4. ✅ `JustConnected()` - Initialization
5. ✅ `RegularMoveTimeout` - Timeout property

---

## ⬜ TODO - CNC.cs Integration (16 methods)

### 🔴 CRITICAL PRIORITY
- [ ] **SetPosition()** - Line 282 - **USER REPORTED MISSING**
- [ ] **Nozzle_ProbeDown()** - Line 939 - **CRITICAL FOR LITEPLACER**
- [ ] **XYA()** - Line 1224 - Movement command
- [ ] **Home_m()** - Line 1176 - Homing command

### 🟡 HIGH PRIORITY - Movement
- [ ] **CancelJog()** - Line 312
- [ ] **Jog()** - Line 329
- [ ] **SetMachineSizeX()** - Line 808
- [ ] **SetMachineSizeY()** - Line 850

### 🟢 MEDIUM PRIORITY - Z-Axis Control  
- [ ] **DisableZswitches()** - Line 893
- [ ] **EnableZswitches()** - Line 916

### 🟢 MEDIUM PRIORITY - Motor/Power
- [ ] **MotorPowerOn()** - Line 981
- [ ] **MotorPowerOff()** - Line 1003

### 🟢 LOW PRIORITY - Peripherals
- [ ] **VacuumOn()** - Line 1038
- [ ] **VacuumOff()** - Line 1063
- [ ] **PumpOn()** - Line 1102
- [ ] **PumpOff()** - Line 1127

---

## ⬜ TODO - MZ_CNCControl.cs Methods (5 methods)

### 🔴 CRITICAL
- [ ] **SetPosition(X,Y,Z,A)** - Combined coordinate set
- [ ] **ProbeZ() result parsing** - Parse `[PRB:x,y,z,a:1]` response
- [ ] **UpdatePosition()** - Parse `<Idle|MPos:x,y,z|...>` status

### 🟡 HIGH
- [ ] **CancelJog()** - Implement jog cancel (Ctrl+X or $X)
- [ ] **Jog()** - Implement jog commands

### 🟢 MEDIUM  
- [ ] **DisableZswitches()** - May not apply to GRBL (TinyG specific?)
- [ ] **EnableZswitches()** - May not apply to GRBL (TinyG specific?)
- [ ] **SetMachineSizeX/Y()** - GRBL $130/$131 settings
- [ ] **VacuumOn/Off()** - M7/M9 coolant commands (if used for vacuum)
- [ ] **PumpOn/Off()** - M8/M9 coolant commands (if used for pump)
- [ ] **MotorPowerOn/Off()** - May not apply (GRBL enables motors automatically)

---

## 📝 Implementation Notes

### GRBL vs TinyG Protocol Differences

**TinyG Specific (May Not Apply to GRBL)**:
- JSON format commands (`{"zsn":0}`)
- DisableZswitches/EnableZswitches (TinyG-specific switch reconfiguration)
- MotorPowerOn/Off (TinyG has explicit motor enable/disable)

**GRBL Equivalent Commands**:
- Vacuum: M7 (mist coolant) or M8 (flood coolant)  
- Pump: M8 (flood coolant) or M9 (all off)
- Machine Size: $130 (X max), $131 (Y max), $132 (Z max)
- Jog: $J=X10Y10F500 (GRBL jogging mode)
- CancelJog: Ctrl+X (soft reset) or feed hold (!)

### Probe Command Comparison
- **TinyG**: G28.4 (probe toward max, configurable switch)
- **GRBL**: G38.2 (probe toward, alarm on fail) / G38.3 (probe toward, no alarm)

---

## 🎯 Work Order (Recommended)

### Session 1: Critical Methods (2-3 hours)
1. Fix `SetPosition()` in CNC.cs - Add MZ_CNC case
2. Implement `SetPosition(X,Y,Z,A)` in MZ_CNCControl.cs
3. Test basic position setting

### Session 2: Movement Commands (2-3 hours)
4. Add `XYA()` routing in CNC.cs
5. Add `Home_m()` routing in CNC.cs
6. Add `CancelJog()`, `Jog()` routing
7. Test basic movement

### Session 3: Probe Integration (3-4 hours) 🔴 CRITICAL
8. Add `Nozzle_ProbeDown()` routing in CNC.cs
9. Implement probe result parsing in `ProbeZ()`
10. Parse `[PRB:x,y,z,a:1]` response
11. Update `Cnc.CurrentZ` with triggered position
12. Test probe workflow (most critical for LitePlacer!)

### Session 4: Remaining Methods (2-3 hours)
13. Add all remaining CNC.cs routing
14. Implement peripheral controls (vacuum, pump)
15. Implement machine size configuration
16. Test comprehensive functionality

### Session 5: Testing & Polish (2-4 hours)
17. Test all methods with PIC32MZ hardware
18. Fix any protocol issues discovered
19. Document any GRBL-specific quirks
20. Update STATUS.md with results

---

## ✅ Acceptance Criteria

- [ ] All 16 CNC.cs methods have MZ_CNC routing
- [ ] All critical MZ_CNCControl.cs methods implemented
- [ ] SetPosition() works correctly (user reported issue resolved)
- [ ] Probe workflow functional (critical for LitePlacer)
- [ ] Basic movement commands work (XY, Z, A, XYA)
- [ ] Homing works ($H command)
- [ ] Error/alarm handling verified
- [ ] No compiler errors
- [ ] No runtime crashes with MZ_CNC selected

---

## 📚 Reference Documents

- [STATUS.md](STATUS.md) - Overall development plan
- [MZ_CNCControl.cs](LitePlacer/MZ_CNCControl.cs) - Controller implementation
- [CNC.cs](LitePlacer/CNC.cs) - Routing layer
- [Pic32mzCNC_V3 README.md](../Pic32mzCNC_V3/README.md) - Firmware documentation

---

**Next Action**: Start with SetPosition() - it's user-reported and foundational!
