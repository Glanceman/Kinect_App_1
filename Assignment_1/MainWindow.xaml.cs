using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;



namespace Assignment_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private KinectSensor KinectSensor;
        private FrameDescription depthFrameDescription;
        private ushort[] depthFrameData;
        private WriteableBitmap depthBitmap = null;
        private byte[] depthBuffer = null;



        int gridSize = 8; //fixed
        int deltaWidth = 0;
        int deltaHeight = 0;
        float[,] avgDistZone;
        private int[,] currentPatternState = null;
        private GridPattern gridPattern = new GridPattern();
        int[,] pattern = null;
        private bool isGameStart = false;
        private bool isMatched = true;
        private Random random = new Random();
        private int currentCount = 0;
        private int playCount = 0;
        private int seed = 0;
        private bool isWaiting = false;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KinectSensor = KinectSensor.GetDefault();

            depthFrameDescription = KinectSensor.DepthFrameSource.FrameDescription;
            depthFrameData = new ushort[depthFrameDescription.LengthInPixels];// each depth pixel carry 13bit <ushort 16bit(2B)


            MultiSourceFrameReader multiSourceFrameReader = KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth);
            multiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReader_MultiSourceFrameArrived;


            depthBuffer = new byte[depthFrameDescription.LengthInPixels * 4];

            depthBitmap = new WriteableBitmap(
                depthFrameDescription.Width,
                depthFrameDescription.Height,
                96, 96,
                PixelFormats.Bgr32, //each pixel 24bit (3B)+8bit
                null);

            deltaWidth = depthFrameDescription.Width / gridSize;
            deltaHeight = depthFrameDescription.Height / gridSize;
            avgDistZone = new float[gridSize, gridSize];
            currentPatternState = new int[gridSize, gridSize];

            //KinectView.Source = colorBitmap; // WriteableBitmap is child of ImageSource
            KinectSensor.Open();

            System.Console.WriteLine(depthFrameDescription.Width + " " + depthFrameDescription.Height);

        }

        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
            {

                if (depthFrame == null) { return; }
                depthFrame.CopyFrameDataToArray(depthFrameData);

                for (int i = 0; i < depthFrameData.Length; i++)
                {
                    ushort depth = depthFrameData[i];

                    ushort minDepth = depthFrame.DepthMinReliableDistance; // 500 
                    ushort maxDepth = (ushort)(depthFrame.DepthMaxReliableDistance - 3000); // 4500 


                    byte depthByte = (byte)(255 - map(depth, minDepth, maxDepth, 0, 255));
                    depthBuffer[i * 4] = depthByte;
                    depthBuffer[i * 4 + 1] = depthByte;
                    depthBuffer[i * 4 + 2] = depthByte;
                }

            }
            depthBitmap.WritePixels(
                 new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height),
                 depthBuffer,
                 depthFrameDescription.Width * 4,
                 0
                );


            BitmapSource bitmapSource = depthBitmap;
            System.Drawing.Bitmap bitmap = BitmapConvertion.ToBitmap(bitmapSource);
            Image<Bgr, byte> DepthImg = bitmap.ToImage<Bgr, byte>();
            Image<Bgr, byte> openCVImg = DepthImg.CopyBlank();


            // cal avg dist

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    //openCVImg.Data[(row * deltaHeight)+deltaHeight/2 ,(col* deltaWidth)+deltaWidth/2 ,1]=255;
                    avgDistZone[row, col] = (DepthImg.Data[(row * deltaHeight) + deltaHeight / 2, (col * deltaWidth) + deltaWidth / 2, 0] + DepthImg.Data[(row * deltaHeight) + deltaHeight / 2, (col * deltaWidth) + deltaWidth / 2, 1] + DepthImg.Data[(row * deltaHeight) + deltaHeight / 2, (col * deltaWidth) + deltaWidth / 2, 2]) / 3;
                }
            }
            // System.Console.WriteLine(depthFrame.DepthMinReliableDistance+" "+ depthFrame.DepthMaxReliableDistance);
            //Print2DArray<float>(avgDistZone);
            if (isGameStart)
            {
                // random pick a pattern 
                if (isMatched == true)
                {
                    int randInt = random.Next(gridPattern.getNumberofPatterns());
                    while (randInt == seed)
                    {
                        randInt = random.Next(gridPattern.getNumberofPatterns());
                    };
                    seed = randInt;
                    pattern = gridPattern.getPattern(seed);
                    isMatched = false;  
                }
            }
            //if (pattern != null) { Print2DArray<int>(pattern); }
            //
            for (int rows = 0; rows < gridSize; rows++)
            {
                for (int cols = 0; cols < gridSize; cols++)
                {
                    float dist = avgDistZone[rows, cols];
                    float val = dist;
                    System.Drawing.PointF rectPos = new System.Drawing.PointF((cols * deltaWidth) + deltaWidth / 2, (rows * deltaHeight) + deltaHeight / 2);
                    System.Drawing.SizeF rectSize = new System.Drawing.SizeF((float)(deltaWidth - deltaWidth * (1 - map(dist, 0, 255, 0, 0.95f))), (float)(deltaHeight - deltaHeight * (1 - map(dist, 0, 255, 0, 0.95f))));
                    Bgr color = new Bgr(val, val, val);
                    //mark the curruent State
                    if (dist < 180)
                    {
                        if (pattern != null && pattern[rows, cols] == 1) //show the pattern
                        {
                            color.Blue = 0;
                            color.Green = 0;
                            color.Red = 255;
                            rectSize.Width = (float)(deltaWidth * 0.5);
                            rectSize.Height = (float)(deltaWidth * 0.5);
                        }
                    }
                    else
                    {
                        if(pattern != null && pattern[rows, cols] == 1)
                        {
                            color.Blue = 0;
                            color.Green = 255;
                            color.Red = 0;
                        }
                        currentPatternState[rows, cols] = 1;
                    }

                    RotatedRect rect = new RotatedRect(rectPos, rectSize, 0);
                    openCVImg.Draw(rect, color, -1);
                }
            }
            bool isPatternMatched = isOverlappedTo(currentPatternState, pattern);
            
            //System.Console.WriteLine("current:"+currentCount);
            //reset currentState
            Array.Clear(currentPatternState, 0, currentPatternState.Length);
            if (isPatternMatched == true)
            {
                //set wait time
                if (isWaiting == false)
                {
                    isWaiting = true;
                    pattern = null; //clear pattern
                    System.Timers.Timer timer = new System.Timers.Timer(1000);
                    timer.Elapsed += (source, eve) =>
                    {
                        timer.Dispose();
                        System.Console.WriteLine("Timer");
                        isWaiting = false;  
                        isMatched = true; //mark the current pattern is matched
                        currentCount=currentCount+1;
                    };
                    timer.Start();
                }
            }

            if (currentCount == playCount) // when player finish, set game to stop and reset count
            {
                isGameStart = false;
                isMatched=true;
                currentCount = 0;
                pattern = null; //clear pattern
                                //output the record time
                stopwatch.Stop();
                Label_TimeTaken.Content = (stopwatch.ElapsedMilliseconds / 1000).ToString()+"s";
                
            }
            // System.Console.WriteLine(temp);
            //update frame
            bitmap = openCVImg.ToBitmap<Bgr, byte>();
            wpfView.Source = BitmapConvertion.ToBitmapSource(bitmap);

        }


        public static void Print2DArray<T>(T[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Console.Write(matrix[i, j] + "\t");
                }
                Console.WriteLine();
            }
        }

        public static bool isOverlappedTo(int[,] matrix, int[,] ref_Matrix)
        {
            if(matrix == null || ref_Matrix == null) { return false; }
            if (matrix.GetLength(0) != ref_Matrix.GetLength(0))
            {
                return false;
            }
            else if (matrix.GetLength(1) != ref_Matrix.GetLength(1))
            {
                return false;
            }

            for (int i = 0; i < ref_Matrix.GetLength(0); i++)
            {
                for (int j = 0; j < ref_Matrix.GetLength(1); j++)
                {
                    if (ref_Matrix[i, j] != 0)
                    {
                        if (matrix[i, j] != ref_Matrix[i, j]) { return false; }
                    }
                }
            }

            return true;
        }
        public static bool compare2DArray<T>(T[,] matrix, T[,] matrix2)
        {
            if (matrix.GetLength(0) != matrix2.GetLength(0))
            {
                return false;
            }
            else if (matrix.GetLength(1) != matrix2.GetLength(1))
            {
                return false;
            }

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(matrix[i, j], matrix2[i, j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static float map(float val, float min, float max, float targetMin, float targetMax)
        {
            if (val < min) { val = min; }
            if (val > max) { val = max; }
            float range = max - min;
            float weight = (val - min) / range;
            return ((targetMax - targetMin) * weight + targetMin);

        }

        private void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            if (isGameStart == false)
            {
                if(int.TryParse(Txt_PlayLoops.Text, out int temp))
                {
                    playCount=temp;
                }
                else
                {
                    MessageBox.Show("Error", "please enter a number");
                    return;
                }
                stopwatch.Start();
                isGameStart = true;
            }
        }
    }
}
