param(
    [string]$Name,
    [string]$Image
)

if ([string]::IsNullOrWhiteSpace($Name)) {
    Write-Output "A container name is required for this action."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Image)) {
    Write-Output "An image name is required for this action."
    exit 1
}

$distroName = "Ubuntu-LXC"

# Ensure storage pool, root disk, and network (in case setup didn't configure them)
$poolCheck = & wsl -d $distroName -u root -- bash -lc 'incus storage list --format csv 2>/dev/null | head -1' 2>$null
if ([string]::IsNullOrWhiteSpace($poolCheck)) {
    Write-Output "Creating storage pool..."
    & wsl -d $distroName -u root -- bash -lc 'incus storage create default dir' 2>&1 | Out-Null
}
$rootCheck = & wsl -d $distroName -u root -- bash -lc 'incus profile show default 2>/dev/null | grep -c "root disk"' 2>$null
if ($rootCheck -eq 0) {
    Write-Output "Adding root disk device to default profile..."
    & wsl -d $distroName -u root -- bash -lc 'incus profile device add default root disk path=/ pool=default' 2>&1 | Out-Null
}
$nicCheck = & wsl -d $distroName -u root -- bash -lc 'incus profile show default 2>/dev/null | grep -c incusbr0' 2>$null
if ($nicCheck -eq 0) {
    Write-Output "Creating bridge network and adding NIC to default profile..."
    & wsl -d $distroName -u root -- bash -lc 'incus network create incusbr0 ipv4.address=10.10.10.1/24 ipv4.nat=true ipv6.address=none 2>/dev/null || true'
    & wsl -d $distroName -u root -- bash -lc 'incus profile device add default eth0 nic network=incusbr0 name=eth0' 2>&1 | Out-Null
}

$incusImage = if ($Image -match ':') {
    $Image
} elseif ($Image -match '/') {
    "images:$Image"
} else {
    switch -Wildcard ($Image) {
        "ubuntu*"    { "images:ubuntu/26.04" }
        "debian*"    { "images:debian/12" }
        "alpine*"    { "images:alpine/3.21" }
        "centos*"    { "images:centos/9" }
        "fedora*"    { "images:fedora/41" }
        "rocky*"     { "images:rockylinux/9" }
        "arch*"      { "images:archlinux" }
        "busybox*"   { "images:busybox/1.36" }
        default      { "images:$Image" }
    }
}

Write-Output "Creating container '$Name' from image '$incusImage'..."

$result = & wsl -d $distroName -u root -- lxc init $incusImage $Name 2>&1
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Output "Container '$Name' created successfully."
} else {
    Write-Output "Failed to create container: $result"
    Write-Output "You can list available images with: lxc image list images:"
}

exit $exitCode
