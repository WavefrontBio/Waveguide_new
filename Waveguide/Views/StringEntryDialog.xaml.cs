using System;
using System.Collections.Generic;
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
    /// Interaction logic for StringEntryDialog.xaml
    /// </summary>
    public partial class StringEntryDialog : Window
    {
        StringEntryDialog_ViewModel vm;
        public MessageBoxResult result;
        public string enteredString;

        public StringEntryDialog(string WindowTitle, string Prompt)
        {
            InitializeComponent();
            vm = new StringEntryDialog_ViewModel(WindowTitle, Prompt);
            DataContext = vm;
            stringEntryTextBox.Focus();
        }

        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.OK;
            enteredString = vm.EnteredString;
            Close();
        }

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Cancel;
            enteredString = vm.EnteredString;
            Close();
        }
    }

    public class StringEntryDialog_ViewModel : INotifyPropertyChanged
    {
        private string windowTitle;
        private string promptString;
        private string enteredString;

        public StringEntryDialog_ViewModel(string title, string prompt)
        {
            WindowTitle = title;
            PromptString = prompt;
        }

        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                windowTitle = value;
                NotifyPropertyChanged("WindowTitle");
            }
        }

        public string PromptString
        {
            get { return promptString; }
            set
            {
                promptString = value;
                NotifyPropertyChanged("PromptString");
            }
        }

        public string EnteredString
        {
            get { return enteredString; }
            set
            {
                enteredString = value;
                NotifyPropertyChanged("EnteredString");
            }
        }
      

        public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
    }
}
