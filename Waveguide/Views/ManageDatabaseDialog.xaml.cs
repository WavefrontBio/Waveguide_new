using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ManageDatabaseDialog.xaml
    /// </summary>
    public partial class ManageDatabaseDialog : Window
    {

        WaveguideDB m_db;
        ManageDatabase_ViewModel m_vm;

        public ManageDatabaseDialog(WaveguideDB db)
        {
            InitializeComponent();
            m_db = db;

            m_vm = new ManageDatabase_ViewModel();
            DataContext = m_vm;
        }

        private void DonePB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DeleteExperimentsBeforeDateTimePB_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ExperimentContainer> experiments;
            bool success = m_db.GetAllExperimentsBeforeDateTime(m_vm.datetime1, out experiments);

            MessageBoxResult result = MessageBox.Show("Delete " + experiments.Count.ToString() + " Experiments?", "Delete Experiments", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if(result == MessageBoxResult.Yes)
            {
                success = m_db.DeleteAllExperimentsBefore(m_vm.datetime1);

                if(!success)
                {
                    string errMsg = m_db.GetLastErrorMsg();
                    MessageBox.Show("Failed to Delete Experiments: " + errMsg);
                }
            }
        }
    }


    public class ManageDatabase_ViewModel : INotifyPropertyChanged
    {


        private DateTime _datetime1;
        public DateTime datetime1
        { 
            get { return _datetime1; }
            set { _datetime1 = value; NotifyPropertyChanged("datetime1"); }
        }


        public ManageDatabase_ViewModel()
        {
            datetime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0,0);
            datetime1 -= TimeSpan.FromDays(1);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


}
