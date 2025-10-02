# 概要
KdxDesignerはKANAMORI SYSTEM Inc.におけるラダープログラム自動作成の為のWindowsアプリケーションです。  
https://amethyst-lobster-62f.notion.site/30f7ce7ff0574570900d3bfba87e9d46?v=14111e15871c46fd89ddcb6bebf189c0&pvs=74
- C#
- .NET 8.0  
- WPF  
- MVVMパターンで開発  
## 機能
- Microsoft Accessファイルのインポート
- CSVニモニックファイルの出力

# ディレクトリについて
## Data
AccessRepository.csファイル内にAccessファイルへのクエリが記述されています。  
このクラスを利用して、ACCESSの情報をコード内で利用できます。
## Models
Accessファイルのテーブル構造を記述しています。
## Utils
再利用可能なコードロジックを保存します。
## ViewModels
Viewとコードロジック間の受け渡し用コードを保存しています。
## Views
WPFの.xamlファイルとViewロジックが格納されています。  
アプリ起動時はMainView.xamlが呼び出され、各Viewに移動できるようになっています。
# 開発環境
Visual Studio 2022  
.NET 8.0のSDKが必要なので、下記リンクから取得  
https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0  
### NuGet
- Dapper Version 2.1.66
- Dapper.SqlBuilder Version 2.1.66
- CommunityToolkit.Mvvm 8.4.0
- System.Data.OleDb 9.0.4

### GitHubの運用について
下記Notion頁参照のこと  
https://www.notion.so/GitHub-1f9e3206de34809ea273c6f36ebdab84
