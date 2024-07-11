using System;
using System.Drawing;
using System.Windows.Forms;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace MandelbrotSet
{
    public partial class Form1 : Form
    {
        private Bitmap mandelbrotBitmap;
        private const int MaxIterations = 1000;
        private const float MinRe = -2.0f;
        private const float MaxRe = 1.0f;
        private const float MinIm = -1.2f;
        private const float MaxIm = 1.2f;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mandelbrotBitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            GenerateMandelbrotSet();
            pictureBox1.Image = mandelbrotBitmap;
        }

        private void GenerateMandelbrotSet()
        {
            using (var context = Context.CreateDefault())
            using (var accelerator = context.GetCudaDevice(0).CreateAccelerator(context))
            {
                var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<int, Stride2D.DenseX>, int, int, float, float, float, float>(MandelbrotKernel);

                var width = mandelbrotBitmap.Width;
                var height = mandelbrotBitmap.Height;

                using (var buffer = accelerator.Allocate2DDenseX<int>(new Index2D(width, height)))
                {
                    kernel(buffer.Extent.ToIntIndex(), buffer.View, MaxIterations, width, MinRe, MaxRe, MinIm, MaxIm);
                    accelerator.Synchronize();

                    var data = buffer.GetAsArray2D();

                    // Buffer boyutlarını kontrol et
                    Console.WriteLine($"Buffer dimensions: {buffer.Extent.X}x{buffer.Extent.Y}");
                    Console.WriteLine($"Data dimensions: {data.GetLength(0)}x{data.GetLength(1)}");
                    Console.WriteLine($"Width: {width}, Height: {height}");

                    for (int y = 0; y < height && y < data.GetLength(0); y++)
                    {
                        for (int x = 0; x < width && x < data.GetLength(1); x++)
                        {
                            int iterations = data[y, x];
                            Color color = GetColor(iterations);
                            mandelbrotBitmap.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }

        private static void MandelbrotKernel(Index2D index, ArrayView2D<int, Stride2D.DenseX> buffer, int maxIterations, int width, float minRe, float maxRe, float minIm, float maxIm)
        {
            float x0 = minRe + (maxRe - minRe) * index.X / (width - 1);
            float y0 = minIm + (maxIm - minIm) * index.Y / (width - 1);

            float x = 0.0f;
            float y = 0.0f;
            int iteration = 0;

            while (x * x + y * y <= 4.0f && iteration < maxIterations)
            {
                float xtemp = x * x - y * y + x0;
                y = 2.0f * x * y + y0;
                x = xtemp;
                iteration++;
            }

            buffer[index] = iteration;
        }

        private Color GetColor(int iteration)
        {
            if (iteration == MaxIterations)
                return Color.Black;

            float hue = (float)iteration / MaxIterations;
            return ColorFromHSV(hue, 1, 1);
        }

        private Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue * 6)) % 6;
            float f = hue * 6 - (float)Math.Floor(hue * 6);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }
}