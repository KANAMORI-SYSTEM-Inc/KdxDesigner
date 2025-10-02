using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Services
{
    public class ConnectionHelper
    {
        public static string GetConnectionString()
        {
            // 実行フォルダを基準に appsettings.json を探す
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true) // 開発用はGit除外
                .AddEnvironmentVariables()
                .Build();

            return config.GetConnectionString("Default")
                   ?? throw new InvalidOperationException("接続文字列 'Default' が見つかりません");
        }
    }
}
