using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for MaskManager.xaml
    /// </summary>
    public partial class MaskManager : Window
    {
        

        WriteableBitmap m_maskImage;
        WriteableBitmap m_pixelImage;

        public MaskViewModel m_mask;
        int m_xImageSize;
        int m_yImageSize;

        bool m_isDragging;
        bool m_isStretching;       

        ColorModel m_colorModel;

        Color m_maskColor;

        WaveguideDB wgDB;

        ushort[] m_image;  // raw grayscale image from camera

        RangeClass m_range;

        ReferenceImageContainer m_refImage;


        public MaskManager()
        {
            m_mask = new MaskViewModel();
            
            m_isDragging = false;
            m_isStretching = false;

            InitializeComponent();

            this.DataContext = m_mask;
                       
            m_maskColor = Colors.Red;

            Init();
                      
            
        }


        public void Init()
        {
            wgDB = new WaveguideDB();
            m_range = new RangeClass();
            
            // setup default color model
            m_colorModel = new ColorModel("Default");
            m_colorModel.InsertColorStop(0, 0, 0, 0);
            m_colorModel.InsertColorStop(1023, 255, 255, 255);
            m_colorModel.m_controlPts[1].m_value = 0;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = 100;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorGradient();
            m_colorModel.BuildColorMap();

            RangeSlider.DataContext = m_range;
            m_range.RangeMin = 0;
            m_range.RangeMax = 100;


            // create default image
            int width = GlobalVars.PixelWidth;
            int height = GlobalVars.PixelHeight;
            
            int numpixels = width*height;
            ushort[] defaultImage = new ushort[numpixels];
            for (int i = 0; i < numpixels; i++) defaultImage[i] = 0;  // black image

            SetImage(defaultImage, width, height);

            SetImageSize(width, height);

            m_mask.Reset((int)m_maskImage.Width, (int)m_maskImage.Height, m_mask.Mask.Rows, m_mask.Mask.Cols);

            m_mask.Mask.Description = "";
            m_mask.Mask.Shape = 0;
        }


        public void SetImageSize(int imagePixelWidth, int imagePixelHeight)
        {
            m_xImageSize = imagePixelWidth;
            m_yImageSize = imagePixelHeight;

            m_maskImage = BitmapFactory.New(m_xImageSize, m_yImageSize);
            m_pixelImage = BitmapFactory.New(m_xImageSize, m_yImageSize);

            MaskImage.Source = m_maskImage;
            PixelImage.Source = m_pixelImage;
        }





/// <summary>
/// ////////////////////////////////////////////////////
/// </summary>

        public void DrawMask()
        {
            if (m_maskImage == null) return;

            m_maskImage.Clear();


            // TODO: these values should be retrieve from the camera or from global variables
            int imagePixelWidth = 1024;
            int imagePixelHeight = 1024;

            
            int[] xp;
            int[] yp;
            int xmin=0, ymin=0, xmax=0, ymax=0;

            int numPts = (m_mask.Mask.Shape == 0) ? 5 : m_mask.Mask.NumEllipseVertices + 1;
            int[] pts = new int[numPts*2];

            // draw the mask
            for (int r = 0; r < m_mask.Mask.Rows; r++)
                for (int c = 0; c < m_mask.Mask.Cols; c++)
                {
                    // assume 1x1 binning on reference image
                    m_mask.Mask.CalculateApertureVertices(r,c, imagePixelWidth, imagePixelHeight,1,1,
                        out xp, out yp, ref xmin, ref ymin, ref xmax, ref ymax);

                    for (int i = 0; i < numPts-1; i++)
                    {
                        pts[i * 2] = xp[i];  
                        pts[i * 2 + 1] = yp[i];
                    }

                    pts[(numPts*2) - 2] = pts[0];
                    pts[(numPts*2) - 1] = pts[1];
                     
                    m_maskImage.DrawPolyline(pts, m_maskColor);

                    if ((r == 0 && c == 0) || (r == m_mask.Mask.Rows - 1 && c == m_mask.Mask.Cols - 1))
                    {
                        int x = m_mask.Mask.XOffset + (int)(c * m_mask.Mask.XStep);
                        int y = m_mask.Mask.YOffset + (int)(r * m_mask.Mask.YStep);
                        int cX = 0, cY = 0;
                        m_mask.Mask.RotatePoint(m_mask.Mask.XOffset, m_mask.Mask.YOffset, m_mask.Mask.Angle, x, y, out cX, out cY);

                        m_maskImage.FillEllipse(cX - 2, cY - 2, cX + 2, cY + 2, Colors.Yellow);
                    }

                }



            //// just for testing pixelList
            //m_mask.Mask.BuildPixelList(imagePixelWidth, imagePixelHeight);

            //// turn on all pixels in pixelList
            //for (int r = 0; r < m_mask.Mask.Rows; r++)
            //    for (int c = 0; c < m_mask.Mask.Cols; c++)
            //    {
            //        foreach(int ndx in m_mask.Mask.PixelList[r,c])
            //        {
            //            int y = ndx / imagePixelWidth;                        
            //            int x = ndx - (y*imagePixelWidth);
                        
            //            if(x>-1 && x<imagePixelWidth && y>-1 && y<imagePixelHeight)
            //                m_maskImage.SetPixel(x,y, Colors.Yellow);
            //        }
            //    }
        }


/// <summary>
/// ////////////////////////////////
/// </summary>



        //public void DrawMask()
        //{
        //    DrawMask1();
        //    return;

        //    if (m_maskImage == null) return;

        //    m_maskImage.Clear();

        //    int[] pts;
        //    int[] templatePts;
        //    int numberOfPoints = 0;

        //    if (m_mask.Mask.Shape == 0) // rectangular
        //    {
        //        numberOfPoints = 2;
        //        templatePts = new int[numberOfPoints * 2];
        //        pts = new int[numberOfPoints * 2];

        //        pts[0] = m_mask.Mask.XOffset - m_mask.Mask.XSize / 2;
        //        pts[1] = m_mask.Mask.YOffset - m_mask.Mask.YSize / 2;

        //        pts[2] = m_mask.Mask.XOffset + m_mask.Mask.XSize / 2;
        //        pts[3] = m_mask.Mask.YOffset + m_mask.Mask.YSize / 2;

        //        //pts[4] = m_mask.m_xOffset + m_mask.m_xSize / 2;
        //        //pts[5] = m_mask.m_yOffset + m_mask.m_ySize / 2;

        //        //pts[6] = m_mask.m_xOffset - m_mask.m_xSize / 2;
        //        //pts[7] = m_mask.m_yOffset + m_mask.m_ySize / 2;

        //        //pts[8] = pts[0];
        //        //pts[9] = pts[1];

        //        for (int i = 0; i < numberOfPoints; i++)
        //        {
        //            RotatePoint(m_mask.Mask.XOffset, m_mask.Mask.YOffset, m_mask.Mask.Angle, pts[i * 2], pts[i * 2 + 1], out templatePts[i * 2], out templatePts[i * 2 + 1]);
        //        }

        //    }
        //    else
        //    {  // elliptical
        //        numberOfPoints = 24;
        //        templatePts = new int[numberOfPoints * 2];
        //        pts = new int[numberOfPoints * 2];

        //        double step = 2*Math.PI / (numberOfPoints-1);
        //        double angle = 0;

        //        for (int i = 0; i < numberOfPoints-1; i++)
        //        {
        //            pts[2 * i] = m_mask.Mask.XOffset + (int)(m_mask.Mask.XSize/2 * Math.Cos(angle));
        //            pts[2 * i + 1] = m_mask.Mask.YOffset + (int)(m_mask.Mask.YSize/2 * Math.Sin(angle));
        //            angle += step;
        //        }

        //        pts[numberOfPoints*2-2] = pts[0];
        //        pts[numberOfPoints*2-1] = pts[1];

        //        for (int i = 0; i < numberOfPoints; i++)
        //        {
        //            RotatePoint(m_mask.Mask.XOffset, m_mask.Mask.YOffset, m_mask.Mask.Angle, pts[i * 2], pts[i * 2 + 1], out templatePts[i * 2], out templatePts[i * 2 + 1]);
        //        }
        //    }



        //    // draw the mask
        //    for (int r = 0; r < m_mask.Mask.Rows; r++)
        //        for (int c = 0; c < m_mask.Mask.Cols; c++)
        //        {
        //            // calculate unrotated center of aperature at r,c
        //            int centerX = (int)(m_mask.Mask.XOffset + c * m_mask.Mask.XStep);
        //            int centerY = (int)(m_mask.Mask.YOffset + r * m_mask.Mask.YStep);

        //            // declare variables for rotated center
        //            int cX;
        //            int cY;

        //            // rotate aperature center by m_mask.m_angle around aperture center at r=0, c=0
        //            RotatePoint(m_mask.Mask.XOffset, m_mask.Mask.YOffset, m_mask.Mask.Angle, centerX, centerY, out cX, out cY);

        //            // translate templatePts to cX,cY
        //            for (int i = 0; i < numberOfPoints; i++)
        //            {
        //                pts[i * 2] = templatePts[i * 2] + (cX-m_mask.Mask.XOffset);
        //                pts[i * 2 + 1] = templatePts[i * 2 + 1] + (cY-m_mask.Mask.YOffset);
        //            }


        //            if (m_mask.Mask.Shape == 0) // rectangular
        //            {
        //                m_maskImage.DrawRectangle(pts[0], pts[1], pts[2], pts[3], m_maskColor);
        //            }
        //            else  // elliptical
        //                m_maskImage.DrawPolyline(pts, m_maskColor);

        //            if ((r == 0 && c == 0) || (r == m_mask.Mask.Rows - 1 && c == m_mask.Mask.Cols - 1))
        //            {
        //                m_maskImage.FillEllipse(cX - 3, cY - 3, cX + 3, cY + 3, m_maskColor);
        //            }

        //        }
        //}


        public void RotatePoint(int cx, int cy, double angle, int px, int py, out int pxRotated, out int pyRotated)
        {
            // cx,cy is the center of rotation (pixels)
            // angle is the amount to rotate (degs)
            // px,py is the point to rotate
            // pxRotated, pyRotated is the new location of the point after rotation

            double s = Math.Sin(angle * Math.PI / 180);
            double c = Math.Cos(angle * Math.PI / 180);

            // translate point back to origin:
            px -= cx;
            py -= cy;

            // rotate point
            double xnew = px * c - py * s;
            double ynew = px * s + py * c;

            // translate point back:
            pxRotated = (int)(xnew + cx);
            pyRotated = (int)(ynew + cy);
        }


        


        private void MaskParameter_ValueChanged(object sender, EventArgs e)
        {
            DrawMask();
        }

        private void Rectangular_Checked(object sender, RoutedEventArgs e)
        {
            if (RectangularRadioButton.IsChecked == true)
            {
                m_mask.Mask.Shape = 0;
                DrawMask();
            }
        }

        private void Elliptical_Checked(object sender, RoutedEventArgs e)
        {
            if (EllipticalRadioButton.IsChecked == true)
            {
                m_mask.Mask.Shape = 1;
                DrawMask();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_maskImage == null) return;

            m_mask.Reset((int)m_maskImage.Width, (int)m_maskImage.Height, m_mask.Mask.Rows, m_mask.Mask.Cols);           
        }



        public void SetImage(ushort[] image, int width, int height)
        {
            m_image = image;

            if (!ImageDisplay.IsReady()) ImageDisplay.Init(width, height, m_colorModel.m_maxPixelValue, m_colorModel.m_colorMap);

            ImageDisplay.DisplayImage(m_image);
        }


        private void Image_Load_Click(object sender, EventArgs e)
        {

            bool success = wgDB.GetAllReferenceImages();
            if (success)
            {
                ImageSelectDialog diag = new ImageSelectDialog();

                foreach (ReferenceImageContainer refImage in wgDB.m_refImageList)
                {
                    diag.AddImage(refImage.ImageData, refImage.Width, refImage.Height, refImage.Description, refImage.ReferenceImageID);
                }

                diag.ShowDialog();

                if (diag.result)
                {
                    LoadReferenceImage(diag.databaseID);
                }
            }
        }



        public void LoadReferenceImage(int refImageID)
        {
            bool success = wgDB.GetReferenceImage(refImageID, out m_refImage);

            if (success)
            {
                if (m_colorModel.m_maxPixelValue != m_refImage.MaxPixelValue)
                {
                    m_colorModel.SetMaxPixelValue(m_refImage.MaxPixelValue);
                    m_colorModel.BuildColorMap();
                }

                SetImage(m_refImage.ImageData, m_refImage.Width, m_refImage.Height);

                m_mask.Mask.ReferenceImageID = m_refImage.ReferenceImageID;

            }
        }
        


        private void Mask_Load_Click(object sender, EventArgs e)
        {
            ListSelectionDialog diag = new ListSelectionDialog();

            bool success = wgDB.GetAllMasks();

            if (success)
            {
                for (int i = 0; i < wgDB.m_maskList.Count(); i++)
                {
                    diag.AddItemToList(wgDB.m_maskList[i].Description, wgDB.m_maskList[i].MaskID);
                }

                diag.ShowDialog();

                if (diag.m_itemSelected)
                {
                    MaskContainer mask;
                    success = wgDB.GetMask(diag.m_databaseID, out mask);

                    if (success)
                    {
                        m_mask.SetupMaskFromContainer(mask);

                        //success = wgDB.GetReferenceImage(mask.ReferenceImageID, out m_refImage);

                        //if (success)
                        //{
                        //    if (m_colorModel.m_maxPixelValue != m_refImage.MaxPixelValue)
                        //    {
                        //        m_colorModel.SetMaxPixelValue(m_refImage.MaxPixelValue);
                        //        m_colorModel.BuildColorMap();
                        //    }

                        //    SetImage(m_refImage.ImageData, m_refImage.Width, m_refImage.Height);
                        //}

                        DrawMask();
                    }
                }
            }
        }

        private void Mask_Delete_Click(object sender, EventArgs e)
        {
            bool done = false;
            bool success; 

            if(m_mask.Mask.MaskID==0) // a mask has not been loaded or this mask has not been saved
            {
                MessageBox.Show("This Mask has not been saved, so there is nothing to delete." + m_mask.Mask.Description + " ?", 
                                "Delete Not Necessary", MessageBoxButton.OK, MessageBoxImage.Information);
                done = true;
            }
            

            if(!done)
            {
                // get Mask from database with MaskID = m_mask.m_maskID so that we display the name of the mask that will be deleted.
                // This is done since it is possible that the Name was edited in the display but not saved.

                MaskContainer tempMask;
                success = wgDB.GetMask(m_mask.Mask.MaskID, out tempMask);

                if (success && tempMask != null)
                {
                    MessageBoxResult result = MessageBox.Show("Are you sure you want to DELETE: " + tempMask.Description + " ?",
                                                              "Verify Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        success = wgDB.DeleteMask(m_mask.Mask.MaskID);
                        Init();
                        DrawMask();
                    }
                }
                else
                {
                    MessageBox.Show("No matching mask found in the database, so there is nothing to delete.",
                                    "Delete Not Necessary", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }



        public class RangeClass
        {
            public int RangeMin
            {
                get;
                set;
            }

            public int RangeMax
            {
                get;
                set;
            }

        }

       
        

        private void RangeMinThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.BuildColorMap();

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }

        }


        private void RangeMaxThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }

        private void RangeSlider_TrackFillDragCompleted(object sender, Infragistics.Controls.Editors.TrackFillChangedEventArgs<double> e)
        {
            m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();

            WG_Color color = m_colorModel.m_colorMap[500];

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }

        private void MaskImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedPoint = e.GetPosition((Image)sender);
            // coordinates are now available in clickedPoint.X and clickedPoint.Y

            double windowSizeX = ((Image)sender).ActualWidth;
            double windowSizeY = ((Image)sender).ActualHeight;

            int x = (int)(clickedPoint.X * m_maskImage.Width / windowSizeX);
            int y = (int)(clickedPoint.Y * m_maskImage.Height / windowSizeY);

            if (IsInsideAperture(x, y, 0, 0))
            {
                m_isDragging = true;
                m_isStretching = false;
            }
            else if(IsInsideAperture(x,y,m_mask.Mask.Rows-1,m_mask.Mask.Cols-1))
            {
                m_isDragging = false;
                m_isStretching = true;
            }
        }

        private void MaskImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            m_isDragging = false;
            m_isStretching = false;
        }

        private void MaskImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_isDragging)
            {
                var clickedPoint = e.GetPosition((Image)sender);
                // coordinates are now available in clickedPoint.X and clickedPoint.Y

                double windowSizeX = ((Image)sender).ActualWidth;
                double windowSizeY = ((Image)sender).ActualHeight;

                int x = (int)(clickedPoint.X * m_maskImage.Width / windowSizeX);
                int y = (int)(clickedPoint.Y * m_maskImage.Height / windowSizeY);

                m_mask.Mask.XOffset = x;
                m_mask.Mask.YOffset = y;

                DrawMask();
            }

            if (m_isStretching)
            {
                var clickedPoint = e.GetPosition((Image)sender);
                // coordinates are now available in clickedPoint.X and clickedPoint.Y

                double windowSizeX = ((Image)sender).ActualWidth;
                double windowSizeY = ((Image)sender).ActualHeight;

                int x = (int)(clickedPoint.X * m_maskImage.Width / windowSizeX);
                int y = (int)(clickedPoint.Y * m_maskImage.Height / windowSizeY);

                // calculate new xStep and yStep
                m_mask.Mask.XStep = (double)(x - m_mask.Mask.XOffset) / (double)(m_mask.Mask.Cols - 1);
                m_mask.Mask.YStep = (double)(y - m_mask.Mask.YOffset) / (double)(m_mask.Mask.Rows - 1);

                DrawMask();


            }
        }

        private void MaskImage_MouseLeave(object sender, MouseEventArgs e)
        {
            m_isDragging = false;
            m_isStretching = false;
        }

        public bool IsInsideAperture(int x, int y, int row, int col)
        {
            bool isInside = false;

            // calculate unrotated center of aperature at r,c
            int centerX = (int)(m_mask.Mask.XOffset + col * m_mask.Mask.XStep);
            int centerY = (int)(m_mask.Mask.YOffset + row * m_mask.Mask.YStep);

            // declare variables for rotated center
            int cX;
            int cY;

            // rotate aperature center by m_mask.m_angle around aperture center at r=0, c=0
            RotatePoint(m_mask.Mask.XOffset, m_mask.Mask.YOffset, m_mask.Mask.Angle, centerX, centerY, out cX, out cY);

            // center of aperture at row,col is at cX,cY            
            int xMin = cX - m_mask.Mask.XSize/2;
            int xMax = cX + m_mask.Mask.XSize/2;
            int yMin = cY - m_mask.Mask.YSize/2;
            int yMax = cY + m_mask.Mask.YSize/2;

            if (x > xMin && x < xMax && y > yMin && y < yMax) isInside = true;

            return isInside;
        }





        private void Mask_Save_Click(object sender, EventArgs e)
        {
            MaskContainer mask = new MaskContainer();

            mask.Rows = m_mask.Mask.Rows;
            mask.Cols = m_mask.Mask.Cols;
            mask.XOffset = m_mask.Mask.XOffset;
            mask.YOffset = m_mask.Mask.YOffset;
            mask.XSize = m_mask.Mask.XSize;
            mask.YSize = m_mask.Mask.YSize;
            mask.XStep = m_mask.Mask.XStep;
            mask.YStep = m_mask.Mask.YStep;
            mask.Angle = m_mask.Mask.Angle;
            mask.Shape = m_mask.Mask.Shape;
            mask.Description = m_mask.Mask.Description;
            


            bool available = true;
            int existingMaskID = 0;

            if (m_refImage == null)
            {
                MessageBoxResult result = MessageBox.Show("Mask cannot be saved without a Reference Image.  You must Load a Reference Image.",
                                                               "No Image Loaded", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (mask.Description.Length < 1)
            {
                MessageBoxResult result = MessageBox.Show("Mask cannot be saved without a Name.  Please give the Mask a Name.",
                                                               "No Name given for Mask", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (m_mask.PlateType != null)
                    mask.PlateTypeID = m_mask.PlateType.PlateTypeID;
                else mask.PlateTypeID = 0;

                bool exists = false;
                bool success = wgDB.ReferenceImageExists(m_refImage.ReferenceImageID, ref exists);

                if (!exists)
                {
                    MessageBoxResult result = MessageBox.Show("Image being used is not in Database.  Save the Image to database first.",
                                                               "Reference Image not saved in Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (wgDB.MaskDescriptionAvailable(mask.Description, ref available, ref existingMaskID))
                {
                    if (available)
                    {
                        success = wgDB.InsertMask(ref mask);
                        if (success)
                        {
                            m_mask.Mask.MaskID = mask.MaskID;
                        }
                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show("'" + mask.Description + "' already exists. Do you want to over write it?",
                                                                   "Overwrite Existing Mask", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            mask.MaskID = existingMaskID;
                            success = wgDB.UpdateMask(mask);
                            if (success)
                            {
                                m_mask.Mask.MaskID = mask.MaskID;
                            }

                        }
                    }
                }
            }
            
            
        }



        private void Image_Clear_Click(object sender, EventArgs e)
        {
            //ImageDisplay.ClearImage();
            //m_refImage = null;

            // create default image
            int width = GlobalVars.PixelWidth;
            int height = GlobalVars.PixelHeight;
            
            int numbytes = width * height;
            ushort[] defaultImage = new ushort[numbytes];
            for (int i = 0; i < numbytes; i++) defaultImage[i] = 0;  // black image

            SetImage(defaultImage, width, height);

            SetImageSize(width, height);

            m_mask.Reset((int)m_maskImage.Width, (int)m_maskImage.Height, m_mask.Mask.Rows, m_mask.Mask.Cols);

            DrawMask();

            m_mask.Mask.ReferenceImageID = 0;

        }



        

        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            MaskContainer mc = new MaskContainer();

            mc.MaskID = m_mask.Mask.MaskID;
            mc.Angle = m_mask.Mask.Angle;
            mc.Cols = m_mask.Mask.Cols;
            mc.Description = m_mask.Mask.Description;
            mc.IsDefault = m_mask.Mask.IsDefault;
            mc.PlateTypeID = m_mask.Mask.PlateTypeID;
            mc.ReferenceImageID = m_mask.Mask.ReferenceImageID;
            mc.Rows = m_mask.Mask.Rows;
            mc.Shape = m_mask.Mask.Shape;
            mc.XOffset = m_mask.Mask.XOffset;
            mc.XSize = m_mask.Mask.XSize;
            mc.XStep = m_mask.Mask.XStep;
            mc.YOffset = m_mask.Mask.YOffset;
            mc.YSize = m_mask.Mask.YSize;
            mc.YStep = m_mask.Mask.YStep;

            if(wgDB == null) wgDB = new WaveguideDB();

            bool success = wgDB.UpdateMask(mc);

            if(!success)
            {
                MessageBox.Show(wgDB.GetLastErrorMsg(), "Database Error: Failed to Update Mask", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        private void RangeSlider_TrackFillDragCompleted(object sender, object e)
        {

        }

        private void ShowPixelsCkBx_Checked(object sender, RoutedEventArgs e)
        {
            m_mask.Mask.BuildPixelList(m_xImageSize,m_yImageSize,1,1);

            m_pixelImage.Lock();

            for (int r = 0; r < m_mask.Mask.Rows; r++)
                for (int c = 0; c < m_mask.Mask.Cols; c++ )
                    foreach (int ndx in m_mask.Mask.PixelList[r, c])
                    {
                        int x = ndx % m_xImageSize;
                        int y = ndx / m_xImageSize;
                        m_pixelImage.SetPixel(x, y, Colors.Yellow);
                    }

            m_pixelImage.Unlock();
        }

        private void ShowPixelsCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            m_pixelImage.Clear();
        }

       




        
    } // END class MaskManager







    public class MaskViewModel : INotifyPropertyChanged
    {       
        private PlateTypeContainer _plateType;
  
        private MaskContainer _mask;

        public MaskViewModel()
        {
            _mask = new MaskContainer();

            _mask.MaskID = 0;
            _mask.Rows = 16;
            _mask.Cols = 24;
            _mask.XOffset = 180;
            _mask.YOffset = 30;           
            _mask.XSize = 25;
            _mask.YSize = 25;
            _mask.XStep = 40;
            _mask.YStep = 40;
            _mask.Angle = 0.0;
            _mask.Shape = 0;
            _mask.Description = "";
            _mask.PlateTypeID = 0;
            _mask.ReferenceImageID = 0;
            _mask.IsDefault = false;

            PlateType = null;
            
        }

        public void SetupMaskFromContainer(MaskContainer cont)
        {
            _mask.MaskID = cont.MaskID;
            _mask.Rows = cont.Rows;
            _mask.Cols = cont.Cols;
            _mask.XOffset = cont.XOffset;
            _mask.YOffset = cont.YOffset;
            _mask.XSize = cont.XSize;
            _mask.YSize = cont.YSize;
            _mask.XStep = cont.XStep;
            _mask.YStep = cont.YStep;
            _mask.Angle = cont.Angle;
            _mask.Shape = cont.Shape;
            _mask.Description = cont.Description;
            _mask.PlateTypeID = cont.PlateTypeID;
            _mask.ReferenceImageID = cont.ReferenceImageID;
            _mask.IsDefault = cont.IsDefault;

            WaveguideDB wgDB = new WaveguideDB();
            PlateTypeContainer ptc;
            bool success = wgDB.GetPlateType(Mask.PlateTypeID, out ptc);
            if (success)
            {
                PlateType = ptc;
            }
            else PlateType = null;
        }


        public void Reset(int imageX, int imageY, int rows, int cols)
        {
            int lenX = (int)(imageX * 0.9);
            int stepX = (int)(lenX / (cols - 1));
            int stepY = stepX;
            int lenY = (int)(stepY * (rows - 1));

            _mask.Rows = rows;
            _mask.Cols = cols;
            _mask.XOffset = (int)(imageX * 0.05);
            _mask.YOffset = (imageY - lenY) / 2;
            _mask.XSize = (int)(0.7 * stepX);
            _mask.YSize = _mask.XSize;
            _mask.XStep = stepX;
            _mask.YStep = stepY;
            _mask.Angle = 0;
        }


        public MaskContainer Mask
        {
            get { return _mask; }
            set { _mask = value; if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Mask")); }
        }

        public PlateTypeContainer PlateType
        {
            get { return _plateType; }
            set { _plateType = value; if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("PlateType")); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }





}
