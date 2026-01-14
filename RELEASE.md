# Release Guide

This document explains how to use the automated build and release script.

## Quick Start

### Basic Release (Patch Version Bump)
```powershell
.\build-release.ps1
```
This will:
- Bump the patch version (1.0.0 → 1.0.1)
- Build the project
- Create a ZIP file
- Create and push a git tag
- Create a GitHub release (if GitHub CLI is configured)

### Minor Version Bump
```powershell
.\build-release.ps1 -BumpType minor
```
Bumps: 1.0.0 → 1.1.0

### Major Version Bump
```powershell
.\build-release.ps1 -BumpType major
```
Bumps: 1.0.0 → 2.0.0

### No Version Bump
```powershell
.\build-release.ps1 -BumpType none
```
Uses the current version without bumping.

## Advanced Usage

### Skip Build (Use Existing Build)
```powershell
.\build-release.ps1 -SkipBuild
```
Useful if you've already built and just want to create a release.

### Skip Tag Creation
```powershell
.\build-release.ps1 -SkipTag
```
Creates build and ZIP but doesn't create git tag.

### Skip GitHub Release
```powershell
.\build-release.ps1 -SkipRelease
```
Creates build and tag but doesn't create GitHub release.

### Custom Release Notes
```powershell
.\build-release.ps1 -ReleaseNotes "Fixed critical bug in authentication flow"
```

### Pre-Release (Beta/Alpha)
```powershell
.\build-release.ps1 -PreRelease
```
Marks the GitHub release as a pre-release.

### Combined Options
```powershell
.\build-release.ps1 -BumpType minor -ReleaseNotes "Added new features" -PreRelease
```

## Prerequisites

1. **.NET SDK 10.0** - Required for building
2. **Git** - Required for version control and tagging
3. **GitHub CLI (optional)** - For automatic GitHub release creation
   - Install from: https://cli.github.com/
   - Authenticate: `gh auth login`

## Version Tracking

The script uses `version.txt` to track the current version. The version format is `MAJOR.MINOR.PATCH` (e.g., `1.0.0`).

The version is also stored in `HyZaap.csproj` and will be updated automatically when you run the script.

## Workflow

1. **Make your changes** and commit them
2. **Run the script**: `.\build-release.ps1`
3. **The script will**:
   - Read current version from `version.txt`
   - Bump version based on `-BumpType`
   - Update `version.txt` and `HyZaap.csproj`
   - Commit version changes
   - Build the project
   - Create a ZIP archive
   - Create a git tag
   - Push tag to remote
   - Create GitHub release (if GitHub CLI is available)

## Manual Steps (if GitHub CLI not available)

If you don't have GitHub CLI installed or configured:

1. Run: `.\build-release.ps1 -SkipRelease`
2. Go to: https://github.com/yourusername/HyZaap/releases/new
3. Select tag: `v1.0.1` (or your version)
4. Upload: `HyZaap-v1.0.1.zip`
5. Add release notes
6. Click "Publish release"

## Troubleshooting

### "GitHub CLI not found"
- Install GitHub CLI: https://cli.github.com/
- Or use `-SkipRelease` flag and create release manually

### "GitHub CLI not authenticated"
- Run: `gh auth login`
- Or use `-SkipRelease` flag

### "Tag already exists"
- The script will skip tag creation if tag exists
- Delete tag if needed: `git tag -d v1.0.1` and `git push origin :refs/tags/v1.0.1`

### "Build failed"
- Ensure .NET SDK 10.0 is installed
- Check for compilation errors
- Run `dotnet restore` manually

### "ZIP file not found"
- Ensure build completed successfully
- Check `bin/Release/net10.0-windows/win-x64/publish/` directory

## Examples

### Release a Hotfix
```powershell
# Make your fixes, commit them
git add .
git commit -m "Fix critical bug"
.\build-release.ps1 -BumpType patch
```

### Release a Feature Update
```powershell
# Make your changes, commit them
git add .
git commit -m "Add new feature"
.\build-release.ps1 -BumpType minor -ReleaseNotes "Added new server management features"
```

### Release a Major Update
```powershell
# Make your changes, commit them
git add .
git commit -m "Major refactor"
.\build-release.ps1 -BumpType major -ReleaseNotes "Complete UI overhaul and new features"
```

### Create a Beta Release
```powershell
.\build-release.ps1 -BumpType minor -PreRelease -ReleaseNotes "Beta release - testing new features"
```

