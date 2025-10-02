using System.IO;
using System.Text;
using Kdx.Contracts.DTOs;


namespace KdxDesigner.Utils
{
    public static class LadderCsvExporter
    {
        public static void ExportLadderCsv(List<LadderCsvRow> rows, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.Unicode);

            // 固定ヘッダー3行
            writer.WriteLine("\"E5482L_220317\"");
            writer.WriteLine("\"機種情報\"\t\"RCPU R16N\"");
            writer.WriteLine("\"ステップ番号\"\t\"行間ステートメント\"\t\"命令\"\t\"I/Oデバイス\"\t\"空欄\"\t\"PIステートメント\"\t\"ノート\"");

            // データ行
            foreach (var row in rows)
            {
                var fields = new List<string>
                {
                    row.StepNo,
                    row.StepComment,
                    row.Command,
                    row.Address,
                    row.Blank1,
                    row.PiStatement,
                    row.Note
                };

                writer.WriteLine(string.Join("\t", fields.Select(f => string.IsNullOrEmpty(f) ? "\"\"" : f)));
            }

            // END
            writer.WriteLine("\"\"\t\"\"\t\"END\"\t\"\"\t\"\"\t\"\"\t\"\"");
        }
    }
}
