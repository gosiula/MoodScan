using System.Windows.Input;

namespace MoodScan.ViewModel
{
    public class StartViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public ICommand StartCommand { get; }

        public StartViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            StartCommand = new CommandViewModel(ExecuteStartCommand);
        }

        // Start button logic - switch to camera view
        private void ExecuteStartCommand(object obj)
        {
            _mainViewModel.ShowCameraViewCommand.Execute(null);
        }
    }
}
