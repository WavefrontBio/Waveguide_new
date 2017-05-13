using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for WellDisplayControl.xaml
    /// </summary>
    public partial class WellDisplayControl : UserControl
    {
        ObservableCollection<Tuple<int,int>> m_wellList;

        WriteableBitmap m_bitmap;
        int m_rows;
        int m_cols;
        int m_pixelW;
        int m_pixelH;
        float m_ratio;
        

        public WellDisplayControl()
        {
            InitializeComponent();
        }

        public void Init(int _rows, int _cols, ObservableCollection<Tuple<int,int>> wellList)
        {
            m_ratio = 1.5f;
            m_pixelW = 240;
            m_pixelH = (int)((float)m_pixelW / m_ratio);
            m_rows = _rows;
            m_cols = _cols;
            m_wellList = wellList;

            m_bitmap = BitmapFactory.New(m_pixelW, m_pixelH);

            PlateImage.Source = m_bitmap;

            Redraw();
        }

        public void Redraw()
        {
            m_bitmap.Clear();

            int padding = 2;

            int xmin = padding;
            int ymin = padding;
            int xmax = m_bitmap.PixelWidth - padding - padding;
            int ymax = m_bitmap.PixelHeight - padding - padding;

            int x1,x2,y1,y2;

            float stepY = (float)(ymax-ymin) / (float)(m_rows);
            if (stepY < 1.0f) stepY = 1.0f;
            x1 = xmin;
            x2 = xmax;
            for(int r = 0; r < (m_rows-1); r++)
            {
                y1 = (int)((float)(r + 1) * stepY) + ymin;
                m_bitmap.DrawLine(x1,y1,x2,y1,Colors.Black);                
            }


            float stepX = (float)(xmax-xmin) / (float)(m_cols);
            if (stepX < 1.0f) stepX = 1.0f;
            y1 = ymin;
            y2 = ymax;            
            for(int c = 0; c < (m_cols-1); c++)
            {
                x1 = (int)((float)(c+1) * stepX) + xmin;
                m_bitmap.DrawLine(x1, y1, x1, y2, Colors.Black);
            }

            m_bitmap.DrawRectangle(xmin,ymin, xmax, ymax, Colors.Red);


            // mark wells

            foreach (Tuple<int,int> well in m_wellList)
            {
                int row = well.Item1;
                int col = well.Item2;

                x1 = (int)((float)(col) * stepX) + xmin + 3;
                y1 = (int)((float)(row) * stepY) + ymin + 3;
                x2 = (int)((float)(col + 1) * stepX) + xmin-2;
                y2 = (int)((float)(row + 1) * stepY) + ymin-2;

                m_bitmap.FillRectangle(x1, y1, x2, y2, Colors.Blue);
            }
        }


        private void Resize(int pixelWidth, int pixelHeight)
        {
            m_pixelW = pixelWidth;
            m_pixelH = pixelHeight;
            m_bitmap = BitmapFactory.New(m_pixelW, m_pixelH);

            PlateImage.Source = m_bitmap;

            Redraw();
        }

      

        private void Border1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
          
            Size s = e.NewSize;
            int w = (int)s.Width;
            int h = (int)s.Height;
            
            if(e.WidthChanged)
            {
                h = (int)((float)w / m_ratio);
            }
            else
            {
                w = (int)((float)h * m_ratio);
            }
                        

            Resize(w, h);
        }
    }
}
