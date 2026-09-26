# Windows Security Posture Checker

**Coded by Cyber Security Engineer Mr Sabaz Ali Khan**

A C# Windows Forms application that collects local security evidence and displays clear findings with practical review guidance. The collector queries built-in Windows PowerShell commands and selected registry values. It does not change security settings, automatically remediate findings, send reports to a server, scan other devices, or read BitLocker recovery keys.

## Build and run

Target: 64-bit Windows 10/11 with .NET Framework 4.8 and Windows PowerShell 5.1. Windows edition, device capabilities, enterprise policy, and privileges affect available evidence. Running on an old Windows release does not imply that release is still supported; check your organization's servicing policy separately.

### Quick build

1. Extract the entire ZIP into a writable folder.
2. Double-click `build_windows.bat`. It uses the installed 64-bit .NET Framework C# compiler. No NuGet packages are needed.
3. After a successful build, double-click `run_windows.bat` or `bin\SecurityPostureChecker.exe`.
4. Select **Scan this computer**. The scan has an overall 120-second timeout.
5. Select a row to see full evidence and guidance. Use the status filter or text search.
6. Choose **Export full report** and select HTML, CSV or JSON.

The compiler fallback requires the .NET Framework installation's `Framework64\v4.0.30319\csc.exe`. If it is missing, use the Visual Studio route below. Compilation itself does not require administrator privileges.

### Visual Studio

Install the **.NET desktop development** workload and **.NET Framework 4.8 targeting/development tools**. Open `SecurityPostureChecker.csproj`, then Build and Run. The project deliberately uses C# 5-compatible syntax and .NET Framework APIs for a simple Windows build. It is not a modern .NET 8/10 SDK project; `dotnet run` is not the intended workflow. The normal MSBuild output is under `bin\Debug` or `bin\Release`.

### Privileges

The app runs as the current user and does not silently elevate. Some checks, such as optional features, Secure Boot and BitLocker, can require an elevated process. To obtain more evidence, close the app and deliberately **right-click the built EXE > Run as administrator** if authorized on that computer. Permission failures remain **UNKNOWN**. An administrator scan may still return UNKNOWN for unavailable modules/hardware/edition features.

The collector is embedded into the EXE at build time and launched using the full path to Windows PowerShell, with `-NoProfile -NonInteractive -EncodedCommand`. Encoding transports the embedded script reliably; it is not encryption. No execution-policy bypass, policy change, or downloaded script is used. Application control or constrained language policy may prevent execution; respect that policy rather than disabling it. Editing `Collect.ps1` requires rebuilding.

## Checks (15 findings)

| Finding | Evidence / interpretation |
|---|---|
| Domain, Private, Public firewall profiles (3) | `Get-NetFirewallProfile -PolicyStore ActiveStore`; effective enabled/disabled setting per profile. Does not determine which network currently uses each profile or audit every rule. |
| Antivirus inventory | SecurityCenter2 product names, informational only. Registration does not prove a provider is protecting the system. |
| Defender real-time protection | Evaluated only when Defender reports normal active mode. Passive or other modes require review of the actual provider. |
| Defender signature age | Active Defender signature timestamp; seven-day warning threshold is an app heuristic. Future dates require review. |
| UAC | `EnableLUA` registry setting; does not audit consent levels or whether a reboot is pending for a change. |
| Secure Boot | `Confirm-SecureBootUEFI`; unsupported firmware/permission errors are UNKNOWN. |
| TPM readiness | Presence and readiness; does not assess TPM version, attestation or every firmware property. |
| OS volume encryption | BitLocker volume state and protection status for the OS drive only. Does not establish backup/recovery-key custody. |
| SMBv1 optional feature | Installed optional-feature state; pending changes require review. |
| Remote Desktop | Configured RDP deny/allow value; enabled means REVIEW, not automatically insecure or internet-exposed. |
| RDP NLA | Registry configuration for Network Level Authentication; not a complete effective domain-policy evaluation. |
| Hotfix inventory | Latest dated `Get-HotFix` record; does not list all update types or identify missing updates. |
| Reboot indicators | Two common servicing/Windows Update registry indicators. No indicator is not proof that no reboot is required. |

## Status meanings

- **PASS**: the specific tested criterion is satisfied. It does not mean the entire computer is secure.
- **WARNING**: a checked setting or condition warrants attention.
- **REVIEW**: context, business need or a different provider must be assessed.
- **UNKNOWN**: evidence was unavailable, unsupported, incomplete or denied. Never counted as PASS.
- **INFO**: factual evidence without a security verdict.

There is intentionally no overall security percentage or compliance score. This is a point-in-time configuration checker, not antivirus, a vulnerability scanner, a missing-patch scanner, or a compliance certification tool. Evidence can change immediately after collection.

## Reports

Exports always include **all findings**, even with a dashboard filter active. Report files include the machine name, UTC collection time, whether the collector was elevated, findings and guidance. Nothing is uploaded by the application. No automatic report history is saved: export snapshots to retain them.

- **HTML**: self-contained printable report with no external resources. Open in a browser and use Print > Save as PDF if a PDF is needed.
- **CSV**: spreadsheet-friendly UTF-8 with quotes and formula protection.
- **JSON**: structured raw findings, schema version 1.

Export uses a temporary file and replacement to protect an existing report from partial writes. If the destination filesystem cannot replace a file, choose a new filename. Reports contain machine/configuration details; share them only with intended recipients.

## Troubleshooting

- **Build compiler missing**: build in Visual Studio with the specified workload and targeting pack.
- **Reference assemblies not found**: install the .NET Framework 4.8 targeting pack in Visual Studio Installer.
- **Many UNKNOWN results**: read each evidence field. Try an authorized elevated scan; some commands are unavailable on some Windows editions.
- **Defender REVIEW despite installed antivirus**: another provider may be active. Open Windows Security > Manage providers.
- **PowerShell blocked**: follow your administrator's approved application process; do not disable controls.
- **Scan timeout**: check local CIM/Windows component health. Partial results are discarded so they cannot be mistaken for a complete scan.
- **Cancel or close during scanning**: the app requests cancellation, terminates its collector process and produces no completed report for that run.
- **Export locked**: close the file in Excel/browser applications or export using a new name.

## Developer layout

`src/MainForm.cs` — dashboard, filters, scan lifecycle, export selection.

`src/Collector.cs` — embedded PowerShell process, asynchronous output, cancellation and timeout.

`src/Collect.ps1` — read-only evidence collection with per-check error isolation.

`src/Models.cs` — report schema and validation.

`src/Export.cs` — HTML/CSV encoding and atomic report writes.

`tests/Tests.cs` and `tests/run_tests.bat` — Windows report tests and PowerShell syntax validation.

## Validation scope

The creation environment has no Windows runtime, C# compiler or PowerShell. **Compilation, live scanning, UI rendering and the supplied Windows tests have not been executed here.** Static package checks verified project XML, referenced files, embedded resource consistency, check inventory and the encoded PowerShell command length. See `VALIDATION.md` for exact results and the local test checklist. The ZIP contains source code, not a precompiled EXE.

## Primary implementation references

- [Microsoft: Get-NetFirewallProfile](https://learn.microsoft.com/en-us/powershell/module/netsecurity/get-netfirewallprofile)
- [Microsoft: Get-MpComputerStatus](https://learn.microsoft.com/en-us/powershell/module/defender/get-mpcomputerstatus)
- [Microsoft: Confirm-SecureBootUEFI](https://learn.microsoft.com/en-us/powershell/module/secureboot/confirm-securebootuefi)
- [Microsoft: BitLocker operations guide](https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide)
