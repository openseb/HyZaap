# Simple script to push all changes to master instantly
# Usage: .\push-master.ps1 [commit message]

param(
    [string]$Message = "Update"
)

$ErrorActionPreference = "Continue"

Write-Host "Pushing all changes to master..."
Write-Host ""

# Add all changes
Write-Host "Adding all changes..."
git add . 2>&1 | Out-Null

$hasChanges = $false
$committed = $false

# Check if there are changes to commit
$status = git status --porcelain
if (-not [string]::IsNullOrWhiteSpace($status)) {
    $hasChanges = $true
    # Commit changes
    Write-Host "Committing changes..."
    git commit -m $Message 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Committed: $Message"
        $committed = $true
    } else {
        Write-Host "ERROR: Commit failed. Git may not be configured." -ForegroundColor Red
        Write-Host "Configure git with: git config user.name 'Your Name' && git config user.email 'your.email@example.com'" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "No changes to commit."
}

# Check if there are commits to push
$commitsAhead = git rev-list --count origin/master..HEAD 2>&1
if ($LASTEXITCODE -ne 0) {
    $commitsAhead = 0
}

if ($commitsAhead -eq 0 -and -not $committed) {
    Write-Host "Nothing to push."
    Write-Host ""
    Write-Host "Done!"
    exit 0
}

# Push to master
Write-Host "Pushing to master..."
git push origin master 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Pushed to master successfully!"
} else {
    Write-Host "ERROR: Failed to push to master" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Done!"

