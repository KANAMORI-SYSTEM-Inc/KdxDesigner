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
                // フォールバック: 既存の非同期メソッドを使用
                await SaveTimerDevicesLegacyAsync();
                return;
            }

            var timer = await _repository!.GetTimersAsync();
            var details = await _repository.GetProcessDetailsAsync();
            
            // 新しいユースケースを使用
            await _saveTimerDevicesUseCase.ExecuteAsync(
                timer, 
                details, 
                SelectedPlc!.Id);
        }

        /// <summary>
        /// 
        /// </summary>

        private async Task SaveTimerDevicesLegacyAsync()
        {
            var timer = await _repository!.GetTimersAsync();
            var details = await _repository.GetProcessDetailsAsync();
            var operations = await _repository.GetOperationsAsync();
            var cylinders = await _repository.GetCYsAsync();

            int timerCount = 0;

            // 既存の処理をそのまま呼び出す
            await _repository.DeleteAllMnemonicTimerDeviceAsync();
            timerCount = await _timerService!.SaveWithDetail(timer, details, DeviceStartT, SelectedPlc!.Id, timerCount);
            timerCount = await _timerService!.SaveWithOperation(timer, operations, DeviceStartT, SelectedPlc!.Id, timerCount);
            timerCount = await _timerService!.SaveWithCY(timer, cylinders, DeviceStartT, SelectedPlc!.Id, timerCount);
        }
    }
}
