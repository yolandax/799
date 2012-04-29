// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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
using Coding4Fun.Kinect.Wpf; 

namespace SkeletalTracking
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

        bool closing = false;
        const int skeletonCount = 6; 
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        Canvas cvs = new Canvas();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
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
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
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
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                

                //Map a joint location to a point on the depth map
                //head
                
                DepthImagePoint headDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.Head].Position);
                //left elbow
                DepthImagePoint leftElbowDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ElbowLeft].Position);
                //right eblow
                DepthImagePoint rightElbowDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ElbowRight].Position);

                DepthImagePoint leftShoulderDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderLeft].Position);

                DepthImagePoint rightShoulderDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderRight].Position);

                DepthImagePoint spineDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.Spine].Position);

                DepthImagePoint hipCenterDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.HipCenter].Position);

                //left wrist
                DepthImagePoint leftWristDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.WristLeft].Position);
                //right wrist
                DepthImagePoint rightWristDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.WristRight].Position);
                //right hand
                DepthImagePoint rightHandDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);
                //left hand
                DepthImagePoint leftHandDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);

                //Map a depth point to a point on the color image
                //head
                ColorImagePoint headColorPoint =
                    depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //leftNeiguan
                ColorImagePoint leftElbowColorPoint =
                    depth.MapToColorImagePoint(leftElbowDepthPoint.X, leftElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right elbow
                ColorImagePoint rightElbowColorPoint =
                    depth.MapToColorImagePoint(rightElbowDepthPoint.X, rightElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left shoulder
                ColorImagePoint leftShoulderColorPoint =
                    depth.MapToColorImagePoint(leftShoulderDepthPoint.X, leftShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right shoulder
                ColorImagePoint rightShoulderColorPoint =
                    depth.MapToColorImagePoint(rightShoulderDepthPoint.X, rightShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //tiantu
                ColorImagePoint tianTu =
                    depth.MapToColorImagePoint((leftShoulderDepthPoint.X + rightShoulderDepthPoint.X) / 2, (leftShoulderDepthPoint.Y + rightShoulderDepthPoint.Y)/2,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //tanzhong
                ColorImagePoint tanZhong =
                    depth.MapToColorImagePoint(spineDepthPoint.X, spineDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                tianTu.X = (leftShoulderColorPoint.X + rightShoulderColorPoint.X) / 2;
                tianTu.Y = (leftShoulderColorPoint.Y + rightShoulderColorPoint.Y) / 2;
                //shuifen
                ColorImagePoint shuiFen =
                    depth.MapToColorImagePoint(hipCenterDepthPoint.X, hipCenterDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //qimenLeft
                ColorImagePoint qimenLeft =
                    depth.MapToColorImagePoint(leftShoulderDepthPoint.X, leftShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //qimenRight
                ColorImagePoint qimenRight =
                    depth.MapToColorImagePoint(rightShoulderDepthPoint.X, rightShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //qianshuLeft
                ColorImagePoint tianshuLeft =
                    depth.MapToColorImagePoint(hipCenterDepthPoint.X, hipCenterDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //qianshuRight
                ColorImagePoint tianshuRight =
                    depth.MapToColorImagePoint(hipCenterDepthPoint.X, hipCenterDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //neiguanLeft
                ColorImagePoint neiguanLeft =
                    depth.MapToColorImagePoint((leftWristDepthPoint.X+leftHandDepthPoint.X)/2, (leftWristDepthPoint.Y+leftHandDepthPoint.Y)/2,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //quchiLeft
                ColorImagePoint quchiLeft =
                    depth.MapToColorImagePoint(leftElbowDepthPoint.X, leftElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //zhiyangLeft
                ColorImagePoint zhiyangLeft =
                    depth.MapToColorImagePoint(leftElbowDepthPoint.X, leftElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //liequeRight
                ColorImagePoint liequeLeft =
                    depth.MapToColorImagePoint(leftWristDepthPoint.X, leftWristDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //liequeRight
                ColorImagePoint taiyuanRight =
                    depth.MapToColorImagePoint(rightWristDepthPoint.X, rightWristDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //handRight
                ColorImagePoint handRight =
                    depth.MapToColorImagePoint(rightHandDepthPoint.X, rightHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                

                //Set location
               // CameraPosition(headImage, headColorPoint);
                TiantuPosition(tiantu, tianTu);
                CameraPositionTanzhong(tanzhong, tanZhong);
                CameraPositionShuifen(shuifen, shuiFen);
                CameraPositionQimenLeft(qimenleft, qimenLeft);
                CameraPositionQimenRight(qimenright, qimenRight);
               // CameraPositionTianshuLeft(tianshuleft, tianshuLeft);
               // CameraPositionTianshuRight(tianshuright, tianshuRight);
                CameraPositionNeiguanLeft(neiguanleft, neiguanLeft);
                CameraPositionQuchiLeft(quchileft, quchiLeft);
                CameraPositionZhiyangLeft(zhiyangleft, zhiyangLeft);
                CameraPositionLiequeLeft(liequeleft, liequeLeft);
                //CameraPositionTaiyuanRight(taiyuanright, taiyuanRight);
                //CameraPositionHandRight(handright, handRight);        
                CameraPosition(handright, handRight);         
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
        private void TiantuPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            //Canvas.SetTop(element, point.Y-element.ActualHeight-20);   
            Canvas.SetTop(element, point.Y - 3*element.ActualHeight);  

        }


        private void CameraPositionShoulder(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - 6*element.Height / 2);
        }

        private void CameraPositionTanzhong(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y-6*element.Height);
        }

        private void CameraPositionShuifen(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - element.Height);
        }

        private void CameraPositionQimenLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X+2*element.Width);
            Canvas.SetTop(element, point.Y + 4.5*element.Height);
        }

        private void CameraPositionQimenRight(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - 2 * element.Width);
            Canvas.SetTop(element, point.Y + 4.5*element.Height);
        }

        private void CameraPositionTianshuLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X+element.Width);
            Canvas.SetTop(element, point.Y - element.Height);
        }

        private void CameraPositionTianshuRight(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X-element.Width);
            Canvas.SetTop(element, point.Y - element.Height);
        }

        private void CameraPositionNeiguanLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - 4*element.Height);
        }

        private void CameraPositionQuchiLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - 2.3*element.Height);
        }

        private void CameraPositionZhiyangLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - 4.8*element.Height);
        }

        private void CameraPositionLiequeLeft(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X-element.Width);
            Canvas.SetTop(element, point.Y - element.Height);
        }

        private void CameraPositionTaiyuanRight(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - element.ActualHeight - 20);
        }

        private void CameraPositionHandRight(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - element.ActualHeight - 20);
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

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y); 
            
            
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true; 
            StopKinect(kinectSensorChooser1.Kinect); 
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MainMenu menu = new MainMenu(); //Create object of MainWindow
            menu.Show();
            this.Close(); 
            
        }

    }
}
