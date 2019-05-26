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
    public partial class MainWindow
    {
        private readonly string[] usedLetters = {"a", "q", "z"};
        private Network network;

        private readonly List<List<double>> expectedOutputs = new List<List<double>>()
        {
            new List<double>() {1, 0, 0},
            new List<double>() {0, 1, 0},
            new List<double>() {0, 0, 1}
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
            network = NetworkBuilder.GetBuilder().SetOutputLayerNeurons(3)
                .SetInputLayerNeurons(20).SetMaxEras(200000).SetHiddenLayerNeurons(7).Build();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/letters");

            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith(".bmp"))
                    continue;

                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                StrokeCollection strokes = new StrokeCollection(fileStream);
                DrawingCanvas.Strokes = strokes;
                currentCanvasToBitmap();

                IEnumerable<double> input = readBitmapPoints(bitmap, 10);

                int outputIndex = Array.IndexOf(usedLetters, Path.GetFileNameWithoutExtension(file).Substring(0, 1));

                network.AddLearningPair(input.ToList(), expectedOutputs[outputIndex]);
            }

            network.SingleEraEnded += data =>
            {
                if ((data.CurrentEra + 1) % 10000 == 0 || data.CurrentEra == 0 || data.PercentOfError >= 100)
                {
                    Console.WriteLine(
                        $"Era: {data.CurrentEra + 1}, Learn progress: {data.LearnProgress}, Overall Error: {data.OverallError}" +
                        $", Percentage of error: {data.PercentOfError}");
                }
            };

            network.Learn();
        }

        private void readCanvasClick(object sender, RoutedEventArgs e)
        {
            currentCanvasToBitmap();
            IEnumerable<double> input = readBitmapPoints(bitmap, 10);

            Console.WriteLine();

            foreach (double output in network.GetResultForInputs(input.ToList()))
            {
                Console.WriteLine(output);
            }

            Console.WriteLine();
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
            Console.WriteLine(DrawingCanvas.Strokes.Count);

            Stroke stroke = DrawingCanvas.Strokes[0];
            
            DrawingCanvas.Strokes.Clear();
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


        private IEnumerable<double> readBitmapPoints(Bitmap currentBitmap, int inputNeurons)
        {
            int rightSideStart = 0,
                bottomSideStart = 0,
                topSideStart = currentBitmap.Height,
                leftSideStart = currentBitmap.Width;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    if (!isPixelIsNotWhite(currentBitmap.GetPixel(x, y)))
                        continue;

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

            leftSideStart--;
            topSideStart--;
            rightSideStart++;
            bottomSideStart++;

            Bitmap newBitmap = new Bitmap(200, 200);

            using (Graphics graphics = Graphics.FromImage(newBitmap))
            {
                Rectangle destRegion = new Rectangle(0, 0, 200, 200);
                Rectangle drawRegion = new Rectangle(leftSideStart, topSideStart, rightSideStart - leftSideStart,
                    bottomSideStart - topSideStart);
                graphics.DrawImage(currentBitmap, destRegion, drawRegion, GraphicsUnit.Pixel);
            }

            newBitmap.Save("tempbitmap.bmp");

            List<double> horizontalLines = new List<double>();
            List<double> verticalLines = new List<double>();

            for (int i = 0; i < 200; i += 200 / inputNeurons)
            {
                int counterX = 0;
                int counterY = 0;

                for (int j = 0; j < 200; j += 200 / inputNeurons)
                {
                    if (isPixelIsNotWhite(newBitmap.GetPixel(i, j)))
                    {
                        counterX++;
                        counterY++;
                    }
                }

                horizontalLines.Add(counterX / (double)inputNeurons);
                verticalLines.Add(counterY / (double)inputNeurons);
            }

            List<double> output = new List<double>();
            output.AddRange(horizontalLines);
            output.AddRange(verticalLines);
            
            return output;
        }

        private static bool isPixelIsNotWhite(Color color)
        {
            return color.R != 255 || color.G != 255 || color.B != 255;
        }
    }
}