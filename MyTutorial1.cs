using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using legotribot = Robotics.MyTutorial1;
using bumper = Microsoft.Robotics.Services.ContactSensor.Proxy;
using drive = Microsoft.Robotics.Services.Drive.Proxy;
using gamepad = Microsoft.Robotics.Services.Sample.XInputGamepad.Proxy;
using Microsoft.Kinect;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Ccr.Adapters.WinForms;
using System.Windows;

namespace Robotics.MyTutorial1
{
    /// <summary>
    /// Implementation class for LegoTriBot
    /// </summary>
    [DisplayName("LegoTriBot")]
    [Description("The LegoTriBot Service")]
    [Contract(Contract.Identifier)]
    public class MyTutorial1Service : DsspServiceBase
    {
        // The following variables handle Kinect sensor data
        private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
        private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
        private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };
        private const int RedIndex = 2;
        private const int GreenIndex = 1;
        private const int BlueIndex = 0;
        KinectSensor kinectSensor;
        private string connectedStatus = "Not connected";
        Skeleton[] allSkeletons = new Skeleton[6];
        public Skeleton first;
        public Skeleton second;
        bool closing = false;
        float i = 0;
        float e = 0;
        bool regulator = false;
        bool regulator2 = false;
        System.Timers.Timer updateTimer = new System.Timers.Timer(10);
        drive.SetDrivePowerRequest req = new drive.SetDrivePowerRequest();
        drive.RotateDegreesRequest Dreq = new drive.RotateDegreesRequest();
        bool isMovingForward = false;
        bool isMovingBackward = false;
        int percentPower = 100;
        bool isEnabled = false;
        string[] data = { "False", "False", "False" };

        /// <summary>
        /// _state
        /// </summary>
        private MyTutorial1State _state = new MyTutorial1State();
        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/legotribot", AllowMultipleInstances = false)]
        private MyTutorial1Operations _mainPort = new MyTutorial1Operations();

        [Partner("drive", Contract = drive.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        private drive.DriveOperations _drivePort = new drive.DriveOperations();

        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public MyTutorial1Service(DsspServiceCreationPort creationPort) :
            base(creationPort)
        {
             
        }
        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            base.Start();

            //Start listening for bumpers
            if (connectedStatus == "Not Connected")
            {
            }

            //for kinect sensor
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
            DiscoverKinectSensor();

            _state.MotorEnabled = true;

            updateTimer.Elapsed +=new System.Timers.ElapsedEventHandler(timer_Elapsed2);
            updateTimer.Start();

            while (_state.MotorEnabled == true)
            {

                if (data[0] == "True")
                {
                    isMovingForward = true;
                }
                else
                {
                    isMovingForward = false;
                }

                if (data[1] == "True")
                {
                    isMovingBackward = true;
                }
                else
                {
                    isMovingBackward = false;
                }

                if (data[2] == "True")
                {
                    isEnabled = true;
                }
                else
                {
                    isEnabled = false;
                }

                if (isEnabled == true)
                {
                    if (isMovingForward == true)
                    {
                        req.RightWheelPower = -(percentPower * 0.01);
                    }
                    else if (isMovingBackward == true)
                    {
                        req.RightWheelPower = (percentPower * 0.01);
                    }
                    else
                    {
                        req.RightWheelPower = 0.0;
                    }
                }

                if (first != null)
                {
                    float wall = first.Joints[JointType.Spine].Position.X + 0.3f;

                    // assign the values
                    if (first.Joints[JointType.HandRight].Position.X > wall)
                    {
                        if (i < 750)
                        {
                            i++;
                            req.LeftWheelPower = -1.0;
                        }
                        else
                        {
                            req.LeftWheelPower = 0.0;
                        }
                        regulator = true;
                    }
                    else if (regulator == true)
                    {
                        if (i < 1500)
                        {
                            i++;
                            req.LeftWheelPower = 1.0;
                        }
                        else
                        {
                            regulator = false;
                        }
                    }
                    else
                    {
                        req.LeftWheelPower = 0.0;
                        i = 0;
                    }
                }

                    if (second != null)
                    {
                        float wall2 = second.Joints[JointType.Spine].Position.X - 0.3f;

                        if (second.Joints[JointType.HandRight].Position.X > wall2)
                        {
                            if (e < 750)
                            {
                                e++;
                                req.RightWheelPower = -1.0;
                            }
                            else
                            {
                                req.RightWheelPower = 0.0;
                            }
                            regulator2 = true;
                        }
                        else if (regulator2 == true)
                        {
                            if (e < 1500)
                            {
                                e++;
                                req.RightWheelPower = 1.0;
                            }
                            else
                            {
                                regulator2 = false;
                            }
                        }
                        else
                        {
                            req.RightWheelPower = 0.0;
                            e = 0;
                        }
                    }

                LogInfo(LogGroups.Console, "Running");

                //post the request
                _drivePort.SetDrivePower(req);
            }
        }

        void timer_Elapsed2(object sender, System.Timers.ElapsedEventArgs e)
        {
            data = System.IO.File.ReadAllLines(@"C:\Users\Tyler\Desktop\ControlData.txt");
        }


        // The following code handles the Kinect Sensor

        public void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (this.kinectSensor == e.Sensor)
            {
                if (e.Status == KinectStatus.Disconnected || e.Status == KinectStatus.NotPowered)
                {
                    this.kinectSensor = null;
                    this.DiscoverKinectSensor();
                }
            }
        }

        private bool InitializeKinect()
        {
            // provides smoother skeleton joint tracking
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };

            // Uncomment this code to enable the RGB camera, depth streaming, and skeleton streaming

            // Uncommenting this block of code enables color streaming from the kinect RGB camera
            //kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            //kinectSensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady);

            // Uncommenting this block of code enables depth streaming
            //kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            //kinectSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinectSensor_DepthFrameReady);

            // Uncommenting this block of code enables skeleton tracking
            kinectSensor.SkeletonStream.Enable(parameters);
            kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);

            try
            {
                kinectSensor.Start();
            }
            catch
            {
                connectedStatus = "Unable to start the Kinect Sensor";
                return false;
            }
            //these two statements are used for speech recognition through the kinect
            //speechRecognizer = CreateSpeechRecognizer();
            //StartSpeechRecognition();

            return true;
        }

        public void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            //first = GetFirstSkeleton(e);

            //Get another skeleton
            //second = GetSecondSkeleton(e);

            if (first == null)
            {
                return;
            }

            if (second == null)
            {
                return;
            }

        }

        Skeleton GetFirstSkeleton(SkeletonFrameReadyEventArgs e)
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

        Skeleton GetSecondSkeleton(SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }

                Skeleton second = (from s in allSkeletons
                                   where s.TrackingState == SkeletonTrackingState.Tracked
                                   select s).LastOrDefault();

                return second;

            }
        }

        public void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame != null)
                {
                    short[] pixelsFromFrame = new short[depthImageFrame.PixelDataLength];
                    depthImageFrame.CopyPixelDataTo(pixelsFromFrame);
                    byte[] convertedPixels = ConvertDepthFrame(pixelsFromFrame, ((KinectSensor)sender).DepthStream, 640 * 480 * 4);
                    //Color[] color = new Color[depthImageFrame.Height * depthImageFrame.Width];
                    //kinectDepthVideo = new Texture2D(graphics.GraphicsDevice, depthImageFrame.Width, depthImageFrame.Height);
                    // Set convertedPixels from the DepthImageFrame to a the datasource for our Texture2D 
                    //kinectDepthVideo.SetData<byte>(convertedPixels);
                }
            }
        }

        private byte[] ConvertDepthFrame(short[] depthFrame, DepthImageStream depthStream, int depthFrame32Length)
        {
            int tooNearDepth = depthStream.TooNearDepth;
            int tooFarDepth = depthStream.TooFarDepth;
            int unknownDepth = depthStream.UnknownDepth;
            byte[] depthFrame32 = new byte[depthFrame32Length];
            for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < depthFrame32.Length; i16++, i32 += 4)
            {
                int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
                int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // transform 13-bit depth information into an 8-bit intensity appropriate          
                // for display (we disregard information in most significant bit)            
                byte intensity = (byte)(~(realDepth >> 4));
                if (player == 0 && realDepth == 0)
                {
                    // white           
                    depthFrame32[i32 + RedIndex] = 255;
                    depthFrame32[i32 + GreenIndex] = 255;
                    depthFrame32[i32 + BlueIndex] = 255;
                }
                else if (player == 0 && realDepth == tooFarDepth)
                {
                    // dark purple             
                    depthFrame32[i32 + RedIndex] = 66;
                    depthFrame32[i32 + GreenIndex] = 0;
                    depthFrame32[i32 + BlueIndex] = 66;
                }
                else if (player == 0 && realDepth == unknownDepth)
                {
                    // dark brown         
                    depthFrame32[i32 + RedIndex] = 66;
                    depthFrame32[i32 + GreenIndex] = 66;
                    depthFrame32[i32 + BlueIndex] = 33;
                }
                else
                {
                    // tint the intensity by dividing by per-player values  
                    depthFrame32[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
                    depthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
                    depthFrame32[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);
                }
            }
            return depthFrame32;
        }

        public void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame != null)
                {
                    byte[] pixelsFromFrame = new byte[colorImageFrame.PixelDataLength];
                    colorImageFrame.CopyPixelDataTo(pixelsFromFrame);
                    //Color[] color = new Color[colorImageFrame.Height * colorImageFrame.Width];
                    //kinectRGBVideo = new Texture2D(graphics.GraphicsDevice, colorImageFrame.Width, colorImageFrame.Height);
                    // Go through each pixel and set the bytes correctly       
                    // Remember, each pixel got a Rad, Green and Blue        
                    int index = 0;
                    for (int y = 0; y < colorImageFrame.Height; y++)
                    {
                        for (int x = 0; x < colorImageFrame.Width; x++, index += 4)
                        {
                            //color[y * colorImageFrame.Width + x] = new Color(pixelsFromFrame[index + 2], pixelsFromFrame[index + 1], pixelsFromFrame[index + 0]);
                        }
                    }
                    // Set pixeldata from the ColorImageFrame to a Texture2D           
                   // kinectRGBVideo.SetData(color);
                }
            }
        }

        private void DiscoverKinectSensor()
        {
            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    // Found one, set our sensor to this          
                    kinectSensor = sensor;
                    break;
                }
            }
            if (this.kinectSensor == null)
            {
                connectedStatus = "Found none Kinect Sensors connected to USB";
                return;
            }
            // You can use the kinectSensor.Status to check for status    
            // and give the user some kind of feedback         
            switch (kinectSensor.Status)
            {
                case KinectStatus.Connected:
                    {
                        connectedStatus = "Status: Connected";
                        break;
                    }
                case KinectStatus.Disconnected:
                    {
                        connectedStatus = "Status: Disconnected";
                        break;
                    }
                case KinectStatus.NotPowered:
                    {
                        connectedStatus = "Status: Connect the power";
                        break;
                    }
                default:
                    {
                        connectedStatus = "Status: Error";
                        break;
                    }
            }
            // Init the found and connected device    
            if (kinectSensor.Status == KinectStatus.Connected)
            {
                InitializeKinect();
            }
        }

        }
    }


