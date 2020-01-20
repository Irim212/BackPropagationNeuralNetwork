using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Accord.Imaging.Filters;
using Accord.Statistics.Filters;
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
        private readonly string[] usedSigns = {"0", "60", "z"};
        private Network network;

        private readonly List<List<double>> expectedOutputs = new List<List<double>>()
        {
            new List<double>() {1, 0, 0},
            new List<double>() {0, 1, 0},
            new List<double>() {0, 0, 1}
        };

        public MainWindow()
        {
            InitializeComponent();
            
            LoadButton.Click += loadButtonClick;
            LearnButton.Click += learnButtonClick;
        }

        private void learnButtonClick(object sender, RoutedEventArgs e)
        {
            network = NetworkBuilder.GetBuilder().SetOutputLayerNeurons(3)
                .SetInputLayerNeurons(20).SetMaxEras(200000).SetHiddenLayerNeurons(5).Build();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/Images");

            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith(".bmp"))
                    continue;

                Bitmap bmp = new Bitmap(file);

                IEnumerable<double> input = readBitmapPoints(bmp, 10, Path.GetFileNameWithoutExtension(file));

                int outputIndex = Array.IndexOf(usedSigns, Path.GetFileNameWithoutExtension(file).Split('_')[0]);

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

        private void loadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Title = "Please select an image file to process.";

            if (dialog.ShowDialog() == true)
            {
                Bitmap bmp = new Bitmap(dialog.FileName);
                
                IEnumerable<double> input = readBitmapPoints(bmp, 10, Path.GetFileNameWithoutExtension(dialog.FileName));

                foreach (double output in network.GetResultForInputs(input.ToList()))
                {
                    Console.WriteLine(output);
                }
            }
        }


        private IEnumerable<double> readBitmapPoints(Bitmap currentBitmap, int inputNeurons, string fileName)
        {
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
                    if (isPixelWhite(currentBitmap.GetPixel(i, j)))
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

        private Bitmap processBitmap(Bitmap bitmap)
        {
            Grayscale grayscale = new Grayscale(1, 1, 1);
            GaborFilter gaborFilter = new GaborFilter();
            gaborFilter.Gamma *= 25;
            gaborFilter.Lambda *= 25;
            gaborFilter.Psi *= 25;
            gaborFilter.Theta *= 25;
                
            BrightnessCorrection brightnessCorrection = new BrightnessCorrection(40);
            Sharpen sharpen = new Sharpen();
            Threshold threshold = new Threshold(140);
            Mean mean = new Mean();
            Median median = new Median();
            
            ContrastCorrection contrastCorrection = new ContrastCorrection(50);
            
            Bitmap returnBitmap = mean.Apply(bitmap);
            returnBitmap = median.Apply(returnBitmap);
            returnBitmap = gaborFilter.Apply(returnBitmap);
            returnBitmap = brightnessCorrection.Apply(returnBitmap);
            returnBitmap = grayscale.Apply(returnBitmap);
            returnBitmap = sharpen.Apply(returnBitmap);
            returnBitmap = contrastCorrection.Apply(returnBitmap);

            Console.WriteLine(Accord.Imaging.Tools.Mean(returnBitmap));
            //returnBitmap = threshold.Apply(returnBitmap);

            Bitmap bmp = new Bitmap(200, 200);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(returnBitmap, 0, 0, 200, 200);

                return bmp.Clone(new Rectangle(0, 0, 200, 200), PixelFormat.Format8bppIndexed);
            }
        }

        private static bool isPixelWhite(Color color)
        {
            return color.R == 255 && color.G == 255 && color.B == 255;
        }
    }
}