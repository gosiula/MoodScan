using System.IO;

namespace MoodScan.Service
{
    public class EmotionLoggingService
    {
        private StreamWriter _csvWriter;
        private DateTime _lastSaveTime;
        private readonly List<string> _emotionBuffer = new List<string>();
        private string _csvPath;

        // Method to start logging emotions to .csv file
        public void StartLogging(string outputsFolder, int tryNumber)
        {
            if (!Directory.Exists(outputsFolder))
                Directory.CreateDirectory(outputsFolder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _csvPath = Path.Combine(outputsFolder, $"Emotions_log_{timestamp}_try_{tryNumber}.csv");

            _csvWriter = new StreamWriter(_csvPath, true);
            _csvWriter.WriteLine("Elapsed;Emotion;VideoFile;VideoElapsed;Timestamp");

            _lastSaveTime = DateTime.MinValue;
        }

        // Method to delete .csv file
        public void DeleteFile()
        {
            try
            {
                File.Delete(_csvPath);
            }
            catch
            {
            }
        }

        // Method to stop logging to .csv file
        public void StopLogging()
        {
            _csvWriter?.Flush();
            _csvWriter?.Close();
            _csvWriter = null;
        }

        // Method to log emotion to .csv file
        public void LogEmotion(string currentEmotion, double elapsed, string videoFile, double videoElapsed)
        {
            if (_csvWriter == null)
                return;

            _emotionBuffer.Add(currentEmotion);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _csvWriter.WriteLine($"{elapsed:F2};{currentEmotion};{videoFile};{videoElapsed:F2};{timestamp}");
            _csvWriter.Flush();
        }

        // Method to log the end of the video in the .csv file
        public void LogEnd(double elapsed, string videoFile, double videoElapsed)
        {
            if (_csvWriter == null)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _csvWriter.WriteLine($"{elapsed:F2};end;{videoFile};{videoElapsed:F2};{timestamp}");
            StopLogging();
        }
    }
}
