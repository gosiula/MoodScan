using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MoodScan.Service;
namespace MoodScan.ViewModel
{
    public class VideoCameraViewModel : BaseViewModel
    {
        // ViewModels and Services
        private readonly MainViewModel _mainViewModel;
        private readonly CameraViewModel _cameraViewModel;
        private readonly ScreenRecordingService _screenRecordingService;
        private readonly EmotionLoggingService _emotionLoggingService;
        private readonly VideoLoaderService _videoLoaderService;
        private readonly Dispatcher _ui = Application.Current.Dispatcher;

        // Prevent re-entrancy
        private int _emotionTickWorking = 0;

        // Per-video emotion events
        private readonly ConcurrentDictionary<string, List<(double Time, string Emotion)>> _perVideoEmotionEvents
            = new ConcurrentDictionary<string, List<(double Time, string Emotion)>>();

        // Countdown cancellation
        private CancellationTokenSource _countdownCts;

        // Pause timing lock
        private readonly object _pauseLock = new();

        // Total paused time
        private TimeSpan _pausedDuration = TimeSpan.Zero;

        // Pause start timestamp
        private DateTime? _pauseStartTime = null;

        // Emotion state lock
        private readonly object _emotionLock = new();

        // Countdown state lock
        private readonly object _countdownLock = new();

        // Face warining text
        public string FaceWarningMessage => _cameraViewModel.FaceWarningMessage;

        // Time left of the video
        private TimeSpan _timeLeft;

        // Try number for the files names
        private readonly int _tryNumber;

        // Current displayed video
        private Model.VideoModel _currentVideo;

        // Duration od the current video
        private int _durationSeconds;
        public int DurationSeconds
        {
            get => _durationSeconds;
            private set
            {
                _durationSeconds = value;
                OnPropertyChanged(nameof(DurationSeconds));
            }
        }

        // Bool for ending actions (stop logging emotions and screen recording)
        private bool _finalEndActionsExecuted = false;
        public bool FinalEndActionsExecuted
        {
            get => _finalEndActionsExecuted;
            set
            {
                _finalEndActionsExecuted = value;
                OnPropertyChanged(nameof(FinalEndActionsExecuted));
            }
        }

        // Camera image
        public BitmapImage CameraImage => _cameraViewModel.CameraImage;

        // Recording status
        public string RecordingStatus { get; private set; }

        // Last logged emotions
        private string _lastLoggedEmotion;

        // Last known VideoProgress 
        private double _lastKnownVideoProgress = 0;

        // Recording start time
        private DateTime recordingStartTime;

        // All user's emotions in the video
        private readonly List<string> allVideoEmotions = new();

        // Timer to dave emotions to .csv file
        private DispatcherTimer _emotionTimer;

        // Last emotion log time
        private DateTime _lastEmotionLogTime = DateTime.MinValue;

        // Bool - is screen recording on
        private bool _isRecording;

        // Bool - is media opened
        private bool _mediaIsOpened;
        public bool MediaIsOpened
        {
            get => _mediaIsOpened;
            set
            {
                _mediaIsOpened = value;
                OnPropertyChanged(nameof(MediaIsOpened));
            }
        }

        // Bool - is the view loading (video, timer, recording and emotion logging)
        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // Timer text
        private string _countdownText;
        public string CountdownText
        {
            get => _countdownText;
            private set
            {
                if (_countdownText != value)
                {
                    _countdownText = value;
                    OnPropertyChanged(nameof(CountdownText));
                }
            }
        }

        // Popup
        private bool _isQuitPopupOpen;
        public bool IsQuitPopupOpen
        {
            get => _isQuitPopupOpen;
            set
            {
                _isQuitPopupOpen = value;
                OnPropertyChanged(nameof(IsQuitPopupOpen));
            }
        }

        // End popup
        private bool _isOverlayVisible;
        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set
            {
                if (_isOverlayVisible != value)
                {
                    _isOverlayVisible = value;
                    OnPropertyChanged(nameof(IsOverlayVisible));
                }
            }
        }

        // Video
        private Uri _videoSource;
        public Uri VideoSource
        {
            get => _videoSource;
            set
            {
                if (_videoSource != value)
                {
                    _videoSource = value;
                    OnPropertyChanged(nameof(VideoSource));
                }
            }
        }

        // Bool - is the video playing
        private bool _isPlaying = true;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        // Video watch progress
        private double _videoProgress;
        public double VideoProgress
        {
            get => _videoProgress;
            set
            {
                if (_videoProgress != value)
                {
                    _videoProgress = value;
                    OnPropertyChanged(nameof(VideoProgress));
                    _lastKnownVideoProgress = value;
                }
            }
        }

        // Signal to start the video
        private bool _startVideoRequested;
        public bool StartVideoRequested
        {
            get => _startVideoRequested;
            private set
            {
                if (_startVideoRequested != value)
                {
                    _startVideoRequested = value;
                    OnPropertyChanged(nameof(StartVideoRequested));
                }
            }
        }

        // Actions
        public event Action RequestForceStopVideo;
        public event Action RequestVideoPlay;

        // Commands
        public ICommand QuitCommand { get; }
        public ICommand OpenQuitPopupCommand { get; }
        public ICommand CloseQuitPopupCommand { get; }
        public ICommand TogglePlayPauseCommand { get; }

        public VideoCameraViewModel(MainViewModel mainViewModel,
            CameraViewModel cameraViewModel,
            ScreenRecordingService screenRecordingService,
            EmotionLoggingService emotionLoggingService,
            VideoLoaderService videoLoaderService)
        {
            _mainViewModel = mainViewModel;
            _screenRecordingService = screenRecordingService;
            _emotionLoggingService = emotionLoggingService;
            _videoLoaderService = videoLoaderService;
            _cameraViewModel = cameraViewModel;
            _cameraViewModel.PropertyChanged += CameraViewModel_PropertyChanged;
            cameraViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(cameraViewModel.FaceWarningMessage))
                    OnPropertyChanged(nameof(FaceWarningMessage));
            };
            QuitCommand = new CommandViewModel(ExecuteQuitCommand);
            OpenQuitPopupCommand = new CommandViewModel(ExecuteOpenQuitPopupCommand);
            CloseQuitPopupCommand = new CommandViewModel(ExecuteCloseQuitPopupCommand);
            TogglePlayPauseCommand = new CommandViewModel(ExecuteTogglePlayPauseCommand);
            _tryNumber = GetNextTryNumber("Outputs");
            IsLoading = true;
            _ = InitializeAndStartEverythingAsync();
        }

        // Method 
        private double GetAdjustedElapsed()
        {
            var now = DateTime.Now;
            lock (_pauseLock)
            {
                var currentPause = _pauseStartTime.HasValue ? now - _pauseStartTime.Value : TimeSpan.Zero;
                return (now - recordingStartTime - _pausedDuration - currentPause).TotalSeconds;
            }
        }

        // Method to start screen recording and emotion logging
        public void StartRecordingAndLogging()
        {
            StartScreenRecording();
            _emotionLoggingService.StartLogging("Outputs", _tryNumber);
        }

        // Method to load and start the video
        private async Task InitializeAndStartEverythingAsync()
        {
            try
            {
                await LoadVideoMetadataAsync(); // Load the video

                await _ui.InvokeAsync(() =>
                {
                    VideoSource = new Uri(_currentVideo.FullPath);
                }, DispatcherPriority.Loaded);

                await WaitForMediaOpenedAsync(); // Wait for media to open

                await _ui.InvokeAsync(() =>
                {
                    IsLoading = false;
                    StartVideoRequested = true;
                });

                RequestVideoPlay?.Invoke(); // Request the start of the video
            }
            catch (Exception ex)
            {
                await _ui.InvokeAsync(() =>
                {
                    RecordingStatus = "Błąd startu: " + ex.Message;
                    OnPropertyChanged(nameof(RecordingStatus));
                    IsLoading = false;
                });
            }
        }

        // Method to load the video
        private async Task LoadVideoMetadataAsync()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(basePath, "Video", "VideoLabels.csv");
            if (!File.Exists(csvPath))
            {
                string devRoot = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\.."));
                csvPath = Path.Combine(devRoot, "Video", "VideoLabels.csv");
            }

            _currentVideo = await Task.Run(() =>
                _videoLoaderService.LoadSingleVideoFromCsv(csvPath));

            DurationSeconds = (int)Math.Round(_currentVideo.Length);
        }

        // Method to stop screen recording and emotion logging
        public void PerformFinalEndActions()
        {
            StopScreenRecording();

            double elapsed = GetAdjustedElapsed();
            string videoFile = Path.GetFileName(VideoSource?.LocalPath ?? _currentVideo?.FileName ?? "unknown.mp4");
            double videoElapsed = _currentVideo?.Length * (_lastKnownVideoProgress / 100.0) ?? _durationSeconds;
            if (videoElapsed < 0.5) videoElapsed = _durationSeconds;

            _emotionLoggingService.LogEnd(elapsed, videoFile, videoElapsed);
            _emotionLoggingService.StopLogging();
            StopEmotionTimer();
        }

        // Method to wait for the media to open
        private Task WaitForMediaOpenedAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            void Handler(object sender, RoutedEventArgs e)
            {
                MediaIsOpened = true;
                tcs.TrySetResult(true);

                if (Application.Current?.MainWindow?.FindName("VideoPlayer") is MediaElement player)
                {
                    player.MediaOpened -= Handler;
                }
            }

            _ui.Invoke(() =>
            {
                if (Application.Current?.MainWindow?.FindName("VideoPlayer") is MediaElement player)
                {
                    player.MediaOpened += Handler;
                }
                else
                {
                    Task.Delay(8000).ContinueWith(_ => tcs.TrySetResult(true));
                }
            });

            return tcs.Task;
        }

        // Method to update camera image
        private void CameraViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CameraViewModel.CameraImage))
            {
                OnPropertyChanged(nameof(CameraImage));
            }
        }

        // Command to quit
        private void ExecuteQuitCommand(object obj)
        {
            StopAllVideoCamera();
            IsQuitPopupOpen = false;
            _mainViewModel.ShowCameraViewCommand.Execute(null);
        }

        // Command to show quit popup
        private void ExecuteOpenQuitPopupCommand(object obj)
        {
            IsQuitPopupOpen = true;
        }

        // Command to close the quit popup
        private void ExecuteCloseQuitPopupCommand(object obj)
        {
            IsQuitPopupOpen = false;
        }

        // Method to start and stop the video
        private void ExecuteTogglePlayPauseCommand(object parameter)
        {
            if (parameter is MediaElement player)
            {
                Task.Run(() =>
                {
                    string videoFile = Path.GetFileName(VideoSource?.LocalPath ?? "");
                    double elapsed = GetAdjustedElapsed();
                    string currentVideo = _currentVideo.FileName;
                    double duration = _currentVideo.Length;
                    double videoElapsed = duration * (_lastKnownVideoProgress / 100.0);
                    if (videoElapsed == 0)
                    {
                        videoElapsed = duration;
                    }
                    bool shouldPlay = !IsPlaying;
                    _ui.Invoke(() =>
                    {
                        if (shouldPlay)
                        {
                            player.Play();
                            IsPlaying = true;
                        }
                        else
                        {
                            player.Pause();
                            IsPlaying = false;
                        }
                    });
                    _emotionLoggingService.LogEmotion(shouldPlay ? "start" : "stop", elapsed, videoFile, videoElapsed);
                    lock (_pauseLock)
                    {
                        if (shouldPlay)
                        {
                            if (_pauseStartTime.HasValue)
                            {
                                _pausedDuration += DateTime.Now - _pauseStartTime.Value;
                                _pauseStartTime = null;
                            }
                        }
                        else
                        {
                            _pauseStartTime = DateTime.Now;
                        }
                    }
                });
            }
        }

        // Method to acknowledge the start of the video
        public void AcknowledgeVideoStart()
        {
            StartVideoRequested = false;
        }

        // Method to get the numer for the file name
        private int GetNextTryNumber(string outputsFolder)
        {
            if (!Directory.Exists(outputsFolder))
                return 1;
            int maxTry = 0;
            var files = Directory.GetFiles(outputsFolder);
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var match = System.Text.RegularExpressions.Regex.Match(name, @"_try_(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                {
                    if (num > maxTry)
                        maxTry = num;
                }
            }
            return maxTry + 1;
        }

        // Method to update the countdown text
        public void UpdateCountdownText(string newText)
        {
            if (_countdownText != newText)
            {
                _countdownText = newText;
                OnPropertyChanged(nameof(CountdownText));
            }
        }

        // Method to start the countdown
        public void StartCountdown(int totalSeconds)
        {
            CountdownText = TimeSpan.FromSeconds(totalSeconds).ToString(@"mm\:ss");
        }

        // Method to start screen recording
        private void StartScreenRecording()
        {
            try
            {
                _screenRecordingService.StartRecording(_durationSeconds, _tryNumber);
                _ui.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    RecordingStatus = "Trwa nagrywanie ekranu";
                    OnPropertyChanged(nameof(RecordingStatus));
                }));
                _isRecording = true;
            }
            catch (Exception ex)
            {
                _ui.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    RecordingStatus = $"Błąd nagrywania: {ex.Message}";
                    OnPropertyChanged(nameof(RecordingStatus));
                }));
            }
        }

        // Method to stop screen recording
        private void StopScreenRecording()
        {
            try
            {
                if (!_isRecording) return;

                _screenRecordingService.StopRecording();

                Thread.Sleep(800);

                _isRecording = false;

                _ui.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    RecordingStatus = "Nagrywanie zatrzymane";
                    OnPropertyChanged(nameof(RecordingStatus));
                }));
            }
            catch (Exception ex)
            {
                _ui.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    OnPropertyChanged(nameof(RecordingStatus));
                }));
            }
        }

        //  Method to start emotion timer for logging emotions
        public void StartEmotionTimer()
        {
            _lastLoggedEmotion = null;
            _emotionTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _emotionTimer.Interval = TimeSpan.FromMilliseconds(500);
            _emotionTimer.Tick += EmotionTimer_Tick;
            _emotionTimer.Start();
        }

        // Method to notify that the video have started
        public void NotifyVideoActuallyStarted()
        {
            lock (_pauseLock)
            {
                recordingStartTime = DateTime.Now;
                _pausedDuration = TimeSpan.Zero;
                _pauseStartTime = null;
            }
        }

        // Method to log emotions
        private void EmotionTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying) return;
            // Blokada reentrancy
            if (Interlocked.Exchange(ref _emotionTickWorking, 1) == 1) return;
            string fileName = _currentVideo.FileName;
            var currentEmotion = _cameraViewModel.DetectedEmotion ?? "no_face";
            var elapsed = GetAdjustedElapsed();
            var videoProgress = _lastKnownVideoProgress;
            string lastLoggedEmotion;
            DateTime lastEmotionLogTime;
            lock (_emotionLock)
            {
                lastLoggedEmotion = _lastLoggedEmotion;
                lastEmotionLogTime = _lastEmotionLogTime;
            }
            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(fileName)) return;
                    var videoElapsed = _currentVideo.Length * (videoProgress / 100.0);
                    bool emotionChanged = currentEmotion != lastLoggedEmotion;
                    if ((emotionChanged && (DateTime.Now - lastEmotionLogTime).TotalSeconds >= 0.5))
                    {
                        _emotionLoggingService.LogEmotion(currentEmotion, elapsed, Path.GetFileName(fileName), videoElapsed);
                        var list = _perVideoEmotionEvents.GetOrAdd(fileName, _ => new List<(double, string)>());
                        lock (list)
                        {
                            list.Add((videoElapsed, currentEmotion));
                        }
                        lock (allVideoEmotions)
                        {
                            if (currentEmotion != "no_face")
                            {
                                allVideoEmotions.Add(currentEmotion);
                            }
                        }
                        lock (_emotionLock)
                        {
                            _lastLoggedEmotion = currentEmotion;
                            _lastEmotionLogTime = DateTime.Now;
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _emotionTickWorking, 0);
                }
            });
        }

        // Method to stop the timer for logging emotions
        private void StopEmotionTimer()
        {
            _emotionTimer?.Stop();
            _emotionTimer = null;
        }

        // Method to stop the camera and do a cleanup
        public void StopAllVideoCamera()
        {
            double remaining;
            lock (_countdownLock)
            {
                remaining = _timeLeft.TotalSeconds;
            }
            if (remaining > 0)
            {
                Cleanup();
                DeleteFiles();
            }
            else
            {

                Cleanup();
            }
        }

        // Method for cleanup
        private void Cleanup()
        {
            VideoSource = null;
            IsPlaying = false;
            VideoProgress = 0;
            StartVideoRequested = false;
            _lastLoggedEmotion = null;
            allVideoEmotions.Clear();
            _lastEmotionLogTime = DateTime.MinValue;
            StopEmotionTimer();
            recordingStartTime = DateTime.MinValue;
            _countdownCts?.Cancel();
            _countdownCts = null;
            lock (_countdownLock)
            {
                _timeLeft = TimeSpan.FromSeconds(_durationSeconds);
            }
            RecordingStatus = "";
            IsQuitPopupOpen = false;
            IsOverlayVisible = false;
            CountdownText = TimeSpan.FromSeconds(_durationSeconds).ToString(@"mm\:ss");
            _lastKnownVideoProgress = 0;
            _cameraViewModel?.StopCamera();
            StopScreenRecording();
            _emotionLoggingService.StopLogging();
            lock (_pauseLock)
            {
                _pausedDuration = TimeSpan.Zero;
                _pauseStartTime = null;
            }
            _finalEndActionsExecuted = false;
        }

        // Method to delete files (screen recording and emotion logging)
        private void DeleteFiles()
        {
            try
            {
                _screenRecordingService.DeleteFile();
                _emotionLoggingService.DeleteFile();
            }
            catch (Exception ex)
            {
            }
        }
    }
}