using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waveguide;

namespace ImageSaveTool
{

    public class ColorStop
    {
        public ColorStop(int index, WG_Color color)
        {
            m_color = color;
            m_index = index;
        }

        public WG_Color m_color;
        public int m_index;
    }

    public class WG_Color
    {
        public WG_Color(byte red, byte green, byte blue)
        {
            m_red = red;
            m_green = green;
            m_blue = blue;
            m_alpha = 255;
        }

        public byte m_red;
        public byte m_green;
        public byte m_blue;
        public byte m_alpha;
    }

    public class ColorControlPoint : INotifyPropertyChanged
    {
        private int _value;  // this is the percent full scale (0-100)
        private int _colorIndex;  // this is the index into the color gradient (0-1023)
        private int _mapIndex;  // this is the index into the color map (0 - maxPixelValue-1)

        public int m_value  // x axis percent full scale
        {
            get { return _value; }
            set
            {
                _value = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_value"));
            }
        }

        public int m_colorIndex // y axis
        {
            get { return _colorIndex; }
            set
            {
                _colorIndex = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_colorIndex"));
            }
        }

        public int m_mapIndex // x axis - index into the color map
        {
            get { return _mapIndex; }
            set
            {
                _mapIndex = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_mapIndex"));
            }
        }

        public ColorControlPoint(int value, int colorIndex)
        {
            _value = value;
            _colorIndex = colorIndex;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ColorModel
    {

        int MaxPixelValue = 65535;

        public ColorModel(string description, int maxPixelValue = 0, int gradientSize = 1024)
        {
            if (maxPixelValue == 0)
                maxPixelValue = MaxPixelValue;

            m_description = description;

            m_gradientSize = gradientSize;

            m_gain = 1.0;

            m_controlPts = new ObservableCollection<ColorControlPoint>();
            m_stops = new ObservableCollection<ColorStop>();

            m_gradient = new WG_Color[m_gradientSize];

            for (int i = 0; i < m_gradientSize; i++)
            {
                m_gradient[i] = new WG_Color(0, 0, 0);
            }

            SetMaxPixelValue(maxPixelValue);
        }



        public ColorModel(ColorModelContainer cModelCont, int maxPixelValue = 65535, int gradientSize = 1024)
        {
            m_description = cModelCont.Description;
            m_gradientSize = gradientSize;
            m_gain = 1.0;

            m_controlPts = new ObservableCollection<ColorControlPoint>();
            m_stops = new ObservableCollection<ColorStop>();

            m_gradient = new WG_Color[m_gradientSize];

            for (int i = 0; i < m_gradientSize; i++)
            {
                m_gradient[i] = new WG_Color(0, 0, 0);
            }

            foreach (ColorModelControlPointContainer pc in cModelCont.ControlPts)
            {
                m_controlPts.Add(new ColorControlPoint(pc.Value, pc.ColorIndex));
            }

            foreach (ColorModelStopContainer sc in cModelCont.Stops)
            {
                m_stops.Add(new ColorStop(sc.ColorIndex, new WG_Color(sc.Red, sc.Green, sc.Blue)));
            }

            //SetMaxPixelValue(maxPixelValue);
            m_maxPixelValue = maxPixelValue;
            m_colorMap = new WG_Color[m_maxPixelValue + 1];

            for (int i = 0; i < m_maxPixelValue + 1; i++)
            {
                m_colorMap[i] = new WG_Color(0, 0, 0);
            }

            BuildColorGradient();

            BuildColorMap();
        }


        public ColorModel()
        {
            m_description = "Default Color Model";
            m_gradientSize = 1024;
            m_gain = 1.0;

            m_controlPts = new ObservableCollection<ColorControlPoint>();
            m_stops = new ObservableCollection<ColorStop>();

            m_gradient = new WG_Color[m_gradientSize];


            m_controlPts.Add(new ColorControlPoint(0, 0));
            m_controlPts.Add(new ColorControlPoint(MaxPixelValue, m_gradientSize - 1));

            m_stops.Add(new ColorStop(0, new WG_Color(0, 0, 0)));
            m_stops.Add(new ColorStop(MaxPixelValue, new WG_Color(255, 255, 255)));

            m_maxPixelValue = MaxPixelValue;
            m_colorMap = new WG_Color[m_maxPixelValue + 1];

            for (int i = 0; i < m_maxPixelValue + 1; i++)
            {
                m_colorMap[i] = new WG_Color(0, 0, 0);
            }

            //SetMaxPixelValue(maxPixelValue);
            m_maxPixelValue = MaxPixelValue;
            m_colorMap = new WG_Color[m_maxPixelValue + 1];

            for (int i = 0; i < m_gradientSize; i++)
            {
                byte val = (byte)((float)(i * 255) / (float)(m_gradientSize));
                m_gradient[i] = new WG_Color(val, val, val);
            }

            for (int i = 0; i < m_maxPixelValue + 1; i++)
            {
                byte val = (byte)((float)(i * 255) / (float)(m_maxPixelValue));
                m_colorMap[i] = new WG_Color(val, val, val);
            }

        }


        public ObservableCollection<ColorStop> m_stops;
        public ObservableCollection<ColorControlPoint> m_controlPts;  // These control points define how the m_gradient is mapped to the m_colorMap.
        // The control points define a graph with an X range (index) of 0 to maxiumum
        // pixel value, and a Y range of 0 to 1023 (value) which is the fixed range of
        // of all color gradients.

        public string m_description;

        public WG_Color[] m_gradient;  // m_gradientSize element array of colors defined by color stops at 0, 1023, and possibly  
        // intermediate color stops

        public WG_Color[] m_colorMap;  // this is the m_gradient mapped into an array with a range of 0 to m_maxPixelValue.
        // The mapping is defined by the linear interpolations between control points defined in 
        // m_controlPts.  

        public int m_gradientSize;     // number of colors in the gradient, i.e. color stops will be interpolated across 0 to 
        // m_gradientSize-1 elements in the m_gradient array

        public int m_maxPixelValue;    // the maximum value that a pixel can have

        public double m_gain;          // value used to amplify pixel values


        public void SetMaxPixelValue(int maxPixelValue)
        {
            m_maxPixelValue = maxPixelValue;

            m_colorMap = new WG_Color[m_maxPixelValue + 1];

            for (int i = 0; i < m_maxPixelValue + 1; i++)
            {
                m_colorMap[i] = new WG_Color(0, 0, 0);
            }

            //m_controlPts.Add(new ColorControlPoint(0, 0));
            //m_controlPts.Add(new ColorControlPoint(100 / 3, m_gradientSize / 3));
            //m_controlPts.Add(new ColorControlPoint(100 * 2 / 3, m_gradientSize * 2 / 3));
            //m_controlPts.Add(new ColorControlPoint(100, m_gradientSize - 1));

            m_controlPts.Add(new ColorControlPoint(0, 0));
            m_controlPts.Add(new ColorControlPoint(0, 0));
            m_controlPts.Add(new ColorControlPoint(m_maxPixelValue, m_gradientSize - 1));
            m_controlPts.Add(new ColorControlPoint(m_maxPixelValue, m_gradientSize - 1));
        }


        public void InsertColorStop(int index, byte red, byte green, byte blue)
        {
            WG_Color color = new WG_Color(red, green, blue);
            ColorStop cstop = new ColorStop(index, color);

            int position = 0;
            int maxIndex = m_gradientSize - 1;

            if (index == 0)
            {
                position = 0;
            }

            else if (index == maxIndex)
            {
                position = m_stops.Count();
            }

            else
            {
                for (int i = 0; i < m_stops.Count(); i++)
                {
                    if (index > m_stops[i].m_index) position = i + 1;
                    else break;
                }
            }


            m_stops.Insert(position, cstop);

        }



        public void BuildColorMap()
        {
            for (int i = 0; i < m_controlPts.Count(); i++)
            {
                m_controlPts[i].m_mapIndex = (int)(m_maxPixelValue * (double)m_controlPts[i].m_value / 100);
                if (m_controlPts[i].m_mapIndex >= m_colorMap.Length) m_controlPts[i].m_mapIndex = m_colorMap.Length - 1;
            }


            for (int val = 0; val < m_controlPts.Count() - 1; val++)
            {
                int valueRange = m_controlPts[val + 1].m_mapIndex - m_controlPts[val].m_mapIndex;
                int colorIndexRange = m_controlPts[val + 1].m_colorIndex - m_controlPts[val].m_colorIndex;

                for (int i = m_controlPts[val].m_mapIndex; i < m_controlPts[val + 1].m_mapIndex; i++)
                {
                    int valOffset = i - m_controlPts[val].m_mapIndex;

                    int colorIndexOffset = (int)((double)colorIndexRange * (double)valOffset / (double)valueRange);

                    int colorIndex = m_controlPts[val].m_colorIndex + colorIndexOffset;

                    if (colorIndex > m_gradientSize - 1) colorIndex = m_gradientSize - 1;

                    m_colorMap[i].m_red = m_gradient[colorIndex].m_red;
                    m_colorMap[i].m_green = m_gradient[colorIndex].m_green;
                    m_colorMap[i].m_blue = m_gradient[colorIndex].m_blue;
                }
            }

            // the last value in the map has not been set, so just use the same as the next-to-last value in the map
            m_colorMap[m_maxPixelValue].m_red = m_colorMap[m_maxPixelValue - 1].m_red;
            m_colorMap[m_maxPixelValue].m_green = m_colorMap[m_maxPixelValue - 1].m_green;
            m_colorMap[m_maxPixelValue].m_blue = m_colorMap[m_maxPixelValue - 1].m_blue;
            m_colorMap[m_maxPixelValue].m_alpha = m_colorMap[m_maxPixelValue - 1].m_alpha;
        }



        public void BuildColorMapForGPU(out byte[] red, out byte[] green, out byte[] blue, int maxPixelValue)
        {
            red = new byte[maxPixelValue + 1];
            green = new byte[maxPixelValue + 1];
            blue = new byte[maxPixelValue + 1];

            for (int i = 0; i < (maxPixelValue + 1); i++)
            {
                int gradientIndex = (int)(((float)i) / ((float)maxPixelValue) * ((float)(m_gradientSize - 1)));
                red[i] = m_gradient[gradientIndex].m_red;
                green[i] = m_gradient[gradientIndex].m_green;
                blue[i] = m_gradient[gradientIndex].m_blue;
            }
        }



        public void BuildColorGradient()
        {
            for (int ndx = 0; ndx < m_stops.Count() - 1; ndx++)
            {
                double redRange = m_stops[ndx + 1].m_color.m_red - m_stops[ndx].m_color.m_red;
                double greenRange = m_stops[ndx + 1].m_color.m_green - m_stops[ndx].m_color.m_green;
                double blueRange = m_stops[ndx + 1].m_color.m_blue - m_stops[ndx].m_color.m_blue;

                for (int i = m_stops[ndx].m_index; i <= m_stops[ndx + 1].m_index; i++)
                {
                    int a = m_stops[ndx].m_index;
                    int b = m_stops[ndx + 1].m_index;


                    // interpolate
                    double percentValueRange = (double)(i - m_stops[ndx].m_index) / (double)(m_stops[ndx + 1].m_index - m_stops[ndx].m_index);

                    m_gradient[i].m_red = (byte)((percentValueRange * redRange) + m_stops[ndx].m_color.m_red);
                    m_gradient[i].m_green = (byte)((percentValueRange * greenRange) + m_stops[ndx].m_color.m_green);
                    m_gradient[i].m_blue = (byte)((percentValueRange * blueRange) + m_stops[ndx].m_color.m_blue);
                }

            }

            int iiii = 0;
            iiii += 1;

        }


    }

}
