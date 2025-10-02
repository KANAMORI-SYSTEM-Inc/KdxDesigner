using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KdxDesigner.Utils
{
    public static class CsvExporter
    {
        public static void Export(string filePath, IEnumerable<string[]> data)
        {
            var sb = new StringBuilder();
            foreach (var row in data)
            {
                // 空の配列は空行として扱う
                if (row.Length == 0)
                {
                    sb.AppendLine();
                    continue;
                }

                // 各フィールドをダブルクォートで囲み、内部のダブルクォートは2つにエスケープ
                var escapedRow = row.Select(field =>
                    $"\"{field?.Replace("\"", "\"\"") ?? ""}\"");

                sb.AppendLine(string.Join(",", escapedRow));
            }

            // Shift-JISで保存（Excelで文字化けしないように）
            File.WriteAllText(filePath, sb.ToString(), Encoding.GetEncoding("shift_jis"));
        }
    }
}