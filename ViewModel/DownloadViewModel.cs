using System.Windows.Input;
using MoodScan.Service;

namespace MoodScan.ViewModel
{
    public class DownloadViewModel : BaseViewModel
    {
        private MainViewModel _mainViewModel;
        private readonly DownloadService _downloadService;

        // Popup
        private bool _isDownloadPopupOpen;
        public bool IsDownloadPopupOpen
        {
            get => _isDownloadPopupOpen;
            set
            {
                _isDownloadPopupOpen = value;
                OnPropertyChanged(nameof(IsDownloadPopupOpen));
            }
        }

        // Popup Message
        private string _downloadMessage;
        public string DownloadMessage
        {
            get => _downloadMessage;
            set
            {
                _downloadMessage = value;
                OnPropertyChanged(nameof(DownloadMessage));
            }
        }

        // Download status
        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
                OnPropertyChanged(nameof(IsDownloadButtonEnabled));
            }
        }

        public bool IsDownloadButtonEnabled => !IsDownloading;

        // Commands
        public ICommand SaveFilesCommand { get; }
        public ICommand CloseDownloadPopupCommand { get; }

        public DownloadViewModel(MainViewModel mainViewModel, DownloadService downloadService)
        {
            _mainViewModel = mainViewModel;
            _downloadService = downloadService;

            SaveFilesCommand = new CommandViewModel(ExecuteSaveFilesCommand);
            CloseDownloadPopupCommand = new CommandViewModel(ExecuteCloseDownloadPopupCommand);
        }

        // Method to to save all files to .zip
        private async void ExecuteSaveFilesCommand(object obj)
        {
            if (IsDownloading) return;

            IsDownloading = true;

            try
            {
                await Task.Run(() => _downloadService.SaveAllFilesAsZip());

                DownloadMessage = "Pliki zostały pobrane!";
                IsDownloadPopupOpen = true;
            }
            catch (Exception ex)
            {
                DownloadMessage = "Uwaga! Błąd pobierania plików";
                IsDownloadPopupOpen = true;
            }
            finally
            {
                IsDownloading = false;
            }
        }

        // Method to close the popup
        private void ExecuteCloseDownloadPopupCommand(object obj)
        {
            IsDownloadPopupOpen = false;
        }
    }
}