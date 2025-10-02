using Kdx.Core.Application;
using Kdx.Core.Domain.Services;
using Kdx.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel
    {
        private SaveProcessDetailTimerDevicesUseCase? _saveTimerDevicesUseCase;

        public void InitializeUseCases(
            SaveProcessDetailTimerDevicesUseCase? saveTimerDevicesUseCase = null)
        {
            _saveTimerDevicesUseCase = saveTimerDevicesUseCase;
        }

        public async Task SaveTimerDevicesAsync()
        {
            if (_saveTimerDevicesUseCase == null)
            {
                // フォールバック: 既存の同期メソッドを使用
                SaveTimerDevicesLegacy();
                return;
            }

            var timer = _repository!.GetTimers();
            var details = _repository.GetProcessDetails();
            
            // 新しいユースケースを使用
            await _saveTimerDevicesUseCase.ExecuteAsync(
                timer, 
                details, 
                SelectedPlc!.Id);
        }

        /// <summary>
        /// 
        /// </summary>

        private void SaveTimerDevicesLegacy()
        {
            var timer = _repository!.GetTimers();
            var details = _repository.GetProcessDetails();
            var operations = _repository.GetOperations();
            var cylinders = _repository.GetCYs();

            int timerCount = 0;
            
            // 既存の処理をそのまま呼び出す
            _repository.DeleteAllMnemonicTimerDevices();
            _timerService!.SaveWithDetail(timer, details, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService!.SaveWithOperation(timer, operations, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService!.SaveWithCY(timer, cylinders, DeviceStartT, SelectedPlc!.Id, ref timerCount);
        }
    }
}
