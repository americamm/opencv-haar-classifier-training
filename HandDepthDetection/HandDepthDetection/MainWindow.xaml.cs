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
using Emgu.Util;
using Emgu.CV.CvEnum; 
using Emgu.CV.Structure;
using System.Drawing;
using System.IO; 



namespace HandDepthDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private WriteableBitmap DepthImagenBitmap;
        private Int32Rect DepthImagenRect;
        private int DepthImagenStride;
        private byte[] DepthImagenPixeles;
        private short[] DepthValores;
        bool grabacion = false;
        bool grabaImagen = true; 
        List<WriteableBitmap> imagenesDepth = new List<WriteableBitmap>();

        //private HaarCascade haar;
        private CascadeClassifier haar;
        //:::::::::::::fin variables::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 

        
        //:::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        }
        //:::::::::::::Fin Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //::::::::::::Se realizan los metodos cuando se carga la ventana:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //haar = new HaarCascade(@"C:\Users\America\Documents\opencv-haar-clasisifier-training\classifier\cascade.xml");
            //haar = new CascadeClassifier(@"C:\Users\America\Documents\opencv-haar-clasisifier-training\classifier\cascade.xml"); 
            EncuentraInicializaKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);  
        }
        //::::::::::::end windows loaded:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


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


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
           DepthImage.Source = PollDepth();
        } //fin CompositionTarget_Rendering() 


        private WriteableBitmap PollDepth()
        {
            Bitmap bitmapDepth; 

            if (this.Kinect != null)
            {
                DepthImageStream DepthStream = this.Kinect.DepthStream;
                this.DepthImagenBitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this.DepthImagenRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
                this.DepthImagenStride = DepthStream.FrameWidth * 4;
                this.DepthValores = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength * 4];

                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.DepthValores);

                            int index = 0;
                            for (int i = 0; i < frame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValores[i] >> 3;

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

                            this.DepthImagenBitmap.WritePixels(this.DepthImagenRect, this.DepthImagenPixeles, this.DepthImagenStride, 0);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }

            }

            if (grabaImagen)
            { 
                bitmapDepth = convertWriteablebitmap(DepthImagenBitmap);
                Detection(bitmapDepth);
                grabaImagen = false; 
            }

            return DepthImagenBitmap;
        }//fin PollDepth()
        //:::::::::::::Fin de los metodos para manipular los datos del Kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        


        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private System.Drawing.Bitmap convertWriteablebitmap(WriteableBitmap wbitmap)
        {
            System.Drawing.Bitmap returnbitmap;
            using (MemoryStream Stream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)wbitmap));
                encoder.Save(Stream);
                returnbitmap = new System.Drawing.Bitmap(Stream);
            }
            return returnbitmap;
        }  //finaliza convertWriteableBitmap()


        private void Detection(System.Drawing.Bitmap bitmap)
        {
            string file = @"C:\Users\AmericaIvone\Documents\opencv-haar-classifier-training\classifier\cascade.xml";
            haar = new CascadeClassifier(file); 

            Image<Gray, Byte> frameDepth = new Image<Gray, Byte>(bitmap);
            byte[] pixeles;
            WriteableBitmap wbitmap;
            Image<Gray, Byte> manita = new Image<Gray, Byte>(bitmap);

            //Int32Rect rectwbitmap; 

            //using(Image<Gray, Byte> frameDepth = new Image<Gray,Byte>(bitmap)) 
            //{
                if (frameDepth != null)
                { 
                    
                    System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frameDepth, 1.4, 0, new  System.Drawing.Size(frameDepth.Width/8, frameDepth.Height/8), new  System.Drawing.Size(frameDepth.Width/3, frameDepth.Height/3));
                    //var hands= frameDepth.DetectHaarCascade(haar, 1.4, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new  System.Drawing.Size(frameDepth.Width/6, frameDepth.Height/6))[0];

                    foreach (System.Drawing.Rectangle roi in hands)
                    {  
                        //System.Drawing.Rectangle Roi = new System.Drawing.Rectangle(10,10,20,20);
                        Gray  colorcillo = new Gray(double.MaxValue);  
                        frameDepth.Draw( roi,colorcillo, 3);
                         
                    } 
                    pixeles = frameDepth.Bytes;
                    wbitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Gray8, null);
                    wbitmap.WritePixels(new Int32Rect(0, 0, 640, 480), pixeles, 640, 0);
                    image1.Source = wbitmap; 
                }
           // }     
        }//finaliza detection()


    }//END CLASS
}//ENS NAMESPACE
