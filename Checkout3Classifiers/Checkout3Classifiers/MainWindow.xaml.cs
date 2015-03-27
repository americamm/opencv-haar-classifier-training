using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO;
using System.Drawing; 



namespace Checkout3Classifiers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        //private WriteableBitmap DepthImagenBitmap;
        //private Int32Rect DepthImagenRect;
        //private int DepthImagenStride;
        private byte[] DepthImagenPixeles;
        private short[] DepthValoresStream;
        private Image<Gray, Byte> depthFrameKinect; 
        private CascadeClassifier haar;
        //:::::::::::::fin variables::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        }
        //:::::::::::::end Constructor::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Call Methods::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EncuentraInicializaKinect();
            PollDepth();
        } 
        //:::::::::::::end event::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //:::::::::::::Enseguida estan los metodos para desplegar los datos de profundidad de Kinect:::::::::::::::::::::::::::::::
        private void EncuentraInicializaKinect()
        {
            Kinect = KinectSensor.KinectSensors.FirstOrDefault();

            try
            {
                if (Kinect.Status == KinectStatus.Connected)
                {
                    Kinect.ColorStream.Enable();
                    Kinect.DepthStream.Enable();
                    Kinect.DepthStream.Range = DepthRange.Near;
                    Kinect.Start();
                }
            }
            catch
            {
                MessageBox.Show("El dispositivo Kinect no se encuentra conectado", "Error Kinect");
            }
        } //fin EncuentraKinect()   


        private void PollDepth()
        {
      
            //Bitmap bitmapDepth;

            if (this.Kinect != null)
            {
                DepthImageStream DepthStream = this.Kinect.DepthStream;
                //this.DepthImagenBitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                //this.DepthImagenRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
                //this.DepthImagenStride = DepthStream.FrameWidth * 4;
                this.DepthValoresStream = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength * 4];
                this.depthFrameKinect = new Image<Gray,Byte>(DepthStream.FrameWidth,DepthStream.FrameHeight);

                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.DepthValoresStream);

                            int index = 0;
                            for (int i = 0; i < frame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValoresStream[i] >> 3;

                                if (valorDistancia == this.Kinect.DepthStream.UnknownDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                    DepthImagenPixeles[index + 1] = 0;
                                    DepthImagenPixeles[index + 2] = 0;
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                    DepthImagenPixeles[index + 1] = 0;
                                    DepthImagenPixeles[index + 2] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                    DepthImagenPixeles[index + 1] = byteDistancia;
                                    DepthImagenPixeles[index + 2] = byteDistancia;
                                }
                                index = index + 4;
                            }

                            depthFrameKinect.Bytes = DepthImagenPixeles; 
                            //this.DepthImagenBitmap.WritePixels(this.DepthImagenRect, this.DepthImagenPixeles, this.DepthImagenStride, 0);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }

            }


        }//fin PollDepth()

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.Stop(); 
        }//end unloaded window 
        //:::::::::::::Fin de los metodos para manipular los datos del Kinect:::::::::::::::::::::::::::::



    }//end class
}//end namespace 
