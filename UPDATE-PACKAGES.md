# KdxProjects パッケージ更新ガイド

## 🚀 クイックスタート

KdxProjectsライブラリが更新された場合、以下の手順でKdxDesignerを更新します。

### 自動更新（推奨）

```powershell
# KdxDesignerディレクトリで実行
.\update-kdxdesigner.ps1 -NewVersion "1.0.1"
```

このスクリプトは自動的に：
1. ✅ KdxDesigner.csprojのパッケージバージョンを更新
2. ✅ NuGetキャッシュをクリア
3. ✅ パッケージを復元
4. ✅ Releaseビルドを実行

### 手動更新

```powershell
# 1. KdxDesigner.csprojを編集
# src/KdxDesigner/KdxDesigner.csproj

<ItemGroup>
  <PackageReference Include="Kdx.Contracts" Version="1.0.1" />
  <PackageReference Include="Kdx.Contracts.ViewModels" Version="1.0.1" />
  <PackageReference Include="Kdx.Core" Version="1.0.1" />
  <PackageReference Include="Kdx.Infrastructure" Version="1.0.1" />
  <PackageReference Include="Kdx.Infrastructure.Supabase" Version="1.0.1" />
</ItemGroup>

# 2. NuGetキャッシュをクリア
dotnet nuget locals all --clear

# 3. パッケージを復元
dotnet restore

# 4. ビルド
dotnet build -c Release
```

## 📋 前提条件

### GitHub Packagesからの取得（本番・推奨）

KdxProjectsの新しいバージョンがGitHub Packagesに公開されている必要があります。

**確認方法:**
```
https://github.com/orgs/KANAMORI-SYSTEM-Inc/packages
```

**認証設定:**
```powershell
# 環境変数GITHUB_PACKAGES_TOKENを設定（初回のみ）
[System.Environment]::SetEnvironmentVariable('GITHUB_PACKAGES_TOKEN', 'your-github-pat', 'User')
```

GitHub Personal Access Token (PAT)の作成:
- GitHub → Settings → Developer settings → Personal access tokens
- Scopes: `read:packages`

### ローカルフィードからの取得（開発・テスト用）

ローカル開発時は、nuget.configでローカルフィードをコメント解除してください。

```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <add key="KdxGitHub" value="https://nuget.pkg.github.com/KANAMORI-SYSTEM-Inc/index.json" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  <!-- ローカル開発時のみコメント解除 -->
  <add key="KdxLocal" value="C:\NuGetLocal" />
</packageSources>
```

ローカルフィードのパッケージを確認:
```powershell
dir C:\NuGetLocal\*.nupkg
```

KdxProjectsで更新を実行:
```powershell
cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxProjects
.\update-kdxprojects.ps1 -NewVersion "1.0.1"
```

## 💥 破壊的変更への対応

メジャーバージョンアップ（例: 1.x.x → 2.0.0）の場合、コード修正が必要な場合があります。

### 手順

1. **CHANGELOG確認**
   ```powershell
   # KdxProjectsのCHANGELOGを確認
   cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxProjects
   cat CHANGELOG.md | Select-String "Breaking" -Context 10
   ```

2. **コード修正**
   - Breaking Changesセクションの指示に従ってコードを修正

3. **パッケージ更新**
   ```powershell
   cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxDesigner
   .\update-kdxdesigner.ps1 -NewVersion "2.0.0"
   ```

4. **ビルドエラー修正**
   - エラーメッセージを確認し、必要に応じてコードを修正

## ⚠️ トラブルシューティング

### エラー: パッケージが見つからない

```powershell
# 原因1: GitHub Packagesに未公開
# 解決策: KdxProjectsでタグをpushしてリリース
cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxProjects
git tag -a v1.0.1 -m "Release v1.0.1"
git push origin v1.0.1
# GitHub Actionsが自動公開（2-3分待つ）

# 原因2: 認証エラー
# 解決策: GITHUB_PACKAGES_TOKENを設定
$env:GITHUB_PACKAGES_TOKEN = "your-github-pat"

# 原因3: ローカルフィードにパッケージが存在しない（ローカル開発時）
# 解決策:
cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxProjects
.\update-kdxprojects.ps1 -NewVersion "1.0.1"
```

### エラー: 古いバージョンが使われる

```powershell
# 原因: NuGetキャッシュに古いバージョンが残っている
# 解決策:
dotnet nuget locals all --clear
dotnet restore --no-cache
```

### エラー: ビルド失敗

```powershell
# 原因: 破壊的変更がある
# 解決策:
# 1. KdxProjectsのCHANGELOG.mdで破壊的変更を確認
# 2. エラーメッセージから変更箇所を特定
# 3. コードを修正
```

## 📊 バージョニング理解

| KdxProjectsバージョン | 変更内容 | KdxDesigner対応 |
|---------------------|---------|----------------|
| 1.0.0 → 1.0.1 | バグ修正 | そのまま更新可能 |
| 1.0.1 → 1.1.0 | 新機能追加 | そのまま更新可能 |
| 1.1.0 → 2.0.0 | 破壊的変更 | コード修正必要 |

## ✅ 更新後のチェックリスト

- [ ] ビルド成功
- [ ] アプリケーション起動確認
- [ ] 主要機能テスト
  - [ ] プロセスフロー表示
  - [ ] シリンダー設定
  - [ ] インターロック設定
  - [ ] データベース接続
- [ ] コミット
  ```powershell
  git add src/KdxDesigner/KdxDesigner.csproj
  git commit -m "Update KdxProjects packages to v1.0.1"
  ```

## 🔗 関連ドキュメント

- [KdxProjects 更新ワークフロー](../../kdx_projects/docs/kdxprojects-update-workflow.md) - KdxProjects側の更新手順
- [KdxProjects QUICK-UPDATE-GUIDE](../../KdxProjects/QUICK-UPDATE-GUIDE.md) - クイックリファレンス
- [README.md](README.md) - プロジェクト概要

---

**最終更新**: 2025-10-02
