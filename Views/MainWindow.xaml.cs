using PureMediaPlayer.ViewModels;
using System.Windows;

namespace PureMediaPlayer.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainPlayerViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Closed += (_, _) => _viewModel.Dispose();
        }

        //private void InitializeComponent()
        //{
        //    throw new NotImplementedException();
        //}
    }
}