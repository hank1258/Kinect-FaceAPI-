using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.Drawing;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    /// <summary>
    /// Window1.xaml 的互動邏輯
    /// </summary>
    public partial class Window1 : Window
    {


        private MainWindow form1;
        string final_name;
        public Window1(MainWindow form1)
        {
            InitializeComponent();
            this.form1 = form1;
             
        }
        
        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            string Fi_Photos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string fi_path = System.IO.Path.Combine(Fi_Photos, "Final"+final_name+ ".jpg");
            // ... Create a new BitmapImage.
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(fi_path);
            b.EndInit();

            // ... Get Image reference from sender.
            var image = sender as System.Windows.Controls.Image;
            // ... Assign Source.
            image.Source = b;

        }

        public void SetName(string name)
        {
            final_name = name;
        }

        private void button_Click(object sender, EventArgs e)
        {
            string reverseString = "qrcode";
            form1.SetName(reverseString);
            this.Close();
        }

    }
}
