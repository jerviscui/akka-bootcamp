using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        public class InitializeChart
        {
        }

        public class AddSeries
        {
            public AddSeries(Series series)
            {
                Series = series;
            }

            public Series Series { get; private set; }
        }

        public class RemoveSeries
        {
            public RemoveSeries(string series)
            {
                Series = series;
            }

            public string Series { get; private set; }
        }

        public class TogglePause
        {
        }

        #endregion

        public const int MaxPoints = 250;

        private int xPosCounter = 0;

        private readonly Chart _chart;

        private readonly Button _pauseButton;

        private readonly Dictionary<string, Series> _seriesIndex;

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, pauseButton,
            new Dictionary<string, Series>())
        {
        }

        private ChartingActor(Chart chart, Button pauseButton, Dictionary<string, Series> seriesIndex)
        {
            _chart = chart;
            _pauseButton = pauseButton;
            _seriesIndex = seriesIndex;

            Charting();
        }

        private void Charting()
        {
            Receive<InitializeChart>(HandleInitialize);
            Receive<AddSeries>(HandleAddSeries);
            Receive<RemoveSeries>(HandleAddSeries);
            Receive<Metric>(HandleMetrics);

            Receive<TogglePause>(pause =>
            {
                SetPausedText(true);
                BecomeStacked(Paused);
            });
        }

        private void SetPausedText(bool paused)
        {
            _pauseButton.Text = !paused ? "PAUSE ||" : "RESUME ->";
        }

        private void Paused()
        {
            Receive<AddSeries>(series => Stash.Stash());
            Receive<RemoveSeries>(series => Stash.Stash());

            Receive<Metric>(HandleMetricsPaused);

            Receive<TogglePause>(pause =>
            {
                SetPausedText(false);
                UnbecomeStacked();

                Stash.UnstashAll();
            });
        }

        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            //if (ic.InitialSeries != null)
            //{
            //    //swap the two series out
            //    _seriesIndex = ic.InitialSeries;
            //}

            //delete any existing series
            _chart.Series.Clear();

            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundaries();

            //attempt to render the initial chart
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    //force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            SetChartBoundaries();
        }

        private void SetChartBoundaries()
        {
            var dataPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            var yValues = dataPoints.SelectMany(point => point.YValues).ToList();
            double maxAxisX = xPosCounter;
            double minAxisX = xPosCounter - MaxPoints;
            var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1;
            var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0;

            if (dataPoints.Count > 2)
            {
                var area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);

                SetChartBoundaries();
            }
        }

        private void HandleAddSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series) && _seriesIndex.ContainsKey(series.Series))
            {
                var remove = _seriesIndex[series.Series];
                _seriesIndex.Remove(series.Series);
                _chart.Series.Remove(remove);

                SetChartBoundaries();
            }
        }

        private void HandleMetrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.TryGetValue(metric.Series, out var series))
            {
                series.Points.AddXY(xPosCounter++, metric.CounterValue);
                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }

                SetChartBoundaries();
            }
        }

        private void HandleMetricsPaused(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.TryGetValue(metric.Series, out var series))
            {
                series.Points.AddXY(xPosCounter++, 0);
                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }

                //SetChartBoundaries();
            }
        }

        #endregion

        /// <inheritdoc />
        public IStash Stash { get; set; }
    }
}
