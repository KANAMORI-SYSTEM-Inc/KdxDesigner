using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.ViewModels;
using Kdx.Contracts.Interfaces;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.Utils.ProcessDetail
{
    public class ProcessDetailBuilder
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly IAccessRepository _repository;

        public ProcessDetailBuilder(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService, IAccessRepository repository)
        {
            _mainViewModel = mainViewModel; // MainViewModelのインスタンスを取得
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _repository = repository;
        }

        public List<LadderCsvRow> GenerateAllLadderCsvRows(
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            LadderCsvRow.ResetKeyCounter();
            var allRows = new List<LadderCsvRow>();

            // 1. BuildDetail のインスタンス化を修正
            //    processes もコンストラクタに渡すようにする
            BuildDetail buildDetail = new(
                _mainViewModel,
                _ioAddressService,
                _errorAggregator,
                _repository,
                processes,
                details,
                operations,
                cylinders,
                ioList
            );
            foreach (var detail in details)
            {
                // DetailUnitBuilder を使うようにリファクタリングされた
                // BuildDetail の各メソッドを呼び出す。
                // 呼び出し側の見た目は同じだが、内部実装がクリーンになっている。
                switch (detail.Detail.CategoryId)
                {
                    case 1:  // 通常工程
                        allRows.AddRange(buildDetail.Normal(detail));
                        break;
                    case 2:  // 工程まとめ
                        allRows.AddRange(buildDetail.Summarize(detail));
                        break;
                    case 3:  // センサON確認
                        allRows.AddRange(buildDetail.SensorON(detail));
                        break;
                    case 4:     // センサOFF確認
                        allRows.AddRange(buildDetail.SensorOFF(detail));
                        break;
                    case 5:     // 工程分岐
                        allRows.AddRange(buildDetail.Branch(detail));
                        break;
                    case 6:     // 工程合流
                        allRows.AddRange(buildDetail.Merge(detail));
                        break;
                    case 7:     // サーボ座標指定
                        break;
                    case 8:     // サーボ番号指定
                        break;
                    case 9:     // INV座標指定
                        break;
                    case 10:    // IL待ち
                        allRows.AddRange(buildDetail.ILWait(detail));
                        break;
                    case 11:    // リセット工程開始
                        break;
                    case 12:    // リセット工程完了
                        break;
                    case 13:    // 工程OFF確認
                        allRows.AddRange(buildDetail.ProcessOFF(detail));
                        break;
                    case 15:    // 期間工程
                        allRows.AddRange(buildDetail.Season(detail));
                        break;
                    case 16:    // タイマ工程
                        allRows.AddRange(buildDetail.TimerProcess(detail, detailTimers));
                        break;                    
                    case 17:    // タイマ
                        allRows.AddRange(buildDetail.Timer(detail, detailTimers));
                        break;
                    case 18:    // 複数工程
                        allRows.AddRange(buildDetail.Module(detail));
                        break;
                    default:
                        break;
                }
            }
            // プロセス詳細のニモニックを生成
            return allRows;
        }

    }
}
