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



namespace DetectionNoiseRemove
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private WriteableBitmap ImagenWriteablebitmap;
        private Int32Rect WriteablebitmapRect;
        private int WriteablebitmapStride;
        private DepthImageStream DepthStream;
        private byte[] DepthImagenPixeles;
        private short[] DepthValoresStream;
        private Image<Gray, Byte> depthFrameKinect;
        private CascadeClassifier haar1;
        //private CascadeClassifier haar2;
        //private CascadeClassifier haar3;
        private string path1 = @"C:\imagenClassifiersWitoutNoise\Noise\";
        private string path2 = @"C:\imagenClassifiersWitoutNoise\NoNoise\";
        int i = 20;
        int j = 20;
        //int k = 20;
        //:::::::::::::fin variables::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //:::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        }
        //:::::::::::::Fin Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //:::::::::::::Call Methods::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            haar1 = new CascadeClassifier(@"C:\Users\America\Documents\opencv-haar-clasisifier-training\classifier\cascade.xml"); //La compu de escritorio
            //haar2 = new CascadeClassifier(@"C:\Users\America\Documents\opencv-haar-clasisifier-training\Mas Clasificadores\test\classifier\cascade.xml");
            //haar3 = new CascadeClassifier(@"C:\Users\America\Documents\opencv-haar-clasisifier-training\Mas Clasificadores\test_resize\classifier\cascade.xml");

            EncuentraInicializaKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
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


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            Image<Gray, Byte> imagenClasificar;
            Image<Gray, Byte> imageHaar1;
            Image<Gray, Byte> imageMedianF;
            Image<Gray, Byte> imageHaar1NoNoise;

            imagenClasificar = PollDepth();

            imageMedianF = removeNoise(imagenClasificar); 

            //Call detection an save the image with the result of the classifier.
            imageHaar1 = Detection(haar1, imagenClasificar);
            imageHaar1NoNoise = Detection(haar1, imageMedianF); 
            //imageHaar2 = Detection(haar2, imagenClasificar);
            //imageHaar3 = Detection(haar3, imagenClasificar);

            //Display the result of the classifier, so the bytes of the imagen
            //are converted in a wriablebitmap.  
            imageKinect.Source = imagetoWriteablebitmap(imageHaar1);
            imageMedianFilter.Source = imagetoWriteablebitmap(imageHaar1NoNoise); 

            if (i < 70)
            {
                guardaimagen(imageHaar1, path1, i);
                guardaimagen(imageHaar1NoNoise, path2, j);
                //guardaimagen(imageHaar3, path3, k);
                i++;
                j++;
                //k++;
            }


        } //fin CompositionTarget_Rendering()  


        private Image<Gray, Byte> PollDepth()
        {
            if (this.Kinect != null)
            {
                this.DepthStream = this.Kinect.DepthStream;
                this.DepthValoresStream = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength];
                this.depthFrameKinect = new Image<Gray, Byte>(DepthStream.FrameWidth, DepthStream.FrameHeight);

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
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                }
                                index++; //= index + 4;
                            }

                            depthFrameKinect.Bytes = DepthImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            return depthFrameKinect;
        }//fin PollDepth()


        //:::::::::::::Fin de los metodos para manipular los datos del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        //:::::::::::::Methods to do the detection, using haar features and cascade trining, the classifiers already trained::::::::::::

        private Image<Gray, Byte> Detection(CascadeClassifier haar, Image<Gray, Byte> frameDepth)
        {
            if (frameDepth != null)
            {
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frameDepth, 1.4, 0, new System.Drawing.Size(frameDepth.Width / 9, frameDepth.Height / 9), new System.Drawing.Size(frameDepth.Width / 4, frameDepth.Height / 4));

                foreach (System.Drawing.Rectangle roi in hands)
                {
                    Gray colorcillo = new Gray(double.MaxValue);
                    frameDepth.Draw(roi, colorcillo, 3);
                }
            }

            return frameDepth;
        }//finaliza detection()


        //:::::::::::::Method to convert a byte[] to a writeablebitmap::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private WriteableBitmap imagetoWriteablebitmap(Image<Gray, Byte> frameHand)
        {
            byte[] imagenPixels = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight];

            this.ImagenWriteablebitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Gray8, null);
            this.WriteablebitmapRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
            this.WriteablebitmapStride = DepthStream.FrameWidth;

            imagenPixels = frameHand.Bytes;
            ImagenWriteablebitmap.WritePixels(WriteablebitmapRect, imagenPixels, WriteablebitmapStride, 0);

            return ImagenWriteablebitmap;
        }//end 


        //::::::::::::Method to remove the noise, using median filters::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private Image<Gray, Byte> removeNoise(Image<Gray, Byte> imagenKinet)
        {
            Image<Gray, Byte> imagenSinRuido;

            imagenSinRuido = imagenKinet.SmoothMedian(7);

            return imagenSinRuido; 
        }//endremoveNoise 
         

        //:::::::::::::Method to saves the images with tha detection ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        private void guardaimagen(Image<Gray, Byte> imagen, string path, int i)
        {
            imagen.Save(path + i.ToString() + ".png");
        }

        //::::::::::::Method to stop de Kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.Stop();
        }



    }//end class
}//end namespace
