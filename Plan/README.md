# LitePlacer Planning Documents

This folder contains planning and learning documents for the LitePlacer firmware refactor project.

## Purpose

These documents track the **learning phase** before refactoring the LitePlacer codebase to support multiple CNC firmware types via a dependency injection architecture.

## Current Phase: LEARNING

**Status:** Understanding existing architecture, NOT refactoring yet.

## Document Structure

### `.copilot-instructions.md` (Root Directory)
**Purpose:** Persistent context for GitHub Copilot  
**Location:** Project root  
**Updates:** Keep current with latest understanding  
**Critical:** This file is automatically read by Copilot for context

### `Architecture-Analysis.md`
**Purpose:** Document understanding of current codebase structure  
**Contents:**
- Class hierarchy and relationships
- CNC communication patterns
- Coordinate system transformations
- Vision system architecture
- Threading model

### `Firmware-Protocol-Comparison.md`
**Purpose:** Compare TinyG, GRBL, and Smoothie protocols  
**Contents:**
- Command formats for each firmware
- Response parsing requirements
- Error code mappings
- Translation tables
- Abstraction requirements

### `PIC33MZ-Integration-Notes.md`
**Purpose:** Specific details about user's custom PIC33MZ firmware  
**Contents:**
- Hardware specifications
- Custom GRBL commands
- Connection parameters
- Firmware features and limitations
- Testing procedures

### `Refactor-Roadmap.md`
**Purpose:** Step-by-step plan for multi-firmware refactor  
**Contents:**
- Phase breakdown (7+ phases)
- Task checklists for each phase
- Timeline estimates
- Risk assessment
- Decision log

### `Questions-And-Discoveries.md`
**Purpose:** Learning journal tracking questions and answers  
**Contents:**
- Date-stamped discoveries
- Code reference locations
- Open questions
- Code snippets
- Next steps

## How to Use

### During Learning Phase (Now)
1. **Ask questions** about the codebase
2. **Document answers** in Questions-And-Discoveries.md
3. **Update Architecture-Analysis.md** as understanding grows
4. **Fill in protocol details** in Firmware-Protocol-Comparison.md
5. **Test and document** PIC33MZ firmware details
6. **Update `.copilot-instructions.md`** with key insights

### Before Starting Refactor
1. Complete all checklists in Refactor-Roadmap.md prerequisites
2. Ensure architecture is fully understood
3. Have working test hardware for all firmware types
4. Review and approve roadmap
5. Create feature branch

### During Refactor
1. Follow phases in Refactor-Roadmap.md
2. Check off completed tasks
3. Update decision log with choices made
4. Document blockers and workarounds
5. Keep `.copilot-instructions.md` updated with current state

## Document Maintenance

### Keep Updated
- ? Questions-And-Discoveries.md (daily/weekly as you learn)
- ? Architecture-Analysis.md (as understanding evolves)
- ? `.copilot-instructions.md` (important insights, status changes)

### Update When Needed
- ?? Firmware-Protocol-Comparison.md (as protocol details discovered)
- ?? PIC33MZ-Integration-Notes.md (as firmware tested)
- ?? Refactor-Roadmap.md (major decisions, timeline changes)

### Archive When Done
- ?? After refactor completes, keep as historical reference
- ?? Move to `docs/archive/` or similar
- ?? Create new planning docs for next project

## Rules

### During Learning Phase
- ? **DO NOT** create refactor branch yet
- ? **DO NOT** start modifying code for multi-firmware support
- ? **DO NOT** add DI packages or interfaces
- ? **DO** document everything you learn
- ? **DO** test existing functionality
- ? **DO** ask questions about architecture

### Before Refactoring
- ? Complete all "Prerequisites" in Refactor-Roadmap.md
- ? Have user approval to proceed
- ? Ensure test hardware available
- ? Create feature branch
- ? Commit all planning docs to master first

## Quick Reference

### Key Files to Update Often
1. `Questions-And-Discoveries.md` - Your learning journal
2. `.copilot-instructions.md` - Copilot's reference guide

### Key Sections to Fill In
- [ ] Protocol comparison with actual TinyG commands
- [ ] PIC33MZ custom commands and hardware specs
- [ ] Complete architecture understanding
- [ ] Refactor prerequisites checklist

### When to Ask for Help
- Stuck on understanding how something works
- Found conflicting patterns in code
- Discovered potential bugs or issues
- Need hardware specifications
- Unclear on refactor approach

## Version History

- **2026-01-03:** Initial planning structure created
  - All template documents created
  - Learning phase begun
  - User studying codebase and testing hardware

---

**Remember:** The goal is **understanding** first, **refactoring** second. Take time to learn the system thoroughly before making changes.
