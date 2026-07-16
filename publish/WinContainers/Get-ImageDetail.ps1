param([string]$Repository)

if (-not $Repository) {
  return "{}"
}

try {
  # Handle official library images (namespace = "library")
  $normalizedRepo = if ($Repository -notmatch "/") { "library/$Repository" } else { $Repository }
  
  $baseUrl = "https://hub.docker.com/v2/repositories/$normalizedRepo"
  $headers = @{ "User-Agent" = "WinContainers/1.0" }
  
  $repoResponse = Invoke-RestMethod -Uri $baseUrl -Headers $headers -ErrorAction Stop
  
  $tagsUrl = "$baseUrl/tags/?page_size=50&ordering=-last_updated"
  $tagsResponse = Invoke-RestMethod -Uri $tagsUrl -Headers $headers -ErrorAction Stop
  
  $tags = $tagsResponse.results | ForEach-Object {
    $images = $_.images
    if (-not $images) { return $null }
    
    $primary = $images | Where-Object { $_.architecture -eq 'amd64' -and $_.os -eq 'linux' } | Select-Object -First 1
    if (-not $primary) { $primary = $images | Select-Object -First 1 }
    if (-not $primary) { return $null }
    
    @{
      name = $_.name
      architecture = $primary.architecture
      variant = $primary.variant
      os = $primary.os
      size = $primary.size
      digest = $primary.digest
      last_pushed = $primary.last_pushed
      last_pulled = $primary.last_pulled
      status = $primary.status
    }
  } | Where-Object { $_ -ne $null }
  
  $result = @{
    name = $repoResponse.name
    namespace = $repoResponse.namespace
    description = $repoResponse.description
    is_official = $repoResponse.is_official
    is_private = $repoResponse.is_private
    star_count = $repoResponse.star_count
    pull_count = $repoResponse.pull_count
    last_updated = $repoResponse.last_updated
    tags = $tags
  }
  
  $result | ConvertTo-Json -Depth 5 -Compress
}
catch {
  $errorResult = @{
    error = $_.Exception.Message
  }
  $errorResult | ConvertTo-Json -Compress
}
