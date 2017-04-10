//using Arction.WPF.LightningChartUltimate;
//using Arction.WPF.LightningChartUltimate.Annotations;
//using Arction.WPF.LightningChartUltimate.Axes;
//using Arction.WPF.LightningChartUltimate.SeriesXY;
//using Arction.WPF.LightningChartUltimate.Views.ViewXY;
using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Views.ViewXY;


using Infragistics.Controls.Editors;
using Infragistics.Windows.DataPresenter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for ChartArray.xaml
    /// </summary>
   

    public partial class ChartArray : UserControl
    {

        struct ButtonTag
        {
            public string type;
            public int position;
        };

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

        enum VISIBLE_SIGNAL
        {
            RAW,
            STATIC_RATIO,
            CONTROL_SUBTRACTION,
            DYNAMIC_RATIO
        };

        public ColorModel m_colorModel;
        WriteableBitmap m_colorMapBitmap;

        public ViewModel_ChartArray VM;

        RangeClass m_range;  // limits for color model slider

        Double XLen = 100; // window width for each chart

        private LightningChartUltimate[] m_charts;
        private LightningChartUltimate m_aggregateChart;

        private VISIBLE_SIGNAL m_visibleSignal;

        private Band[,] m_band;
        private bool[,] m_chartSelected;

        bool[] m_allChartsInRowSelected;
        bool[] m_allChartsInColumnSelected;
        bool m_allChartsSelected;

        // Data for each chart is put into either a SampleDataSeries or a PointLineSeries (these are LightningChart provided structures).
        // Since there is a chart for each aperature in a mask, there is an array of these data series with each element of
        // the array being either a SampleDataSeries or a PointLineSeries.  Since there may be multiple experiment indicators in 
        // an experiment, there may be multiple series for these charts, and since the data is sent to the chart with a given
        // ExperimentIndicatorID, a Dictionary is used to organize of these data.
        //
        // For the following Dictionaries, the key (int) is the ExperimentIndicatorID and the 
        // value is a 2D array of points (either SampleDataSeries or PointLineSeries), one for
        // each aperture in the mask.  So, for example, the points used for the raw data for the
        // A1 aperture for ExperimentIndicatorID = 99 would be retrieved using:
        //      1) first get the raw data array for this indicator: 
        //          SampleDataSeries[,] raw_data_array = m_ChartArray_Raw_Dictionary[99];
        //      2) next get a specific series from the array (in this case, [0,0] since we're after A1): 
        //          SampleDataSeries rawA1_data = raw_data_array[0,0];
        private Dictionary<int, SampleDataSeries[,]> m_ChartArray_Raw_Dictionary;
        private Dictionary<int, SampleDataSeries[,]> m_ChartArray_StaticRatio_Dictionary;
        private Dictionary<int, SampleDataSeries[,]> m_ChartArray_ControlSubtraction_Dictionary;
        private Dictionary<int, SampleDataSeries[,]> m_ChartArray_DynamicRatio_Dictionary;

        private Dictionary<int, PointLineSeries[,]> m_Aggregate_Raw_Dictionary;
        private Dictionary<int, PointLineSeries[,]> m_Aggregate_StaticRatio_Dictionary;
        private Dictionary<int, PointLineSeries[,]> m_Aggregate_ControlSubtraction_Dictionary;
        private Dictionary<int, PointLineSeries[,]> m_Aggregate_DynamicRatio_Dictionary;

  
        // Dictionary to store range of each data series, used to adjust the range of the charts
        struct DataRange { public double xMin; public double xMax; public double yMin; public double yMax; };
        private DataRange m_Raw_Range;
        private DataRange m_StaticRatio_Range;
        private DataRange m_ControlSubtraction_Range;
        private DataRange m_DynamicRatio_Range;

        private List<Button> m_columnButton;
        private List<Button> m_rowButton;

                
        private Dictionary<int, ExperimentIndicatorContainer> m_indicatorDictionary;
        private Dictionary<int, bool> m_indicatorVisibleDictionary;
        private Dictionary<int, Color> m_indicatorColor;

        private Int32 m_iChartCount;
        private Int32 m_iTraceCountPerChart;
        public int m_numPoints;

        public int m_rows;
        public int m_cols;

        public int m_hBinning;
        public int m_vBinning;

        int m_mouseDownRow, m_mouseUpRow;
        int m_mouseDownCol, m_mouseUpCol;
        Point m_mouseDownPoint, m_mouseUpPoint;
        Point m_dragDown, m_drag;
        double m_colWidth, m_rowHeight;

        System.Windows.Media.Color m_buttonColorSelected;
        System.Windows.Media.Color m_buttonColorNotSelected;

        WaveguideDB wgDB;

        Imager m_imager;
        MaskContainer m_mask;

        public ChartArray()
        {
            m_mouseDownRow = -1;
            m_mouseDownCol = -1; 

            VM = new ViewModel_ChartArray();

            m_indicatorDictionary = new Dictionary<int, ExperimentIndicatorContainer>();
            m_indicatorVisibleDictionary = new Dictionary<int, bool>();
            m_indicatorColor = new Dictionary<int, Color>();

            m_charts = null;
            m_band = null;
            m_iChartCount = 0;
            m_iTraceCountPerChart = 0;
            m_numPoints = 0;

            m_hBinning = 1;
            m_vBinning = 1;

            InitializeComponent();

            this.DataContext = VM;

            m_visibleSignal = VISIBLE_SIGNAL.RAW;
            RawRadioButton.IsChecked = true;

            m_buttonColorNotSelected = Colors.LightGray;
            m_buttonColorSelected = Colors.Red;

            m_rowButton = new List<Button>();
            m_columnButton = new List<Button>();

            m_range = new RangeClass();
            RangeSlider.DataContext = m_range;
            m_range.RangeMin = 0;
            m_range.RangeMax = 100;

            wgDB = new WaveguideDB();


            bool success = wgDB.GetDefaultColorModel(out m_colorModel, GlobalVars.MaxPixelValue, 1024);

            if(!success || m_colorModel == null)   
            {
                // setup default color model
                m_colorModel = new ColorModel("Default");
                m_colorModel.InsertColorStop(0, 0, 0, 0);
                m_colorModel.InsertColorStop(1023, 255, 255, 255);                     
            }


            m_colorModel.SetMaxPixelValue(GlobalVars.MaxPixelValue);

            m_colorModel.m_controlPts.Clear();
            m_colorModel.m_controlPts.Add(new ColorControlPoint(0, 0));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(0, 0));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(m_colorModel.m_maxPixelValue, m_colorModel.m_gradientSize - 1));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(m_colorModel.m_maxPixelValue, m_colorModel.m_gradientSize - 1));

            m_colorModel.m_controlPts[1].m_value = 0;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = 100;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorGradient();
            m_colorModel.BuildColorMap();

            DrawColorMap();

            ChartArrayGrid.SizeChanged += ChartArrayGrid_SizeChanged;

            InitAggregateChart();
            DrawGridLines();

            VM.TemperatureTarget = GlobalVars.CameraTargetTemperature;
            VM.CycleTime = GlobalVars.CameraDefaultCycleTime;
            VM.TemperatureReady = false;

        }


        void ChartArrayGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGridLines();
        }

      
        public void SetStatus(ViewModel_ChartArray.RUN_STATUS status)
        {
            VM.Status = status;
        }


        public void Reset()
        {
            VM.Reset();
            ClearPlotData();
        }


        public TaskScheduler GetTaskScheduler()
        {
            return TaskScheduler.FromCurrentSynchronizationContext();
        }


        public void DrawGridLines()
        {           

            VM.GridLines.Clear(); 

            double width = VM.GridLines.Width;
            double height = VM.GridLines.Height;

            double xStep = width / m_cols;
            double yStep = height / m_rows;

            double x = xStep;
            double y = yStep;
            for (int c = 1; c < m_cols; c++ )
            {
                VM.GridLines.DrawLine((int)(c * xStep), 0, (int)(c * xStep), (int)height, Colors.LightGray);
            }

            int offset = 0;
            for (int r = 1; r < m_rows; r++)
            {
                VM.GridLines.DrawLine(0, (int)(r * yStep + offset), (int)width, (int)(r * yStep + offset), Colors.LightGray);
            }         
        }


        public void Configure(Imager imager, MaskContainer mask)
        {
            m_imager = imager;
            m_mask = mask;
        }


        


        public void BuildChartArray(int rows, int cols, 
            ObservableCollection<ExperimentIndicatorContainer> indicatorList, 
            ObservableCollection<ExperimentCompoundPlateContainer> compoundPlateList)
        {
            DisposeCharts();

            VM.IndicatorList = indicatorList;
            VM.CompoundPlateList = compoundPlateList;
            
            m_indicatorDictionary = new Dictionary<int,ExperimentIndicatorContainer>();

            m_indicatorVisibleDictionary = new Dictionary<int, bool>();

            m_indicatorColor = new Dictionary<int, Color>();

            m_rows = rows;
            m_cols = cols;            
            
            int i = 0;
            foreach(ExperimentIndicatorContainer indicator in indicatorList)
            {            
                m_indicatorDictionary.Add(indicator.ExperimentIndicatorID, indicator);
                m_indicatorVisibleDictionary.Add(indicator.ExperimentIndicatorID, true);
                m_indicatorColor.Add(indicator.ExperimentIndicatorID, GlobalVars.DefaultTraceColorList[i]);
                i++;
            }

            // creat the range of each series
            m_Raw_Range = new DataRange();
            m_StaticRatio_Range = new DataRange();
            m_ControlSubtraction_Range = new DataRange();
            m_DynamicRatio_Range = new DataRange();
            // initialize the ranges  
            double min = 99999999999;
            double max = -999999999;
            m_Raw_Range.xMin = min; m_Raw_Range.xMax = max;
            m_Raw_Range.yMin = min; m_Raw_Range.yMax = max;

            m_StaticRatio_Range.xMin = min; m_StaticRatio_Range.xMax = max;
            m_StaticRatio_Range.yMin = min; m_StaticRatio_Range.yMax = max;

            m_ControlSubtraction_Range.xMin = min; m_ControlSubtraction_Range.xMax = max;
            m_ControlSubtraction_Range.yMin = min; m_ControlSubtraction_Range.yMax = max;

            m_DynamicRatio_Range.xMin = min; m_DynamicRatio_Range.xMax = max;
            m_DynamicRatio_Range.yMin = min; m_DynamicRatio_Range.yMax = max;
                        

            // create the dictionaries that hold the data of each series
            m_ChartArray_Raw_Dictionary = new Dictionary<int, SampleDataSeries[,]>();
            m_ChartArray_StaticRatio_Dictionary = new Dictionary<int, SampleDataSeries[,]>();
            m_ChartArray_ControlSubtraction_Dictionary = new Dictionary<int, SampleDataSeries[,]>();
            m_ChartArray_DynamicRatio_Dictionary = new Dictionary<int, SampleDataSeries[,]>();

            m_Aggregate_Raw_Dictionary = new Dictionary<int, PointLineSeries[,]>();
            m_Aggregate_StaticRatio_Dictionary = new Dictionary<int, PointLineSeries[,]>();
            m_Aggregate_ControlSubtraction_Dictionary = new Dictionary<int, PointLineSeries[,]>();
            m_Aggregate_DynamicRatio_Dictionary = new Dictionary<int, PointLineSeries[,]>();


            // create the data series arrays for each indicator
            foreach (ExperimentIndicatorContainer indicator in indicatorList)
            {
                SampleDataSeries[,] chartArrayRaw = new SampleDataSeries[m_rows, m_cols];
                SampleDataSeries[,] chartArrayStaticRatio = new SampleDataSeries[m_rows, m_cols];
                SampleDataSeries[,] chartArrayControlSubtraction = new SampleDataSeries[m_rows, m_cols];
                SampleDataSeries[,] chartArrayDynamicRatio = new SampleDataSeries[m_rows, m_cols];

                PointLineSeries[,] aggregateRaw = new PointLineSeries[m_rows, m_cols];
                PointLineSeries[,] aggregateStaticRatio = new PointLineSeries[m_rows, m_cols];
                PointLineSeries[,] aggregateControlSubtraction = new PointLineSeries[m_rows, m_cols];
                PointLineSeries[,] aggregateDynamicRatio = new PointLineSeries[m_rows, m_cols];

                m_ChartArray_Raw_Dictionary.Add(indicator.ExperimentIndicatorID,chartArrayRaw);
                m_ChartArray_StaticRatio_Dictionary.Add(indicator.ExperimentIndicatorID,chartArrayStaticRatio);
                m_ChartArray_ControlSubtraction_Dictionary.Add(indicator.ExperimentIndicatorID,chartArrayControlSubtraction);
                m_ChartArray_DynamicRatio_Dictionary.Add(indicator.ExperimentIndicatorID,chartArrayDynamicRatio);
                
                m_Aggregate_Raw_Dictionary.Add(indicator.ExperimentIndicatorID,aggregateRaw);
                m_Aggregate_StaticRatio_Dictionary.Add(indicator.ExperimentIndicatorID,aggregateStaticRatio); 
                m_Aggregate_ControlSubtraction_Dictionary.Add(indicator.ExperimentIndicatorID,aggregateControlSubtraction);
                m_Aggregate_DynamicRatio_Dictionary.Add(indicator.ExperimentIndicatorID, aggregateDynamicRatio);

                for (int r = 0; r < m_rows; r++)
                    for (int c = 0; c < m_cols; c++)
                    {
                        chartArrayRaw[r, c] = new SampleDataSeries();
                        chartArrayStaticRatio[r, c] = new SampleDataSeries();
                        chartArrayControlSubtraction[r, c] = new SampleDataSeries();
                        chartArrayDynamicRatio[r, c] = new SampleDataSeries();

                        aggregateRaw[r, c] = new PointLineSeries();
                        aggregateStaticRatio[r, c] = new PointLineSeries();
                        aggregateControlSubtraction[r, c] = new PointLineSeries();
                        aggregateDynamicRatio[r, c] = new PointLineSeries();
                    }
            }

          
            m_band = new Band[rows, cols];
            m_chartSelected = new bool[rows, cols];

            InitAggregateChart();
            CreateCharts(cols, rows);
            ArrangeCharts();

            m_allChartsInColumnSelected = new bool[m_cols];
            m_allChartsInRowSelected = new bool[m_rows];
            SetUpChartArrayButtons();

            DrawColorMap();

            DrawGridLines();

            SetAnalysisVisibility();
        }


        public void BuildDisplayGrid()
        {
            m_hBinning = m_imager.m_camera.m_acqParams.HBin;
            m_vBinning = m_imager.m_camera.m_acqParams.VBin;
            VM.HorzBinning = m_hBinning;
            VM.VertBinning = m_vBinning;


            ImageGrid.Children.Clear();
            ImageGrid.RowDefinitions.Clear();
            ImageGrid.ColumnDefinitions.Clear();

            RowDefinition row0 = new RowDefinition();
            row0.Height = new GridLength(30, GridUnitType.Pixel);

            RowDefinition row1= new RowDefinition();
            row1.Height = new GridLength(1, GridUnitType.Star);

            ImageGrid.RowDefinitions.Add(row0);
            ImageGrid.RowDefinitions.Add(row1);

            SolidColorBrush brush = new SolidColorBrush(Colors.LightGray);

            m_imager.ResetImagingDictionary();

            int i = 0;
            foreach(KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {               
                ExperimentIndicatorContainer indicator = entry.Value;
                int expIndicatorID = entry.Key;
                ColumnDefinition colDef = new ColumnDefinition();
                colDef.Width = new GridLength(1, GridUnitType.Star);
                
                ImageGrid.ColumnDefinitions.Add(colDef);

                StackPanel stack = new StackPanel();
                CheckBox chkBox = new CheckBox();
                chkBox.Width = Double.NaN; // sets the width to "Auto"
                chkBox.Content = indicator.Description;
                chkBox.FontSize = 16;
                chkBox.FontWeight = FontWeights.Bold;
                chkBox.Tag = expIndicatorID;
                chkBox.IsChecked = true;
                chkBox.Checked += Indicator_ChkBox_Checked;
                chkBox.Unchecked += Indicator_ChkBox_Checked;                

                XamColorPicker colorPicker = new XamColorPicker();
                colorPicker.DerivedPalettesCount = 10;
                colorPicker.SelectedColor = m_indicatorColor[expIndicatorID];
                colorPicker.Width = 50;
                colorPicker.Height = 10;
                colorPicker.Tag = expIndicatorID;
                colorPicker.Margin = new Thickness(10, 0, 0, 0);
                colorPicker.ShowAdvancedEditorButton = true;
                colorPicker.SelectedColorChanged += colorPicker_SelectedColorChanged;

                ColorPalette MyColorPalette = new ColorPalette();
                MyColorPalette.Colors.Add(Colors.Red);
                MyColorPalette.Colors.Add(Colors.Orange);
                MyColorPalette.Colors.Add(Colors.Yellow);
                MyColorPalette.Colors.Add(Colors.Green);
                MyColorPalette.Colors.Add(Colors.Blue);
                MyColorPalette.Colors.Add(Colors.Purple);
                MyColorPalette.Colors.Add(Colors.Magenta);
                MyColorPalette.Colors.Add(Colors.White);
                MyColorPalette.Colors.Add(Colors.LightBlue);
                MyColorPalette.Colors.Add(Colors.HotPink);
                colorPicker.ColorPalettes.Clear();
                colorPicker.ColorPalettes.Add(MyColorPalette);                

                stack.Orientation = Orientation.Horizontal;
                stack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                stack.Children.Add(chkBox);
                stack.Children.Add(colorPicker);
                Grid.SetColumn(stack, i);
                Grid.SetRow(stack, 0);
                ImageGrid.Children.Add(stack);


                Image image = new Image();
                image.Margin = new Thickness(2);
                Grid.SetColumn(image,i);
                Grid.SetRow(image,1);
                ImageGrid.Children.Add(image);

                ImagingParamsStruct ips = new ImagingParamsStruct();
                ips.cycleTime = 1000; // TODO:  this needs to be set somewhere else
                ips.emissionFilterPos = (byte)indicator.EmissionFilterPos;
                ips.excitationFilterPos = (byte)indicator.ExcitationFilterPos;
                ips.experimentIndicatorID = indicator.ExperimentIndicatorID;
                ips.exposure = indicator.Exposure;
                ips.flatfieldType = indicator.FlatFieldCorrection;
                ips.gain = indicator.Gain;
                ips.indicatorName = indicator.Description; 
                ips.histBitmap = null;  // no histogram shown 
                ips.ImageControl = image;
                
                ips.d3dImage = null; // set after adding to m_imagingDictionary                                
                ips.pSurface = IntPtr.Zero;

                m_imager.m_ImagingDictionary.Add(indicator.ExperimentIndicatorID,ips);

                // this call sets d3dImage and pSurface
                m_imager.ConfigImageD3DSurface(indicator.ExperimentIndicatorID,
                                                m_imager.m_camera.m_acqParams.BinnedFullImageWidth,
                                                m_imager.m_camera.m_acqParams.BinnedFullImageHeight,false);

                i++;
            }

       
        }




        void colorPicker_SelectedColorChanged(object sender, SelectedColorChangedEventArgs e)
        {
            XamColorPicker colorPicker = (XamColorPicker)sender;

            Color color = (Color)colorPicker.SelectedColor;

            int expIndicatorID = (int)colorPicker.Tag;

            m_indicatorColor[expIndicatorID] = color;

            SampleDataSeries[,] caRaw = m_ChartArray_Raw_Dictionary[expIndicatorID];
            SampleDataSeries[,] caStaticRatio = m_ChartArray_StaticRatio_Dictionary[expIndicatorID];
            SampleDataSeries[,] caControlSubtraction = m_ChartArray_ControlSubtraction_Dictionary[expIndicatorID];
            SampleDataSeries[,] caDynamicRatio = m_ChartArray_DynamicRatio_Dictionary[expIndicatorID];

            PointLineSeries[,] aggRaw = m_Aggregate_Raw_Dictionary[expIndicatorID];
            PointLineSeries[,] aggStaticRatio = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
            PointLineSeries[,] aggControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
            PointLineSeries[,] aggDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];


            m_aggregateChart.BeginUpdate();

            for(int c = 0; c<m_cols; c++)
            {
                m_charts[c].BeginUpdate();
                for(int r = 0; r<m_rows; r++)
                {
                    caRaw[r,c].LineStyle.Color = color;
                    caStaticRatio[r,c].LineStyle.Color = color;
                    caControlSubtraction[r,c].LineStyle.Color = color;
                    caDynamicRatio[r, c].LineStyle.Color = color;

                    aggRaw[r, c].LineStyle.Color = color;
                    aggStaticRatio[r, c].LineStyle.Color = color;
                    aggControlSubtraction[r, c].LineStyle.Color = color;
                    aggDynamicRatio[r, c].LineStyle.Color = color;
                }
                m_charts[c].EndUpdate();
            }

            m_aggregateChart.EndUpdate();
        }




        void Indicator_ColorRect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle)) return;

            Rectangle rect = (Rectangle)sender;

            Color currentColor = ((SolidColorBrush)rect.Fill).Color;

            ColorPicker colorPicker = new ColorPicker();

            colorPicker.ColorPickerControl.SelectedColor = currentColor;

            colorPicker.ShowDialog();

            if(colorPicker.m_colorSelected)
            {
                Color color = colorPicker.m_color;
                rect.Fill = new SolidColorBrush(color);
            }
        }

        void Indicator_ChkBox_Checked(object sender, RoutedEventArgs e)
        {
            if(sender.GetType() != typeof(CheckBox)) return;

            CheckBox chkBox = (CheckBox)sender;

            bool isChecked = (bool)chkBox.IsChecked;

            int expIndicatorID = (int)chkBox.Tag;

            m_indicatorVisibleDictionary[expIndicatorID] = isChecked;

            SetTraceVisibility(expIndicatorID, isChecked);
        }


        public Dictionary<int,ImagingParamsStruct> GetImageDisplayDictionary()
        {
            return m_imager.m_ImagingDictionary;
        }

        public Dictionary<int,ExperimentIndicatorContainer> GetExperimentIndicatorDictionary()
        {
            return m_indicatorDictionary;
        }



        public void CleanUp()
        {
            DisposeCharts();
            GC.SuppressFinalize(this);
        }


        private void DisposeCharts()
        {
            if (m_charts != null)
            {
                gridChart.Children.Clear();

                int iCount = m_charts.Length;
                for (int i = 0; i < iCount; i++)
                {
                    if (m_charts[i] != null)
                    {
                        m_charts[i].Dispose();
                        m_charts[i] = null;
                    }
                }
            }

            if(m_band != null)
            {
                int rows = m_band.GetLength(0);
                int cols = m_band.GetLength(1);
                for(int r = 0; r<rows;r++)
                    for(int c = 0; c<cols; c++)
                    {
                        if(m_band[r,c] != null)
                        {
                            m_band[r, c].Dispose();
                            m_band[r, c] = null;
                        }
                    }
            }



            foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {
                ExperimentIndicatorContainer indicator = entry.Value;
                int expIndicatorID = entry.Key;

                SampleDataSeries[,] chartArrayRaw = m_ChartArray_Raw_Dictionary[expIndicatorID];
                SampleDataSeries[,] chartArrayStaticRatio = m_ChartArray_StaticRatio_Dictionary[expIndicatorID];
                SampleDataSeries[,] chartArrayControlSubtraction = m_ChartArray_ControlSubtraction_Dictionary[expIndicatorID];
                SampleDataSeries[,] chartArrayDynamicRatio = m_ChartArray_DynamicRatio_Dictionary[expIndicatorID];

                PointLineSeries[,] aggregateRaw = m_Aggregate_Raw_Dictionary[expIndicatorID];
                PointLineSeries[,] aggregateStaticRatio = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
                PointLineSeries[,] aggregateControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
                PointLineSeries[,] aggregateDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];                

                for (int r = 0; r < m_rows; r++)
                    for (int c = 0; c < m_cols; c++)
                    {
                        if (chartArrayRaw[r, c]!=null) chartArrayRaw[r, c].Clear();
                        if (chartArrayStaticRatio[r, c] != null) chartArrayStaticRatio[r, c].Clear();
                        if (chartArrayControlSubtraction[r, c] != null) chartArrayControlSubtraction[r, c].Clear();
                        if (chartArrayDynamicRatio[r, c] != null) chartArrayDynamicRatio[r, c].Clear();

                        if (aggregateRaw[r, c] != null) aggregateRaw[r, c].Clear();
                        if (aggregateStaticRatio[r, c] != null) aggregateStaticRatio[r, c].Clear();
                        if (aggregateControlSubtraction[r, c] != null) aggregateControlSubtraction[r, c].Clear();
                        if (aggregateDynamicRatio[r, c] != null) aggregateDynamicRatio[r, c].Clear();
                    }
            }

        }




        private void SetUpChartArrayButtons()
        {
            // clear any previously set up buttons from grids
            ColumnButtonGrid.Children.Clear();
            ColumnButtonGrid.ColumnDefinitions.Clear();
            RowButtonGrid.Children.Clear();
            RowButtonGrid.RowDefinitions.Clear();
            m_rowButton.Clear();
            m_columnButton.Clear();

            SolidColorBrush brush = new SolidColorBrush(m_buttonColorNotSelected);

            for (int i = 0; i < m_cols; i++)
            {
                ColumnDefinition colDef = new ColumnDefinition();
                ColumnButtonGrid.ColumnDefinitions.Add(colDef);
                Button button = new Button();
                ButtonTag tag = new ButtonTag();
                tag.type = "C";
                tag.position = i;
                button.Tag = tag;
                button.Content = (i + 1).ToString();
                button.Click += button_Click;
                ColumnButtonGrid.Children.Add(button);
                Grid.SetColumn(button, i);
                Grid.SetRow(button, 0);
                m_columnButton.Add(button);
                button.Background = brush;
            }

            for (int i = 0; i < m_rows; i++)
            {
                RowDefinition rowDef = new RowDefinition();
                RowButtonGrid.RowDefinitions.Add(rowDef);
                Button button = new Button();
                ButtonTag tag = new ButtonTag();
                tag.type = "R";
                tag.position = i;
                button.Tag = tag;
                int unicode;
                char character;
                string text;
                if (i < 26)
                {
                    unicode = 65 + i;
                    character = (char)unicode;
                    text = character.ToString();
                }
                else
                {
                    unicode = 39 + i;
                    character = (char)unicode;
                    text = "A" + character.ToString();
                }

                button.Content = text;
                button.Click += button_Click;

                RowButtonGrid.Children.Add(button);
                Grid.SetColumn(button, 0);
                Grid.SetRow(button, i);
                m_rowButton.Add(button);
                button.Background = brush;
            }
        }



        bool IsRowSeleted(int row)
        {
            bool result = true;

            for (int c = 0; c < m_cols; c++ )
            {
                if(!m_chartSelected[row,c])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }


        bool IsColumnSeleted(int col)
        {
            bool result = true;

            for (int r = 0; r < m_rows; r++)
            {
                if (!m_chartSelected[r, col])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }


        bool AreAllSelected()
        {
            bool result = true;

            for (int r = 0; r < m_rows; r++ )
            {
                if(!IsRowSeleted(r))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }


        void SetButtonStates()
        {
            SolidColorBrush brushSelected = new SolidColorBrush(m_buttonColorSelected);
            SolidColorBrush brushNotSelected = new SolidColorBrush(m_buttonColorNotSelected);
            bool all = true;
            for (int r = 0; r < m_rows; r++)
            {
                m_allChartsInRowSelected[r] = IsRowSeleted(r);
                if (!m_allChartsInRowSelected[r])
                {
                    all = false;
                    m_rowButton.ElementAt(r).Background = brushNotSelected;
                }
                else m_rowButton.ElementAt(r).Background = brushSelected;
            }

            for (int c = 0; c < m_cols; c++)
            {
                m_allChartsInColumnSelected[c] = IsColumnSeleted(c);
                if (!m_allChartsInColumnSelected[c])
                {
                    all = false;
                    m_columnButton.ElementAt(c).Background = brushNotSelected;
                }
                else m_columnButton.ElementAt(c).Background = brushSelected;
            }

            m_allChartsSelected = all;

            if (m_allChartsSelected)
                SelectAllPB.Background = brushSelected;
            else
                SelectAllPB.Background = brushNotSelected;
        }



        private void SelectAllPB_Click(object sender, RoutedEventArgs e)
        {
            bool state;

            m_aggregateChart.BeginUpdate();

            if (AreAllSelected()) state = false; else state = true;

            foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {
                int expIndicatorID = entry.Key;

                if (m_indicatorVisibleDictionary[expIndicatorID])
                {
                    PointLineSeries[,] rawAgg = m_Aggregate_Raw_Dictionary[expIndicatorID];
                    PointLineSeries[,] staticRatioAgg = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
                    PointLineSeries[,] controlSubAgg = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
                    PointLineSeries[,] dynRatioAgg = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];

                    for (int c = 0; c < m_cols; c++)
                    {
                        m_charts[c].BeginUpdate();

                        for (int r = 0; r < m_rows; r++)
                        {
                            m_chartSelected[r, c] = state;

                            m_band[r, c].Visible = state;

                            switch (m_visibleSignal)
                            {
                                case VISIBLE_SIGNAL.RAW:
                                    rawAgg[r, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.STATIC_RATIO:
                                    staticRatioAgg[r, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                                    controlSubAgg[r, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                                    dynRatioAgg[r, c].Visible = state;
                                    break;
                            }
                        }

                        m_charts[c].EndUpdate();
                    }
                }
            }

            m_aggregateChart.EndUpdate();

            SetButtonStates();
        }



        // this function gets called whenever a row or column button is clicked
        void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            ButtonTag tag = (ButtonTag)(((Button)sender).Tag);
            bool state;


            foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {
                int expIndicatorID = entry.Key;

                if (m_indicatorVisibleDictionary[expIndicatorID])
                {
                    PointLineSeries[,] rawAgg = m_Aggregate_Raw_Dictionary[expIndicatorID];
                    PointLineSeries[,] staticRatioAgg = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
                    PointLineSeries[,] controlSubAgg = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
                    PointLineSeries[,] dynRatioAgg = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];

                    if (tag.type == "C") // it's a column button
                    {
                        int columnNumber = tag.position;
                        
                        m_aggregateChart.BeginUpdate();

                        m_charts[columnNumber].BeginUpdate();

                        if (m_allChartsInColumnSelected[columnNumber]) state = false; else state = true;

                        for (int r = 0; r < m_rows; r++)
                        {
                            m_chartSelected[r, columnNumber] = state;

                            m_band[r, columnNumber].Visible = state;

                            switch (m_visibleSignal)
                            {
                                case VISIBLE_SIGNAL.RAW:
                                    rawAgg[r, columnNumber].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.STATIC_RATIO:
                                    staticRatioAgg[r, columnNumber].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                                    controlSubAgg[r, columnNumber].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                                    dynRatioAgg[r, columnNumber].Visible = state;
                                    break;
                            }
                        }
                        m_charts[columnNumber].EndUpdate();

                        m_aggregateChart.EndUpdate();
                    }

                    if (tag.type == "R") // it's a row button
                    {
                        int rowNumber = tag.position;

                        m_aggregateChart.BeginUpdate();

                        if (m_allChartsInRowSelected[rowNumber]) state = false; else state = true;

                        for (int c = 0; c < m_cols; c++)
                        {
                            m_chartSelected[rowNumber, c] = state;

                            m_charts[c].BeginUpdate();

                            m_band[rowNumber, c].Visible = m_chartSelected[rowNumber, c];

                            switch (m_visibleSignal)
                            {
                                case VISIBLE_SIGNAL.RAW:
                                    rawAgg[rowNumber, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.STATIC_RATIO:
                                    staticRatioAgg[rowNumber, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                                    controlSubAgg[rowNumber, c].Visible = state;
                                    break;
                                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                                    dynRatioAgg[rowNumber, c].Visible = state;
                                    break;
                            }

                            m_charts[c].EndUpdate();
                        }

                        m_aggregateChart.EndUpdate();

                    }

                }
            }

            SetButtonStates();

        }






        private void InitAggregateChart()
        {
            AggregateGrid.Children.Clear();

            if(m_aggregateChart != null)
            {
                m_aggregateChart.Dispose();
                m_aggregateChart = null;
            }


            m_aggregateChart = new LightningChartUltimate("David Weaver/Developer1 - Renewed subscription/LightningChartUltimate/KRLD2KS3KTX5YQ42P6V8Q42JKU2B355YTMEU");
          
            m_aggregateChart.BeginUpdate();

            m_aggregateChart.Tag = "Aggregate Chart";

            AxisX xAxis = m_aggregateChart.ViewXY.XAxes[0];
            xAxis.ValueType = AxisValueType.Number;

            xAxis.SetRange(0,1);
            xAxis.Title.Visible = false;
            xAxis.ScrollMode = XAxisScrollMode.None;
            xAxis.ScrollingGap = 0;
            xAxis.Visible = true;

            m_aggregateChart.ViewXY.Margins = new Thickness(50, 10, 10, 50);

            m_aggregateChart.ViewXY.GraphBackground.Color = Colors.DimGray;
            m_aggregateChart.ViewXY.GraphBackground.GradientColor = Colors.Black;
            m_aggregateChart.ViewXY.GraphBackground.GradientDirection = 270;
            m_aggregateChart.ViewXY.GraphBackground.GradientFill = GradientFill.Linear;

            m_aggregateChart.ViewXY.LegendBox.Visible = false;


            // CHANGE - comment out line below
            //m_aggregateChart.Title.Font = new WPFFont(System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular);
            m_aggregateChart.Title.Text = "";
            
            m_aggregateChart.ViewXY.ZoomPanOptions.LeftMouseButtonAction = MouseButtonAction.None;
            m_aggregateChart.ViewXY.ZoomPanOptions.RightMouseButtonAction = MouseButtonAction.None;
            
            m_aggregateChart.ChartName = "Aggregate Chart";

           
                // define a row
            AxisY axisY = m_aggregateChart.ViewXY.YAxes[0];
            axisY.MajorGrid.Visible = true;
            axisY.Visible = true;
            axisY.SetRange(0,1);
            axisY.LabelsVisible = true;
            axisY.Title.Visible = false;
            axisY.MouseInteraction = false;
            axisY.MouseScaling = false;
            axisY.MouseScrolling = false;
            axisY.AllowAutoYFit = true;
      

            int colorIndex = 0;

            foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {
                ExperimentIndicatorContainer indicator = entry.Value;
                int expIndicatorID = entry.Key;

                PointLineSeries[,] aggRaw = m_Aggregate_Raw_Dictionary[expIndicatorID];
                PointLineSeries[,] aggStaticRatio = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
                PointLineSeries[,] aggControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
                PointLineSeries[,] aggDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];
                                

                for (int r = 0; r < m_rows; r++)
                    for (int c = 0; c < m_cols; c++)
                    {
                        // create the Raw sample series as a point series for the Aggregate Chart
                        PointLineSeries pls = new PointLineSeries(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0], axisY);
                        pls.PointsVisible = false;
                        pls.Visible = false;
                        pls.LineStyle.Color = m_indicatorColor[expIndicatorID];
                        pls.LineStyle.AntiAliasing = LineAntialias.None;
                        aggRaw[r, c] = pls;
                        m_aggregateChart.ViewXY.PointLineSeries.Add(pls);

                        pls = new PointLineSeries(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0], axisY);
                        pls.PointsVisible = false;
                        pls.Visible = false;
                        pls.LineStyle.Color = m_indicatorColor[expIndicatorID];
                        pls.LineStyle.AntiAliasing = LineAntialias.None;
                        aggStaticRatio[r, c] = pls;
                        m_aggregateChart.ViewXY.PointLineSeries.Add(pls);

                        pls = new PointLineSeries(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0], axisY);
                        pls.PointsVisible = false;
                        pls.Visible = false;
                        pls.LineStyle.Color = m_indicatorColor[expIndicatorID];
                        pls.LineStyle.AntiAliasing = LineAntialias.None;
                        aggControlSubtraction[r, c] = pls;
                        m_aggregateChart.ViewXY.PointLineSeries.Add(pls);

                        pls = new PointLineSeries(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0], axisY);
                        pls.PointsVisible = false;
                        pls.Visible = false;
                        pls.LineStyle.Color = m_indicatorColor[expIndicatorID];
                        pls.LineStyle.AntiAliasing = LineAntialias.None;
                        aggDynamicRatio[r, c] = pls;
                        m_aggregateChart.ViewXY.PointLineSeries.Add(pls);
                    }

                colorIndex++;
            }


        

             m_aggregateChart.EndUpdate();

             
             AggregateGrid.Children.Add(m_aggregateChart);
        }





        private void CreateCharts(int chartCount, int traceCountPerChart)
        {
            m_charts = new LightningChartUltimate[chartCount];

            


                for (int iChart = 0; iChart < chartCount; iChart++)
                {
                    // define a column
                    RenderingSettings renderSettings = new RenderingSettings();
                    renderSettings.AntiAliasLevel = 1;

                    // CHANGE - comment out line below
                    //renderSettings.EffectCaching = true; 

                    //m_charts[iChart] = new LightningChartUltimate(LicenseKeys.LicenseKeyStrings.LightningChartUltimate, renderSettings);

                    m_charts[iChart] = new LightningChartUltimate("David Weaver/Developer1 - Renewed subscription/LightningChartUltimate/KRLD2KS3KTX5YQ42P6V8Q42JKU2B355YTMEU", renderSettings);

                    //m_charts[iChart] = new LightningChartUltimate("David Weaver/LicensePack1/LightningChartUltimate/F3SCJUDJ3K2AYU42BMWMP9Q2KT279YSXMN3V", renderSettings);


                    m_charts[iChart].BeginUpdate();

                    m_charts[iChart].Tag = iChart;

                    m_charts[iChart].MouseLeftButtonDown += ChartArray_MouseLeftButtonDown;
                    m_charts[iChart].MouseLeftButtonUp += ChartArray_MouseLeftButtonUp;

                    m_charts[iChart].VerticalAlignment = VerticalAlignment.Top;
                    m_charts[iChart].HorizontalAlignment = HorizontalAlignment.Left;

                    m_charts[iChart].ViewXY.LegendBox.Visible = false;

                    m_charts[iChart].ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
                    m_charts[iChart].ViewXY.XAxes[0].ScrollingGap = 0;
                    m_charts[iChart].ViewXY.XAxes[0].SteppingInterval = XLen / 3.0;
                    m_charts[iChart].ViewXY.XAxes[0].MajorGrid.Visible = false;
                    m_charts[iChart].ViewXY.XAxes[0].SetRange(0,1);
                    m_charts[iChart].ViewXY.XAxes[0].LabelsVisible = false;
                    m_charts[iChart].ViewXY.XAxes[0].Title.Visible = false;
                    m_charts[iChart].ViewXY.XAxes[0].Visible = false;
                    m_charts[iChart].ViewXY.XAxes[0].MouseInteraction = false;
                    m_charts[iChart].ViewXY.XAxes[0].MouseScaling = false;
                    m_charts[iChart].ViewXY.XAxes[0].MouseScrolling = false;


                    m_charts[iChart].ViewXY.Margins = new Thickness(0, 0, 0, 0);

                    // CHANGE = comment out line below
                    //m_charts[iChart].Title.Font = new WPFFont(System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular);
                    m_charts[iChart].Title.Text = "";
                    m_charts[iChart].ChartBackground.Color = Colors.Black;
                    m_charts[iChart].ChartBackground.GradientFill = GradientFill.Solid;
                    m_charts[iChart].ViewXY.GraphBackground.Color = Color.FromArgb(255, 40, 40, 40);
                    m_charts[iChart].ViewXY.GraphBackground.GradientColor = Colors.Black;
                    m_charts[iChart].ViewXY.GraphBackground.GradientFill = GradientFill.Solid;
                    m_charts[iChart].ViewXY.GraphBorderColor = Colors.Black;

                    m_charts[iChart].ViewXY.ZoomPanOptions.LeftMouseButtonAction = MouseButtonAction.None;
                    m_charts[iChart].ViewXY.ZoomPanOptions.RightMouseButtonAction = MouseButtonAction.None;
                    m_charts[iChart].ViewXY.DropOldSeriesData = true;

                    m_charts[iChart].ViewXY.AxisLayout.YAxesLayout = YAxesLayout.Stacked;
                    m_charts[iChart].ViewXY.YAxes.Clear();

                    // CHANGE - change StackYAxesGap to SegmentsGap
                    m_charts[iChart].ViewXY.AxisLayout.SegmentsGap = 1;

                    // CHANGE - AutoShrinkYAxesGap to 
                    m_charts[iChart].ViewXY.AxisLayout.AutoShrinkSegmentsGap = true;

                    m_charts[iChart].ViewXY.AxisLayout.AutoAdjustMargins = false;
                    m_charts[iChart].ViewXY.Margins = new Thickness(0, 0, 0, 0);

                    m_charts[iChart].ChartName = m_charts[iChart].Title.Text;

                    for (int iCh = 0; iCh < traceCountPerChart; iCh++)
                    {
                        // define a row
                        AxisY axisY = new AxisY(m_charts[iChart].ViewXY, false);
                        axisY.MajorGrid.Visible = false;
                        axisY.Visible = false;
                        axisY.SetRange(0, 1);
                        axisY.LabelsVisible = false;
                        axisY.Title.Visible = false;
                        axisY.MouseInteraction = false;
                        axisY.MouseScaling = false;
                        axisY.MouseScrolling = false;
                        axisY.AllowAutoYFit = true;

                        m_charts[iChart].ViewXY.YAxes.Add(axisY);

                        foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
                        {
                            ExperimentIndicatorContainer indicator = entry.Value;
                            int expIndicatorID = entry.Key;

                            SampleDataSeries[,] chartArrayRaw = m_ChartArray_Raw_Dictionary[expIndicatorID];
                            SampleDataSeries[,] chartArrayStaticRatio = m_ChartArray_StaticRatio_Dictionary[expIndicatorID];
                            SampleDataSeries[,] chartArrayControlSubtraction = m_ChartArray_ControlSubtraction_Dictionary[expIndicatorID];
                            SampleDataSeries[,] chartArrayDynamicRatio = m_ChartArray_DynamicRatio_Dictionary[expIndicatorID];


                            SampleDataSeries sds = new SampleDataSeries(m_charts[iChart].ViewXY, m_charts[iChart].ViewXY.XAxes[0], axisY);
                            sds.FirstSampleTimeStamp = 0;
                            sds.SamplingFrequency = 1;
                            sds.SampleFormat = SampleFormat.SingleFloat;
                            sds.Title.Text = indicator.Description;
                            sds.Title.Visible = false;
                            sds.LineStyle.Width = 1;
                            sds.ScrollModePointsKeepLevel = 10;
                            //sds.ScrollingStabilizing = true;                   
                            sds.LineStyle.Color = m_indicatorColor[expIndicatorID];
                            sds.LineStyle.AntiAliasing = LineAntialias.None;
                            sds.MouseInteraction = false;
                            sds.Visible = true;
                            chartArrayRaw[iCh, iChart] = sds; //m_ChartArray_Raw_Dictionary[expIndicatorID][iCh, iChart] = sds;
                            m_charts[iChart].ViewXY.SampleDataSeries.Add(sds);

                            sds = new SampleDataSeries(m_charts[iChart].ViewXY, m_charts[iChart].ViewXY.XAxes[0], axisY);
                            sds.FirstSampleTimeStamp = 0;
                            sds.SamplingFrequency = 1;
                            sds.SampleFormat = SampleFormat.SingleFloat;
                            sds.Title.Text = indicator.Description;
                            sds.Title.Visible = false;
                            sds.LineStyle.Width = 1;
                            sds.ScrollModePointsKeepLevel = 10;
                            //sds.ScrollingStabilizing = true;                   
                            sds.LineStyle.Color = m_indicatorColor[expIndicatorID];
                            sds.LineStyle.AntiAliasing = LineAntialias.None;
                            sds.MouseInteraction = false;
                            sds.Visible = true;
                            chartArrayStaticRatio[iCh, iChart] = sds;
                            m_charts[iChart].ViewXY.SampleDataSeries.Add(sds);

                            sds = new SampleDataSeries(m_charts[iChart].ViewXY, m_charts[iChart].ViewXY.XAxes[0], axisY);
                            sds.FirstSampleTimeStamp = 0;
                            sds.SamplingFrequency = 1;
                            sds.SampleFormat = SampleFormat.SingleFloat;
                            sds.Title.Text = indicator.Description;
                            sds.Title.Visible = false;
                            sds.LineStyle.Width = 1;
                            sds.ScrollModePointsKeepLevel = 10;
                            //sds.ScrollingStabilizing = true;                   
                            sds.LineStyle.Color = m_indicatorColor[expIndicatorID];
                            sds.LineStyle.AntiAliasing = LineAntialias.None;
                            sds.MouseInteraction = false;
                            sds.Visible = true;
                            chartArrayControlSubtraction[iCh, iChart] = sds;
                            m_charts[iChart].ViewXY.SampleDataSeries.Add(sds);

                            sds = new SampleDataSeries(m_charts[iChart].ViewXY, m_charts[iChart].ViewXY.XAxes[0], axisY);
                            sds.FirstSampleTimeStamp = 0;
                            sds.SamplingFrequency = 1;
                            sds.SampleFormat = SampleFormat.SingleFloat;
                            sds.Title.Text = indicator.Description;
                            sds.Title.Visible = false;
                            sds.LineStyle.Width = 1;
                            sds.ScrollModePointsKeepLevel = 10;
                            //sds.ScrollingStabilizing = true;                   
                            sds.LineStyle.Color = m_indicatorColor[expIndicatorID];
                            sds.LineStyle.AntiAliasing = LineAntialias.None;
                            sds.MouseInteraction = false;
                            sds.Visible = true;
                            chartArrayDynamicRatio[iCh, iChart] = sds;
                            m_charts[iChart].ViewXY.SampleDataSeries.Add(sds);
                        } // END foreach indicator


                        // define Band (used for highlighting selected charts)                    
                        m_band[iCh, iChart] = new Band(m_charts[iChart].ViewXY, m_charts[iChart].ViewXY.XAxes[0], axisY);
                        m_band[iCh, iChart].Behind = true;
                        m_band[iCh, iChart].Fill.Color = Colors.Gray;
                        m_band[iCh, iChart].Binding = AxisBinding.YAxis;
                        m_band[iCh, iChart].ValueBegin = 0;
                        m_band[iCh, iChart].ValueEnd = 1;
                        m_band[iCh, iChart].Visible = false;
                        m_band[iCh, iChart].MouseInteraction = false;
                        m_chartSelected[iCh, iChart] = false;
                        m_charts[iChart].ViewXY.Bands.Add(m_band[iCh, iChart]);

                    }


                    m_charts[iChart].EndUpdate();

                    gridChart.Children.Add(m_charts[iChart]);
                }

            
        

            m_iChartCount = m_cols;
            m_iTraceCountPerChart = m_rows;
    
        }



        public void SetTraceVisibility(int indicatorID, bool indicatorIsVisible)
        {
            m_indicatorVisibleDictionary[indicatorID] = indicatorIsVisible;

            SampleDataSeries[,] chartArraySeries;
            PointLineSeries[,] aggregateSeries;

            switch (m_visibleSignal)
            {
                case VISIBLE_SIGNAL.RAW:
                    chartArraySeries = m_ChartArray_Raw_Dictionary[indicatorID];
                    aggregateSeries = m_Aggregate_Raw_Dictionary[indicatorID];
                    break;
                case VISIBLE_SIGNAL.STATIC_RATIO:
                    chartArraySeries = m_ChartArray_StaticRatio_Dictionary[indicatorID];
                    aggregateSeries = m_Aggregate_StaticRatio_Dictionary[indicatorID];
                    break;
                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                    chartArraySeries = m_ChartArray_ControlSubtraction_Dictionary[indicatorID];
                    aggregateSeries = m_Aggregate_ControlSubtraction_Dictionary[indicatorID];
                    break;
                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                    chartArraySeries = m_ChartArray_DynamicRatio_Dictionary[indicatorID];
                    aggregateSeries = m_Aggregate_DynamicRatio_Dictionary[indicatorID];
                    break;
                default:
                    chartArraySeries = null;
                    aggregateSeries = null;
                    break;
            }

            if (chartArraySeries != null && aggregateSeries != null)
            {
                m_aggregateChart.BeginUpdate();
                for (int c = 0; c < m_cols; c++)
                {
                    m_charts[c].BeginUpdate();
                    for (int r = 0; r < m_rows; r++)
                    {
                        chartArraySeries[r, c].Visible = indicatorIsVisible;
                        aggregateSeries[r, c].Visible = m_chartSelected[r, c] && indicatorIsVisible;
                    }
                    m_charts[c].EndUpdate();
                }
                m_aggregateChart.EndUpdate();
            }
        }


        public void SetAnalysisVisibility()
        {
            bool turnOnRaw = false;
            bool turnOnStaticRatio = false;
            bool turnOnControlSubtraction = false;
            bool turnOnDynamicRatio = false;

            switch (m_visibleSignal)
            {
                case VISIBLE_SIGNAL.RAW:
                    turnOnRaw = true;
                    break;
                case VISIBLE_SIGNAL.STATIC_RATIO:
                    turnOnStaticRatio = true;
                    break;
                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                    turnOnControlSubtraction = true;
                    break;
                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                    turnOnDynamicRatio = true;
                    break;
                default:                    
                    break;
            }

            m_aggregateChart.BeginUpdate();

            for(int c = 0; c<m_cols; c++)
            {
                m_charts[c].BeginUpdate();

                for(int r = 0; r<m_rows; r++)
                {
                    foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
                    {
                        int expIndicatorID = entry.Key;

                        m_ChartArray_Raw_Dictionary[expIndicatorID][r,c].Visible = 
                            turnOnRaw && m_indicatorVisibleDictionary[expIndicatorID];
                        m_Aggregate_Raw_Dictionary[expIndicatorID][r, c].Visible = 
                            turnOnRaw && m_indicatorVisibleDictionary[expIndicatorID] && m_chartSelected[r, c];

                        m_ChartArray_StaticRatio_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnStaticRatio && m_indicatorVisibleDictionary[expIndicatorID];
                        m_Aggregate_StaticRatio_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnStaticRatio && m_indicatorVisibleDictionary[expIndicatorID] && m_chartSelected[r, c];

                        m_ChartArray_ControlSubtraction_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnControlSubtraction && m_indicatorVisibleDictionary[expIndicatorID];
                        m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnControlSubtraction && m_indicatorVisibleDictionary[expIndicatorID] && m_chartSelected[r, c];

                        m_ChartArray_DynamicRatio_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnDynamicRatio && m_indicatorVisibleDictionary[expIndicatorID];
                        m_Aggregate_DynamicRatio_Dictionary[expIndicatorID][r, c].Visible =
                            turnOnDynamicRatio && m_indicatorVisibleDictionary[expIndicatorID] && m_chartSelected[r, c];
                    }

                }

                m_charts[c].EndUpdate();
            }

            m_aggregateChart.EndUpdate();

            SetRangeForSignalType(m_visibleSignal);                
        }




        public void AddEventMarker(int sequenceNumber, string text)
        {
            m_aggregateChart.BeginUpdate();

            LineSeriesCursor cursor = new LineSeriesCursor(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0]);
            cursor.ValueAtXAxis = (double)sequenceNumber;
            cursor.SnapToPoints = true;
            cursor.Style = CursorStyle.VerticalNoTracking;
            cursor.MouseInteraction = false;
            m_aggregateChart.ViewXY.LineSeriesCursors.Add(cursor);


            //Arrow from location to target
            AnnotationXY anno = new AnnotationXY(m_aggregateChart.ViewXY, m_aggregateChart.ViewXY.XAxes[0], m_aggregateChart.ViewXY.YAxes[0]);
            anno.Style = AnnotationStyle.Arrow;
            anno.LocationCoordinateSystem = CoordinateSystem.AxisValues;
            anno.Text = text;
            anno.TargetAxisValues.X = (double)sequenceNumber;
            anno.TargetAxisValues.Y = m_aggregateChart.ViewXY.YAxes[0].Maximum;
            anno.LocationAxisValues.X = (double)sequenceNumber;
            double yRange = (m_aggregateChart.ViewXY.YAxes[0].Maximum - m_aggregateChart.ViewXY.YAxes[0].Minimum);
            double yLoc = yRange * 0.9 + m_aggregateChart.ViewXY.YAxes[0].Minimum;
            anno.LocationAxisValues.Y = yLoc;
            anno.TextStyle.Color = Colors.White;
            anno.Visible = true;
            m_aggregateChart.ViewXY.Annotations.Add(anno);

            m_aggregateChart.EndUpdate();
        }

        public void ShowAnnotations(bool show)
        {
            foreach(AnnotationXY anno in m_aggregateChart.ViewXY.Annotations)
            {
                anno.Visible = show;
            }
        }



        void ChartArray_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(sender.GetType() == typeof(LightningChartUltimate))
            {
                if (m_mouseDownRow != -1 && m_mouseDownCol != -1) // make sure we have a captured mouse down first
                {
                    LightningChartUltimate chart = (LightningChartUltimate)sender;

                    m_mouseUpCol = (int)chart.Tag;

                    m_mouseUpPoint = e.GetPosition(chart);

                    m_mouseUpRow = GetClickedRow(chart, (int)m_mouseUpPoint.Y);

                    if (m_mouseUpRow != -1)
                    {
                        // set the Band(s) for the selected charts
                        int rowStart, rowStop;
                        int colStart, colStop;

                        if (m_mouseUpCol < m_mouseDownCol)
                        {
                            colStart = m_mouseUpCol; colStop = m_mouseDownCol;
                        }
                        else
                        {
                            colStart = m_mouseDownCol; colStop = m_mouseUpCol;
                        }

                        if (m_mouseUpRow < m_mouseDownRow)
                        {
                            rowStart = m_mouseUpRow; rowStop = m_mouseDownRow;
                        }
                        else
                        {
                            rowStart = m_mouseDownRow; rowStop = m_mouseUpRow;
                        }


                        m_aggregateChart.BeginUpdate();

                        bool alreadySet = false;

                        foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
                        {
                            ExperimentIndicatorContainer indicator = entry.Value;
                            int expIndicatorID = entry.Key;

                            PointLineSeries[,] aggRaw = m_Aggregate_Raw_Dictionary[expIndicatorID];
                            PointLineSeries[,] aggStaticRatio = m_Aggregate_StaticRatio_Dictionary[expIndicatorID];
                            PointLineSeries[,] aggControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[expIndicatorID];
                            PointLineSeries[,] aggDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[expIndicatorID];

                            if (m_indicatorVisibleDictionary[expIndicatorID])  // is the indicator visible?
                            {
                                for (int c = colStart; c <= colStop; c++)
                                {
                                    m_charts[c].BeginUpdate();
                                    for (int r = rowStart; r <= rowStop; r++)
                                    {
                                        if (!alreadySet)
                                        {
                                            m_chartSelected[r, c] = !m_chartSelected[r, c];

                                            m_band[r, c].Visible = m_chartSelected[r, c];                                            
                                        }

                                        switch (m_visibleSignal)
                                        {
                                            case VISIBLE_SIGNAL.RAW:                                                
                                                    aggRaw[r,c].Visible = m_chartSelected[r, c];                                               
                                                break;
                                            case VISIBLE_SIGNAL.STATIC_RATIO:                                                
                                                    aggStaticRatio[r,c].Visible = m_chartSelected[r, c];                                                
                                                break;
                                            case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                                                    aggControlSubtraction[r,c].Visible = m_chartSelected[r, c];
                                                break;
                                            case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                                                    aggDynamicRatio[r,c].Visible = m_chartSelected[r, c];
                                                break;
                                        }
                                    }
                                    m_charts[c].EndUpdate();
                                }

                                alreadySet = true;
                            }
                        }

                        m_aggregateChart.EndUpdate();

                    }

                    // reset the mouse down locations
                    m_mouseDownRow = -1;
                    m_mouseDownCol = -1;

                    SetButtonStates();

                }

                VM.Overlay.Clear();
            }
        }



        void ChartArray_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() == typeof(LightningChartUltimate))
            {
                LightningChartUltimate chart = (LightningChartUltimate)sender;

                m_mouseDownCol = (int)chart.Tag;

                m_mouseDownPoint = e.GetPosition(chart);

                m_mouseDownRow = GetClickedRow(chart, (int)m_mouseDownPoint.Y);

                m_colWidth = gridChart.ActualWidth / m_cols;
                m_rowHeight = gridChart.ActualHeight / m_rows;

                m_dragDown.X = (m_mouseDownPoint.X + (m_mouseDownCol * m_colWidth)) / gridChart.ActualWidth * VM.Overlay.Width;
                m_dragDown.Y = (m_mouseDownPoint.Y) / gridChart.ActualHeight * VM.Overlay.Height;
            }
        }



        private void ChartArrayGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            VM.Overlay.Clear();

            // reset the mouse down locations
            m_mouseDownRow = -1;
            m_mouseDownCol = -1;
        }



        private void ChartArrayGrid_MouseMove(object sender, MouseEventArgs e)
        {
            VM.Overlay.Clear();

            if(m_mouseDownRow != -1) // mouse button is down
            {
                Point pt = e.GetPosition(gridChart);

                m_drag.X = pt.X / ChartArrayGrid.ActualWidth * VM.Overlay.Width;
                m_drag.Y = pt.Y / ChartArrayGrid.ActualHeight * VM.Overlay.Height;

                int x1,x2,y1,y2;

                if(m_dragDown.X<m_drag.X) 
                {
                    x1 = (int)(m_dragDown.X);
                    x2 = (int)(m_drag.X);
                }
                else
                {
                    x1 = (int)(m_drag.X);
                    x2 = (int)(m_dragDown.X);
                }


                if(m_dragDown.Y<m_drag.Y) 
                {
                    y1 = (int)(m_dragDown.Y);
                    y2 = (int)(m_drag.Y);
                }
                else
                {
                    y1 = (int)(m_drag.Y);
                    y2 = (int)(m_dragDown.Y);
                }

                
                VM.Overlay.DrawRectangle(x1, y1, x2, y2, Colors.Red);
                VM.Overlay.DrawRectangle(x1 + 1, y1 + 1, x2 - 1, y2 - 1, Colors.Red);

                
            }
        }




        int GetClickedRow(LightningChartUltimate chart, int mouseY)
        {
            GraphSegmentInfo gsi = chart.ViewXY.GetGraphSegmentInfo();
            for (int i = 0; i < gsi.SegmentCount; i++)
            {
                if (mouseY >= gsi.SegmentTops[i] && mouseY <= gsi.SegmentBottoms[i]) return i;
            }
            return -1;
        }






        private void UpdateChart(LightningChartUltimate chart, float[][] data)
        {
            if (chart == null) return;

            chart.BeginUpdate();  // disable chart redRaws

            for (int iCh = 0; iCh < m_iTraceCountPerChart; iCh++)
            {
                chart.ViewXY.SampleDataSeries[iCh].AddSamples(data[iCh], false);
            }


            //int pos = m_numPoints - (int)XLen;
            //if (pos < 0) pos = 0;

            chart.ViewXY.XAxes[0].ScrollPosition = m_numPoints;

            chart.EndUpdate();  // enable chart redRaws
        }


        public void ClearPlotData()
        {
            // clear all data from the plots.  Used between runs on the RunExperiment Window.

            foreach(KeyValuePair<int,ExperimentIndicatorContainer> item in m_indicatorDictionary)
            {
                int indicatorID = item.Key;

                PointLineSeries[,] aggRaw = m_Aggregate_Raw_Dictionary[indicatorID];
                PointLineSeries[,] aggStaticRatio = m_Aggregate_StaticRatio_Dictionary[indicatorID];
                PointLineSeries[,] aggControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[indicatorID];
                PointLineSeries[,] aggDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[indicatorID];

                SampleDataSeries[,] caRaw = m_ChartArray_Raw_Dictionary[indicatorID];
                SampleDataSeries[,] caStaticRatio = m_ChartArray_StaticRatio_Dictionary[indicatorID];
                SampleDataSeries[,] caControlSubtraction = m_ChartArray_ControlSubtraction_Dictionary[indicatorID];
                SampleDataSeries[,] caDynamicRatio = m_ChartArray_DynamicRatio_Dictionary[indicatorID];

                m_aggregateChart.BeginUpdate();

                for (int c = 0; c < m_cols; c++)
                {
                    m_charts[c].BeginUpdate();

                    for (int r = 0; r < m_rows; r++)
                    {
                        aggRaw[r, c].Clear();
                        aggStaticRatio[r, c].Clear();
                        aggControlSubtraction[r, c].Clear();
                        aggDynamicRatio[r, c].Clear();                        

                        caRaw[r, c].Clear();
                        caStaticRatio[r, c].Clear();
                        caControlSubtraction[r, c].Clear();
                        caDynamicRatio[r, c].Clear();
                    }

                    m_charts[c].EndUpdate();
                }

                // clear the event markers
                m_aggregateChart.ViewXY.Annotations.Clear();
                m_aggregateChart.ViewXY.LineSeriesCursors.Clear();

                m_aggregateChart.EndUpdate();
            }
        }



        public void AppendNewData(ref float[,] dataRaw, 
                                  ref float[,] dataStaticRatio,
                                  ref float[,] dataControlSubtraction, 
                                  ref float[,] dataDynamicRatio,
                                  int sequenceNumber, int indicatorID)
        {
            SeriesPoint[] points = new SeriesPoint[1];
            float[] sample = new float[1];

            bool resizeRaw = false;
            bool resizeStaticRatio = false;
            bool resizeControlSubtraction = false;
            bool resizeDynamicRatio = false;

            PointLineSeries[,] aggRaw = m_Aggregate_Raw_Dictionary[indicatorID];
            PointLineSeries[,] aggStaticRatio = m_Aggregate_StaticRatio_Dictionary[indicatorID];
            PointLineSeries[,] aggControlSubtraction = m_Aggregate_ControlSubtraction_Dictionary[indicatorID];
            PointLineSeries[,] aggDynamicRatio = m_Aggregate_DynamicRatio_Dictionary[indicatorID];

            SampleDataSeries[,] caRaw = m_ChartArray_Raw_Dictionary[indicatorID];
            SampleDataSeries[,] caStaticRatio = m_ChartArray_StaticRatio_Dictionary[indicatorID];
            SampleDataSeries[,] caControlSubtraction = m_ChartArray_ControlSubtraction_Dictionary[indicatorID];
            SampleDataSeries[,] caDynamicRatio = m_ChartArray_DynamicRatio_Dictionary[indicatorID];


            DataRange range;

            switch (m_visibleSignal)
            {
                case VISIBLE_SIGNAL.RAW:
                    range = m_Raw_Range;
                    break;
                case VISIBLE_SIGNAL.STATIC_RATIO:
                    range = m_StaticRatio_Range;
                    break;
                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                    range = m_ControlSubtraction_Range;
                    break;
                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                    range = m_DynamicRatio_Range;
                    break;
                default:
                    range = new DataRange();
                    range.xMin = 0; range.xMax = 1;
                    range.yMin = 0; range.yMax = 1;
                    break;
            }
            
            int numPoints = caRaw[0,0].PointCount;


            m_aggregateChart.BeginUpdate();

            for(int c = 0; c<m_cols; c++)
            {                
                m_charts[c].BeginUpdate();

                for (int r = 0; r < m_rows; r++ )
                {
                    // RAW
                        // add point in chart array
                        sample[0] = dataRaw[r, c];
                        caRaw[r, c].AddSamples(sample, false);  // indicatorNum came in as 57! 
                        
                        // add point in aggregate chart
                        points[0].Y = dataRaw[r, c];
                        points[0].X = (double)sequenceNumber;
                        aggRaw[r, c].AddPoints(points, false);

                        // check range
                        if (points[0].X < m_Raw_Range.xMin) { m_Raw_Range.xMin = points[0].X * 0.9; resizeRaw = true; }
                        if (points[0].X > m_Raw_Range.xMax) { m_Raw_Range.xMax = points[0].X * 1.2; resizeRaw = true; }
                        if (points[0].Y < m_Raw_Range.yMin) { m_Raw_Range.yMin = points[0].Y * 0.9; resizeRaw = true; }
                        if (points[0].Y > m_Raw_Range.yMax) { m_Raw_Range.yMax = points[0].Y * 1.2; resizeRaw = true; }

                    // STATIC RATIO
                    if (dataStaticRatio != null)
                    {
                        points = new SeriesPoint[1];
                        sample = new float[1];

                        // add point in chart array
                        sample[0] = dataStaticRatio[r, c];
                        caStaticRatio[r, c].AddSamples(sample, false);

                        // add point in aggregate chart
                        points[0].Y = dataStaticRatio[r, c];
                        points[0].X = (double)sequenceNumber;
                        aggStaticRatio[r, c].AddPoints(points, false);

                        // check range
                        if (points[0].X < m_StaticRatio_Range.xMin) { m_StaticRatio_Range.xMin = points[0].X * 0.9; resizeStaticRatio = true; }
                        if (points[0].X > m_StaticRatio_Range.xMax) { m_StaticRatio_Range.xMax = points[0].X * 1.2; resizeStaticRatio = true; }
                        if (points[0].Y < m_StaticRatio_Range.yMin) { m_StaticRatio_Range.yMin = points[0].Y * 0.9; resizeStaticRatio = true; }
                        if (points[0].Y > m_StaticRatio_Range.yMax) { m_StaticRatio_Range.yMax = points[0].Y * 1.2; resizeStaticRatio = true; }
                    }

                    // Control Subtraction
                    if (dataControlSubtraction != null)
                    {
                        points = new SeriesPoint[1];
                        sample = new float[1];

                        // add point in chart array
                        sample[0] = dataControlSubtraction[r, c];
                        caControlSubtraction[r, c].AddSamples(sample, false);

                        // add point in aggregate chart
                        points[0].Y = dataControlSubtraction[r, c];
                        points[0].X = (double)sequenceNumber;
                        aggControlSubtraction[r, c].AddPoints(points, false);

                        // check range
                        if (points[0].X < m_ControlSubtraction_Range.xMin) { m_ControlSubtraction_Range.xMin = points[0].X * 0.9; resizeControlSubtraction = true; }
                        if (points[0].X > m_ControlSubtraction_Range.xMax) { m_ControlSubtraction_Range.xMax = points[0].X * 1.2; resizeControlSubtraction = true; }
                        if (points[0].Y < m_ControlSubtraction_Range.yMin) { m_ControlSubtraction_Range.yMin = points[0].Y * 0.9; resizeControlSubtraction = true; }
                        if (points[0].Y > m_ControlSubtraction_Range.yMax) { m_ControlSubtraction_Range.yMax = points[0].Y * 1.2; resizeControlSubtraction = true; }
                    }

                    // Dynamic Ratio
                    if (dataDynamicRatio != null)
                    {
                        points = new SeriesPoint[1];
                        sample = new float[1];

                        // add point in chart array
                        sample[0] = dataDynamicRatio[r, c];
                        caDynamicRatio[r, c].AddSamples(sample, false);

                        // add point in aggregate chart
                        points[0].Y = dataDynamicRatio[r, c];
                        points[0].X = (double)sequenceNumber;
                        aggDynamicRatio[r, c].AddPoints(points, false);

                        // check range
                        if (points[0].X < m_DynamicRatio_Range.xMin) { m_DynamicRatio_Range.xMin = points[0].X * 0.9; resizeDynamicRatio = true; }
                        if (points[0].X > m_DynamicRatio_Range.xMax) { m_DynamicRatio_Range.xMax = points[0].X * 1.2; resizeDynamicRatio = true; }
                        if (points[0].Y < m_DynamicRatio_Range.yMin) { m_DynamicRatio_Range.yMin = points[0].Y * 0.9; resizeDynamicRatio = true; }
                        if (points[0].Y > m_DynamicRatio_Range.yMax) { m_DynamicRatio_Range.yMax = points[0].Y * 1.2; resizeDynamicRatio = true; }
                    }
                }

                m_charts[c].EndUpdate();                
            }

            m_aggregateChart.EndUpdate();



            ////////////////////////////////////////////////
            //  Set Axes Ranges and location of Bands (used to show selected charts)

            if(resizeRaw && m_visibleSignal==VISIBLE_SIGNAL.RAW)
            {
                SetRangeForSignalType(VISIBLE_SIGNAL.RAW);
            }
            else if (resizeStaticRatio && m_visibleSignal == VISIBLE_SIGNAL.STATIC_RATIO)
            {
                SetRangeForSignalType(VISIBLE_SIGNAL.STATIC_RATIO);               
            }
            else if (resizeControlSubtraction && m_visibleSignal == VISIBLE_SIGNAL.CONTROL_SUBTRACTION)
            {
                SetRangeForSignalType(VISIBLE_SIGNAL.CONTROL_SUBTRACTION);                
            }
            else if (resizeDynamicRatio && m_visibleSignal == VISIBLE_SIGNAL.DYNAMIC_RATIO)
            {
                SetRangeForSignalType(VISIBLE_SIGNAL.DYNAMIC_RATIO);            
            }
            
        }



        private void SetRangeForSignalType(VISIBLE_SIGNAL signalType)
        {
            // this function sets the ranges of all the charts (in both the Chart Array and Aggregate) to the 
            // max/min values for the specified signal type

            DataRange range;

            // get the number of datapoints in the series
            KeyValuePair<int, ExperimentIndicatorContainer> first = m_indicatorDictionary.First();
            SampleDataSeries[,] caRaw = m_ChartArray_Raw_Dictionary[first.Key];
            int numPoints = caRaw[0, 0].PointCount;

            switch (m_visibleSignal)
            {
                case VISIBLE_SIGNAL.RAW:
                    range = m_Raw_Range;
                    break;
                case VISIBLE_SIGNAL.STATIC_RATIO:
                    range = m_StaticRatio_Range;
                    break;
                case VISIBLE_SIGNAL.CONTROL_SUBTRACTION:
                    range = m_ControlSubtraction_Range;
                    break;
                case VISIBLE_SIGNAL.DYNAMIC_RATIO:
                    range = m_DynamicRatio_Range;
                    break;
                default:
                    range = new DataRange();
                    range.xMin = 0; range.xMax = 1;
                    range.yMin = 0; range.yMax = 1;
                    break;
            }            

            for (int c = 0; c < m_cols; c++)
            {
                m_charts[c].BeginUpdate();

                for (int r = 0; r < m_rows; r++)
                {
                    m_charts[c].ViewXY.YAxes[r].SetRange(range.yMin, range.yMax);
                    m_band[r, c].ValueBegin = range.yMin;
                    m_band[r, c].ValueEnd = range.yMax;
                }

                m_charts[c].ViewXY.XAxes[0].SetRange(0, numPoints + 10);

                m_charts[c].EndUpdate();
            }

            m_aggregateChart.BeginUpdate();
            m_aggregateChart.ViewXY.XAxes[0].SetRange(range.xMin, range.xMax);
            m_aggregateChart.ViewXY.YAxes[0].SetRange(range.yMin, range.yMax);
            m_aggregateChart.EndUpdate();

        }
    


        private void gridChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArrangeCharts();
        }



        private void ArrangeCharts()
        {

            if (m_charts == null)
            {
                return;
            }

            Double dAvailW = gridChart.ActualWidth;
            Double dAvailH = gridChart.ActualHeight;
            Double dWidthPerChart = dAvailW / (Double)m_iChartCount;
            Double dHeightPerChart = dAvailH;

            // Set each chart's margin, width and height accordingly.
            for (int iChart = 0; iChart < m_iChartCount; iChart++)
            {
                m_charts[iChart].BeginUpdate();

                m_charts[iChart].Margin = new Thickness(iChart*dWidthPerChart, 1.0, 1.0, 1.0);
                m_charts[iChart].Width = dWidthPerChart;
                m_charts[iChart].Height = dHeightPerChart;

                m_charts[iChart].EndUpdate();
            }
        }

        private void AggregateGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_aggregateChart == null) return;

            m_aggregateChart.BeginUpdate();

            m_aggregateChart.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
            m_aggregateChart.Width = AggregateGrid.ActualWidth;
            m_aggregateChart.Height = AggregateGrid.ActualHeight;

            m_aggregateChart.EndUpdate();
        }




        private void RangeSlider_TrackFillDragCompleted(object sender, object e)
        {
            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            m_colorModel.m_controlPts[1].m_value = (int)lowerSliderValue;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = (int)upperSliderValue;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();
            DrawColorMap();

            foreach (KeyValuePair<int,ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {                
                m_imager.m_RangeSliderLowerSliderPosition = lowerSliderValue;
                m_imager.m_RangeSliderUpperSliderPosition = upperSliderValue;

                m_imager.RedisplayCurrentImage(entry.Key, lowerSliderValue, upperSliderValue);
            }
        }

        private void RangeMinThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.BuildColorMap();
            DrawColorMap();

            foreach (KeyValuePair<int,ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                m_imager.m_RangeSliderLowerSliderPosition = lowerSliderValue;
                m_imager.m_RangeSliderUpperSliderPosition = upperSliderValue;

                m_imager.RedisplayCurrentImage(entry.Key, lowerSliderValue, upperSliderValue);
            }
        }

        private void RangeMaxThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();
            DrawColorMap();

            foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                m_imager.m_RangeSliderLowerSliderPosition = lowerSliderValue;
                m_imager.m_RangeSliderUpperSliderPosition = upperSliderValue;

                m_imager.RedisplayCurrentImage(entry.Key, lowerSliderValue, upperSliderValue);
            }
        }

       

        public void DrawColorMap()
        {
            if (m_colorModel == null) return;

            int colorMapWidth = 40;

            if (m_colorMapBitmap == null)
            {
                m_colorMapBitmap = BitmapFactory.New(colorMapWidth, m_colorModel.m_maxPixelValue);
                ColorMapImage.Source = m_colorMapBitmap;
            }

            for (int i = 0; i < m_colorModel.m_maxPixelValue; i++)
            {
                Color color = new Color();
                color.A = 255;
                color.R = m_colorModel.m_colorMap[i].m_red;
                color.G = m_colorModel.m_colorMap[i].m_green;
                color.B = m_colorModel.m_colorMap[i].m_blue;

                m_colorMapBitmap.DrawLine(0, m_colorModel.m_maxPixelValue - 1 - i, colorMapWidth, m_colorModel.m_maxPixelValue - 1 - i, color);
            }
        }



        private void AnalysisRadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() != typeof(RadioButton)) return;

            RadioButton rb = (RadioButton)sender;
            string tag = (string)rb.Tag;

            switch (tag)
            {
                case "Raw":
                    m_visibleSignal = VISIBLE_SIGNAL.RAW;
                    break;
                case "StaticRatio":
                    m_visibleSignal = VISIBLE_SIGNAL.STATIC_RATIO;
                    break;
                case "ControlSubtraction":
                    m_visibleSignal = VISIBLE_SIGNAL.CONTROL_SUBTRACTION;
                    break;
                case "DynamicRatio":
                    m_visibleSignal = VISIBLE_SIGNAL.DYNAMIC_RATIO;
                    break;
                default:
                    m_visibleSignal = VISIBLE_SIGNAL.RAW;
                    break;
            }

            if(m_charts != null) SetAnalysisVisibility();  // only set the visibility if the actual charts have been created
        }



        private void ColorMapImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WaveguideDB wgDB = new WaveguideDB();

            bool success = wgDB.GetAllColorModels();

            if (success)
            {
                ColorModelSelectDialog diag = new ColorModelSelectDialog(wgDB.m_colorModelList);
                diag.ShowDialog();

                int colorModelID = diag.dbID;

                for (int i = 0; i < wgDB.m_colorModelList.Count(); i++)
                {
                    if (wgDB.m_colorModelList[i].ColorModelID == colorModelID)
                    {
                        ColorModel model = new ColorModel(wgDB.m_colorModelList[i].Description, wgDB.m_colorModelList[i].MaxPixelValue, wgDB.m_colorModelList[i].GradientSize);
                        for (int j = 0; j < wgDB.m_colorModelList[i].Stops.Count(); j++)
                        {
                            model.InsertColorStop(wgDB.m_colorModelList[i].Stops[j].ColorIndex,
                                                  wgDB.m_colorModelList[i].Stops[j].Red,
                                                  wgDB.m_colorModelList[i].Stops[j].Green,
                                                  wgDB.m_colorModelList[i].Stops[j].Blue);
                        }

                        model.SetMaxPixelValue(GlobalVars.MaxPixelValue);

                        model.m_controlPts.Clear();
                        model.m_controlPts.Add(new ColorControlPoint(0, 0));
                        model.m_controlPts.Add(new ColorControlPoint(0, 0));
                        model.m_controlPts.Add(new ColorControlPoint(model.m_maxPixelValue, model.m_gradientSize - 1));
                        model.m_controlPts.Add(new ColorControlPoint(model.m_maxPixelValue, model.m_gradientSize - 1));

                        model.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
                        model.m_controlPts[1].m_colorIndex = 0;
                        model.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
                        model.m_controlPts[2].m_colorIndex = 1023;
                        model.BuildColorGradient();
                        model.BuildColorMap();

                        m_colorModel = model;

                        DrawColorMap();

                        UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
                        UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

                        foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
                        {
                            int experimentIndicatorID = entry.Key;

                            m_imager.SetColorModel(m_colorModel);

                            m_imager.RedisplayCurrentImage(experimentIndicatorID,lowerSliderValue,upperSliderValue);
                        }

                        break;
                    }
                }
            }
           
        }


        private void TemperatureEdit_ValueChanged(object sender, EventArgs e)
        {
            GlobalVars.CameraTargetTemperature = VM.TemperatureTarget;

            if (m_imager != null)
                m_imager.m_camera.SetCoolerTemp(VM.TemperatureTarget);

            VM.EvalTemperature();
        }

        private void Binning_1x1_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            VM.VertBinning = 1;
            VM.HorzBinning = 1;
        }

        private void Binning_2x2_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            VM.VertBinning = 2;
            VM.HorzBinning = 2;
        }

        private void Binning_4x4_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            VM.VertBinning = 4;
            VM.HorzBinning = 4;
        }

        private void Binning_8x8_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            VM.VertBinning = 8;
            VM.HorzBinning = 8;
        }


        private void VerifyPB_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataRecord record = btn.DataContext as DataRecord;
            ExperimentIndicatorContainer indicator = (ExperimentIndicatorContainer)record.DataItem;

            ManualControlDialog dlg = new ManualControlDialog(m_imager, indicator.ExperimentIndicatorID, false, false);


            FilterContainer exFilt, emFilt;
            exFilt = null;
            emFilt = null;
            int previousBinning = m_imager.m_camera.m_acqParams.HBin;

            bool success = wgDB.GetAllExcitationFilters();
            if (success)
            {
                foreach (FilterContainer fc in wgDB.m_filterList)
                {
                    if (fc.PositionNumber == indicator.ExcitationFilterPos)
                        exFilt = fc;
                }
            }

            success = wgDB.GetAllEmissionFilters();
            if (success)
            {
                foreach (FilterContainer fc in wgDB.m_filterList)
                {
                    if (fc.PositionNumber == indicator.EmissionFilterPos)
                        emFilt = fc;
                }
            }

            
            dlg.Title = indicator.Description;
            dlg.CameraSetupControl.vm.Exposure = indicator.Exposure;
            dlg.CameraSetupControl.vm.EMGain = indicator.Gain;
            dlg.CameraSetupControl.vm.EmFilter = emFilt;
            dlg.CameraSetupControl.vm.ExFilter = exFilt;
          
                     
            dlg.ShowDialog();
            
            indicator.Exposure = dlg.CameraSetupControl.vm.Exposure;
            indicator.Gain = dlg.CameraSetupControl.vm.EMGain;            
             

            // if the binning was changed, un-verify all other indicators
            if (VM.HorzBinning != previousBinning) // binning was changed
            {
                foreach (ExperimentIndicatorContainer ind in VM.IndicatorList)
                {
                    ind.Verified = false;
                }
            }


            indicator.Verified = true;

            VM.EvalRunStatus();
        
        }



        private void CompoundPlateDataGrid_CellUpdated(object sender, Infragistics.Windows.DataPresenter.Events.CellUpdatedEventArgs e)
        {
            VM.EvalRunStatus();
        }



    }




/// <summary>
/// ////////////////////////////////
///  VIEW MODEL
///  
/// </summary>


    public class ViewModel_ChartArray : IDataErrorInfo, INotifyPropertyChanged
    {
        public enum RUN_STATUS { NEEDS_INPUT, READY_TO_RUN, RUNNING, RUN_FINISHED };
                
        


        private RUN_STATUS _status;
        public RUN_STATUS Status
        {
            get
            {
                return this._status;
            }

            set
            {
                if (value != this._status)
                {
                    this._status = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private PlateContainer _experimentPlate;
        public PlateContainer ExperimentPlate
        {
            get
            {
                return this._experimentPlate;
            }

            set
            {
                if (value != this._experimentPlate)
                {
                    this._experimentPlate = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ExperimentContainer _experiment;
        public ExperimentContainer Experiment
        {
            get
            {
                return this._experiment;
            }

            set
            {
                if (value != this._experiment)
                {
                    this._experiment = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private string _imagePlateBarcode;
        public string ImagePlateBarcode
        {
            get
            {
                return this._imagePlateBarcode;
            }

            set
            {
                if (value != this._imagePlateBarcode)
                {
                    this._imagePlateBarcode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        


        private int _cycleTime;
        public int CycleTime
        {
            get
            {
                return this._cycleTime;
            }

            set
            {
                if (value != this._cycleTime)
                {
                    this._cycleTime = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private int _temperatureTarget;
        public int TemperatureTarget
        {
            get
            {
                return this._temperatureTarget;
            }

            set
            {
                if (value != this._temperatureTarget)
                {
                    this._temperatureTarget = value;
                    NotifyPropertyChanged();
                }                
            }
        }


        private int _temperatureActual;
        public int TemperatureActual
        {
            get
            {
                return this._temperatureActual;
            }

            set
            {
                if (value != this._temperatureActual)
                {
                    this._temperatureActual = value;
                    NotifyPropertyChanged();
                }

                EvalTemperature();
            }
        }


        private bool _temperatureReady;
        public bool TemperatureReady
        {
            get
            {
                return this._temperatureReady;
            }

            set
            {
                if (value != this._temperatureReady)
                {
                    this._temperatureReady = value;
                    NotifyPropertyChanged();
                }
            }
        }



        private int _vertBinning;
        public int VertBinning
        {
            get
            {
                return this._vertBinning;
            }

            set
            {
                if (value != this._vertBinning)
                {
                    this._vertBinning = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private int _horzBinning;
        public int HorzBinning
        {
            get
            {
                return this._horzBinning;
            }

            set
            {
                if (value != this._horzBinning)
                {
                    this._horzBinning = value;
                    NotifyPropertyChanged();
                }
            }
        }



        private ObservableCollection<ExperimentIndicatorContainer> _indicatorList;
        public ObservableCollection<ExperimentIndicatorContainer> IndicatorList
        {
            get
            {
                return this._indicatorList;
            }

            set
            {
                if (value != this._indicatorList)
                {
                    this._indicatorList = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private ObservableCollection<ExperimentCompoundPlateContainer> _compoundPlateList;
        public ObservableCollection<ExperimentCompoundPlateContainer> CompoundPlateList
        {
            get
            {
                return this._compoundPlateList;
            }

            set
            {
                if (value != this._compoundPlateList)
                {
                    this._compoundPlateList = value;
                    NotifyPropertyChanged();
                }
            }
        }




        private WriteableBitmap _overlay;
        public WriteableBitmap Overlay
        {
            get
            {
                return this._overlay;
            }

            set
            {
                if (value != this._overlay)
                {
                    this._overlay = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private WriteableBitmap _gridLines;
        public WriteableBitmap GridLines
        {
            get
            {
                return this._gridLines;
            }

            set
            {
                if (value != this._gridLines)
                {
                    this._overlay = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private bool _plateBarcodeValid;
        public bool PlateBarcodeValid
        {
            get{ return this._plateBarcodeValid; }
            set{
                if (value != this._plateBarcodeValid)
                {
                    this._plateBarcodeValid = value;
                    NotifyPropertyChanged();
                }
            }
        }



        public ViewModel_ChartArray()
        {
            _overlay = BitmapFactory.New(800, 450);
            _gridLines = BitmapFactory.New(800, 450);
        }


        public string Error
        {
            get { throw new NotImplementedException(); }
        }


        public string this[string columnName]
        {
            get
            {
                string result = null;
                if (columnName == "ImagePlateBarcode")
                {
                    if (string.IsNullOrEmpty(ImagePlateBarcode))
                    {
                        PlateBarcodeValid = false;
                        result = "Please enter a Barcode for the Image Plate";
                        EvalRunStatus();
                    }
                    //else if (ImagePlateBarcode.Length != 8)
                    //{
                    //    PlateBarcodeValid = false;
                    //    result = "Barcode must be exactly 8 characters";
                    //    SetExperimentStatus();
                    //}
                    else
                    {
                        PlateBarcodeValid = true;
                        EvalRunStatus();
                    }
                }
                return result;
            }
        }


        public void Reset()
        {
            ImagePlateBarcode = "";

            foreach (ExperimentIndicatorContainer ind in IndicatorList)
            {
                ind.Verified = false;
            }

            foreach (ExperimentCompoundPlateContainer cp in CompoundPlateList)
            {
                cp.Barcode = "";
            }

            SetRunStatus(RUN_STATUS.NEEDS_INPUT);
        }


        public void EvalTemperature()
        {
            int deviation = TemperatureActual - (TemperatureTarget + GlobalVars.MaxTemperatureThresholdDeviation);

            if (deviation <= 0)
                TemperatureReady = true;
            else
                TemperatureReady = false;

            EvalRunStatus();
        }



        public void EvalRunStatus()
        {  // Possible Status: NEEDS_INPUT, READY_TO_RUN, RUNNING, RUN_FINISHED  

            if (Status == RUN_STATUS.RUN_FINISHED || Status == RUN_STATUS.RUNNING) return;

            if(PlateBarcodeValid)
            {
                bool allIndicatorsVerified = true;
                foreach(ExperimentIndicatorContainer ind in IndicatorList)
                {
                    if (!ind.Verified) allIndicatorsVerified = false;
                }

                bool allCompoundPlatesVerified = true;
                foreach(ExperimentCompoundPlateContainer cp in CompoundPlateList)
                {
                    if (!cp.BarcodeValid) allCompoundPlatesVerified = false;
                }

                if (allIndicatorsVerified && allCompoundPlatesVerified && TemperatureReady) Status = RUN_STATUS.READY_TO_RUN;
                else Status = RUN_STATUS.NEEDS_INPUT;
            }
            else
            {
                Status = RUN_STATUS.NEEDS_INPUT;
            }

            ChartArrayViewModel_EventArgs e = new ChartArrayViewModel_EventArgs();
            e.RunStatus = Status;
            StatusChange(this, e);
        }

        public void SetRunStatus(RUN_STATUS runStatus)
        {
            Status = runStatus;

            ChartArrayViewModel_EventArgs e = new ChartArrayViewModel_EventArgs();
            e.RunStatus = runStatus;
            StatusChange(this, e);
        }



        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        // ////////////////////////////////////////////////
        // Set up the event
        public event StatusChange_EventHandler StatusChange;
        public delegate void StatusChange_EventHandler(ViewModel_ChartArray VM_ChartArray, ChartArrayViewModel_EventArgs e);

    }



    public class ChartArrayViewModel_EventArgs : EventArgs
    {
        private ViewModel_ChartArray.RUN_STATUS _runStatus;
        public ViewModel_ChartArray.RUN_STATUS RunStatus
        {
            set { _runStatus = value; }
            get { return this._runStatus; }
        }
    }


}
