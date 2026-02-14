using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using MoodScan.Service;

namespace MoodScan.ViewModel
{
    public class CameraViewModel : BaseViewModel
    {
        // Services
        private readonly MainViewModel _mainViewModel;
        private readonly CameraService _cameraService;
        private readonly FaceDetectionService _faceDetectionService;
        private readonly EmotionRecognitionService _emotionRecognitionService;

        // Tokens
        private CancellationTokenSource _detectionCancellation;
        private CancellationTokenSource _recognitionCancellation;

        // Camera
        private VideoCaptureDevice _videoSource;
        private readonly object _frameLock = new object();
        private List<(OpenCvSharp.Rect face, string result)> _faceResults = new List<(OpenCvSharp.Rect face, string result)>();
        private Bitmap _latestFrame;
        private List<(OpenCvSharp.Rect face, string result)> _lastFaceResults = new();

        // Available cameras
        private ObservableCollection<string> _availableDevices = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableDevices
        {
            get => _availableDevices;
            set
            {
                _availableDevices = value;
                OnPropertyChanged(nameof(AvailableDevices));
            }
        }

        // Selected camera index
        private int _selectedDeviceIndex;
        public int SelectedDeviceIndex
        {
            get => _selectedDeviceIndex;
            set
            {
                _selectedDeviceIndex = value;
                OnPropertyChanged(nameof(SelectedDeviceIndex));
            }
        }

        // Displayed image from the camera
        private BitmapImage _cameraImage;
        public BitmapImage CameraImage
        {
            get
            {
                return _cameraImage;
            }

            set
            {
                _cameraImage = value;
                OnPropertyChanged(nameof(CameraImage));
            }
        }

        // Emotion detected
        private string _detectedEmotion = "no_face";
        public string DetectedEmotion
        {
            get => _detectedEmotion;
            set
            {
                if (_detectedEmotion != value)
                {
                    _detectedEmotion = value;
                    OnPropertyChanged(nameof(DetectedEmotion));
                }
            }
        }

        // Detected faces
        public OpenCvSharp.Rect[] _detectedFaces { get; private set; }

        // Popup
        private bool _isFacesPopupOpen;
        public bool IsFacesPopupOpen
        {
            get => _isFacesPopupOpen;
            set
            {
                _isFacesPopupOpen = value;
                OnPropertyChanged(nameof(IsFacesPopupOpen));
            }
        }

        // Popup message
        private string _facesMessage;
        public string FacesMessage
        {
            get => _facesMessage;
            set
            {
                _facesMessage = value;
                OnPropertyChanged(nameof(FacesMessage));
            }
        }

        // Commands
        public ICommand StartVideoCommand { get; }
        public ICommand ClosePopupCommand { get; }

        public CameraViewModel(MainViewModel mainViewModel, CameraService cameraService, FaceDetectionService faceDetectionService, EmotionRecognitionService emotionRecognitionService)
        {
            _mainViewModel = mainViewModel;
            _cameraService = cameraService;
            _faceDetectionService = faceDetectionService;
            _emotionRecognitionService = emotionRecognitionService;

            StartVideoCommand = new CommandViewModel(ExecuteStartVideoCommand);
            ClosePopupCommand = new CommandViewModel(ExecuteClosePopupCommand);

            StartCamera();
            StartFaceDetection();
            StartPredictionLoop();
        }

        // Message about how many people are on the screen
        public string FaceWarningMessage
        {
            get
            {
                if (_detectedFaces == null || _detectedFaces.Length == 0)
                    return "Uwaga! Nie wykryto twarzy!";
                else if (_detectedFaces.Length > 1)
                    return "Uwaga! Wykryto zbyt wiele twarzy!";
                else
                    return "";
            }
        }

        // Support for the start button and moving to the next view or displaying a popup
        private void ExecuteStartVideoCommand(object obj)
        {
            if (_detectedFaces == null || _detectedFaces.Length == 0)
            {
                FacesMessage = "Nie wykryto żadnej twarzy!" + Environment.NewLine + "Powinna być widoczna jedna osoba.";
                IsFacesPopupOpen = true;
            }
            else if (_detectedFaces.Length > 1)
            {
                FacesMessage = "Wykryto za dużo twarzy!" + Environment.NewLine + "Powinna być widoczna jedna osoba.";
                IsFacesPopupOpen = true;
            }
            else
            {
                _mainViewModel.ShowVideoCameraViewCommand.Execute(null);
            }
        }

        // Popup closing support
        private void ExecuteClosePopupCommand(object obj)
        {
            IsFacesPopupOpen = false;
        }


        // Method to load available cameras and launch the first one on the list
        private void StartCamera()
        {
            var cameras = _cameraService.GetAvailableCameras();
            foreach (var cam in cameras)
            {
                AvailableDevices.Add(cam.Name);
            }

            if (AvailableDevices.Count > 0)
            {
                SelectedDeviceIndex = 0;
            }
            if (cameras.Count == 0) return;

            _videoSource = new VideoCaptureDevice(cameras[SelectedDeviceIndex].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
        }

        // Method for capturing consecutive frames
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            var bitmapFrame = (Bitmap)eventArgs.Frame.Clone();

            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = (Bitmap)bitmapFrame.Clone();
            }

            // Drawing faces and emotions
            lock (_frameLock)
            {
                using (Graphics g = Graphics.FromImage(bitmapFrame))
                {
                    if (_detectedFaces != null)
                    {
                        foreach (var face in _detectedFaces)
                        {
                            using (System.Drawing.Pen thickPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 13, 253, 161), 5))
                            {
                                g.DrawRectangle(thickPen, face.X, face.Y, face.Width, face.Height);
                            }
                            lock (_faceResults)
                            {
                                (OpenCvSharp.Rect face, string result)? match = null;
                                double bestDistance = double.MaxValue;

                                foreach (var result in _lastFaceResults)
                                {
                                    var dx = result.face.X - face.X;
                                    var dy = result.face.Y - face.Y;
                                    var distance = Math.Sqrt(dx * dx + dy * dy);

                                    if (distance < 50 && distance < bestDistance)
                                    {
                                        bestDistance = distance;
                                        match = result;
                                    }
                                }

                                if (match.HasValue)
                                {
                                    var font = new Font("Montserrat", 20);
                                    var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 13, 253, 161));
                                    var point = new PointF(face.X, face.Y - 40);

                                    var state = g.Save();

                                    g.TranslateTransform(point.X, point.Y);
                                    g.ScaleTransform(-1, 1);
                                    g.TranslateTransform(-face.Width, 0);

                                    var layoutRect = new RectangleF(0, 0, face.Width, 40);

                                    var format = new StringFormat
                                    {
                                        Trimming = StringTrimming.EllipsisCharacter,
                                        Alignment = StringAlignment.Near,
                                        LineAlignment = StringAlignment.Near,
                                    };

                                    g.DrawString(match.Value.result, font, brush, layoutRect, format);
                                    g.Restore(state);
                                }
                            }
                        }
                    }
                }
            }

            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    using (var memory = new System.IO.MemoryStream())
                    {
                        bitmapFrame.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        CameraImage = bitmapImage;
                    }
                });
            }
            catch (System.Exception ex)
            {
            }
            bitmapFrame.Dispose();
        }

        // Face detection method
        private void StartFaceDetection()
        {
            _detectionCancellation = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    while (_detectionCancellation != null && !_detectionCancellation.Token.IsCancellationRequested)
                    {
                        Bitmap frameToProcess = null;
                        lock (_frameLock)
                        {
                            if (_latestFrame != null)
                            {
                                frameToProcess = (Bitmap)_latestFrame.Clone();
                            }
                        }

                        if (frameToProcess != null)
                        {
                            var faces = _faceDetectionService.DetectFaces(frameToProcess);

                            lock (_frameLock)
                            {
                                _detectedFaces = faces;
                                OnPropertyChanged(nameof(FaceWarningMessage));
                            }

                            frameToProcess.Dispose();
                        }

                        Thread.Sleep(200);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                }
            }, _detectionCancellation.Token);
        }

        // Method for recognizing emotions
        private void StartPredictionLoop()
        {
            _recognitionCancellation = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    while (!_recognitionCancellation.Token.IsCancellationRequested)
                    {
                        Bitmap frameCopy = null;
                        OpenCvSharp.Rect[] facesCopy;

                        lock (_frameLock)
                        {
                            if (_latestFrame != null && _detectedFaces != null)
                            {
                                frameCopy = (Bitmap)_latestFrame.Clone();
                                facesCopy = _detectedFaces.ToArray();
                            }
                            else
                            {
                                facesCopy = new OpenCvSharp.Rect[0];
                            }
                        }

                        var newResults = new List<(OpenCvSharp.Rect face, string result)>();
                        var newResultsEnglish = new List<(OpenCvSharp.Rect face, string result)>();

                        if (frameCopy != null)
                        {
                            foreach (var face in facesCopy)
                            {
                                try
                                {
                                    var faceBitmap = frameCopy.Clone(new System.Drawing.Rectangle(face.X, face.Y, face.Width, face.Height), frameCopy.PixelFormat);

                                    var prediction = _emotionRecognitionService.PredictEmotion(faceBitmap);

                                    faceBitmap.Dispose();

                                    newResults.Add((face, $"{prediction.Polish}, {(prediction.Confidence * 100):F2}%"));
                                    newResultsEnglish.Add((face, $"{prediction.English}"));
                                }
                                catch (Exception ex)
                                {
                                    newResults.Add((face, $"Prediction error: {ex.Message}"));
                                }
                            }

                            frameCopy.Dispose();
                        }

                        lock (_faceResults)
                        {
                            _faceResults = newResults;
                            if (newResults.Count > 0)
                            {
                                _lastFaceResults = newResults;
                            }

                            if (_detectedFaces != null)
                            {
                                if (_detectedFaces.Length == 0)
                                {
                                    DetectedEmotion = "no_face";
                                }
                                else if (_detectedFaces.Length > 1)
                                {
                                    DetectedEmotion = "too_many_faces";
                                }
                                else if (_detectedFaces.Length == 1)
                                {
                                    if (newResults.Count == 1)
                                    {
                                        DetectedEmotion = newResultsEnglish[0].result;
                                    }
                                    else
                                    {
                                        DetectedEmotion = "unknown";
                                    }
                                }
                                else
                                {
                                    DetectedEmotion = "unknown";
                                }
                            }
                        }
                        if (_recognitionCancellation == null || _recognitionCancellation.IsCancellationRequested)
                            break;

                        await Task.Delay(1000, _recognitionCancellation.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                }
            }, _recognitionCancellation.Token);
        }

        // Method to stop the camera
        public void StopCamera()
        {
            // Stop emotion recognition
            _recognitionCancellation?.Cancel();

            // Stop face detection
            _detectionCancellation?.Cancel();

            _recognitionCancellation = null;
            _detectionCancellation = null;

            // Stop camera
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
            }
        }
    }
}
