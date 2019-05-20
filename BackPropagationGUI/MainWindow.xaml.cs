using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BackPropagationGUI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap bitmap;

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
            string expectedValue = ExpectedOutputTextBlock.Text;
            currentCanvasToBitmap();
            double[] neuralNetworkInput = readBitmapPoints(bitmap, 10);
        }

        private void readCanvasClick(object sender, RoutedEventArgs e)
        {
            currentCanvasToBitmap();
            readBitmapPoints(bitmap, 10);
        }

        private void loadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.bmp;*.gif)|*.png;*.bmp;*.gif|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = @"C:\Users\Sebastian\source\repos\BackPropagationGUI\BackPropagationGUI\bin\Debug";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    FileName.Text = System.IO.Path.GetFileName(filename);
                    var fs = new FileStream(System.IO.Path.GetFileName(filename),
                        FileMode.Open, FileAccess.Read);
                    StrokeCollection strokes = new StrokeCollection(fs);
                    DrawingCanvas.Strokes = strokes;
                }
            }
        }

        private void saveButtonClick(object sender, RoutedEventArgs e)
        {
            if(FileName.Equals(""))
            {
                Console.WriteLine("PODAJ NAZWE PLIKU DO ZAPISU");
            }
            else
            {
                currentCanvasToBitmap();

                bitmap.Save(FileName.Text + ".bmp");    

                var fs = new FileStream(FileName.Text, FileMode.Create);
                DrawingCanvas.Strokes.Save(fs);
            }   
        }

        private void eraseButtonClick(object sender, RoutedEventArgs e)
        {
            this.DrawingCanvas.Strokes.Clear();
        }

        public void setCanvas(int pencilWidth)
        {
            DrawingCanvas.DefaultDrawingAttributes.Width = pencilWidth;
            DrawingCanvas.DefaultDrawingAttributes.Height = pencilWidth;
        }

        public void currentCanvasToBitmap()
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(DrawingCanvas);

            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            MemoryStream ms = new MemoryStream();
            encoder.Save(ms);

            bitmap = new Bitmap(ms);
        }


        public static double[] readBitmapPoints(Bitmap bitmap, int pointsAmountPerRow)
        {
            int arraySize = (bitmap.Height / pointsAmountPerRow) - 1;
            Console.WriteLine("Rozmiar tablicy: " + arraySize);

            double[] pixelMap = new double[arraySize];

            double rowValue = 0;
            if (bitmap!=null)
            {
                int size = 0;

                for (int i = pointsAmountPerRow; i < bitmap.Height; i += pointsAmountPerRow)
                {

                    for (int x = pointsAmountPerRow; x < bitmap.Width; x += pointsAmountPerRow)
                    {
                        if (bitmap.GetPixel(x, i).R != 255 || bitmap.GetPixel(x, i).G != 255 || bitmap.GetPixel(x, i).B != 255)
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
