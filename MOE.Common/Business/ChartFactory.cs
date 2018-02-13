﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.DataVisualization.Charting;
using MOE.Common.Business.Bins;
using MOE.Common.Business.DataAggregation;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business
{
    public static class ChartFactory
    {
        private static Random _rnd = new Random();

        private static List<Series> SeriesList = new List<Series>();
        private static List<Bin> Bins;

        public static List<Bin> GetBins(SignalAggregationMetricOptions options)
        {
            List <Bin> bins = new List<Bin>();

            DateTime timeX = new DateTime();

            timeX = options.StartDate;

            while(timeX <= options.EndDate)
            {
                Bin bin = new Bin();

                bin.Start = timeX;

                //timeX = timeX.AddMinutes(options.BinSize);

                bin.End = timeX;

                bins.Add(bin);


            }

            return bins;

        }

        public static void AddSeriesToSeriesList(Series series)
        {
            List<Series> checkSeries = (from r in SeriesList
                where series.ChartType != r.ChartType
                select r).ToList();

            if (checkSeries == null || checkSeries.Count == 0)
            {
                SeriesList.Add(series);
            }

        }



        public static Chart ChartInitialization(MetricOptions options)
        {
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateChartArea(options));



            return chart;
        }

        public static Chart ChartInitialization(SignalAggregationMetricOptions options)
        {
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateTimeXIntYChartArea(options));
            chart.Titles.Add(options.ChartTitle);
            return chart;
        }

        public static Chart CreateDefaultChart(MetricOptions options)
        {
            
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateChartArea(options));
            return chart;
        }

        public static Chart CreateSplitFailureChart(SplitFailOptions options)
        {
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateSplitFailChartArea(options));
            SetLegend(chart);
            return chart;
        }
        
        public static Chart CreateTimeXIntYChart(SignalAggregationMetricOptions options, List<Models.Signal> signals)
        {
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateTimeXIntYChartArea(options));
            SetLegend(chart);
            string signalDescriptions = string.Empty;
            foreach (var signal in signals)
            {
                signalDescriptions += signal.SignalDescription + ",";
            }
            signalDescriptions = signalDescriptions.TrimEnd(',');
            chart.Titles.Add( signalDescriptions + "\n" + options.ChartTitle);
            return chart;
        }

        public static Chart CreateLaneByLaneAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 16;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }

        public static Chart CreateAdvancedCountsAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 17;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            
            return chart;
        }

        public static Chart CreateArrivalOnGreenAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 18;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }

        public static Chart CreatePlatoonRatioAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 19;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }

        //public static Chart CreatePurdueSplitFailureAggregationChart(SignalAggregationMetricOptions options)
        //{
        //    options.MetricTypeID = 20;
        //    Chart chart = ChartInitialization(options);
        //    Bins = GetBins(options);
        //    foreach (var a in options.Approaches)
        //    {
        //        Series s = CreateLineSeries(a.Description, Color.FromArgb(_rnd.Next(100,255), _rnd.Next(100, 255), _rnd.Next(100, 255)));
        //        List<ApproachSplitFailAggregation> records = GetApproachAggregationRecords(a,options);
        //        PopulateBinsWithSplitFailAggregateSums(records);
        //        foreach (var bin in Bins)
        //        {
        //            s.Points.AddXY(bin.Start, bin.Sum);
        //        }

        //        chart.Series.Add(s);
        //    }
        //    return chart;
        //}

        private static void PopulateBinsWithSplitFailAggregateSums(List<ApproachSplitFailAggregation> records)
        {
            foreach (var bin in Bins)
            {
                var recordsForBins = from r in records
                    where r.BinStartTime >= bin.Start && r.BinStartTime < bin.End
                    select r;

                bin.Sum = recordsForBins.Sum(s => s.SplitFailures);
            }
        }

        private static void PopulateBinsWithSplitFailAggregateAverages(List<ApproachSplitFailAggregation> records)
        {
            foreach (var bin in Bins)
            {
                var recordsForBins = from r in records
                    where r.BinStartTime >= bin.Start && r.BinStartTime < bin.End
                    select r;

                bin.Sum = Convert.ToInt32( recordsForBins.Average(s => s.SplitFailures));
            }
        }

        public static List<ApproachSplitFailAggregation> GetApproachAggregationRecords(Approach approach, SignalAggregationMetricOptions options)
        {
            IApproachSplitFailAggregationRepository Repo = ApproachSplitFailAggregationRepositoryFactory.Create();
            if (approach != null)
            { 
                //List<ApproachSplitFailAggregation> aggregations =
                //    Repo.GetApproachSplitFailAggregationByApproachIdAndDateRange(
                //        approach.ApproachID, options.StartDate, options.EndDate);
                //return aggregations;
            }
            return null;
        }

        public static Chart CreatePedestrianActuationAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 21;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }

        public static Chart PreemptionAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 22;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }

        public static Chart CreateApproachDelayAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 23;
            Chart chart = ChartInitialization(options);
            Bins = GetBins(options);
            return chart;
        }



        public static Chart TransitSignalPriorityAggregationChart(SignalAggregationMetricOptions options)
        {
            options.MetricTypeID = 24;
            Chart chart = ChartInitialization(options);
            return chart;
        }

        public static Series GetSeriesFromBins()
        {
            Series s = new Series();
            foreach(Bin bin in Bins)
            {
                s.Points.AddXY(bin.Start, bin.Sum);
            }

            return s;
        }

        private static void SetLegend(Chart chart)
        {
            Legend chartLegend = new Legend();
            chartLegend.Name = "MainLegend";
            chartLegend.Docking = Docking.Left;
            chart.Legends.Add(chartLegend);
        }

        private static ChartArea CreateSplitFailChartArea(SplitFailOptions options)
        {
            ChartArea chartArea = new ChartArea();
            chartArea.Name = "ChartArea1";
            SetSplitFailYAxis(chartArea, options);
            SetSplitFailXAxis(chartArea, options);
            SetSplitFailX2Axis(chartArea, options);
            return chartArea;
        }

        private static ChartArea CreateTimeXIntYChartArea(SignalAggregationMetricOptions options)
        {
            ChartArea chartArea = new ChartArea();
            SetDimension(options, chartArea);
            chartArea.Name = "ChartArea1";
            SetIntYAxis(chartArea, options);
            SetTimeXAxis(chartArea, options);
            return chartArea;
        }

        private static void SetSplitFailX2Axis(ChartArea chartArea, SplitFailOptions options)
        {
            var reportTimespan = options.EndDate - options.StartDate;
            chartArea.AxisX2.LabelStyle.Format = "HH";
            if (reportTimespan.Days < 1)
            {
                if (reportTimespan.Hours > 1)
                {
                    chartArea.AxisX2.Interval = 1;
                }
                else
                {
                    chartArea.AxisX2.LabelStyle.Format = "HH:mm";
                }
            }
            chartArea.AxisX2.Minimum = options.StartDate.ToOADate();
            chartArea.AxisX2.Maximum = options.EndDate.ToOADate();
            chartArea.AxisX2.Enabled = AxisEnabled.True;
            chartArea.AxisX2.MajorTickMark.Enabled = true;
            chartArea.AxisX2.IntervalType = DateTimeIntervalType.Hours;
            chartArea.AxisX2.LabelAutoFitStyle = LabelAutoFitStyles.None;
        }

        private static void SetSplitFailXAxis(ChartArea chartArea, SplitFailOptions options)
        {
            var reportTimespan = options.EndDate - options.StartDate;
            chartArea.AxisX.Title = "Time (Hour of Day)";
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
            chartArea.AxisX.LabelStyle.Format = "HH";
            chartArea.AxisX.Minimum = options.StartDate.ToOADate();
            chartArea.AxisX.Maximum = options.EndDate.ToOADate();
            if (reportTimespan.Days < 1)
            {
                if (reportTimespan.Hours > 1)
                {
                    chartArea.AxisX.Interval = 1;
                }
                else
                {
                    chartArea.AxisX.LabelStyle.Format = "HH:mm";
                }
            }
        }

        private static void SetSplitFailYAxis(ChartArea chartArea, SplitFailOptions options)
        {
            if (options.YAxisMax != null)
            {
                chartArea.AxisY.Maximum = options.YAxisMax.Value;
            }
            else
            {
                chartArea.AxisY.Maximum = 100;
            }
            chartArea.AxisY.Title = "Occupancy Ratio (percent)";
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Interval = 10;
        }

        private static void SetIntYAxis(ChartArea chartArea, SignalAggregationMetricOptions options)
        {
            if (options.YAxisMax != null)
            {
                chartArea.AxisY.Maximum = options.YAxisMax.Value;
            }
            else
            {
                chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            }
            if (options.SelectedAggregationType == AggregationType.Sum)
            {
                chartArea.AxisY.Title = "Sum of SplitFailures " + options.TimeOptions.SelectedBinSize.Description() + " bins";
            }
            else if (options.SelectedAggregationType == AggregationType.Average)
            {
                chartArea.AxisY.Title = "Average of SplitFailures";
            }
            else
            {
                chartArea.AxisY.Title = "";
            }
            chartArea.AxisY.Minimum = 0;
        }
        private static void SetTimeXAxis(ChartArea chartArea, SignalAggregationMetricOptions options)
        {
            //var reportTimespan = options.EndDate - options.StartDate;
            chartArea.AxisX.Title = "Time (Hour of Day)";
            chartArea.AxisX.LabelStyle.IsEndLabelVisible = false;
            chartArea.AxisX.LabelStyle.Angle = 45;
            if (options.SelectedXAxisType ==
                XAxisType.TimeOfDay)
            {
                chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                chartArea.AxisX.LabelStyle.Format = "HH:mm";
                chartArea.AxisX.Minimum = options.StartDate.AddMinutes(-15).ToOADate();
            }
            else
            {
                switch (options.TimeOptions.SelectedBinSize)
                {
                    case BinFactoryOptions.BinSize.FifteenMinute:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.LabelStyle.Format = "HH:mm";
                        chartArea.AxisX.Minimum = options.StartDate.AddMinutes(-15).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.ThirtyMinute:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.LabelStyle.Format = "HH:mm";
                        chartArea.AxisX.Minimum = options.StartDate.AddMinutes(-30).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.Hour:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.LabelStyle.Format = "HH";
                        chartArea.AxisX.Minimum = options.StartDate.AddHours(-1).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.Day:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Days;
                        chartArea.AxisX.LabelStyle.Format = "dd";
                        chartArea.AxisX.Title = "Day of Month";
                        chartArea.AxisX.Minimum = options.StartDate.AddDays(-1).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.Week:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
                        chartArea.AxisX.LabelStyle.Format = "MM/dd/yy";
                        chartArea.AxisX.Title = "Start of Week";
                        chartArea.AxisX.Minimum = options.StartDate.AddDays(-7).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.Month:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Months;
                        chartArea.AxisX.LabelStyle.Format = "MM/yyyy";
                        chartArea.AxisX.Title = "Month and Year";
                        chartArea.AxisX.Minimum = options.StartDate.AddMonths(-1).ToOADate();
                        break;
                    case BinFactoryOptions.BinSize.Year:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Years;
                        chartArea.AxisX.LabelStyle.Format = "yyyy";
                        chartArea.AxisX.Title = "Year";
                        chartArea.AxisX.Minimum = options.StartDate.AddYears(-1).ToOADate();
                        break;
                    default:
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.LabelStyle.Format = "HH";
                        break;
                }
                DateTime tempStart;
                DateTime tempEnd;
                if (options.TimeOptions.TimeOption == BinFactoryOptions.TimeOptions.TimePeriod &&
                    (options.TimeOptions.SelectedBinSize == BinFactoryOptions.BinSize.FifteenMinute ||
                     options.TimeOptions.SelectedBinSize == BinFactoryOptions.BinSize.ThirtyMinute ||
                     options.TimeOptions.SelectedBinSize == BinFactoryOptions.BinSize.Hour))
                {
                    tempStart = new DateTime(options.TimeOptions.Start.Year, options.TimeOptions.Start.Month,
                        options.TimeOptions.Start.Day, options.TimeOptions.TimeOfDayStartHour ?? 0,
                        options.TimeOptions.TimeOfDayStartMinute ?? 0, 0);
                    tempEnd = new DateTime(options.TimeOptions.Start.Year, options.TimeOptions.Start.Month,
                        options.TimeOptions.Start.Day, options.TimeOptions.TimeOfDayEndHour ?? 0,
                        options.TimeOptions.TimeOfDayEndMinute ?? 0, 0);
                    chartArea.AxisX.Minimum = tempStart.AddMinutes(-15).ToOADate();
                    chartArea.AxisX.Maximum = tempEnd.ToOADate();
                }
            }
            chartArea.AxisX.Interval = 1;
        }

        private static void SetImageProperties(Chart chart)
        {
            chart.ImageType = ChartImageType.Jpeg;
            chart.Height = 550;
            chart.Width = 1100;
            chart.ImageStorageMode = ImageStorageMode.UseImageLocation;
        }

        private static ChartArea CreateChartArea(MetricOptions options)
        {
            ChartArea chartArea = new ChartArea();
            chartArea.Name = "ChartArea1";
            SetUpYAxis(chartArea, options);
            SetUpY2Axis(chartArea, options);
            SetUpXAxis(chartArea, options);
            SetUpX2Axis(chartArea, options);
            return chartArea;
        }

        private static void SetUpX2Axis(ChartArea chartArea, MetricOptions options)
        {
            chartArea.AxisX2.Enabled = AxisEnabled.True;
            chartArea.AxisX2.MajorTickMark.Enabled = true;
            chartArea.AxisX2.IntervalType = DateTimeIntervalType.Hours;
            chartArea.AxisX2.LabelAutoFitStyle = LabelAutoFitStyles.None;
            chartArea.AxisX2.LabelStyle.Format = "HH";
            TimeSpan reportTimespan = options.EndDate - options.StartDate;
            if (reportTimespan.Days < 1)
            {
                if (reportTimespan.Hours > 1)
                {
                    chartArea.AxisX2.Interval = 1;
                }
                else
                {
                    chartArea.AxisX2.LabelStyle.Format = "HH:mm";
                }
            }
        }

        private static void SetUpXAxis(ChartArea chartArea, MetricOptions options)
        {
            chartArea.AxisX.Title = "Time (Hour of Day)";
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
            chartArea.AxisX.LabelStyle.Format = "HH";
            chartArea.AxisX.Minimum = options.StartDate.ToOADate();
            chartArea.AxisX.Maximum = options.EndDate.ToOADate();
            TimeSpan reportTimespan = options.EndDate - options.StartDate;
            if (reportTimespan.Days < 1)
            {
                if (reportTimespan.Hours > 1)
                {
                    chartArea.AxisX.Interval = 1;
                }
                else
                {
                    chartArea.AxisX.LabelStyle.Format = "HH:mm";
                }
            }
        }

        private static void SetUpY2Axis(ChartArea chartArea, MetricOptions options)
        {
            if (options.Y2AxisMax != null)
            {
                chartArea.AxisY2.Maximum = options.Y2AxisMax.Value;
            }
            chartArea.AxisY2.Enabled = AxisEnabled.True;
            chartArea.AxisY2.MajorTickMark.Enabled = true;
            chartArea.AxisY2.MajorGrid.Enabled = false;
            chartArea.AxisY2.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY2.Title = "Volume Per Hour ";
        }

        private static void SetUpYAxis(ChartArea chartArea, MetricOptions options)
        {
            if (options.YAxisMax != null)
            {
                chartArea.AxisY.Maximum = options.YAxisMax.Value;
            }
            chartArea.AxisY.Title = "Cycle Time (Seconds) ";
            chartArea.AxisY.Minimum = 0;
        }

        public static Series CreateLineSeries(string seriesName, Color seriesColor)
        {
            Series s = new Series();
            s.ChartType = SeriesChartType.Line;
            s.Color = seriesColor;
            return s;
        }

        public static Series CreateStackedAreaSeries(string seriesName, Color seriesColor)
        {
            Series s = new Series();
            s.ChartType = SeriesChartType.StackedArea;
            s.Color = seriesColor;
            return s;
        }

        public static Series CreateColumnSeries(string seriesName, Color seriesColor)
        {
            Series s = new Series();
            s.ChartType = SeriesChartType.Column;
            s.Color = seriesColor;
            return s;
        }

        public static Series CreateStackedColumnSeries(string seriesName, Color seriesColor)
        {
            Series s = new Series();
            s.ChartType = SeriesChartType.StackedColumn;
            s.Color = seriesColor;
            return s;
        }


        public static Chart CreateStringXIntYChart(SignalAggregationMetricOptions options)
        {
            Chart chart = new Chart();
            SetImageProperties(chart);
            chart.ChartAreas.Add(CreateStringXIntYChartArea(options));
            SetLegend(chart);
            chart.Titles.Add(options.ChartTitle);
            return chart;
        }

        private static ChartArea CreateStringXIntYChartArea(SignalAggregationMetricOptions options)
        {
            ChartArea chartArea = new ChartArea();
            chartArea.Name = "ChartArea1";
            SetDimension(options, chartArea);
            SetIntYAxis(chartArea, options);
            SetStringXAxis(chartArea, options);
            return chartArea;
        }

        private static void SetDimension(SignalAggregationMetricOptions options, ChartArea chartArea)
        {
            if (options.SelectedDimension == Dimension.ThreeDimensional)
            {
                chartArea.Area3DStyle = new ChartArea3DStyle { Enable3D = true, WallWidth = 0 };
            }
        }

        private static void SetStringXAxis(ChartArea chartArea, SignalAggregationMetricOptions options)
        {
            chartArea.AxisX.Title = "Signals";
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelAutoFitStyle = LabelAutoFitStyles.None;
            chartArea.AxisX.LabelStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12);
            chartArea.AxisX.LabelStyle.Angle = 45;
        }
    }
}
