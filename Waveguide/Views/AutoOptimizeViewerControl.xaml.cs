using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Waveguide;

namespace WPFTools
{
    /// <summary>
    /// Interaction logic for AutoOptimizeViewerControl.xaml
    /// </summary>
    public partial class AutoOptimizeViewerControl : UserControl
    {

        public AutoOptimize_ViewModel vm;
        public Imager m_imager;
      
        public AutoOptimizeViewerControl()
        {
            InitializeComponent();

      
            vm = new AutoOptimize_ViewModel();
            DataContext = vm;

         
            foreach(ExperimentIndicatorContainer eic in vm.ExpParams.indicatorList)
            {
                AddIndicator(eic.ExperimentIndicatorID, eic.Description, eic.Exposure, eic.Gain, eic.PreAmpGain, 1, eic.ExcitationFilterDesc, eic.EmissionFilterDesc);
            }

        }

        public void Configure(Imager imager)
        {
            m_imager = imager;

            if(m_imager != null)
                m_imager.m_optimizeEvent += m_imager_m_optimizeEvent;
        }

        public void Init()
        {
            vm.OptimizeIndicatorList.Clear();

            foreach(ExperimentIndicatorContainer eic in vm.ExpParams.indicatorList)
            {
                vm.OptimizeIndicatorList.Add(new OptimizeIndicatorItem(eic.ExperimentIndicatorID, eic.Description, eic.Exposure, eic.Gain, eic.PreAmpGain, 1, eic.ExcitationFilterDesc, eic.EmissionFilterDesc));
            }
        }

        void m_imager_m_optimizeEvent(object sender, OptimizeEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {

                foreach (OptimizeIndicatorItem oii in vm.OptimizeIndicatorList)
                {
                    if (oii.IndicatorID == e.IndicatorID)
                    {
                        if (e.ImageData != null)
                            oii.SetImage(e.ImageWidth, e.ImageHeight, e.ImageData);
                        oii.Exposure = e.Exposure;
                        oii.Gain = e.Gain;
                        oii.Binning = e.Binning;
                        oii.PreAmpGain = e.PreAmpGain;
                        break;
                    }
                }

                foreach (ExperimentIndicatorContainer eic in vm.ExpParams.indicatorList)
                {
                    if (eic.ExperimentIndicatorID == e.IndicatorID)
                    {
                        eic.Exposure = e.Exposure;
                        eic.Gain = e.Gain;
                        eic.PreAmpGain = e.PreAmpGain;
                        break;
                    }
                }

            }));

        }

      

        public void Reset()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                vm.OptimizeIndicatorList.Clear();
            }));
        }


        public void AddIndicator(int indicatorID, string indicatorName, int exposure, int gain, int preAmpGain, int binning, string excitationFilter, string emissionFilter)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                vm.OptimizeIndicatorList.Add(new OptimizeIndicatorItem(indicatorID, indicatorName,exposure,gain,preAmpGain,binning,excitationFilter,emissionFilter));
            }));
        }


        public void UpdateImage(int indicatorID, int width, int height, byte[] colorImage)
        {
            foreach(OptimizeIndicatorItem oii in vm.OptimizeIndicatorList)
            {
                if(oii.IndicatorID == indicatorID)
                {
                    oii.SetImage(width, height, colorImage);
                    break;
                }
            }
        }


        public void UpdateData(int indicatorID, int exposure, int gain, int preAmpGain, int binning)
        {
            foreach (OptimizeIndicatorItem oii in vm.OptimizeIndicatorList)
            {
                if (oii.IndicatorID == indicatorID)
                {
                    oii.Exposure = exposure;
                    oii.Gain = gain;
                    oii.PreAmpGain = preAmpGain;
                    oii.Binning = binning;
                    break;
                }
            }
        }

        public void IsOptimizing(int indicatorID)
        {
            foreach (OptimizeIndicatorItem oii in vm.OptimizeIndicatorList)
            {
                if (oii.IndicatorID == indicatorID)
                {
                    oii.IsOptimizing = true;                    
                }
                else
                {
                    oii.IsOptimizing = false;
                }
            }
        }

        public void FinishedOptimizing()
        {
            foreach (OptimizeIndicatorItem oii in vm.OptimizeIndicatorList)
            {               
                oii.IsOptimizing = false;               
            }
        }

    }

    

    public class OptimizeIndicatorItem : INotifyPropertyChanged
    {
      

        private int _indicatorID;
        public int IndicatorID
        {
            get { return _indicatorID; }
            set { _indicatorID = value; NotifyPropertyChanged("IndicatorID"); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        private int _exposure;
        public int Exposure
        {
            get { return _exposure; }
            set { _exposure = value; NotifyPropertyChanged("Exposure"); }
        }

        private int _gain;
        public int Gain
        {
            get { return _gain; }
            set { _gain = value; NotifyPropertyChanged("Gain"); }
        }

        private int _preAmpGain;
        public int PreAmpGain
        {
            get { return _preAmpGain; }
            set { _preAmpGain = value; NotifyPropertyChanged("PreAmpGain"); }
        }

        private int _binning;
        public int Binning
        {
            get { return _binning; }
            set
            {
                _binning = value; NotifyPropertyChanged("Binning");
                switch (value)
                {
                    case 1: BinningString = "1x1";
                        break;
                    case 2: BinningString = "2x2";
                        break;
                    case 4: BinningString = "4x4";
                        break;
                    case 8: BinningString = "8x8";
                        break;
                    default:
                        BinningString = "--";
                        break;
                }
            }
        }

        private string _binningString;
        public string BinningString
        {
            get { return _binningString; }
            set { _binningString = value; NotifyPropertyChanged("BinningString"); }
        }

        private string _excitationFilter;
        public string ExcitationFilter
        {
            get { return _excitationFilter; }
            set { _excitationFilter = value; NotifyPropertyChanged("ExcitationFilter"); }
        }

        private string _emissionFilter;
        public string EmissionFilter
        {
            get { return _emissionFilter; }
            set { _emissionFilter = value; NotifyPropertyChanged("EmissionFilter"); }
        }

        private bool _isOptimizing;
        public bool IsOptimizing
        {
            get { return _isOptimizing; }
            set { _isOptimizing = value; NotifyPropertyChanged("IsOptimizing"); }
        }

        private WriteableBitmap _bitmap;
        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; NotifyPropertyChanged("Bitmap"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

        
        public void SetImage(int width, int height, byte[] colorImage)
        {
            Bitmap = BitmapFactory.New(width, height);

            Int32Rect rect = new Int32Rect(0, 0, width, height);

            Bitmap.Lock();
            Bitmap.WritePixels(rect, colorImage, width * 4, 0);
            Bitmap.Unlock();
        }

        public OptimizeIndicatorItem(int indicatorID, string name, int exposure, int gain, int preAmpGain, int binning, string exFilt, string emFilt)
        {
            IndicatorID = indicatorID;
            Name = name;
            Exposure = exposure;
            Gain = gain;
            PreAmpGain = preAmpGain;
            Binning = binning;
            ExcitationFilter = exFilt;
            EmissionFilter = emFilt;
            IsOptimizing = false;
        }
    }


    public class AutoOptimize_ViewModel : INotifyPropertyChanged
    {

        // make ExperimentParams Singleton part of view model (used to store selections made by user)
        private ExperimentParams _expParams;
        public ExperimentParams ExpParams { get { return _expParams; } }

        private ObservableCollection<OptimizeIndicatorItem> _optimizeIndicatorList;
        public ObservableCollection<OptimizeIndicatorItem> OptimizeIndicatorList
        {
            get { return _optimizeIndicatorList; }
            set { _optimizeIndicatorList = value; NotifyPropertyChanged("OptimizeIndicatorList"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

        
        public AutoOptimize_ViewModel()
        {
            _optimizeIndicatorList = new ObservableCollection<OptimizeIndicatorItem>();
            _expParams = ExperimentParams.GetExperimentParams;
        }

    }
}
