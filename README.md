# Mandelbrot Set Visualizer
![alt text](https://i.imgur.com/SZVLkeU.png)


This project is a C# Windows Forms application that visualizes the Mandelbrot set using CUDA-accelerated GPU computations via the ILGPU library.

## Description

The Mandelbrot Set Visualizer generates a colorful representation of the Mandelbrot set, a famous fractal in mathematics. 
It utilizes GPU acceleration to perform the complex calculations required for rendering the set, resulting in faster performance compared to CPU-only implementations.

## Features

- GPU-accelerated Mandelbrot set generation using CUDA and ILGPU
- Real-time rendering of the Mandelbrot set
- Colorful visualization based on iteration count

## Prerequisites

Before you begin, ensure you have met the following requirements:

- Windows operating system
- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later
- CUDA-capable NVIDIA GPU
- CUDA Toolkit 10.0 or later

## Installation

To install the Mandelbrot Set Visualizer, follow these steps:

1. Clone the repository:
   ```
   git clone https://github.com/your-username/mandelbrot-set-visualizer.git
   ```
2. Open the solution file (`MandelbrotSet.sln`) in Visual Studio.
3. Restore NuGet packages if necessary.
4. Build the solution.

## Usage

To use the Mandelbrot Set Visualizer:

1. Run the application from Visual Studio or by executing the built executable.
2. The Mandelbrot set will be automatically generated and displayed in the window.
3. Close the application when finished viewing.

## Contact

If you want to contact me, you can reach me at <hhasanguclu@gmail.com>.

## Acknowledgements

- [ILGPU](https://github.com/m4rs-mt/ILGPU) - The ILGPU library used for GPU acceleration
- [Mandelbrot Set](https://en.wikipedia.org/wiki/Mandelbrot_set) - Wikipedia page on the Mandelbrot set
