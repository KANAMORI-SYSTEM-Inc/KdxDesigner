using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;
using Kdx.Contracts.Interfaces;
using System.Windows;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class ProcessBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            Cycle selectedCycle,
            int processStartDevice,
            int detailStartDevice,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<IO> ioList,
            IAccessRepository repository,
            out List<OutputError> errors)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();             // ニモニック配列を格納するリスト
            errors = new List<OutputError>();                   // エラーリストの初期化

            processes = processes.OrderBy(p => p.Process.SortNumber).ToList(); // 工程をカテゴリIDでソート

            // プロセスのニモニックを生成
            allRows = GenerateCsvRowsProcess(processes, details, repository);
            // プロセス詳細のニモニックを生成
            return allRows;
        }

        public static List<LadderCsvRow> GenerateCsvRowsProcess(
            List<MnemonicDeviceWithProcess> list, 
            List<MnemonicDeviceWithProcessDetail> details,
            IAccessRepository repository)
        {
            var mnemonic = new List<LadderCsvRow>();

            foreach (var pros in list)
            {
                switch(pros.Process.ProcessCategoryId)
                {
                    case 1:     // 通常工程
                        mnemonic.AddRange(BuildProcess.BuildNormal(repository, pros, details));
                        break;
                    case 2:     // Single
                        mnemonic.AddRange(BuildProcess.BuildNormal(repository, pros, details));
                        break;
                    case 3:     // リセット後工程 #issue16
                        mnemonic.AddRange(BuildProcess.BuildResetAfter(repository, pros, details));
                        break;
                    case 4:     // センサON確認 #issue17
                        mnemonic.AddRange(BuildProcess.BuildIL(repository, pros, details));
                        break;
                    case 5:     // リセット
                        mnemonic.AddRange(BuildProcess.BuildReset(repository, pros, details));
                        break;
                    default:
                        break;
                }
            }

            // テスト実行釦のリセットサブルーチン
            mnemonic.AddRange(BuildProcess.BuildSubRoutine(list));

            return mnemonic;
        }
    }
}
