using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for SaveImageDialog.xaml
    /// </summary>
    public partial class SaveImageDialog : Window
    {
        WriteableBitmap m_bitmap;
        ushort[] m_imageData;
        int m_width;
        int m_height;
        VM_SaveImageDialog VM;

        public SaveImageDialog(WriteableBitmap bitmap, ushort[] imageData, int width, int height, bool AllowRefImageSave)
        {
            VM = new VM_SaveImageDialog();
            m_bitmap = bitmap;
            m_imageData = imageData;
            m_width = width;
            m_height = height;

            InitializeComponent();            

            DataContext = VM;

            if(!AllowRefImageSave)
            {
                SaveRefImageRB.IsChecked = false;
                SaveRefImageRB.IsEnabled = false;
                SaveImageInFileRB.IsChecked = true;
                BinningWarningMessage.Content = "Reference Images must be 1x1 Binning";
            }
        }

        private void SaveRefImageRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.SaveAsFile = false;
            VM.SaveAsReference = true;
        }

        private void SaveImageInFileRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.SaveAsFile = true;
            VM.SaveAsReference = false;
        }

        private void BrowseLocationPB_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            // Show the FolderBrowserDialog.
            dlg.SelectedPath = GlobalVars.ImageFileSaveLocation;
            DialogResult result = dlg.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                GlobalVars.ImageFileSaveLocation = dlg.SelectedPath;
                VM.Location = dlg.SelectedPath;
            }
            
        }

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SavePB_Click(object sender, RoutedEventArgs e)
        {

            if(VM.SaveAsFile)
            {
                if (VM.Filename.Length == 0)
                {
                    System.Windows.MessageBox.Show("Filename cannot be empty.",
                        "Enter a Filename", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string path = VM.Location + "\\" + VM.Filename + ".png";

                if (File.Exists(path))
                {
                    MessageBoxResult result =
                        System.Windows.MessageBox.Show("File already exists.  Do you want to OverWrite it?",
                        "File Already Exists",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.No) return;
                }

                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(m_bitmap.Clone()));
                    encoder.Save(stream);
                    stream.Close();
                }

            }
            else
            {

                if(VM.Description.Length == 0)
                {
                    System.Windows.MessageBox.Show("Description cannot be empty for the  Reference Image.",
                        "Enter a Description",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }

                // save as reference image in database
                WaveguideDB wgDB = new WaveguideDB();

                ReferenceImageContainer refCont = new ReferenceImageContainer();

                refCont.CompressionAlgorithm = GlobalVars.CompressionAlgorithm;
                refCont.Depth = 2;
                refCont.Height = m_height;
                refCont.Width = m_width;
                refCont.ImageData = m_imageData;
                refCont.MaxPixelValue = GlobalVars.MaxPixelValue;
                refCont.NumBytes = m_imageData.Length * 2;
                refCont.TimeStamp = DateTime.Now;
                refCont.Description = VM.Description;
                refCont.Type = VM.Type;

                bool alreadyExists;
                
                bool success = wgDB.ReferenceImageTypeAlreadyExists(VM.Type, out alreadyExists);
                if(success)
                {
                    if (!alreadyExists)
                    {
                        success = wgDB.InsertReferenceImage(ref refCont);

                        if (!success)
                        {
                            System.Windows.MessageBox.Show("Failed to insert Reference Image: " + wgDB.GetLastErrorMsg(),
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        string sMessageBoxText = "This reference already exists for: " + VM.Type.ToString() +
                            ".  Do you want to replace it?";
                        string sCaption = "Reference Image Already Exists For This Type";

                        MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
                        MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

                        MessageBoxResult rsltMessageBox = System.Windows.MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

                        switch (rsltMessageBox)
                        {
                            case MessageBoxResult.Yes:
                                // delete the existing reference image
                                success = wgDB.ClearReferenceImageType(VM.Type);
                                if(success)
                                {
                                    // insert the new reference image
                                    success = wgDB.InsertReferenceImage(ref refCont);

                                    if (!success)
                                    {
                                        System.Windows.MessageBox.Show("Failed to insert Reference Image: " + wgDB.GetLastErrorMsg(),
                                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show("Failed to clear Reference Image: " + wgDB.GetLastErrorMsg(),
                                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                
                                break;

                            case MessageBoxResult.No:
                                // do nothing
                                return;
                               
                        }
                    }
                }


                
            }

            Close();
        }
    }

    public class VM_SaveImageDialog : INotifyPropertyChanged
    {
        private bool _saveAsFile;
        private bool _saveAsReference;

        private string _description;
        private string _location;
        private string _filename;
        private REFERENCE_IMAGE_TYPE _type;
        
        public bool SaveAsFile
        {
            get { return _saveAsFile; }
            set
            {
                _saveAsFile = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("SaveAsFile"));
            }
        }

        public bool SaveAsReference
        {
            get { return _saveAsReference; }
            set
            {
                _saveAsReference = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("SaveAsReference"));
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Description"));
            }
        }

        public string Location
        {
            get { return _location; }
            set
            {
                _location = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Location"));
            }
        }

        public string Filename
        {
            get { return _filename; }
            set
            {
                _filename = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Filename"));
            }
        }

        public REFERENCE_IMAGE_TYPE Type
        {
            get { return _type; }
            set
            {
                _type = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Type"));
            }
        }

        public VM_SaveImageDialog()
        {
            _saveAsFile = false;
            _location = GlobalVars.ImageFileSaveLocation;
            _filename = "";
            _description = "";
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
