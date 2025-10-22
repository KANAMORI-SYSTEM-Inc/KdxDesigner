using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Utils.Process
{
    /// <summary>
    /// Process固有のビルドロジックを提供するクラス
    /// IBuildProcessを継承し、必要に応じてメソッドをオーバーライド可能
    /// </summary>
    internal class BuildProcessCommon : IBuildProcess
    {
        /// <summary>
        /// BuildProcessCommon のインスタンスを初期化します
        /// </summary>
        /// <param name="repository">Supabaseリポジトリ</param>
        /// <param name="details">工程詳細のリスト</param>
        public BuildProcessCommon(
            ISupabaseRepository repository,
            List<MnemonicDeviceWithProcessDetail> details)
            : base(repository, details)
        {
        }
    }
}
