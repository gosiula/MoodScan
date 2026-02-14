using System.Drawing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MoodScan.Model;

namespace MoodScan.Service
{
    public class EmotionRecognitionService
    {
        private readonly InferenceSession session;

        // Emotions
        private readonly List<string> labels = new List<string>
        {
            "angry", "confused", "happy", "neutral", "sad", "scared", "surprised"
        };
        private readonly List<string> labelsPolish = new List<string>
        {
            "zły", "zniesmaczony", "szczęśliwy", "neutralny", "smutny", "przestraszony", "zaskoczony"
        };

        public EmotionRecognitionService(string modelPath)
        {
            session = new InferenceSession(modelPath);
        }

        // Method to predict emotions
        public EmotionResultModel PredictEmotion(Bitmap faceBitmap)
        {
            float[] inputData = PrepareImage(faceBitmap);
            var inputTensor = new DenseTensor<float>(inputData, new int[] { 1, 224, 224, 3 });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            if (output.Length == labels.Count)
            {
                int maxIndex = Array.IndexOf(output, output.Max());
                string predictedEmotionPolish = labelsPolish[maxIndex];
                float confidence = output[maxIndex];

                return new EmotionResultModel
                {
                    Polish = labelsPolish[maxIndex],
                    English = labels[maxIndex],
                    Confidence = confidence
                };
            }
            else
            {
                return new EmotionResultModel
                {
                    Polish = "błąd",
                    English = "error",
                    Confidence = 0f
                };
            }
        }

        // Method to prepare image
        private float[] PrepareImage(Bitmap bmp)
        {
            // 1️. Change the size to 224x224
            Bitmap resized = new Bitmap(bmp, new System.Drawing.Size(224, 224));

            // 2️. Create input array (1, 224, 224, 3)
            float[] input = new float[224 * 224 * 3];

            // 3️. Lock bitmap for quick access
            var data = resized.LockBits(
                new Rectangle(0, 0, 224, 224),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb
            );

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;

                for (int y = 0; y < 224; y++)
                {
                    for (int x = 0; x < 224; x++)
                    {
                        // Getting values ​​in BGR order
                        byte blue = ptr[y * stride + x * 3 + 0];
                        byte green = ptr[y * stride + x * 3 + 1];
                        byte red = ptr[y * stride + x * 3 + 2];

                        // 4️. Normalization according to preprocess_input (VGG16):
                        // BGR, subtract ImageNet averages
                        input[(y * 224 + x) * 3 + 0] = blue - 103.939f; // B
                        input[(y * 224 + x) * 3 + 1] = green - 116.779f; // G
                        input[(y * 224 + x) * 3 + 2] = red - 123.68f;  // R
                    }
                }
            }
            resized.UnlockBits(data);
            resized.Dispose();

            return input;
        }
    }
}
