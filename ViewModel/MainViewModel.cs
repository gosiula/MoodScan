using System.Windows.Input;
using MoodScan.Service;

namespace MoodScan.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentChildView;
        private readonly CameraService _cameraService;
        private readonly FaceDetectionService _faceDetectionService;
        private readonly EmotionRecognitionService _emotionRecognitionService;
        private readonly ScreenRecordingService _screenRecordingService;
        private readonly VideoLoaderService _videoLoaderService;
        private readonly EmotionLoggingService _emotionLoggingService;
        private readonly DownloadService _downloadService;
        private CameraViewModel _cameraViewModel;

        private bool _isMenuVisible;
        public bool IsMenuVisible
        {
            get => _isMenuVisible;
            set
            {
                _isMenuVisible = value;
                OnPropertyChanged(nameof(IsMenuVisible));
            }
        }

        private bool _isCameraChecked;
        public bool IsCameraChecked
        {
            get => _isCameraChecked;
            set
            {
                _isCameraChecked = value;
                OnPropertyChanged(nameof(IsCameraChecked));
            }
        }

        private bool _isDownloadChecked;
        public bool IsDownloadChecked
        {
            get => _isDownloadChecked;
            set
            {
                _isDownloadChecked = value;
                OnPropertyChanged(nameof(IsDownloadChecked));
            }
        }

        private bool _isScrollEnabled = true;
        public bool IsScrollEnabled
        {
            get => _isScrollEnabled;
            set
            {
                _isScrollEnabled = value;
                OnPropertyChanged(nameof(IsScrollEnabled));
            }
        }

        public BaseViewModel CurrentChildView
        {
            get
            {
                return _currentChildView;
            }

            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }

        public ICommand ShowCameraViewCommand { get; }
        public ICommand ShowVideoCameraViewCommand { get; }
        public ICommand ShowDownloadViewCommand { get; }
        public ICommand ShowStartViewCommand { get; }

        public MainViewModel()
        {
            _cameraService = new CameraService();
            _faceDetectionService = new FaceDetectionService("Resources/haarcascade_frontalface_default.xml");
            _emotionRecognitionService = new EmotionRecognitionService(System.IO.Path.GetFullPath(@"Resources\model.onnx"));
            _screenRecordingService = new ScreenRecordingService();
            _videoLoaderService = new VideoLoaderService();
            _emotionLoggingService = new EmotionLoggingService();
            _downloadService = new DownloadService();

            ShowStartViewCommand = new CommandViewModel(ExecuteShowStartViewCommand);
            ShowCameraViewCommand = new CommandViewModel(ExecuteShowCameraViewCommand);
            ShowVideoCameraViewCommand = new CommandViewModel(ExecuteShowVideoCameraViewCommand);
            ShowDownloadViewCommand = new CommandViewModel(ExecuteShowDownloadViewCommand);

            ExecuteShowStartViewCommand(null);
        }

        private void ExecuteShowStartViewCommand(object obj)
        {
            if (CurrentChildView is CameraViewModel camVm)
            {
                camVm.StopCamera();
            }

            CurrentChildView = new StartViewModel(this);
            IsMenuVisible = false;
            IsScrollEnabled = true;
        }

        private void ExecuteShowCameraViewCommand(object obj)
        {
            _cameraViewModel = new CameraViewModel(this, _cameraService, _faceDetectionService, _emotionRecognitionService);
            CurrentChildView = _cameraViewModel;
            IsMenuVisible = true;
            IsScrollEnabled = true;

            IsCameraChecked = true;
            IsDownloadChecked = false;
        }

        private void ExecuteShowVideoCameraViewCommand(object obj)
        {
            if (_cameraViewModel != null)
            {
                CurrentChildView = new VideoCameraViewModel(this, _cameraViewModel, _screenRecordingService, _emotionLoggingService, _videoLoaderService);
            }
            IsMenuVisible = false;
            IsScrollEnabled = true;
        }

        private void ExecuteShowDownloadViewCommand(object obj)
        {
            if (CurrentChildView is CameraViewModel camVm)
            {
                camVm.StopCamera();
            }

            CurrentChildView = new DownloadViewModel(this, _downloadService);
            IsMenuVisible = true;
            IsScrollEnabled = true;

            IsCameraChecked = false;
            IsDownloadChecked = true;
        }
    }
}
