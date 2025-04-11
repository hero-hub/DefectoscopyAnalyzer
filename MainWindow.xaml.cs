using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;

namespace DefectoscopyAnalyzer
{
    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private const int VALUES_PER_SIGNAL = 16384;
        private const int SIGNALS_PER_ROTATION = 1057;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();  
        }
    }
}