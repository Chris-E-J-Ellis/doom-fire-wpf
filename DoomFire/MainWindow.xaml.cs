using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoomFire
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int dpi = 96;
        WriteableBitmap writeableBitmap;
        byte[] buffer;
        Int32Rect region;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            writeableBitmap = new WriteableBitmap((int)DrawMe.Width, (int)DrawMe.Height, dpi, dpi, PixelFormats.Bgra32, null);
            DrawMe.Source = writeableBitmap;
            buffer = new byte[writeableBitmap.PixelHeight * writeableBitmap.PixelWidth * writeableBitmap.Format.BitsPerPixel / 8];
            region = new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
        }

        private void Window_Loaded2(object sender, RoutedEventArgs e)
        {
            var rect = new System.Drawing.Rectangle(0, 0, 400, 400);
            var bitmap = new Bitmap(rect.Width, rect.Height);

            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = data.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(data.Stride) * data.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Test
            System.Runtime.InteropServices.Marshal.Copy(ptr, new int[] { 0x50, 0x50, 0x50, 0x50 }, 0, bytes);

            // Copy the modified RBG values back 
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bitmap.UnlockBits(data);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(
                  o =>
                  {
                      while (true)
                      {
                          for (int i = 0; i < buffer.Length; i += 4)
                          {
                              buffer[i] = (byte)DateTime.Now.Ticks;
                              buffer[i + 1] = (byte)(DateTime.Now.Ticks);
                              buffer[i + 2] = (byte)(DateTime.Now.Second);
                              buffer[i + 3] = 0xFF;
                          }

                          Dispatcher.BeginInvoke((Action)(() => writeableBitmap.WritePixels(region, buffer, writeableBitmap.PixelWidth * writeableBitmap.Format.BitsPerPixel / 8, 0)));
                      }
                  });
        }
    }
}
