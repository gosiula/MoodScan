using System.Globalization;
using System.IO;
using MoodScan.Model;


namespace MoodScan.Service
{
    public class VideoLoaderService
    {
        // Method to load video from .csv 
        public VideoModel LoadSingleVideoFromCsv(string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"CSV file was not found: {csvPath}.");
            }

            var lines = File.ReadAllLines(csvPath);

            if (lines.Length < 2)
            {
                throw new InvalidDataException("The CSV file must contain a header and at least one row of data.");
            }

            // We skip the header (line 0) and read the first line of data (line 1)
            string dataLine = lines[1].Trim();

            if (string.IsNullOrWhiteSpace(dataLine))
            {
                throw new InvalidDataException("The data row in the CSV file is empty.");
            }

            // Separate data by a separator (semicolon)
            string[] columns = dataLine.Split(';');

            if (columns.Length == 0 || string.IsNullOrWhiteSpace(columns[0]))
            {
                throw new InvalidDataException("The first column is empty.");
            }

            // The first column is the path to the video
            string relativeVideoPath = columns[0].Trim();

            string videoFileName = Path.GetFileName(relativeVideoPath);

            // The third column is the video duration
            string lengthStr = columns.Length > 2 ? columns[2].Trim() : "0";

            lengthStr = lengthStr.Replace(',', '.');

            double length;

            if (!double.TryParse(lengthStr, NumberStyles.Float, CultureInfo.InvariantCulture, out length))
            {
                length = 0d;
            }

            // Path to Video
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string videos2Folder = Path.Combine(basePath, "Video");
            string videoPath = Path.Combine(videos2Folder, relativeVideoPath);

            if (!File.Exists(videoPath))
            {
                string devRoot = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\.."));
                videos2Folder = Path.Combine(devRoot, "Video");
                videoPath = Path.Combine(videos2Folder, relativeVideoPath);
            }

            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException($"The file was not found: {relativeVideoPath}\nSearched for in: {videoPath}");
            }

            return new VideoModel
            {
                FileName = videoFileName,
                FullPath = videoPath,
                Length = length
            };
        }
    }
}