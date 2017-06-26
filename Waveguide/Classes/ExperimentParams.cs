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

        WaveguideDB m_wgDB;

        /////////////////////////////
        // Private Constructor
        private ExperimentParams()
        {
            _numFoFrames = 5;
            _experimentPlate = new PlateContainer();
            _experiment = new ExperimentContainer();
            _compoundPlateList = new System.Collections.ObjectModel.ObservableCollection<ExperimentCompoundPlateContainer>();
            _controlSubtractionWellList = new System.Collections.ObjectModel.ObservableCollection<Tuple<int, int>>();
            _indicatorList = new System.Collections.ObjectModel.ObservableCollection<ExperimentIndicatorContainer>();


            m_wgDB = new WaveguideDB();
            bool success = m_wgDB.GetCameraSettingsDefault(out _cameraSettings);
            if (!success)
            {
                _cameraSettings = new CameraSettingsContainer(); 
            }
        }

        /////////////////////////////
        // Properties

        private UserContainer _user;
        public UserContainer user { get { return _user; } set { if (value != _user) { _user = value; NotifyPropertyChanged("user"); } } }

        private PlateContainer _experimentPlate;
        public PlateContainer experimentPlate { get { return _experimentPlate; } set { if (value != _experimentPlate) { _experimentPlate = value; NotifyPropertyChanged("experimentPlate"); } } }

        private ExperimentContainer _experiment;
        public ExperimentContainer experiment { get { return _experiment; } set { if (value != _experiment) { _experiment = value; NotifyPropertyChanged("experiment"); } } }

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

        private CameraSettingsContainer _cameraSettings;
        public CameraSettingsContainer cameraSettings { get { return _cameraSettings; } set { if (value != _cameraSettings) { _cameraSettings = value; NotifyPropertyChanged("cameraSettings"); } } }


        /////////////////////////////
        // INotifyPropertyChanged implemented
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

    }

  
}
