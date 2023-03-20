using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
                //convertedImage.Source = convertedBitmpImage;
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
            applyFilter(myRGBs,1,1,BasicConvolutionFilters.sharpen_s,0,
                BasicConvolutionFilters.sharpen,ref result);
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
                BasicConvolutionFilters.emboss_filters,ref result);

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
            Regex regex = new Regex("[+-]?\b[0-9]+\b");
            if (regex.IsMatch(e.Text[0].ToString()))
            {
                e.Handled = true;
                return;
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
            applyFilter(myRGBs, anchorx, anchory, edit_kernal_s, offset,edit_kernal, ref result);

        }

        private void applyFilter(myRGB[,] initialImage,int anchorx, int anchory,int divisor, int offset,int[,] kernal, ref myRGB[,] result)
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
    }

}
