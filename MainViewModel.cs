using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace DefectoscopyAnalyzer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private PlotModel _plotModel;
        private string _defectsInfoText;
        private bool _isBusy = false;
        private readonly List<List<double>> _cachedSignals; //  кэш для сигналов
        private const double THRESHOLD = 7.0;
        private const int DEFECT_MIN_DURATION = 4;
        private const int CHECK_START = 631;
        private const int CHECK_END = 831;

        public MainViewModel()
        {
            SetupPlot();
            Analyze = new RelayCommand(async _ => await AnalyzeDirectoryAsync(), _ => !IsBusy);
            //_cachedSignals = new List<List<double>>();
        }

        public ICommand Analyze { get; } 
        public PlotModel PlotModel
        {
            get => _plotModel;
            set
            {
                _plotModel = value;
                OnPropertyChanged(nameof(PlotModel));
            }
        }
        public string DefectsInfoText
        {
            get => _defectsInfoText;
            set
            {
                _defectsInfoText = value;
                OnPropertyChanged(nameof(DefectsInfoText));
            }
        }
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        private void SetupPlot()
        {
            PlotModel = new PlotModel
            {
                Title = "Анализ дефектоскопии",
                DefaultColors = new List<OxyColor> { OxyColors.Blue },
                IsLegendVisible = true
            };

            PlotModel.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.RightTop,
                LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
                LegendOrientation = OxyPlot.Legends.LegendOrientation.Vertical
            });

            PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Величина сигнала",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Время (с)"
            });
        }
        private async Task AnalyzeDirectoryAsync()
        {
            IsBusy = true;

            //_cachedSignals.Clear();
            string directoryPath = @"D:\WORK\DefectoscopyAnalyzer\signals\";
            string[] filePaths = Directory.GetFiles(directoryPath, "*.txt");
            List<(int start, int end)> defects = new List<(int, int)>();

            int defectEnd = 0;
            int defectStart = 0;
            int durationDefect = 0;

            for (int signalIndex = 0; signalIndex < filePaths.Length; signalIndex++)
            {
                var values = await Task.Run(() => LoadDataFromFile(filePaths[signalIndex]));
                //_cachedSignals.Add(values);
                await PlotSignalAsync(values, signalIndex);

                if (signalIndex >= DEFECT_MIN_DURATION - 1)
                {
                    //var values = _cachedSignals[signalIndex];
                    bool hasDefect = false;

                    for (int i = 631; i < 831; i++)
                    {
                        if (values[i] > 7)
                        {
                            hasDefect = true;
                            break;
                        }
                    }

                    if (hasDefect)
                    {
                        defectEnd = signalIndex;
                        durationDefect++;
                    }
                    else if(defectEnd != 0 && durationDefect > 3)
                    {
                        defectStart = defectEnd - durationDefect;
                        defects.Add((defectStart, defectEnd));
                        defectEnd = 0;
                        durationDefect = 0;
                    }
                }
                UpdateDefectsInfo(defects);
            }

            IsBusy = false;
        }

        private async Task PlotSignalAsync(List<double> values, int signalIndex)
        {
            PlotModel.Series.Clear();
            LineSeries lineSeries = new LineSeries
            {
                Title = $"Сигнал {signalIndex + 1}",
                StrokeThickness = 1
            };

            double totalTime = 23.3;
            double timeStep = totalTime / (values.Count - 1);

            for (int i = 0; i < values.Count; i++)
            {
                double time = i * timeStep;
                lineSeries.Points.Add(new DataPoint(time, values[i]));
            }

            PlotModel.Series.Add(lineSeries);
            PlotModel.InvalidatePlot(true);
            await Task.Delay(10);
            //await Task.Delay(500);
        }

        private List<double> LoadDataFromFile(string filePath)
        {
            List<double> values = new List<double>();
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (double.TryParse(line.Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value))
                {
                    values.Add(value);
                }
            }
            return values;
        }

        private void UpdateDefectsInfo(List<(int start, int end)> defects)
        {
            string info = $"Количество дефектов: {defects.Count}\n";
            for (int i = 0; i < defects.Count; i++)
            {
                info += $"№{i + 1}: {defects[i].start} - {defects[i].end}\n";
            }
            DefectsInfoText = info.TrimEnd();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}