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
        public const int MAX_FACE_NUM = 4;
        enum Types { Male_Young, Male_Old, Female_Young, Female_Old};
        public const  int FIGURE_WIDTH = 1858;
        public const  int FIGURE_HEIGHT = 2480;
        public const float resizeRatio = 0.3f;
        public static Point[] FIGURE_OFFSET = new Point[]
        {
            new Point(970, 880),
            new Point(970, 880),
            new Point(970, 880),
            new Point(970, 880)
        };

        public static Point[] POSITION_OFFSET = new Point[]
        {
            new Point(-100,100),
            new Point(233,100),
            new Point(520,100),
            new Point(820,100)
        };


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
