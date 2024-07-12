using System;
using System.Drawing;
using System.Windows.Forms;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace MandelbrotSet
{
    public partial class Form1 : Form
    {
        private Bitmap mandelbrotBitmap;
        private const int MaxIterations = 1000;
        private double MinRe = -2.0;
        private double MaxRe = 1.0;
        private double MinIm = -1.2;
        private double MaxIm = 1.2;
        private double zoomFactor = 1.0;
        private PointF panOffset = PointF.Empty;
        private Point lastMousePosition = Point.Empty;
        private bool isPanning = false;
        private Context context;
        private Accelerator accelerator;
        private Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>, int, double, double, double, double> mandelbrotKernel;
        private MemoryBuffer2D<uint, Stride2D.DenseX> colorBuffer;

        public Form1()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(this.Form1_MouseWheel);
            this.pictureBox1.MouseDown += new MouseEventHandler(this.PictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new MouseEventHandler(this.PictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new MouseEventHandler(this.PictureBox1_MouseUp);
            this.ClientSize = new Size(1920, 1080);
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            InitializeGPU();
        }

        private void InitializeGPU()
        {
            context = Context.CreateDefault();
            accelerator = context.GetCudaDevice(0).CreateAccelerator(context);
            mandelbrotKernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<uint, Stride2D.DenseX>, int, double, double, double, double>(MandelbrotKernel);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            EnsureBufferSize();
            GenerateMandelbrotSet();
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            double zoomChange = 0.1 * (e.Delta > 0 ? 1 : -1);
            PointF zoomCenter = pictureBox1.PointToClient(Cursor.Position);
            ZoomMandelbrot(zoomChange, zoomCenter);
        }

        private void ZoomMandelbrot(double zoomChange, PointF zoomCenter)
        {
            double zoomCenterRe = MinRe + (MaxRe - MinRe) * zoomCenter.X / pictureBox1.Width;
            double zoomCenterIm = MinIm + (MaxIm - MinIm) * zoomCenter.Y / pictureBox1.Height;

            double newZoomFactor = zoomFactor * (1.0 + zoomChange);
            double widthRe = (MaxRe - MinRe) / newZoomFactor;
            double heightIm = (MaxIm - MinIm) / newZoomFactor;

            MinRe = zoomCenterRe - widthRe * zoomCenter.X / pictureBox1.Width;
            MaxRe = MinRe + widthRe;
            MinIm = zoomCenterIm - heightIm * zoomCenter.Y / pictureBox1.Height;
            MaxIm = MinIm + heightIm;

            zoomFactor = newZoomFactor;

            GenerateMandelbrotSet();
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPanning = true;
                lastMousePosition = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                panOffset.X += e.X - lastMousePosition.X;
                panOffset.Y += e.Y - lastMousePosition.Y;
                lastMousePosition = e.Location;
                UpdateMandelbrotBounds();
                GenerateMandelbrotSet();
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPanning = false;
            }
        }

        private void UpdateMandelbrotBounds()
        {
            double centerX = (MinRe + MaxRe) / 2.0;
            double centerY = (MinIm + MaxIm) / 2.0;

            double width = (MaxRe - MinRe);
            double height = (MaxIm - MinIm);

            MinRe = centerX - width / 2.0 + panOffset.X / pictureBox1.Width * width;
            MaxRe = centerX + width / 2.0 + panOffset.X / pictureBox1.Width * width;
            MinIm = centerY - height / 2.0 + panOffset.Y / pictureBox1.Height * height;
            MaxIm = centerY + height / 2.0 + panOffset.Y / pictureBox1.Height * height;

            panOffset = PointF.Empty;
        }

        private void EnsureBufferSize()
        {
            var width = pictureBox1.Width;
            var height = pictureBox1.Height;

            Debug.WriteLine($"EnsureBufferSize called with dimensions: {width}x{height}");

            if (colorBuffer == null || colorBuffer.Extent.X != height || colorBuffer.Extent.Y != width)
            {
                colorBuffer?.Dispose();
                // ILGPU için Index2D'yi height, width sırasıyla oluşturuyoruz
                colorBuffer = accelerator.Allocate2DDenseX<uint>(new Index2D(height, width));
                mandelbrotBitmap?.Dispose();
                mandelbrotBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                Debug.WriteLine($"New colorBuffer allocated with dimensions: {colorBuffer.Extent.X}x{colorBuffer.Extent.Y}");
                Debug.WriteLine($"New mandelbrotBitmap created with dimensions: {mandelbrotBitmap.Width}x{mandelbrotBitmap.Height}");
            }
            else
            {
                Debug.WriteLine("Existing buffers reused");
            }
        }

        private void GenerateMandelbrotSet()
        {
            var width = pictureBox1.Width;
            var height = pictureBox1.Height;

            Debug.WriteLine($"GenerateMandelbrotSet called with dimensions: {width}x{height}");

            EnsureBufferSize();

            try
            {
                // ILGPU için Index2D'yi height, width sırasıyla oluşturuyoruz
                mandelbrotKernel(new Index2D(height, width), colorBuffer.View, MaxIterations, MinRe, MaxRe, MinIm, MaxIm);

                accelerator.Synchronize();

                Debug.WriteLine("GPU kernel execution completed");

                var colorData = colorBuffer.GetAsArray2D();

                Debug.WriteLine($"colorData retrieved. Dimensions: {colorData.GetLength(0)}x{colorData.GetLength(1)}");
                Debug.WriteLine($"mandelbrotBitmap dimensions: {mandelbrotBitmap.Width}x{mandelbrotBitmap.Height}");

                // Boyutları kontrol ederken sırayı değiştiriyoruz
                if (colorData.GetLength(0) != height || colorData.GetLength(1) != width)
                {
                    throw new InvalidOperationException($"Mismatch in dimensions. Expected: {width}x{height}, Got: {colorData.GetLength(1)}x{colorData.GetLength(0)}");
                }

                BitmapData bitmapData = mandelbrotBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                Debug.WriteLine("Bitmap locked for writing");

                unsafe
                {
                    uint* pixelPtr = (uint*)bitmapData.Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // colorData'nın boyutlarını ters çeviriyoruz
                            pixelPtr[y * width + x] = colorData[y, x];
                        }
                    }
                }

                Debug.WriteLine("Pixel data written to bitmap");

                mandelbrotBitmap.UnlockBits(bitmapData);

                Debug.WriteLine("Bitmap unlocked");

                pictureBox1.Image = mandelbrotBitmap;
                pictureBox1.Invalidate();

                Debug.WriteLine("PictureBox updated");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.GetType().Name}");
                Debug.WriteLine($"Exception message: {ex.Message}");
                Debug.WriteLine($"Exception stack trace: {ex.StackTrace}");
                MessageBox.Show($"An error occurred: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private static void MandelbrotKernel(Index2D index, ArrayView2D<uint, Stride2D.DenseX> colorBuffer, int maxIterations, double minRe, double maxRe, double minIm, double maxIm)
        {
            // index.X ve index.Y'yi ters çeviriyoruz
            double x0 = minRe + (maxRe - minRe) * index.Y / (colorBuffer.Extent.Y - 1);
            double y0 = minIm + (maxIm - minIm) * index.X / (colorBuffer.Extent.X - 1);

            double x = 0.0;
            double y = 0.0;
            int iteration = 0;

            while (x * x + y * y <= 4.0 && iteration < maxIterations)
            {
                double xtemp = x * x - y * y + x0;
                y = 2.0 * x * y + y0;
                x = xtemp;
                iteration++;
            }

            colorBuffer[index] = GetColor(iteration, maxIterations);
        }

        private static uint GetColor(int iteration, int maxIterations)
        {
            if (iteration == maxIterations)
                return 0xFF000000; // Black

            double hue = (double)iteration / maxIterations;
            return ColorFromHSV(hue, 1, 1);
        }

        private static uint ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = (int)(hue * 6) % 6;
            double f = hue * 6 - Math.Floor(hue * 6);

            byte v = (byte)(value * 255);
            byte p = (byte)(v * (1 - saturation));
            byte q = (byte)(v * (1 - f * saturation));
            byte t = (byte)(v * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return (uint)((0xFF << 24) | (v << 16) | (t << 8) | p);
                case 1: return (uint)((0xFF << 24) | (q << 16) | (v << 8) | p);
                case 2: return (uint)((0xFF << 24) | (p << 16) | (v << 8) | t);
                case 3: return (uint)((0xFF << 24) | (p << 16) | (q << 8) | v);
                case 4: return (uint)((0xFF << 24) | (t << 16) | (p << 8) | v);
                default: return (uint)((0xFF << 24) | (v << 16) | (p << 8) | q);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            colorBuffer?.Dispose();
            mandelbrotBitmap?.Dispose();
            accelerator?.Dispose();
            context?.Dispose();
        }
    }
}