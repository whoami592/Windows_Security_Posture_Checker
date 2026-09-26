# Read-only local evidence collection. Embedded in the compiled executable.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$items = New-Object System.Collections.Generic.List[object]
function Add-Result($id,$title,$status,$evidence,$advice) {
    $items.Add([pscustomobject]@{Id=$id;Title=$title;Status=$status;Evidence=[string]$evidence;Advice=$advice})
}
function Check($id,$title,$advice,[scriptblock]$body) {
    try { & $body } catch { Add-Result $id $title 'UNKNOWN' $_.Exception.Message $advice }
}
foreach ($profile in @('Domain','Private','Public')) {
    $id = 'FW-' + $profile
    $title = $profile + ' firewall profile'
    $advice = 'Review Windows Security > Firewall & network protection. Check organizational policy before changing settings.'
    Check $id $title $advice {
        $p = Get-NetFirewallProfile -Name $profile -PolicyStore ActiveStore
        $state = 'UNKNOWN'
        if ([string]$p.Enabled -eq 'True') { $state = 'PASS' }
        elseif ([string]$p.Enabled -eq 'False') { $state = 'WARNING' }
        Add-Result $id $title $state ('Enabled=' + $p.Enabled + '; default inbound=' + $p.DefaultInboundAction + '. Effective policy; profile may not currently be in use.') $advice
    }
}
Check 'AV-Inventory' 'Registered antivirus products' 'Open Windows Security > Virus & threat protection > Manage providers to verify the active provider.' {
    $av = @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntivirusProduct)
    if ($av.Count -eq 0) { Add-Result 'AV-Inventory' 'Registered antivirus products' 'REVIEW' 'No products returned. This does not prove antivirus is absent.' 'Verify the provider in Windows Security.' }
    else { Add-Result 'AV-Inventory' 'Registered antivirus products' 'INFO' (($av | ForEach-Object {$_.displayName}) -join '; ') 'Registration alone does not establish current protection. Verify providers in Windows Security.' }
}
$mp = $null
$mpError = ''
try { $mp = Get-MpComputerStatus } catch { $mpError = $_.Exception.Message }
foreach ($which in @('Realtime','Signatures')) {
    $id = 'Defender-' + $which
    $title = 'Microsoft Defender ' + $which
    $advice = 'Review the active antivirus provider in Windows Security. Defender may be passive when another provider is active.'
    if ($null -eq $mp) { Add-Result $id $title 'UNKNOWN' $mpError $advice; continue }
    if ([string]$mp.AMRunningMode -ne 'Normal') {
        Add-Result $id $title 'REVIEW' ('Defender mode=' + $mp.AMRunningMode + '; not evaluated as active antivirus.') $advice; continue
    }
    if ($which -eq 'Realtime') {
        if ($null -eq $mp.RealTimeProtectionEnabled) { Add-Result $id $title 'UNKNOWN' 'RealTimeProtectionEnabled unavailable.' $advice }
        elseif ($mp.RealTimeProtectionEnabled -eq $true) { Add-Result $id $title 'PASS' 'Defender reports real-time protection enabled; active mode.' $advice }
        else { Add-Result $id $title 'WARNING' 'Defender reports real-time protection disabled; active mode.' $advice }
    } else {
        if ($null -eq $mp.AntivirusSignatureLastUpdated) { Add-Result $id $title 'UNKNOWN' 'Signature timestamp unavailable.' $advice; continue }
        $age = ((Get-Date) - $mp.AntivirusSignatureLastUpdated).TotalDays
        $s = 'PASS'
        if ($age -lt 0) { $s = 'REVIEW' } elseif ($age -gt 7) { $s = 'WARNING' }
        Add-Result $id $title $s ('Updated=' + $mp.AntivirusSignatureLastUpdated.ToString('o') + '; age days=' + [math]::Round($age,1) + '; app threshold=7 days.') 'Update security intelligence in Windows Security. Seven days is an application heuristic, not a compliance standard.'
    }
}
Check 'UAC' 'User Account Control' 'Review User Account Control settings and organization policy.' {
    $v = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name EnableLUA).EnableLUA
    $s = 'UNKNOWN'; if ($v -eq 1) {$s='PASS'} elseif ($v -eq 0) {$s='WARNING'}
    Add-Result 'UAC' 'User Account Control' $s ('EnableLUA=' + $v + '; registry configuration only, not consent prompt level or pending reboot state.') 'Review UAC settings. This check does not assess every UAC policy.'
}
Check 'SecureBoot' 'UEFI Secure Boot' 'Review System Information > Secure Boot State. Unsupported firmware and insufficient permissions are UNKNOWN, not disabled.' {
    $v = Confirm-SecureBootUEFI
    $s = 'WARNING'; if ($v -eq $true) {$s='PASS'}
    Add-Result 'SecureBoot' 'UEFI Secure Boot' $s ('SecureBootEnabled=' + $v) 'Review firmware support and vendor guidance before changing Secure Boot settings.'
}
Check 'TPM' 'TPM readiness' 'Review Windows Security > Device security > Security processor details. Do not clear the TPM as an automatic fix.' {
    $t = Get-Tpm
    $s = 'REVIEW'; if ($t.TpmPresent -eq $true -and $t.TpmReady -eq $true) {$s='PASS'}
    Add-Result 'TPM' 'TPM readiness' $s ('Present=' + $t.TpmPresent + '; Ready=' + $t.TpmReady + '. Version is not checked.') 'Review hardware capabilities and security processor details.'
}
Check 'BitLocker' 'OS volume encryption' 'Review Manage BitLocker or Device encryption. Availability depends on Windows edition, hardware and privileges.' {
    $v = Get-BitLockerVolume -MountPoint $env:SystemDrive
    $s = 'REVIEW'
    if ([string]$v.ProtectionStatus -eq 'On' -and [string]$v.VolumeStatus -eq 'FullyEncrypted') {$s='PASS'}
    elseif ([string]$v.ProtectionStatus -eq 'Off') {$s='WARNING'}
    Add-Result 'BitLocker' 'OS volume encryption' $s ('Drive=' + $env:SystemDrive + '; Protection=' + $v.ProtectionStatus + '; Volume=' + $v.VolumeStatus + '; encrypted=' + $v.EncryptionPercentage + '%') 'Review encryption and protection status. This tool never reads or exports recovery keys.'
}
Check 'SMB1' 'SMBv1 optional feature' 'Review legacy application dependencies before changing optional Windows features.' {
    $f = Get-WindowsOptionalFeature -Online -FeatureName SMB1Protocol
    $s = 'REVIEW'
    if ([string]$f.State -eq 'Disabled' -or [string]$f.State -eq 'DisabledWithPayloadRemoved') {$s='PASS'}
    elseif ([string]$f.State -eq 'Enabled') {$s='WARNING'}
    Add-Result 'SMB1' 'SMBv1 optional feature' $s ('Feature state=' + $f.State) 'Review and retire unnecessary SMBv1 dependencies. Pending feature changes require separate review.'
}
Check 'RDP' 'Remote Desktop configuration' 'Review Settings > System > Remote Desktop. Enable only when required with appropriate access controls.' {
    $v = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name fDenyTSConnections).fDenyTSConnections
    $s='UNKNOWN'; if($v -eq 1){$s='PASS'} elseif($v -eq 0){$s='REVIEW'}
    Add-Result 'RDP' 'Remote Desktop configuration' $s ('fDenyTSConnections=' + $v + '; 1=disabled, 0=enabled. Does not establish network reachability.') 'Review business need for Remote Desktop and effective policy.'
}
Check 'RDP-NLA' 'Remote Desktop NLA configuration' 'Review Remote Desktop authentication policy; this registry snapshot is not an effective domain policy audit.' {
    $v=(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' -Name UserAuthentication).UserAuthentication
    $s='REVIEW'; if($v -eq 1){$s='PASS'}
    Add-Result 'RDP-NLA' 'Remote Desktop NLA configuration' $s ('UserAuthentication=' + $v + '; 1 requires NLA. Configuration checked even if RDP is disabled.') 'Require NLA where RDP is used, subject to organizational compatibility requirements.'
}
Check 'Updates' 'Recent installed hotfix evidence' 'Use Windows Update or your managed update service to verify missing updates and support status.' {
    $h = @(Get-HotFix | Where-Object {$null -ne $_.InstalledOn} | Sort-Object InstalledOn -Descending | Select-Object -First 1)
    if($h.Count -eq 0) {throw 'No dated hotfix record returned.'}
    Add-Result 'Updates' 'Recent installed hotfix evidence' 'INFO' ($h[0].HotFixID + '; installed=' + $h[0].InstalledOn.ToString('o') + '. Hotfix inventory is incomplete by design.') 'This is not a missing-update scan and cannot establish that Windows is up to date.'
}
Check 'Reboot' 'Common pending reboot indicators' 'Review Windows Update and pending maintenance before restarting.' {
    $a=Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'
    $b=Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'
    $s='INFO'; if($a -or $b){$s='REVIEW'}
    Add-Result 'Reboot' 'Common pending reboot indicators' $s ('CBS indicator=' + $a + '; Windows Update indicator=' + $b + '. Other reboot conditions are not checked.') 'Review pending maintenance. No automatic restart is performed.'
}
$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
[pscustomobject]@{SchemaVersion=1;Machine=$env:COMPUTERNAME;CollectedUtc=[DateTime]::UtcNow.ToString('o');Elevated=$admin;Items=@($items.ToArray())} | ConvertTo-Json -Depth 5 -Compress
