using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KinectMouseController;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Windows.Navigation;




namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
        }

    

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            FaceWindow faceLearn = new FaceWindow(); //Create object of MainWindow
            faceLearn.Show();
            this.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MainWindow bodyLearn = new MainWindow(); //Create object of MainWindow
            bodyLearn.Show();
            this.Close();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Acupuncture is an ancient Chinese form of healing which maps out the subtle networks and interrelationships that reveal our bodies to be dynamic cellular ecosystems. Acupuncture points are key anatomical locations on or under the skin. Pressing important acupuncture points helps people relieve from various health problems. So we design this e-learning system for acupuncture points to help people learn the locations of acupoints and do acupress themselves.");
        }
        
        /////////////////////////////////
        bool closing = false;
        const int skeletonCount = 6; 
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        Canvas cvs = new Canvas();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser2.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser2_KinectSensorChanged);

        }

        void kinectSensorChooser2_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            


            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };
            sensor.SkeletonStream.Enable(parameters);

            sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30); 
            //sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser2.AppConflictOccurred();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first =  GetFirstSkeleton(e);

            if (first == null)
            {
                return; 
            }

            //set scaled position
            //ScalePosition(headImage, first.Joints[JointType.Head]);
            //  ScalePosition(leftEllipse, first.Joints[JointType.HandLeft]);
            //ScalePosition(rightEllipse, first.Joints[JointType.HandRight]);
            GetCameraPoint(first, e); 

        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser2.Kinect == null)
                {
                    return;
                }
                

                //Map a joint location to a point on the depth map
                
                //right hand
                DepthImagePoint rightHandDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);
                //Map a depth point to a point on the color image
                ColorImagePoint handRightColor =
                    depth.MapToColorImagePoint(rightHandDepthPoint.X, rightHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //Set location
                //CameraPosition(handright, handRightColor); 
               
               Joint handRight = first.Joints[JointType.HandRight].ScaleTo(1280, 720);
                //CameraPositionHandRight(handright, handRight);        
               int cursorX = (int)handRight.Position.X;
               int cursorY = (int)handRight.Position.Y;
        
                KinectMouseController.KinectMouseMethods.SendMouseInput(cursorX, cursorY, 1280, 720, false); 
            }        
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null; 
                }

                
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();

                return first;

            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }


                }
            }
        }

        

        

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);     
        }

        private void AddLine(ColorImagePoint p1, ColorImagePoint p2)
        {
            Line myLine = new Line();
            myLine.Stroke = System.Windows.Media.Brushes.Black;

            myLine.X1 = p1.X;
            myLine.X2 = p2.X;
            myLine.Y1 = p1.Y;
            myLine.Y2 = p2.Y;
            myLine.StrokeThickness = 1;
            cvs.Children.Add(myLine);
 
        }

        /*private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y); 
            
            
        }*/


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true; 
            StopKinect(kinectSensorChooser2.Kinect); 
        }

        
      

        

    }
    
}
