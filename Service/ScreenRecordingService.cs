using System.Diagnostics;
using System.IO;

namespace MoodScan.Service
{
    public class ScreenRecordingService
    {
        private Process _ffmpegProcess;
        private string _currentRecordingPath;

        // Method to start recording the screen
        public string StartRecording(int durationSeconds, int tryNumber)
        {
            string outputsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Outputs");
            if (!Directory.Exists(outputsFolder))
                Directory.CreateDirectory(outputsFolder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _currentRecordingPath = Path.Combine(outputsFolder, $"Screen_recording_{timestamp}_try_{tryNumber}.mp4");

            var startInfo = new ProcessStartInfo
            {
                FileName = $"Resources/ffmpeg.exe",
                Arguments = $"-y -f gdigrab -framerate 30 -i desktop -vcodec libx264 -preset ultrafast -pix_fmt yuv420p -t {durationSeconds} \"{_currentRecordingPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            };

            _ffmpegProcess = new Process { StartInfo = startInfo };
            _ffmpegProcess.Start();
            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();

            return _currentRecordingPath;
        }

        // Method to delete the file with screen recording
        public void DeleteFile()
        {
            try
            {
                File.Delete(_currentRecordingPath);
            }
            catch
            {
            }
        }

        // Method to stop recording
        public void StopRecording()
        {
            if (_ffmpegProcess == null || _ffmpegProcess.HasExited) return;

            try
            {
                _ffmpegProcess.StandardInput.WriteLine("q");
                _ffmpegProcess.StandardInput.Flush();

                if (!_ffmpegProcess.WaitForExit(5000))
                {
                    _ffmpegProcess.Kill();
                    _ffmpegProcess.WaitForExit();
                }
            }
            finally
            {
                _ffmpegProcess.Dispose();
                _ffmpegProcess = null;
            }
        }
    }

}
