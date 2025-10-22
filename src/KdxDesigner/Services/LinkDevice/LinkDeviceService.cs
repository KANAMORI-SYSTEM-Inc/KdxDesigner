using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Utils;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Services.LinkDevice
{
    /// <summary>
    /// リンクデバイスのデータ操作を行うサービス実装
    /// </summary>
    public class LinkDeviceService : ILinkDeviceService
    {
        private readonly ISupabaseRepository _repository;
        // エラー集約サービスもコンストラクタで受け取ることを推奨
        // private readonly IErrorAggregator _errorAggregator;

        public LinkDeviceService(ISupabaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> CreateLinkDeviceRecords(PLC mainPlc, List<PlcLinkSettingViewModel> selectedSettings)
        {
            var allIoData = await _repository.GetIoListAsync();
            var ioRecordsToUpdate = new List<IO>();

            foreach (var setting in selectedSettings)
            {
                var subordinateIoList = allIoData
                    .Where(io => io.PlcId == setting.Plc.Id && io.Address != null && !io.Address.StartsWith("F"))
                    .ToList();

                // Xデバイスの処理
                ProcessDeviceType(subordinateIoList, "X", setting.XDeviceStart, ioRecordsToUpdate);

                // Yデバイスの処理
                ProcessDeviceType(subordinateIoList, "Y", setting.YDeviceStart, ioRecordsToUpdate);
            }

            if (ioRecordsToUpdate.Any())
            {
                await _repository.UpdateIoLinkDevicesAsync(ioRecordsToUpdate);
            }

            // TODO: Memoryテーブルへの転送ロジック

            return true;
        }

        /// <summary>
        /// 特定のデバイス種別（XまたはY）のリンク処理を実行します。
        /// </summary>
        private void ProcessDeviceType(List<IO> subordinateIoList, string devicePrefix, string? linkStartAddress, List<IO> masterUpdateList)
        {
            if (string.IsNullOrEmpty(linkStartAddress)) return;

            // 1. 対象のデバイスを抽出し、アドレスで並び替える（連番の基準にするため重要）
            var devicesToProcess = subordinateIoList
                .Where(io => io.Address!.StartsWith(devicePrefix))
                .OrderBy(io => io.Address)
                .ToList();

            if (!devicesToProcess.Any()) return;

            // 2. メインPLC側のリンク開始アドレスを数値に変換
            if (!LinkDeviceCalculator.TryParseLinkAddress(linkStartAddress, out string mainPrefix, out long mainStartOffsetValue))
            {
                // TODO: エラー処理（例: _errorAggregator.AddError(...)）
                return;
            }

            // 3. ソート済みリストの順番（インデックス）をオフセットとして利用し、連番を生成
            for (int i = 0; i < devicesToProcess.Count; i++)
            {
                var currentIoDevice = devicesToProcess[i];

                // オフセット = リスト内でのインデックス（順番）
                long relativeOffset = i;

                // 最終的なリンク先アドレスのオフセット値を計算
                long finalLinkOffsetValue = mainStartOffsetValue + relativeOffset;

                // 計算結果を "Wxxxx.F" のような文字列にフォーマット
                string calculatedLinkDevice = LinkDeviceCalculator.FormatLinkAddress(mainPrefix, finalLinkOffsetValue);

                // IOオブジェクトのLinkDeviceプロパティを更新し、更新リストに追加
                currentIoDevice.LinkDevice = calculatedLinkDevice;
                masterUpdateList.Add(currentIoDevice);
            }
        }

        public async Task ExportLinkDeviceCsv(string filePath)
        {
            // 1. IOテーブルからLinkDeviceが設定されているデータを取得
            var allIo = await _repository.GetIoListAsync();
            var linkedIoList = allIo.Where(io => !string.IsNullOrWhiteSpace(io.LinkDevice)).ToList();

            if (!linkedIoList.Any())
            {
                throw new InvalidOperationException("出力対象のリンクデバイスが見つかりません。");
            }

            // 2. LinkDeviceを16進数として解釈し、正しくソート
            var sortedIo = linkedIoList
                .Select(io =>
                {
                    // ソートキーとしてアドレスの数値表現を取得
                    LinkDeviceCalculator.TryParseLinkAddress(io.LinkDevice, out _, out long sortKey);
                    return new { IO = io, SortKey = sortKey };
                })
                .OrderBy(item => item.SortKey)
                .Select(item => item.IO)
                .ToList();

            // 3. CSVに出力するデータ形式に変換（空行挿入ロジックを含む）
            var csvData = new List<string[]>();
            csvData.Add(new[] { "LinkDevice", "Comment" }); // ヘッダー行

            string? previousWordPart = null;

            foreach (var io in sortedIo)
            {
                string linkDevice = io.LinkDevice!;

                // 現在のワード部を取得 (例: "W01B5.F" -> "W01B5")
                string currentWordPart = linkDevice.Split('.')[0];

                // ★ ワード部が前回と変わったタイミングで空行を挿入
                if (previousWordPart != null && currentWordPart != previousWordPart)
                {
                    csvData.Add(new string[0]); // 空の配列を空行とする
                }

                // XかYかでコメントを切り替える
                string comment = io.Address?.StartsWith("X") == true ? io.XComment ?? "" : io.YComment ?? "";

                csvData.Add(new[] { linkDevice, comment });

                previousWordPart = currentWordPart;
            }

            // 4. 汎用エクスポーターを呼び出してファイルに書き込み
            CsvExporter.Export(filePath, csvData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plc"></param>
        /// <returns></returns>
        public async Task<List<LadderCsvRow>> CreateLadderCsvRows(PLC plc)
        {
            // 1. PLCに紐づくLinkDeviceが設定されているIOを取得
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var allIo = await _repository.GetIoListAsync();
            var linkedIoList = allIo.Where(io => !string.IsNullOrWhiteSpace(io.LinkDevice) && io.PlcId == plc.Id).ToList();
            string? previousWordPart = null;

            // 2. LinkDeviceを16進数として解釈し、正しくソート
            var sortedIo = linkedIoList
                .Select(io =>
                {
                    // ソートキーとしてアドレスの数値表現を取得
                    LinkDeviceCalculator.TryParseLinkAddress(io.LinkDevice, out _, out long sortKey);
                    return new { IO = io, SortKey = sortKey };
                })
                .OrderBy(item => item.SortKey)
                .Select(item => item.IO)
                .ToList();

            foreach (var io in sortedIo)
            {
                string linkDevice = io.LinkDevice!;
                // 現在のワード部を取得 (例: "W01B5.F" -> "W01B5")
                string currentWordPart = linkDevice.Split('.')[0];


                if (io.LinkDevice == null || io.Address == null)
                {
                    // LinkDeviceがnullの場合はスキップ
                    continue;
                }

                // ★ ワード部が前回と変わったタイミングで空行を挿入
                if (previousWordPart != null && currentWordPart != previousWordPart)
                {
                    result.Add(LadderRow.AddStatement(currentWordPart));
                }

                if (io.Address.StartsWith("X"))
                {
                    result.Add(LadderRow.AddLD(io.Address));
                    result.Add(LadderRow.AddOUT(io.LinkDevice));
                }
                else if (io.Address.StartsWith("Y"))
                {
                    result.Add(LadderRow.AddLD(io.LinkDevice));
                    result.Add(LadderRow.AddOUT(io.Address));
                }
                previousWordPart = currentWordPart;

            }
            return result;
        }
    }
}
