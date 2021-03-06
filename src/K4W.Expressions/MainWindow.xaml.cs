﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace K4W.Expressions
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Instance of Kinect sensor
        /// </summary>
        private KinectSensor _kinect;

        /// <summary>
        /// Body reader
        /// </summary>
        public BodyFrameReader _bodyReader;

        /// <summary>
        /// Collection of all tracked bodies
        /// </summary>
        public Body[] _bodies;

        /// <summary>
        /// Requested face features
        /// </summary>
        private const FaceFrameFeatures _faceFrameFeatures = FaceFrameFeatures.BoundingBoxInInfraredSpace
                                                        | FaceFrameFeatures.PointsInInfraredSpace
                                                        | FaceFrameFeatures.MouthMoved
                                                        | FaceFrameFeatures.MouthOpen
                                                        | FaceFrameFeatures.LeftEyeClosed
                                                        | FaceFrameFeatures.RightEyeClosed
                                                        | FaceFrameFeatures.LookingAway
                                                        | FaceFrameFeatures.Happy
                                                        | FaceFrameFeatures.FaceEngagement
                                                        | FaceFrameFeatures.Glasses;

        /// <summary>
        /// Face Source
        /// </summary>
        private FaceFrameSource _faceSource;

        /// <summary>
        /// Face Reader
        /// </summary>
        private FaceFrameReader _faceReader;

        /// <summary>
        /// Default CTOR
        /// </summary>
        public MainWindow()
        {
            // Initialize Components
            InitializeComponent();

            // Initialize Kinect
            InitializeKinect();
        }

        /// <summary>
        /// Initialize Kinect
        /// </summary>
        private void InitializeKinect()
        {
            // Get Kinect sensor
            _kinect = KinectSensor.GetDefault();

            if (_kinect == null) return;

            // Initialize Camera
            InitializeCamera();

            // Initialize body tracking
            InitializeBodyTracking();

            // Start receiving
            _kinect.Open();
        }

        /// <summary>
        /// Initialize body tracking
        /// </summary>
        private void InitializeBodyTracking()
        {
            // Body Reader
            _bodyReader = _kinect.BodyFrameSource.OpenReader();

            // Wire event
            _bodyReader.FrameArrived += OnBodyFrameReceived;
        }

        /// <summary>
        /// Process body frames
        /// </summary>
        private void OnBodyFrameReceived(object sender, BodyFrameArrivedEventArgs e)
        {
            // Get Frame ref
            BodyFrameReference bodyRef = e.FrameReference;

            if (bodyRef == null) return;

            // Get body frame
            using (BodyFrame frame = bodyRef.AcquireFrame())
            {
                if (frame == null) return;

                // Allocate array when required
                if (_bodies == null)
                    _bodies = new Body[frame.BodyCount];

                // Refresh bodies
                frame.GetAndRefreshBodyData(_bodies);

                foreach (Body body in _bodies)
                {
                    if (body.IsTracked && _faceSource == null)
                    {
                        // Create new sources with body TrackingId
                        _faceSource = new FaceFrameSource(_kinect, body.TrackingId, _faceFrameFeatures);

                        // Create new reader
                        _faceReader = _faceSource.OpenReader();

                        // Wire events
                        _faceReader.FrameArrived += OnFaceFrameArrived;
                        _faceSource.TrackingIdLost += OnTrackingIdLost;
                    }
                }
            }
        }

        /// <summary>
        /// Process the face frame
        /// </summary>
        private void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            // Retrieve the face reference
            FaceFrameReference faceRef = e.FrameReference;

            if (faceRef == null) return;

            // Acquire the face frame
            using (FaceFrame faceFrame = faceRef.AcquireFrame())
            {
                if (faceFrame == null) return;

                // Retrieve the face frame result
                FaceFrameResult frameResult = faceFrame.FaceFrameResult;

                // Display the values
                HappyResult.Text = frameResult.FaceProperties[FaceProperty.Happy].ToString();
                EngagedResult.Text = frameResult.FaceProperties[FaceProperty.Engaged].ToString();
                GlassesResult.Text = frameResult.FaceProperties[FaceProperty.WearingGlasses].ToString();
                LeftEyeResult.Text = frameResult.FaceProperties[FaceProperty.LeftEyeClosed].ToString();
                RightEyeResult.Text = frameResult.FaceProperties[FaceProperty.RightEyeClosed].ToString();
                MouthOpenResult.Text = frameResult.FaceProperties[FaceProperty.MouthOpen].ToString();
                MouthMovedResult.Text = frameResult.FaceProperties[FaceProperty.MouthMoved].ToString();
                LookingAwayResult.Text = frameResult.FaceProperties[FaceProperty.LookingAway].ToString();
            }
        }

        /// <summary>
        /// Handle when the tracked body is gone
        /// </summary>
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // Update UI
            HappyResult.Text = "No face tracked";
            EngagedResult.Text = "No face tracked";
            GlassesResult.Text = "No face tracked";
            LeftEyeResult.Text = "No face tracked";
            RightEyeResult.Text = "No face tracked";
            MouthOpenResult.Text = "No face tracked";
            MouthMovedResult.Text = "No face tracked";
            LookingAwayResult.Text = "No face tracked";

            // Reset values for next body
            _faceReader = null;
            _faceSource = null;
        }

        #region CAMERA
        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _colorBitmap = null;
        /// <summary>
        /// Array of color pixels
        /// </summary>
        private byte[] _colorPixels = null;

        /// <summary>
        /// FrameReader for our coloroutput
        /// </summary>
        private ColorFrameReader _colorReader = null;
        /// <summary>
        /// Size fo the RGB pixel in bitmap
        /// </summary>
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private void InitializeCamera()
        {
            if (_kinect == null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            // Get the framereader for Color
            _colorReader = _kinect.ColorFrameSource.OpenReader();

            // Allocate pixel array
            _colorPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _colorBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
            CameraImage.Source = _colorBitmap;

            // Hook-up event
            _colorReader.FrameArrived += OnColorFrameArrived;
        }
        /// <summary>
        /// Process color frames & show in UI
        /// </summary>
        private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            // Acquire frame for specific reference
            ColorFrame frame = colorRef.AcquireFrame();

            // It's possible that we skipped a frame or it is already gone
            if (frame == null) return;

            using (frame)
            {
                // Get frame description
                FrameDescription frameDesc = frame.FrameDescription;

                // Check if width/height matches
                if (frameDesc.Width == _colorBitmap.PixelWidth && frameDesc.Height == _colorBitmap.PixelHeight)
                {
                    // Copy data to array based on image format
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(_colorPixels);
                    }
                    else frame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);

                    // Copy output to bitmap
                    _colorBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _colorPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }
        #endregion CAMERA
    }
}
