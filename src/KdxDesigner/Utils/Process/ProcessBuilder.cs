using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Utils.Process;

namespace KdxDesigner.Utils
{
    public static class ProcessBuilder
    {
        public static async Task<List<LadderCsvRow>> GenerateAllLadderCsvRows(
            Cycle selectedCycle,
            int processStartDevice,
            int detailStartDevice,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<IO> ioList,
            ISupabaseRepository repository)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();             // ニモニック配列を格納するリスト
            processes = processes.OrderBy(p => p.Process.SortNumber).ToList(); // 工程をカテゴリIDでソート

            // プロセスのニモニックを生成
            allRows = await GenerateCsvRowsProcess(processes, details, repository);
            // プロセス詳細のニモニックを生成
            return allRows;
        }

        public static async Task<List<LadderCsvRow>> GenerateCsvRowsProcess(
            List<MnemonicDeviceWithProcess> list,
            List<MnemonicDeviceWithProcessDetail> details,
            ISupabaseRepository repository)
        {
            var mnemonic = new List<LadderCsvRow>();

            // BuildProcessCommon のインスタンスを作成
            var buildProcess = new BuildProcessCommon(repository, details);

            foreach (var pros in list)
            {
                switch (pros.Process.ProcessCategoryId)
                {
                    case 1:     // 通常工程
                        mnemonic.AddRange(await buildProcess.BuildNormal(pros));
                        break;
                    case 2:     // Single
                        mnemonic.AddRange(await buildProcess.BuildNormal(pros));
                        break;
                    case 3:     // リセット後工程 #issue16
                        mnemonic.AddRange(await buildProcess.BuildResetAfter(pros));
                        break;
                    case 4:     // センサON確認 #issue17
                        mnemonic.AddRange(await buildProcess.BuildIL(pros));
                        break;
                    case 5:     // リセット
                        mnemonic.AddRange(await buildProcess.BuildReset(pros));
                        break;
                    default:
                        break;
                }
            }

            // テスト実行釦のリセットサブルーチン
            mnemonic.AddRange(buildProcess.BuildSubRoutine(list));

            return mnemonic;
        }
    }
}
