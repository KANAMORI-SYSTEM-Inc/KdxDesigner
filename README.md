# KdxDesigner

KDXシステムのWPFデスクトップアプリケーション。

## 概要

KdxDesignerは、PLCプログラムの設計・管理を行うWindowsデスクトップアプリケーションです。

## 主な機能

- プロセスフロー図の作成・編集
- シリンダー制御設定
- インターロック条件設定
- タイマー設定
- メモリ管理
- データベース連携（Supabase）

## 必要な環境

- Windows 10 バージョン 1809 以上
- .NET 8.0 Runtime

## インストール

1. 最新リリースをダウンロード
2. インストーラーを実行（将来実装予定）
3. アプリケーションを起動

## 開発環境のセットアップ

**📖 詳細なセットアップ手順は [SETUP.md](SETUP.md) を参照してください。**

### クイックスタート

```bash
# 1. リポジトリのクローン
git clone https://github.com/KANAMORI-SYSTEM-Inc/KdxDesigner.git
cd KdxDesigner

# 2. 環境変数の設定（初回のみ）
Copy-Item .env.example .env
# .envファイルを編集してGITHUB_PACKAGES_TOKEN等を設定

# 3. 依存関係の復元
dotnet restore

# 4. デバッグビルド
dotnet build

# 5. 実行
dotnet run --project src/KdxDesigner/KdxDesigner.csproj
```

### 必要な環境変数

- `GITHUB_PACKAGES_TOKEN` - GitHub Packagesからパッケージを取得するために必要
- `SUPABASE_URL` - Supabaseプロジェクトの接続URL
- `SUPABASE_ANON_KEY` - Supabase認証キー

詳細は [SETUP.md](SETUP.md) を参照してください。

## 依存パッケージ

KdxDesignerは以下のNuGetパッケージを使用しています：

### KdxProjects (内部パッケージ)
- Kdx.Contracts v1.0.0
- Kdx.Core v1.0.0
- Kdx.Infrastructure v1.0.0
- Kdx.Infrastructure.Supabase v1.0.0
- Kdx.Contracts.ViewModels v1.0.0

**📦 パッケージ更新:**
KdxProjectsライブラリが更新された場合は [UPDATE-PACKAGES.md](UPDATE-PACKAGES.md) を参照してください。

```powershell
# クイック更新
.\update-kdxdesigner.ps1 -NewVersion "1.0.1"
```

### 外部パッケージ
- CommunityToolkit.Mvvm v8.4.0 - MVVM support
- ClosedXML v0.105.0 - Excel file operations
- Microsoft.Extensions.* - Dependency Injection, Configuration
- Npgsql v9.0.3 - PostgreSQL client

## プロジェクト構造

```
KdxDesigner/
├── src/
│   └── KdxDesigner/
│       ├── Views/           # WPF Views (XAML)
│       ├── ViewModels/      # ViewModels
│       ├── Models/          # UI Models
│       ├── Services/        # Application Services
│       ├── Utils/           # Utility Classes
│       └── Resources/       # Resources (Styles, etc.)
└── docs/                    # Documentation
```

## 設定

アプリケーション設定は `appsettings.json` で管理されます。

```json
{
  "Supabase": {
    "Url": "your-supabase-url",
    "Key": "your-supabase-anon-key"
  },
  "DeviceOffsets": {
    "...": "..."
  }
}
```

## バージョニング

このプロジェクトは **Calendar Versioning (CalVer)** を採用しています。

- フォーマット: `YYYY.MM.PATCH`
- 例: `2025.10.0` (2025年10月の最初のリリース)

## ライセンス

MIT License

## 関連リポジトリ

- **KdxProjects**: コアライブラリNuGetパッケージ群
  - https://github.com/KANAMORI-SYSTEM-Inc/KdxProjects

## 貢献

プルリクエストは歓迎します。大きな変更の場合は、まずissueを開いて変更内容について議論してください。

---

**作成日**: 2025-10-02
**ステータス**: Active Development
