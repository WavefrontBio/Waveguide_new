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
        ObservableCollection<ProjectContainer> projectList;

        WaveguideDB wgDB;

  
        public static readonly RoutedCommand BrowseButtonCommand = new RoutedCommand();
        public static readonly RoutedCommand IsPublicCheckBoxCommand = new RoutedCommand();
        public static readonly RoutedCommand IsAutoCheckBoxCommand = new RoutedCommand();


        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private ObservableCollection<MethodItem> m_methods;

        private ObservableCollection<FilterContainer> m_excitationFilters;
        private ObservableCollection<FilterContainer> m_emissionsFilters;
        private ObservableCollection<SignalTypeContainer> m_signalTypeList;
        private ObservableCollection<BarcodeResetContainer> m_barcodeResetTypeList;

        private ObservableCollection<ProjectContainer> m_projectList;


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////


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


        void IsAutoCheckBoxCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // The ShowInChartCommand command can execute if the parameter references a Customer.
            e.CanExecute = e.Parameter is MethodItem;
        }

        void IsAutoCheckBoxCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var method = e.Parameter as MethodItem;
            if (method != null)
                this.SetIsAutoOnMethod(method, method.IsAuto);
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
            dlg.InitialDirectory = GlobalVars.Instance.VWorksProtocolFileDirectory;
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
                mc.IsAuto = method.IsAuto;
                mc.MethodID = method.MethodID;
                mc.OwnerID = method.OwnerID;
                mc.ProjectID = method.ProjectID;
                mc.ImagePlateBarcodeReset = method.ImagePlateBarcodeReset;

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
            mc.ProjectID = method.ProjectID;
            mc.IsAuto = method.IsAuto;
            mc.ImagePlateBarcodeReset = method.ImagePlateBarcodeReset;

            wgDB.UpdateMethod(mc);
        }


        void SetIsAutoOnMethod(MethodItem method, bool isAuto)
        {
            method.IsAuto = isAuto;

            if (wgDB == null) wgDB = new WaveguideDB();

            MethodContainer mc = new MethodContainer();
            mc.Description = method.Description;
            mc.BravoMethodFile = method.BravoMethodFile;
            mc.IsPublic = method.IsPublic;
            mc.MethodID = method.MethodID;
            mc.OwnerID = method.OwnerID;
            mc.ProjectID = method.ProjectID;
            mc.IsAuto = method.IsAuto;
            mc.ImagePlateBarcodeReset = method.ImagePlateBarcodeReset;

            wgDB.UpdateMethod(mc);
        }






        public MethodManager()
        {
            InitializeComponent();

            wgDB = new WaveguideDB();

            m_methods = new ObservableCollection<MethodItem>();
            xamDataGrid.DataSource = m_methods;
            LoadProjectList();

            m_barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
            foreach (PLATE_ID_RESET_BEHAVIOR st in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
            {
                BarcodeResetContainer brc = new BarcodeResetContainer();
                brc.Description = st.ToString();
                brc.Value = st;
                m_barcodeResetTypeList.Add(brc);
            }


            RefreshSignalTypeList();
            RefreshFilterList();
            RefreshProjectList();
            RefreshBarcodeResetTypeList();
                     
             //Initialize data in the XamDataGrid - NOTE: A blank record is added FIRST, this is key to this approach for the XamDataGrid
            MethodItem blank = new MethodItem(0,"","",GlobalVars.Instance.UserID,m_projectList.Count > 0 ? m_projectList.ElementAt(0).ProjectID:0,false,false,PLATE_ID_RESET_BEHAVIOR.CONSTANT, ref projectList);
            
            m_methods.Add(blank);
            
            // load all methods for user
            bool success = wgDB.GetAllMethodsForUser(GlobalVars.Instance.UserID);
            if (success)
            {                
                foreach(MethodContainer mc in wgDB.m_methodList)
                {
                    MethodItem mi = new MethodItem(mc,ref projectList);
                    
                    m_methods.Add(mi);

                    // load all indicators for the method
                         // add blank Indicator container to hold the AddRecord
                    ObservableCollection<FilterContainer> exFilts = m_excitationFilters;
                    ObservableCollection<FilterContainer> emFilts = m_emissionsFilters;
                    ObservableCollection<SignalTypeContainer> stList = m_signalTypeList;
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
                    ObservableCollection<BarcodeResetContainer> brcList = m_barcodeResetTypeList;
                    CompoundPlateItem blankCP = new CompoundPlateItem(0,mi.MethodID,"",PLATE_ID_RESET_BEHAVIOR.CONSTANT,ref brcList);
                    
                    mi.CompoundPlates.Add(blankCP);

                    success = wgDB.GetAllCompoundPlatesForMethod(mc.MethodID);
                    if (success)
                    {
                        foreach (CompoundPlateContainer cpc in wgDB.m_compoundPlateList)
                        {
                            ObservableCollection<BarcodeResetContainer> brList = m_barcodeResetTypeList;
                            CompoundPlateItem cpi = new CompoundPlateItem(cpc.CompoundPlateID, cpc.MethodID, cpc.Description, cpc.BarcodeReset, ref brcList);
                            
                            mi.CompoundPlates.Add(cpi);
                        }
                    }

                }
             

            }
          
        }


        public void RefreshBarcodeResetTypeList()
        {
            if (m_barcodeResetTypeList == null) m_barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
            else m_barcodeResetTypeList.Clear();

            foreach (PLATE_ID_RESET_BEHAVIOR st in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
            {
                BarcodeResetContainer brc = new BarcodeResetContainer();
                brc.Description = st.ToString();
                brc.Value = st;
                m_barcodeResetTypeList.Add(brc);
            }
        }


        public void RefreshSignalTypeList()
        {
            if (m_signalTypeList == null) m_signalTypeList = new ObservableCollection<SignalTypeContainer>();
            else m_signalTypeList.Clear();
                        
            foreach (SIGNAL_TYPE st in Enum.GetValues(typeof(SIGNAL_TYPE)))
            {
                SignalTypeContainer stc = new SignalTypeContainer();
                stc.Value = st;
                stc.Description = st.ToString();
                m_signalTypeList.Add(stc);
            }
        }

        public void RefreshFilterList()
        {
            // Get Excitation Filters
            bool success = wgDB.GetAllExcitationFilters();
            if (success)
            {
                if (m_excitationFilters == null) m_excitationFilters = new ObservableCollection<FilterContainer>();
                else m_excitationFilters.Clear();

                foreach(FilterContainer filter in wgDB.m_filterList)
                {
                    FilterContainer newFilter = new FilterContainer();
                    newFilter.Description = filter.Description;
                    newFilter.FilterChanger = filter.FilterChanger;
                    newFilter.FilterID = filter.FilterID;
                    newFilter.Manufacturer = filter.Manufacturer;
                    newFilter.PartNumber = filter.PartNumber;
                    newFilter.PositionNumber = filter.PositionNumber;

                    m_excitationFilters.Add(newFilter);
                }
            }


            // Get Emission Filters
            success = wgDB.GetAllEmissionFilters();
            if (success)
            {
                if (m_emissionsFilters == null) m_emissionsFilters = new ObservableCollection<FilterContainer>();
                else m_emissionsFilters.Clear();

                foreach (FilterContainer filter in wgDB.m_filterList)
                {
                    FilterContainer newFilter = new FilterContainer();
                    newFilter.Description = filter.Description;
                    newFilter.FilterChanger = filter.FilterChanger;
                    newFilter.FilterID = filter.FilterID;
                    newFilter.Manufacturer = filter.Manufacturer;
                    newFilter.PartNumber = filter.PartNumber;
                    newFilter.PositionNumber = filter.PositionNumber;

                    m_emissionsFilters.Add(newFilter);
                }
            }
        }



       public void RefreshProjectList()
        {
            LoadProjectList();

            projectList = new ObservableCollection<ProjectContainer>();

            foreach(ProjectContainer pc in m_projectList)
            {
                projectList.Add(pc);
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
                    mc.IsAuto = mi.IsAuto;
                    mc.ProjectID = mi.ProjectID;
                    mc.ImagePlateBarcodeReset = mi.ImagePlateBarcodeReset;

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
                    cpc.BarcodeReset = cpi.BarcodeReset;

                    bool success = wgDB.UpdateCompoundPlate(cpc);
                }
            }

          
        }



        private void Project_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            MethodItem m = (MethodItem)record.DataItem;

            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(MethodItem))
            {
                MethodItem mi = (MethodItem)xamDataGrid.ActiveDataItem;
                
                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(ProjectContainer))
                {
                    ProjectContainer proj = (ProjectContainer)e.NewValue;

                    if (mi.MethodID != 0 && mi.MethodID == m.MethodID)  // the 2nd condition makes sure the event is for the currently active Method
                    {
                        MethodContainer mc = new MethodContainer();
                        mc.BravoMethodFile = mi.BravoMethodFile;
                        mc.Description = mi.Description;
                        mc.IsPublic = mi.IsPublic;
                        mc.IsAuto = mi.IsAuto;
                        mc.MethodID = mi.MethodID;
                        mc.OwnerID = mi.OwnerID;
                        mc.ProjectID = proj.ProjectID;
                        mc.ImagePlateBarcodeReset = mi.ImagePlateBarcodeReset;

                        bool success = wgDB.UpdateMethod(mc);
                    }
                }
            }
        }



        private void BarcodeReset_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            
            MethodItem m = (MethodItem)record.DataItem;

            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(MethodItem))
            {
                MethodItem mi = (MethodItem)xamDataGrid.ActiveDataItem;

                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(BarcodeResetContainer)) 
                {
                    BarcodeResetContainer brc = (BarcodeResetContainer)e.NewValue;

                    if (mi.MethodID != 0 && mi.MethodID == m.MethodID)  // the 2nd condition makes sure the event is for the currently active Method
                    {
                        MethodContainer mc = new MethodContainer();
                        mc.BravoMethodFile = mi.BravoMethodFile;
                        mc.Description = mi.Description;
                        mc.IsPublic = mi.IsPublic;
                        mc.IsAuto = mi.IsAuto;
                        mc.MethodID = mi.MethodID;
                        mc.OwnerID = mi.OwnerID;
                        mc.ProjectID = mi.ProjectID;
                        mc.ImagePlateBarcodeReset = brc.Value;
                        mi.ImagePlateBarcodeReset = brc.Value;  

                        bool success = wgDB.UpdateMethod(mc);
                    }
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




        private void BarcodeResetType_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {          
            XamComboEditor xce = (XamComboEditor)sender;
            DataRecord record = (DataRecord)xce.DataContext;
            if (record == null) return;
            
            CompoundPlateItem cpItem = (CompoundPlateItem)record.DataItem;

            if (xamDataGrid.ActiveDataItem == null) return;

            if (xamDataGrid.ActiveDataItem.GetType() == typeof(CompoundPlateItem))
            {
                CompoundPlateItem compoundPlate = (CompoundPlateItem)xamDataGrid.ActiveDataItem;

                if (e.NewValue == null) return;

                if (e.NewValue.GetType() == typeof(BarcodeResetContainer))
                {
                    BarcodeResetContainer st = (BarcodeResetContainer)e.NewValue;

                    if(compoundPlate.CompoundPlateID != 0 && compoundPlate.CompoundPlateID == cpItem.CompoundPlateID) // the 2nd condition makes sure the event is for the currently active Compound Plate
                    {
                        CompoundPlateContainer cpc = new CompoundPlateContainer();
                        cpc.BarcodeReset = compoundPlate.BarcodeReset;
                        cpc.CompoundPlateID = compoundPlate.CompoundPlateID;
                        cpc.Description = compoundPlate.Description;
                        cpc.MethodID = compoundPlate.MethodID;
                       
                        bool success = wgDB.UpdateCompoundPlate(cpc);
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
                    newMethod.IsAuto = mi.IsAuto;
                    newMethod.ProjectID = mi.ProjectID;
                    newMethod.ImagePlateBarcodeReset = mi.ImagePlateBarcodeReset;
                  
                    bool success = wgDB.InsertMethod(ref newMethod);
                    if (success)
                    {
                        mi.MethodID = newMethod.MethodID;

                        UnMarkAddNewRecord(methodRecord);
                        
                        MethodItem miNew = new MethodItem(mi.MethodID,"","",mi.OwnerID,mi.ProjectID,false,false,PLATE_ID_RESET_BEHAVIOR.CONSTANT, ref projectList);
                        
                        m_methods.Insert(0, miNew);

                        // mark the new Method as the AddRecord
                        RecordCollectionBase coll = e.Record.ParentCollection;
                        DataRecord newMethodRecord = (DataRecord)coll.ElementAt(0);
                        MarkAddNewRecord(newMethodRecord);

                        // add the AddRecord Indicator for this new method                       
                        ObservableCollection<FilterContainer> exFilts = m_excitationFilters;
                        ObservableCollection<FilterContainer> emFilts = m_emissionsFilters;
                        ObservableCollection<SignalTypeContainer> stList = m_signalTypeList;
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
                        ObservableCollection<BarcodeResetContainer> brcList = m_barcodeResetTypeList;
                        CompoundPlateItem cpi = new CompoundPlateItem(0, mi.MethodID, "", PLATE_ID_RESET_BEHAVIOR.CONSTANT, ref brcList);
                      
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

                        ObservableCollection<FilterContainer> exFilts = m_excitationFilters;
                        ObservableCollection<FilterContainer> emFilts = m_emissionsFilters;
                        ObservableCollection<SignalTypeContainer> stList = m_signalTypeList;
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
                    cpc.BarcodeReset = cpi.BarcodeReset;

                    bool success = wgDB.InsertCompoundPlate(ref cpc);

                    if (success)
                    {
                        cpi.CompoundPlateID = cpc.CompoundPlateID;

                        UnMarkAddNewRecord(e.Record);

                        ObservableCollection<BarcodeResetContainer> brcList = m_barcodeResetTypeList;
                        CompoundPlateItem cpiNew = new CompoundPlateItem(0, cpc.MethodID, "", PLATE_ID_RESET_BEHAVIOR.CONSTANT, ref brcList);


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




        public void LoadProjectList()
        {
            m_projectList = new ObservableCollection<ProjectContainer>();

            WaveguideDB wgDB = new WaveguideDB();

            ObservableCollection<ProjectContainer> projList;
            bool success = wgDB.GetAllProjectsForUser(GlobalVars.Instance.UserID, out projList);

            if (success)
            {
                foreach (ProjectContainer project in projList)
                {
                    m_projectList.Add(project);
                }
            }
        }







        /// /////////////////////////////////////////////////////////////////////////////////////////////
        /// /////////////////////////////////////////////////////////////////////////////////////////////






        //class MethodManagerViewModel : INotifyPropertyChanged
        //{            
        //    private ObservableCollection<MethodItem> _methods;

        //    private ObservableCollection<FilterContainer> _excitationFilters;
        //    private ObservableCollection<FilterContainer> _emissionsFilters;
        //    private ObservableCollection<SignalTypeContainer> _signalTypeList;
        //    private ObservableCollection<BarcodeResetContainer> _barcodeResetTypeList;

        //    private ObservableCollection<ProjectContainer> _projectList;            

        //    public ObservableCollection<MethodItem> Methods
        //    {
        //        get { return _methods; }
        //        set
        //        {
        //            _methods = value;
        //            NotifyPropertyChanged("Methods");
        //        }
        //    }
                        

        //    public ObservableCollection<FilterContainer> ExcitationFilters
        //    {
        //        get { return _excitationFilters; }
        //        set
        //        {
        //            _excitationFilters = value;
        //            NotifyPropertyChanged("ExcitationFilters");
        //        }
        //    }

        //    public ObservableCollection<FilterContainer> EmissionsFilters
        //    {
        //        get { return _emissionsFilters; }
        //        set
        //        {
        //            _emissionsFilters = value;
        //            NotifyPropertyChanged("EmissionsFilters");
        //        }
        //    }


        //    public ObservableCollection<SignalTypeContainer> SignalTypeList
        //    {
        //        get { return _signalTypeList; }
        //        set
        //        {
        //            _signalTypeList = value;
        //            NotifyPropertyChanged("SignalTypeList");
        //        }
        //    }


           

        //    public ObservableCollection<ProjectContainer> ProjectList
        //    {
        //        get { return _projectList; }
        //        set
        //        {
        //            _projectList = value;
        //            NotifyPropertyChanged("ProjectList");
        //        }
        //    }

        //    public ObservableCollection<BarcodeResetContainer> BarcodeResetTypeList
        //    {
        //        get { return _barcodeResetTypeList; }
        //        set
        //        {
        //            _barcodeResetTypeList = value;
        //            NotifyPropertyChanged("BarcodeResetTypeList");
        //        }
        //    }


        //    public void LoadProjectList()
        //    {
        //        ProjectList = new ObservableCollection<ProjectContainer>();

        //        WaveguideDB wgDB = new WaveguideDB();

        //        ObservableCollection<ProjectContainer> projList;
        //        bool success = wgDB.GetAllProjectsForUser(GlobalVars.Instance.UserID, out projList);

        //        if (success)
        //        {
        //            foreach (ProjectContainer project in projList)
        //            {
        //                ProjectList.Add(project);
        //            }
        //        }
        //    }
            


        //    public MethodManagerViewModel()
        //    {
        //        _methods = new ObservableCollection<MethodItem>();
        //        LoadProjectList();

        //        _barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
        //        foreach (PLATE_ID_RESET_BEHAVIOR st in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
        //        {
        //            BarcodeResetContainer brc = new BarcodeResetContainer();
        //            brc.Description = st.ToString();
        //            brc.Value = st;
        //            _barcodeResetTypeList.Add(brc);
        //        }
        //    }


           


        //    public event PropertyChangedEventHandler PropertyChanged;
        //    private void NotifyPropertyChanged(String info)
        //    {
        //        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        //    }
        //}




        class MethodItem : INotifyPropertyChanged
        {
            private int _methodID;
            private string _description;
            private string _bravoMethodFile;
            private int _ownerID;
            private int _projectID;
            private bool _isPublic;
            private bool _isAuto;
            private PLATE_ID_RESET_BEHAVIOR _imagePlateBarcodeReset;

            private ObservableCollection<IndicatorItem> _indicators;
            private ObservableCollection<CompoundPlateItem> _compoundPlates;
            private ObservableCollection<ProjectContainer> _projectList;
            private ObservableCollection<BarcodeResetContainer> _barcodeResetTypeList;

            public MethodItem(int methodID, string description, string bravoMethodFile, int ownerID, int projectID, bool isPublic, bool isAuto, PLATE_ID_RESET_BEHAVIOR imagePlateBarcodeReset,
                ref ObservableCollection<ProjectContainer> projectList)
            {
                _methodID = methodID;
                _description = description;
                _bravoMethodFile = bravoMethodFile;
                _ownerID = ownerID;
                _projectID = projectID;
                _isPublic = isPublic;
                _isAuto = isAuto;
                _imagePlateBarcodeReset = imagePlateBarcodeReset;

                _indicators = new ObservableCollection<IndicatorItem>();
                _compoundPlates = new ObservableCollection<CompoundPlateItem>();
                _projectList = projectList;

                _barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
                foreach (PLATE_ID_RESET_BEHAVIOR st in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
                {
                    BarcodeResetContainer brc = new BarcodeResetContainer();
                    brc.Description = st.ToString();
                    brc.Value = st;
                    _barcodeResetTypeList.Add(brc);
                }

            }

            public MethodItem(MethodContainer mc, ref ObservableCollection<ProjectContainer> projectList)
            {
                _methodID = mc.MethodID;
                _description = mc.Description;
                _bravoMethodFile = mc.BravoMethodFile;
                _ownerID = mc.OwnerID;
                _projectID = mc.ProjectID;
                _isPublic = mc.IsPublic;
                _isAuto = mc.IsAuto;
                _imagePlateBarcodeReset = mc.ImagePlateBarcodeReset;

                _indicators = new ObservableCollection<IndicatorItem>();
                _compoundPlates = new ObservableCollection<CompoundPlateItem>();
                _projectList = projectList;

                _barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
                foreach (PLATE_ID_RESET_BEHAVIOR st in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
                {
                    BarcodeResetContainer brc = new BarcodeResetContainer();
                    brc.Description = st.ToString();
                    brc.Value = st;
                    _barcodeResetTypeList.Add(brc);
                }
            }


            public int MethodID
            { get { return _methodID; }  set { _methodID = value; NotifyPropertyChanged("MethodID"); } }

            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

            public string BravoMethodFile
            { get { return _bravoMethodFile; } set { _bravoMethodFile = value; NotifyPropertyChanged("BravoMethodFile"); } }

            public int OwnerID
            { get { return _ownerID; } set { _ownerID = value; NotifyPropertyChanged("OwnerID"); } }

            public int ProjectID
            { get { return _projectID; } 
                set { 
                    _projectID = value; 
                    NotifyPropertyChanged("ProjectID"); 
                } 
            }

            public bool IsPublic
            { get { return _isPublic; } set { _isPublic = value; NotifyPropertyChanged("IsPublic"); } }

            public bool IsAuto
            { get { return _isAuto; } set { _isAuto = value; NotifyPropertyChanged("IsAuto"); } }

            public PLATE_ID_RESET_BEHAVIOR ImagePlateBarcodeReset
            { get { return _imagePlateBarcodeReset; } set { _imagePlateBarcodeReset = value; NotifyPropertyChanged("ImagePlateBarcodeReset"); } }



            public ObservableCollection<IndicatorItem> Indicators
            { get { return _indicators; } set { _indicators = value; NotifyPropertyChanged("Indicators"); } }

            public ObservableCollection<CompoundPlateItem> CompoundPlates
            { get { return _compoundPlates; } set { _compoundPlates = value; NotifyPropertyChanged("CompoundPlates"); } }

            public ObservableCollection<ProjectContainer> ProjectList
            { get { return _projectList; } set { _projectList = value; NotifyPropertyChanged("ProjectList"); } }

            public ObservableCollection<BarcodeResetContainer> BarcodeResetTypeList
            { get { return _barcodeResetTypeList; } set { _barcodeResetTypeList = value; NotifyPropertyChanged("BarcodeResetTypeList"); } }

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


        class BarcodeResetContainer: INotifyPropertyChanged
        {
            private PLATE_ID_RESET_BEHAVIOR _value;
             public PLATE_ID_RESET_BEHAVIOR Value
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
            private PLATE_ID_RESET_BEHAVIOR _barcodeReset;
            private ObservableCollection<BarcodeResetContainer> _barcodeResetTypeList;

         

            public CompoundPlateItem()
            {
                _barcodeReset = PLATE_ID_RESET_BEHAVIOR.CONSTANT;
                _barcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
            }


            public CompoundPlateItem(int cpID, int methodID, string description, PLATE_ID_RESET_BEHAVIOR bcReset, ref ObservableCollection<BarcodeResetContainer> barcodeResetTypeList)
            {
                _compoundPlateID = cpID;
                _methodID = methodID;
                _description = description;
                _barcodeReset = bcReset;
                _barcodeResetTypeList = barcodeResetTypeList;
            }

        
            public CompoundPlateItem(int cpID, int methodID, string description, ref ObservableCollection<BarcodeResetContainer> barcodeResetTypeList)
            {
                 _compoundPlateID = cpID;
                _methodID = methodID;
                _description = description;
                _barcodeResetTypeList = barcodeResetTypeList;

                _barcodeReset = PLATE_ID_RESET_BEHAVIOR.CONSTANT;                
            }

            public void LoadBarcodeResetTypes()
            {
                BarcodeResetTypeList = new ObservableCollection<BarcodeResetContainer>();
                foreach (PLATE_ID_RESET_BEHAVIOR br in Enum.GetValues(typeof(PLATE_ID_RESET_BEHAVIOR)))
                {
                    BarcodeResetContainer brc = new BarcodeResetContainer();
                    brc.Value = br;
                    brc.Description = br.ToString();
                    BarcodeResetTypeList.Add(brc);
                }
            }



            public int CompoundPlateID
            { get { return _compoundPlateID; } set { _compoundPlateID = value; NotifyPropertyChanged("CompoundPlateID"); } }

            public int MethodID
            { get { return _methodID; } set { _methodID = value; NotifyPropertyChanged("MethodID"); } }

            public string Description
            { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

            public PLATE_ID_RESET_BEHAVIOR BarcodeReset
            { get { return _barcodeReset; } set { _barcodeReset = value; NotifyPropertyChanged("BarcodeReset"); } }


            public ObservableCollection<BarcodeResetContainer> BarcodeResetTypeList
            {
                get { return _barcodeResetTypeList; }
                set { _barcodeResetTypeList = value; NotifyPropertyChanged("BarcodeResetTypeList"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }


        




    }
}
