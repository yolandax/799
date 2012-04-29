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
using System.Drawing;

//for face
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;


namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FaceWindow : Window
    {
        public FaceWindow()
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
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                //return;
            }

            doFace(first, e);


        }

        void doFace(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                SaveColorImage(first, e);


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


        /***********************************/
        private void SaveColorImage(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                BitmapSource imSource = colorFrame.ToBitmapSource();
                //Change the path
                imSource.Save("../image.jpg", ImageFormat.Jpeg);
            }
        }
        /***********************************/

        public static System.Drawing.Point getMiddlePoint(System.Drawing.Point p1, System.Drawing.Point p2)
        {
            System.Drawing.Point newP = new System.Drawing.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            return newP;
        }

        public static System.Drawing.Point getOffsetPoint(int dir, int twoeye, int offset, System.Drawing.Point p)
        {
            System.Drawing.Point xp;
            int units = twoeye / 8;
            if (dir == 0)
            { //up
                xp = new System.Drawing.Point(p.X, p.Y - offset * units);
            }
            else
            {
                xp = new System.Drawing.Point(p.X, p.Y + offset * units);
            }
            return xp;
        }


        [DllImport(@"../data/stasm_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AsmSearchDll(
                 ref int pnlandmarks, // out: number of landmarks, 0 if can't get landmarks
                 int[] landmarks, // out: the landmarks, caller must allocate
               [MarshalAs(UnmanagedType.LPStr)] String imagename, // in: used in internal error messages, if necessary
            //string imagename,
                byte[] imagedata, // in: image data, 3 bytes per pixel if is_color
                int width, // in: the width of the image
                int height, // in: the height of the image
                int is_color, // in: 1 if RGB image, 0 if grayscale
             [MarshalAs(UnmanagedType.LPStr)] String conf_file0, // in: 1st config filename, NULL for default
               [MarshalAs(UnmanagedType.LPStr)] String conf_file1 //  in: 2nd config filename, NULL for default, "" for none
            //  string conf_file0,
            //string conf_file1

               );
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }




        private void showFaceAcuP()
        {
            String imageName = "../image.jpg";
            Image<Bgr, byte> newimage = new Image<Bgr, byte>(imageName);
            int nlandmarks = 0;
            int[] landmarks = new int[100];
            System.Drawing.Point[] points = new System.Drawing.Point[100];
            MCvScalar color = new MCvScalar(255, 0, 0);
            AsmSearchDll(ref nlandmarks, landmarks, "../image.jpg", newimage.Bytes, newimage.Width, newimage.Height, 1, null, null);
            for (int i = 0; i < nlandmarks; i++)
            {
                Console.WriteLine("landmark" + i + "   " + landmarks[2 * i] + "   " + landmarks[2 * i + 1]);
                points[i] = new System.Drawing.Point(landmarks[2 * i], landmarks[2 * i + 1]);
            }
            IntPtr[] p = new IntPtr[100];
            int[] ptrn = new int[100];
            ptrn[0] = nlandmarks;

            GCHandle handle = GCHandle.Alloc(landmarks, GCHandleType.Pinned);
            try
            {
                p[0] = handle.AddrOfPinnedObject();

            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();

                }
            }

            System.Drawing.Point renzhong = points[41];
            System.Drawing.Point yintang = getMiddlePoint(points[18], points[24]);
            System.Drawing.Point yingxiang = getMiddlePoint(points[43], points[42]);
            System.Drawing.Point jingming1 = getMiddlePoint(points[29], points[37]);
            System.Drawing.Point jingming2 = getMiddlePoint(points[34], points[45]);
            System.Drawing.Point zanzhu1 = points[18];
            System.Drawing.Point zanzhu2 = points[24];
            System.Drawing.Point tongziliao1 = getMiddlePoint(points[21], points[27]);
            System.Drawing.Point tongziliao2 = getMiddlePoint(points[14], points[32]);
            int twoeye = Math.Abs(points[27].X - points[32].X);
            System.Drawing.Point yangbai1 = getOffsetPoint(0, twoeye, 2, points[25]);
            System.Drawing.Point yangbai2 = getOffsetPoint(0, twoeye, 2, points[19]);
            System.Drawing.Point touwei1 = getOffsetPoint(0, twoeye, 4, points[21]);
            System.Drawing.Point touwei2 = getOffsetPoint(0, twoeye, 4, points[15]);
            System.Drawing.Point chengqi1 = getOffsetPoint(1, twoeye, 1, points[30]);
            System.Drawing.Point chengqi2 = getOffsetPoint(1, twoeye, 1, points[35]);
            System.Drawing.Point sibai1 = getOffsetPoint(1, twoeye, 2, points[30]);
            System.Drawing.Point sibai2 = getOffsetPoint(1, twoeye, 2, points[35]);


            CvInvoke.cvCircle(newimage, renzhong, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, yintang, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, yingxiang, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, jingming1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, jingming2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, zanzhu1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, zanzhu2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, tongziliao1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, tongziliao2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, yangbai1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, yangbai2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, touwei1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, touwei2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, chengqi1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, chengqi2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, sibai1, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
            CvInvoke.cvCircle(newimage, sibai2, 1, color, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);

            image1.Source = ToBitmapSource(newimage);



        }

    }
}
