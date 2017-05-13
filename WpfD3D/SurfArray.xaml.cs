using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfD3D
{
  
    public partial class SurfArray : UserControl
    {
        // define convenience structures Struct1 and Struct2
        struct Struct1
        {
            public Grid grid;
            public Canvas canvas;
            public Image image;
            public D3DImage d3dimage;
            public TextBlock textBlock;
            public UInt32 cameraID;
            public int surfaceIndex;  // surface index used by D3DSurfaceManager
            public uint width; // pixel width of D3D surface, should match image
            public uint height; // pixel height of D3D surface, should match image
            public bool selected;

            public void Clear()
            {
                grid = null;
                canvas = null;
                image = null;
                d3dimage = null;
                textBlock = null;
                cameraID = 0;
                surfaceIndex = -1;
                width = 0;
                height = 0;
                selected = false;
            }
        }

        struct Struct2
        {
            public D3DImage d3dimage;
            public int row;
            public int col;
            public int surfaceIndex;  // surface index used by D3DSurfaceManager
            public uint width; // pixel width of D3D surface, should match image
            public uint height; // pixel height of D3D surface, should match image           
       
            public void Clear()
            {
                d3dimage = null;
                row = -1;
                col = -1;
                surfaceIndex = -1;
                width = 0;
                height = 0;
            }
        }


        // Two dictionaries so that we can find the D3DImage either by row,column position in grid -OR- by cameraID
        Dictionary<Tuple<int, int>,Struct1> m_SurfMap1;
        Dictionary<UInt32, Struct2> m_SurfMap2;

        Grid m_SurfGrid;
        int m_numRows;
        int m_numCols;

        SolidColorBrush m_selectedColor;
        SolidColorBrush m_unselectedColor;
        SolidColorBrush m_titleColor;
        SolidColorBrush m_mainGridColor;
        double m_titleFontSize;
        double m_gridMargin;

        bool m_PanelsSelectable;

        IntPtr mp_D3D;
        IntPtr mp_D3D_Device;
        IntPtr mp_D3D_DeviceEx;


        Action<int, int, UInt32> m_callbackFunction;

        // Import the methods exported by the unmanaged D3DSurfaceManager.
        [DllImport("D3DSurfaceManager.dll")]
        static extern int CreateNewSurface(uint uWidth, uint uHeight, bool useAlpha);

        [DllImport("D3DSurfaceManager.dll")]
        static extern bool DestroySurface(int SurfaceIndex);

        [DllImport("D3DSurfaceManager.dll")]
        static extern void CreateSurfaceManager();

        [DllImport("D3DSurfaceManager.dll")]
        static extern int LoadNewImage(int SurfaceIndex, IntPtr pImageData, uint width, uint height, uint numBytes);

        [DllImport("D3DSurfaceManager.dll")]
        static extern int GetBackBufferNoRef(int SurfaceIndex, out IntPtr pSurface);

        [DllImport("D3DSurfaceManager.dll")]
        static extern int Test();

        [DllImport("D3DSurfaceManager.dll")]
        static extern int GetSurfaceData(int SurfaceIndex, out IntPtr pImageData, out uint width, out uint height);

        [DllImport("D3DSurfaceManager.dll")]        
        static extern void GetD3D_Objects(out IntPtr pD3D, out IntPtr pDevice, out IntPtr pDeviceEx);

        [DllImport("D3DSurfaceManager.dll")]
        static extern bool GetD3D_SurfaceParams(int SurfaceIndex, out IntPtr pSurface, out uint Width, out uint Height, out bool UseAlpha);

        [DllImport("D3DSurfaceManager.dll")]
        static extern void ReleaseSurfaceManager();


        public void Shutdown()
        {
            ReleaseSurfaceManager();
        }


        public int GetSurfaceIndex(int row, int col)
        {
            int surfNdx = -1;  // this gets returned if no surface exists at row,col
            Struct1 s1;            
            if (m_SurfMap1.TryGetValue(new Tuple<int, int>(row, col), out s1))
            {
                surfNdx = s1.surfaceIndex;
            }
            return surfNdx;
        }
        
        public D3DImage GetD3DImage(int row, int col)
        {
            D3DImage d3dImage = null;  // this gets returned if no surface exists at row,col
            Struct1 s1;
            if (m_SurfMap1.TryGetValue(new Tuple<int, int>(row, col), out s1))
            {
                d3dImage = s1.d3dimage;
            }
            return d3dImage;
        }

        public void RemoveFromDictionaries(int row, int col)
        {           
            Struct1 s1;
            if (m_SurfMap1.TryGetValue(new Tuple<int, int>(row, col), out s1))
            {                
                m_SurfMap1.Remove(new Tuple<int, int>(row, col));
                m_SurfMap2.Remove(s1.cameraID);                
            }
        }


   
        public SurfArray(int rows, int cols, Grid parentGrid)
        {
            InitializeComponent();
            m_numCols = 0;
            m_numRows = 0;

            m_PanelsSelectable = true;

            m_selectedColor = new SolidColorBrush(Colors.LightSeaGreen);
            m_unselectedColor = new SolidColorBrush(Colors.LightGray);
            m_titleColor = new SolidColorBrush(Colors.DarkBlue);
            m_mainGridColor = new SolidColorBrush(Colors.LightGray);

            m_titleFontSize = 14;
            m_gridMargin = 4;

            m_callbackFunction = null;

            m_SurfMap1 = new Dictionary<Tuple<int, int>, Struct1>();
            m_SurfMap2 = new Dictionary<uint, Struct2>();

            // create the D3D Surface Manager.  The Surface Manager takes care of all the D3D stuff for you.
            CreateSurfaceManager();

            // build the grid
            Build(rows, cols, parentGrid);
        }

        void Build(int rows, int cols, Grid parentGrid)
        {
            // TODO: need to clear out any existing surfaces in the D3DSurfaceManager, just in case this Build function gets called more than once

            m_numRows = rows;
            m_numCols = cols;            

            // Create Grid 
            m_SurfGrid = parentGrid; // new Grid();
            m_SurfGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            m_SurfGrid.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            m_SurfGrid.ShowGridLines = false;
            m_SurfGrid.Background = m_mainGridColor;

            // Create Columns
            for (int c = 0; c < cols; c++)
            {
                ColumnDefinition column = new ColumnDefinition();
                m_SurfGrid.ColumnDefinitions.Add(column);
            }

            // Create Rows
            for (int r = 0; r < rows; r++)
            {
                RowDefinition row = new RowDefinition();
                m_SurfGrid.RowDefinitions.Add(row);
            }


            // populate Grid
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    // create Grid that holds everything inside of each array position
                    Grid innerGrid = new Grid();
                    innerGrid.Margin = new Thickness(m_gridMargin);
                    innerGrid.Background = m_unselectedColor;
                    innerGrid.MouseLeftButtonUp += innerGrid_MouseLeftButtonUp;
                 
                    // add columns to grid
                    ColumnDefinition c1 = new ColumnDefinition();
                    innerGrid.ColumnDefinitions.Add(c1);

                    // add rows to grid
                    RowDefinition r1 = new RowDefinition();
                    r1.Height = GridLength.Auto;
                    innerGrid.RowDefinitions.Add(r1);
                    r1 = new RowDefinition();
                    innerGrid.RowDefinitions.Add(r1);

                    // add items to inner grid 
                        TextBlock txt1 = new TextBlock();
                        txt1.Text = r.ToString() + "," + c.ToString();
                        txt1.FontSize = m_titleFontSize;
                        txt1.FontWeight = FontWeights.Normal;
                        txt1.Foreground = m_titleColor;
                        txt1.VerticalAlignment = VerticalAlignment.Top;
                        txt1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        innerGrid.Children.Add(txt1);
                        Grid.SetRow(txt1, 0);
                        Grid.SetColumn(txt1, 0);

                        Image img = new Image();
                        D3DImage d3dimg = new D3DImage();
                        img.Source = d3dimg;
                        img.Stretch = Stretch.Uniform;
                        img.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        innerGrid.Children.Add(img);
                        Grid.SetRow(img, 1);
                        Grid.SetColumn(img, 0);

                        // create a Viewbox/Canvas on top of the Image so that we can draw over the image if needed (like selecting an ROI)
                        Viewbox viewbox = new Viewbox();
                        Canvas can = new Canvas();
                        can.Background = new SolidColorBrush(Colors.Transparent);
                        viewbox.Child = can;
                        viewbox.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        viewbox.Stretch = Stretch.Uniform;                        
                        innerGrid.Children.Add(viewbox);
                        Grid.SetRow(viewbox, 1);
                        Grid.SetColumn(viewbox, 0);
                        
                                        

                    // Add D3DImage to Map for lookup later
                    // Will throw exception. Item with the same key already exists.
                    Struct1 s1 = new Struct1();
                    s1.grid = innerGrid;
                    s1.canvas = can;
                    s1.image = img;
                    s1.d3dimage = d3dimg;
                    s1.textBlock = txt1;
                    s1.cameraID = 0; // no camera assigned yet
                    s1.surfaceIndex = -1; // not defined yet, i.e. CreateSurface not yet called for this panel of the array
                    s1.width = 0;
                    s1.height = 0;
                    s1.selected = false;
                    
                    m_SurfMap1.Add(new Tuple<int, int>(r, c), s1);

                    // Add innerGrid to main Grid
                    m_SurfGrid.Children.Add(innerGrid);
                    Grid.SetRow(innerGrid, r);
                    Grid.SetColumn(innerGrid, c);
                }

        }

        public void SetPanelsSelectable(bool panelsSelectable)
        {
            m_PanelsSelectable = panelsSelectable;
        }

        public void SetCallback(Action<int,int,UInt32> callbackFunction)
        {
            m_callbackFunction = callbackFunction;
        }

        public void ClearCallback()
        {
            m_callbackFunction = null;
        }

        void innerGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (m_PanelsSelectable && sender != null)
            {
                Grid _grid = sender as Grid;
                int _row = (int)_grid.GetValue(Grid.RowProperty);
                int _column = (int)_grid.GetValue(Grid.ColumnProperty);

                Struct1 s1;
                GetStruct1(_row, _column, out s1);

                // if the panel is already selected, unselect it
                if (s1.selected)
                {
                    s1.selected = false;
                    s1.grid.Background = m_unselectedColor;
                }
                else
                {
                    // unselect all panels
                    foreach (var item in m_SurfMap1)
                    {
                        Struct1 s = (Struct1)item.Value;
                        s.grid.Background = m_unselectedColor;
                        s.selected = false;
                    }

                    // select this panel                
                    s1.grid.Background = m_selectedColor;
                    s1.selected = true;
                }

                SetStruct1(_row, _column, s1);

                if (m_callbackFunction != null)
                    m_callbackFunction(_row, _column, s1.cameraID);

                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
            
        }

        public void SetDisplayParams(Color panelSelectedColor, Color panelUnselectedColor, Color panelTitleColor, 
                                     double panelTitleFontSize, Color parentBackgroundColor, double panelMargin)
        {
            m_selectedColor = new SolidColorBrush(panelSelectedColor);
            m_unselectedColor = new SolidColorBrush(panelUnselectedColor);
            m_titleColor = new SolidColorBrush(panelTitleColor);
            m_mainGridColor = new SolidColorBrush(parentBackgroundColor);
            m_titleFontSize = panelTitleFontSize;
            m_gridMargin = panelMargin;

            m_SurfGrid.Background = m_mainGridColor;            

            // iterate through all panels
            foreach (var item in m_SurfMap1)
            {
                Struct1 s = (Struct1)item.Value;
                if (s.selected)
                    s.grid.Background = m_selectedColor;                
                else                
                    s.grid.Background = m_unselectedColor;

                s.textBlock.Foreground = m_titleColor;
                s.textBlock.FontSize = m_titleFontSize;

                s.grid.Margin = new Thickness(m_gridMargin);
            }

        }

        public void SetPanelTitle(int row, int col, string title)
        {
            Struct1 s1;            
            if (m_SurfMap1.TryGetValue(new Tuple<int, int>(row, col), out s1))
            {
                if(s1.textBlock != null)
                    s1.textBlock.Text = title;
            }
        }

        void GetStruct1(int row, int col, out Struct1 s1)
        {
            
            if(!m_SurfMap1.TryGetValue(new Tuple<int,int>(row,col),out s1))
            {
                s1.Clear();
            }            
        }


        void GetStruct2(UInt32 cameraID, out Struct2 s2)
        {
            if(!m_SurfMap2.TryGetValue(cameraID,out s2))
            {
                s2.Clear();
            }
        }

        void SetStruct1(int row, int col, Struct1 s1)
        {            
            if(m_SurfMap1.ContainsKey(new Tuple<int,int>(row,col)))
            {
                m_SurfMap1[new Tuple<int, int>(row, col)] = s1;
            }
            else
            {
                m_SurfMap1.Add(new Tuple<int, int>(row, col), s1);
            }
        }


        void SetStruct2(UInt32 cameraID, Struct2 s2)
        {
            if (m_SurfMap2.ContainsKey(cameraID))
            {
                m_SurfMap2[cameraID] = s2;
            }
            else
            {
                m_SurfMap2.Add(cameraID, s2);
            }
        }


        public bool AssignCameraToPosition(int row, int col, UInt32 cameraID, uint pixelWidth, uint pixelHeight, string title, bool useAlpha)
        {
            // TODO:  consider adding another parameter to tell whether to call CreateNewSurface.  If we are using vglib to decode, the surface is created by vglib and a pointer to that surface
            //        will be returned in the output queue, so we shouldn't create another surface that won't be used...waste of resources.

            bool success = true;          
            Struct1 s1;

            // get a reference to the D3DImage that is at row,col of grid
            //GetD3DImage(row, col, out d3dimage, out currentCameraID, out surfIndex, out width, out height, out textBlock);
            GetStruct1(row, col, out s1);

            if(s1.d3dimage != null)
            {// found 

                if(s1.surfaceIndex != -1) // already has a DirectX Surface, so destroy it before creating an new one.
                {
                    DestroySurface(s1.surfaceIndex);
                }

                int surfIndex = CreateNewSurface(pixelWidth, pixelHeight, useAlpha);

                s1.cameraID = cameraID;
                s1.width = pixelWidth;
                s1.height = pixelHeight;
                s1.textBlock.Text = title;
                s1.surfaceIndex = surfIndex;
                s1.canvas.Width = pixelWidth;
                s1.canvas.Height = pixelHeight;

                // update the cameraID in m_SurfMap1                
                m_SurfMap1[new Tuple<int,int>(row,col)] = s1;

                Struct2 s2 = new Struct2();
                s2.d3dimage = s1.d3dimage;
                s2.row = row;
                s2.col = col;
                s2.surfaceIndex = s1.surfaceIndex;
                s2.width = pixelWidth;
                s2.height = pixelHeight;
                s2.surfaceIndex = surfIndex;

                // check to see if an entry in m_SurfMap2 already exists for this cameraID.  If so, just update.  If not, add new entry
                if (m_SurfMap2.ContainsKey(cameraID))
                {// already there, so just update                                        
                    m_SurfMap2[cameraID] = s2;
                }
                else
                {// not there, so add if the surface was create successfully
                    if (s2.surfaceIndex > -1)
                        m_SurfMap2.Add(cameraID, s2);
                    else
                        success = false;
                }
            }
            else
            { // no D3DImage found for row,col
                success = false;
            }

            return success;
        }



        public Canvas GetCanvas(UInt32 cameraID)
        {
            // return null if it fails
            Canvas can = null;
            Struct1 s1;
            Struct2 s2;
            GetStruct2(cameraID, out s2);
            if(s2.d3dimage != null)
            {
                GetStruct1(s2.row, s2.col, out s1);
                if(s1.d3dimage != null)
                {
                    can = s1.canvas;
                }
            }

            return can;    
        }

        public void PostNewImage(UInt32 cameraID, byte[] data)
        {
            Struct2 s2;

            GetStruct2(cameraID, out s2);

            PostNewImage(s2.surfaceIndex, s2.d3dimage, s2.width, s2.height, data);            
        }
 

        void PostNewImage(int surfaceIndex, D3DImage d3dImage, uint imageWidth, uint imageHeight, byte[] data)
        {
            int size = Marshal.SizeOf(data[0]) * data.Length;

            //int cnt = data.Length;

            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(data, 0, pnt, data.Length);
            }
            finally
            {
                // Free the unmanaged memory.
                // Marshal.FreeHGlobal(pnt);
            }


            IntPtr pSurface = IntPtr.Zero;
            int result = GetBackBufferNoRef(surfaceIndex, out pSurface);  // pSurface is a pointer to a IDirect3DSurface9
            if (pSurface != IntPtr.Zero && result == 1)
            {
                try
                {
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                    LoadNewImage(surfaceIndex, pnt, imageWidth, imageHeight, (uint)data.Length);
                    d3dImage.AddDirtyRect(new Int32Rect(0, 0, (int)imageWidth, (int)imageHeight));
                    d3dImage.Unlock();
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                }
            }

            Marshal.FreeHGlobal(pnt);
        }



        void PostNewGPUImage(int surfaceIndex, D3DImage d3dImage, uint imageWidth, uint imageHeight, byte[] data)
        {
            int size = Marshal.SizeOf(data[0]) * data.Length;

            //int cnt = data.Length;

            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(data, 0, pnt, data.Length);
            }
            finally
            {
                // Free the unmanaged memory.
                // Marshal.FreeHGlobal(pnt);
            }


            IntPtr pSurface = IntPtr.Zero;
            int result = GetBackBufferNoRef(surfaceIndex, out pSurface);  // pSurface is a pointer to a IDirect3DSurface9
            if (pSurface != IntPtr.Zero && result == 1)
            {
                try
                {
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);

                    // copy GPU array into IDirect3DSurface9
                    

                    d3dImage.AddDirtyRect(new Int32Rect(0, 0, (int)imageWidth, (int)imageHeight));
                    d3dImage.Unlock();
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                }
            }

            Marshal.FreeHGlobal(pnt);
        }


        public void GetSurface(int surfaceIndex, out IntPtr data, ref uint width, ref uint height)
        {
            IntPtr pData = IntPtr.Zero;
            GetSurfaceData(surfaceIndex, out pData, out width, out height);

            data = pData;
        }


        public void PostNewImage(UInt32 cameraID, IntPtr pSurface)
        {
            // use this function if the surface data is already on the GPU and you have an IntPtr to the surface

            Struct2 s2;

            GetStruct2(cameraID, out s2);

            if (s2.d3dimage != null)
            {
                try
                {
                    s2.d3dimage.Lock();
                    s2.d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                    s2.d3dimage.AddDirtyRect(new Int32Rect(0, 0, (int)s2.width, (int)s2.height));
                    s2.d3dimage.Unlock();
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                }
            }
        }

        public void GetD3DObjects(out IntPtr pD3D, out IntPtr pDevice, out IntPtr pDeviceEx)
        {
            mp_D3D = IntPtr.Zero;
            mp_D3D_Device = IntPtr.Zero;
            mp_D3D_DeviceEx = IntPtr.Zero;
            GetD3D_Objects(out mp_D3D, out mp_D3D_Device, out mp_D3D_DeviceEx);
            pD3D = mp_D3D;
            pDevice = mp_D3D_Device;
            pDeviceEx = mp_D3D_DeviceEx;
        }

        public void GetSurface_Params(int surfaceIndex, out IntPtr pSurface, out uint width, out uint height, out bool UseAlpha)
        {
            GetD3D_SurfaceParams(surfaceIndex, out pSurface, out width, out height, out UseAlpha);
        }

    } // END class SurfArray



    public static class HRESULT
    {
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void Check(int hr)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }


} // END namespace WpfD3D
