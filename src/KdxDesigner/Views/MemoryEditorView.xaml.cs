using KdxDesigner.Models;
using KdxDesigner.ViewModels;
using Kdx.Contracts.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KdxDesigner.Views
{
    /// <summary>
    /// MemoryEdit.xaml の相互作用ロジック
    /// </summary>
    public partial class MemoryEditorView : Window
    {
        public MemoryEditorView(int plcId, IAccessRepository repository)
        {
            InitializeComponent();
            DataContext = new MemoryEditorViewModel(plcId, repository);
        }


    }


}
