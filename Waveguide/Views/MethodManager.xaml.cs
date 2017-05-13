using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Editors;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    /// Interaction logic for MethodManager.xaml
    /// </summary>
    public partial class MethodManager : UserControl
    {
        ObservableCollection<MethodContainer> methodSource = new ObservableCollection<MethodContainer>();

        WaveguideDB wgDB;

        MethodManagerViewModel VM;

        public static readonly RoutedCommand BrowseButtonCommand = new RoutedCommand();
        public static readonly RoutedCommand IsPublicCheckBoxCommand = new RoutedCommand();




        void IsPublicCheckBoxCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // The ShowInChartCommand command can execute if the parameter references a Customer.
            e.CanExecute = e.Parameter is MethodItem;
        }

        void IsPublicCheckBoxCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var method = e.Parameter as MethodItem;
            if (method != null)
                this.SetIsPublicOnMethod(method,method.IsPublic);
        }




        void BrowseButtonCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // The ShowInChartCommand command can execute if the parameter references a Customer.
            e.CanExecute = e.Parameter is MethodItem;
        }

        void BrowseButtonCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var method = e.Parameter as MethodItem;
            if (method != null)
                this.BrowseForBravoMethodFile(method);
        }

        void BrowseForBravoMethodFile(MethodItem method)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".pro";
            dlg.Filter = "Bravo Protocol Files (*.pro)|*.pro|ANY Files (*.*)|*.*";
            dlg.InitialDirectory = GlobalVars.VWorksProtocolFileDirectory;
            if(File.Exists(method.BravoMethodFile))
                dlg.FileName = method.BravoMethodFile;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();
            
            // 
            if (result == true)
            {                
                method.BravoMethodFile = dlg.FileName;

                if (wgDB == null) wgDB = new WaveguideDB();

                MethodContainer mc = new MethodContainer();
                mc.Description = method.Description;
                mc.BravoMethodFile = method.BravoMethodFile;
                mc.IsPublic = method.IsPublic;
                mc.MethodID = method.MethodID;
                mc.OwnerID = method.OwnerID;               

                wgDB.UpdateMethod(mc);
            }

        }


        void SetIsPublicOnMethod(MethodItem method, bool isPublic)
        {
            method.IsPublic = isPublic;

            if (wgDB == null) wgDB = new WaveguideDB();

            MethodContainer mc = new MethodContainer();
            mc.Description = method.Description;
            mc.BravoMethodFile = method.BravoMethodFile;
            mc.IsPublic = method.IsPublic;
            mc.MethodID = method.MethodID;
            mc.OwnerID = method.OwnerID;

            wgDB.UpdateMethod(mc);
        }






        public MethodManager()
        {
            InitializeComponent();

            wgDB = new WaveguideDB();

            VM = new MethodManagerViewModel();

            RefreshSignalTypeList();
            RefreshFilterList();

             //Initialize data in the XamDataGrid - NOTE: A blank record is added FIRST, this is key to this approach for the XamDataGrid
            MethodItem blank = new MethodItem();
            blank.Description = "";
            blank.BravoMethodFile = "";
            blank.OwnerID = GlobalVars.UserID;
            blank.MethodID = 0;
            blank.IsPublic = false;
            VM.Methods.Add(blank);

            // load all methods for user
            bool success = wgDB.GetAllMethodsForUser(GlobalVars.UserID);
            if (success)
            {                
                foreach(MethodContainer mc in wgDB.m_methodList)
                {
                    MethodItem mi = new MethodItem();
                    mi.Description = mc.Description;
                    mi.BravoMethodFile = mc.BravoMethodFile;
                    mi.OwnerID = mc.OwnerID;
                    mi.MethodID = mc.MethodID;
                    mi.IsPublic = mc.IsPublic;

                    VM.Methods.Add(mi);

                    // load all indicators for the method
                         // add blank Indicator container to hold the AddRecord
                    ObservableCollection<FilterContainer> exFilts = VM.ExcitationFilters;
                    ObservableCollection<FilterContainer> emFilts = VM.EmissionsFilters;
                    ObservableCollection<SignalTypeContainer> stList = VM.SignalTypeList;
                    IndicatorItem blankInd = new IndicatorItem(0, mi.MethodID, "",ref stList,ref exFilts, ref emFilts);                  
                                      
                    mi.Indicators.Add(blankInd);

                    success = wgDB.GetAllIndicatorsForMethod(mc.MethodID);
                    if(success)
                    {
                        foreach(IndicatorContainer ic in wgDB.m_indicatorList)
                        {                           
                            IndicatorItem ii = new IndicatorItem(ic.IndicatorID, ic.MethodID,ic.Description,ic.ExcitationFilterPosition,ic.EmissionsFilterPosition,ic.SignalType,ref stList, ref exFilts, ref emFilts);

                            mi.Indicators.Add(ii);
                        }
                    }
                    


                    // load all compound plates for the method
                        // add blank Compound Plate container to hold the AddRecord
                    CompoundPlateItem blankCP = new CompoundPlateItem();
                    blankCP.CompoundPlateID = 0;
                    blankCP.MethodID = mi.MethodID;
                    blankCP.Description = "";
                    mi.CompoundPlates.Add(blankCP);

                    success = wgDB.GetAllCompoundPlatesForMethod(mc.MethodID);
                    if (success)
                    {
                        foreach (CompoundPlateContainer cpc in wgDB.m_compoundPlateList)
                        {
                            CompoundPlateItem cpi = new CompoundPlateItem();
                            cpi.CompoundPlateID = cpc.CompoundPlateID;
                            cpi.MethodID = cpc.MethodID;
                            cpi.Description = cpc.Description;

                            mi.CompoundPlates.Add(cpi);
                        }
                    }

                }
             

            }


            
        
            
            xamDataGrid.DataContext = VM;

            
          
        }

        public void RefreshSignalTypeList()
        {
            if (VM.SignalTypeList == null) VM.SignalTypeList = new ObservableCollection<SignalTypeContainer>();
            else VM.SignalTypeList.Clear();
                        
            foreach (SIGNAL_TYPE st in Enum.GetValues(typeof(SIGNAL_TYPE)))
            {
                SignalTypeContainer stc = new SignalTypeContainer();
                stc.Value = st;
                stc.Description = st.ToString();
                VM.SignalTypeList.Add(stc);
            }
        }

        public void RefreshFilterList()
        {
            // Get Excitation Filters
            bool success = wgDB.GetAllExcitationFilters();
            if (success)
            {
                if (VM.ExcitationFilters == null) VM.ExcitationFilters = new ObservableCollection<FilterContainer>();
                else VM.ExcitationFilters.Clear();

                foreach(FilterContainer filter in wgDB.m_filterList)
                {
                    FilterContainer newFilter = new FilterContainer();
                    newFilter.Description = filter.Description;
                    newFilter.FilterChanger = filter.FilterChanger;
                    newFilter.FilterID = filter.FilterID;
                    newFilter.Manufacturer = filter.Manufacturer;
                    newFilter.PartNumber = filter.PartNumber;
                    newFilter.PositionNumber = filter.PositionNumber;

                    VM.ExcitationFilters.Add(newFilter);
                }
            }


            // Get Emission Filters
            success = wgDB.GetAllEmissionFilters();
            if (success)
            {
                if (VM.EmissionsFilters == null) VM.EmissionsFilters = new ObservableCollection<FilterContainer>();
                else VM.EmissionsFilters.Clear();

                foreach (FilterContainer filter in wgDB.m_filterList)
                {
                    FilterContainer newFilter = new FilterContainer();
                    newFilter.Description = filter.Description;
                    newFilter.FilterChanger = filter.FilterChanger;
                    newFilter.FilterID = filter.FilterID;
                    newFilter.Manufacturer = filter.Manufacturer;
                    newFilter.PartNumber = filter.PartNumber;
                    newFilter.PositionNumber = filter.PositionNumber;

                    VM.EmissionsFilters.Add(newFilter);
                }
            }
        }



        
                

        private void xamDataGrid_EditModeEnded(object sender, Infragistics.Windows.DataPresenter.Events.EditModeEndedEventArgs e)
        {
            // use this method to update a record after one of the cells of the record has been edited         

            if (((string)e.Cell.Record.Tag) == "AddRecord") return;  // not updating the AddRecord here

           

            if (e.Cell.Record.DataItem.GetType() == typeof(MethodItem))
            {
                MethodItem mi = (MethodItem)e.Cell.Record.DataItem;

                if (mi.MethodID != 0)
                {
                    MethodContainer mc = new MethodContainer();

                    mc.MethodID = mi.MethodID;
                    mc.Description = mi.Description;
                    mc.BravoMethodFile = mi.BravoMethodFile;
                    mc.OwnerID = mi.OwnerID;
                    mc.IsPublic = mi.IsPublic;

                    bool success = wgDB.UpdateMethod(mc);
                }

            }
            else if (e.Cell.Record.DataItem.GetType() == typeof(IndicatorItem))
            {
                IndicatorItem ii = (IndicatorItem)e.Cell.Record.DataItem;

                if (ii.IndicatorID != 0)
                {
                    IndicatorContainer ic = new IndicatorContainer();
                    ic.Description = ii.Description;
                    ic.MethodID = ii.MethodID;
                    ic.IndicatorID = ii.IndicatorID;
                    ic.ExcitationFilterPosition = ii.ExcitationFilterPosition;
                    ic.EmissionsFilterPosition = ii.EmissionsFilterPosition;
                    ic.SignalType = ii.SignalType;
                    
                    bool succcess = wgDB.UpdateIndicator(ic);
                }
            }
            else if (e.Cell.Record.DataItem.GetType() == typeof(CompoundPlateItem))
            {
                CompoundPlateItem cpi = (CompoundPlateItem)e.Cell.Record.DataItem;

                if (cpi.CompoundPlateID != 0)
                {
                    CompoundPlateContainer cpc = new CompoundPlateContainer();
                    cpc.CompoundPlateID = cpi.CompoundPlateID;
                    cpc.MethodID = cpi.MethodID;
                    cpc.Description = cpi.Description;

                    bool success = wgDB.UpdateCompoundPlate(cpc);
                }
            }

          
        }


        private void ExcitationFilter_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) 
        {
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            IndicatorItem indItem = (IndicatorItem)record.DataItem;

            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(IndicatorItem))
            {
                IndicatorItem indicator = (IndicatorItem)xamDataGrid.ActiveDataItem;

                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(FilterContainer))
                {
                    FilterContainer filter = (FilterContainer)e.NewValue;

                    if (indicator.IndicatorID != 0 && indicator.IndicatorID == indItem.IndicatorID)  // the 2nd condition makes sure the event is for the currently active Indicator
                    {   
                        IndicatorContainer ic = new IndicatorContainer();
                        ic.Description = indicator.Description;
                        ic.EmissionsFilterPosition = indicator.EmissionsFilterPosition;
                        ic.ExcitationFilterPosition = filter.PositionNumber;
                        ic.IndicatorID = indicator.IndicatorID;
                        ic.MethodID = indicator.MethodID;
                        ic.SignalType = indicator.SignalType;

                        bool succcess = wgDB.UpdateIndicator(ic);
                    }
                }
            }
        }

        private void SignalType_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            IndicatorItem indItem = (IndicatorItem)record.DataItem;

            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(IndicatorItem))
            {
                IndicatorItem indicator = (IndicatorItem)xamDataGrid.ActiveDataItem;

                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(SignalTypeContainer))
                {
                    SignalTypeContainer st = (SignalTypeContainer)e.NewValue;

                    if (indicator.IndicatorID != 0 && indicator.IndicatorID == indItem.IndicatorID)  // the 2nd condition makes sure the event is for the currently active Indicator
                    {
                        IndicatorContainer ic = new IndicatorContainer();
                        ic.Description = indicator.Description;
                        ic.EmissionsFilterPosition = indicator.EmissionsFilterPosition;
                        ic.ExcitationFilterPosition = indicator.ExcitationFilterPosition;
                        ic.IndicatorID = indicator.IndicatorID;
                        ic.MethodID = indicator.MethodID;
                        ic.SignalType = st.Value;
                        
                        bool succcess = wgDB.UpdateIndicator(ic);
                    }
                }
            }
        }


        private void EmissionFilter_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            IndicatorItem indItem = (IndicatorItem)record.DataItem;


            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(IndicatorItem))
            {
                IndicatorItem indicator = (IndicatorItem)xamDataGrid.ActiveDataItem;

                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(FilterContainer))
                {
                    FilterContainer filter = (FilterContainer)e.NewValue;

                    if (indicator.IndicatorID != 0 && indicator.IndicatorID == indItem.IndicatorID)  // the 2nd condition makes sure the event is for the currently active Indicator
                    {
                        IndicatorContainer ic = new IndicatorContainer();
                        ic.Description = indicator.Description;
                        ic.EmissionsFilterPosition = filter.PositionNumber;
                        ic.ExcitationFilterPosition = indicator.ExcitationFilterPosition;
                        ic.IndicatorID = indicator.IndicatorID;
                        ic.MethodID = indicator.MethodID;
                        ic.SignalType = indicator.SignalType;

                        bool succcess = wgDB.UpdateIndicator(ic);
                    }
                }
            }
        }


       

        private void xamDataGrid_RecordUpdated(object sender, Infragistics.Windows.DataPresenter.Events.RecordUpdatedEventArgs e)
        {
            if (e.Record.Tag == null) return;

            if (((string)e.Record.Tag).Equals("AddRecord"))  // is this the "AddRecord"?
            {
                if (e.Record.DataItem.GetType() == typeof(MethodItem))
                {
                    DataRecord methodRecord = (DataRecord)e.Record;

                    MethodItem mi = ((MethodItem)(methodRecord.DataItem));

                    MethodContainer newMethod = new MethodContainer();
                    newMethod.Description = mi.Description;                    
                    newMethod.OwnerID = mi.OwnerID;
                    newMethod.BravoMethodFile = mi.BravoMethodFile;
                    newMethod.IsPublic = mi.IsPublic;
                  
                    bool success = wgDB.InsertMethod(ref newMethod);
                    if (success)
                    {
                        mi.MethodID = newMethod.MethodID;

                        UnMarkAddNewRecord(methodRecord);
                        
                        MethodItem miNew = new MethodItem();
                        miNew.Description = "";
                        miNew.MethodID = mi.MethodID;
                        miNew.OwnerID = mi.OwnerID;
                        miNew.IsPublic = false;
                        miNew.BravoMethodFile = "";

                        VM.Methods.Insert(0, miNew);

                        // mark the new Method as the AddRecord
                        RecordCollectionBase coll = e.Record.ParentCollection;
                        DataRecord newMethodRecord = (DataRecord)coll.ElementAt(0);
                        MarkAddNewRecord(newMethodRecord);

                        // add the AddRecord Indicator for this new method                       
                        ObservableCollection<FilterContainer> exFilts = VM.ExcitationFilters;
                        ObservableCollection<FilterContainer> emFilts = VM.EmissionsFilters;
                        ObservableCollection<SignalTypeContainer> stList = VM.SignalTypeList;
                        IndicatorItem ii = new IndicatorItem(0, mi.MethodID, "",ref stList, ref exFilts, ref emFilts);            


                        mi.Indicators.Add(ii);

                        // mark the new Indicator as the AddRecord  
                        ExpandableFieldRecord expRecord = (ExpandableFieldRecord)methodRecord.ChildRecords[0];
                        DataRecord indicatorRecord = (DataRecord)expRecord.ChildRecords[0];

                        if (indicatorRecord.DataItem.GetType() == typeof(IndicatorItem))
                        {
                            MarkAddNewRecord(indicatorRecord);
                        }


                        // add the AddRecord CompoundPlate for this new method
                        CompoundPlateItem cpi = new CompoundPlateItem();
                        cpi.CompoundPlateID = 0;
                        cpi.MethodID = mi.MethodID;
                        cpi.Description = "";

                        mi.CompoundPlates.Add(cpi);

                        // mark the new CompoundPlate as the AddRecord
                        ExpandableFieldRecord expRecord1 = (ExpandableFieldRecord)methodRecord.ChildRecords[1];
                        DataRecord compoundPlateRecord = (DataRecord)expRecord1.ChildRecords[0];

                        if (compoundPlateRecord.DataItem.GetType() == typeof(CompoundPlateItem))
                        {
                            MarkAddNewRecord(compoundPlateRecord);
                        }
                    }
                }
                else if (e.Record.DataItem.GetType() == typeof(IndicatorItem))
                {
                    IndicatorItem ii = (IndicatorItem)(e.Record.DataItem);

                    IndicatorContainer ic = new IndicatorContainer();
                    ic.Description = ii.Description;
                    ic.MethodID = ii.MethodID;
                    ic.IndicatorID = ii.IndicatorID;
                    ic.ExcitationFilterPosition = ii.ExcitationFilterPosition;
                    ic.EmissionsFilterPosition = ii.EmissionsFilterPosition;
                    ic.SignalType = ii.SignalType;

                    bool success = wgDB.InsertIndicator(ref ic);

                    if (success)
                    {
                        ii.IndicatorID = ic.IndicatorID;

                        UnMarkAddNewRecord(e.Record);

                        ObservableCollection<FilterContainer> exFilts = VM.ExcitationFilters;
                        ObservableCollection<FilterContainer> emFilts = VM.EmissionsFilters;
                        ObservableCollection<SignalTypeContainer> stList = VM.SignalTypeList;
                        IndicatorItem iiNew = new IndicatorItem(0, ic.MethodID, "",ref stList, ref exFilts, ref emFilts);           


                        MethodItem mi = (MethodItem)(((DataRecord)e.Record.ParentRecord.ParentRecord).DataItem);

                        mi.Indicators.Insert(0, iiNew);

                        DataRecord newIndicatorRecord = (DataRecord)e.Record.ParentCollection[0];
                        MarkAddNewRecord(newIndicatorRecord);
                    }
                }
                else if (e.Record.DataItem.GetType() == typeof(CompoundPlateItem))
                {
                    CompoundPlateItem cpi = (CompoundPlateItem)(e.Record.DataItem);

                    CompoundPlateContainer cpc = new CompoundPlateContainer();
                    cpc.CompoundPlateID = cpi.CompoundPlateID;
                    cpc.Description = cpi.Description;
                    cpc.MethodID = cpi.MethodID;

                    bool success = wgDB.InsertCompoundPlate(ref cpc);

                    if (success)
                    {
                        cpi.CompoundPlateID = cpc.CompoundPlateID;

                        UnMarkAddNewRecord(e.Record);

                        CompoundPlateItem cpiNew = new CompoundPlateItem();
                        cpiNew.Description = "";                        
                        cpiNew.CompoundPlateID = 0;
                        cpiNew.MethodID = cpc.MethodID;

                        MethodItem mi = (MethodItem)(((DataRecord)e.Record.ParentRecord.ParentRecord).DataItem);

                        mi.CompoundPlates.Insert(0, cpiNew);

                        DataRecord newCompoundPlateRecord = (DataRecord)e.Record.ParentCollection[0];
                        MarkAddNewRecord(newCompoundPlateRecord);
                    }
                }
                               
            }
        }




        private void xamDataGrid_RecordsDeleting(object sender, Infragistics.Windows.DataPresenter.Events.RecordsDeletingEventArgs e)
        {
            bool success;

            foreach (DataRecord record in e.Records)
            {
                if (record.DataItem.GetType() == typeof(MethodItem))
                {
                    MethodItem mi = (MethodItem)record.DataItem;
                    foreach (IndicatorItem ii in mi.Indicators)
                    {
                        success = wgDB.DeleteIndicator(ii.IndicatorID);
                        if (!success) break;                    
                    }

                    foreach (CompoundPlateItem cpi in mi.CompoundPlates)
                    {
                        success = wgDB.DeleteCompoundPlate(cpi.CompoundPlateID);
                        if (!success) break;
                    }
                 
                    success = wgDB.DeleteMethod(mi.MethodID);
                    if (!success) break;
                }
                else if (record.DataItem.GetType() == typeof(IndicatorItem))
                {
                    IndicatorItem ii = (IndicatorItem)record.DataItem;
                    success = wgDB.DeleteIndicator(ii.IndicatorID);
                    if (!success) break;
                }
                else if (record.DataItem.GetType() == typeof(CompoundPlateItem))
                {
                    CompoundPlateItem cpi = (CompoundPlateItem)record.DataItem;
                    success = wgDB.DeleteCompoundPlate(cpi.CompoundPlateID);
                    if (!success) break;
                }
            }
        }

        


        private void MarkAddNewRecord(DataRecord record)
        {
            record.IsFixed = true;
            record.Tag = "AddRecord";            
        }

        private void UnMarkAddNewRecord(DataRecord record)
        {
            record.IsFixed = false;
            record.Tag = null;          
        }


        private void xamDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            XamDataGrid xdg = xamDataGrid;

            MarkAddNewRecord((DataRecord)xamDataGrid.Records[0]);

            // mark AddRecord for each Indicator and CompoundPlate list
            foreach(DataRecord rec in xamDataGrid.Records)  // step through all methods
            {
                if (rec.HasChildren)  // if the method has children
                {
                    foreach (ExpandableFieldRecord expRecord in rec.ChildRecords)
                    {
                        // get first record and mark as "AddRecord"
                        DataRecord addRecord = (DataRecord)(expRecord.ChildRecords[0]);

                        if (addRecord.DataItem.GetType() == typeof(IndicatorItem))
                        {
                            MarkAddNewRecord(addRecord);
                        }
                        else if (addRecord.DataItem.GetType() == typeof(CompoundPlateItem))
                        {
                            MarkAddNewRecord(addRecord);
                        }
                    }                   
                 }
             }
        }






/// /////////////////////////////////////////////////////////////////////////////////////////////
/// /////////////////////////////////////////////////////////////////////////////////////////////
        





        class MethodManagerViewModel : INotifyPropertyChanged
        {            
            private ObservableCollection<MethodItem> _methods;

            private ObservableCollection<FilterContainer> _excitationFilters;
            private ObservableCollection<FilterContainer> _emissionsFilters;
            private ObservableCollection<SignalTypeContainer> _signalTypeList;

            public ObservableCollection<MethodItem> Methods
            {
                get { return _methods; }
                set
                {
                    _methods = value;
                    NotifyPropertyChanged("Methods");
                }
            }


            public ObservableCollection<FilterContainer> ExcitationFilters
            {
                get { return _excitationFilters; }
                set
                {
                    _excitationFilters = value;
                    NotifyPropertyChanged("ExcitationFilters");
                }
            }

            public ObservableCollection<FilterContainer> EmissionsFilters
            {
                get { return _emissionsFilters; }
                set
                {
                    _emissionsFilters = value;
                    NotifyPropertyChanged("EmissionsFilters");
                }
            }


            public ObservableCollection<SignalTypeContainer> SignalTypeList
            {
                get { return _signalTypeList; }
                set
                {
                    _signalTypeList = value;
                    NotifyPropertyChanged("SignalTypeList");
                }
            }



            public MethodManagerViewModel()
            {
                _methods = new ObservableCollection<MethodItem>();              
            }




            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }


        class MethodItem : INotifyPropertyChanged
        {
            private int _methodID;
            private string _description;
            private string _bravoMethodFile;
            private int _ownerID;
            private bool _isPublic;

            private ObservableCollection<IndicatorItem> _indicators;
            private ObservableCollection<CompoundPlateItem> _compoundPlates;
            
            public MethodItem()
            {
                _indicators = new ObservableCollection<IndicatorItem>();
                _compoundPlates = new ObservableCollection<CompoundPlateItem>();
            }

            public int MethodID
            { get { return _methodID; }  set { _methodID = value; NotifyPropertyChanged("MethodID"); } }

            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

            public string BravoMethodFile
            { get { return _bravoMethodFile; } set { _bravoMethodFile = value; NotifyPropertyChanged("BravoMethodFile"); } }

            public int OwnerID
            { get { return _ownerID; } set { _ownerID = value; NotifyPropertyChanged("OwnerID"); } }

            public bool IsPublic
            { get { return _isPublic; } set { _isPublic = value; NotifyPropertyChanged("IsPublic"); } }

            public ObservableCollection<IndicatorItem> Indicators
            { get { return _indicators; } set { _indicators = value; NotifyPropertyChanged("Indicators"); } }

            public ObservableCollection<CompoundPlateItem> CompoundPlates
            { get { return _compoundPlates; } set { _compoundPlates = value; NotifyPropertyChanged("CompoundPlates"); } }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }

        class SignalTypeContainer : INotifyPropertyChanged
        {
            private SIGNAL_TYPE _value;
            public SIGNAL_TYPE Value
            { get { return _value; } set { _value = value; NotifyPropertyChanged("Value"); } }

            private string _description;
            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }

        class IndicatorItem : INotifyPropertyChanged
        {
            private int _indicatorID;
            private int _methodID;
            private int _excitationFilterPosition;
            private int _emissionsFilterPosition;
            private string _description;
            private SIGNAL_TYPE _signalType;

            private ObservableCollection<FilterContainer> _excitationFilterList;
            private ObservableCollection<FilterContainer> _emissionsFilterList;
            private ObservableCollection<SignalTypeContainer> _signalTypeList;

            public IndicatorItem()
            {
                _excitationFilterList = new ObservableCollection<FilterContainer>();
                _emissionsFilterList = new ObservableCollection<FilterContainer>();
                _signalTypeList = new ObservableCollection<SignalTypeContainer>();

            }



            public IndicatorItem(int indID, int methID, string desc, int exFiltPos, int emFiltPos,SIGNAL_TYPE signalType,ref ObservableCollection<SignalTypeContainer> signalTypeList, ref ObservableCollection<FilterContainer> exFiltList, ref ObservableCollection<FilterContainer> emFiltList)
            {
                _indicatorID = indID;
                _methodID = methID;
                _description = desc;
                _excitationFilterPosition = exFiltPos;
                _emissionsFilterPosition = emFiltPos;
                _excitationFilterList = exFiltList;
                _emissionsFilterList = emFiltList;
                _signalTypeList = signalTypeList;
                _signalType = signalType;
            }


            public IndicatorItem(int indID, int methID, string desc,ref ObservableCollection<SignalTypeContainer> signalTypeList,ref ObservableCollection<FilterContainer> exFiltList, ref ObservableCollection<FilterContainer> emFiltList)
            {
                _indicatorID = indID;
                _methodID = methID;                
                _description = desc;
                
                _excitationFilterList = exFiltList; 
                _emissionsFilterList = emFiltList;
                _signalTypeList = signalTypeList;

                if (_excitationFilterList.Count() > 0)
                {
                    FilterContainer filter = _excitationFilterList.ElementAt(0);
                    _excitationFilterPosition = filter.PositionNumber;
                }

                if (_emissionsFilterList.Count() > 0)
                {
                    FilterContainer filter = _emissionsFilterList.ElementAt(0);
                    _emissionsFilterPosition = filter.PositionNumber;
                }

                if (_signalTypeList.Count() > 0)
                {
                    SignalTypeContainer st = _signalTypeList.ElementAt(0);
                    _signalType = st.Value;
                }
            }


            public int IndicatorID
            { get { return _indicatorID; } set { _indicatorID = value; NotifyPropertyChanged("IndicatorID"); } }

            public int MethodID
            { get { return _methodID; } set { _methodID = value; NotifyPropertyChanged("MethodID"); } }

            public int ExcitationFilterPosition
            { get { return _excitationFilterPosition; } set { _excitationFilterPosition = value; NotifyPropertyChanged("ExcitationFilterPosition"); } }

            public int EmissionsFilterPosition
            { get { return _emissionsFilterPosition; } set { _emissionsFilterPosition = value; NotifyPropertyChanged("EmissionsFilterPosition"); } }

            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

            public SIGNAL_TYPE SignalType
            { get { return _signalType; } set { _signalType = value; NotifyPropertyChanged("SignalType"); } }


            public void LoadSignalTypes()
            {
                SignalTypeList = new ObservableCollection<SignalTypeContainer>();
                foreach(SIGNAL_TYPE st in Enum.GetValues(typeof(SIGNAL_TYPE)))
                {
                    SignalTypeContainer stc = new SignalTypeContainer();
                    stc.Value = st;
                    stc.Description = st.ToString();
                    SignalTypeList.Add(stc);
                }
            }

            public void LoadFilters()
            {
                EmissionsFilterList = new ObservableCollection<FilterContainer>();
                ExcitationFilterList = new ObservableCollection<FilterContainer>();

                WaveguideDB wgDB = new WaveguideDB();

                bool success = wgDB.GetAllFilters();

                if (success)
                {
                    foreach (FilterContainer filter in wgDB.m_filterList)
                    {
                        if (filter.FilterChanger == (int)FilterChangerEnum.Emission)
                        {
                            EmissionsFilterList.Add(filter);
                        }
                        else if (filter.FilterChanger == (int)FilterChangerEnum.Excitation)
                        {
                            ExcitationFilterList.Add(filter);
                        }
                    }
                }

                LoadSignalTypes();
            }


            public ObservableCollection<FilterContainer> ExcitationFilterList
            {
                get { return _excitationFilterList; }
                set { _excitationFilterList = value; NotifyPropertyChanged("ExcitationFilterList"); }
            }


            public ObservableCollection<FilterContainer> EmissionsFilterList
            {
                get { return _emissionsFilterList; }
                set { _emissionsFilterList = value; NotifyPropertyChanged("EmissionsFilterList"); }
            }

            public ObservableCollection<SignalTypeContainer> SignalTypeList
            {
                get { return _signalTypeList; }
                set { _signalTypeList = value; NotifyPropertyChanged("SignalTypeList"); }
            }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }







        class CompoundPlateItem : INotifyPropertyChanged
        {
            private int _compoundPlateID;
            private int _methodID;            
            private string _description;

            public CompoundPlateItem()
            {            
            }

            public int CompoundPlateID
            { get { return _compoundPlateID; } set { _compoundPlateID = value; NotifyPropertyChanged("CompoundPlateID"); } }

            public int MethodID
            { get { return _methodID; } set { _methodID = value; NotifyPropertyChanged("MethodID"); } }

            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }


        




    }
}
