using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;

namespace DefectoscopyAnalyzer
{
    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private TextBlock defectsInfo;
        private const int VALUES_PER_SIGNAL = 16384;
        private const int SIGNALS_PER_ROTATION = 1057;
        private const double THRESHOLD = 7.0;
        private const int DEFECT_MIN_DURATION = 5;
        private const int CHECK_START = 631;
        private const int CHECK_END = 831;

        public MainWindow()
        {
            InitializeComponent();
            SetupPlot();
            SetupUI();
            plotView.Model = plotModel;
        }

        private void SetupPlot()
        {
            plotModel = new PlotModel
            {
                Title = "Анализ дефектоскопии",
                DefaultColors = new List<OxyColor> { OxyColors.Blue }
            };

            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Величина сигнала",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Время (с)"
            });
        }

        private void SetupUI()
        {
            defectsInfo = new TextBlock
            {
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };

            // Добавляем TextBlock в третью строку Grid (Row="2")
            Grid.SetRow(defectsInfo, 2);
            ((Grid)this.Content).Children.Add(defectsInfo);
        }

        private async Task AnalyzeDirectoryAsync(string directoryPath)
        {
            try
            {
                string[] filePaths = Directory.GetFiles(directoryPath, "*.txt");
                List<(int start, int end)> defects = new List<(int, int)>();

                for (int signalIndex = 0; signalIndex < filePaths.Length; signalIndex++)
                {
                    var values = await Task.Run(() => LoadDataFromFile(filePaths[signalIndex]));
                    await PlotSignalAsync(values, signalIndex);

                    if (signalIndex >= DEFECT_MIN_DURATION - 1)
                    {
                        defects.AddRange(DetectDefects(filePaths, signalIndex));
                    }
                }

                UpdateDefectsInfo(defects);
                MessageBox.Show($"Анализ завершен. Найдено дефектов: {defects.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private List<(int start, int end)> DetectDefects(string[] files, int currentIndex)
        {
            List<(int start, int end)> defects = new List<(int, int)>();
            int windowSize = DEFECT_MIN_DURATION;

            for (int i = CHECK_START - 1; i <= CHECK_END - windowSize; i++)
            {
                bool isDefect = true;
                for (int j = 0; j < windowSize; j++)
                {
                    int signalIndex = currentIndex - (windowSize - 1) + j;
                    if (signalIndex < 0) continue;

                    var values = LoadDataFromFile(files[signalIndex]);
                    if (values[i + j] <= THRESHOLD)
                    {
                        isDefect = false;
                        break;
                    }
                }

                if (isDefect)
                {
                    int defectStart = currentIndex - (windowSize - 1) + 1;
                    int defectEnd = currentIndex + 1;
                    defects.Add((defectStart, defectEnd));
                    i += windowSize - 1;
                }
            }

            return defects;
        }

        private async Task PlotSignalAsync(List<double> values, int signalIndex)
        {
            plotModel.Series.Clear();
            var lineSeries = new LineSeries
            {
                Title = $"Сигнал {signalIndex + 1}",
                StrokeThickness = 1
            };

            double totalTime = 16.0;
            double timeStep = totalTime / (values.Count - 1);

            for (int i = 0; i < values.Count; i++)
            {
                double time = i * timeStep;
                lineSeries.Points.Add(new DataPoint(time, values[i]));
            }

            plotModel.Series.Add(lineSeries);
            plotModel.InvalidatePlot(true);
            await Task.Delay(500);
        }

        private List<double> LoadDataFromFile(string filePath)
        {
            var values = new List<double>();
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
            defectsInfo.Text = info.TrimEnd();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string directoryPath = @"D:\WORK\FlawDetector\signals\";
            await AnalyzeDirectoryAsync(directoryPath);
        }
    }
}