using System.Windows;

namespace MoodScan
{
    public partial class App : Application
    {
        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
    }
}
