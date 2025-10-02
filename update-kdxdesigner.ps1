param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,  # 例: "1.0.1"

    [Parameter(Mandatory=$false)]
    [ValidateSet("Local", "GitHub", "Auto")]
    [string]$Source = "Auto"  # パッケージソース選択
)

$kdxDesignerRoot = $PSScriptRoot
$csprojPath = Join-Path $kdxDesignerRoot "src\KdxDesigner\KdxDesigner.csproj"

Write-Host "Updating KdxDesigner to use KdxProjects $NewVersion..." -ForegroundColor Green

# 1. .csprojファイルの存在確認
if (-not (Test-Path $csprojPath)) {
    Write-Host "✗ KdxDesigner.csproj not found at: $csprojPath" -ForegroundColor Red
    exit 1
}

# 2. .csprojファイルのパッケージバージョンを更新
Write-Host "Updating KdxDesigner.csproj..." -ForegroundColor Yellow

$csprojContent = Get-Content $csprojPath -Raw

# 各パッケージのバージョンを更新
$csprojContent = $csprojContent -replace 'Include="Kdx\.Contracts" Version="[\d\.]+"', "Include=`"Kdx.Contracts`" Version=`"$NewVersion`""
$csprojContent = $csprojContent -replace 'Include="Kdx\.Contracts\.ViewModels" Version="[\d\.]+"', "Include=`"Kdx.Contracts.ViewModels`" Version=`"$NewVersion`""
$csprojContent = $csprojContent -replace 'Include="Kdx\.Core" Version="[\d\.]+"', "Include=`"Kdx.Core`" Version=`"$NewVersion`""
$csprojContent = $csprojContent -replace 'Include="Kdx\.Infrastructure\.Supabase" Version="[\d\.]+"', "Include=`"Kdx.Infrastructure.Supabase`" Version=`"$NewVersion`""
$csprojContent = $csprojContent -replace 'Include="Kdx\.Infrastructure" Version="[\d\.]+"', "Include=`"Kdx.Infrastructure`" Version=`"$NewVersion`""

Set-Content -Path $csprojPath -Value $csprojContent
Write-Host "✓ Updated KdxDesigner.csproj" -ForegroundColor Cyan

# 3. NuGetキャッシュをクリア
Set-Location $kdxDesignerRoot
Write-Host "`nClearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "✓ Cache cleared" -ForegroundColor Cyan

# 4. パッケージ復元（ソース選択）
Write-Host "`nRestoring packages from $Source source..." -ForegroundColor Yellow

switch ($Source) {
    "Local" {
        Write-Host "Using Local NuGet feed only..." -ForegroundColor Cyan
        dotnet restore --source "C:\NuGetLocal" --source "https://api.nuget.org/v3/index.json"
    }
    "GitHub" {
        Write-Host "Using GitHub Packages..." -ForegroundColor Cyan
        # GitHub認証トークンを確認
        $githubToken = $env:GITHUB_PACKAGES_TOKEN
        if (-not $githubToken) {
            Write-Host "⚠ GITHUB_PACKAGES_TOKEN not set. Trying without authentication..." -ForegroundColor Yellow
        }
        dotnet restore --source "https://nuget.pkg.github.com/KANAMORI-SYSTEM-Inc/index.json" --source "https://api.nuget.org/v3/index.json"
    }
    "Auto" {
        Write-Host "Using nuget.config (Auto)..." -ForegroundColor Cyan
        dotnet restore
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore failed!" -ForegroundColor Red
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check that NuGet packages exist in C:\NuGetLocal" -ForegroundColor White
    Write-Host "  2. Verify nuget.config has KdxLocal source configured" -ForegroundColor White
    Write-Host "  3. Try running: dotnet restore --no-cache" -ForegroundColor White
    exit 1
}
Write-Host "✓ Packages restored" -ForegroundColor Cyan

# 5. ビルド
Write-Host "`nBuilding Release..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Write-Host "`nPossible issues:" -ForegroundColor Yellow
    Write-Host "  - Breaking changes in KdxProjects v$NewVersion" -ForegroundColor White
    Write-Host "  - Check CHANGELOG.md in KdxProjects for migration guide" -ForegroundColor White
    exit 1
}
Write-Host "✓ Build succeeded" -ForegroundColor Cyan

# 6. パッケージ参照の確認
Write-Host "`n📦 Updated package references:" -ForegroundColor Green
$csprojContent | Select-String 'PackageReference Include="Kdx\.' | ForEach-Object {
    $line = $_.Line.Trim()
    if ($line -match 'Include="(Kdx[^"]+)" Version="([^"]+)"') {
        Write-Host "  - $($matches[1]): $($matches[2])" -ForegroundColor White
    }
}

Write-Host "`n✓ KdxDesigner updated to use KdxProjects $NewVersion successfully!" -ForegroundColor Green

# パッケージソースの情報を表示
Write-Host "`n📦 Package source used: $Source" -ForegroundColor Cyan
switch ($Source) {
    "Local" {
        Write-Host "  Packages loaded from: C:\NuGetLocal" -ForegroundColor White
    }
    "GitHub" {
        Write-Host "  Packages loaded from: GitHub Packages" -ForegroundColor White
    }
    "Auto" {
        Write-Host "  Packages loaded from: nuget.config priority order" -ForegroundColor White
        Write-Host "    1. C:\NuGetLocal (Local)" -ForegroundColor White
        Write-Host "    2. GitHub Packages (Remote)" -ForegroundColor White
        Write-Host "    3. NuGet.org (External)" -ForegroundColor White
    }
}

Write-Host "`n📝 Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the application thoroughly"
Write-Host "  2. If there are breaking changes, update your code accordingly"
Write-Host "  3. Commit changes:"
Write-Host "     git add src/KdxDesigner/KdxDesigner.csproj"
Write-Host "     git commit -m 'Update KdxProjects packages to v$NewVersion'"
