using System.Windows;
using KdxDesigner.Models;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.Views
{
    public partial class TimerEditDialog : Window
    {
        public TimerEditDialog(MnemonicTimerDevice device)
        {
            InitializeComponent();
        }
    }
}