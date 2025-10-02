param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion  # 例: "1.0.1"
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

# 4. パッケージ復元
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore

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
Write-Host "`n📝 Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the application thoroughly"
Write-Host "  2. If there are breaking changes, update your code accordingly"
Write-Host "  3. Commit changes:"
Write-Host "     git add src/KdxDesigner/KdxDesigner.csproj"
Write-Host "     git commit -m 'Update KdxProjects packages to v$NewVersion'"
