# FPTester — FS64 Fingerprint Test Utility

Standalone `.exe` for testing Futronic FS64 fingerprint scanning,
enrollment, and matching. **No database required** — templates are
stored as binary files on your local disk under `%AppData%\FPTester\`.

---

## Architecture (important — read before debugging)

This project calls Futronic's own pre-built, vendor-tested managed
wrapper, **`ftrSDKHelper13.dll`**, rather than hand-rolling P/Invoke
declarations against the native DLLs. That wrapper is a C++/CLI
assembly that ships inside the SDK install and internally drives two
native DLLs:

- `ftrScanAPI.dll` — raw device control
- `FTRAPI.dll` — the actual biometric engine (template creation,
  verification, identification)

`ftrSDKHelper13.dll` exposes clean, **event-driven** .NET classes:
`FutronicEnrollment`, `FutronicVerification`, `FutronicIdentification`
(all extending `FutronicSdkBase`). You set properties, subscribe to
events (`OnPutOn`, `OnTakeOff`, `OnEnrollmentComplete`, etc.), then
call the operation's start method — the SDK drives the scanner and
fires the completion event when the user has finished interacting
with it. This mirrors Futronic's own official sample, `WorkedEx.NET`,
found at `...\SDK 4.2\Examples\Net\Vs2013\`.

---

## Requirements

| Item | Details |
|---|---|
| OS | Windows 10/11 |
| .NET SDK | .NET 10 (to build) — [download](https://dotnet.microsoft.com/download) |
| SDK Helper DLL | `ftrSDKHelper13.dll` must exist at `...\SDK 4.2\Bin\ftrSDKHelper13.dll` |
| Native DLLs | `ftrScanAPI.dll` and `FTRAPI.dll` must also be present in the same `Bin\` folder (the helper DLL loads them at runtime) |
| Hardware | Futronic FS64 plugged in with drivers installed |

If your SDK is installed somewhere other than
`C:\Program Files (x86)\Futronic\SDK 4.2\`, update the `<HintPath>`
in `FPTester.csproj` to match.

---

## Build & Run

```bat
build.bat
```

This produces `publish\FPTester.exe` — a single self-contained executable.

Double-click it, or run:

```bat
publish\FPTester.exe
```

---

## What Each Tab Does

### Scan Test
Runs one real enrollment cycle through the SDK purely to test capture
and report the quality score (1–10, shown scaled to a 0–100 meter).
Nothing is saved. Use this to verify the scanner and SDK are working
before trying Enroll or Match.

### Enroll
Captures a fingerprint and saves the resulting template to disk. Give
it a slot name (e.g. `Alice — Right Index`). The SDK itself decides
how many impressions it needs internally — when `OnEnrollmentComplete`
fires with success, the template is ready.

### Match
Select any enrolled slot from the list, then scan. The tool verifies
whether the live finger matches the stored template and shows:
- ✓ **MATCH** (green) — finger matches
- ✗ **NO MATCH** (red) — finger does not match

---

## Template Storage

Templates are saved here — no install, no database:

```
%AppData%\FPTester\templates\
    index.json          ← slot name → filename map
    alice_right_20250601120000.fpt
    ...
```

---

## Troubleshooting history (for future reference)

| Symptom | Root cause | Fix |
|---|---|---|
| `BadImageFormatException` | Project built x64 against a 32-bit DLL | `<PlatformTarget>x86</PlatformTarget>` in `.csproj` |
| `DllNotFoundException: ftrScanAPI.dll` | Hardcoded path missing `\Bin\` subfolder | Corrected path to `...\SDK 4.2\Bin\ftrScanAPI.dll` |
| `EntryPointNotFoundException: ftrScanSaveRegTemplate` | That function doesn't exist in `ftrScanAPI.dll` — it's a raw device-control DLL, not the matcher | Switched to the real matcher engine via `ftrSDKHelper13.dll` (`FutronicEnrollment`/`FutronicVerification`) |
| Hand-rolled `FTRAPI.dll` P/Invoke risked further marshaling bugs | Struct packing, `DGTBOOL` size, calling convention all had to be guessed | Used Futronic's own compiled, version-matched wrapper instead of guessing |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| "SDK DLL Missing" dialog | Confirm `ftrSDKHelper13.dll`, `ftrScanAPI.dll`, and `FTRAPI.dll` all exist in `...\SDK 4.2\Bin\` |
| Scanner stays "Not connected" | Replug USB; check Device Manager for FS64 driver |
| Enrollment never completes | Make sure no other app (e.g. the Enrollment Kit) is holding the scanner open |
| Low quality even with firm press | Clean the sensor glass with a dry cloth |
| Templates not saving | Check `%AppData%\FPTester\` folder permissions |
