using LINQtoCSV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    static public class Constants
    {
        public const int QRIMG_SIZE = 800;
        public const bool WHITE_BOARDING = false;
        public const int MAX_BG_NUM = 13;
        public const int MAX_FACE_NUM = 4;
        enum Types { Male_Young, Male_Old, Female_Young, Female_Old};
        public const int FIGURE_WIDTH = 1858;
        public const int FIGURE_HEIGHT = 2480;
        public const int FIGURE_FACE_SIZE = 340;
        public const float resizeRatio = 0.3f;

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
        public string  ticket{ get; set; }
        [CsvColumn(FieldIndex = 2)]
        public string email { get; set; }

        [CsvColumn(FieldIndex = 5)]
        public string job { get; set; }

        [CsvColumn(FieldIndex = 8)]
        public string url { get; set; }


    }

    class Utils
    {
        public static Bitmap insertFigures(Bitmap bgImage, Bitmap figImage, int index)
        {
            using (Graphics gr = Graphics.FromImage(bgImage))
            {
                int dx = Constants.POSITION_OFFSET[index].X - Constants.FIGURE_OFFSET[0].X;
                int dy = Constants.POSITION_OFFSET[index].Y - Constants.FIGURE_OFFSET[0].Y;
                gr.DrawImage(figImage, dx, dy, Constants.FIGURE_WIDTH, Constants.FIGURE_HEIGHT);
                
            }
            return bgImage;
        }
    }
}
