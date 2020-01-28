using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Accord.Imaging;
using Accord.Imaging.Filters;
using BackpropagationNeuralNetwork;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace BackPropagationGUI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string[] usedSigns = {"30", "60", "zakazwjazdu", "zatrzymywanie", "stop"};
        private Network network;

        private readonly List<List<double>> expectedOutputs = new List<List<double>>()
        {
            new List<double> {1, 0, 0, 0, 0},
            new List<double> {0, 1, 0, 0, 0},
            new List<double> {0, 0, 1, 0, 0},
            new List<double> {0, 0, 0, 1, 0},
            new List<double> {0, 0, 0, 0, 1},
        };

        public MainWindow()
        {
            InitializeComponent();

            LoadButton.Click += loadButtonClick;
            LearnButton.Click += learnButtonClick;
        }

        private void learnButtonClick(object sender, RoutedEventArgs e)
        {
            network = NetworkBuilder.GetBuilder().SetOutputLayerNeurons(5)
                .SetInputLayerNeurons(103).SetMaxEras(100000).SetHiddenLayerNeurons(6)
                .SetLearningRate(0.2f).SetMomentum(0.4f).SetMinError(0.0001).Build();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/Images");

            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith(".bmp"))
                    continue;

                Bitmap bmp = new Bitmap(file);

                IEnumerable<double> input = readBitmapPoints(bmp, 100, Path.GetFileNameWithoutExtension(file));

                int outputIndex = Array.IndexOf(usedSigns, Path.GetFileNameWithoutExtension(file).Split('_')[0]);

                Console.WriteLine("Adding " + file + " as " + usedSigns[outputIndex]);
                
                network.AddLearningPair(input.ToList(), expectedOutputs[outputIndex]);
            }

            network.SingleEraEnded += data =>
            {
                if (data.PercentOfError > 20)
                {
                    network.SetLearningRate(0.18);
                    network.SetMomentum(0.35);
                }
                else if (data.PercentOfError > 50)
                {
                    network.SetLearningRate(0.15);
                    network.SetMomentum(0.3);
                }
                else if (data.PercentOfError > 75)
                {
                    network.SetLearningRate(0.1);
                    network.SetMomentum(0.25);
                }

                if ((data.CurrentEra + 1) % 2000 == 0 || data.CurrentEra == 0 || data.PercentOfError >= 100)
                {
                    Console.WriteLine(
                        $"Era: {data.CurrentEra + 1}, Learn progress: {data.LearnProgress}, Overall Error: {data.OverallError}" +
                        $", Percentage of error: {data.PercentOfError}");
                }
            };

            network.Learn();
        }

        private void loadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Title = "Please select an image file to process.";

            if (dialog.ShowDialog() == true)
            {
                Bitmap bmp = new Bitmap(dialog.FileName);

                IEnumerable<double> input =
                    readBitmapPoints(bmp, 100, Path.GetFileNameWithoutExtension(dialog.FileName));

                int i = 0;

                Console.Clear();
                
                StringBuilder stringBuilder = new StringBuilder();
                
                foreach (double output in network.GetResultForInputs(input.ToList()))
                {
                    stringBuilder.AppendLine(usedSigns[i++] + " - " + Convert.ToDouble(output.ToString("N6")) + "%");
                }

                stringBuilder.AppendLine();
                
                Console.WriteLine(stringBuilder);
            }
        }


        private IEnumerable<double> readBitmapPoints(Bitmap currentBitmap, int inputNeurons, string fileName)
        {
            ImageStatistics imageStatistics = new ImageStatistics(currentBitmap);

            List<double> output = new List<double>();
            
            output.Add(imageStatistics.Red.Min / 255f);
            output.Add(imageStatistics.Red.Max / 255f);
            output.Add(imageStatistics.Red.Mean / 255f);

            currentBitmap = processBitmap(currentBitmap);

            currentBitmap.Save(fileName + ".jpg", ImageFormat.Jpeg);

            List<double> horizontalLines = new List<double>();
            List<double> verticalLines = new List<double>();

            for (int i = 0; i < 200; i += 200 / inputNeurons)
            {
                int counterX = 0;
                int counterY = 0;

                for (int j = 0; j < 200; j += 200 / inputNeurons)
                {
                    if (isPixelNotBlack(currentBitmap.GetPixel(i, j)))
                    {
                        counterX++;
                        counterY++;
                    }
                }

                horizontalLines.Add(counterX / (double) inputNeurons);
                //verticalLines.Add(counterY / (double) inputNeurons);
            }

            output.AddRange(horizontalLines);
            output.AddRange(verticalLines);

            return output;
        }

        private Bitmap processBitmap(Bitmap bitmap)
        {
            Bitmap bitmapToProcess = bitmap.Clone(new Rectangle(bitmap.Width / 10, bitmap.Height / 10, 
                bitmap.Width / 10 * 8, bitmap.Height / 10 * 8), bitmap.PixelFormat);

            bitmapToProcess = Grayscale.CommonAlgorithms.BT709.Apply(bitmapToProcess);
            bitmapToProcess = new Dilation().Apply(bitmapToProcess);
            bitmapToProcess = new Erosion().Apply(bitmapToProcess);
            bitmapToProcess = new CannyEdgeDetector().Apply(bitmapToProcess);
            bitmapToProcess = new Threshold(1).Apply(bitmapToProcess);

            Bitmap bmp = new Bitmap(200, 200);

            using (Graphics g2 = Graphics.FromImage(bmp))
            {
                g2.DrawImage(bitmapToProcess, 20, 20, 160, 160);

                return bmp.Clone(new Rectangle(0, 0, 200, 200), PixelFormat.Format32bppArgb);
            }
        }

        private static bool isPixelNotBlack(Color color)
        {
            return color.R > 0 && color.G > 0 && color.B > 0;
        }
    }
}