# Multi-Firmware Refactor Roadmap

## STATUS: PLANNING PHASE - DO NOT START YET

---

## Prerequisites (Current Phase)
- [x] Create Plan folder structure
- [x] Create .copilot-instructions.md
- [ ] Complete architecture analysis
- [ ] Document current TinyG protocol usage
- [ ] Map all CNC.cs methods that need abstraction
- [ ] Test current codebase thoroughly with hardware
- [ ] Document all quirks and gotchas
- [ ] Understand coordinate transformation completely
- [ ] Understand vision system integration points

---

## Phase 1: Interface Definition (1-2 weeks)
**Branch:** `feature/firmware-di-refactor` (NOT created yet)

### Prerequisites Before Starting
- Complete understanding of existing CNC.cs API
- All protocol differences documented
- Clear list of breaking vs non-breaking changes
- Test hardware available (both TinyG and PIC33MZ)

### Tasks
- [ ] Create `LitePlacer/Firmware/IFirmwareInterface.cs`
- [ ] Define core methods (Move, Home, GetState, etc.)
- [ ] Define event model (PositionChanged, ErrorOccurred, etc.)
- [ ] Create DTO classes (Position, MachineState, etc.)
- [ ] Add Microsoft.Extensions.DependencyInjection NuGet package
- [ ] Create unit test project (optional but recommended)

### Deliverables
- Clean interface definition with XML documentation
- Example implementations (skeleton)
- Test plan document

---

## Phase 2: GRBL Implementation (2-3 weeks)

### Tasks
- [ ] Create `LitePlacer/Firmware/GrblFirmware.cs`
- [ ] Implement all IFirmwareInterface methods
- [ ] GRBL response parser (status, errors, settings)
- [ ] Custom command support for PIC33MZ extensions
- [ ] Error code mapping and descriptions
- [ ] Test with PIC33MZ hardware in isolation

### Testing Checklist
- [ ] Connection/disconnection stability
- [ ] Homing all axes
- [ ] Individual axis moves (X, Y, Z, A separately)
- [ ] Combined moves (XYA)
- [ ] Position reporting accuracy
- [ ] Error handling and recovery
- [ ] Timeout behavior
- [ ] Pump/vacuum control
- [ ] Multi-command sequences

---

## Phase 3: Legacy Adapter (1 week)

### Tasks
- [ ] Create `LitePlacer/Firmware/TinyGFirmware.cs`
- [ ] Wrap existing TinyG code with IFirmwareInterface
- [ ] Ensure backward compatibility
- [ ] No breaking changes to existing users
- [ ] Test with TinyG hardware (if available)

### Backward Compatibility
- Existing TinyG users should see no difference
- All existing features continue to work
- Settings migrate automatically

---

## Phase 4: CNC Controller Refactor (2 weeks)

### Tasks
- [ ] Create `LitePlacer/CNC/CNCController.cs`
- [ ] Accept IFirmwareInterface via constructor injection
- [ ] Migrate public API from old CNC.cs
- [ ] Maintain backward compatibility layer temporarily
- [ ] Handle firmware-specific edge cases
- [ ] Update position tracking logic

### API Migration
- Keep existing method signatures where possible
- Deprecate firmware-specific methods
- Add firmware-agnostic alternatives

---

## Phase 5: UI Integration (2-3 weeks)

### Tasks
- [ ] Add firmware selector dropdown to Basic Setup tab
- [ ] Update Program.cs for DI container setup
- [ ] Inject CNCController into FormMain constructor
- [ ] Update all button click handlers
- [ ] Add firmware-specific settings panels (show/hide based on selection)
- [ ] Remove hardcoded firmware-specific conditionals
- [ ] Update settings save/load to include firmware type

### UI Changes
- Firmware type dropdown near serial port selector
- Dynamic motor settings panel (show relevant controls)
- Status indicator shows firmware name and version
- Settings validation per firmware type

---

## Phase 6: Testing & Validation (1-2 weeks)

### Testing Checklist
- [ ] End-to-end testing with GRBL/PIC33MZ firmware
- [ ] Regression testing with TinyG (if hardware available)
- [ ] Regression testing with SKR3/Marlin
- [ ] Camera integration still works
- [ ] Coordinate transforms still accurate
- [ ] Pick and place workflow complete
- [ ] Settings persistence works
- [ ] Error recovery scenarios

### Performance Testing
- [ ] Movement latency comparable to original
- [ ] No memory leaks during long runs
- [ ] UI responsiveness acceptable

---

## Phase 7: Cleanup & Documentation (1 week)

### Tasks
- [ ] Remove old CNC.cs (archive in git history)
- [ ] Remove obsolete TinyG-specific code
- [ ] Update README.md with firmware support matrix
- [ ] Create firmware integration guide
- [ ] Document custom command extensions
- [ ] Update user manual

---

## Phase 8: Polish & Enhancements (Optional, Ongoing)

### Optional Improvements
- [ ] Add async/await support (breaking change - major refactor)
- [ ] Fix UI layout/resize issues
- [ ] Add firmware auto-detection via version query
- [ ] Improve error messages with context
- [ ] Add telemetry/logging framework
- [ ] Create firmware simulator for testing without hardware

---

## Decision Log

### 2026-01-03: Keep Projects Separate
- ? LitePlacer (C# app) stays in current repo
- ? PIC33MZ firmware (C) stays in separate repo/VSCode workspace
- ? No Git submodules for now
- ? Communication only via serial protocol
- **Reason:** Simpler version control, independent development cycles

### 2026-01-03: No Immediate Branch
- ? Learn codebase first on master branch
- ? Create branch only when ready to refactor
- ? Document everything in Plan/ folder
- **Reason:** Avoid premature optimization, understand before modifying

### 2026-01-03: DI-Based Firmware Abstraction
- ? Use interface-based design with dependency injection
- ? Target: `IFirmwareInterface` with multiple implementations
- ? Use Microsoft.Extensions.DependencyInjection
- **Reason:** Testable, maintainable, follows modern C# best practices

### 2026-01-03: Keep Existing Camera/Vision Code
- ? Camera.cs and ImagesForCameras.cs remain unchanged
- ? Vision algorithms proven and working
- ? Only refactor CNC communication layer
- **Reason:** Don't fix what isn't broken, reduce refactor scope

---

## Risk Assessment

### High Risk Items
- Breaking existing TinyG/SKR3 users
- Coordinate transform bugs introduced during refactor
- Performance regression
- Serial communication race conditions

### Mitigation Strategies
- Maintain backward compatibility layer
- Extensive testing with all firmware types
- Keep old code path as fallback during transition
- User acceptance testing before removing legacy code

---

## Timeline Estimate

| Phase | Duration | Depends On |
|-------|----------|-----------|
| Prerequisites | 2-3 weeks | Hardware availability, learning |
| Phase 1: Interfaces | 1-2 weeks | Prerequisites complete |
| Phase 2: GRBL | 2-3 weeks | Phase 1 |
| Phase 3: Legacy | 1 week | Phase 2 |
| Phase 4: Controller | 2 weeks | Phase 3 |
| Phase 5: UI | 2-3 weeks | Phase 4 |
| Phase 6: Testing | 1-2 weeks | Phase 5 |
| Phase 7: Cleanup | 1 week | Phase 6 |
| **Total** | **12-16 weeks** | - |

**Note:** Timeline assumes part-time work, learning curve, hardware debugging time.

---

*This roadmap will be updated as planning progresses*
*Do not start Phase 1 until user signals readiness*
