using System.IO;
using System.IO.Compression;

namespace MoodScan.Service
{
    public class DownloadService
    {
        // Method to save all files from Outputs to .zip
        public void SaveAllFilesAsZip()
        {
            // Path to the folder
            string sourceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Outputs");
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "wyniki.zip");

            // Check if the folder exists
            if (!Directory.Exists(sourceFolder))
            {
                Directory.CreateDirectory(sourceFolder);
            }

            var files = Directory.GetFiles(sourceFolder);

            // Save files to .zip
            using (var zip = new System.IO.Compression.ZipArchive(
                new FileStream(downloadsPath, FileMode.Create),
                System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }
        }
    }
}
