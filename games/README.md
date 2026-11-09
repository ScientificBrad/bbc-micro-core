# Floppy Disk Images (.SSD / .DSD)

Place standard Acorn DFS floppy disk images in this directory to load software into the emulator.

### Supported Disk Formats:
- **`.ssd` (Single-Sided Disk):** 80 tracks, 10 sectors/track, 256 bytes/sector (204,800 bytes).
- **`.dsd` (Double-Sided Disk):** Interleaved tracks for sides 0 and 2.

### Booting Disks:
In the emulator GUI or command line:
```powershell
BbcMicro.Emulator.exe --window --disk games/Disc002-ChuckieEgg.ssd --autoboot
```
Or from the BBC BASIC prompt:
```text
*EXEC !BOOT
```
Or to run a specific executable:
```text
*RUN <FILENAME>
```
