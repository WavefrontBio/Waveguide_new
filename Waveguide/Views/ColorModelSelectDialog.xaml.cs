using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ColorModelSelectDialog.xaml
    /// </summary>
    public partial class ColorModelSelectDialog : Window
    {
        ObservableCollection<ColorModelContainer> m_colorModelList;
        List<DisplayListItem> m_displayList;

        public int dbID = 0;

        public ColorModelSelectDialog(ObservableCollection<ColorModelContainer> colorModelList)
        {
            m_colorModelList = colorModelList;

            // build display list
            m_displayList = new List<DisplayListItem>();

            for (int i = 0; i < m_colorModelList.Count(); i++)
            {
                WriteableBitmap bm = BuildColorMapBitmap(m_colorModelList[i]);  

                m_displayList.Add(new DisplayListItem(m_colorModelList[i].ColorModelID, bm, m_colorModelList[i].Description));
            }

            InitializeComponent();

            CarouselList.DataContext = m_displayList;
        }


        public WriteableBitmap BuildColorMapBitmap(ColorModelContainer modelContainer)
        {
            WriteableBitmap bmap = BitmapFactory.New(100, modelContainer.GradientSize);

            ColorModel model = new ColorModel(modelContainer.Description,modelContainer.MaxPixelValue, modelContainer.GradientSize);

            for (int i = 0; i < modelContainer.Stops.Count(); i++)
            {
                model.InsertColorStop(modelContainer.Stops[i].ColorIndex, modelContainer.Stops[i].Red, modelContainer.Stops[i].Green, modelContainer.Stops[i].Blue);
            }

            model.BuildColorGradient();
                      
            Color color = new Color();
            color.A = 255;

            for (int i = 0; i < model.m_gradientSize; i++)
            {
                color.R = model.m_gradient[i].m_red;
                color.G = model.m_gradient[i].m_green;
                color.B = model.m_gradient[i].m_blue;

                bmap.DrawLine(0, model.m_gradientSize - i, 100, model.m_gradientSize - i, color);
            }

         
            return bmap;
        }


        public class DisplayListItem : INotifyPropertyChanged
        {
            private WriteableBitmap _image;
            private string _desc;
            private int _dbID;

            public DisplayListItem(int id, WriteableBitmap img, string description)
            {
                _dbID = id;
                _image = img;
                _desc = description;
            }

            public WriteableBitmap m_image 
            {
                get { return _image; }
                set 
                {
                    _image = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_image"));
                }
            }

            public string m_desc
            {
                get { return _desc; }
                set
                {
                    _desc = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_desc"));
                }
            }

            public int m_dbID
            {
                get { return _dbID; }
                set
                {
                    _dbID = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_dbID"));
                }
            }

     
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

         }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            dbID = 0;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int ndx = CarouselList.SelectedIndex;

            if (ndx >= 0)
                dbID = m_colorModelList[ndx].ColorModelID;
            else dbID = 0;

            Close();
        }

    }


   


}
