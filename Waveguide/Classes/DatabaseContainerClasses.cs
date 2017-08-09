using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Waveguide
{

    /////////////////////////////////////////////////////////
    // Image Compression Algorithms

    public enum COMPRESSION_ALGORITHM
    {
        NONE,
        GZIP
    };

    public enum SIGNAL_TYPE
    {
        UP,
        DOWN,
        UPDOWN
    };




    public enum PLATE_ID_RESET_BEHAVIOR
    {
        CONSTANT, // Barcode is constant, and is not effected by reset (user provides barcode)
        INCREMENT, // Barcode is incremented upon reset (user provides initial barcode that is incremented)
        CLEAR,  // Barcode is cleared upon reset (user must provide a new barcode after a reset)
        VWORKS  // VWorks Barcode Scan provides barcode
    }


    /////////////////////////////////////////////////////////
    // Color Model
    public class ColorModelContainer : INotifyPropertyChanged
    {
        private int _colorModelID;
        private string _description;
        private bool _isDefault;

        private int _maxPixelValue;  // not in database, but set from an image that is to be displayed
        private int _gradientSize;   // not in database, always 1024 (left as a variable, since may decide to change later)
        private double _gain;           // not in database, set by GUI

        private ObservableCollection<ColorModelControlPointContainer> _controlPts;
        private ObservableCollection<ColorModelStopContainer> _stops;

        public int ColorModelID
        {
            get { return _colorModelID; }
            set { _colorModelID = value; NotifyPropertyChanged("ColorModelID"); }
        }

        public string   Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; NotifyPropertyChanged("IsDefault"); }
        }

        public int      MaxPixelValue  // not in database, but set from an image that is to be displayed
        {
            get { return _maxPixelValue; }
            set { _maxPixelValue = value; NotifyPropertyChanged("MaxPixelValue"); }
        }

        public int      GradientSize   // not in database, always 1024 (left as a variable, since may decide to change later)
        {
            get { return _gradientSize; }
            set { _gradientSize = value; NotifyPropertyChanged("GradientSize"); }
        }

        public double   Gain           // not in database, set by GUI
        {
            get { return _gain; }
            set { _gain = value; NotifyPropertyChanged("Gain"); }
        }

        public ObservableCollection<ColorModelControlPointContainer> ControlPts
        {
            get { return _controlPts; }
            set { _controlPts = value; NotifyPropertyChanged("ControlPts"); }
        }

        public ObservableCollection<ColorModelStopContainer> Stops
        {
            get { return _stops; }
            set { _stops = value; NotifyPropertyChanged("Stops"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    public class ColorModelControlPointContainer : INotifyPropertyChanged
    {
        private int _value;       // percent of maxPixelValue (0-100)
        private int _colorIndex;  // index within the color gradient (0-1023)

        public int Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public int ColorIndex
        {
            get { return _colorIndex; }
            set { _colorIndex = value; NotifyPropertyChanged("ColorIndex"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    public class ColorModelStopContainer : INotifyPropertyChanged
    {
        private int _colorModelStopID;
        private int _colorModelID;
        private int _colorIndex;  // index with the color gradient (0-1023)
        private byte _red;
        private byte _green;
        private byte _blue;

        public int  ColorModelStopID
        {
            get { return _colorModelStopID; }
            set { _colorModelStopID = value; NotifyPropertyChanged("ColorModelStopID"); }
        }

        public int  ColorModelID
        {
            get { return _colorModelID; }
            set { _colorModelID = value; NotifyPropertyChanged("ColorModelID"); }
        }

        public int  ColorIndex
        {
            get { return _colorIndex; }
            set { _colorIndex = value; NotifyPropertyChanged("ColorIndex"); }
        }

        public byte Red
        {
            get { return _red; }
            set { _red = value; NotifyPropertyChanged("Red"); }
        }

        public byte Green
        {
            get { return _green; }
            set { _green = value; NotifyPropertyChanged("Green"); }
        }
        public byte Blue
        {
            get { return _blue; }
            set { _blue = value; NotifyPropertyChanged("Blue"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }



    /////////////////////////////////////////////////////////
    // Analysis
    public class AnalysisContainer : INotifyPropertyChanged
    {
        private int _analysisID;
        private bool _runtimeAnalysis;
        private int _experimentIndicatorID;
        private string _description;
        private DateTime _timeStamp;
        private List<float[,]> _data;

        // constructor
        public AnalysisContainer()
        {
            _data = new List<float[,]>();
        }

        
        public int AnalysisID
        {
            get { return _analysisID; }
            set { _analysisID = value; NotifyPropertyChanged("AnalysisID"); }
        }

        public bool RuntimeAnalysis
        {
            get { return _runtimeAnalysis; }
            set { _runtimeAnalysis = value; NotifyPropertyChanged("RuntimeAnalysis"); }
        }

        public int ExperimentIndicatorID
        {
            get { return _experimentIndicatorID; }
            set { _experimentIndicatorID = value; NotifyPropertyChanged("ExperimentIndicatorID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }


        public List<float[,]> Data
        {
            get { return _data; }
            set { _data = value; NotifyPropertyChanged("Data"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }



    /////////////////////////////////////////////////////////
    // Analysis Frame
    public class AnalysisFrameContainer : INotifyPropertyChanged
    {
        private int _analysisFrameID;
        private int _analysisID;
        private int _rows;
        private int _cols;
        private int _sequenceNumber;
        private string _valueString;  // holds comma-separated list of all analysis values for a single image


        // constructor
        public AnalysisFrameContainer()
        {
        }


        public int AnalysisFrameID
        {
            get { return _analysisFrameID; }
            set { _analysisFrameID = value; NotifyPropertyChanged("AnalysisFrameID"); }
        }

        public int AnalysisID
        {
            get { return _analysisID; }
            set { _analysisID = value; NotifyPropertyChanged("AnalysisID"); }
        }

        public int Rows
        {
            get { return _rows; }
            set { _rows = value; NotifyPropertyChanged("Rows"); }
        }

        public int Cols
        {
            get { return _cols; }
            set { _cols = value; NotifyPropertyChanged("Cols"); }
        }

        public int SequenceNumber
        {
            get { return _sequenceNumber; }
            set { _sequenceNumber = value; NotifyPropertyChanged("SequenceNumber"); }
        }

        public string ValueString
        {
            get { return _valueString; }
            set { _valueString = value; NotifyPropertyChanged("ValueString"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }

    
    /////////////////////////////////////////////////////////
    // Mask
    public class MaskContainer : INotifyPropertyChanged
    {
        private int _maskID;
        private int _rows;
        private int _cols;
        private int _xOffset;
        private int _yOffset;
        private int _xSize;
        private int _ySize;
        private double _xStep;
        private double _yStep;
        private double _angle;
        private int _shape;
        private string _description;
        private int _plateTypeID;
        private int _referenceImageID;
        private bool _isDefault;
        private int[,][] _pixelList;
        private int _numEllipseVertices;
        private char[,] _pixelMask;
        private UInt16[] _pixelMaskImage;

        public MaskContainer()
        {
            _numEllipseVertices = 24;           
        }

        
        public int MaskID
        {
            get { return _maskID; }
            set { _maskID = value; NotifyPropertyChanged("MaskID"); }
        }

        public int Rows
        {
            get { return _rows; }
            set { _rows = value; NotifyPropertyChanged("Rows"); }
        }

        public int Cols
        {
            get { return _cols; }
            set { _cols = value; NotifyPropertyChanged("Cols"); }
        }

        public int XOffset
        {
            get { return _xOffset; }
            set { _xOffset = value; NotifyPropertyChanged("XOffset"); }
        }

        public int YOffset
        {
            get { return _yOffset; }
            set { _yOffset = value; NotifyPropertyChanged("YOffset"); }
        }

        public int XSize
        {
            get { return _xSize; }
            set { _xSize = value; NotifyPropertyChanged("XSize"); }
        }

        public int YSize
        {
            get { return _ySize; }
            set { _ySize = value; NotifyPropertyChanged("YSize"); }
        }

        public double XStep
        {
            get { return _xStep; }
            set { _xStep = value; NotifyPropertyChanged("XStep"); }
        }

        public double YStep
        {
            get { return _yStep; }
            set { _yStep = value; NotifyPropertyChanged("YStep"); }
        }

        public double Angle
        {
            get { return _angle; }
            set { _angle = value; NotifyPropertyChanged("Angle"); }
        }

        public int Shape
        {
            get { return _shape; }
            set { _shape = value; NotifyPropertyChanged("Shape"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public int PlateTypeID
        {
            get { return _plateTypeID; }
            set { _plateTypeID = value; NotifyPropertyChanged("PlateTypeID"); }
        }

        public int ReferenceImageID
        {
            get { return _referenceImageID; }
            set { _referenceImageID = value; NotifyPropertyChanged("ReferenceImageID"); }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; NotifyPropertyChanged("IsDefault"); }
        }

        public int[,][] PixelList
        {
            get { return _pixelList; }
            set { _pixelList = value; NotifyPropertyChanged("PixelList"); }
        }

        public int NumEllipseVertices
        {
            get { return _numEllipseVertices; }
            set { _numEllipseVertices = value; NotifyPropertyChanged("NumEllipseVertices"); }
        }


        public char[,] PixelMask
        {
            get { return _pixelMask; }
            set { _pixelMask = value; NotifyPropertyChanged("PixelMask"); }
        }


        public UInt16[] PixelMaskImage // this is loaded to the GPU in order to do masking
        {
            get { return _pixelMaskImage; }
            set { _pixelMaskImage = value; NotifyPropertyChanged("PixelMaskImage"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }





        public bool IsPixelInsideAperture(int px, int py, int[] xp, int[] yp)
        {
            // px, py are the pixel coordinates of the pixel in question
            // xp, yp are arrays containing the coordinates of the vertices of the aperture

            bool isInside = true;
            int numPoints;

            // calculate vertices of aperture
            switch (this.Shape)
            {
                // rectangle
                case 0:
                default:
                    numPoints = 4;
                    break;
                // ellipse
                case 1:
                    numPoints = _numEllipseVertices;
                    break;
            }

            // algorithm for checking to see if a point is inside a convex polygon (which includes rectangles and a linear piece-wise estimate 
            // for an ellipse)
            // this algorithm is taken from: http://stackoverflow.com/questions/2752725/finding-whether-a-point-lies-inside-a-rectangle-or-not

            int j;
            int last = numPoints - 1;
            for(int i = 0; i<numPoints; i++)
            {  
                j = (i<last) ? i+1 : 0;
                int A = -(yp[j] - yp[i]);
                int B = xp[j] - xp[i];
                int C = -(A * xp[i] + B * yp[i]);
                int D = A * px + B * py + C;
                if(D<0)
                {
                    isInside = false;
                    break;
                }
            }

            return isInside;
        }




        public void CalculateApertureVertices(int row, int col, int imagePixelWidth, int imagePixelHeight, int hBinning, int vBinning,
            out int[] xp, out int[] yp, ref int xmin, ref int ymin, ref int xmax, ref int ymax)
        {
            // xp[], yp[] contain the coordinates of the vertices of the aperture
            // (xmin,ymin) and (xmax,ymax) define a bounding box aroud the aperture, which is convenient for some other functions (like
            // the function for checking whether a pixel is inside or out...it reduces the search space).

            xmin = 1000000; xmax = 0;
            ymin = 1000000; ymax = 0;

            // //////////////////////////////////////////////////////
            // this is done because the mask parameters were set based on a 1x1 (unbinned) reference image
            imagePixelWidth *= hBinning;
            imagePixelHeight *= vBinning;

            // //////////////////////////////////////////////////////
          
            int xc = XOffset + (int)(col * XStep);
            int yc = YOffset + (int)(row * YStep);
            int xcTemp = xc;
            int ycTemp = yc;
            RotatePoint(XOffset, YOffset, Angle, xcTemp, ycTemp, out xc, out yc);

            switch (Shape)
            {
                case  0: // rectangle
                default:

                    xp = new int[4];
                    yp = new int[4];

                    // define the vertices
                    xp[0] = xc - (XSize / 2);
                    xp[1] = xc + (XSize / 2);
                    xp[2] = xp[1];
                    xp[3] = xp[0];

                    yp[0] = yc - (YSize / 2);
                    yp[1] = yp[0];
                    yp[2] = yc + (YSize / 2);
                    yp[3] = yp[2];

                    // limit check and find max/min values
                    for (int i = 0; i < 4; i++)
                    {                        
                        if (xp[i] < 0) xp[i] = 0;
                        if (xp[i] > (imagePixelWidth - 1)) xp[i] = imagePixelWidth - 1;

                        if (yp[i] < 0) yp[i] = 0;
                        if (yp[i] > (imagePixelHeight - 1)) yp[i] = imagePixelHeight - 1;

                        if (xp[i] > xmax) xmax = xp[i];
                        if (xp[i] < xmin) xmin = xp[i];

                        if (yp[i] > ymax) ymax = yp[i];
                        if (yp[i] < ymin) ymin = yp[i];
                    }

                    // scale by binning values
                    for (int i = 0; i < 4; i++)
                    {
                        xp[i] /= hBinning;
                        yp[i] /= vBinning;
                    }
                    xmax /= hBinning; xmin /= hBinning;
                    ymax /= vBinning; ymin /= vBinning;

                    break;
                case 1: // ellipse
                    xp = new int[_numEllipseVertices];
                    yp = new int[_numEllipseVertices];

                    //define the vertices
                    double step = 2 * Math.PI / (_numEllipseVertices);
                    double angle = 0;

                    for (int i = 0; i < _numEllipseVertices; i++)
                    {
                        xp[i] = XOffset + (int)(XSize / 2 * Math.Cos(angle) + col*XStep);
                        yp[i] = YOffset + (int)(YSize / 2 * Math.Sin(angle) + row*YStep);
                        angle += step;
                    }

                    // limit check and find max/min values
                    for (int i = 0; i < _numEllipseVertices; i++)
                    {
                        if (xp[i] < 0) xp[i] = 0;
                        if (xp[i] > (imagePixelWidth - 1)) xp[i] = imagePixelWidth - 1;

                        if (yp[i] < 0) yp[i] = 0;
                        if (yp[i] > (imagePixelHeight - 1)) yp[i] = imagePixelHeight - 1;

                        if (xp[i] > xmax) xmax = xp[i];
                        if (xp[i] < xmin) xmin = xp[i];

                        if (yp[i] > ymax) ymax = yp[i];
                        if (yp[i] < ymin) ymin = yp[i];
                    }

                    // scale by binning values
                    for (int i = 0; i < _numEllipseVertices; i++)
                    {
                        xp[i] /= hBinning;
                        yp[i] /= vBinning;
                    }
                    xmax /= hBinning; xmin /= hBinning;
                    ymax /= vBinning; ymin /= vBinning;


                    break;
            }
            

        }






        public void BuildPixelList(int imagePixelWidth, int imagePixelHeight, int hBinning, int vBinning)
        {
            // This creates a 2D array with an array element for each mask aperture.  The idea
            // is that for each aperture, create an array of the pixels inside that aperture.  This is only done once, when the 
            // AnalysisPipeline is created.  Thus all that is required to find the sum of pixels within an aperture is to go to
            // that aperture's array of pixels and get the value for each one of those pixels, adding to the sum.  
            //
            // int pixelList[mask.rows, mask.cols][numPixels] will contain the pixel list for each aperture.
            //
            // for example, to sum all pixels in aperture [1,1], you would do this:
            //
            //     foreach(int ndx in pixelList[1,1]) sum[1,1] += grayImage[ndx];  NOTE: grayImage is the raw image from the camera and is a 1D array

            // now...preprocess the mask, i.e. create pixelList[mask.rows,mask.cols][numPixels]

            _pixelList = new int[_rows, _cols][];
            List<int> ndxList = new List<int>();

            // aperture vertices
            int[] xp;
            int[] yp;

            // bounding box of an aperture
            int xmin=0, xmax=0, ymin=0, ymax=0; 
            
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    ndxList.Clear();

                    // find aperture vertices and bounding box around aperture (512,512  and 2x2)
                    CalculateApertureVertices(r, c, imagePixelWidth, imagePixelHeight, hBinning, vBinning, 
                        out xp, out yp, ref xmin, ref ymin, ref xmax, ref ymax);

                    for(int y = ymin; y<=ymax; y++)
                        for (int x = xmin; x<=xmax; x++)
                        {
                            if(IsPixelInsideAperture(x, y, xp, yp))
                            {
                                int pixelNdx = y * imagePixelWidth + x;

                                ndxList.Add(pixelNdx);
                            }
                        }

                    _pixelList[r, c] = new int[ndxList.Count];

                    // copy from ndxList to _pixelList
                    for (int i = 0; i < ndxList.Count; i++) _pixelList[r, c][i] = ndxList[i];
                }
        
        }




        public void RotatePoint(int cx, int cy, double angle, int px, int py, out int pxRotated, out int pyRotated)
        {
            // cx,cy is the center of rotation (pixels)
            // angle is the amount to rotate (degs)
            // px,py is the point to rotate
            // pxRotated, pyRotated is the new location of the point after rotation

            double s = Math.Sin(angle * Math.PI / 180);
            double c = Math.Cos(angle * Math.PI / 180);

            // translate center of rotation to origin:
            px -= cx;
            py -= cy;

            // rotate point
            double xnew = px * c - py * s;
            double ynew = px * s + py * c;

            // translate center of rotation back to original location:
            pxRotated = (int)(xnew + cx);
            pyRotated = (int)(ynew + cy);
        }




        public void BuildPixelMaskImage(int imagePixelWidth, int imagePixelHeight)
        {
            // this function should not be called until AFTER BuildPixelList(...) function has been called since it depends on the 
            // PixelList that is created by it.

            // imagePixelWidth, imagePixelHeight - are the size of the image after being scaled by binning

            // this function builds a bit mask with (rows*cols) number of aperatures.  Each aperture is aWidth x aHeight in pixel size.
            // The mask is built for an image of size (width x height) pixels.
            // The mask is set to zero everywhere except inside the aperatures.  
            // The value inside each aperature corresponds to the aperture number.  For example, the aperture at row=0, col=0 contains pixels with a value of 1.  
            // The aperture at row=0, col=1 contains pixels with a value of 2.  This continues for all of the apertures, resulting in something like:
            //
            //  00000000000000000
            //  00111002220033300           This is an example of a 17x9 pixel mask with 6 apertures numbered 1-6
            //  00111002220033300
            //  00111002220033300
            //  00000000000000000 
            //  00444005550066600
            //  00444005550066600
            //  00444005550066600
            //  00000000000000000

            if (PixelList == null)
            {
                // BuildPixelList(...) has yet to be called!!
                // Need to handle this error
                PixelMaskImage = null;
                return;
            }

            PixelMaskImage = new UInt16[imagePixelWidth * imagePixelHeight];
            Array.Clear(PixelMaskImage, 0, PixelMaskImage.Length); // make sure all elements are initialized to zero

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    UInt16 apertureIndex = (UInt16)((r * Cols) + c + 1);  // 1-based index

                    foreach (int ndx in PixelList[r, c]) PixelMaskImage[ndx] = apertureIndex;
                }
        }


        public void GetMaskROI(ref int roiX, ref int roiY, ref int roiW, ref int roiH,
            int imagePixelHeight, int imagePixelWidth, int hBinning, int vBinning, int padding)
        {
            // roiX,roiY,roiW,roiH - this function finds a ROI the just includes all of the mask apertures plus padding pixels
            // imagePixelWidth,imagePixelHeight - the binned size of image, i.e. imagePixelWidth = SensorPixelsX / hBinning
            // hBinning, vBinning - the binning set on camera
            // padding - the desired padding, in pixels, to be added to the size of the roi

            // init roi
            roiX = imagePixelWidth;
            roiY = imagePixelHeight;
            roiW = 0;
            roiH = 0;

            // aperture vertices
            int[] xp;
            int[] yp;

            // bounding box of an aperture
            int xmin = 0, xmax = 0, ymin = 0, ymax = 0;

            // bounding box of mask 
            int mask_xmax = 0;
            int mask_xmin = imagePixelWidth;
            int mask_ymax = 0;
            int mask_ymin = imagePixelHeight;

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    // find aperture vertices and bounding box around aperture (512,512  and 2x2)
                    CalculateApertureVertices(r, c, imagePixelWidth, imagePixelHeight, hBinning, vBinning,
                        out xp, out yp, ref xmin, ref ymin, ref xmax, ref ymax);
            
                    if (xmin < mask_xmin) mask_xmin = xmin;
                    if (xmax > mask_xmax) mask_xmax = xmax;
                    if (ymin < mask_ymin) mask_ymin = ymin;
                    if (ymax > mask_ymax) mask_ymax = ymax;
                }


            roiX = mask_xmin;
            roiY = mask_ymin;
            roiW = mask_xmax -mask_xmin + 1;
            roiH = mask_ymax -mask_ymin + 1;

            // adjust back to full image (since roi's for the camera are always set with no binning)
            roiX *= hBinning;
            roiY *= vBinning;
            roiW *= hBinning;
            roiH *= vBinning;

            // add padding
            roiX -= padding;
            roiY -= padding;
            roiW += (2 * padding);
            roiH += (2 * padding);

            // make sure roiW is an integer multiple of hBinning, and that roiH is an integer multiple of vBinning
            // otherwise the camera will create an exception with setting the ROI
            //VerifyROISize(ref roiX, ref roiY, ref roiW, ref roiH, hBinning, vBinning);

        }




        public void CheckImageLevelsInMask(UInt16[] image, ObservableCollection<Tuple<int,int>> wells, 
            int minPercentOfPixelsAboveLowLimit, UInt16 lowPixelValueThreshold,
            int maxPercentOfPixelsAboveHighLimit, UInt16 highPixelValueThreshold, ref bool tooDim, ref bool tooBright)
        {
            tooDim = false;
            tooBright = false;
            UInt64 numPixelsAboveLowLimit = 0;
            UInt64 numPixelsAboveHighLimit = 0;
            UInt64 totalPixelCount = 0;

            // wells == null, then use all mask apertures (all wells)
            if (wells == null)
            {
                wells = new ObservableCollection<Tuple<int, int>>();

                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                    {
                        Tuple<int,int> ap = Tuple.Create<int, int>(r, c);                        
                        wells.Add(ap);
                    }
            }

            // go through all selected wells
            UInt16 lowestPixelValue = 65535;
            UInt16 highestPixelValue = 0;
            foreach (Tuple<int, int> ap in wells)
            {
                // go through all pixels in well
                foreach (int pixelNdx in PixelList[ap.Item1, ap.Item2])
                {
                    if (image[pixelNdx] < lowestPixelValue) lowestPixelValue = image[pixelNdx];
                    if (image[pixelNdx] > highestPixelValue) highestPixelValue = image[pixelNdx];

                    if (image[pixelNdx] > lowPixelValueThreshold) numPixelsAboveLowLimit++;
                    else if (image[pixelNdx] > highPixelValueThreshold) numPixelsAboveHighLimit++;

                    totalPixelCount++;
                }

            }

            float percentAboveLowLimit = (float)numPixelsAboveLowLimit / (float)totalPixelCount * 100.0f;
            float percentAboveHighLimit = (float)numPixelsAboveHighLimit / (float)totalPixelCount * 100.0f;

            if (percentAboveLowLimit < (float)minPercentOfPixelsAboveLowLimit) tooDim = true;
            if (percentAboveHighLimit > (float)maxPercentOfPixelsAboveHighLimit) tooBright = true;
        }





    }


    /////////////////////////////////////////////////////////
    // Experiment Image
    public class ExperimentImageContainer : INotifyPropertyChanged
    {
        private int _experimentImageID;     
        private DateTime _timeStamp;
        private int _experimentIndicatorID;
        private int _mSecs;     // milliseconds into experiment that image was taken             
        private int _maxPixelValue;  // max value of a pixel in this image; 1023 = 10-bit, 4095 = 12-bit, 16383 = 14-bit, 65535 = 16-bit
        private COMPRESSION_ALGORITHM _compressionAlgorithm; // algorithm used to compress image data, See enum COMPRESSION_ALGORITHM for a list
        private string _filePath; // path and name of file where this image data is stored.

        private ushort[] _imageData; // not saved in database, but stored/retrieved from a file

        public int ExperimentImageID
        {
            get { return _experimentImageID; }
            set { _experimentImageID = value; NotifyPropertyChanged("ExperimentImageID"); }
        }
        
        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }

        public int ExperimentIndicatorID
        {
            get { return _experimentIndicatorID; }
            set { _experimentIndicatorID = value; NotifyPropertyChanged("ExperimentIndicatorID"); }
        }

        public int MSecs
        {
            get { return _mSecs; }
            set { _mSecs = value; NotifyPropertyChanged("MSecs"); }
        }

        public int MaxPixelValue
        {
            get { return _maxPixelValue; }
            set { _maxPixelValue = value; NotifyPropertyChanged("MaxPixelValue"); }
        }

        public COMPRESSION_ALGORITHM CompressionAlgorithm
        {
            get { return _compressionAlgorithm; }
            set { _compressionAlgorithm = value; NotifyPropertyChanged("CompressionAlgorithm"); }
        }

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; NotifyPropertyChanged("FilePath"); }
        }


        public ushort[] ImageData
        {
            get { return _imageData; }
            set { _imageData = value; NotifyPropertyChanged("ImageData"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }

    /////////////////////////////////////////////////////////
    // Reference Image
    public enum REFERENCE_IMAGE_TYPE{
        NONE,
        REF_FLAT_FIELD_FLUORESCENCE,
        REF_FLAT_FIELD_LUMINESCENCE,
        REF_DARK_FLUORESCENCE,
        REF_DARK_LUMINESCENCE
    };


    public class ReferenceImageContainer : INotifyPropertyChanged
    {
        private int _referenceImageID;
        private int _width;
        private int _height;
        private int _depth;
        private ushort[] _imageData;
        private DateTime _timeStamp;
        private int _numBytes;  // bytes of image data
        private int _maxPixelValue;  // max value of a pixel in this image; 1023 = 10-bit, 4095 = 12-bit, 16383 = 14-bit, 65535 = 16-bit
        private COMPRESSION_ALGORITHM _compressionAlgorithm; // algorithm used to compress image data, See enum COMPRESSION_ALGORITHM for a list
        private string _description;
        private REFERENCE_IMAGE_TYPE _type; // (possible values from REFERENCE_IMAGE_TYPE enum)  defines the type of this ref image, for example, a plate type reference image, or flat field reference image
    
        public int ReferenceImageID
        {
            get { return _referenceImageID; }
            set { _referenceImageID = value; NotifyPropertyChanged("ReferenceImageID"); }
        }

        public int Width
        {
            get { return _width; }
            set { _width = value; NotifyPropertyChanged("Width"); }
        }

        public int Height
        {
            get { return _height; }
            set { _height = value; NotifyPropertyChanged("Height"); }
        }

        public int Depth
        {
            get { return _depth; }
            set { _depth = value; NotifyPropertyChanged("Depth"); }
        }

        public ushort[] ImageData
        {
            get { return _imageData; }
            set { _imageData = value; NotifyPropertyChanged("ImageData"); }
        }

        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }

        public int NumBytes
        {
            get { return _numBytes; }
            set { _numBytes = value; NotifyPropertyChanged("NumBytes"); }
        }

        public int MaxPixelValue
        {
            get { return _maxPixelValue; }
            set { _maxPixelValue = value; NotifyPropertyChanged("MaxPixelValue"); }
        }

        public COMPRESSION_ALGORITHM CompressionAlgorithm
        {
            get { return _compressionAlgorithm; }
            set { _compressionAlgorithm = value; NotifyPropertyChanged("CompressionAlgorithm"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public REFERENCE_IMAGE_TYPE Type
        {
            get { return _type; }
            set { _type = value; NotifyPropertyChanged("Type"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Filter
    public class FilterContainer : INotifyPropertyChanged
    {
        // Filter Changer Designation
        //   0 = Excitation Filter Changer
        //   1 = Emission Filter Changer

        private int _filterID;
        private int _FilterChanger;
        private int _positionNumber;
        private string _description;
        private string _manufacturer;
        private string _partNumber;

        public FilterContainer()
        {
            _filterID = 0;
            _FilterChanger = 0;
            _positionNumber = 0;
            _description = "";
            _manufacturer = "";
            _partNumber = "";
        }

        public FilterContainer(int id, int changer, int position, string desc, string manufacturer, string partNumber)
        {
            _filterID = id;
            _FilterChanger = changer;
            _positionNumber = position;
            _description = desc;
            _manufacturer = manufacturer;
            _partNumber = partNumber;
        }

        public int FilterID
        {
            get { return _filterID; }
            set { _filterID = value; NotifyPropertyChanged("FilterID"); }
        }

        public int FilterChanger
        {
            get { return _FilterChanger; }
            set { _FilterChanger = value; NotifyPropertyChanged("FilterChanger"); }
        }

        public int PositionNumber
        {
            get { return _positionNumber; }
            set { _positionNumber = value; NotifyPropertyChanged("PositionNumber"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public string Manufacturer
        {
            get { return _manufacturer; }
            set { _manufacturer = value; NotifyPropertyChanged("Manufacturer"); }
        }

        public string PartNumber
        {
            get { return _partNumber; }
            set { _partNumber = value; NotifyPropertyChanged("PartNumber"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Indicator
    public class IndicatorContainer : INotifyPropertyChanged
    {
        private int _indicatorID;
        private int _methodID;
        private int _excitationFilterPosition;
        private int _emissionsFilterPosition;       
        private string _description;
        private SIGNAL_TYPE _signalType;

        public int IndicatorID
        {
            get { return _indicatorID; }
            set { _indicatorID = value; NotifyPropertyChanged("IndicatorID"); }
        }

        public int MethodID
        {
            get { return _methodID; }
            set { _methodID = value; NotifyPropertyChanged("MethodID"); }
        }

        public int ExcitationFilterPosition
        {
            get { return _excitationFilterPosition; }
            set { _excitationFilterPosition = value; NotifyPropertyChanged("ExcitationFilterPosition"); }
        }

        public int EmissionsFilterPosition
        {
            get { return _emissionsFilterPosition; }
            set { _emissionsFilterPosition = value; NotifyPropertyChanged("EmissionsFilterPosition"); }
        }
         
        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public SIGNAL_TYPE SignalType
        {
            get { return _signalType; }
            set { _signalType = value; NotifyPropertyChanged("SignalType"); }
        }

 
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }



    /////////////////////////////////////////////////////////
    // Method
    public class MethodContainer : INotifyPropertyChanged
    {
        private int _methodID;
        private string _description;
        private string _bravoMethodFile;
        private int _ownerID;
        private int _projectID;
        private bool _isPublic;
        private bool _isAuto;

        public int MethodID
        {
            get { return _methodID; }
            set { _methodID = value; NotifyPropertyChanged("MethodID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public string BravoMethodFile
        {
            get { return _bravoMethodFile; }
            set { _bravoMethodFile = value; NotifyPropertyChanged("BravoMethodFile"); }
        }

        public int OwnerID
        {
            get { return _ownerID; }
            set { _ownerID = value; NotifyPropertyChanged("OwnerID"); }
        }

        public int ProjectID
        {
            get { return _projectID; }
            set { _projectID = value; NotifyPropertyChanged("ProjectID"); }
        }

        public bool IsPublic
        {
            get { return _isPublic; }
            set { _isPublic = value; NotifyPropertyChanged("IsPublic"); }
        }

        public bool IsAuto
        {
            get { return _isAuto; }
            set { _isAuto = value; NotifyPropertyChanged("IsAuto"); }
        }

        public string IsAutoString
        {
            get { if (IsAuto) return "Automated Protocol"; else return "Manual Protocol"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }



    /////////////////////////////////////////////////////////
    // User   
    public class UserContainer : INotifyPropertyChanged
    {
        private int _userid;
        private string _firstname;
        private string _lastname;
        private string _username;
        private string _password;
        private GlobalVars.USER_ROLE_ENUM _role;

        public int UserID 
        { 
            get { return _userid; } 
            set { _userid = value; NotifyPropertyChanged("UserID"); } 
        }

        public string Firstname 
        { 
            get { return _firstname; } 
            set { _firstname = value; NotifyPropertyChanged("Firstname"); } 
        }

        public string Lastname 
        { 
            get { return _lastname; } 
            set { _lastname = value; NotifyPropertyChanged("Lastname"); } 
        }

        public string Username 
        { 
            get { return _username; } 
            set { _username = value; NotifyPropertyChanged("Username"); } 
        }

        public string Password 
        { 
            get { return _password; } 
            set { _password = value; NotifyPropertyChanged("Password"); } 
        }

        public GlobalVars.USER_ROLE_ENUM Role 
        { 
            get { return _role; } 
            set { _role = value; NotifyPropertyChanged("Role"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Project
    public class ProjectContainer : INotifyPropertyChanged
    {
        private int _projectID;
        private string _description;
        private bool _archived;
        private DateTime _timeStamp;

        public int ProjectID
        {
            get { return _projectID; }
            set { _projectID = value; NotifyPropertyChanged("ProjectID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public bool Archived
        {
            get { return _archived; }
            set { _archived = value; NotifyPropertyChanged("Archived"); }
        }

        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // ProjectUser
    public class UserProjectContainer : INotifyPropertyChanged
    {
        private int _userID;
        private int _projectID;

        public int UserID
        {
            get { return _userID; }
            set { _userID = value; NotifyPropertyChanged("UserID"); }
        }

        public int ProjectID
        {
            get { return _projectID; }
            set { _projectID = value; NotifyPropertyChanged("ProjectID"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }

    /////////////////////////////////////////////////////////
    // Plate
    public class PlateContainer : INotifyPropertyChanged, IDataErrorInfo
    {
        private int _plateID;
        private int _projectID;
        private int _ownerID;
        private string _barcode;
        private int _plateTypeID;
        private string _description;
        private bool _isPublic;

        public int PlateID
        {
            get { return _plateID; }
            set { _plateID = value; NotifyPropertyChanged("PlateID"); }
        }

        public int ProjectID
        {
            get { return _projectID; }
            set { _projectID = value; NotifyPropertyChanged("ProjectID"); }
        }

        public int OwnerID
        {
            get { return _ownerID; }
            set { _ownerID = value; NotifyPropertyChanged("OwnerID"); }
        }

        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; NotifyPropertyChanged("Barcode"); }
        }

        public int PlateTypeID
        {
            get { return _plateTypeID; }
            set { _plateTypeID = value; NotifyPropertyChanged("PlateTypeID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public bool IsPublic
        {
            get { return _isPublic; }
            set { _isPublic = value; NotifyPropertyChanged("IsPublic"); }
        }


        // this field is not stored in database
        private bool _barcodeValid;
        public bool BarcodeValid
        {
            get { return _barcodeValid; }
            set { _barcodeValid = value; NotifyPropertyChanged("BarcodeValid"); }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Barcode")
                {
                    if (String.IsNullOrWhiteSpace(Barcode))
                    {
                        BarcodeValid = false;
                        return "Barcode cannot be empty";
                    }
                    //else if (Barcode.Length != 8)
                    //{
                    //    BarcodeValid = false;
                    //    return "Barcode must be exactly 8 characters";
                    //}
                    else BarcodeValid = true;
                }

                return String.Empty;
            }
        }

        // this field is not stored in database
        private PLATE_ID_RESET_BEHAVIOR _plateIDResetBehavior;
        public PLATE_ID_RESET_BEHAVIOR PlateIDResetBehavior
        {
            get { return _plateIDResetBehavior; }
            set { _plateIDResetBehavior = value; NotifyPropertyChanged("PlateIDResetBehavior"); }
        }

        public string Error
        {
            get { return String.Empty; }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Experiment
    public class ExperimentContainer : INotifyPropertyChanged
    {
        private int _experimentID;
        private int _plateID;
        private int _methodID;
        private DateTime _timeStamp;
        private string _description;
        private int _horzBinning;
        private int _vertBinning;
        private int _roi_Origin_X;
        private int _roi_Origin_Y;
        private int _roi_Width;
        private int _roi_Height;

        public int ExperimentID
        {
            get { return _experimentID; }
            set { _experimentID = value; NotifyPropertyChanged("ExperimentID"); }
        }

        public int PlateID
        {
            get { return _plateID; }
            set { _plateID = value; NotifyPropertyChanged("PlateID"); }
        }

        public int MethodID
        {
            get { return _methodID; }
            set { _methodID = value; NotifyPropertyChanged("MethodID"); }
        }

        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public int HorzBinning
        {
            get { return _horzBinning; }
            set { _horzBinning = value; NotifyPropertyChanged("HorzBinning"); }
        }

        public int VertBinning
        {
            get { return _vertBinning; }
            set { _vertBinning = value; NotifyPropertyChanged("VertBinning"); }
        }

        public int ROI_Origin_X
        {
            get { return _roi_Origin_X; }
            set { _roi_Origin_X = value; NotifyPropertyChanged("ROI_Origin_X"); }
        }

        public int ROI_Origin_Y
        {
            get { return _roi_Origin_Y; }
            set { _roi_Origin_Y = value; NotifyPropertyChanged("ROI_Origin_Y"); }
        }
        public int ROI_Width
        {
            get { return _roi_Width; }
            set { _roi_Width = value; NotifyPropertyChanged("ROI_Width"); }
        }

        public int ROI_Height
        {
            get { return _roi_Height; }
            set { _roi_Height = value; NotifyPropertyChanged("ROI_Height"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Experiment Indicator

    public enum FLATFIELD_SELECT
    {
        NONE = 0,
        USE_FLUOR = 1,
        USE_LUMI = 2
    };



    public class ExperimentIndicatorContainer : INotifyPropertyChanged
    {
        private int _experimentIndicatorID;
        private int _experimentID;
        private string _excitationFilterDesc;
        private string _emissionFilterDesc;
        private int _excitationFilterPos;
        private int _emissionFilterPos;
        private int _maskID;
        private int _exposure;
        private int _gain;
        private int _preAmpGain;
        private string _description;
        private SIGNAL_TYPE _signalType;        
        private FLATFIELD_SELECT _flatFieldCorrection;

        // these items not stored in database with ExperimentIndicatorContainer record
        private bool _verified;
        private int _flatFieldRefImageID;
        private int _darkFieldRefImageID;
        private int _cycleTime;

        

        public int ExperimentIndicatorID
        {
            get { return _experimentIndicatorID; }
            set { _experimentIndicatorID = value; NotifyPropertyChanged("ExperimentIndicatorID"); }
        }

        public int ExperimentID
        {
            get { return _experimentID; }
            set { _experimentID = value; NotifyPropertyChanged("ExperimentID"); }
        }

        public string ExcitationFilterDesc
        {
            get { return _excitationFilterDesc; }
            set { _excitationFilterDesc = value; NotifyPropertyChanged("ExcitationFilterDesc"); }
        }

        public string EmissionFilterDesc
        {
            get { return _emissionFilterDesc; }
            set { _emissionFilterDesc = value; NotifyPropertyChanged("EmissionFilterDesc"); }
        }

        public int ExcitationFilterPos
        {
            get { return _excitationFilterPos; }
            set { _excitationFilterPos = value; NotifyPropertyChanged("ExcitationFilterPos"); }
        }

        public int EmissionFilterPos
        {
            get { return _emissionFilterPos; }
            set { _emissionFilterPos = value; NotifyPropertyChanged("EmissionFilterPos"); }
        }

        public int MaskID
        {
            get { return _maskID; }
            set { _maskID = value; NotifyPropertyChanged("MaskID"); }
        }

        public int Exposure
        {
            get { return _exposure; }
            set { _exposure = value; NotifyPropertyChanged("Exposure"); }
        }

        public int Gain
        {
            get { return _gain; }
            set { _gain = value; NotifyPropertyChanged("Gain"); }
        }

        public int PreAmpGain
        {
            get { return _preAmpGain; }
            set { _preAmpGain = value; NotifyPropertyChanged("PreAmpGain"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public SIGNAL_TYPE SignalType
        {
            get { return _signalType; }
            set { _signalType = value; NotifyPropertyChanged("SignalType"); }
        }

        public bool Verified  // this field not saved in database, it is used only during setup for running an experiment
        {
            get { return _verified; }
            set { _verified = value; NotifyPropertyChanged("Verified"); }
        }

        public FLATFIELD_SELECT FlatFieldCorrection
        {
            get { return _flatFieldCorrection; }
            set { _flatFieldCorrection = value; NotifyPropertyChanged("FlatFieldCorrection"); }
        }

        public int FlatFieldRefImageID
        {
            get { return _flatFieldRefImageID; }
            set { _flatFieldRefImageID = value; NotifyPropertyChanged("FlatFieldRefImageID"); }
        }

        public int DarkFieldRefImageID
        {
            get { return _darkFieldRefImageID; }
            set { _darkFieldRefImageID = value; NotifyPropertyChanged("DarkFieldRefImageID"); }
        }

        public int CycleTime
        {
            get { return _cycleTime; }
            set { _cycleTime = value; NotifyPropertyChanged("CycleTime"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // PlateType
    public class PlateTypeContainer : INotifyPropertyChanged
    {
        private int _plateTypeID;
        private string _description;
        private int _rows;
        private int _cols;
        private bool _isDefault;

        public int PlateTypeID
        {
            get { return _plateTypeID; }
            set { _plateTypeID = value; NotifyPropertyChanged("PlateTypeID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public int Rows
        {
            get { return _rows; }
            set { _rows = value; NotifyPropertyChanged("Rows"); }
        }

        public int Cols
        {
            get { return _cols; }
            set { _cols = value; NotifyPropertyChanged("Cols"); }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; NotifyPropertyChanged("IsDefault"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


    /////////////////////////////////////////////////////////
    // Experiment Compound Plate Container
    public class ExperimentCompoundPlateContainer : INotifyPropertyChanged, IDataErrorInfo
    {
        private int _experimentCompoundPlateID;
        private string _description;
        private string _barcode;
        private int _experimentID;

        // not in database
        private PLATE_ID_RESET_BEHAVIOR _plateIDResetBehavior;
        public PLATE_ID_RESET_BEHAVIOR PlateIDResetBehavior
        {
            get { return _plateIDResetBehavior; }
            set { _plateIDResetBehavior = value; NotifyPropertyChanged("PlateIDResetBehavior"); }
        }

        public ExperimentCompoundPlateContainer()
        {
            _plateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CLEAR;
        }


        public int ExperimentCompoundPlateID
        {
            get { return _experimentCompoundPlateID; }
            set { _experimentCompoundPlateID = value; NotifyPropertyChanged("ExperimentCompoundPlateID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; NotifyPropertyChanged("Barcode"); }
        }

        public int ExperimentID
        {
            get { return _experimentID; }
            set { _experimentID = value; NotifyPropertyChanged("ExperimentID"); }
        }


        // this field is not stored in the database
        private bool _barcodeValid;
        public bool BarcodeValid
        {
            get { return _barcodeValid; }
            set { _barcodeValid = value; NotifyPropertyChanged("BarcodeValid"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }


        public string this[string columnName]
        {
            get
            {
                if (columnName == "Barcode")
                {
                    if (String.IsNullOrWhiteSpace(Barcode))
                    {
                        BarcodeValid = false;
                        return "Barcode cannot be empty";
                    }
                    //else if (Barcode.Length != 8)
                    //{
                    //    BarcodeValid = false;
                    //    return "Barcode must be exactly 8 characters";
                    //}
                    else BarcodeValid = true;
                }
                
                return String.Empty;
            }
        }

        public string Error
        {
            get { return String.Empty; }
        }


    }

    /////////////////////////////////////////////////////////
    // Compound Plate Container
    public class CompoundPlateContainer : INotifyPropertyChanged
    {
        private int _compoundPlateID;
        private int _methodID;
        private string _description;
        
        public int CompoundPlateID
        {
            get { return _compoundPlateID; }
            set { _compoundPlateID = value; NotifyPropertyChanged("CompoundPlateID"); }
        }

        public int MethodID
        {
            get { return _methodID; }
            set { _methodID = value; NotifyPropertyChanged("MethodID"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }




    /////////////////////////////////////////////////////////
    // Event Marker Container
    public class EventMarkerContainer : INotifyPropertyChanged
    {
        private int _eventMarkerID;
        private int _experimentID;
        private int _sequenceNumber;
        private string _name;
        private string _description;
        private DateTime _timeStamp;

        public int EventMarkerID
        {
            get { return _eventMarkerID; }
            set { _eventMarkerID = value; NotifyPropertyChanged("EventMarkerID"); }
        }

        public int ExperimentID
        {
            get { return _experimentID; }
            set { _experimentID = value; NotifyPropertyChanged("ExperimentID"); }
        }

        public int SequenceNumber
        {
            get { return _sequenceNumber; }
            set { _sequenceNumber = value; NotifyPropertyChanged("SequenceNumber"); }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }



    public class CameraSettingsContainer : INotifyPropertyChanged
    {
        private int _cameraSettingID;
        private int _vssIndex;
        private int _hssIndex;
        private int _vertClockAmpIndex;
        private bool _useEMAmp;
        private bool _useFrameTransfer;       
        private string _description;
        private bool _isDefault;

        private int _startingExposure;
        private int _exposureLimit;
        private int _highPixelThresholdPercent;
        private int _lowPixelThresholdPercent;
        private int _minPercentPixelsAboveLowThreshold;
        private int _maxPercentPixelsAboveHighThreshold;
        private bool _increasingSignal;
        private int _startingBinning;
        private int _emGainLimit;

        public CameraSettingsContainer(string desc = "")
        {
            StartingExposure = 1;
            ExposureLimit = 1000;
            HighPixelThresholdPercent = 80;
            MinPercentPixelsAboveLowThreshold = 50;
            MaxPercentPixelsAboveHighThreshold = 10;
            LowPixelThresholdPercent = 10;
            EMGainLimit = 300;
            HSSIndex = 0;
            IncreasingSignal = true;
            IsDefault = false;
            StartingBinning = 1;
            UseEMAmp = true;
            UseFrameTransfer = true;
            VertClockAmpIndex = 2;
            VSSIndex = 0;
            Description = desc;
        }

        public int CameraSettingID
        {
            get { return _cameraSettingID; }
            set { _cameraSettingID = value; NotifyPropertyChanged("CameraSettingID"); }
        }

        public int VSSIndex
        {
            get { return _vssIndex; }
            set { _vssIndex = value; NotifyPropertyChanged("VSSIndex"); }
        }

        public int HSSIndex
        {
            get { return _hssIndex; }
            set { _hssIndex = value; NotifyPropertyChanged("HSSIndex"); }
        }

        public int VertClockAmpIndex
        {
            get { return _vertClockAmpIndex; }
            set { _vertClockAmpIndex = value; NotifyPropertyChanged("VertClockAmpIndex"); }
        }

        public bool UseEMAmp
        {
            get { return _useEMAmp; }
            set { _useEMAmp = value; NotifyPropertyChanged("UseEMAmp"); }
        }
        
        public bool UseFrameTransfer
        {
            get { return _useFrameTransfer; }
            set { _useFrameTransfer = value; NotifyPropertyChanged("UseFrameTransfer"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }
        
        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; NotifyPropertyChanged("IsDefault"); }
        }


        public int StartingExposure
        {
            get { return _startingExposure; }
            set { _startingExposure = value; NotifyPropertyChanged("StartingExposure"); }
        }

        public int ExposureLimit
        {
            get { return _exposureLimit; }
            set { _exposureLimit = value; NotifyPropertyChanged("ExposureLimit"); }
        }

        public int HighPixelThresholdPercent
        {
            get { return _highPixelThresholdPercent; }
            set { _highPixelThresholdPercent = value; NotifyPropertyChanged("HighPixelThresholdPercent"); }
        }

        public int LowPixelThresholdPercent
        {
            get { return _lowPixelThresholdPercent; }
            set { _lowPixelThresholdPercent = value; NotifyPropertyChanged("LowPixelThresholdPercent"); }
        }
                         
        public int MinPercentPixelsAboveLowThreshold
        {
            get { return _minPercentPixelsAboveLowThreshold; }
            set { _minPercentPixelsAboveLowThreshold = value; NotifyPropertyChanged("MinPercentPixelsAboveLowThreshold"); }
        }
                 
        public int MaxPercentPixelsAboveHighThreshold
        {
            get { return _maxPercentPixelsAboveHighThreshold; }
            set { _maxPercentPixelsAboveHighThreshold = value; NotifyPropertyChanged("MaxPercentPixelsAboveHighThreshold"); }
        }
                
        public bool IncreasingSignal
        {
            get { return _increasingSignal; }
            set { _increasingSignal = value; NotifyPropertyChanged("IncreasingSignal"); }
        }

        public int StartingBinning
        {
            get { return _startingBinning; }
            set { _startingBinning = value; NotifyPropertyChanged("StartingBinning"); }
        }
        
        public int EMGainLimit
        {
            get { return _emGainLimit; }
            set { _emGainLimit = value; NotifyPropertyChanged("EMGainLimit"); }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


}