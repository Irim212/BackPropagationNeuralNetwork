using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BackpropagationNeuralNetwork;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace BackPropagationGUI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string[] usedLetters = {"a", "p", "q", "z"};
        private Network network;

        private readonly List<List<double>> expectedOutputs = new List<List<double>>()
        {
            new List<double>() {1, 0, 0, 0},
            new List<double>() {0, 1, 0, 0},
            new List<double>() {0, 0, 1, 0},
            new List<double>() {0, 0, 0, 1}
        };

        private Bitmap bitmap;

        public MainWindow()
        {
            InitializeComponent();
            setCanvas(2);

            EraseButton.Click += eraseButtonClick;
            SaveButton.Click += saveButtonClick;
            LoadButton.Click += loadButtonClick;
            ReadCanvas.Click += readCanvasClick;
            LearnButton.Click += learnButtonClick;
        }

        private void learnButtonClick(object sender, RoutedEventArgs e)
        {
            network = NetworkBuilder.GetBuilder().SetOutputLayerNeurons(4).SetLearningRate(0.05d).SetHiddenLayerNeurons(8)
                .SetInputLayerNeurons(20).SetMaxEras(15000).SetMomentum(0.05d).Build();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/letters");

            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith(".bmp"))
                    continue;

                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                StrokeCollection strokes = new StrokeCollection(fileStream);
                DrawingCanvas.Strokes = strokes;
                currentCanvasToBitmap();

                double[] input = readBitmapPoints(bitmap, 20);

                int outputIndex = Array.IndexOf(usedLetters, Path.GetFileNameWithoutExtension(file).Substring(0, 1));

                network.AddLearningPair(input.ToList(), expectedOutputs[outputIndex]);
            }

            network.SingleEraEnded += data =>
            {
                Console.WriteLine(
                    $"Era: {data.CurrentEra}, Learn progress: {data.LearnProgress}, Overall Error: {data.OverallError}" +
                    $", Percentage of error: {data.PercentOfError}");
            };

            network.Learn();
        }

        private void readCanvasClick(object sender, RoutedEventArgs e)
        {
            currentCanvasToBitmap();
            double[] input = readBitmapPoints(bitmap, 20);

            foreach (double output in network.GetResultForInputs(input.ToList()))
            {
                Console.WriteLine(output);
            }
        }

        private void loadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.bmp;*.gif)|*.png;*.bmp;*.gif|All files (*.*)|*.*",
                InitialDirectory = Directory.GetCurrentDirectory() + "/letters"
            };

            if (openFileDialog.ShowDialog() != true)
                return;
            foreach (string filename in openFileDialog.FileNames)
            {
                FileName.Text = Path.GetFileName(filename);
                var fs = new FileStream(Path.GetFileName(filename),
                    FileMode.Open, FileAccess.Read);
                StrokeCollection strokes = new StrokeCollection(fs);
                DrawingCanvas.Strokes = strokes;
            }
        }

        private void saveButtonClick(object sender, RoutedEventArgs e)
        {
            currentCanvasToBitmap();

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "/letters/"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/letters/");
            }

            bitmap.Save("letters/" + FileName.Text + ".bmp");

            var fs = new FileStream("letters/" + FileName.Text, FileMode.Create);
            DrawingCanvas.Strokes.Save(fs);
        }

        private void eraseButtonClick(object sender, RoutedEventArgs e)
        {
            this.DrawingCanvas.Strokes.Clear();
        }

        private void setCanvas(int pencilWidth)
        {
            DrawingCanvas.DefaultDrawingAttributes.Width = pencilWidth;
            DrawingCanvas.DefaultDrawingAttributes.Height = pencilWidth;
        }

        private void currentCanvasToBitmap()
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(DrawingCanvas);

            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            MemoryStream ms = new MemoryStream();
            encoder.Save(ms);

            bitmap = new Bitmap(ms);
        }


        private static double[] readBitmapPoints(Bitmap bitmap, int linesPerAxis)
        {
            int rightSideStart = 0, bottomSideStart = 0, topSideStart = bitmap.Height, leftSideStart = bitmap.Width;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (!isPixelIsNotWhite(bitmap.GetPixel(x, y)))
                        continue;

                    Color color = bitmap.GetPixel(x, y);

                    if (y > bottomSideStart)
                        bottomSideStart = y;

                    if (x > rightSideStart)
                        rightSideStart = x;

                    if (y < topSideStart)
                        topSideStart = y;

                    if (x < leftSideStart)
                        leftSideStart = x;
                }
            }

            double[] output = new double[linesPerAxis];

            float horizontalStep = (rightSideStart - leftSideStart) / (float) linesPerAxis;
            float verticalStep = (bottomSideStart - topSideStart) / (float) linesPerAxis;

            for (int i = 0; i < linesPerAxis; i++)
            {
                int x = leftSideStart + (int) (i * horizontalStep);

                for (int j = 0; j < linesPerAxis; j++)
                {
                    int y = topSideStart + (int) (j * verticalStep);

                    if (isPixelIsNotWhite(bitmap.GetPixel(x, y)))
                    {
                        output[i] += 1d;
                    }
                }
            }

            double max = output.Max();
            double min = output.Min();
            
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (output[i] - min) / (max - min);
            }      
                   
            return output;
        }

        private static bool isPixelIsNotWhite(Color color)
        {
            return color.R != 255 || color.G != 255 || color.B != 255;
        }
    }
}