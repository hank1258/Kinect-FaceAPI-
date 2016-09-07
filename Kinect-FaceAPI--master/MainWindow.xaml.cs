//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Utils;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ProjectOxford.Face;
    using Microsoft.ProjectOxford.Face.Controls;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Threading;
    using System.Net.Mail;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Windows.Interop;
    using System.Windows.Forms;
    using LINQtoCSV;
    using Utils;

    enum State { Default, Background, Result, QRcode };

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        private Bitmap figureBitmap = null;


        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        int facecount = 0;
        string shareVariable = "default";
        DispatcherTimer timer;
        DispatcherTimer count_timer;
        int countdown = 0;
        DispatcherTimer bgImage_timer;
        int imgno = 0;
        string final_name = "";
        int view_mode = 0;
        BitmapImage[] bg_pool = new BitmapImage[20];
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private State Mode_State = State.Default;
        private string CurrentState = "D";
        Dictionary<int, string> Facename_Pool = new Dictionary<int, string>();
        Dictionary<string, string> QR_DataBase = new Dictionary<string, string>();

        string[] HeadRandom ;
        Dictionary<string, Account> accounts = new Dictionary<string, Account>();
        Bitmap Final_Bitmap;

        StringBuilder Mount_path = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 

        //   private List<FaceAttributeType> _faceattributes = new List<FaceAttributeType>();
        //  BitmapImage _faceimage;
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.figureBitmap = new Bitmap(colorFrameDescription.Width, colorFrameDescription.Height);


            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            if (this.kinectSensor != null)
            {
                Console.Write("Kinect Open");
                this.bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];
                // open the reader for the body frames
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
                //kinect.Open();
            }

            // Define the face attributes the Face API should return
            //    _faceattributes.Add(FaceAttributeType.Gender);
            //    _faceattributes.Add(FaceAttributeType.Age);
            //    _faceattributes.Add(FaceAttributeType.HeadPose);
            //    _faceattributes.Add(FaceAttributeType.Glasses);
            //    _faceattributes.Add(FaceAttributeType.Smile);
            //   _faceattributes.Add(FaceAttributeType.FacialHair);

            
            imgno = 0;
            bgImage_timer = new DispatcherTimer();
            bgImage_timer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            Load_BgImage();
            Account.init();
        }
        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.MediaElement)sender).Position = ((System.Windows.Controls.MediaElement)sender).Position.Add(TimeSpan.FromMilliseconds(1));
        }
        private async void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }

            }
            
            #region gesture detect to switch bg_image
            if (dataReceived)
            {
                foreach (Body body in bodies)
                {
                    Joint userJoint_HandLeft = body.Joints[JointType.HandLeft];
                    Joint userJoint_ElbowLeft = body.Joints[JointType.ElbowLeft];
                    Joint userJoint_HandRight = body.Joints[JointType.HandRight];
                    Joint userJoint_ElbowRight = body.Joints[JointType.ElbowRight];
                    Joint userJoint_ShoulderCenter = body.Joints[JointType.SpineShoulder];
                    Joint userJoint_Spine = body.Joints[JointType.SpineMid];
                    Joint userJoint_Head = body.Joints[JointType.Head];


                    /*
                    int ZvalueforDistinct = 1000;
                    int bodyIndexForUse = 0;
                    // first person
                    for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                    {

                        Body temp_body = this.bodies[bodyIndex];
                        Joint userJoint_Head = temp_body.Joints[JointType.Head];
                        int HeadZAxis = (int)(userJoint_Head.Position.Z * 100);
                        if (ZvalueforDistinct > HeadZAxis && HeadZAxis != 0)
                        {
                            ZvalueforDistinct = HeadZAxis;
                            bodyIndexForUse = bodyIndex;
                        }

                    }*/
                    //Body body = this.bodies[bodyIndexForUse];

                    string Fi_Photos, fi_path;
                    
                    switch (Mode_State)
                    {
                        case State.Background:
                        case State.Result:

                            if (!body.IsTracked)
                                break;
                            

                            //雙手合十
                            if (userJoint_HandRight.Position.X - userJoint_HandLeft.Position.X < 0.01f)
                            {
                                saveFinalImg(imgno);
                            }
                            //右手舉起
                            else if (userJoint_HandRight.Position.Y > userJoint_ElbowRight.Position.Y)
                            {
                                //右手X位置-右手手肘的位置
                                float distance = userJoint_HandRight.Position.X - userJoint_ElbowRight.Position.X;
                                Console.Write("舉右手");
                                System.Console.WriteLine(distance);
                                int flag = 0;

                                //FSM dectect gesture
                                // INPUT : R
                                if (distance > 0.05f)
                                {
                                    if (CurrentState.Equals("D"))
                                    {
                                        CurrentState = "1";
                                    }
                                    else if (CurrentState.Equals("0"))
                                    {
                                        CurrentState = "0";
                                    }
                                    else if (CurrentState.Equals("1"))
                                    {
                                        CurrentState = "0";
                                    }


                                }// INPUT : L
                                else if (distance < -0.05f)
                                {
                                    if (CurrentState.Equals("D"))
                                    {
                                        CurrentState = "0";
                                    }
                                    else if (CurrentState.Equals("0"))
                                    {
                                        CurrentState = "1";
                                    }
                                    else if (CurrentState.Equals("1"))
                                    {
                                        CurrentState = "1";
                                    }


                                }
                                // switch image
                                if (CurrentState.Equals("0"))
                                {
                                    imgno = (imgno + Constants.MAX_BG_NUM - 1) % Constants.MAX_BG_NUM;

                                    Thread.Sleep(200);
                                    BackGround_Screen.Source = bg_pool[imgno];

                                    CurrentState = "D";

                                }
                                else if (CurrentState.Equals("1"))
                                {
                                    imgno = (imgno + 1) % Constants.MAX_BG_NUM;

                                    Thread.Sleep(200);
                                    BackGround_Screen.Source = bg_pool[imgno];

                                    CurrentState = "D";
                                }

                            }//左手舉起
                            else if (userJoint_HandLeft.Position.Y > userJoint_ElbowLeft.Position.Y)
                            {
                            }

                            break;

                        case State.QRcode:

                            break;

                    }
                }
                
            }
            #endregion
        }
        private void Load_BgImage()
        {
            for (int i=0; i<Constants.MAX_BG_NUM; i++)
            {
                StringBuilder st = new StringBuilder();
                st.Append("Images/Slide");
                st.Append((i+1).ToString());
                st.Append(".JPG");
                bg_pool[i] = new BitmapImage(new Uri(st.ToString(), UriKind.RelativeOrAbsolute));
            }
        }
        private void CountTimer_Tick(object sender, EventArgs e)
        {
            countdown++;
            if (countdown == 1)
            {
                image_three.Visibility = Visibility.Visible;
            }
            if (countdown == 2)
            {
                image_three.Visibility = Visibility.Collapsed;
                image_two.Visibility = Visibility.Visible;
            }
            if (countdown == 3)
            {
                image_two.Visibility = Visibility.Collapsed;
                image_one.Visibility = Visibility.Visible;
            }
            if (countdown == 4)
            {
                image_one.Visibility = Visibility.Collapsed;
                captureImg();
                countdown = 0;
                count_timer.Stop();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            ZXing.Result result;
            ZXing.IBarcodeReader reader = new ZXing.BarcodeReader();
            using (Bitmap qr_bitmap = new Bitmap(BitmapFromWriteableBitmap(this.colorBitmap)))
            {
                Rectangle rect = new Rectangle((qr_bitmap.Width - Constants.QRIMG_SIZE) / 2, (qr_bitmap.Height - Constants.QRIMG_SIZE) / 2, Constants.QRIMG_SIZE, Constants.QRIMG_SIZE);
                using (Bitmap subQRBitmap = qr_bitmap.Clone(rect, qr_bitmap.PixelFormat))
                {
                    result = reader.Decode(subQRBitmap);
                }
            }

            System.Console.WriteLine("result:" + result);
            if (result != null)
            {  
                // Create folder
                StringBuilder destFileName = new StringBuilder();
                StringBuilder srcFileName = new StringBuilder();

                if (Constants.SAVE_TO_CLOUD_DRIVE)
                {
                    destFileName.Append("Y:\\MTC\\");
                    destFileName.Append(Account.getAmById(result.ToString()));
                    System.IO.Directory.CreateDirectory(destFileName.ToString());
                    destFileName.Append("\\photo_");
                    destFileName.Append(Account.getNameById(result.ToString()));
                    destFileName.Append(".jpg");

                    srcFileName.Append("Y:\\MTC\\Result\\");
                    srcFileName.Append("MTC_");
                    srcFileName.Append(Facename_Pool[1]);
                    srcFileName.Append(".jpg");
                }
                else
                {
                    destFileName.Append("MTC\\");
                    destFileName.Append(Account.getAmById(result.ToString()));
                    System.IO.Directory.CreateDirectory(destFileName.ToString());
                    destFileName.Append("\\photo_");
                    destFileName.Append(Account.getNameById(result.ToString()));
                    destFileName.Append(".jpg");


                    srcFileName.Append("MTC\\Result\\");
                    srcFileName.Append("MTC_");
                    srcFileName.Append(Facename_Pool[1]);
                    srcFileName.Append(".jpg");
                }

                Thread myNewThread = new Thread(() => Utils.saveBitmap(srcFileName.ToString(), destFileName.ToString()));
                myNewThread.Start();

                timer.Stop();
                Mode_State = State.Default;
                qrcancel_button.Visibility = Visibility.Collapsed;
                qrcancel_button2.Visibility = Visibility.Collapsed;
                qr_text.Visibility = Visibility.Collapsed;
                BackGround_Screen.Source = bg_pool[1];
                BackGround_Screen.Visibility = Visibility.Collapsed;
                check_button.Visibility = Visibility.Collapsed;
                retry_button.Visibility = Visibility.Collapsed;
                DefaultScreen.Visibility = Visibility.Visible;
                shot_button.Visibility = Visibility.Visible;
                shot_button2.Visibility = Visibility.Visible;
                qrcode_frame.Visibility = Visibility.Collapsed;

                view_mode = 0;
            }
        }
        public static BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        public BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }
        private  System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                if (view_mode == 0)
                {
                    return this.colorBitmap;
                }
                else if (view_mode == 1)
                {
                    Bitmap a = new Bitmap(Image.FromFile("Images/Slide1.JPG"));
                    return CreateBitmapSourceFromGdiBitmap(a);
                }
                else
                {
                    return this.colorBitmap;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }


        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // ... Get control that raised this event.
            // var textBox = sender as TextBox;
            // ... Change Window Title.
            
            // this.Title = textBox.Text +
            //"[Length = " + textBox.Text.Length.ToString() + "]";
        }
        private void QRcancelButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            Mode_State = State.Default;
            qr_text.Visibility = Visibility.Collapsed;
            BackGround_Screen.Source = bg_pool[1];
            BackGround_Screen.Visibility = Visibility.Collapsed;
            check_button.Visibility = Visibility.Collapsed;
            retry_button.Visibility = Visibility.Collapsed;
            DefaultScreen.Visibility = Visibility.Visible;
            shot_button.Visibility = Visibility.Visible;
            shot_button2.Visibility = Visibility.Visible;
            qrcode_frame.Visibility = Visibility.Collapsed;
            qrcancel_button.Visibility = Visibility.Collapsed;
            qrcancel_button2.Visibility = Visibility.Collapsed;

            view_mode = 0;
        }
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            saveFinalImg(imgno);

            BackGround_Screen.Visibility = Visibility.Collapsed;
            using (Graphics G = Graphics.FromImage(figureBitmap)) G.Clear(System.Drawing.Color.Transparent);
            Figure_Screen.Source = Utils.Bitmap2BitmapImage(figureBitmap);
            Figure_Screen.Visibility = Visibility.Collapsed;
            check_button.Visibility = Visibility.Collapsed;
            retry_button.Visibility = Visibility.Collapsed;
            DefaultScreen.Visibility = Visibility.Visible;
            qr_text.Visibility = Visibility.Visible;
         
            qrcode_frame.Visibility = Visibility.Visible;
            left_button.Visibility = Visibility.Collapsed;
            right_button.Visibility = Visibility.Collapsed;

            view_mode = 0;

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);


            timer.Tick += Timer_Tick;
            timer.Start();
            qrcancel_button.Visibility = Visibility.Visible;
            qrcancel_button2.Visibility = Visibility.Visible;
            Mode_State = State.QRcode;
        }
        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            saveFinalImg(imgno);
            Mode_State = State.Default;
            
            BackGround_Screen.Visibility = Visibility.Collapsed;
            using (Graphics G = Graphics.FromImage(figureBitmap)) G.Clear(System.Drawing.Color.Transparent);
            Figure_Screen.Source = Utils.Bitmap2BitmapImage(figureBitmap);
            Figure_Screen.Visibility = Visibility.Collapsed;
            check_button.Visibility = Visibility.Collapsed;
            retry_button.Visibility = Visibility.Collapsed;
            DefaultScreen.Visibility = Visibility.Visible;
            shot_button.Visibility = Visibility.Visible;
            shot_button2.Visibility = Visibility.Visible;
            left_button.Visibility = Visibility.Collapsed;
            right_button.Visibility = Visibility.Collapsed;
        }

       

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            imgno = (imgno + Constants.MAX_BG_NUM - 1) % Constants.MAX_BG_NUM;
            BackGround_Screen.Source = bg_pool[imgno];

        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            imgno = (imgno + 1) % Constants.MAX_BG_NUM;
            BackGround_Screen.Source = bg_pool[imgno];

        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件
            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時
            count_timer = new DispatcherTimer();
            count_timer.Interval = new TimeSpan(0, 0, 0, 1, 0);

            count_timer.Tick += CountTimer_Tick;
            count_timer.Start();
        }

        public async void captureImg()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件
            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時
             
            List<int> pool = new List<int>();
            Facename_Pool = new Dictionary<int, string>();
            QR_DataBase = new Dictionary<string, string>();
            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            if (this.colorBitmap == null)
            {
                return;
            }

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));
            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Color" + time + ".jpg");
            // FileStream is IDisposable
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                encoder.Save(fs);
                fs.Close();
            }

            HeadRandom = System.IO.Path.GetRandomFileName().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                        
            Bitmap oribmp = new Bitmap(path);
            using (Bitmap tmpBmp = new Bitmap(oribmp))
            {
                tmpBmp.Save("back.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                if (Constants.SAVE_TO_CLOUD_DRIVE)
                {
                    Mount_path.Clear();
                    Mount_path.Append("Y:\\MTC\\Original\\");
                    Mount_path.Append("Original_");
                    Mount_path.Append(HeadRandom[0]);
                    Mount_path.Append(".jpg");
                    tmpBmp.Save(Mount_path.ToString(), System.Drawing.Imaging.ImageFormat.Jpeg);
                } 
            }

            view_mode = 1;
            loading_animation.Visibility = Visibility.Visible;
            BackGround_Screen.Visibility = Visibility.Visible;
            Figure_Screen.Visibility = Visibility.Visible;
            DefaultScreen.Visibility = Visibility.Collapsed;

            shot_button.Visibility = Visibility.Collapsed;
            shot_button2.Visibility = Visibility.Collapsed;
            check_button.Visibility = Visibility.Visible;
            retry_button.Visibility = Visibility.Visible;
            left_button.Visibility  = Visibility.Visible;
            right_button.Visibility = Visibility.Visible;

            BackGround_Screen.Source = bg_pool[imgno];


            this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            System.Console.WriteLine(path);
            using (Stream faceimagestream = File.OpenRead("back" + ".jpg"))
            {
                StringBuilder sb = new StringBuilder();
                // Create source
                // BitmapImage.UriSource must be in a BeginInit/EndInit block

                // Call the Face API - request the FaceId, FaceLandmarks, and all FaceAttributes

                sw.Start();//碼表開始計時
                var faceserviceclient = new FaceServiceClient("2c2a7f6eca9e4197926721a886786d6b");

                ProjectOxford.Face.Contract.Face[] faces = await faceserviceclient.DetectAsync(faceimagestream, true, true,
                    new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.HeadPose,FaceAttributeType.Glasses });
                
                sw.Stop();//碼錶停止
                string result3 = sw.Elapsed.TotalMilliseconds.ToString();
                System.Console.WriteLine("face api:" + result3);
                sw.Reset();

                if (faces.Length <= 0)
                {
                    System.Console.WriteLine("There is no face in current frame");
                    return;
                }

                int[] faceimg_x = new int[Constants.MAX_FACE_NUM];
                int[] faceimg_y = new int[Constants.MAX_FACE_NUM];
                int[] faceimg_width = new int[Constants.MAX_FACE_NUM];
                int[] faceimg_height = new int[Constants.MAX_FACE_NUM];
                int[] faceimg_age = new int[Constants.MAX_FACE_NUM];
                int[] faceimg_gender = new int[Constants.MAX_FACE_NUM]; //1 for man 2 for woman

                sw.Start();
                facecount = 0;
                foreach (var face in faces)
                {
                    facecount++;
                    // Get the detailed information from the face to display in the UI
                    //        sb.Append(GetFaceInfo(face));
                    int max_left = Convert.ToInt32(face.FaceLandmarks.EyebrowLeftOuter.X);
                    int max_right = Convert.ToInt32(face.FaceLandmarks.EyebrowRightOuter.X);
                    int max_buttom = Convert.ToInt32(face.FaceLandmarks.UnderLipBottom.Y);
                    int[] top = new int[4];
                    int max_top = int.MaxValue;
                    top[0] = Convert.ToInt32(face.FaceLandmarks.EyebrowLeftInner.Y);
                    top[1] = Convert.ToInt32(face.FaceLandmarks.EyebrowLeftOuter.Y);
                    top[2] = Convert.ToInt32(face.FaceLandmarks.EyebrowRightInner.Y);
                    top[3] = Convert.ToInt32(face.FaceLandmarks.EyebrowRightOuter.Y);
                    for (int k = 0; k <= 3; k++)
                    {
                        if (top[k] < max_top)
                        {
                            max_top = top[k];
                        }
                    }

                    int width = Convert.ToInt32(max_right - max_left);
                    int height = Convert.ToInt32(max_buttom - max_top);
                    int width_factor = Convert.ToInt32(width * 0.1);
                    int height_factor = Convert.ToInt32(height * 0.3);
                    System.Console.WriteLine("{0} {1}", max_left, max_top);
                    System.Console.WriteLine("{0} {1}", width, height);
                    System.Console.WriteLine("{0} {1}", width_factor, height_factor);
                    width = width + Convert.ToInt32(width_factor * 1);
                    height = height + Convert.ToInt32(height_factor * 1);
                    //  Bitmap bmp = new Bitmap(width, height);
                    float x = Convert.ToSingle(max_left - (width_factor)), y = Convert.ToSingle(max_top - (height_factor));

                    string randomName = System.IO.Path.GetRandomFileName();

                    string[] split_filestrs = randomName.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);


                    Bitmap CroppedImage = null;
                    if (face.FaceAttributes.HeadPose.Roll >= 10 || face.FaceAttributes.HeadPose.Roll <= -10)
                    {
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(Convert.ToInt32(face.FaceRectangle.Left), Convert.ToInt32(face.FaceRectangle.Top), face.FaceRectangle.Width, face.FaceRectangle.Height);
                        CroppedImage = new Bitmap(CropRotatedRect(oribmp, rect, Convert.ToSingle(face.FaceAttributes.HeadPose.Roll * -1), true));
                    }
                    else
                    {
                        CroppedImage = new Bitmap(oribmp.Clone(new System.Drawing.Rectangle(Convert.ToInt32(face.FaceRectangle.Left), Convert.ToInt32(face.FaceRectangle.Top), face.FaceRectangle.Width, face.FaceRectangle.Height), oribmp.PixelFormat));
                    }
                    faceimg_x[facecount] = Convert.ToInt32(x);
                    faceimg_y[facecount] = Convert.ToInt32(y);
                    faceimg_width[facecount] = width;
                    faceimg_height[facecount] = height;


                    StringBuilder st = new StringBuilder();
                    st.Append("MTC\\Face\\faceimg");
                    // st.Append(facecount.ToString());
                    st.Append(split_filestrs[0]);
                    Facename_Pool.Add(facecount, split_filestrs[0]);
                    st.Append(".png");
                    string outputFileName = st.ToString();
                    using (MemoryStream memory = new MemoryStream())
                    {
                        using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
                        {
                            CroppedImage.Save(memory, ImageFormat.Png);
                            byte[] bytes = memory.ToArray();
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Flush();
                            fs.Close();
                            memory.Flush();
                            memory.Close();
                        }

                    }

                    if (Constants.SAVE_TO_CLOUD_DRIVE) {
                        Mount_path.Clear();
                        Mount_path.Append("Y:\\MTC\\Face\\");
                        Mount_path.Append("face_");
                        Mount_path.Append(HeadRandom[0]);
                        Mount_path.Append("_");
                        Mount_path.Append(facecount.ToString());
                        Mount_path.Append(".png");
                        using (MemoryStream memory = new MemoryStream())
                        {
                            using (FileStream fs = new FileStream(Mount_path.ToString(), FileMode.Create, FileAccess.ReadWrite))
                            {
                                CroppedImage.Save(memory, ImageFormat.Png);
                                byte[] bytes = memory.ToArray();
                                fs.Write(bytes, 0, bytes.Length);
                                fs.Flush();
                                fs.Close();
                                memory.Flush();
                                memory.Close();
                            }

                        }
                    }
                    CroppedImage.Dispose();

                    if (Constants.STREAM_ANALYSTIC)
                    {
                        if(Constants.SAVE_TO_CLOUD_DRIVE)
                            Utils.sendFaceDetectedEvent(face, Mount_path.ToString());
                        else
                            Utils.sendFaceDetectedEvent(face, outputFileName); 
                    }


                    if (face.FaceAttributes.Gender.Equals("male"))
                    {
                        faceimg_gender[facecount] = 1;
                    }
                    else if (face.FaceAttributes.Gender.Equals("female"))
                    {
                        faceimg_gender[facecount] = 2;
                    }
                    faceimg_age[facecount] = Convert.ToInt32(face.FaceAttributes.Age);


                }
   
                if (faces.Length >= Constants.MAX_FACE_NUM)
                {
                    facecount = Constants.MAX_FACE_NUM-1;
                }

                sw.Stop();//碼錶停止
                string result4 = sw.Elapsed.TotalMilliseconds.ToString();
                System.Console.WriteLine("cal top & save faceimg :" + result4);
                sw.Reset();

                //body+face
                for (int j = 1; j <= facecount; j++)
                {
                    StringBuilder body_img_path = new StringBuilder();
                    //body_img.Append("Images/");

                    if (faceimg_gender[j] == 1)
                    {
                        body_img_path.Append("man_");
                    }
                    else if (faceimg_gender[j] == 2)
                    {
                        body_img_path.Append("woman_");
                    }

                    if (faceimg_age[j] <= 35)
                    {
                        body_img_path.Append("young");
                    }
                    else if (faceimg_age[j] > 35)
                    {
                        body_img_path.Append("mature");
                    }
                    body_img_path.Append(".png");

                    Image body_face;
                    Image body_frame = Image.FromFile(body_img_path.ToString());
                    using (body_frame)
                    {
                        using (var bitmap = new Bitmap(body_frame.Width, body_frame.Height))
                        {
                            using (var canvas = Graphics.FromImage(bitmap))
                            {
                                //  canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                canvas.DrawImage(body_frame,
                                                 new System.Drawing.Rectangle(0,
                                                               0,
                                                               body_frame.Width,
                                                               body_frame.Height),
                                                 new System.Drawing.Rectangle(0,
                                                               0,
                                                               body_frame.Width,
                                                               body_frame.Height),
                                                 GraphicsUnit.Pixel);

                                StringBuilder st = new StringBuilder();
                                st.Append("MTC\\Face\\faceimg");
                                st.Append(Facename_Pool[j]);
                                st.Append(".png");

                                body_face = Image.FromFile(st.ToString());
                                int bx = 0, by = 0;
                                if (body_img_path.ToString().Equals("man_mature.png"))
                                {
                                    bx = 760;
                                    by = 656;
                                }
                                else if (body_img_path.ToString().Equals("man_young.png"))
                                {
                                    bx = 890;
                                    by = 545;
                                }
                                else if (body_img_path.ToString().Equals("woman_mature.png"))
                                {
                                    bx = 873;
                                    by = 653;
                                }
                                else if (body_img_path.ToString().Equals("woman_young.png"))
                                {
                                    bx = 781;
                                    by = 659;
                                }

                                canvas.DrawImage(body_face, bx, by, 340, 340);

                                body_img_path.Replace(".png","_hat.png");
                                Image hatImg = Image.FromFile(body_img_path.ToString());
                                canvas.DrawImage(hatImg,
                                                new System.Drawing.Rectangle(0,
                                                              0,
                                                              body_frame.Width,
                                                              body_frame.Height),
                                                new System.Drawing.Rectangle(0,
                                                              0,
                                                              body_frame.Width,
                                                              body_frame.Height),
                                                GraphicsUnit.Pixel);


                                canvas.Save();
                            }
                            try
                            {
                                string Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                                string fi_path = System.IO.Path.Combine(Fi_Photos, "Body" + Facename_Pool[j] + ".png");
                                final_name = Facename_Pool[1];
                                using (Bitmap tempBitmap = new Bitmap(bitmap))
                                {
                                    using (Bitmap resizedBitmap = new Bitmap(tempBitmap, new System.Drawing.Size((int)(Constants.FIGURE_WIDTH * Constants.resizeRatio), (int)(Constants.FIGURE_HEIGHT * Constants.resizeRatio))))
                                    {
                                        resizedBitmap.Save(fi_path, System.Drawing.Imaging.ImageFormat.Png);
                                        Mode_State = State.Background; // Choose background
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine(ex);
                            }
                        }
                    }
                }

                this.figureBitmap.MakeTransparent(System.Drawing.Color.White);
                using (var bitmap = new Bitmap(Constants.BG_WIDTH, Constants.BG_HEIGHT))
                {
                    using (var canvas = Graphics.FromImage(bitmap))
                    {
                        for (int i = 1; i <= facecount; i++)
                        {

                            String Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                            String fi_path = System.IO.Path.Combine(Fi_Photos, "Body" + Facename_Pool[i] + ".png");
                            Image temp_body = Image.FromFile(fi_path);

                            int dx = Constants.POSITION_OFFSET[i - 1].X;// - Constants.FIGURE_OFFSET[0].X;
                            int dy = Constants.POSITION_OFFSET[i - 1].Y;// - Constants.FIGURE_OFFSET[0].Y;
                            //canvas.DrawImage(temp_body, dx, dy, Constants.FIGURE_WIDTH * Constants.resizeRatio, Constants.FIGURE_HEIGHT * Constants.resizeRatio);
                            canvas.DrawImage(temp_body, dx, dy, Constants.FIGURE_WIDTH * Constants.resizeRatio, Constants.FIGURE_HEIGHT * Constants.resizeRatio);

                        }

                        canvas.Save();
                        loading_animation.Visibility = Visibility.Collapsed;
                        Figure_Screen.Source = Utils.Bitmap2BitmapImage(bitmap);
                        System.Console.WriteLine("finish fig bitmap");
                    }
                    
                }
            }
        }

        public void SetName(string name)
        {
            shareVariable = name;
        }

        private static float RotatePoint_x(float x, float y, double degree)
        {
            //  x' = x cos f - y sin f
            return Convert.ToSingle(x * Math.Cos(degree * Math.PI / 180f) - y * Math.Sin(degree * Math.PI / 180f));
        }
        private static float RotatePoint_y(float x, float y, double degree)
        {
            //  y' = y cos f + x sin f
            return Convert.ToSingle(y * Math.Cos(degree * Math.PI / 180f) + x * Math.Sin(degree * Math.PI / 180f));
        }

        public static Bitmap CropRotatedRect(Bitmap source, System.Drawing.Rectangle rect, float angle, bool HighQuality)
        {
            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (System.Drawing.Drawing2D.Matrix mat = new System.Drawing.Drawing2D.Matrix())
                {
                    mat.Translate(-rect.Location.X, -rect.Location.Y);
                    System.Drawing.Point p = new System.Drawing.Point(rect.Location.X + rect.Width / 2, rect.Location.Y + rect.Height / 2);
                    mat.RotateAt(angle, p);
                    g.Transform = mat;
                    g.DrawImage(source, new System.Drawing.Point(0, 0));
                }
            }
            return result;
        }

        public Image RoundCorners(Image StartImage, int CornerRadius, System.Drawing.Color BackgroundColor)
        {
            CornerRadius *= 2;
            Bitmap RoundedImage = new Bitmap(StartImage.Width, StartImage.Height);
            Graphics g = Graphics.FromImage(RoundedImage);
            g.Clear(BackgroundColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            System.Drawing.Brush brush = new TextureBrush(StartImage);
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
            gp.AddArc(0, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
            g.FillPath(brush, gp);
            return RoundedImage;
        }


        public void ShuffleList<T>(ref List<T> list)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = list.Count - 1; i > 0; i--)
            {

                int p = rnd.Next(i);
                T swap = list[p];
                list[p] = list[i];
                list[i] = swap;
            }
        }


        // take as input an array of strings and rearrange them in random order
        public static void shuffle(List<int> list)
        {
            int N = list.Count;
            for (int i = 0; i < N; i++)
            {
                int r = (new Random().Next(0, N)); // between i and N-1
                exch(list, i, r);
            }
        }
        public static void exch(List<int> list, int i, int j)
        {

            int swap = list[i];
            list[i] = list[j];
            list[j] = swap;
        }
  

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private async void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }
                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        public string findQrCodeText(ZXing.Reader decoder, Bitmap bitmap)
        {
            byte[] bmpBytes = Utils.BitmapToByteArray(bitmap);

            var rgb = new ZXing.RGBLuminanceSource(bmpBytes, bitmap.Width, bitmap.Height);
            var hybrid = new ZXing.Common.HybridBinarizer(rgb);
            ZXing.BinaryBitmap binBitmap = new ZXing.BinaryBitmap(hybrid);

            var result = decoder.decode(binBitmap, null);
            if (result == null)
                return string.Empty;
            else
                return result.Text;
        }

       

        public void saveFinalImg(int imgno)
        {
            Image temp_body;
            Image frame = Utils.BitmapImage2Bitmap(bg_pool[imgno]);
            using (frame)
            {
                using (var bitmap = new Bitmap(frame.Width, frame.Height))
                {
                    using (var canvas = Graphics.FromImage(bitmap))
                    {
                        //  canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        canvas.DrawImage(frame,
                                            new System.Drawing.Rectangle(0,
                                                        0,
                                                        frame.Width,
                                                        frame.Height),
                                            new System.Drawing.Rectangle(0,
                                                        0,
                                                        frame.Width,
                                                        frame.Height),
                                            GraphicsUnit.Pixel);

                        for (int i = 1; i <= facecount; i++)
                        {

                            String Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                            String fi_path = System.IO.Path.Combine(Fi_Photos, "Body" + Facename_Pool[i] + ".png");
                            temp_body = Image.FromFile(fi_path);

                            int dx = Constants.POSITION_OFFSET[i - 1].X;// - Constants.FIGURE_OFFSET[0].X;
                            int dy = Constants.POSITION_OFFSET[i - 1].Y;// - Constants.FIGURE_OFFSET[0].Y;
                            canvas.DrawImage(temp_body, dx, dy, Constants.FIGURE_WIDTH * Constants.resizeRatio, Constants.FIGURE_HEIGHT * Constants.resizeRatio);

                        }

                        canvas.Save();

                    }
                    try
                    {
                        String resultPath = "MTC\\Result\\MTC_" + Facename_Pool[1] + ".jpg";
                        using (Bitmap tempBitmap = new Bitmap(bitmap))
                        {
                            tempBitmap.Save(resultPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }

                        Utils.sendFinalDetectedEvent(resultPath);

                        if (Constants.SAVE_TO_CLOUD_DRIVE)
                        {
                            StringBuilder st = new StringBuilder();
                            st.Append("Y:\\MTC\\Result\\");
                            st.Append("MTC_");
                            st.Append(Facename_Pool[1]);
                            st.Append(".jpg");
    
                            Thread myNewThread = new Thread(() => Utils.saveBitmap(resultPath, st.ToString()));
                            myNewThread.Start();
                        }
                       

                        Mode_State = State.Result;
                        BackGround_Screen.Source = Utils.Bitmap2BitmapImage(bitmap);
                        hand_text.Visibility = Visibility.Collapsed;
                        wave_rhandes.Visibility = Visibility.Collapsed;
                        wave_lhandes.Visibility = Visibility.Collapsed;
                        check_button.Visibility = Visibility.Visible;
                        retry_button.Visibility = Visibility.Visible;
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }
            }

        }
       
    }
}