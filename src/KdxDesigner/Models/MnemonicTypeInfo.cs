namespace KdxDesigner.Models
{
    /// <summary>
    /// MnemonicIdの種類を表す
    /// </summary>
    public class MnemonicTypeInfo
    {
        public int Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public static List<MnemonicTypeInfo> GetAll()
        {
            return new List<MnemonicTypeInfo>
            {
                new MnemonicTypeInfo { Id = 0, TableName = "未設定", Description = "MnemonicTypeが未設定のタイマー" },
                new MnemonicTypeInfo { Id = 1, TableName = "Process", Description = "工程：工程詳細のまとまり（ブロック）" },
                new MnemonicTypeInfo { Id = 2, TableName = "ProcessDetail", Description = "工程詳細：アクチュエータの動作タイミング" },
                new MnemonicTypeInfo { Id = 3, TableName = "Operation", Description = "操作：アクチュエータが要求する動作" },
                new MnemonicTypeInfo { Id = 4, TableName = "Cylinder", Description = "出力：アクチュエータに指令" }
            };
        }
    }
}
