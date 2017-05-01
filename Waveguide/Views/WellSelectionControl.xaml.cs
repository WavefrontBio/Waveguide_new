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
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for WellSelectionDialog.xaml
    /// </summary>
    public partial class WellSelectionControl : UserControl
    {

        int m_rows;
        int m_cols;
        ObservableCollection<Tuple<int, int>> m_wellList;
        WriteableBitmap m_plateBitmap;
        WriteableBitmap m_selectBitmap;
        int m_xPixelRange;
        int m_yPixelRange;

        int m_startX;
        int m_startY;
        int m_endX;
        int m_endY;

        bool m_dragging;

        bool[,] m_selected;

        RowDefinition m_extraRow;
        ColumnDefinition m_extraColumn;

        Color m_SelectedWellColor;

        public event EventHandler NewWellSetSelected;

        protected virtual void OnNewWellSetSelected(WellSelectionEventArgs e)
        {
            if (NewWellSetSelected != null)
                NewWellSetSelected(this, e);
        }



        public WellSelectionControl()
        {
           
        }


        public void Init(int rows, int cols, ObservableCollection<Tuple<int, int>> preSelectedWellList = null)
        {
            m_SelectedWellColor = Colors.Red;

            m_rows = rows;
            m_cols = cols;
            m_wellList = new ObservableCollection<Tuple<int, int>>();

            if(preSelectedWellList != null)
            {
                foreach (Tuple<int, int> well in preSelectedWellList) m_wellList.Add(well);
            }

            

            m_selected = new bool[m_rows, m_cols];
            for (int r = 0; r < m_rows; r++)
                for (int c = 0; c < m_cols; c++)
                    m_selected[r, c] = false;

            foreach (Tuple<int, int> well in m_wellList)
            {
                int r = well.Item1;
                int c = well.Item2;
                m_selected[r, c] = true;
            }


            // this trick makes sure that the image pixel size is a nice integer multiple
            // of rows and cols
            m_xPixelRange = 1024 / m_cols * m_cols;
            m_yPixelRange = 1024 / m_rows * m_rows;

            InitializeComponent();

            m_plateBitmap = BitmapFactory.New(m_xPixelRange, m_yPixelRange);
            PlateImage.Source = m_plateBitmap;

            m_selectBitmap = BitmapFactory.New(m_xPixelRange, m_yPixelRange);
            SelectImage.Source = m_selectBitmap;

            m_dragging = false;

            SetUpButtons();

            DrawPlate();

        }

        private void SetUpButtons()
        {
            for(int r = 0; r<m_rows; r++)
            {
                RowDefinition gridRow = new RowDefinition();

                RowButtonGrid.RowDefinitions.Add(gridRow);
                gridRow.Height = new GridLength(1, GridUnitType.Star);

                Button button = new Button();
                button.Tag = r;
                button.Content = (char)(r + 65);
                button.FontSize = 8;
                button.Click += RowButton_Click;
                

                Grid.SetRow(button, r);
                Grid.SetColumn(button, 0);

                RowButtonGrid.Children.Add(button);
            }           
            // add extra row that is used in resizing
            m_extraRow = new RowDefinition();
            m_extraRow.Height = new GridLength((double)(m_yPixelRange % m_rows));
            RowButtonGrid.RowDefinitions.Add(m_extraRow);
         

            for(int c = 0; c<m_cols; c++)
            {
                ColumnDefinition gridCol = new ColumnDefinition();
                
                ColumnButtonGrid.ColumnDefinitions.Add(gridCol);
                gridCol.Width = new GridLength(1, GridUnitType.Star);

                Button button = new Button();
                button.Tag = c;
                button.Content = (c + 1).ToString(); button.FontSize = 8;
                button.Click += ColumnButton_Click;

                Grid.SetRow(button, 0);
                Grid.SetColumn(button, c);

                ColumnButtonGrid.Children.Add(button);
            }
            // add extra column that is used for resizing
            m_extraColumn = new ColumnDefinition();
            m_extraColumn.Width = new GridLength((double)(m_xPixelRange % m_cols));
            ColumnButtonGrid.ColumnDefinitions.Add(m_extraColumn);

        }


        void RowButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int row = (int)button.Tag;

            bool allSelected = true;

            for (int c = 0; c < m_cols; c++)
            {
                if (!m_selected[row, c])
                {
                    allSelected = false;
                    break;
                }
            }
            
            for (int c = 0; c < m_cols; c++)
            {
                m_selected[row, c] = !allSelected;
            }

            DrawPlate();

            BroadcastWellList();
        }



        void ColumnButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int col = (int)button.Tag;

            bool allSelected = true;

            for (int r = 0; r < m_rows; r++)
            {
                if (!m_selected[r, col])
                {
                    allSelected = false;
                    break;
                }
            }

            for (int r = 0; r < m_rows; r++)
            {
                m_selected[r, col] = !allSelected;
            }

            DrawPlate();

            BroadcastWellList();
        }


        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool allSelected = true;

            for (int r = 0; r < m_rows; r++)
                for (int c = 0; c < m_cols; c++ )
                {
                    if (!m_selected[r, c])
                    {
                        allSelected = false;
                        break;
                    }
                }

            for (int r = 0; r < m_rows; r++)
                for (int c = 0; c < m_cols; c++)
                {
                    m_selected[r, c] = !allSelected;
                }

            DrawPlate();

            BroadcastWellList();
        }



        public void DrawPlate()
        {                      
            int colWidth = m_xPixelRange / m_cols;
            int rowHeight = m_yPixelRange / m_rows;

            int x1;
            int x2;
            int y1;
            int y2;

            m_plateBitmap.Lock();

            m_plateBitmap.Clear();

            // OLD
            //for (int r = 0; r < m_rows; r++)
            //    for (int c = 0; c < m_cols; c++)
            //    {
            //        x1 = (c * colWidth);
            //        x2 = x1 + colWidth;
            //        y1 = (r * rowHeight);
            //        y2 = y1 + rowHeight;
            //        m_plateBitmap.DrawRectangle(x1, y1, x2, y2, Colors.Black);

            //        if(m_selected[r,c])
            //            m_plateBitmap.FillRectangle(x1+2, y1+2, x2-2, y2-2, Colors.Red);
            //    }

            // NEW
            x1 = 0; x2 = m_cols * colWidth;
            for (int r = 0; r <= m_rows; r++ )
            {
                y1 = r * rowHeight;
                m_plateBitmap.DrawLine(x1, y1, x2, y1, Colors.Black);
            }
            y1 = 0; y2 = m_rows * rowHeight;
            for (int c = 0; c <= m_cols; c++ )
            {
                x1 = c * colWidth;
                m_plateBitmap.DrawLine(x1, y1, x1, y2, Colors.Black);
            }

            int padding = 2;
            for (int r = 0; r < m_rows; r++)
                for (int c = 0; c < m_cols; c++)
                {
                    x1 = (c * colWidth);
                    x2 = x1 + colWidth;
                    y1 = (r * rowHeight);
                    y2 = y1 + rowHeight;                 

                    if (m_selected[r, c])  m_plateBitmap.FillRectangle(x1 + padding + 1, y1 + padding + 1, x2 - padding, y2 - padding, m_SelectedWellColor);
                }


                m_plateBitmap.Unlock();

        }

        private void SelectImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_dragging = true;

            Point pt = e.GetPosition(SelectImage);
           
            m_startX = (int)(pt.X / SelectImage.ActualWidth * m_xPixelRange);
            m_startY = (int)(pt.Y / SelectImage.ActualHeight * m_yPixelRange);
        }



        private void SelectImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            m_dragging = false;
            m_selectBitmap.Clear();
            Point pt = e.GetPosition(SelectImage);
            int x = (int)(pt.X / SelectImage.ActualWidth * m_xPixelRange);
            int y = (int)(pt.Y / SelectImage.ActualHeight * m_yPixelRange);

            if (x < m_startX)
            {
                m_endX = m_startX;
                m_startX = x;
            }
            else
            {
                m_endX = x;
            }

            if (y < m_startY)
            {
                m_endY = m_startY;
                m_startY = y;
            }
            else
            {
                m_endY = y;
            }

            int startCol = 0, endCol = 0, startRow = 0, endRow = 0;

            if(GetRowColFromPoint(m_startX,m_startY,ref startRow,ref startCol))
                if(GetRowColFromPoint(m_endX,m_endY,ref endRow, ref endCol))
                {
                    bool allSelected = true;
                    for (int r = startRow; r <= endRow; r++)
                        for (int c = startCol; c <= endCol; c++)
                            if (!m_selected[r, c]) allSelected = false;

                    for (int r = startRow; r <= endRow; r++)
                        for (int c = startCol; c <= endCol; c++)
                            m_selected[r, c] = !allSelected;
                }

            DrawPlate();

            BroadcastWellList();
            
        }


        private void BroadcastWellList()
        {
            m_wellList.Clear();
            for (int r = 0; r < m_rows; r++)
                for (int c = 0; c < m_cols; c++)
                {
                    if (m_selected[r, c]) m_wellList.Add(Tuple.Create<int, int>(r, c));
                }
            OnNewWellSetSelected(new WellSelectionEventArgs(m_wellList));
        }


        private void SelectImage_MouseLeave(object sender, MouseEventArgs e)
        {
            m_dragging = false;
            m_selectBitmap.Clear();
        }



        private void SelectImage_MouseMove(object sender, MouseEventArgs e)
        {
            if(m_dragging)
            {
                m_selectBitmap.Clear();
                Point pt = e.GetPosition(SelectImage);
                int x = (int)(pt.X / SelectImage.ActualWidth *m_xPixelRange);
                int y = (int)(pt.Y / SelectImage.ActualHeight * m_yPixelRange);

                if(x<m_startX)
                {
                    m_endX = m_startX;
                    m_startX = x;
                }
                else
                {
                    m_endX = x;
                }

                if (y < m_startY)
                {
                    m_endY = m_startY;
                    m_startY = y;
                }
                else
                {
                    m_endY = y;
                }

                m_selectBitmap.DrawRectangle(m_startX, m_startY, m_endX, m_endY, Colors.Blue);

            }
        }


        private bool GetRowColFromPoint(int x, int y, ref int row, ref int col)
        {
            bool success = true;

            int colWidth = m_xPixelRange / m_cols;
            int rowHeight = m_yPixelRange / m_rows;

            col = x / colWidth;
            row = y / rowHeight;

            if (row > (m_rows-1) || col > (m_cols-1)) success = false;
            return success;
        }

        private void PlateImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = MainGrid.ColumnDefinitions[1].ActualWidth;
            double height = MainGrid.RowDefinitions[1].ActualHeight;

            Size size = new Size();
            MainGrid.Measure(size);

            m_xPixelRange = (int)width;
            m_yPixelRange = (int)height;

            m_plateBitmap = BitmapFactory.New(m_xPixelRange, m_yPixelRange);
            PlateImage.Source = m_plateBitmap;

            m_selectBitmap = BitmapFactory.New(m_xPixelRange, m_yPixelRange);
            SelectImage.Source = m_selectBitmap;

            m_extraRow.Height = new GridLength((double)(m_yPixelRange % m_rows));
            m_extraColumn.Width = new GridLength((double)(m_xPixelRange % m_cols));

            DrawPlate();

        }

  

                
    }


    public class WellSelectionEventArgs : EventArgs
    {        
        private ObservableCollection<Tuple<int, int>> wellList;

        public WellSelectionEventArgs(ObservableCollection<Tuple<int, int>> _wellList)
        {
            wellList = _wellList;
        }

        public ObservableCollection<Tuple<int, int>> WellList
        {
            get { return wellList; }
            set { wellList = value; }
        }
    }


}
