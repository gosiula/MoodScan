using System.Drawing;
using OpenCvSharp;

namespace MoodScan.Service
{
    public class FaceDetectionService
    {
        private readonly CascadeClassifier _faceCascade;


        public FaceDetectionService(string cascadeFilePath)
        {
            _faceCascade = new CascadeClassifier(cascadeFilePath);
        }

        // Method to detect faces
        public OpenCvSharp.Rect[] DetectFaces(Bitmap bitmap)
        {
            using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            var faces = _faceCascade.DetectMultiScale(gray, 1.1, 4);
            return faces;
        }
    }
}
