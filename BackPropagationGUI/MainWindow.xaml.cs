using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BackpropagationNeuralNetwork;
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
            new List<double>(){1, 0, 0, 0},
            new List<double>(){0, 1, 0, 0},
            new List<double>(){0, 0, 1, 0},
            new List<double>(){0, 0, 0, 1}
        };
        
        private Bitmap bitmap;

        public MainWindow()
        {
            InitializeComponent();
            setCanvas(10);

            EraseButton.Click += eraseButtonClick;
            SaveButton.Click += saveButtonClick;
            LoadButton.Click += loadButtonClick;
            ReadCanvas.Click += readCanvasClick;
            LearnButton.Click += learnButtonClick;
        }

        private void learnButtonClick(object sender, RoutedEventArgs e)
        {
            network = NetworkBuilder.GetBuilder().SetOutputLayerNeurons(4)
                .SetInputLayerNeurons(18).Build();

            string[] files =  Directory.GetFiles(Directory.GetCurrentDirectory() + "/letters");

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
                Console.WriteLine($"Era: {data.CurrentEra}, Learn progress: {data.LearnProgress}, Overall Error: {data.OverallError}" +
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


        private static double[] readBitmapPoints(Bitmap bitmap, int pointsAmountPerRow)
        {
            int arraySize = ((bitmap.Height / pointsAmountPerRow) - 1) * 2;
            Console.WriteLine("Rozmiar tablicy: " + arraySize);
            
            double[] pixelMap = new double[arraySize];

            double rowValue = 0;
            
            if (bitmap != null)
            {
                int size = 0;

                for (int i = pointsAmountPerRow; i < bitmap.Height; i += pointsAmountPerRow)
                {
                    for (int x = pointsAmountPerRow; x < bitmap.Width; x += pointsAmountPerRow)
                    {
                        if (bitmap.GetPixel(x, i).R != 255 || bitmap.GetPixel(x, i).G != 255 ||
                            bitmap.GetPixel(x, i).B != 255)
                        {
                            rowValue += 1.0;
                        }
                    }

                    pixelMap[size] = rowValue / ((bitmap.Width / pointsAmountPerRow) - 1);
                    Console.WriteLine("Wartość dla tablicy pixelMap[" + size + "] = " + pixelMap[size]);

                    size++;

                    rowValue = 0f;
                }
                
                for (int i = pointsAmountPerRow; i < bitmap.Height; i += pointsAmountPerRow)
                {
                    for (int x = pointsAmountPerRow; x < bitmap.Width; x += pointsAmountPerRow)
                    {
                        if (bitmap.GetPixel(i, x).R != 255 || bitmap.GetPixel(i, x).G != 255 ||
                            bitmap.GetPixel(i, x).B != 255)
                        {
                            rowValue += 1.0;
                        }
                    }

                    pixelMap[size] = rowValue / ((bitmap.Width / pointsAmountPerRow) - 1);
                    Console.WriteLine("Wartość dla tablicy pixelMap[" + size + "] = " + pixelMap[size]);

                    size++;

                    rowValue = 0f;
                }

                return pixelMap;
            }
            else
            {
                return null;
            }

            return null;
        }
    }
}