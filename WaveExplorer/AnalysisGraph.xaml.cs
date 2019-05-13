using System;
using System.Collections.Generic;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using WPFTools;

namespace WaveExplorer
{

    public enum SeriesType
    {
        RAW,
        RAW_DERIVATIVE
    };




    public partial class AnalysisGraph : UserControl
    {
        AnalysisGraph_ViewModel m_vm;

        Dictionary<Tuple<SeriesType,int,int>, LineSeries> m_lineseriesDictionary;

        public AnalysisGraph()
        {
            InitializeComponent();
            m_vm = new AnalysisGraph_ViewModel();
            DataContext = m_vm;
           
        }

        public void Init(int numRows, int numCols)
        {
            m_vm.rows = numRows;
            m_vm.cols = numCols;

            m_lineseriesDictionary = new Dictionary<Tuple<SeriesType, int,int>, LineSeries>();

            m_vm.model.Series.Clear();

            for(int r = 0; r<numRows; r++)
                for(int c = 0; c<numCols; c++)
                {
                    LineSeries series = new LineSeries();
                    series.IsVisible = false;

                    m_lineseriesDictionary.Add(Tuple.Create<SeriesType, int, int>(SeriesType.RAW, r, c), series);                   

                    m_vm.model.Series.Add(series);
                }
        }

        public void AddDataPoint_ToAllInSeries(SeriesType seriesType, double[] data, double time)
        {
            int ndx = 0;
            for (int r = 0; r < m_vm.rows; r++)
                for (int c = 0; c < m_vm.cols; c++)
                {
                    LineSeries series;
                    if(m_lineseriesDictionary.TryGetValue(Tuple.Create<SeriesType, int, int>(seriesType, r, c), out series))
                    {
                        series.Points.Add(new DataPoint(data[ndx], time));
                    }
                    ndx++;
                }
        }


    }



    public class AnalysisGraph_ViewModel : ObservableObject
    {

        private int _rows;
        public int rows
        {
            get { return _rows; }
            set { if (value != _rows) { _rows = value; OnPropertyChanged("rows"); } }
        }


        private int _cols;
        public int cols
        {
            get { return _cols; }
            set { if (value != _cols) { _cols = value; OnPropertyChanged("cols"); } }
        }


        private PlotModel _model;
        public PlotModel model
        {
            get { return _model; }
            set { if (value != _model) { _model = value; OnPropertyChanged("model"); }}
        }




        private static LineSeries CreateNormalDistributionSeries(double x0, double x1, double mean, double variance, int n = 1000)
        {
            var ls = new LineSeries
            {
                Title = string.Format("μ={0}, σ²={1}", mean, variance)
            };

            for (int i = 0; i < n; i++)
            {
                double x = x0 + ((x1 - x0) * i / (n - 1));
                double f = 1.0 / Math.Sqrt(2 * Math.PI * variance) * Math.Exp(-(x - mean) * (x - mean) / 2 / variance);
                ls.Points.Add(new DataPoint(x, f));
            }

            return ls;
        }


        // define Constructor
        public AnalysisGraph_ViewModel()
        {

            rows = 0;
            cols = 0;


            _model = new PlotModel();


            _model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = -0.05,
                Maximum = 1.05,
                MajorStep = 0.2,
                MinorStep = 0.05,
                TickStyle = TickStyle.Inside
            });
            _model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = -5.25,
                Maximum = 5.25,
                MajorStep = 1,
                MinorStep = 0.25,
                TickStyle = TickStyle.Inside
            });

        }

       
    }
}
