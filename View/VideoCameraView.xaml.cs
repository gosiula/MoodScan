using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MoodScan.ViewModel;

namespace MoodScan.View
{
    public partial class VideoCameraView : UserControl
    {
        private readonly DispatcherTimer _progressTimer = new DispatcherTimer();
        private readonly DispatcherTimer _playbackStartDetector = new DispatcherTimer();
        private TimeSpan _lastPosition = TimeSpan.Zero;
        private bool _playbackStartedDetected = false;

        public VideoCameraView()
        {
            InitializeComponent();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(150);
            _progressTimer.Tick += ProgressTimer_Tick;

            _playbackStartDetector.Interval = TimeSpan.FromMilliseconds(100);
            _playbackStartDetector.Tick += PlaybackStartDetector_Tick;
        }

        // Method to start all threads (logging emotions, recording the screen, countdown and video)
        private void PlaybackStartDetector_Tick(object sender, EventArgs e)
        {
            if (VideoPlayer == null || DataContext is not VideoCameraViewModel vm) return;

            if (VideoPlayer.NaturalDuration.HasTimeSpan && VideoPlayer.IsLoaded)
            {
                TimeSpan current = VideoPlayer.Position;
                if (current > TimeSpan.Zero && current != _lastPosition)
                {
                    _playbackStartedDetected = true;
                    _playbackStartDetector.Stop();

                    vm.NotifyVideoActuallyStarted();

                    // Start logging emotions and recording the screen
                    Task.Run(() =>
                    {
                        vm.StartRecordingAndLogging();
                    });

                    // Start emotion timer
                    vm.StartEmotionTimer();

                    // Start countdown
                    vm.StartCountdown(vm.DurationSeconds);

                    // IsPlaying flag = true (makes the video play)
                    vm.IsPlaying = true;
                }
                _lastPosition = current;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _progressTimer.Start();

            if (DataContext is VideoCameraViewModel vm)
            {
                vm.RequestVideoPlay += OnRequestVideoPlay;
            }
        }

        // Method to make the video play after loading
        private void OnRequestVideoPlay()
        {
            Dispatcher.Invoke(() =>
            {
                if (VideoPlayer == null) return;

                VideoPlayer.Play();

                if (DataContext is VideoCameraViewModel vm)
                {
                    vm.AcknowledgeVideoStart();
                    vm.IsPlaying = true;

                    _lastPosition = TimeSpan.Zero;
                    _playbackStartedDetected = false;
                    _playbackStartDetector.Start();
                }
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VideoCameraViewModel vm)
            {
                vm.RequestVideoPlay -= OnRequestVideoPlay;
            }

            _progressTimer.Stop();
            if (VideoPlayer != null)
            {
                VideoPlayer.Pause();
                VideoPlayer.Source = null;
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer != null)
            {
                VideoPlayer.Stop();
            }
        }

        // Method to change the video player position and to show overlay after the ending
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (VideoPlayer == null || DataContext is not VideoCameraViewModel vm) return;

            // Showing the overlay
            if (vm.CountdownText == "00:00" || vm.CountdownText == "0:00")
            {
                vm.IsOverlayVisible = true;
                vm.IsPlaying = false;

                if (VideoPlayer != null)
                {
                    VideoPlayer.Pause();
                    VideoPlayer.Stop();
                }

                if (!vm.FinalEndActionsExecuted)
                {
                    vm.PerformFinalEndActions();
                    vm.FinalEndActionsExecuted = true;
                }

                return;
            }

            // Changing the video position
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan pos = VideoPlayer.Position;
                TimeSpan dur = VideoPlayer.NaturalDuration.TimeSpan;

                if (dur.TotalSeconds > 0)
                {
                    vm.VideoProgress = Math.Min(100, (pos.TotalSeconds / dur.TotalSeconds) * 100);

                    TimeSpan remaining = dur - pos;
                    string timeStr = remaining.TotalSeconds > 0
                        ? remaining.ToString(@"mm\:ss")
                        : "00:00";

                    vm.UpdateCountdownText(timeStr);
                }
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

            if (DataContext is VideoCameraViewModel vm)
            {
                vm.NotifyVideoActuallyStarted();
            }
        }
    }
}