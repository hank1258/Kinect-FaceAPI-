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
    // using Microsoft.ProjectOxford.Face.Contract;
    using Microsoft.ProjectOxford.Face.Controls;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Threading;
    using System.Net.Mail;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Threading;
    using Emgu.CV;
    using System.Windows.Interop;

    enum State { Default, Background, Result};

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

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        int facecount = 0;
        int bg_facecount = 0;
        string shareVariable = "default";
        DispatcherTimer timer;
        DispatcherTimer bgImage_timer;
        int imgno = 1;
        string final_name = "";
        WriteableBitmap Background_image;
        int view_mode = 0;
        BitmapImage[] bg_pool = new BitmapImage[20];
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private bool IsLeftHandRaise = false;
        private bool IsRightHandRaise = false;
        private State Mode_State = State.Default;
        private string CurrentState = "D";
        Dictionary<int, string> Facename_Pool = new Dictionary<int, string>();
        string[] state_buffer;

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

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
            imgno = 1;
            timer.Tick += Timer_Tick;

            bgImage_timer = new DispatcherTimer();
            bgImage_timer.Interval = new TimeSpan(0, 0, 0, 2, 0);

            Load_BgImage();



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

                }
                Body body = this.bodies[bodyIndexForUse];

                string Fi_Photos, fi_path;
                switch (Mode_State)
                {
                    case State.Background:
                        if (!body.IsTracked)
                            break;
                        Console.WriteLine("body.IsTracked");
                        Joint userJoint_HandLeft = body.Joints[JointType.HandLeft];
                        Joint userJoint_ElbowLeft = body.Joints[JointType.ElbowLeft];
                        Joint userJoint_HandRight = body.Joints[JointType.HandRight];
                        Joint userJoint_ElbowRight = body.Joints[JointType.ElbowRight];
                        Joint userJoint_ShoulderCenter = body.Joints[JointType.SpineShoulder];
                        Joint userJoint_Spine = body.Joints[JointType.SpineMid];
                        Joint userJoint_Head = body.Joints[JointType.Head];
                        shot_button.Visibility = Visibility.Collapsed;
                        wave_lhandes.Visibility = Visibility.Visible;
                        wave_rhandes.Visibility = Visibility.Visible;
                        hand_text.Visibility = Visibility.Visible;
                        //右手舉起
                        if (userJoint_HandRight.Position.Y > userJoint_ElbowRight.Position.Y)
                        {
                            Console.Write("舉右手");
                            //右手X位置-右手手肘的位置
                            float distance = userJoint_HandRight.Position.X - userJoint_ElbowRight.Position.X;
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
                                imgno--;
                                if (imgno == 0)
                                {
                                    imgno = 14;
                                }
                                Thread.Sleep(200);
                                BackGround_Screen.Source = bg_pool[imgno];

                                CurrentState = "D";

                            }
                            else if (CurrentState.Equals("1"))
                            {
                                imgno++;
                                if (imgno == 15)
                                {
                                    imgno = 1;
                                }
                                Thread.Sleep(200);
                                BackGround_Screen.Source = bg_pool[imgno];

                                CurrentState = "D";
                            }

                        }//左手舉起
                        else if (userJoint_HandLeft.Position.Y > userJoint_ElbowLeft.Position.Y)
                        {
                        }

                        //雙手合十
                        if (userJoint_HandRight.Position.X - userJoint_HandLeft.Position.X < 0.01f)
                        {
                            Image temp_body;
                            Image frame = BitmapImage2Bitmap(bg_pool[imgno]);
                            
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

                                            Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                                            fi_path = System.IO.Path.Combine(Fi_Photos, "Body" + Facename_Pool[i] + ".png");
                                            temp_body = Image.FromFile(fi_path);

                                            int dx = Constants.POSITION_OFFSET[i - 1].X;// - Constants.FIGURE_OFFSET[0].X;
                                            int dy = Constants.POSITION_OFFSET[i - 1].Y;// - Constants.FIGURE_OFFSET[0].Y;
                                            canvas.DrawImage(temp_body, dx, dy, Constants.FIGURE_WIDTH * Constants.resizeRatio, Constants.FIGURE_HEIGHT * Constants.resizeRatio);

                                        }

                                        canvas.Save();

                                    }
                                    try
                                    {
                                        Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                                        fi_path = System.IO.Path.Combine(Fi_Photos, "MTC_" + Facename_Pool[1] + ".jpg");
                                        final_name = Facename_Pool[1];
                                        using (Bitmap tempBitmap = new Bitmap(bitmap))
                                        {
                                            tempBitmap.Save(fi_path, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        }
                                        Mode_State = State.Result;
                                        BackGround_Screen.Source = Bitmap2BitmapImage(bitmap);

                                    }
                                    catch (Exception ex)
                                    {
                                        System.Console.WriteLine(ex);
                                    }
                                }
                            }
                        }
                    


                        break;

                    case State.Result:
                        
 
                        break;

                }

            }
            #endregion
        }
        private void Load_BgImage()
        {
            for (int i = 1; i < 15; i++)
            {
                StringBuilder st = new StringBuilder();
                st.Append("Images/Slide");
                st.Append(i.ToString());
                st.Append(".JPG");
                bg_pool[i] = new BitmapImage(new Uri(st.ToString(), UriKind.RelativeOrAbsolute));
            }
        }
       
        private async void Timer_Tick(object sender, EventArgs e)
        {
             
            //imgno++;
            if (shareVariable.Equals("default"))
            {
                //do nothing
            }
            else if (shareVariable.Equals("qrcode"))
            {

                //System.Console.WriteLine(bg_pool[1]);
                /*
                Bitmap bitmap = BitmapFromWriteableBitmap(this.colorBitmap);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));
                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                StringBuilder qr_st = new StringBuilder();
                qr_st.Append("qr_temp");
                qr_st.Append(final_name);
                qr_st.Append(".jpg");
                string path = System.IO.Path.Combine(myPhotos, qr_st.ToString());
                */
                // FileStream is IDisposable
                try
                {
                    /*
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                    {
                        encoder.Save(fs);
                        fs.Close();
                    }
                    
                    */
                    string Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    
                    string fi_path = System.IO.Path.Combine(Fi_Photos, "Final" + final_name + ".jpg");
                    
                    Bitmap qr_bitmap = BitmapFromWriteableBitmap(this.colorBitmap);//new Bitmap(Image.FromFile(path));

                    ZXing.IBarcodeReader reader = new ZXing.BarcodeReader();
                    ZXing.Result result = reader.Decode(qr_bitmap);

                    //string result = findQrCodeText(new ZXing.QrCode.QRCodeReader(), qr_bitmap);


                    System.Console.WriteLine("result:" + result);
                    if (result != null)
                    {   
                        // label1.Text = result.Text;
                        System.Console.WriteLine(result);
                        try
                        {
                            SmtpClient mailServer = new SmtpClient("smtp.gmail.com", 587);
                            mailServer.EnableSsl = true;

                            mailServer.Credentials = new System.Net.NetworkCredential("test125899@gmail.com", "test1258");

                            string[] split_strs = result.Text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            //則string[] strs的內容為  mailto ,  user@mail.com
                            string from = "test125899@gmail.com";
                            //string to = "hank125899@gmail.com";
                            string to = split_strs[1];
                            System.Console.WriteLine(to);
                            MailMessage msg = new MailMessage(from, to);
                            msg.Subject = "Hello From Microsoft :)";
                            msg.Body = "The message goes here.";
                            msg.Attachments.Add(new Attachment(fi_path));
                            mailServer.Send(msg);
                            timer.Stop();
                            string reverseString = "default";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Unable to send email. Error : " + ex);
                        }
                    }

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                }

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
        private System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
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
                    Bitmap a=new Bitmap (Image.FromFile("Images/Slide1.JPG"));
                    return CreateBitmapSourceFromGdiBitmap(a);
                }
                else
                {
                    System.Console.Write("ASDADADADSADAS");
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


            List<int> pool = new List<int>();
            Facename_Pool = new Dictionary<int, string>();
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
            

  
            Bitmap oribmp = new Bitmap(path);
            oribmp.Save("back.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);


            this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            System.Console.WriteLine(path);
            using (Stream faceimagestream = File.OpenRead("back.jpg"))
            {


                StringBuilder sb = new StringBuilder();
                // Create source
                // BitmapImage.UriSource must be in a BeginInit/EndInit block

                // Call the Face API - request the FaceId, FaceLandmarks, and all FaceAttributes

                sw.Start();//碼表開始計時
                var faceserviceclient = new FaceServiceClient("2c2a7f6eca9e4197926721a886786d6b");

                ProjectOxford.Face.Contract.Face[] faces = await faceserviceclient.DetectAsync(faceimagestream, false, true, 
                    new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.HeadPose });

                sw.Stop();//碼錶停止
                string result3 = sw.Elapsed.TotalMilliseconds.ToString();
                System.Console.WriteLine("face api:" + result3);
                sw.Reset();

                if (faces.Length <= 0)
                {
                    return;
                }

                int[] faceimg_x = new int[Utils.Constants.MAX_FACE_NUM];
                int[] faceimg_y = new int[Utils.Constants.MAX_FACE_NUM];
                int[] faceimg_width = new int[Utils.Constants.MAX_FACE_NUM];
                int[] faceimg_height = new int[Utils.Constants.MAX_FACE_NUM];
                int[] faceimg_age = new int[Utils.Constants.MAX_FACE_NUM];
                int[] faceimg_gender = new int[Utils.Constants.MAX_FACE_NUM]; //1 for man 2 for woman

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

                    if (face.FaceAttributes.HeadPose.Roll >= 10 || face.FaceAttributes.HeadPose.Roll <= -10)
                    {
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(Convert.ToInt32(x), Convert.ToInt32(y), width, height);

                        Bitmap CroppedImage = new Bitmap(CropRotatedRect(oribmp, rect, Convert.ToSingle(face.FaceAttributes.HeadPose.Roll * -1), true));

                        faceimg_x[facecount] = Convert.ToInt32(x);
                        faceimg_y[facecount] = Convert.ToInt32(y);
                        faceimg_width[facecount] = width;
                        faceimg_height[facecount] = height;
                       

                        StringBuilder st = new StringBuilder();
                        st.Append("faceimg");
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
                        CroppedImage.Dispose();
                    }
                    else
                    {
                       // using (Bitmap CroppedImage = new Bitmap(oribmp.Clone(new System.Drawing.Rectangle(Convert.ToInt32(x), Convert.ToInt32(y), width, height), oribmp.PixelFormat)))
                        using (Bitmap CroppedImage = new Bitmap(oribmp.Clone(new System.Drawing.Rectangle(Convert.ToInt32(face.FaceRectangle.Left), Convert.ToInt32(face.FaceRectangle.Top), face.FaceRectangle.Width, face.FaceRectangle.Height), oribmp.PixelFormat)))
                        {
                            faceimg_x[facecount] = Convert.ToInt32(x);
                            faceimg_y[facecount] = Convert.ToInt32(y);
                            faceimg_width[facecount] = width;
                            faceimg_height[facecount] = height;
                            using (Graphics g = Graphics.FromImage(CroppedImage))
                            {
                                g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Brushes.White, 10), new Rectangle(0, 0, CroppedImage.Width, CroppedImage.Height));
                            }

                            StringBuilder st = new StringBuilder();
                            st.Append("faceimg");
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
                            CroppedImage.Dispose();
                        }
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
                sw.Stop();//碼錶停止
                string result4 = sw.Elapsed.TotalMilliseconds.ToString();
                System.Console.WriteLine("cal top & save faceimg :" + result4);
                sw.Reset();
                

                //body+face
                for (int j=1; j<=facecount; j++)
                {
                    StringBuilder body_img = new StringBuilder();
                    //body_img.Append("Images/");
                     
                    if (faceimg_gender[j] == 1)
                    {
                        body_img.Append("man_");
                    }
                    else if(faceimg_gender[j] == 2)
                    {
                        body_img.Append("woman_");
                    }

                    if (faceimg_age[j] <= 35)
                    {
                        body_img.Append("young");
                    }
                    else if (faceimg_age[j]>35)
                    {
                        body_img.Append("mature");
                    }
                    body_img.Append(".png");

                    Image body_face;
                    Image body_frame = Image.FromFile(body_img.ToString());
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
                                st.Append("faceimg");
                                st.Append(Facename_Pool[j]);
                                st.Append(".png");
                                
                                body_face = Image.FromFile(st.ToString());
                                int bx=0, by=0;
                                if (body_img.ToString().Equals("man_mature.png"))
                                {
                                    bx = 717;
                                    by = 521;
                                }else if (body_img.ToString().Equals("man_young.png"))
                                {
                                    bx = 769;
                                    by = 413;
                                }
                                    //canvas.DrawImage(temp_face, bg_faceimg_x[pool.IndexOf(i) + 1], bg_faceimg_y[pool.IndexOf(i) + 1], bg_faceimg_width[pool.IndexOf(i) + 1], bg_faceimg_height[pool.IndexOf(i) + 1]);
                                canvas.DrawImage(body_face,bx, by, 430, 500);
                                canvas.Save();
                            }
                            try
                            {
                                string Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                                string fi_path = System.IO.Path.Combine(Fi_Photos, "Body" + Facename_Pool[j] + ".png");
                                final_name = Facename_Pool[1];
                                using (Bitmap tempBitmap = new Bitmap(bitmap))
                                {
                                    using (Bitmap resizedBitmap = new Bitmap(tempBitmap, new System.Drawing.Size((int)(Utils.Constants.FIGURE_WIDTH * Utils.Constants.resizeRatio), (int)(Constants.FIGURE_HEIGHT * Constants.resizeRatio))))
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

            }
            //
            //Background_image = Image.FromFile("Status.png");
            /*Window1 frm = new Window1(this);
            frm.SetName(final_name);
            frm.Show();
            timer.Start();
            */
            view_mode = 1;
            BackGround_Screen.Visibility = Visibility.Visible;
            DefaultScreen.Visibility = Visibility.Collapsed;
            
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
                    mat.RotateAt(angle, rect.Location);
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


        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        private void DrawFaceLandmarks(DrawingContext drawingcontext, Face face, double resizefactor)
        {
            try
            {
                /*
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyebrowLeftInner.X * resizefactor, face.FaceLandmarks.EyebrowLeftInner.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyebrowLeftOuter.X * resizefactor, face.FaceLandmarks.EyebrowLeftOuter.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyebrowRightInner.X * resizefactor, face.FaceLandmarks.EyebrowRightInner.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyebrowRightOuter.X * resizefactor, face.FaceLandmarks.EyebrowRightOuter.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeLeftBottom.X * resizefactor, face.FaceLandmarks.EyeLeftBottom.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeLeftInner.X * resizefactor, face.FaceLandmarks.EyeLeftInner.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeLeftOuter.X * resizefactor, face.FaceLandmarks.EyeLeftOuter.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeLeftTop.X * resizefactor, face.FaceLandmarks.EyeLeftTop.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeRightBottom.X * resizefactor, face.FaceLandmarks.EyeRightBottom.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeRightInner.X * resizefactor, face.FaceLandmarks.EyeRightInner.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeRightOuter.X * resizefactor, face.FaceLandmarks.EyeRightOuter.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.EyeRightTop.X * resizefactor, face.FaceLandmarks.EyeRightTop.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.MouthLeft.X * resizefactor, face.FaceLandmarks.MouthLeft.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.MouthRight.X * resizefactor, face.FaceLandmarks.MouthRight.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseLeftAlarOutTip.X * resizefactor, face.FaceLandmarks.NoseLeftAlarOutTip.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseLeftAlarTop.X * resizefactor, face.FaceLandmarks.NoseLeftAlarTop.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseRightAlarOutTip.X * resizefactor, face.FaceLandmarks.NoseRightAlarOutTip.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseRightAlarTop.X * resizefactor, face.FaceLandmarks.NoseRightAlarTop.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseRootLeft.X * resizefactor, face.FaceLandmarks.NoseRootLeft.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseRootRight.X * resizefactor, face.FaceLandmarks.NoseRootRight.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.NoseTip.X * resizefactor, face.FaceLandmarks.NoseTip.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.PupilLeft.X * resizefactor, face.FaceLandmarks.PupilLeft.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.PupilRight.X * resizefactor, face.FaceLandmarks.PupilRight.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.UnderLipBottom.X * resizefactor, face.FaceLandmarks.UnderLipBottom.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.UnderLipTop.X * resizefactor, face.FaceLandmarks.UnderLipTop.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.UpperLipBottom.X * resizefactor, face.FaceLandmarks.UpperLipBottom.Y * resizefactor), 2, 2);
                drawingcontext.DrawEllipse(System.Windows.Media.Brushes.Green, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 2),
                    new System.Windows.Point(face.FaceLandmarks.UpperLipTop.X * resizefactor, face.FaceLandmarks.UpperLipTop.Y * resizefactor), 2, 2);
                    */
            }
            catch { }
        }
        private string GetFaceInfo(Face face)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                /*
                sb.AppendLine("Face ID: " + face.FaceId.ToString());
                sb.AppendLine("Face Attributes: ");
                sb.AppendLine("  Age: " + face.FaceAttributes.Age ?? "");
                sb.AppendLine("  Gender: " + face.FaceAttributes.Gender ?? "");
                sb.AppendLine("  Glasses: " + face.FaceAttributes.Glasses ?? "");
                sb.AppendLine("  Facial Hair: ");
                sb.AppendLine("    Beard: " + face.FaceAttributes.FacialHair.Beard ?? "");
                sb.AppendLine("    Moustache: " + face.FaceAttributes.FacialHair.Moustache ?? "");
                sb.AppendLine("    Sideburns: " + face.FaceAttributes.FacialHair.Sideburns ?? "");
                sb.AppendLine("  HeadPose: ");
                sb.AppendLine("    Roll:" + face.FaceAttributes.HeadPose.Roll ?? "");
                sb.AppendLine("    Pitch:" + face.FaceAttributes.HeadPose.Pitch ?? "");
                sb.AppendLine("    Yaw:" + face.FaceAttributes.HeadPose.Yaw ?? "");
                sb.AppendLine("  Smile: " + face.FaceAttributes.Smile ?? "");
                sb.AppendLine("Face Rectangle:");
                sb.AppendLine("  Top: " + face.FaceRectangle.Top.ToString());
                sb.AppendLine("  Left: " + face.FaceRectangle.Left.ToString());
                sb.AppendLine("  Width: " + face.FaceRectangle.Width.ToString());
                sb.AppendLine("  Height: " + face.FaceRectangle.Height.ToString());
                sb.AppendLine("Face Landmarks:");
                sb.AppendLine("  EyebrowLeftInner: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyebrowLeftInner.X.ToString() + ", " + face.FaceLandmarks.EyebrowLeftInner.Y.ToString());
                sb.AppendLine("  EyebrowLeftOuter: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyebrowLeftOuter.X.ToString() + ", " + face.FaceLandmarks.EyebrowLeftOuter.Y.ToString());
                sb.AppendLine("  EyebrowRightInner: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyebrowRightInner.X.ToString() + ", " + face.FaceLandmarks.EyebrowRightInner.Y.ToString());
                sb.AppendLine("  EyebrowRightOuter: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyebrowRightOuter.X.ToString() + ", " + face.FaceLandmarks.EyebrowRightOuter.Y.ToString());
                sb.AppendLine("  EyeLeftBottom: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeLeftBottom.X.ToString() + ", " + face.FaceLandmarks.EyeLeftBottom.Y.ToString());
                sb.AppendLine("  EyeLeftInner: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeLeftInner.X.ToString() + ", " + face.FaceLandmarks.EyeLeftInner.Y.ToString());
                sb.AppendLine("  EyeLeftOuter: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeLeftOuter.X.ToString() + ", " + face.FaceLandmarks.EyeLeftOuter.Y.ToString());
                sb.AppendLine("  EyeLeftTop: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeLeftTop.X.ToString() + ", " + face.FaceLandmarks.EyeLeftTop.Y.ToString());
                sb.AppendLine("  EyeRightBottom: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeRightBottom.X.ToString() + ", " + face.FaceLandmarks.EyeRightBottom.Y.ToString());
                sb.AppendLine("  EyeRightInner: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeRightInner.X.ToString() + ", " + face.FaceLandmarks.EyeRightInner.Y.ToString());
                sb.AppendLine("  EyeRightOuter: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeRightOuter.X.ToString() + ", " + face.FaceLandmarks.EyeRightOuter.Y.ToString());
                sb.AppendLine("  EyeRightTop: ");
                sb.AppendLine("    " + face.FaceLandmarks.EyeRightTop.X.ToString() + ", " + face.FaceLandmarks.EyeRightTop.Y.ToString());
                sb.AppendLine("  MouthLeft: ");
                sb.AppendLine("    " + face.FaceLandmarks.MouthLeft.X.ToString() + ", " + face.FaceLandmarks.MouthLeft.Y.ToString());
                sb.AppendLine("  MouthRight: ");
                sb.AppendLine("    " + face.FaceLandmarks.MouthRight.X.ToString() + ", " + face.FaceLandmarks.MouthRight.Y.ToString());
                sb.AppendLine("  NoseLeftAlarOutTip: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseLeftAlarOutTip.X.ToString() + ", " + face.FaceLandmarks.NoseLeftAlarOutTip.Y.ToString());
                sb.AppendLine("  NoseLeftAlarTop: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseLeftAlarTop.X.ToString() + ", " + face.FaceLandmarks.NoseLeftAlarTop.Y.ToString());
                sb.AppendLine("  NoseRightAlarOutTip: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseRightAlarOutTip.X.ToString() + ", " + face.FaceLandmarks.NoseRightAlarOutTip.Y.ToString());
                sb.AppendLine("  NoseRootLeft: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseRootLeft.X.ToString() + ", " + face.FaceLandmarks.NoseRootLeft.Y.ToString());
                sb.AppendLine("  NoseRootRight: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseRootRight.X.ToString() + ", " + face.FaceLandmarks.NoseRootRight.Y.ToString());
                sb.AppendLine("  NoseTip: ");
                sb.AppendLine("    " + face.FaceLandmarks.NoseTip.X.ToString() + ", " + face.FaceLandmarks.NoseTip.Y.ToString());
                sb.AppendLine("  PupilLeft: ");
                sb.AppendLine("    " + face.FaceLandmarks.PupilLeft.X.ToString() + ", " + face.FaceLandmarks.PupilLeft.Y.ToString());
                sb.AppendLine("  PupilRight: ");
                sb.AppendLine("    " + face.FaceLandmarks.PupilRight.X.ToString() + ", " + face.FaceLandmarks.PupilRight.Y.ToString());
                sb.AppendLine("  UnderLipBottom: ");
                sb.AppendLine("    " + face.FaceLandmarks.UnderLipBottom.X.ToString() + ", " + face.FaceLandmarks.UnderLipBottom.Y.ToString());
                sb.AppendLine("  UnderLipTop: ");
                sb.AppendLine("    " + face.FaceLandmarks.UnderLipTop.X.ToString() + ", " + face.FaceLandmarks.UnderLipTop.Y.ToString());
                sb.AppendLine("  UpperLipBottom: ");
                sb.AppendLine("    " + face.FaceLandmarks.UpperLipBottom.X.ToString() + ", " + face.FaceLandmarks.UpperLipBottom.Y.ToString());
                sb.AppendLine("  UpperLipTop: ");
                sb.AppendLine("    " + face.FaceLandmarks.UpperLipTop.X.ToString() + ", " + face.FaceLandmarks.UpperLipTop.Y.ToString());
                sb.AppendLine("");
                */
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
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
            byte[] bmpBytes = BitmapToByteArray(bitmap);

            var rgb = new ZXing.RGBLuminanceSource(bmpBytes, bitmap.Width, bitmap.Height);
            var hybrid = new ZXing.Common.HybridBinarizer(rgb);
            ZXing.BinaryBitmap binBitmap = new ZXing.BinaryBitmap(hybrid);

            var result = decoder.decode(binBitmap, null);
            if (result == null)
                return string.Empty;
            else
                return result.Text;
        }

        public static byte[] BitmapToByteArray(Bitmap bitmap)
        {

            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bmpdata.Stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }

        }
    }
}
