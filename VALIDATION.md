# Validation

Static checks performed on 2026-09-26 UTC:

- PASS: Project source/resource references exist.
- PASS: Project, manifest and runtime configuration XML parse successfully.
- PASS: Embedded resource identifier matches project, batch build and C# loader.
- PASS: Encoded collector command length: 24324 characters; below the 30,000 application guard.
- PASS: No execution-policy override is configured; failed individual checks emit UNKNOWN.
- PASS: Finding inventory: 3 firewall + 2 Defender + 10 individually declared checks = 15 findings.

Not executed: C# compilation, PowerShell syntax/runtime, Windows UI rendering, collection under standard/admin users, export execution, timeout/cancel runtime, and supplied Windows tests. No Windows/.NET/PowerShell runtime is available in this creation environment.

## Local acceptance steps

1. Build using `build_windows.bat` or Visual Studio.
2. Run `tests/run_tests.bat`; it validates report parsing/export escaping and parses the collector without running it.
3. Run a standard-user scan. Verify permission failures are UNKNOWN.
4. Where authorized, run an elevated scan and compare firewall/Defender values with Windows Security.
5. Confirm third-party antivirus/passive Defender is not treated as active Defender protection.
6. Check filters and selection details; export all three formats, including while filtering.
7. Cancel a scan and close during scanning. Confirm no completed report is offered for an incomplete run.
8. Keep source and build output together until locally validated.
