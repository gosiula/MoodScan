using AForge.Video.DirectShow;

namespace MoodScan.Service
{
    public class CameraService
    {
        // Method to get available cameras
        public List<FilterInfo> GetAvailableCameras()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            return devices.Cast<FilterInfo>().ToList();
        }
    }
}
