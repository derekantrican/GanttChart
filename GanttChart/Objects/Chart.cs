using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System;
using static GanttChart.Enums;
using System.ComponentModel;

namespace GanttChart
{
    public class Chart : UserControl
    {
        #region Initializations
        public Chart()
        {
            Init();
        }

        private void Init()
        {
            this.AutoScroll = true;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            doubleClickTimer.Tick += doubleClickTimer_Tick;

            NowIndicatorTimer = new System.Timers.Timer() { Interval = 1000 }; //Don't really need such a quick interval but it helps with render issues by refreshing the chart often
            NowIndicatorTimer.Elapsed += NowIndicatorTimer_Elapsed;
            NowIndicatorTimer.Start();
            NowIndicatorTimer.Enabled = true;
        }

        private void NowIndicatorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
                this.Invoke((MethodInvoker)delegate { UpdateView(); });

            NowTick?.Invoke();
        }
        #endregion Initializations

        #region Interaction
        //Double click vs Single click code from https://docs.microsoft.com/en-us/dotnet/framework/winforms/how-to-distinguish-between-clicks-and-double-clicks
        private Rectangle doubleClickRectangle = new Rectangle();
        private Timer doubleClickTimer = new Timer() { Interval = 100 };
        private bool isFirstClick = true;
        private bool isDoubleClick = false;
        private int milliseconds = 0;
        private Point doubleClickLocation = new Point();
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            //This is the first mouse click.
            if (isFirstClick)
            {
                isFirstClick = false;
                doubleClickLocation = e.Location;

                //Determine the location and size of the double click 
                //rectangle area to draw around the cursor point.
                doubleClickRectangle = new Rectangle(
                    e.X - (SystemInformation.DoubleClickSize.Width / 2),
                    e.Y - (SystemInformation.DoubleClickSize.Height / 2),
                    SystemInformation.DoubleClickSize.Width,
                    SystemInformation.DoubleClickSize.Height);
                Invalidate();

                //Start the double click timer.
                doubleClickTimer.Start();
            }
            else //This is the second mouse click.
            {
                //Verify that the mouse click is within the double click
                //rectangle and is within the system-defined double 
                //click period.
                if (doubleClickRectangle.Contains(e.Location) &&
                    milliseconds < SystemInformation.DoubleClickTime)
                {
                    isDoubleClick = true;
                }
            }
        }

        private void doubleClickTimer_Tick(object sender, EventArgs e)
        {
            milliseconds += 100;

            //The timer has reached the double click time limit.
            if (milliseconds >= SystemInformation.DoubleClickTime)
            {
                doubleClickTimer.Stop();

                Row clickedRow = null;
                Row relatedRow = null;
                TimeBlock clickedTimeBlock = null;
                foreach (Row row in Rows.Where(p => p.IsVisible))
                {
                    if (row.Rect.Contains(doubleClickLocation))
                    {
                        clickedRow = row;
                        break;
                    }
                    else if (row.TimeBlocks.Find(p => p.Rect.Contains(doubleClickLocation)) != null)
                    {
                        relatedRow = row;
                        clickedTimeBlock = row.TimeBlocks.Find(p => p.Rect.Contains(doubleClickLocation));
                        break;
                    }
                }

                Point eventGlobalLocation = new Point(doubleClickLocation.X + this.Left,
                                                doubleClickLocation.Y + this.Top);

                if (isDoubleClick)
                {
                    if (clickedRow != null)
                    {
                        if (clickedRow.IconRect.Contains(doubleClickLocation))
                            RowIconDoubleClick?.Invoke(new RowClickedEventArgs() { ClickedRow = clickedRow, CursorLocation = eventGlobalLocation });
                        else
                            RowDoubleClick?.Invoke(new RowClickedEventArgs() { ClickedRow = clickedRow, CursorLocation = eventGlobalLocation });
                    }
                    else if (clickedTimeBlock != null && clickedTimeBlock.Clickable)
                        TimeBlockDoubleClick?.Invoke(new TimeBlockClickedEventArgs() { ClickedTimeBlock = clickedTimeBlock, RelatedRow = relatedRow, CursorLocation = eventGlobalLocation });
                    else if (renderer.MainCanvas.Contains(doubleClickLocation))
                    {
                        Row horizontalRow = Rows.FirstOrDefault(p => p.Rect.Contains(1, doubleClickLocation.Y));

                        DateTime? clickedTime = null;
                        for (int i = 0; i < renderer.TimeXLocations.Count - 1; i++) //Loop to 1 less than the end so we don't get index exceptions
                        {
                            Tuple<DateTime, int, StringFormat> time = renderer.TimeXLocations[i];
                            Rectangle rowRect = horizontalRow.Rect;
                            rowRect.X = time.Item2;
                            rowRect.Width = renderer.TimeXLocations[i + 1].Item2 - rowRect.X;

                            if (rowRect.Contains(doubleClickLocation))
                            {
                                clickedTime = time.Item1;
                                break;
                            }
                        }

                        MainCanvasDoubleClick?.Invoke(new CanvasClickedEventArgs() { RelatedRow = horizontalRow, ClickedLocation = clickedTime, CursorLocation = eventGlobalLocation });
                    }
                }
                else
                {
                    if (clickedRow != null)
                    {
                        if (clickedRow.IconRect.Contains(doubleClickLocation))
                            RowIconSingleClick?.Invoke(new RowClickedEventArgs() { ClickedRow = clickedRow, CursorLocation = eventGlobalLocation });
                        else
                            RowSingleClick?.Invoke(new RowClickedEventArgs() { ClickedRow = clickedRow, CursorLocation = eventGlobalLocation });
                    }
                    else if (clickedTimeBlock != null && clickedTimeBlock.Clickable)
                        TimeBlockSingleClick?.Invoke(new TimeBlockClickedEventArgs() { ClickedTimeBlock = clickedTimeBlock, RelatedRow = relatedRow, CursorLocation = eventGlobalLocation });
                    else if (renderer.MainCanvas.Contains(doubleClickLocation))
                    {
                        Row horizontalRow = Rows.FirstOrDefault(p => p.Rect.Contains(1, doubleClickLocation.Y));

                        DateTime? clickedTime = null;
                        for (int i = 0; i < renderer.TimeXLocations.Count - 1; i++) //Loop to 1 less than the end so we don't get index exceptions
                        {
                            Tuple<DateTime, int, StringFormat> time = renderer.TimeXLocations[i];
                            Rectangle rowRect = horizontalRow.Rect;
                            rowRect.X = time.Item2;
                            rowRect.Width = renderer.TimeXLocations[i + 1].Item2 - rowRect.X;

                            if (rowRect.Contains(doubleClickLocation))
                            {
                                clickedTime = time.Item1;
                                break;
                            }
                        }

                        MainCanvasSingleClick?.Invoke(new CanvasClickedEventArgs() { RelatedRow = horizontalRow, ClickedLocation = clickedTime, CursorLocation = eventGlobalLocation });
                    }
                }

                //Allow the MouseDown event handler to process clicks again.
                isFirstClick = true;
                isDoubleClick = false;
                milliseconds = 0;
            }
        }
        #endregion Interaction

        #region Draw Interface
        private System.Timers.Timer NowIndicatorTimer;

        private Renderer renderer = new Renderer();
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //Don't paint if in design mode
            if (IsDesignerHosted || this.DesignMode)
                return;

            this.AutoScrollMinSize = renderer.CalculateAutoScrollSize(this.AutoScrollMinSize, this.Size, this.VerticalScroll);

            renderer.Render(e.Graphics, this.AutoScrollPosition, this.AutoScrollMinSize, this.Font);
        }

        //Check for "DesignMode" if inside a UserControl
        //https://stackoverflow.com/a/708594/2246411
        private bool IsDesignerHosted
        {
            get
            {
                Control ctrl = this;

                while (ctrl != null)
                {
                    if ((ctrl.Site != null) && ctrl.Site.DesignMode)
                        return true;
                    ctrl = ctrl.Parent;
                }
                return false;
            }
        }
        #endregion Draw Interface

        #region Public Events
        public delegate void MainCanvasSingleClickDelegate(CanvasClickedEventArgs e);
        [Category("Gantt Chart")]
        public event MainCanvasSingleClickDelegate MainCanvasSingleClick;

        public delegate void MainCanvasDoubleClickDelegate(CanvasClickedEventArgs e);
        [Category("Gantt Chart")]
        public event MainCanvasDoubleClickDelegate MainCanvasDoubleClick;

        public delegate void RowSingleClickDelegate(RowClickedEventArgs e);
        [Category("Gantt Chart")]
        public event RowSingleClickDelegate RowSingleClick;

        public delegate void RowDoubleClickDelegate(RowClickedEventArgs e);
        [Category("Gantt Chart")]
        public event RowDoubleClickDelegate RowDoubleClick;

        public delegate void RowIconSingleClickDelegate(RowClickedEventArgs e);
        [Category("Gantt Chart")]
        public event RowIconSingleClickDelegate RowIconSingleClick;

        public delegate void RowIconDoubleClickDelegate(RowClickedEventArgs e);
        [Category("Gantt Chart")]
        public event RowIconDoubleClickDelegate RowIconDoubleClick;

        public delegate void TimeBlockSingleClickDelegate(TimeBlockClickedEventArgs e);
        [Category("Gantt Chart")]
        public event TimeBlockSingleClickDelegate TimeBlockSingleClick;

        public delegate void TimeBlockDoubleClickDelegate(TimeBlockClickedEventArgs e);
        [Category("Gantt Chart")]
        public event TimeBlockDoubleClickDelegate TimeBlockDoubleClick;
        #endregion Public Events

        #region Public Methods
        public void UpdateView()
        {
            Invalidate(); //Force chart to redraw
        }

        public Dictionary<Row, List<TimeBlock>> GetNowTimeBlocks()
        {
            DateTime now = DateTime.Now;
            now = now.AddHours(NowIndicatorHourOffset);

            Dictionary<Row, List<TimeBlock>> result = new Dictionary<Row, List<TimeBlock>>();
            foreach (Row row in Rows)
            {
                foreach (TimeBlock timeBlock in row.TimeBlocks)
                {
                    if (now >= timeBlock.StartTime &&
                        now <= timeBlock.EndTime)
                    {
                        if (result.ContainsKey(row))
                            result[row].Add(timeBlock);
                        else
                            result.Add(row, new List<TimeBlock>() { timeBlock });
                    }
                }
            }

            return result;
        }

        public void RecaculateFormatDictionaries(bool overwrite = false)
        {
            if (overwrite)
            {
                DayLabelFormats.Clear();
                TimeLabelFormats.Clear();
            }

            DateTime curDay = StartDate;
            while (curDay < EndDate)
            {
                int curHour = StartHourInDay;
                while (curHour <= EndHourInDay)
                {
                    DateTime dateWithHour = curDay.AddHours(curHour);

                    if (!TimeLabelFormats.ContainsKey(dateWithHour))
                        TimeLabelFormats.Add(dateWithHour, DefaultTimeLabelFormat);

                    curHour++;
                }

                if (!DayLabelFormats.ContainsKey(curDay))
                    DayLabelFormats.Add(curDay, DefaultDayLabelFormat);

                curDay = curDay.AddDays(1);
            }
        }
        #endregion Public Methods

        #region Public Properties
        /// <summary>
        /// The main data for the chart
        /// </summary>
        [Category("Gantt Chart")]
        public List<Row> Rows
        {
            get { return renderer.Rows; }
            set { renderer.Rows = value; }
        }

        /// <summary>
        /// The height of the top header (where the horizontal axis labels are)
        /// </summary>
        [Category("Gantt Chart")]
        public int TopHeaderHeight
        {
            get { return renderer.TopHeaderHeight; }
            set { renderer.TopHeaderHeight = value; }
        }

        /// <summary>
        /// Controls the minimum width (in pixels) of each timeblock. When the chart is resized, all timeblocks will compress down to this size. 
        /// Then a horizontal scrollbar will start to appear.
        /// </summary>
        [Category("Gantt Chart")]
        public int MinTimeIntervalWidth //= 0; //In pixels. If left to 0, the chart width with always match the width of the parent control (no horizontal scrollbar)
        {
            get { return renderer.MinTimeIntervalWidth; }
            set { renderer.MinTimeIntervalWidth = value; }
        }

        /// <summary>
        /// Controls whether or not the "now indicator" (a red line drawn vertically across the chart) should be shown
        /// </summary>
        [Category("Gantt Chart")]
        public bool ShowNowIndicator
        {
            get { return renderer.ShowNowIndicator; }
            set { renderer.ShowNowIndicator = value; }
        }

        /// <summary>
        /// Controls the offset for the now indicator. Useful for displaying the now indicator at a different timezone than the computer's current timezone
        /// </summary>
        [Category("Gantt Chart")]
        public int NowIndicatorHourOffset
        {
            get { return renderer.NowIndicatorHourOffset; }
            set { renderer.NowIndicatorHourOffset = value; }
        }

        /// <summary>
        /// Sets whether or not horizontal gridlines (the lines between rows) should be visible
        /// </summary>
        [Category("Gantt Chart")]
        public bool HorizontalGridLinesVisible
        {
            get { return renderer.HorizontalGridLinesVisible; }
            set { renderer.HorizontalGridLinesVisible = value; }
        }

        /// <summary>
        /// Sets whether or not vertical gridlines (the lines between days) should be shown
        /// </summary>
        [Category("Gantt Chart")]
        public bool VerticalGridLinesVisible
        {
            get { return renderer.VerticalGridLinesVisible; }
            set { renderer.VerticalGridLinesVisible = value; }
        }

        /// <summary>
        /// StartDate is inclusive (chart starts at 00:00 of this day)
        /// </summary>
        [Category("Gantt Chart")]
        public DateTime StartDate
        {
            get { return renderer.StartDate.Date; }
            set { renderer.StartDate = value.Date; }
        }

        /// <summary>
        /// EndDate is exclusive (chart ends at 00:00 of this day)
        /// </summary>
        [Category("Gantt Chart")]
        public DateTime EndDate
        {
            get { return renderer.EndDate.Date; }
            set { renderer.EndDate = value.Date; }
        }

        /// <summary>
        /// StartHourInDay is inclusive (first hour on the chart for the day will be StartHourInDay). Note: this is only relevant for TimeInterval.Hour
        /// </summary>
        [Category("Gantt Chart")]
        public int StartHourInDay
        {
            get { return renderer.StartHourInDay; }
            set { renderer.StartHourInDay = value; }
        }

        /// <summary>
        /// EndHourInDay is exclusive (last hour on the chart for the day will be EndHourInDay). Note: this is only relevant for TimeInterval.Hour
        /// </summary>
        [Category("Gantt Chart")]
        public int EndHourInDay
        {
            get { return renderer.EndHourInDay; }
            set { renderer.EndHourInDay = value; }
        }

        //Todo: need to support this in the future. IMPORTANT: this may also require a lot of renaming of other properties or functionality
        //public TimeInterval TimeInterval = TimeInterval.Hour; //Todo: need to support other time intervals

        /// <summary>
        /// The format for the time labels
        /// </summary>
        [Category("Gantt Chart")]
        public string DefaultTimeLabelFormat //Here is where someone can display military time instead
        {
            get { return renderer.DefaultTimeLabelFormat; }
            set
            {
                string oldFormat = renderer.DefaultTimeLabelFormat;
                renderer.DefaultTimeLabelFormat = value;

                foreach (DateTime key in TimeLabelFormats.Keys.ToList())
                {
                    if (TimeLabelFormats[key] == oldFormat)
                        TimeLabelFormats[key] = renderer.DefaultTimeLabelFormat;
                }
            }
        }

        /// <summary>
        /// The format for the date labels
        /// </summary>
        [Category("Gantt Chart")]
        public string DefaultDayLabelFormat
        {
            get { return renderer.DefaultDayLabelFormat; }
            set
            {
                string oldFormat = renderer.DefaultDayLabelFormat;
                renderer.DefaultDayLabelFormat = value;

                foreach (DateTime key in DayLabelFormats.Keys.ToList())
                {
                    if (DayLabelFormats[key] == oldFormat)
                        DayLabelFormats[key] = renderer.DefaultDayLabelFormat;
                }
            }
        }

        /// <summary>
        /// Controls the color of the text of most of the form (timeblock text color automatically adjusts based on timeblock color)
        /// </summary>
        [Category("Gantt Chart")]
        public Color TextColor
        {
            get { return renderer.TextColor; }
            set { renderer.TextColor = value; }
        }

        /// <summary>
        /// Controls the background of the row headers
        /// </summary>
        [Category("Gantt Chart")]
        public Color HeaderBackgroundColor
        {
            get { return renderer.HeaderBackgroundColor; }
            set { renderer.HeaderBackgroundColor = value; }
        }

        /// <summary>
        /// Controls the background color of the chart canvas area
        /// </summary>
        [Category("Gantt Chart")]
        public Color BackgroundColor
        {
            get { return renderer.BackgroundColor; }
            set { renderer.BackgroundColor = value; }
        }

        /// <summary>
        /// Location of the icon in the row header
        /// </summary>
        [Category("Gantt Chart")]
        public Corner RowIconLocation
        {
            get { return renderer.RowIconLocation; }
            set { renderer.RowIconLocation = value; }
        }

        /// <summary>
        /// Size of the icon in the row header
        /// </summary>
        [Category("Gantt Chart")]
        public Size RowIconSize
        {
            get { return renderer.RowIconSize; }
            set { renderer.RowIconSize = value; }
        }

        public delegate void NowTickDelegate();
        /// <summary>
        /// This event is fired whenver the chart updates its now indicator (about every second). NOTE: this
        /// event does not run on the UI thread
        /// </summary>
        [Category("Gantt Chart")]
        public event NowTickDelegate NowTick;

        /// <summary>
        /// Each date shown in the chart along with its X location
        /// </summary>
        [Category("Gantt Chart")]
        public IReadOnlyDictionary<DateTime, int> DayLocations
        {
            get
            {
                Dictionary<DateTime, int> result = new Dictionary<DateTime, int>();
                renderer.DayXLocations.ForEach(p =>
                {
                    result.Add(p.Item1, p.Item2);
                });

                return result;
            }
        }

        /// <summary>
        /// Each time shown in the chart along with its X location
        /// </summary>
        [Category("Gantt Chart")]
        public IReadOnlyDictionary<DateTime, int> TimeLocations
        {
            get
            {
                Dictionary<DateTime, int> result = new Dictionary<DateTime, int>();
                renderer.TimeXLocations.ForEach(p =>
                {
                    result.Add(p.Item1, p.Item2);
                });

                return result;
            }
        }

        /// <summary>
        /// <para>Controls the time display format for each individual time label</para>
        /// <para>Keys are DateTime and Values are string (the DateTime format string)</para>
        /// </summary>
        [Category("Gantt Chart")]
        public Dictionary<DateTime, string> TimeLabelFormats
        {
            get { return renderer.TimeLabelFormats; }
            set { renderer.TimeLabelFormats = value; }
        }

        /// <summary>
        /// <para>Controls the day display format for each individual day label</para>
        /// <para>Keys are DateTime and Values are string (the DateTime format string)</para>
        /// </summary>
        [Category("Gantt Chart")]
        public Dictionary<DateTime, string> DayLabelFormats
        {
            get { return renderer.DayLabelFormats; }
            set { renderer.DayLabelFormats = value; }
        }

        /// <summary>
        /// <para>Controls holidays for the form. On each holiday, a cross hatch is drawn through all rows and no timeblocks are shown.</para>
        /// <para>Keys are DateTime and Values are string (the name of the Holiday)</para>
        /// </summary>
        [Category("Gantt Chart")]
        public Dictionary<DateTime, string> Holidays
        {
            get { return renderer.Holidays; }
            set { renderer.Holidays = value; }
        }
        #endregion Public Properties
    }
}