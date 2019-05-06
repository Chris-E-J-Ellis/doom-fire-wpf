using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoomFire
{
    public partial class MainWindow : Window
    {
        private int width;
        private int height;
        private WriteableBitmap renderSurfaceBitmap;
        private int bitmapStride;
        private Int32Rect renderSurfaceRegion;
        private byte[] renderSurfaceBuffer;
        private int[] firePixelBuffer;
        private bool runFire = false;
        private bool killFire = false;
        private readonly Random random;
        private byte[] fireRGBPalette = new byte[] {
                0x07,0x07,0x07,
                0x1F,0x07,0x07,
                0x2F,0x0F,0x07,
                0x47,0x0F,0x07,
                0x57,0x17,0x07,
                0x67,0x1F,0x07,
                0x77,0x1F,0x07,
                0x8F,0x27,0x07,
                0x9F,0x2F,0x07,
                0xAF,0x3F,0x07,
                0xBF,0x47,0x07,
                0xC7,0x47,0x07,
                0xDF,0x4F,0x07,
                0xDF,0x57,0x07,
                0xDF,0x57,0x07,
                0xD7,0x5F,0x07,
                0xD7,0x5F,0x07,
                0xD7,0x67,0x0F,
                0xCF,0x6F,0x0F,
                0xCF,0x77,0x0F,
                0xCF,0x7F,0x0F,
                0xCF,0x87,0x17,
                0xC7,0x87,0x17,
                0xC7,0x8F,0x17,
                0xC7,0x97,0x1F,
                0xBF,0x9F,0x1F,
                0xBF,0x9F,0x1F,
                0xBF,0xA7,0x27,
                0xBF,0xA7,0x27,
                0xBF,0xAF,0x2F,
                0xB7,0xAF,0x2F,
                0xB7,0xB7,0x2F,
                0xB7,0xB7,0x37,
                0xCF,0xCF,0x6F,
                0xDF,0xDF,0x9F,
                0xEF,0xEF,0xC7,
                0xFF,0xFF,0xFF };

        public MainWindow()
        {
            InitializeComponent();
            random = new Random((int)DateTime.Now.Ticks); // Vary random seed.
            InitialiseRenderSurface();
        }

        private void Button_Go_Click(object sender, RoutedEventArgs e)
        {
            Go.IsEnabled = false;
            Step.IsEnabled = false;

            StartFire();
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            Go.IsEnabled = true;
            Step.IsEnabled = true;
            runFire = false;
        }

        private void Button_Step_Click(object sender, RoutedEventArgs e)
        {
            Go.IsEnabled = true;
            runFire = false;

            StepFire(width, height);
            RenderFire();

            if (killFire)
                KillFire();
        }

        private void Button_SetRenderSurfaceSize_Click(object sender, RoutedEventArgs e)
        {
            runFire = false;
            Go.IsEnabled = true;

            InitialiseRenderSurface();
        }

        private void Button_Kill_Click(object sender, RoutedEventArgs e)
        {
            killFire = true;
        }

        private void Button_FlameOn_Click(object sender, RoutedEventArgs e)
        {
            killFire = false;
            SeedFire();
        }

        private void InitialiseRenderSurface()
        {
            RenderSurface.Width = Width;
            RenderSurface.Height = Height;
            width = (int)RenderSurface.Width;
            height = (int)RenderSurface.Height;
            firePixelBuffer = new int[width * height];
            InitialiseFireBuffer();
            SeedFire();

            int dpi = 96;
            renderSurfaceBitmap = new WriteableBitmap(width, height, dpi, dpi, PixelFormats.Bgra32, null);
            bitmapStride = renderSurfaceBitmap.PixelWidth * renderSurfaceBitmap.Format.BitsPerPixel / 8;
            renderSurfaceBuffer = new byte[renderSurfaceBitmap.PixelHeight * bitmapStride];
            InitialiseRenderBuffer();
            RenderSurface.Source = renderSurfaceBitmap;
            renderSurfaceRegion = new Int32Rect(0, 0, renderSurfaceBitmap.PixelWidth, renderSurfaceBitmap.PixelHeight);

            RenderFire();
        }

        private void StartFire()
        {
            runFire = true;
            killFire = false;

            Task.Run(() =>
            {
                while (runFire)
                {
                    StepFire(width, height);
                    RenderFire();

                    if (killFire)
                        KillFire();
                }
            });
        }

        private void RenderFire()
        {
            Dispatcher.BeginInvoke((Action)(() => renderSurfaceBitmap.WritePixels(renderSurfaceRegion, renderSurfaceBuffer, bitmapStride, 0)));
        }

        private void StepFire(int fireWidth, int fireHeight)
        {
            for (int x = 0; x < fireWidth; x++)
            {
                for (int y = 1; y < fireHeight; y++)
                {
                    var bufferPosition = (y * fireWidth) + x;
                    SpreadFire(bufferPosition, fireWidth, fireHeight);
                }
            }
        }

        private void SpreadFire(int sourcePosition, int fireWidth, int fireHeight)
        {
            var pixel = firePixelBuffer[sourcePosition];

            if (pixel <= 0)
            {
                firePixelBuffer[sourcePosition - fireWidth] = 0;
            }
            else
            {
                var decay = random.Next(0, 3);
                var destinationPosition = (sourcePosition - fireWidth) - decay + 1;

                firePixelBuffer[destinationPosition] = firePixelBuffer[sourcePosition] - (decay & 1);
            }

            // N.B. Collapsing the rendering into this function means we don't draw the top line of pixels. 
            // Currently not a problem as we'll have decayed at that point.
            var paletteIndex = pixel * 3;
            var bitmapBufferPosition = sourcePosition * 4;                                    // 32 Bits per pixel
            renderSurfaceBuffer[bitmapBufferPosition] = fireRGBPalette[paletteIndex + 2];     // B
            renderSurfaceBuffer[bitmapBufferPosition + 1] = fireRGBPalette[paletteIndex + 1]; // G
            renderSurfaceBuffer[bitmapBufferPosition + 2] = fireRGBPalette[paletteIndex + 0]; // R
        }

        private void KillFire()
        {
            for (int i = (height - 1) * width; i < firePixelBuffer.Length; i++)
            {
                var decay = random.Next(0, 2); // Not sure if this looks better inside or outside the loop.

                if (firePixelBuffer[i] > 0)
                    firePixelBuffer[i] = firePixelBuffer[i] - decay;
            }
        }

        private void InitialiseFireBuffer()
        {
            for (int i = 0; i < firePixelBuffer.Length; i++)
                firePixelBuffer[i] = 0;
        }

        private void InitialiseRenderBuffer()
        {
            // Initiliase bitmap with correct alpha (fully opaque);
            for (int i = 0; i < renderSurfaceBuffer.Length; i += 4)
            {
                renderSurfaceBuffer[i] = 0x00;
                renderSurfaceBuffer[i + 1] = 0x00;
                renderSurfaceBuffer[i + 2] = 0x00;
                renderSurfaceBuffer[i + 3] = 0xFF;
            }
        }

        private void SeedFire()
        {
            // Seed the buffer with some white pixels on the bottom line;
            for (int i = (height - 1) * width; i < firePixelBuffer.Length; i++)
            {
                firePixelBuffer[i] = ((fireRGBPalette.Length - 1) / 3);
            }
        }
    }
}