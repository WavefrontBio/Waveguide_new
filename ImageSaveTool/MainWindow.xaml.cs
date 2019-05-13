using System;
using System.Collections.Generic;
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
using System.IO.Compression;
using System.IO;
using System.Windows.Forms;
using XmlSettings;
using System.ComponentModel;
using Waveguide;

namespace ImageSaveTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindow_ViewModel m_vm;
        XmlSettings.Settings m_settings;
    

        public MainWindow()
        {
            GetConfigData();
            InitializeComponent();
            m_vm = new MainWindow_ViewModel();
            DataContext = m_vm;
        }


        public void GetConfigData()
        {
            m_settings = new Settings("settings.xml");

            string serverName = m_settings.GetValue("MAIN", "DBServerName");
            string dbName =     m_settings.GetValue("MAIN", "DBName");
            string Username =   m_settings.GetValue("MAIN", "DBUsername");
            string Password =   m_settings.GetValue("MAIN", "DBPassword");

            GlobalVars.Instance.DatabaseConnectionString = "Data Source=" + serverName +
                                               ";Initial Catalog=" + dbName +
                                               ";User ID=" + Username +
                                               ";Password=" + Password;

            GlobalVars.Instance.MaxPixelValue = (ushort)Convert.ToUInt16(m_settings.GetValue("MAIN", "MaxPixelValue"));
        }

        public void SetConfigData()
        {
            // used to create a new config.xml file
            if (m_settings == null) m_settings = new Settings("config.xml");

            m_settings.SetValue("MAIN", "DBServerName", "HTS-WAVEFRONT\\SQLEXPRESS");
            m_settings.SetValue("MAIN", "DBName", "WaveguideDB");
            m_settings.SetValue("MAIN", "DBUsername", "sa");
            m_settings.SetValue("MAIN", "DBPassword", "wavefront");

            m_settings.SetValue("MAIN", "MaxPixelValue", (65535).ToString());
        }


        public bool ExtractZipFile(string filename, out ushort[] dataArray)
        {
            bool success = true;
            dataArray = null;
            if (File.Exists(filename))
            {
                try
                {
                    dataArray = Zip.Decompress_File(filename);
                }
                catch(Exception ex)
                {
                    success = false;
                    dataArray = null;
                    string errMsg = ex.Message;
                }
            }
            else
            {
                success = false;               
            }

            return success;
        }


        public ushort[] SynthesizeImage(int rows, int cols)
        {
            ushort count = 0;
            ushort[] data = new ushort[rows * cols];
            for (int i = 0; i < rows * cols; i++)
            {
                data[i] = count;
                count++;
            }

            return data;
        }

        private void QuitPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SavePB_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "JPG file (*.jpg)|*.jpg";
            DialogResult result = dlg.ShowDialog();
            
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                string filename = dlg.FileName;

                if (!filename.EndsWith(".jpg")) filename += ".jpg";

                bool success = MyImageFileViewer.SaveImage(filename);

                if(!success)
                {
                    System.Windows.MessageBox.Show("Failed to save file: " + filename + "\n" + MyImageFileViewer.GetLastError(), 
                        "File Save Error",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }            
        }



    
        
    }






    public class MainWindow_ViewModel : INotifyPropertyChanged
    {       

   


        public MainWindow_ViewModel()
        {
              
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }
    }
}
