using LINQtoCSV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

using System.IO;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Face;
using Newtonsoft.Json;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Utils
{
    static public class Constants
    {
        public const int QRIMG_SIZE = 800;
        public const bool STREAM_ANALYSTIC = true;
        public const bool WHITE_BOARDING = false;
        public const int MAX_BG_NUM = 13;
        public const int MAX_FACE_NUM = 4;
        enum Types { Male_Young, Male_Old, Female_Young, Female_Old };
        public const int FIGURE_WIDTH = 1858;
        public const int FIGURE_HEIGHT = 2480;
        public const int FIGURE_FACE_SIZE = 340;
        public const float resizeRatio = 0.3f;
        public const int BG_WIDTH = 1280;
        public const int BG_HEIGHT = 720;


        public const string filename = "account.csv";

        public static Point[] FIGURE_OFFSET = new Point[]
        {
            new Point(970, 880),
            new Point(970, 880),
            new Point(970, 880),
            new Point(970, 880)
        };

        public static Point[] POSITION_OFFSET = new Point[]
        {
            new Point(233,100),
            new Point(520,100),
            new Point(-100,100),
            new Point(820,100)
        };


    }

    public class Account
    {
        private static Dictionary<string, Account> accounts;

        [CsvColumn(FieldIndex = 3)]
        public string name { get; set; }

        [CsvColumn(FieldIndex = 4)]
        public string company { get; set; }

        [CsvColumn(FieldIndex = 6)]
        public string am { get; set; }

        [CsvColumn(FieldIndex = 7)]
        public string id { get; set; }


        [CsvColumn(FieldIndex = 0)]
        public string qrcode { get; set; }
        [CsvColumn(FieldIndex = 1)]
        public string ticket { get; set; }
        [CsvColumn(FieldIndex = 2)]
        public string email { get; set; }

        [CsvColumn(FieldIndex = 5)]
        public string job { get; set; }

        [CsvColumn(FieldIndex = 8)]
        public string url { get; set; }


        public static void init()
        {
            accounts = new Dictionary<string, Account>();
            CsvFileDescription cd = new CsvFileDescription
            {
                SeparatorChar = ',',
                FirstLineHasColumnNames = false,
                EnforceCsvColumnAttribute = true
            };
            CsvContext cc = new CsvContext();
            IEnumerable<Account> data = cc.Read<Account>(Constants.filename, cd);
            foreach (Account acc in data)
            {
                if (!accounts.ContainsKey(acc.id))
                {
                    accounts.Add(acc.id, acc);
                }
            }
        }

        public static string getNameById(string key)
        {
            if (accounts.ContainsKey(key))
                return accounts[key].name;
            else
                return string.Empty;
        }

        public static string getCompanyById(string key)
        {
            if (accounts.ContainsKey(key))
                return accounts[key].company;
            else
                return string.Empty;
        }

        public static string getAmById(string key)
        {
            if (accounts.ContainsKey(key))
                return accounts[key].am;
            else
                return null;
        }

    }

    class Utils
    {
        public void insertFigures(ref Bitmap figureImage)
        {
            using (var canvas = Graphics.FromImage(figureImage))
            {
                //  canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                canvas.DrawImage(figureImage,
                                    new System.Drawing.Rectangle(0,
                                                0,
                                                figureImage.Width,
                                                figureImage.Height),
                                    new System.Drawing.Rectangle(0,
                                                0,
                                                figureImage.Width,
                                                figureImage.Height),
                                    GraphicsUnit.Pixel);
            }
        }

        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
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

        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
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

        private static string EVENT_HUB = "opendemoeh";
        private static string CONNECTION_STRING = "Endpoint=sb://opendemoeh.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=6VujxD91yRAdF0Y3Hyq4UHIlnIetUm2ZcfJJ7QcfF6g=";
        private static string BLOB_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=opendemost;AccountKey=t6wYwnwoG1E6iuxSubks7OKlsJCRsELGyFRz7P65hPalOSJgO/BcrWuK2Q+vb9+5ZrPQAa5+STszN5aZYofgsA==";
        private static string FILE_URL = "https://opendemost.blob.core.windows.net/photo/";

        public static void sendFaceDetectedEvent(Face face, string path)
        {
            var fileName = path.Replace(Path.GetPathRoot(path), "");
            fileName.Replace("\\", "/");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BLOB_CONNECTION_STRING);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("photo");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            using (var fileStream = System.IO.File.OpenRead(path))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            Random rand = new Random();
            var eventHubClient = EventHubClient.CreateFromConnectionString(CONNECTION_STRING, EVENT_HUB);

            var dict = new Dictionary<string, string>();
            dict.Add("id", face.FaceId.ToString());
            dict.Add("gender", face.FaceAttributes.Gender);
            dict.Add("age", face.FaceAttributes.Age.ToString());
            dict.Add("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            dict.Add("smile", face.FaceAttributes.Smile.ToString());
            dict.Add("glasses", face.FaceAttributes.Glasses.ToString());
            dict.Add("avgs", rand.Next(5, 8).ToString());
            dict.Add("avgrank", (3 + rand.NextDouble() * 1.5).ToString());
            dict.Add("path", FILE_URL + fileName);

            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            eventHubClient.Send(new EventData(Encoding.UTF8.GetBytes(json)));
        }


        public static void saveBitmap(string param1, string param2)
        {
            //do stuff
            using (Bitmap tmpBmp = new Bitmap(param1))
            {
                tmpBmp.Save(param2, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }
        
    }
}
