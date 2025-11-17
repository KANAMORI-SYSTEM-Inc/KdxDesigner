using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Services.ErrorService
{
    /// <summary>
    /// エラー情報のデータ操作を行うサービス実装
    /// </summary>
    internal class ErrorService : IErrorService
    {
        private readonly ISupabaseRepository _repository;

        public ErrorService(ISupabaseRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task DeleteErrorTable()
        {
            await _repository.DeleteErrorTableAsync();
        }

        public async Task<List<Kdx.Contracts.DTOs.ProcessError>> GetErrors(int plcId, int cycleId, int mnemonicId)
        {
            return await _repository.GetErrorsAsync(plcId, cycleId, mnemonicId);
        }

        // Operationのリストを受け取り、Errorテーブルに保存する
        public async Task<int> SaveMnemonicDeviceOperation(
            List<Operation> operations,
            List<IO> iOs,
            int startNum,
            int startNumTimer,
            int plcId,
            int cycleId,
            int startErrorNum = 0)
        {
            // MnemonicDeviceテーブルの既存データを取得
            // 注: 複数Cycleを処理する際の重複を防ぐため、現在のCycleIdのエラーのみを取得
            // （最初のCycleの前にエラーテーブル全体が削除されているため、
            //  現在のCycleIdのエラーのみが存在するはず）
            var allExisting = await GetErrors(plcId, cycleId, (int)MnemonicType.Operation);
            var messages = await _repository.GetErrorMessagesAsync((int)MnemonicType.Operation);

            // 全Cycle通して一意なエラー番号を生成するため、開始値を受け取る
            int alarmCount = startErrorNum;
            foreach (Operation operation in operations)
            {
                if (operation == null) continue;
                var existing = allExisting.FirstOrDefault(m => m.RecordId == operation.Id);
                var category = operation.CategoryId;

                List<int> AlarmIds = new();
                switch (category)
                {
                    case 2 or 29 or 30: // 保持
                        AlarmIds.AddRange([1, 2, 5]);
                        break;
                    case 3 or 9 or 15 or 27: // 速度制御INV1
                        AlarmIds.AddRange([1, 2, 3, 4, 5]);
                        break;
                    case 4 or 10 or 16 or 28: // 速度制御INV2
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 5]);
                        break;
                    case 5 or 11 or 17:     // 速度制御INV3
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 6 or 12 or 18: // 速度制御INV4
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 7 or 13 or 19: // 速度制御INV5
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 20:            // バネ
                        AlarmIds.AddRange([5]);
                        break;
                    case 31:            // サーボ
                        break;
                    default:
                        break;
                }

                List<ProcessError> insertErrors = new();
                List<ProcessError> updateErrors = new();
                List<Memory> insertMemoriesM = new();
                List<Memory> insertMemoriesT = new();

                foreach (int id in AlarmIds)
                {
                    string device = "M" + (startNum + alarmCount).ToString();
                    string timerDevice = "T" + (startNumTimer + alarmCount).ToString();

                    string comment = messages.FirstOrDefault(m => m.AlarmId == id)?.BaseMessage ?? string.Empty;
                    string alarm = messages.FirstOrDefault(m => m.AlarmId == id)?.BaseAlarm ?? string.Empty;
                    int count = messages.FirstOrDefault(m => m.AlarmId == id)?.DefaultCountTime ?? 1000;

                    var comment2 = operation.Valve1 + operation.GoBack;
                    var comment3 = messages.FirstOrDefault(m => m.AlarmId == id)?.Category2 ?? string.Empty;
                    var comment4 = messages.FirstOrDefault(m => m.AlarmId == id)?.Category3 ?? string.Empty;

                    ProcessError saveError = new()
                    {
                        PlcId = plcId,
                        CycleId = cycleId,
                        Device = device,
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        AlarmId = id,
                        ErrorNum = alarmCount,
                        Comment1 = "操作ｴﾗｰ",
                        Comment2 = comment2,
                        Comment3 = comment3,
                        Comment4 = comment4,
                        AlarmComment = alarm,
                        MessageComment = comment,
                        ErrorTime = count,
                        ErrorTimeDevice = timerDevice
                    };

                    Memory memoryM = new()
                    {
                        PlcId = plcId,
                        Device = device,
                        MemoryCategory = 2,     // M
                        DeviceNumber = startNum + alarmCount,
                        DeviceNumber1 = (startNum + alarmCount).ToString(),
                        DeviceNumber2 = "",
                        Category = "操作ｴﾗｰ",
                        Row_1 = "操作ｴﾗｰ",
                        Row_2 = comment2,
                        Row_3 = comment3,
                        Row_4 = comment4,
                        Direct_Input = "",
                        Confirm = "",
                        Note = "",
                        GOT = "false",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        OutcoilNumber = 0
                    };

                    Memory memoryT = new()
                    {
                        PlcId = plcId,
                        Device = device,
                        MemoryCategory = 7,     // M
                        DeviceNumber = startNum + alarmCount,
                        DeviceNumber1 = (startNum + alarmCount).ToString(),
                        DeviceNumber2 = "",
                        Category = "操作ｴﾗｰ",
                        Row_1 = "操作ｴﾗｰ",
                        Row_2 = comment2,
                        Row_3 = comment3,
                        Row_4 = comment4,
                        Direct_Input = "",
                        Confirm = "",
                        Note = "",
                        GOT = "false",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        OutcoilNumber = 0
                    };

                    insertMemoriesM.Add(memoryM);
                    insertMemoriesT.Add(memoryT);

                    if (existing != null)
                    {
                        updateErrors.Add(saveError);
                    }
                    else
                    {
                        insertErrors.Add(saveError);
                    }

                    alarmCount++;
                }

                await _repository.SaveErrorsAsync(insertErrors);
                await _repository.SaveOrUpdateMemoriesBatchAsync(insertMemoriesM);
                await _repository.SaveOrUpdateMemoriesBatchAsync(insertMemoriesT);
                await _repository.UpdateErrorsAsync(updateErrors);
            }

            // 次のCycleで使用するために、累積エラー番号を返す
            return alarmCount;
        }
    }
}
