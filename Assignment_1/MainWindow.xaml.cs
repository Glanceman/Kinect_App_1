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

namespace Assignment_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KinectSensor = KinectSensor.GetDefault();
            /*
            DepthFrameReader depthFrameReader=KinectSensor.DepthFrameSource.OpenReader();
            depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;
            depthFrameDescription = KinectSensor.DepthFrameSource.FrameDescription;
            depthFrameData = new ushort[depthFrameDescription.LengthInPixels];// each depth pixel carry 13bit <ushort 16bit(2B)
            */

            MultiSourceFrameReader multiSourceFrameReader = KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth);
            multiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReader_MultiSourceFrameArrived;

            MultiSourceFrameReader multi = KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);
            viewData = new byte[depthFrameDescription.LengthInPixels * 4];
            writeableBitmap = new WriteableBitmap(
                depthFrameDescription.Width,
                depthFrameDescription.Height,
                96,96,
                PixelFormats.Bgr32, //each pixel 24bit (3B)+8bit
                null
                );
            KinectView.Source = writeableBitmap; // WriteableBitmap is child of ImageSource
            KinectSensor.Open();

            System.Console.WriteLine(depthFrameDescription.Width+" "+depthFrameDescription.Height);

        }

        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame =)
            {

            }
        }

        /*
        private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null) { return; }
                depthFrame.CopyFrameDataToArray(depthFrameData);
                //write depthFrameData into frameData
                for (int i = 0; i < depthFrameData.Length; i++)
                {
                    ushort temp=depthFrameData[i];
                    viewData[i*4]=(byte)temp;
                    viewData[i*4+1] = (byte)temp;
                    viewData[i*4+2] = (byte)temp;
                }

                //show data to kinectView
                writeableBitmap.WritePixels(
                    new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height),
                    viewData,
                    depthFrameDescription.Width * 4,
                    0
                );
            }
        }
        */

        private KinectSensor KinectSensor;
        private FrameDescription depthFrameDescription;
        private ushort[] depthFrameData;
        private WriteableBitmap writeableBitmap=null;
        private byte[] viewData=null;
    }    
}
