# Mandelbrot Set Visualizer

![alt text](https://i.imgur.com/SZVLkeU.png)

This project is a high-performance Mandelbrot Set viewer implemented in C# using Windows Forms and ILGPU for GPU acceleration. It allows users to explore the fascinating world of the Mandelbrot Set with smooth zooming and panning capabilities.

## Features

- Real-time rendering of the Mandelbrot Set
- GPU acceleration using ILGPU and CUDA
- Smooth zooming with left and right mouse clicks
- Interactive panning
- Full-screen display
- Exit button for easy closure of the application

## Requirements

- Windows operating system
- .NET Framework 4.7.2 or higher
- CUDA-capable NVIDIA GPU
- CUDA Toolkit 10.0 or higher

## Installation

1. Clone this repository:
   ```
   git clone https://github.com/yourusername/mandelbrot-set-viewer.git
   ```
2. Open the solution in Visual Studio.
3. Restore NuGet packages if necessary.
4. Build the solution.

## Usage

1. Run the application.
2. Use the mouse to interact with the Mandelbrot Set:
   - Left-click to zoom in at the clicked point
   - Right-click to zoom out from the clicked point
3. To exit the application, click the red "X" button in the top-right corner.

## Performance Tips

For optimal performance, ensure that your NVIDIA drivers are up to date and that you have the latest version of the CUDA Toolkit installed.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Thanks to the ILGPU team for their excellent GPU computing library.
- Inspired by the beauty and complexity of the Mandelbrot Set.
