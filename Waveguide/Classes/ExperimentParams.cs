using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waveguide
{
    public class ExperimentParams
    {
        public ProjectContainer project;
        public MethodContainer method;
        public PlateTypeContainer plateType;
        public MaskContainer mask;
        public ObservableCollection<ExperimentIndicatorContainer> indicatorList;
        public ObservableCollection<ExperimentCompoundPlateContainer> compoundPlateList;
        public ObservableCollection<Tuple<int, int>> controlSubtractionWellList;
        public int numFoFrames;
        public ExperimentIndicatorContainer dynamicRatioNumerator;
        public ExperimentIndicatorContainer dynamicRatioDenominator;
    }
}
