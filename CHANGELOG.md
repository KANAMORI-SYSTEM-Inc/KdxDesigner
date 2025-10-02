# Changelog

このファイルは、KdxDesignerの注目すべき変更をすべて記録します。

フォーマットは [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Calendar Versioning](https://calver.org/) に準拠しています。

## [Unreleased]

## [2025.10.0] - 2025-10-02

### Added
- 初回リリース
- WPFデスクトップアプリケーション
- プロセスフロー図作成機能
- シリンダー制御設定機能
- インターロック条件設定機能
- タイマー設定機能
- メモリ管理機能
- Supabaseデータベース連携

### Changed
- モノリポから独立したアプリケーションリポジトリに分離
- プロジェクト参照からNuGetパッケージ参照に変更
  - KdxProjects v1.0.0 パッケージ群を使用

### Technical
- .NET 8.0対応
- MVVM パターン実装（CommunityToolkit.Mvvm使用）
- 依存性注入（Microsoft.Extensions.DependencyInjection）
- Supabase接続管理
