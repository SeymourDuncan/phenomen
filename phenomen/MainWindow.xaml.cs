using CoreData;
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

namespace phenomen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Dictionary<double, double> Kcalc = new Dictionary<double, double>();
        List<Point> Kreal;
        DeseaseType Desease;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<NodeData> resultList = new List<CoreData.NodeData>();
            

            var dataStorage = new DataStorage();
            if (!dataStorage.Init())
                return;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".csv";
            dlg.Filter = "Spectr data (.csv)|*.csv";
            // Display OpenFileDialog by calling ShowDialog method

            Nullable<bool> result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                //resultList = dataStorage.HandleCsvRet(filename);
                Kcalc.Clear();
                // всё что нужно получаем тут
                Desease = dataStorage.AnalyzeFile(filename, Kcalc);
                Kreal = dataStorage.SpmVals;

                cchart.DataContext = resultList;
            }

        }
    }
}
