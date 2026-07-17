$ErrorActionPreference = 'Stop'

$distroName = "Ubuntu-LXC"
$wslConfigPath = "$env:USERPROFILE\.wslconfig"
$scriptVersion = "2026-06-15-incus-v1"

$logFile = "$env:TEMP\WinContainers-LxcSetup.log"
"=== LXC Setup started at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File $logFile

function Write-Step($message) {
    $line = "[LXC-SETUP] $message"
    Write-Output $line
    $line | Out-File $logFile -Append
}

function Get-DistroLines($output) {
    $lines = if ($output -is [array]) { $output } else { $output -split "`n" }
    return $lines | ForEach-Object { $_.Replace("`0", "").Trim() } | Where-Object { $_ -ne "" }
}

function Invoke-External($filePath, $arguments, $timeoutSeconds = 0, [switch]$CaptureOutput) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $filePath
    $psi.Arguments = $arguments
    $psi.RedirectStandardOutput = $CaptureOutput.IsPresent
    $psi.RedirectStandardError = $CaptureOutput.IsPresent
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = [System.Diagnostics.Process]::new()
    $proc.StartInfo = $psi

    $null = $proc.Start()

    if ($timeoutSeconds -gt 0) {
        $finished = $proc.WaitForExit($timeoutSeconds * 1000)
        if (-not $finished) {
            try { $proc.Kill($true) } catch {}
            return [PSCustomObject]@{
                ExitCode = -1
                Output = ""
                TimedOut = $true
            }
        }
    } else {
        $proc.WaitForExit()
    }

    $output = ""
    if ($CaptureOutput.IsPresent) {
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $output = (($stdout + "`n" + $stderr) -replace "`0", "").Trim()
    }

    return [PSCustomObject]@{
        ExitCode = $proc.ExitCode
        Output = $output
        TimedOut = $false
    }
}

Write-Step "Starting LXC setup for WSL2 distro '$distroName'..."
Write-Step "Script version: $scriptVersion"

# 1. Check WSL is installed
Write-Step "Checking WSL installation..."
$wslVersion = & wsl --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Output "ERROR: WSL is not installed. Install WSL 2 first: wsl --install"
    exit 1
}
Write-Step "WSL is installed."

# 2. Check if distro already exists
$distrosRaw = & wsl --list --quiet 2>&1
$distros = Get-DistroLines $distrosRaw
$distroExists = ($distros | Where-Object { $_ -eq $distroName }).Count -gt 0

$createdFresh = $false

if (-not $distroExists) {
    Write-Step "Creating WSL2 distro '$distroName'..."

    Write-Step "Trying direct install: wsl --install -d Ubuntu --name $distroName --no-launch..."
    $namedInstallOut = & wsl --install -d Ubuntu --name $distroName --no-launch 2>&1
    $namedInstallText = (($namedInstallOut -join "`n") -replace "`0", "").Trim()

    if ($LASTEXITCODE -eq 0) {
        Start-Sleep 5
        $distrosAfterNamedInstallRaw = & wsl --list --quiet 2>&1
        $distrosAfterNamedInstall = Get-DistroLines $distrosAfterNamedInstallRaw
        $namedInstallCreated = ($distrosAfterNamedInstall | Where-Object { $_ -eq $distroName }).Count -gt 0
        if ($namedInstallCreated) {
            $createdFresh = $true
            Write-Step "Created '$distroName' via direct install."
        }
    } elseif ($namedInstallText -match "already exists|ALREADY_EXISTS") {
        Write-Step "Install reported '$distroName' already exists. Continuing."
    } else {
        Write-Step "Direct install not used (exit $LASTEXITCODE). Output: $namedInstallText"
    }

    if ($createdFresh) {
        Write-Step "Skipping export/import clone path."
    } else {
    $baseTar = "$env:TEMP\ubuntu-base.tar"
    $rootfsPath = "$env:TEMP\ubuntu-lxc-rootfs.tar.gz"
    $imported = $false

    function Find-UbuntuDistro($distroLines) {
        foreach ($d in $distroLines) {
            if ($d -eq "Ubuntu" -or $d -match "^Ubuntu-\d") { return $d }
        }
        return $null
    }

    # Try path A: export existing Ubuntu distro
    $distroLines = Get-DistroLines $distros
    $ubuntuDistro = Find-UbuntuDistro $distroLines
    if ($ubuntuDistro) {
        Write-Step "Found '$ubuntuDistro', terminating to allow clean export..."
        & wsl --terminate $ubuntuDistro 2>&1 | Out-Null
        Start-Sleep 2

        Write-Step "Running global 'wsl --shutdown' before export..."
        & wsl --shutdown 2>&1 | Out-Null
        Start-Sleep 2

        Write-Step "Exporting distro (timeout: 180s)..."
        $exportResult = Invoke-External "wsl.exe" "--export $ubuntuDistro \"$baseTar\"" 180

        if ($exportResult.TimedOut) {
            Write-Step "Export timed out. Retrying after another 'wsl --shutdown'..."
            try { Remove-Item $baseTar -Force -ErrorAction SilentlyContinue } catch {}
            & wsl --shutdown 2>&1 | Out-Null
            Start-Sleep 3
            $exportResult = Invoke-External "wsl.exe" "--export $ubuntuDistro \"$baseTar\"" 180
        }

        $exportOk = ($exportResult.ExitCode -eq 0 -and (Test-Path $baseTar) -and ((Get-Item $baseTar).Length -gt 1MB))
        if ($exportOk) {
            $imported = $true
            Write-Step "Export successful ($((Get-Item $baseTar).Length / 1MB -as [int]) MB)."
        } else {
            if ($exportResult.TimedOut) {
                Write-Step "Export failed: command timed out after retries."
            }

            Write-Step "Export failed (exit $($exportResult.ExitCode)). Output: $($exportResult.Output)"
            Remove-Item $baseTar -Force -ErrorAction SilentlyContinue
        }
    }

    # Try path B: install Ubuntu via wsl --install
    if (-not $imported) {
        Write-Step "Attempting: wsl --install -d Ubuntu --no-launch..."
        $installOut = & wsl --install -d Ubuntu --no-launch 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Step "Ubuntu installed. Waiting for setup..."
            Start-Sleep 15
            $newDistros = Get-DistroLines (& wsl --list --quiet 2>&1)
            $newUbuntu = Find-UbuntuDistro $newDistros
            if ($newUbuntu) {
                & wsl --export $newUbuntu $baseTar 2>&1
                if ($LASTEXITCODE -eq 0) { $imported = $true }
            }
        } else {
            $cleanOut = $installOut -replace "`0", ""
            Write-Step "wsl --install output: $cleanOut"
            if ($cleanOut -match "ALREADY_EXISTS|already exists") {
                Write-Step "Ubuntu exists. Exporting..."
                & wsl --export Ubuntu $baseTar 2>&1
                if ($LASTEXITCODE -eq 0) { $imported = $true }
            }
        }
    }

    # If we have a tar, import as Ubuntu-LXC
    if ($imported -and (Test-Path $baseTar)) {
        Write-Step "Importing as '$distroName'..."
        & wsl --import $distroName "$env:LOCALAPPDATA\WslDistros\$distroName" $baseTar --version 2 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Output "ERROR: Failed to import distro. Try running as admin."
            Remove-Item $baseTar -Force -ErrorAction SilentlyContinue
            exit 1
        }
        $createdFresh = $true
        Write-Step "WSL2 distro '$distroName' created."
        Remove-Item $baseTar -Force -ErrorAction SilentlyContinue
    }

    if (-not $createdFresh) {
        Write-Output "ERROR: Could not obtain Ubuntu. Install manually: wsl --install -d Ubuntu"
        Write-Output "Then run: wsl --export Ubuntu $baseTar"
        Write-Output "Then run this setup again."
        exit 1
    }
    }
} else {
    Write-Step "WSL2 distro '$distroName' already exists."
}

# 3. Enable systemd
Write-Step "Enabling systemd in '$distroName'..."
$systemdCommand = "mkdir -p /etc && { echo '[boot]'; echo 'systemd=true'; } > /etc/wsl.conf"
$systemdArgs = "-d $distroName -u root -- sh -c '$systemdCommand'"
$systemdResult = Invoke-External "wsl.exe" $systemdArgs 90 -CaptureOutput
if ($systemdResult.TimedOut) {
    Write-Step "systemd step timed out. Retrying after WSL restart..."
    & wsl --terminate $distroName 2>&1 | Out-Null
    & wsl --shutdown --force 2>&1 | Out-Null
    Start-Sleep 3
    $systemdResult = Invoke-External "wsl.exe" $systemdArgs 90 -CaptureOutput
}

if ($systemdResult.ExitCode -ne 0 -or $systemdResult.TimedOut) {
    if ($systemdResult.TimedOut) {
        Write-Output "ERROR: Failed to configure systemd (command timed out)."
        Write-Output "Open once and finish first-launch setup: wsl -d $distroName"
        Write-Output "Then run Setup LXC again."
    } else {
        Write-Output "ERROR: Failed to configure systemd. Output: $($systemdResult.Output)"
    }
    exit 1
}
Write-Step "systemd configured in wsl.conf."

# 3b. Restart distro so systemd/snapd can start cleanly
Write-Step "Restarting '$distroName' to apply systemd settings..."
& wsl --terminate $distroName 2>&1 | Out-Null
Start-Sleep 3
$initProbe = Invoke-External "wsl.exe" "-d $distroName -u root -- sh -c 'ps -p 1 -o comm='" 30 -CaptureOutput
if ($initProbe.ExitCode -eq 0) {
    Write-Step "Init process after restart: $($initProbe.Output)"
}

# 4. Configure AppArmor in .wslconfig (best effort)
Write-Step "Configuring AppArmor in .wslconfig (best effort)..."
$appArmorConfig = @"
[wsl2]
kernelCommandLine = lsm=apparmor
"@
if (Test-Path $wslConfigPath) {
    $currentConfig = Get-Content $wslConfigPath -Raw
    if ($currentConfig -match "lsm=apparmor") {
        Write-Step ".wslconfig already has AppArmor."
    } else {
        Write-Step "Appending AppArmor config..."
        Add-Content -Path $wslConfigPath -Value "`n$appArmorConfig"
    }
} else {
    Write-Step "Creating .wslconfig..."
    Set-Content -Path $wslConfigPath -Value $appArmorConfig
}

# AppArmor availability inside distro
$aaProbe = Invoke-External "wsl.exe" "-d $distroName -u root -- sh -c 'if [ -d /sys/kernel/security/apparmor ]; then echo enabled; else echo disabled; fi'" 30 -CaptureOutput
$appArmorEnabled = $aaProbe.ExitCode -eq 0 -and $aaProbe.Output -match "enabled"
if ($appArmorEnabled) {
    Write-Step "AppArmor appears enabled in distro."
} else {
    Write-Step "AppArmor not enabled in distro yet (continuing with LXC CLI install)."
}

# 5. Prepare apt (sources + update) for Incus install
$sourcesArgs = '-d ' + $distroName + ' -u root -- bash -lc ''if [ ! -s /etc/apt/sources.list ] && ! ls /etc/apt/sources.list.d/*.sources /etc/apt/sources.list.d/*.list >/dev/null 2>&1; then codename=$(. /etc/os-release && echo ${UBUNTU_CODENAME:-$VERSION_CODENAME}); echo "deb http://archive.ubuntu.com/ubuntu ${codename} main restricted universe multiverse" > /etc/apt/sources.list; echo "deb http://archive.ubuntu.com/ubuntu ${codename}-updates main restricted universe multiverse" >> /etc/apt/sources.list; echo "deb http://archive.ubuntu.com/ubuntu ${codename}-backports main restricted universe multiverse" >> /etc/apt/sources.list; echo "deb http://security.ubuntu.com/ubuntu ${codename}-security main restricted universe multiverse" >> /etc/apt/sources.list; fi'''
$sourcesResult = Invoke-External "wsl.exe" $sourcesArgs 60 -CaptureOutput
if ($sourcesResult.ExitCode -ne 0 -or $sourcesResult.TimedOut) {
    Write-Output "ERROR: Failed to prepare apt sources. Output: $($sourcesResult.Output)"
    exit 1
}

Write-Step "Running apt-get update..."
$updateResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'export DEBIAN_FRONTEND=noninteractive; apt-get -qq -o DPkg::Lock::Timeout=120 update'" 300
if ($updateResult.ExitCode -ne 0 -or $updateResult.TimedOut) {
    if ($updateResult.TimedOut) {
        Write-Output "ERROR: apt-get update timed out."
    } else {
        $diag = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'tail -n 80 /var/log/apt/term.log 2>/dev/null || echo apt-update-failed'" 30 -CaptureOutput
        Write-Output "ERROR: apt-get update failed. Output: $($diag.Output)"
    }
    exit 1
}

# 6. Install Incus via apt (community fork of LXD, natively packaged)
Write-Step "Installing Incus (LXD-compatible container manager)..."
$installResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'export DEBIAN_FRONTEND=noninteractive; apt-get -qq -o DPkg::Lock::Timeout=120 install -y incus incus-client'" 300
if ($installResult.ExitCode -ne 0 -or $installResult.TimedOut) {
    if ($installResult.TimedOut) {
        Write-Output "ERROR: apt-get install incus timed out."
    } else {
        $diag = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'tail -n 120 /var/log/apt/term.log 2>/dev/null || echo apt-install-failed'" 30 -CaptureOutput
        Write-Output "ERROR: Failed to install Incus. Output: $($diag.Output)"
    }
    exit 1
}
Write-Step "Incus installed."

Write-Step "Creating lxc -> incus compatibility symlink..."
& wsl -d $distroName -u root -- bash -lc "ln -sf /usr/bin/incus /usr/local/bin/lxc && ln -sf /usr/bin/incusd /usr/local/bin/lxd && groupadd --force incus" 2>&1 | Out-Null

Write-Step "Starting Incus daemon (socket-activated)..."
& wsl -d $distroName -u root -- bash -lc "systemctl enable incus.socket --now" 2>&1 | Out-Null
Start-Sleep 4

Write-Step "Waiting for Incus socket (up to 60s)..."
$waitResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'for i in \$(seq 1 30); do sleep 2; if [ -S /var/lib/incus/unix.socket ] && lxc list >/dev/null 2>&1; then echo ready; exit 0; fi; done; echo timeout'" 120 -CaptureOutput
if ($waitResult.Output -match "timeout") {
    Write-Step "Socket wait timed out. Checking daemon status..."
    $diagResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'systemctl status incus.socket 2>&1; echo ---; journalctl -u incus --no-pager -n 30 2>&1'" 30 -CaptureOutput
    Write-Output "ERROR: Incus daemon not ready. Output: $($diagResult.Output)"
    exit 1
}

Write-Step "Initializing Incus..."
$initResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus admin init --auto'" 120 -CaptureOutput
if ($initResult.ExitCode -ne 0) {
    Write-Output "ERROR: Failed to initialize Incus. Output: $($initResult.Output)"
    exit 1
}
Write-Step "Incus initialized."

Write-Step "Creating storage pool..."
$storageCreate = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus storage list --format csv 2>/dev/null | head -1'" 15 -CaptureOutput
if ([string]::IsNullOrWhiteSpace($storageCreate.Output)) {
    $poolResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus storage create default dir'" 30 -CaptureOutput
    if ($poolResult.ExitCode -ne 0) {
        Write-Output "ERROR: Failed to create storage pool. Output: $($poolResult.Output)"
        exit 1
    }
    Write-Step "Storage pool 'default' (dir) created."
} else {
    Write-Step "Storage pool 'default' already exists."
}

Write-Step "Adding root disk to default profile..."
$profileDevices = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus profile show default --format json 2>/dev/null | python3 -c \"import sys,json; d=json.load(sys.stdin); print(\\\"has_root\\\" if d.get(\\\"devices\\\",{}).get(\\\"root\\\") else \\\"no_root\\\")\"'" 15 -CaptureOutput
if ($profileDevices.Output -match "no_root") {
    $rootResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus profile device add default root disk path=/ pool=default'" 15 -CaptureOutput
    if ($rootResult.ExitCode -ne 0) {
        Write-Output "ERROR: Failed to add root disk. Output: $($rootResult.Output)"
        exit 1
    }
    Write-Step "Root disk device added to default profile."
} else {
    Write-Step "Root disk device already present in default profile."
}

Write-Step "Creating Incus bridge network..."
$netCheck = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus network list --format csv 2>/dev/null | grep -c incusbr0'" 15 -CaptureOutput
if ($netCheck.Output -match '^0$' -or [string]::IsNullOrWhiteSpace($netCheck.Output)) {
    $netResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus network create incusbr0 ipv4.address=10.10.10.1/24 ipv4.nat=true ipv6.address=none'" 30 -CaptureOutput
    if ($netResult.ExitCode -ne 0) {
        Write-Output "ERROR: Failed to create Incus bridge. Output: $($netResult.Output)"
        exit 1
    }
    Write-Step "Bridge network 'incusbr0' created (10.10.10.1/24)."
} else {
    Write-Step "Bridge network 'incusbr0' already exists."
}

Write-Step "Adding NIC to default profile..."
$nicCheck = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus profile show default 2>/dev/null | grep -c \"incusbr0\"'" 15 -CaptureOutput
if ($nicCheck.Output -match '^0$' -or [string]::IsNullOrWhiteSpace($nicCheck.Output)) {
    $nicResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'incus profile device add default eth0 nic network=incusbr0 name=eth0'" 15 -CaptureOutput
    if ($nicResult.ExitCode -ne 0) {
        Write-Output "ERROR: Failed to add NIC to default profile. Output: $($nicResult.Output)"
        exit 1
    }
    Write-Step "NIC 'eth0' (incusbr0) added to default profile."
} else {
    Write-Step "NIC already present in default profile."
}

Write-Step "Verifying lxc CLI..."
$versionResult = Invoke-External "wsl.exe" "-d $distroName -u root -- bash -lc 'lxc --version'" 15 -CaptureOutput
if ($versionResult.ExitCode -ne 0) {
    Write-Output "ERROR: lxc command not available. Output: $($versionResult.Output)"
    exit 1
}
Write-Step "lxc (Incus) version $($versionResult.Output) ready."

# 7. Add user to incus group
Write-Step "Adding user '$env:USERNAME' to incus group..."
& wsl -d $distroName -u root -- usermod -aG incus $env:USERNAME 2>&1 | Out-Null

Write-Step "=== Setup Complete ==="

[PSCustomObject]@{
    success = $true
    distro = $distroName
    message = "LXC setup complete. Incus $($versionResult.Output) installed in Ubuntu-LXC."
    requiresRestart = $true
} | ConvertTo-Json -Depth 4
