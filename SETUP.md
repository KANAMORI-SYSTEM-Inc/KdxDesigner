# KdxDesigner セットアップガイド

## 📋 目次

1. [前提条件](#前提条件)
2. [環境変数の設定](#環境変数の設定)
3. [ビルドと実行](#ビルドと実行)
4. [トラブルシューティング](#トラブルシューティング)

## 前提条件

- Windows 10/11
- .NET 8.0 SDK
- Git
- Visual Studio 2022 または VS Code（推奨）
- GitHub Personal Access Token（GitHub Packagesへのアクセス用）

## 環境変数の設定

KdxDesignerは環境変数を使用して、機密情報や環境固有の設定を管理します。

### 方法1: .envファイルを使用（推奨）

#### 1. .envファイルの作成

```powershell
# KdxDesignerディレクトリで実行
cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxDesigner

# .env.exampleをコピー
Copy-Item .env.example .env
```

#### 2. .envファイルの編集

`.env`ファイルをテキストエディタで開いて、各値を設定します：

```env
# GitHub Packages 認証
GITHUB_PACKAGES_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Supabase データベース接続
SUPABASE_URL=https://xxxxxxxxxxxxx.supabase.co
SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# アプリケーション設定
ENVIRONMENT=Development
LOG_LEVEL=Debug

# ローカルNuGetフィード使用（開発時のみ）
USE_LOCAL_NUGET=false
LOCAL_NUGET_PATH=C:\NuGetLocal
```

#### 3. GitHub Personal Access Tokenの取得

GitHub PATが必要です（GitHub Packagesからパッケージをダウンロードするため）。

**取得手順:**

1. GitHubにログイン
2. Settings → Developer settings → Personal access tokens → Tokens (classic)
3. "Generate new token (classic)" をクリック
4. 設定:
   - Note: `KdxDesigner NuGet Packages`
   - Expiration: 90 days または適切な期間
   - Scopes:
     - ✅ `read:packages` - パッケージの読み取り
5. "Generate token" をクリック
6. **トークンをコピーして`.env`ファイルの`GITHUB_PACKAGES_TOKEN`に貼り付け**

#### 4. Supabase接続情報の取得

1. Supabaseプロジェクトにログイン
2. Project Settings → API
3. 以下をコピー:
   - **Project URL** → `SUPABASE_URL`
   - **anon public key** → `SUPABASE_ANON_KEY`

#### 5. .envファイルの確認

```powershell
# .envファイルが存在することを確認
Test-Path .env
# → True

# .envファイルがGitに追跡されていないことを確認
git status .env
# → "Untracked files" または表示されない（.gitignoreで除外されている）
```

### 方法2: Windows環境変数を使用

システム全体またはユーザーレベルで環境変数を設定することもできます。

```powershell
# ユーザー環境変数として設定（推奨）
[System.Environment]::SetEnvironmentVariable('GITHUB_PACKAGES_TOKEN', 'ghp_xxxx...', 'User')
[System.Environment]::SetEnvironmentVariable('SUPABASE_URL', 'https://xxxx.supabase.co', 'User')
[System.Environment]::SetEnvironmentVariable('SUPABASE_ANON_KEY', 'eyJhbGci...', 'User')

# 確認
echo $env:GITHUB_PACKAGES_TOKEN
echo $env:SUPABASE_URL
```

**注意:** Windows環境変数を設定した場合、PowerShellまたはVisual Studioを再起動する必要があります。

### 方法3: PowerShellセッションで一時的に設定

開発セッション中のみ有効な環境変数を設定:

```powershell
# PowerShellで実行（セッション中のみ有効）
$env:GITHUB_PACKAGES_TOKEN = "ghp_xxxxxxxxxxxxxxxxxxxx"
$env:SUPABASE_URL = "https://xxxxxxxxxxxxx.supabase.co"
$env:SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Visual Studioをこのセッションから起動
devenv KdxDesigner.sln
```

## ビルドと実行

### 1. 依存パッケージの復元

```powershell
# NuGetパッケージを復元
dotnet restore

# GitHub Packagesから自動的にKdxProjectsパッケージがダウンロードされます
```

**エラーが出る場合:**
- `GITHUB_PACKAGES_TOKEN`が正しく設定されているか確認
- nuget.configが正しいか確認

### 2. ビルド

```powershell
# Debugビルド
dotnet build

# Releaseビルド
dotnet build -c Release
```

### 3. 実行

```powershell
# コマンドラインから実行
dotnet run --project src/KdxDesigner/KdxDesigner.csproj

# または、Visual Studioから実行
# F5キーまたは「デバッグの開始」
```

## 開発ワークフロー

### ローカルNuGetフィードを使用する場合

KdxProjectsライブラリをローカルで開発している場合:

**1. nuget.configを編集してローカルフィードを有効化:**

```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <!-- ローカルフィードをコメント解除 -->
  <add key="KdxLocal" value="C:\NuGetLocal" />
  <add key="KdxGitHub" value="https://nuget.pkg.github.com/KANAMORI-SYSTEM-Inc/index.json" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

**2. または、.envファイルで設定:**

```env
USE_LOCAL_NUGET=true
LOCAL_NUGET_PATH=C:\NuGetLocal
```

**3. パッケージを更新:**

```powershell
.\update-kdxdesigner.ps1 -NewVersion "1.0.1" -Source Local
```

## トラブルシューティング

### エラー: "Unable to load the service index for source"

**原因:** `GITHUB_PACKAGES_TOKEN`が設定されていない、または無効

**解決策:**
```powershell
# 環境変数を確認
echo $env:GITHUB_PACKAGES_TOKEN

# 未設定の場合、.envファイルを確認または環境変数を設定
```

### エラー: "Package 'Kdx.Contracts' not found"

**原因1:** GitHub Packagesにパッケージが公開されていない

**解決策:**
```powershell
# KdxProjectsでリリースを作成
cd C:\Users\amdet\source\repos\KANAMORI-SYSTEM-Inc\KdxProjects
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
# GitHub Actionsが自動公開（2-3分待つ）
```

**原因2:** 認証エラー

**解決策:**
```powershell
# NuGetキャッシュをクリア
dotnet nuget locals all --clear

# 再度復元
dotnet restore
```

### エラー: "Database connection failed"

**原因:** Supabase接続情報が正しくない

**解決策:**
```powershell
# .envファイルを確認
cat .env | Select-String "SUPABASE"

# Supabase Project Settingsから正しい値を取得
```

### Visual Studioが.envファイルを読み込まない

**.envファイルはNuGet認証には使用されません。** NuGet認証には以下のいずれかが必要:

1. Windows環境変数（推奨）
   ```powershell
   [System.Environment]::SetEnvironmentVariable('GITHUB_PACKAGES_TOKEN', 'ghp_xxx', 'User')
   # Visual Studioを再起動
   ```

2. nuget.configに直接記載（非推奨 - セキュリティリスク）

3. dotnet CLIで認証情報を保存
   ```powershell
   dotnet nuget add source https://nuget.pkg.github.com/KANAMORI-SYSTEM-Inc/index.json \
     --name KdxGitHub \
     --username KANAMORI-SYSTEM-Inc \
     --password ghp_xxxxxxxxxxxx \
     --store-password-in-clear-text
   ```

## セキュリティ

### ⚠️ 重要な注意事項

- ✅ `.env`ファイルはGitにコミットしないでください（.gitignoreで除外済み）
- ✅ GitHub PATは定期的に更新してください
- ✅ Supabase Service Role Keyは本番環境でのみ使用
- ❌ トークンやキーをソースコードに直接書かないでください
- ❌ .envファイルをSlack/Teams等で共有しないでください

### チーム共有

新しいチームメンバーに必要な情報:

1. このSETUP.mdドキュメント
2. Supabaseプロジェクトへのアクセス権
3. GitHubリポジトリへのアクセス権
4. 各自でGitHub PATを作成してもらう

## 次のステップ

- [README.md](README.md) - プロジェクト概要
- [UPDATE-PACKAGES.md](UPDATE-PACKAGES.md) - パッケージ更新手順
- [CHANGELOG.md](CHANGELOG.md) - 変更履歴

---

**作成日**: 2025-10-02
**最終更新**: 2025-10-02
