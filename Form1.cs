using System;
using System.Drawing;
using System.Windows.Forms;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
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
        private Context context;
        private Accelerator accelerator;
        private Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>, int, double, double, double, double> mandelbrotKernel;
        private MemoryBuffer2D<uint, Stride2D.DenseX> colorBuffer;
        private const double ZoomFactor = 0.1;

        public Form1()
        {
            InitializeComponent();
            this.pictureBox1.MouseDown += new MouseEventHandler(this.PictureBox1_MouseDown);
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

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            PointF zoomCenter = e.Location;
            if (e.Button == MouseButtons.Left)
            {
                ZoomMandelbrot(ZoomFactor, zoomCenter);
            }
            else if (e.Button == MouseButtons.Right)
            {
                ZoomMandelbrot(-ZoomFactor, zoomCenter);
            }
        }

        private void ZoomMandelbrot(double zoomChange, PointF zoomCenter)
        {
            double zoomCenterRe = MinRe + (MaxRe - MinRe) * zoomCenter.X / pictureBox1.Width;
            double zoomCenterIm = MinIm + (MaxIm - MinIm) * zoomCenter.Y / pictureBox1.Height;

            double newZoomFactor = zoomFactor * (1.0 + zoomChange);
            double widthRe = (MaxRe - MinRe) / newZoomFactor;
            double heightIm = (MaxIm - MinIm) / newZoomFactor;

            MinRe = zoomCenterRe - widthRe / 2;
            MaxRe = zoomCenterRe + widthRe / 2;
            MinIm = zoomCenterIm - heightIm / 2;
            MaxIm = zoomCenterIm + heightIm / 2;

            zoomFactor = newZoomFactor;

            GenerateMandelbrotSet();
        }

        private void EnsureBufferSize()
        {
            var width = pictureBox1.Width;
            var height = pictureBox1.Height;

            Debug.WriteLine($"EnsureBufferSize called with dimensions: {width}x{height}");

            if (colorBuffer == null || colorBuffer.Extent.X != height || colorBuffer.Extent.Y != width)
            {
                colorBuffer?.Dispose();
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

            EnsureBufferSize();

            try
            {
                mandelbrotKernel(new Index2D(height, width), colorBuffer.View, MaxIterations, MinRe, MaxRe, MinIm, MaxIm);

                accelerator.Synchronize();

                var colorData = colorBuffer.GetAsArray2D();

                unsafe
                {
                    fixed (uint* colorDataPtr = colorData)
                    {
                        BitmapData bitmapData = mandelbrotBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        Buffer.MemoryCopy(colorDataPtr, bitmapData.Scan0.ToPointer(), width * height * 4, width * height * 4);
                        mandelbrotBitmap.UnlockBits(bitmapData);
                    }
                }

                pictureBox1.Image = mandelbrotBitmap;
                pictureBox1.Invalidate();
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