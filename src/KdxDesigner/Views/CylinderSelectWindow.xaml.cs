using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Views
{
    public partial class CylinderSelectWindow : Window
    {
        public Cylinder? SelectedCylinder { get; private set; }
        private List<Cylinder> _cylinders;

        public CylinderSelectWindow(ISupabaseRepository repository, int plcId)
        {
            InitializeComponent();

            // Load cylinders
            var cylinder = repository.GetCyListAsync(plcId).Result;
            _cylinders = cylinder.ToList();
            CylinderDataGrid.ItemsSource = _cylinders;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCylinder = CylinderDataGrid.SelectedItem as Cylinder;
            if (SelectedCylinder != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("シリンダーを選択してください。", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CylinderDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CylinderDataGrid.SelectedItem != null)
            {
                OKButton_Click(sender, e);
            }
        }
    }
}