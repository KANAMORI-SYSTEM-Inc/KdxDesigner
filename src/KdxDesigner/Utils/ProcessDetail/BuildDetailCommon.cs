using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.ProcessDetail
{
    /// <summary>
    /// ProcessDetail固有のビルドロジックを提供するクラス
    /// IBuildDetailを継承し、必要に応じてメソッドをオーバーライド可能
    /// </summary>
    internal class BuildDetailCommon : IBuildDetail
    {
        /// <summary>
        /// BuildDetailCommon のインスタンスを初期化します
        /// </summary>
        /// <param name="mainViewModel">MainViewからの初期値</param>
        /// <param name="ioAddressService">IO検索用のサービス</param>
        /// <param name="errorAggregator">エラー出力用のサービス</param>
        /// <param name="repository">ACCESSファイル検索用のリポジトリ</param>
        /// <param name="processes">工程の一覧</param>
        /// <param name="details">工程詳細の一覧</param>
        /// <param name="operations">操作の一覧</param>
        /// <param name="cylinders">CYの一覧</param>
        /// <param name="ioList">IOの一覧</param>
        public BuildDetailCommon(
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            ISupabaseRepository repository,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
            : base(mainViewModel, ioAddressService, errorAggregator, repository,
                   processes, details, operations, cylinders, ioList)
        {
        }
    }
}
