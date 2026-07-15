param([Parameter(Mandatory=$true)][string]$Branch, [string]$Path)
if (-not $Path) { $Path = Join-Path (Split-Path (Get-Location)) $Branch.Replace('/','-') }
git worktree add -b $Branch $Path
Write-Host "Worktree $Path — open Unity there so Library is per-worktree."
