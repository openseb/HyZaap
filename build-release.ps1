# HyZaap Build and Release Script
# Automates building, versioning, and GitHub release publishing
#
# Usage:
#   .\build-release.ps1                    # Patch bump (1.0.0 → 1.0.1)
#   .\build-release.ps1 -BumpType minor     # Minor bump (1.0.0 → 1.1.0)
#   .\build-release.ps1 -BumpType major     # Major bump (1.0.0 → 2.0.0)
#   .\build-release.ps1 -SkipRelease       # Skip GitHub release creation
#
# If you get execution policy errors, run:
#   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("patch", "minor", "major", "none")]
    [string]$BumpType = "patch",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTag = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipRelease = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$ReleaseNotes = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$PreRelease = $false
)

$ErrorActionPreference = "Stop"

# Get current version from version.txt
function Get-CurrentVersion {
    if (Test-Path "version.txt") {
        $version = Get-Content "version.txt" -Raw | ForEach-Object { $_.Trim() }
        return $version
    }
    return "1.0.0"
}

# Update version in version.txt
function Set-VersionFile($version) {
    Set-Content -Path "version.txt" -Value $version -NoNewline
    Write-Host "Updated version.txt to $version"
}

# Update version in .csproj file
function Update-ProjectVersion($version) {
    $csprojPath = "HyZaap.csproj"
    if (-not (Test-Path $csprojPath)) {
        Write-Host "ERROR: HyZaap.csproj not found!" -ForegroundColor Red
        exit 1
    }
    
    $content = Get-Content $csprojPath -Raw
    
    # Update Version, AssemblyVersion, and FileVersion
    $content = $content -replace '<Version>.*?</Version>', "<Version>$version</Version>"
    $content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$version.0</AssemblyVersion>"
    $content = $content -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$version.0</FileVersion>"
    
    Set-Content -Path $csprojPath -Value $content -NoNewline
    Write-Host "Updated HyZaap.csproj version to $version"
}

# Bump version based on type
function Bump-Version($currentVersion, $bumpType) {
    $parts = $currentVersion -split '\.'
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($bumpType) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
        "none" {
            # Don't bump, use current version
        }
    }
    
    return "$major.$minor.$patch"
}

# Build the project
function Build-Project {
    Write-Host "Building project..."
    
    # Clean previous builds
    if (Test-Path "bin\Release") {
        Remove-Item -Path "bin\Release" -Recurse -Force
        Write-Host "Cleaned previous release builds"
    }
    
    # Restore dependencies
    Write-Host "Restoring dependencies..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to restore dependencies" -ForegroundColor Red
        exit 1
    }
    
    # Build release
    Write-Host "Building release configuration..."
    dotnet build -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Build completed successfully"
}

# Publish the project
function Publish-Project {
    Write-Host "Publishing project..."
    
    $publishDir = "bin\Release\net10.0-windows\win-x64\publish"
    
    # Remove old publish directory
    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force
    }
    
    # Publish as self-contained single file
    dotnet publish -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Publish failed" -ForegroundColor Red
        exit 1
    }
    
    # Resolve full path and verify it exists
    $fullPath = Resolve-Path $publishDir -ErrorAction SilentlyContinue
    if (-not $fullPath -or -not (Test-Path $publishDir)) {
        Write-Host "ERROR: Publish directory not found: $publishDir" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Publish completed successfully"
    return $publishDir
}

# Create ZIP archive
function Create-ZipArchive($publishDir, $version) {
    Write-Host "Creating ZIP archive..."
    
    # Verify publish directory exists
    if (-not (Test-Path $publishDir)) {
        Write-Host "ERROR: Publish directory not found: $publishDir" -ForegroundColor Red
        exit 1
    }
    
    $zipName = "HyZaap-v$version.zip"
    
    # Remove old ZIP if exists
    if (Test-Path $zipName) {
        Remove-Item -Path $zipName -Force
    }
    
    # Create ZIP with all files from publish directory
    $zipPath = Join-Path $publishDir "*"
    Compress-Archive -Path $zipPath -DestinationPath $zipName -Force
    
    if (-not (Test-Path $zipName)) {
        Write-Host "ERROR: Failed to create ZIP archive" -ForegroundColor Red
        exit 1
    }
    
    $zipSize = (Get-Item $zipName).Length / 1MB
    Write-Host "Created $zipName ($([math]::Round($zipSize, 2)) MB)"
    
    return $zipName
}

# Create git tag
function Create-GitTag($version) {
    Write-Host "Creating git tag v$version..."
    
    try {
        $ErrorActionPreference = "Continue"
        # Check if tag already exists
        $existingTag = git tag -l "v$version" 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $existingTag = git tag -l "v$version"
            if ($existingTag) {
                Write-Host "WARNING: Tag v$version already exists. Skipping tag creation." -ForegroundColor Yellow
                return
            }
        }
        
        # Create annotated tag
        git tag -a "v$version" -m "Release version $version" 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Created git tag v$version"
        } else {
            throw "Git tag creation failed"
        }
    } catch {
        Write-Host "WARNING: Failed to create git tag. Git may not be configured." -ForegroundColor Yellow
        Write-Host "Configure git with: git config user.name 'Your Name' && git config user.email 'your.email@example.com'"
    } finally {
        $ErrorActionPreference = "Stop"
    }
}

# Push git tag to remote
function Push-GitTag($version) {
    Write-Host "Pushing git tag to remote..."
    
    try {
        $ErrorActionPreference = "Continue"
        git push origin "v$version" 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Pushed tag v$version to remote"
        } else {
            throw "Git push failed"
        }
    } catch {
        Write-Host "WARNING: Failed to push tag. You may need to push manually: git push origin v$version" -ForegroundColor Yellow
    } finally {
        $ErrorActionPreference = "Stop"
    }
}

# Create GitHub release
function Create-GitHubRelease($version, $zipPath, $notes, $isPreRelease) {
    Write-Host "Creating GitHub release..."
    
    # Get repository URL from git remote
    $repoUrl = ""
    try {
        $remoteUrl = git remote get-url origin 2>&1
        if ($LASTEXITCODE -eq 0) {
            # Extract owner/repo from URL (handles both https and ssh formats)
            if ($remoteUrl -match "github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?$") {
                $owner = $matches[1]
                $repo = $matches[2]
                $repoUrl = "$owner/$repo"
            }
        }
    } catch {
        # Ignore errors
    }
    
    # Check if GitHub CLI is installed
    $ghInstalled = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghInstalled) {
        Write-Host "WARNING: GitHub CLI (gh) not found. Skipping GitHub release creation." -ForegroundColor Yellow
        Write-Host "Install GitHub CLI from: https://cli.github.com/"
        if ($repoUrl) {
            Write-Host "Or create release manually at: https://github.com/$repoUrl/releases/new"
        } else {
            Write-Host "Or create release manually at: https://github.com/YOUR_USERNAME/HyZaap/releases/new"
        }
        Write-Host "Tag: v$version"
        Write-Host "ZIP file: $zipPath"
        return
    }
    
    # Check if authenticated
    $ghAuth = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: GitHub CLI not authenticated. Skipping GitHub release creation." -ForegroundColor Yellow
        Write-Host "Run: gh auth login"
        if ($repoUrl) {
            Write-Host "Or create release manually at: https://github.com/$repoUrl/releases/new"
        }
        return
    }
    
    # Prepare release notes
    if ([string]::IsNullOrWhiteSpace($notes)) {
        $notes = @"
## HyZaap v$version

### Changes
- See commit history for details

### Installation
1. Download `HyZaap-v$version.zip`
2. Extract to your desired location
3. Run `HyZaap.exe`

### Requirements
- Windows 10 or later
- .NET 10.0 Desktop Runtime (included)
"@
    }
    
    # Create release
    $releaseFlags = @("v$version", $zipPath, "--title", "HyZaap v$version", "--notes", $notes)
    if ($isPreRelease) {
        $releaseFlags += "--prerelease"
    }
    
    gh release create $releaseFlags
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Failed to create GitHub release. You may need to create it manually." -ForegroundColor Yellow
    } else {
        Write-Host "Created GitHub release v$version"
    }
}

# Main execution
Write-Host "========================================"
Write-Host "HyZaap Build and Release Script"
Write-Host "========================================"
Write-Host ""

# Get current version
$currentVersion = Get-CurrentVersion
Write-Host "Current version: $currentVersion"

# Determine new version
if ($BumpType -eq "none") {
    $newVersion = $currentVersion
    Write-Host "No version bump requested, using current version"
} else {
    $newVersion = Bump-Version -currentVersion $currentVersion -bumpType $BumpType
    Write-Host "Bumping $BumpType version: $currentVersion -> $newVersion"
    
    # Update version files
    Set-VersionFile -version $newVersion
    Update-ProjectVersion -version $newVersion
    
    # Commit version changes
    Write-Host "Committing version changes..."
    try {
        $ErrorActionPreference = "Continue"
        git add version.txt HyZaap.csproj 2>&1 | Out-Null
        git commit -m "Bump version to $newVersion" 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Committed version changes"
        } else {
            throw "Git commit failed"
        }
    } catch {
        Write-Host "WARNING: Failed to commit version changes. Git may not be configured." -ForegroundColor Yellow
        Write-Host "Configure git with: git config user.name 'Your Name' && git config user.email 'your.email@example.com'"
    } finally {
        $ErrorActionPreference = "Stop"
    }
}

# Build and publish
if (-not $SkipBuild) {
    Build-Project
    $publishDir = Publish-Project
    $zipPath = Create-ZipArchive -publishDir $publishDir -version $newVersion
} else {
    Write-Host "Skipping build (--SkipBuild flag set)"
    $zipPath = "HyZaap-v$newVersion.zip"
    if (-not (Test-Path $zipPath)) {
        Write-Host "ERROR: ZIP file not found: $zipPath" -ForegroundColor Red
        Write-Host "Run without --SkipBuild to create it"
        exit 1
    }
}

# Create git tag
if (-not $SkipTag) {
    Create-GitTag -version $newVersion
    Push-GitTag -version $newVersion
} else {
    Write-Host "Skipping tag creation (--SkipTag flag set)"
}

# Create GitHub release
if (-not $SkipRelease) {
    Create-GitHubRelease -version $newVersion -zipPath $zipPath -notes $ReleaseNotes -isPreRelease $PreRelease
} else {
    Write-Host "Skipping GitHub release (--SkipRelease flag set)"
}

# Summary
Write-Host ""
Write-Host "========================================"
Write-Host "Release Complete!"
Write-Host "========================================"
Write-Host ""
Write-Host "Version: $newVersion"
Write-Host "ZIP File: $zipPath"
Write-Host ""

if ($SkipTag) {
    Write-Host "Next Steps:"
    Write-Host "1. Create git tag: git tag -a v$newVersion -m 'Release version $newVersion'"
    Write-Host "2. Push tag: git push origin v$newVersion"
}

if ($SkipRelease) {
    Write-Host "Next Steps:"
    Write-Host "3. Create GitHub release: https://github.com/yourusername/HyZaap/releases/new"
    Write-Host "4. Upload $zipPath as release asset"
}

Write-Host ""
Write-Host "Done!"
