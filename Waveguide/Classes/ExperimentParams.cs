using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waveguide
{

    /////////////////////////////
    // ExperimentParams Singleton
    public sealed class ExperimentParams : INotifyPropertyChanged
    {
        private static readonly Lazy<ExperimentParams> lazy =
            new Lazy<ExperimentParams>(() => new ExperimentParams());

        public static ExperimentParams GetExperimentParams { get { return lazy.Value; } }


        /////////////////////////////
        // Private Constructor
        private ExperimentParams()
        {
            _numFoFrames = 5;
            _compoundPlateList = new System.Collections.ObjectModel.ObservableCollection<ExperimentCompoundPlateContainer>();
            _controlSubtractionWellList = new System.Collections.ObjectModel.ObservableCollection<Tuple<int, int>>();
            _indicatorList = new System.Collections.ObjectModel.ObservableCollection<ExperimentIndicatorContainer>();
        }

        /////////////////////////////
        // Properties

        private ProjectContainer _project;
        public  ProjectContainer project { get { return _project; } set { if (value != _project) { _project = value; NotifyPropertyChanged("project"); } } }

        private MethodContainer _method;
        public MethodContainer method { get { return _method; } set { if (value != _method) { _method = value; NotifyPropertyChanged("method"); } } }

        private PlateTypeContainer _plateType;
        public PlateTypeContainer plateType { get { return _plateType; } set { if (value != _plateType) { _plateType = value; NotifyPropertyChanged("plateType"); } } }

        private MaskContainer _mask;
        public MaskContainer mask { get { return _mask; } set { if (value != _mask) { _mask = value; NotifyPropertyChanged("mask"); } } }

        private ObservableCollection<ExperimentIndicatorContainer> _indicatorList;
        public ObservableCollection<ExperimentIndicatorContainer> indicatorList { get { return _indicatorList; } set { if (value != _indicatorList) { _indicatorList = value; NotifyPropertyChanged("indicatorList"); } } }

        private ObservableCollection<ExperimentCompoundPlateContainer> _compoundPlateList;
        public ObservableCollection<ExperimentCompoundPlateContainer> compoundPlateList { get { return _compoundPlateList; } set { if (value != _compoundPlateList) { _compoundPlateList = value; NotifyPropertyChanged("compoundPlateList"); } } }

        private ObservableCollection<Tuple<int, int>> _controlSubtractionWellList;
        public ObservableCollection<Tuple<int, int>> controlSubtractionWellList { get { return _controlSubtractionWellList; } set { if (value != _controlSubtractionWellList) { _controlSubtractionWellList = value; NotifyPropertyChanged("controlSubtractionWellList"); } } }

        private int _numFoFrames;
        public int numFoFrames { get { return _numFoFrames; } set { if (value != _numFoFrames) { _numFoFrames = value; NotifyPropertyChanged("numFoFrames"); } } }

        private ExperimentIndicatorContainer _dynamicRatioNumerator;
        public ExperimentIndicatorContainer dynamicRatioNumerator { get { return _dynamicRatioNumerator; } set { if (value != _dynamicRatioNumerator) { _dynamicRatioNumerator = value; NotifyPropertyChanged("dynamicRatioNumerator"); } } }

        private ExperimentIndicatorContainer _dynamicRatioDenominator;
        public ExperimentIndicatorContainer dynamicRatioDenominator { get { return _dynamicRatioDenominator; } set { if (value != _dynamicRatioDenominator) { _dynamicRatioDenominator = value; NotifyPropertyChanged("dynamicRatioDenominator"); } } }



        /////////////////////////////
        // INotifyPropertyChanged implemented
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

    }

  
}
