using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Imaging;



namespace WPF_filter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public class Node
        {
            public bool IsLeaf;
            public uint sumR;
            public uint sumG;
            public uint sumB;
            public uint count;
            public Node[] children;//max 8
            public Node(bool isLeaf, uint sumR, uint sumG, uint sumB, uint count)
            {
                IsLeaf = isLeaf;
                this.sumR = sumR;
                this.sumG = sumG;
                this.sumB = sumB;
                this.count = count;
                this.children = new Node[8];
                for (int i = 0; i < 8; i++)
                {
                    this.children[i] = null;
                }
            }
            public Node()
            {
                this.children = new Node[8];
                for (int i = 0; i < 8; i++)
                {
                    this.children[i] = null;
                }
            }
        }
        public class Octree
        {
            public Node root;
            public byte leafCount;
            public byte maxLeaves;
            public List<Node>[] innerNodes; //non-leaf
            public Octree(Node root, byte leafCount, byte maxLeaves)
            {
                this.root = root;
                this.leafCount = leafCount;
                this.maxLeaves = maxLeaves;
                this.innerNodes = new List<Node>[8];
                for (int i = 0; i < 8; i++)
                {
                    this.innerNodes[i] = new List<Node>();
                    for (int j = 0; j < 8; j++)
                    {
                        //this.innerNodes[i].Add(null);
                    }
                }
            }


        }

        public class customFilter
        {
            public string rowN;
            public string colN;
            public int[,] matrix;
            public bool anchor;
            public string anchorV = "0";
            public bool divisor;
            public string divisorV = "0";
            public bool offset;
            public string offsetV = "";

            public customFilter(string rowN, string colN, int[,] matrix, bool anchor, string anchorV, bool divisor, string divisorV, bool offset, string offsetV)
            {
                this.rowN = rowN;
                this.colN = colN;
                this.matrix = new int[Int32.Parse(rowN), Int32.Parse(colN)];
                this.matrix = matrix.Clone() as int[,];
                this.anchor = anchor;
                this.anchorV = anchorV;
                this.divisor = divisor;
                this.divisorV = divisorV;
                this.offset = offset;
                this.offsetV = offsetV;
            }
        }
        public customFilter[] customFiltersArray = new customFilter[10];
        public int counter = 0;
        public static class Constans
        {
            public const int pixel_size = 4;
            public const int brightness = 20;
            public const int contrast_limit_upper = 20;
            public const int contrast_limit_lower = 20;
            public static int contrast_coefficient_a = (int)Math.Floor((double)(byte.MaxValue /
                (byte.MaxValue - contrast_limit_lower - contrast_limit_upper)));
            public static int contrast_coefficient_b = -1 * contrast_limit_lower * contrast_coefficient_a;
            public const double gamma_corection = 1.7;
        }
        public class BasicConvolutionFilters
        {
            public static int[,] blurr = new int[3, 3];
            public static int[,] gaussian_smoothing = new int[3, 3];
            public static int gaussian_smoothing_s = 0;
            public static int[,] sharpen = new int[3, 3];
            public const int sharpen_b = 5;
            public const int sharpen_a = 1;
            public const int sharpen_s = sharpen_b - 4 * sharpen_a;
            public static int[,] edge_detection = new int[3, 3];
            public static int[,] emboss_filters = new int[3, 3];
            public static int emboss_filters_s = 1;
        }


        public int[,] edit_kernal;
        public int edit_kernal_s = 0;


        private BitmapImage _convertedBitmpImage;
        public BitmapImage convertedBitmpImage
        {
            get { return _convertedBitmpImage; }
            set
            {
                _convertedBitmpImage = value;
                OnPropertyChanged();
            }
        }
        private BitmapImage _originalBitmpImage;
        public BitmapImage originalBitmpImage
        {
            get { return _originalBitmpImage; }
            set
            {
                _originalBitmpImage = value;
            }
        }
        public class myRGB
        {
            public int R;
            public int G;
            public int B;


            public myRGB(int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;

            }
            public override string ToString()
            {
                return R.ToString() + "," + G.ToString() + "," + B.ToString();
            }


        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public BitmapImage BitmapSourceToBitmapImage(BitmapSource bitmapSource)
        {

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bitmapImage = new BitmapImage();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
            bitmapImage.EndInit();
            memoryStream.Close();
            bitmapImage.Freeze();
            return bitmapImage;


        }
        public BitmapImage BitmapImageToBitmapImage(BitmapImage bitmapSource)
        {

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bitmapImage = new BitmapImage();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
            bitmapImage.EndInit();
            memoryStream.Close();
            bitmapImage.Freeze();
            return bitmapImage;

        }
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            convertedBitmpImage = new BitmapImage();
            originalBitmpImage = new BitmapImage();


            for (int i = 0; i < BasicConvolutionFilters.blurr.GetLength(0); i++)
            {
                for (int j = 0; j < BasicConvolutionFilters.blurr.GetLength(1); j++)
                {
                    BasicConvolutionFilters.blurr[i, j] = 1;
                }
            }

            BasicConvolutionFilters.gaussian_smoothing = new int[3, 3] { { 0, 1, 0 }, { 1, 4, 1 }, { 0, 1, 0 } };
            for (int i = 0; i < BasicConvolutionFilters.gaussian_smoothing.GetLength(0); i++)
            {
                for (int j = 0; j < BasicConvolutionFilters.gaussian_smoothing.GetLength(1); j++)
                {
                    BasicConvolutionFilters.gaussian_smoothing_s += BasicConvolutionFilters.gaussian_smoothing[i, j];
                }
            }

            BasicConvolutionFilters.sharpen = new int[3, 3] { { 0, (int)(-1*BasicConvolutionFilters.sharpen_a/
                    BasicConvolutionFilters.sharpen_s), 0 },
                { (int)(-1 * BasicConvolutionFilters.sharpen_a / BasicConvolutionFilters.sharpen_s),
                    (int)(BasicConvolutionFilters.sharpen_b/BasicConvolutionFilters.sharpen_s),
                    (int)(-1*BasicConvolutionFilters.sharpen_a/BasicConvolutionFilters.sharpen_s)},
                { 0, (int)(-1 * BasicConvolutionFilters.sharpen_a / BasicConvolutionFilters.sharpen_s), 0 } };

            BasicConvolutionFilters.edge_detection = new int[3, 3] { { 0, -1, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            BasicConvolutionFilters.emboss_filters = new int[3, 3] { { -1, 0, 1 }, { -1, 1, 1 }, { -1, 0, 1 } };
            controlPanel.IsEnabled = false;


        }

        private void menuLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            //dialog.Filter = "Files|*.jpg;*.jpeg;*.png;";
            dialog.Filter = "Files|*.jpg;*.jpeg;*png;";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string selectedImageName = dialog.FileName;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedImageName);
                bitmap.EndInit();
                originalBitmpImage = bitmap;
                ogiginalImage.Source = originalBitmpImage;

                convertedBitmpImage = BitmapImageToBitmapImage(bitmap);
                controlPanel.IsEnabled = true;
            }
        }

        private void menuSaveImage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog();
            save.Title = "Save picture as ";
            //save.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            save.Filter = "Image Files(*.jpg; *.jpeg; *png;)|*.jpg;*.jpeg;*png;";
            if (convertedBitmpImage != null)
            {
                if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(convertedBitmpImage));
                    using (Stream stm = File.Create(save.FileName))
                    {
                        png.Save(stm);
                    }
                }
            }
        }

        private void menuResetImage_Click(object sender, RoutedEventArgs e)
        {
            convertedBitmpImage = originalBitmpImage;
        }

        private void inversionButtom_Click(object sender, RoutedEventArgs e)
        {

            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);

            for (int i = 0; i < pixels.Length; i++)//
            {
                pixels[i] = (byte)(byte.MaxValue - pixels[i]);
            }
            var result = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight,
                convertedBitmpImage.DpiX, convertedBitmpImage.DpiY, convertedBitmpImage.Format, null, pixels,
                (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(result);
        }

        private void brightness_correctionButton_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (pixels[i] + Constans.brightness) <= (int)(byte.MaxValue)
                    ? (byte)(pixels[i] + Constans.brightness) : (byte)(byte.MaxValue);
            }

            var result = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight,
                convertedBitmpImage.DpiX, convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixels, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(result);
        }

        private void contrast_enhancementButton_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);

            for (int i = 0; i < pixels.Length; i++)//
            {
                pixels[i] = (byte)(contrast_enhancment_color(pixels[i]));
            }

            var result = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight,
                convertedBitmpImage.DpiX, convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixels, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(result);
        }
        private int contrast_enhancment_color(byte pixel)
        {
            if (pixel <= Constans.contrast_limit_lower)
            {
                return 0;
            }
            else if (pixel >= byte.MaxValue - Constans.contrast_limit_upper)
            {
                return 255;
            }
            else
            {
                return (int)(Constans.contrast_coefficient_a * pixel + Constans.contrast_coefficient_b);
            }
        }

        private void gamma_correctionButton_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)Math.Floor((255 * Math.Pow((double)pixels[i] / (double)byte.MaxValue,
                    Constans.gamma_corection)));
            }

            var result = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight,
                convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixels, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(result);
        }

        private void blurButtom_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, 1, 1, 9, 0, BasicConvolutionFilters.blurr, ref result);
        }

        private void gaussian_blurButton_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, 1, 1, BasicConvolutionFilters.gaussian_smoothing_s, 0,
                BasicConvolutionFilters.gaussian_smoothing, ref result);

        }

        private void sharpenButton_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, 1, 1, BasicConvolutionFilters.sharpen_s, 0,
                BasicConvolutionFilters.sharpen, ref result);
        }

        private void edge_detectionButton_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, 1, 1, 1, 0, BasicConvolutionFilters.edge_detection, ref result);

        }

        private void embossButton_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, 1, 1, BasicConvolutionFilters.emboss_filters_s, 0,
                BasicConvolutionFilters.emboss_filters, ref result);

        }

        private void KernalRow_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text[0].ToString()))
            {
                e.Handled = true;
                return;
            }
            if ((int)e.Text[0] % 2 == 0 || (int)e.Text[0] - '0' > 9 || (int)e.Text[0] - '0' <= 0)
                e.Handled = true;
            else if (e.Text[0] - '0' % 2 != 0)
                e.Handled = false;
        }

        private void KernalColumns_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text[0].ToString()))
            {
                e.Handled = true;
                return;
            }
            if ((int)e.Text[0] % 2 == 0 || (int)e.Text[0] - '0' > 9 || (int)e.Text[0] - '0' <= 0)
                e.Handled = true;
            else if (e.Text[0] - '0' % 2 != 0)
                e.Handled = false;
        }
        private void KernalRCell_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Regex regex = new Regex("[+-]?\b[0-9]+\b");
            string pattern = @"-?\d+$";
            if (Regex.IsMatch(e.Text, pattern))
            {
                e.Handled = false;
                //return;
            }
            else
            {
                e.Handled = true;

            }
        }
        private void grayScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Regex regex = new Regex("[+-]?\b[0-9]+\b");
            string pattern = @"^[1-9]\d*$";
            if (Regex.IsMatch(e.Text, pattern))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;

            }
        }
        private void pixelize_Click(object sender, RoutedEventArgs e)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);
            myRGB[] myRGBs_helper = new myRGB[(int)((pixels.Length) / 4)];
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];

            int j = 0;
            for (int i = 0; i < pixels.Length; i = i + 4)
            {
                myRGBs_helper[j] = new myRGB(pixels[i], pixels[i + 1], pixels[i + 2]);
                j++;
            }
            for (int i = 0; i < convertedBitmpImage.PixelHeight; i++)
            {
                for (j = 0; j < (int)convertedBitmpImage.Width; j++)
                {
                    myRGBs[i, j] = myRGBs_helper[i * (int)convertedBitmpImage.Width + j];
                }
            }
            myRGB[,] myRGBs1 = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            myRGB sum = new myRGB(0, 0, 0);
            int a = 0;
            int b = 0;
            for (int i = 0; i < myRGBs.GetLength(0) - 16; i += 16)
            {
                a += 16;
                for (j = 0; j < myRGBs.GetLength(1) - 16; j += 16)
                {

                    for (int k = 0; k < 16; k++)
                    {
                        for (int l = 0; l < 16; l++)
                        {

                            sum.B = sum.B + (myRGBs[i + k, j + l].B);
                            sum.R = sum.R + (myRGBs[i + k, j + l].R);
                            sum.G = sum.G + (myRGBs[i + k, j + l].G);
                        }
                    }

                    sum.B = sum.B / 256 < 255 ? sum.B / 256 : 255;
                    sum.R = sum.R / 256 < 255 ? sum.R / 256 : 255;
                    sum.G = sum.G / 256 < 255 ? sum.G / 256 : 255;

                    for (int k = 0; k < 16; k++)
                    {
                        for (int l = 0; l < 16; l++)
                        {
                            result[i + k, j + l] = new myRGB(sum.R > 0 ? sum.R : 0, sum.G > 0 ? sum.G : 0, sum.B > 0 ? sum.B : 0);
                        }
                    }

                    sum.B = 0;
                    sum.R = 0;
                    sum.G = 0;
                    if (b < j)
                    {
                        b = j;
                    }
                }
            }
            int z = 0;
            b += 16;
            byte[] pixelsv2 = new byte[(int)a * 4 * b * 4];

            for (int i = 0; i < a; i++)
            {
                for (j = 0; j < b; j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;

                    z++;
                }
            }
            var tmp = BitmapSource.Create(b, a, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)b * 4);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }

        private void KernalButton_Click(object sender, RoutedEventArgs e)
        {
            kernalGrid.Children.Clear();
            kernalGrid.RowDefinitions.Clear();
            kernalGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < (char)KernalColumns.Text[0] - '0'; i++)
                kernalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < (char)KernalRow.Text[0] - '0'; i++)
                kernalGrid.RowDefinitions.Add(new RowDefinition());
            var tmp = new string[(char)KernalRow.Text[0] - '0', (char)KernalColumns.Text[0] - '0'];
            int k = 0;
            for (int i = 0; i < (char)KernalRow.Text[0] - '0'; i++)
            {
                for (int j = 0; j < (char)KernalColumns.Text[0] - '0'; j++)
                {
                    TextBox txt1 = new TextBox();
                    txt1.Text = "0";
                    txt1.PreviewTextInput += KernalRCell_PreviewTextInput;
                    txt1.MaxLength = 3;
                    txt1.Name = "cell" + k.ToString();
                    tmp[i, j] = txt1.Name;
                    txt1.Width = 20;
                    txt1.Height = 20;
                    Grid.SetRow(txt1, i);
                    Grid.SetColumn(txt1, j);
                    kernalGrid.Children.Add(txt1);
                    k++;
                }
            }
            edit_kernal = new int[(char)KernalRow.Text[0] - '0', (char)KernalColumns.Text[0] - '0'];
        }

        private void move_point_to_border(ref int x, ref int y, int max_x, int max_y)
        {
            x = Math.Max(x, 0);
            y = Math.Max(y, 0);
            x = Math.Min(x, max_x - 1);
            y = Math.Min(y, max_y - 1);
        }
        private void Use_Click(object sender, RoutedEventArgs e)
        {
            int v = 0;
            var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
            for (int i = 0; i < (char)KernalRow.Text[0] - '0'; i++)
            {
                for (int c = 0; c < (char)KernalColumns.Text[0] - '0'; c++)
                {
                    edit_kernal[i, c] = Int32.Parse(textboxes[v].Text);

                    v++;
                }
            }
            if (checkBoxKernalDivisor.IsChecked == false)
            {
                edit_kernal_s = 0;
                for (int i = 0; i < edit_kernal.GetLength(0); i++) //delete
                {
                    for (int c = 0; c < edit_kernal.GetLength(1); c++)
                    {
                        edit_kernal_s += edit_kernal[i, c];
                    }
                }
            }
            else
                edit_kernal_s = Int32.Parse(KernalDivisor.Text);

            int offset;
            if (checkBoxKernalOffset.IsChecked == false)
            {
                offset = 0;
            }
            else
                offset = Int32.Parse(KernalOffset.Text);

            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);

            int anchory = Int32.Parse(this.KernalColumns.Text) / 2;
            int anchorx = Int32.Parse(this.KernalRow.Text) / 2;
            if (checkBoxKernalAnchor.IsChecked == true)
            {
                string[] strings = this.KernalAnchor.Text.Split(',');
                anchorx = Int32.Parse(strings[1]);
                anchory = Int32.Parse(strings[0]);
            }

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            applyFilter(myRGBs, anchorx, anchory, edit_kernal_s, offset, edit_kernal, ref result);

        }

        private void applyFilter(myRGB[,] initialImage, int anchorx, int anchory, int divisor, int offset, int[,] kernal, ref myRGB[,] result)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            for (int x = 0; x < initialImage.GetLength(0); x++)
            {
                for (int y = 0; y < initialImage.GetLength(1); y++)
                {
                    myRGB sum = new myRGB(0, 0, 0);

                    for (int i = 0; i < kernal.GetLength(0); ++i)
                    {
                        for (int j = 0; j < kernal.GetLength(1); ++j)
                        {

                            int xp = x + i - anchorx, yp = y + j - anchory;

                            move_point_to_border(ref xp, ref yp, initialImage.GetLength(0), initialImage.GetLength(1));

                            sum.B = sum.B + (kernal[i, j] * initialImage[xp, yp].B);
                            sum.R = sum.R + (kernal[i, j] * initialImage[xp, yp].R);
                            sum.G = sum.G + (kernal[i, j] * initialImage[xp, yp].G);
                        }
                    }

                    sum.B = (sum.B / divisor) + offset < 255 ? sum.B / divisor + offset : 255;
                    sum.R = (sum.R / divisor) + offset < 255 ? sum.R / divisor + offset : 255;
                    sum.G = (sum.G / divisor) + offset < 255 ? sum.G / divisor + offset : 255;

                    result[x, y] = new myRGB(sum.R > 0 ? sum.R : 0, sum.G > 0 ? sum.G : 0, sum.B > 0 ? sum.B : 0);

                }
            }
            int z = 0;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var tmp = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }

        private void imageToPixet2dArray(ref myRGB[,] array2d)
        {
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixels = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            convertedBitmpImage.CopyPixels(pixels, (int)stride, 0);
            myRGB[] myRGBs_helper = new myRGB[(int)((pixels.Length) / 4)];

            for (int i = 0, h = 0; i < pixels.Length; i = i + 4, h++)
            {
                myRGBs_helper[h] = new myRGB(pixels[i], pixels[i + 1], pixels[i + 2]);

            }
            for (int i = 0; i < convertedBitmpImage.PixelHeight; i++)
            {
                for (int j = 0; j < (int)convertedBitmpImage.Width; j++)
                {
                    array2d[i, j] = myRGBs_helper[i * (int)convertedBitmpImage.Width + j];
                }
            }
        }

        private void loadFliter_Click(object sender, RoutedEventArgs e)
        {

            //blur
            if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[0])
            {
                this.KernalColumns.Text = 3.ToString();
                this.KernalRow.Text = 3.ToString();
                this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        textboxes[v].Text = BasicConvolutionFilters.blurr[i, j].ToString();
                        v++;
                    }
                }

                checkBoxKernalAnchor.IsChecked = false;
                checkBoxKernalDivisor.IsChecked = false;
                checkBoxKernalOffset.IsChecked = false;
            }

            // Sharpen
            if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[1])
            {
                this.KernalColumns.Text = 3.ToString();
                this.KernalRow.Text = 3.ToString();
                this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        textboxes[v].Text = BasicConvolutionFilters.sharpen[i, j].ToString();
                        v++;
                    }
                }

                checkBoxKernalAnchor.IsChecked = false;
                checkBoxKernalDivisor.IsChecked = true;
                KernalDivisor.Text = BasicConvolutionFilters.sharpen_s.ToString();
                checkBoxKernalOffset.IsChecked = false;
            }
            // Emboss
            if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[2])
            {
                this.KernalColumns.Text = 3.ToString();
                this.KernalRow.Text = 3.ToString();
                this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        textboxes[v].Text = BasicConvolutionFilters.emboss_filters[i, j].ToString();
                        v++;
                    }
                }

                checkBoxKernalAnchor.IsChecked = false;
                checkBoxKernalDivisor.IsChecked = true;
                KernalDivisor.Text = BasicConvolutionFilters.emboss_filters_s.ToString();
                checkBoxKernalOffset.IsChecked = false;
            }
            //Gaussian
            if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[3])
            {
                this.KernalColumns.Text = 3.ToString();
                this.KernalRow.Text = 3.ToString();
                this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        textboxes[v].Text = BasicConvolutionFilters.gaussian_smoothing[i, j].ToString();
                        v++;
                    }
                }

                checkBoxKernalAnchor.IsChecked = false;
                checkBoxKernalDivisor.IsChecked = true;
                KernalDivisor.Text = BasicConvolutionFilters.gaussian_smoothing_s.ToString();
                checkBoxKernalOffset.IsChecked = false;
            }
            // Edge detection
            if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[4])
            {
                this.KernalColumns.Text = 3.ToString();
                this.KernalRow.Text = 3.ToString();
                this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        textboxes[v].Text = BasicConvolutionFilters.edge_detection[i, j].ToString();
                        v++;
                    }
                }

                checkBoxKernalAnchor.IsChecked = false;
                checkBoxKernalDivisor.IsChecked = true;
                KernalDivisor.Text = 1.ToString();
                checkBoxKernalOffset.IsChecked = false;
            }
            if (comboBoxFilters.Items.Count > 5)
            {
                for (int l = 5; l < comboBoxFilters.Items.Count; l++)
                {
                    if (comboBoxFilters.SelectedItem == comboBoxFilters.Items[l])
                    {
                        this.KernalColumns.Text = customFiltersArray[l - 5].colN;
                        this.KernalRow.Text = customFiltersArray[l - 5].rowN;
                        this.KernalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
                        int v = 0;
                        for (int i = 0; i < Int32.Parse(customFiltersArray[l - 5].rowN); i++)
                        {
                            for (int j = 0; j < Int32.Parse(customFiltersArray[l - 5].colN); j++)
                            {
                                textboxes[v].Text = customFiltersArray[l - 5].matrix[i, j].ToString();
                                v++;
                            }
                        }

                        checkBoxKernalAnchor.IsChecked = customFiltersArray[l - 5].anchor;
                        KernalAnchor.Text = customFiltersArray[l - 5].anchorV;
                        checkBoxKernalDivisor.IsChecked = customFiltersArray[l - 5].divisor;
                        KernalDivisor.Text = customFiltersArray[l - 5].divisorV;
                        checkBoxKernalOffset.IsChecked = customFiltersArray[l - 5].offset;
                        KernalOffset.Text = customFiltersArray[l - 5].offsetV;
                    }
                }
            }

        }

        private void saveFilter_Click(object sender, RoutedEventArgs e)
        {
            int[,] matrix = new int[Int32.Parse(KernalRow.Text), Int32.Parse(KernalColumns.Text)];
            var textboxes = this.kernalGrid.Children.OfType<TextBox>().ToArray();
            int v = 0;
            for (int i = 0; i < Int32.Parse(KernalRow.Text); i++)
            {
                for (int j = 0; j < Int32.Parse(KernalColumns.Text); j++)
                {
                    matrix[i, j] = Int32.Parse(textboxes[v].Text);
                    v++;
                }
            }
            customFiltersArray[counter] = new customFilter(KernalRow.Text, KernalColumns.Text, matrix,
                (bool)checkBoxKernalAnchor.IsChecked, KernalAnchor.Text, (bool)checkBoxKernalDivisor.IsChecked,
                KernalDivisor.Text, (bool)checkBoxKernalOffset.IsChecked, KernalOffset.Text);

            ComboBoxItem tmp = new ComboBoxItem();
            tmp.Content = counter.ToString() + " Your filter";
            counter++;
            comboBoxFilters.Items.Add(tmp);
        }

        private void grayscaleButton_Click(object sender, RoutedEventArgs e)
        {
            this.gamma_correctionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];

            for (int i = 0; i < myRGBs.GetLength(0); i++)
            {
                for (int j = 0; j < myRGBs.GetLength(1); j++)
                {
                    int brightness = (int)(0.2126 * myRGBs[i, j].R + 0.7152 * myRGBs[i, j].G + 0.0722 * myRGBs[i, j].B);


                    result[i, j] = new myRGB(brightness > 0 ? brightness : 0, brightness > 0 ? brightness : 0, brightness > 0 ? brightness : 0);

                }
            }
            int z = 0;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var tmp = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }
        private float get_mean(int left, int right, ref int[] array)
        {
            float mean = 0;

            for (int i = left, n = 0; i <= right; i++)
            {
                int prev = n;
                n += array[i];
                mean += ((float)i / Math.Max(1, prev)) * (float)array[i];
                mean /= Math.Max(1, n);
                mean *= Math.Max(1, prev);
            }
            return mean;
        }
        private void threshold(int left, int right, int n, ref List<int> thresholds, ref int[] array)
        {
            if (n == 1)
            {
                return;
            }
            float mean = get_mean(left, right, ref array);
            int limit = (int)Math.Round(mean);
            thresholds.Add(limit);
            threshold(left, limit, n / 2, ref thresholds, ref array);
            threshold(Math.Min(limit + 1, 255), right, n / 2, ref thresholds, ref array);
        }

        private void averageDitheringButton_Click(object sender, RoutedEventArgs e)
        {
            if (grayImageDitheringCheckBox.IsChecked == true)
            {
                numberOfColour_B_TextBox.Text = numberOfColour_G_TextBox.Text;
                numberOfColour_R_TextBox.Text = numberOfColour_G_TextBox.Text;
            }


            if (Int32.Parse(numberOfColour_R_TextBox.Text) > 255 | Int32.Parse(numberOfColour_G_TextBox.Text) > 255 |
                Int32.Parse(numberOfColour_B_TextBox.Text) > 255)
            {
                var massage = MessageBox.Show("Too much for mee!!!");
                return;
            }
            if (!((Int32.Parse(numberOfColour_R_TextBox.Text) != 0)
                && ((Int32.Parse(numberOfColour_R_TextBox.Text) &
                (Int32.Parse(numberOfColour_R_TextBox.Text) - 1)) == 0)) |
                !((Int32.Parse(numberOfColour_G_TextBox.Text) != 0)
                && ((Int32.Parse(numberOfColour_G_TextBox.Text) &
                (Int32.Parse(numberOfColour_G_TextBox.Text) - 1)) == 0)) |
                !((Int32.Parse(numberOfColour_B_TextBox.Text) != 0)
                && ((Int32.Parse(numberOfColour_B_TextBox.Text) &
                (Int32.Parse(numberOfColour_B_TextBox.Text) - 1)) == 0)))
            {
                var massage = MessageBox.Show("Don't you know the powers of two!!!");
                return;
            }

            myRGB[,] myRGBs = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            int[] arrayR = new int[256];
            int[] arrayG = new int[256];
            int[] arrayB = new int[256];

            for (int i = 0; i < 256; i++)
            {
                arrayR[i] = 0;
                arrayG[i] = 0;
                arrayB[i] = 0;
            }
            for (int i = 0; i < myRGBs.GetLength(0); i++)
            {
                for (int j = 0; j < myRGBs.GetLength(1); j++)
                {
                    arrayR[myRGBs[i, j].B]++;
                    arrayG[myRGBs[i, j].B]++;
                    arrayB[myRGBs[i, j].B]++;

                }
            }
            List<int> listR = new List<int>();
            List<int> listG = new List<int>();
            List<int> listB = new List<int>();

            threshold(0, 255, Int32.Parse(numberOfColour_R_TextBox.Text), ref listR, ref arrayR);
            threshold(0, 255, Int32.Parse(numberOfColour_G_TextBox.Text), ref listG, ref arrayG);
            threshold(0, 255, Int32.Parse(numberOfColour_B_TextBox.Text), ref listB, ref arrayB);
            listR.Sort();
            listG.Sort();
            listB.Sort();
            listR.Add(255);
            listG.Add(255);
            listB.Add(255);
            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    result[i, j] = new myRGB(0, 0, 0);
                    for (int k = 0; k < listR.Count; k++)
                    {
                        if (myRGBs[i, j].R <= listR[k])
                        {

                            result[i, j].R = (int)(255 / (listR.Count-1)*k);
                            break;
                        }
                    }
                    for (int k = 0; k < listG.Count; k++)
                    {
                        if (myRGBs[i, j].G <= listG[k])
                        {
                            result[i, j].G = (int)(255 / (listG.Count - 1) * k);
                            break;
                        }
                    }
                    for (int k = 0; k < listB.Count; k++)
                    {
                        if (myRGBs[i, j].B <= listB[k])
                        {
                            result[i, j].B = (int)(255 / (listB.Count - 1) * k);
                            break;
                        }
                    }
                }
            }

            int z = 0;
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var rez = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);
            convertedBitmpImage = BitmapSourceToBitmapImage(rez);
        }

        private void grayImageDitheringCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            numberOfColour_B_TextBox.IsEnabled = false;
            numberOfColour_R_TextBox.IsEnabled = false;
        }

        private void grayImageDitheringCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            numberOfColour_B_TextBox.IsEnabled = true;
            numberOfColour_R_TextBox.IsEnabled = true;
        }

        private uint childIndex(myRGB color, uint depth)
        {
            int byteR = (color.R >> (int)(7 - depth) & 0x1);
            int byteG = (color.G >> (int)(7 - depth) & 0x1);
            int byteB = (color.B >> (int)(7 - depth) & 0x1);

            return (uint)(byteR << 2 | byteG << 1 | byteB);
        }
        private Node createNode(ref Octree octree, uint depth)
        {
            var newNode = new Node();
            newNode.IsLeaf = (depth == 8);
            if (!newNode.IsLeaf)
            {
                octree.innerNodes[depth].Add(newNode);
            }
            else
            {
                octree.leafCount++;
            }

            return newNode;
        }

        private void add(ref Octree octree, myRGB color)
        {
            if (octree.root == null)
            {
                octree.root = createNode(ref octree, 0);

            }
            addRecursive(ref octree, ref octree.root, color, 0);
        }
        private void addRecursive(ref Octree octree, ref Node parent, myRGB color, uint depth)
        {
            if (parent.IsLeaf)
            {
                parent.sumR += (uint)color.R;
                parent.sumG += (uint)color.G;
                parent.sumB += (uint)color.B;
                parent.count = parent.count + 1;
            }
            else
            {
                var i = childIndex(color, depth);
                if (parent.children[i] == null)
                {
                    parent.children[i] = createNode(ref octree, depth + 1);
                }
                addRecursive(ref octree, ref parent.children[i], color, depth + 1);
            }
        }

        private myRGB find(ref Octree octree, myRGB myRGB)
        {
            var node = octree.root;
            int z = 0;
            while (!(node.IsLeaf))
            {

                var i = childIndex(myRGB, (uint)z);
                node = node.children[i];
                ++z;
            }
            return new myRGB((int)Math.Floor((double)node.sumR / (double)node.count),
                (int)Math.Floor((double)node.sumG / (double)node.count), (int)Math.Floor((double)node.sumB / (double)node.count));
        }
        public void ReduceTree(ref Octree octree)
        {
            int z = 8;
            for (int i = 7; i >= 0; i--)
            {
                z--;
                if ((octree.innerNodes[i].Any<Node>() == true))
                    break;

            }

            Node node = new Node();
            for (int j = 0; j < octree.innerNodes[z].Count(); j++)
            {
                if ((octree.innerNodes[z][j] != null))
                {
                    node = octree.innerNodes[z][j];
                    octree.innerNodes[z].RemoveAt(j);
                    break;
                }
            }
            int removed = 0;
            for (int k = 0; k < 8; k++)
            {
                if (!(node.children[k] == null))
                {
                    node.sumR += node.children[k].sumR;
                    node.sumG += node.children[k].sumG;
                    node.sumB += node.children[k].sumB;
                    node.count += node.children[k].count;
                    node.children[k] = null;
                    removed++;
                }
            }
            node.IsLeaf = true;
            octree.leafCount += (byte)(1 - removed);
            //Node node = null;
            //for (int i = 7; i >= 0; i--)
            //{

            //    if (octree.innerNodes[i].Any<Node>() == true)
            //    {                   
            //        for (int j = 0; j < octree.innerNodes[i].Count; j++)
            //        {
            //            if (octree.innerNodes[i][j] != null)
            //            {
            //                node = octree.innerNodes[i][j];
            //                octree.innerNodes[i].RemoveAt(j);
            //                break;
            //            }
            //        }
            //    }
            //    if (node != null)
            //        break;
            // }
            //        int removed = 0;
            //        for (int k = 0; k < 8; k++)
            //        {
            //            if (node.children[k] != null)
            //            {
            //                node.sumR += node.children[k].sumR;
            //                node.sumG += node.children[k].sumG;
            //                node.sumB += node.children[k].sumB;
            //                node.count += node.children[k].count;
            //                node.children[k] = null;
            //                removed++;
            //            }
            //        }
            //        node.IsLeaf = true;
            //        octree.leafCount += (byte)(1 - removed);



        }
        private void octreeColor_Button_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[(int)convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);
            ;
            var octree = new Octree(root: null, leafCount: 0, maxLeaves: (byte)Int32.Parse(octreeColor_TextBox.Text));
            for (int i = 0; i < myRGBs.GetLength(0); i++)
            {
                for (int j = 0; j < myRGBs.GetLength(1); j++)
                {

                    add(ref octree, myRGBs[i, j]);
                    while (octree.leafCount > (byte)Int32.Parse(octreeColor_TextBox.Text))
                    {
                        ReduceTree(ref octree);
                    }


                }
            }

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            int ff = 0;
            for (int i = 0; i < myRGBs.GetLength(0); i++)
            {
                for (int j = 0; j < myRGBs.GetLength(1); j++)
                {
                    result[i, j] = find(ref octree, myRGBs[i, j]);
                    if (result[i, j].R != myRGBs[i, j].R || result[i, j].G != myRGBs[i, j].G || result[i, j].B != myRGBs[i, j].B)
                    {
                        ff++;
                    }
                }
            }

            int z = 0;
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var tmp = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }

        private void rgbToYcbcr_Button_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[(int)convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            for(int i = 0; i < myRGBs.GetLength(0); ++i)
            {
                for (int j = 0; j < myRGBs.GetLength(1); ++j)
                {
                    result[i, j] = new myRGB(0, 0, 0);
                    ////Y
                    //result[i, j].R = 16 + (((myRGBs[i, j].R << 6) + (myRGBs[i, j].R << 1) + (myRGBs[i, j].G << 7) + myRGBs[i, j].G + (myRGBs[i, j].B << 4) + (myRGBs[i, j].B << 3) + myRGBs[i, j].B) >> 8);
                    ////Cb
                    //result[i, j].G = 128 + ((-((myRGBs[i, j].R << 5) + (myRGBs[i, j].R << 2) + (myRGBs[i, j].R << 1)) - ((myRGBs[i, j].G << 6) + (myRGBs[i, j].G << 3) + (myRGBs[i, j].G << 1)) + (myRGBs[i, j].B << 7) - (myRGBs[i, j].B << 4)) >> 8);
                    ////Cr
                    //result[i, j].B = 128 + (((myRGBs[i, j].R << 7) - (myRGBs[i, j].R << 4) - ((myRGBs[i, j].G << 6) + (myRGBs[i, j].G << 5) - (myRGBs[i, j].G << 1)) - ((myRGBs[i, j].B << 4) + (myRGBs[i, j].B << 1))) >> 8);

                    result[i, j].R = (int)((0.299 * myRGBs[i, j].R) + (0.587 * myRGBs[i, j].G) + (0.114 * myRGBs[i, j].B));
                    result[i, j].G = (int)(128 - (0.168736 * myRGBs[i, j].R) - (0.331264 * myRGBs[i, j].G) + (0.5 * myRGBs[i, j].B));
                    result[i, j].B = (int)(128 + (0.5 * myRGBs[i, j].R) - (0.418688 *
                        myRGBs[i, j].G) - (0.081312 * myRGBs[i, j].B));



                    result[i, j].R = (int)Math.Min(result[i, j].R, 255);
                    result[i, j].R = (int)Math.Max(result[i, j].R, 0);
                    result[i, j].G = (int)Math.Min(result[i, j].G, 255);
                    result[i, j].G = (int)Math.Max(result[i, j].G, 0);
                    result[i, j].B = (int)Math.Min(result[i, j].B, 255);
                    result[i, j].B = (int)Math.Max(result[i, j].B, 0);
                }
            }
            int z = 0;
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var tmp = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }

        private void ycbcrToRgb_Click(object sender, RoutedEventArgs e)
        {
            myRGB[,] myRGBs = new myRGB[(int)convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            imageToPixet2dArray(ref myRGBs);

            myRGB[,] result = new myRGB[convertedBitmpImage.PixelHeight, (int)convertedBitmpImage.Width];
            for (int i = 0; i < myRGBs.GetLength(0); ++i)
            {
                for (int j = 0; j < myRGBs.GetLength(1); ++j)
                {
                    result[i, j] = new myRGB(0, 0, 0);
                    double y = (double)myRGBs[i, j].R;
                    double Cr = (double)myRGBs[i, j].B-128;
                    double Cb = (double)myRGBs[i, j].G-128;
                    //result[i, j].R = myRGBs[i, j].R + (Cr + Cr >> 2 + Cr >> 3 + Cr >> 5);
                    //result[i, j].B = myRGBs[i, j].R - (Cb >> 2 + Cb >> 4 + Cb >> 5) - (Cr >> 1 + Cr >> 3 + Cr >> 4 + Cr >> 5);
                    //result[i, j].G = myRGBs[i, j].R + (Cb + Cb >> 1 + Cb >> 2 + Cb >> 6);
                    result[i, j].R = (int)(y + (1.402 * Cr));
                    result[i, j].G = (int)(y - (0.3441 * Cb) - (0.7141 * Cr));
                    result[i, j].B = (int)(y + (1.772 * Cb));
                    result[i, j].R = (int)Math.Min(result[i, j].R, 255);
                    result[i, j].R = (int)Math.Max(result[i, j].R, 0);
                    result[i, j].G = (int)Math.Min(result[i, j].G, 255);
                    result[i, j].G = (int)Math.Max(result[i, j].G, 0);
                    result[i, j].B = (int)Math.Min(result[i, j].B, 255);
                    result[i, j].B = (int)Math.Max(result[i, j].B, 0);
                }
            }
            int z = 0;
            var stride = convertedBitmpImage.Width * Constans.pixel_size;
            byte[] pixelsv2 = new byte[(int)(stride) * convertedBitmpImage.PixelHeight];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    pixelsv2[z] = (byte)result[i, j].R;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].G;
                    z++;
                    pixelsv2[z] = (byte)result[i, j].B;
                    z++;
                    pixelsv2[z] = (byte)255;
                    z++;
                }
            }
            var tmp = BitmapSource.Create(convertedBitmpImage.PixelWidth, convertedBitmpImage.PixelHeight, convertedBitmpImage.DpiX,
                convertedBitmpImage.DpiY, convertedBitmpImage.Format,
                null, pixelsv2, (int)stride);

            convertedBitmpImage = BitmapSourceToBitmapImage(tmp);
        }
    }

}
