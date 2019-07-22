using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using static GanttChart.Enums;

namespace GanttChart
{
    public class Renderer
    {
        #region Private Properties
        private Graphics graphics;
        private Point autoScrollPosition;
        private Size autoScrollMinSize;
        private StringFormat alignAllCenter = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private StringFormat alignVertCenter = new StringFormat() { LineAlignment = StringAlignment.Center };
        private Font font;
        #endregion Private Properties

        #region Constructor
        public Renderer()
        {
            InitDefaultValues();
        }
        #endregion Constructor

        #region Public Methods
        public void Render(Graphics g, Point autoScrollPosition, Size autoScrollSize, Font font)
        {
            this.graphics = g;
            this.autoScrollPosition = autoScrollPosition;
            this.autoScrollMinSize = autoScrollSize;
            this.font = font;

            DrawChart();
        }

        public Size CalculateAutoScrollSize(Size autoScrollMinSize, Size size, VScrollProperties verticalScroll)
        {
            Size autoScrollSize = this.autoScrollMinSize;

            //Dynamically adjust width
            if (verticalScroll.Visible)
                autoScrollSize.Width = size.Width - SystemInformation.VerticalScrollBarWidth;
            else
                autoScrollSize.Width = size.Width;

            int timeDivisions = (EndHourInDay - StartHourInDay) * (EndDate - StartDate).Days;
            int pixelsPerDivision = autoScrollSize.Width / timeDivisions;
            if (pixelsPerDivision < MinTimeIntervalWidth)
            {
                if (verticalScroll.Visible)
                    autoScrollSize.Width = timeDivisions * MinTimeIntervalWidth - SystemInformation.VerticalScrollBarWidth;
                else
                    autoScrollSize.Width = timeDivisions * MinTimeIntervalWidth;
            }

            //Dynamically adjust height
            if (Rows.Count > 0 && Rows.All(p => p.Rect != null))
            {
                int highestYValue = Rows.Select(p => { return p.Rect.Bottom; }).Max();
                autoScrollSize.Height = highestYValue + 1;
            }
            else
                autoScrollSize.Height = size.Height;

            if (autoScrollMinSize != autoScrollSize)
                autoScrollMinSize = autoScrollSize;

            return autoScrollMinSize;
        }
        #endregion Public Methods

        #region Private Methods
        private void InitDefaultValues()
        {
            TextColor = Color.Black;
            HeaderBackgroundColor = Color.LightBlue;
            BackgroundColor = Color.LightGray;
            TopHeaderHeight = 40;
            DefaultTimeLabelFormat = "htt";
            DefaultDayLabelFormat = "dddd";
            StartHourInDay = 0;
            EndHourInDay = 24;
            RowIconSize = new Size(15, 15);
            RowIconLocation = Corner.SW;
            HorizontalGridLinesVisible = false;
            VerticalGridLinesVisible = true;
            ShowNowIndicator = false;
            NowIndicatorHourOffset = 0;
            MinTimeIntervalWidth = 0;

            DayXLocations = new List<Tuple<DateTime, int, int>>();
            TimeXLocations = new List<Tuple<DateTime, int, StringFormat>>();
            TimeLabelFormats = new Dictionary<DateTime, string>();
            DayLabelFormats = new Dictionary<DateTime, string>();
            Rows = new List<Row>();
            Holidays = new Dictionary<DateTime, string>();
        }

        private void PopulateDateTimeXLocs(int startX, int endX)
        {
            int dayDivision = (int)Math.Round((double)(endX - startX) / (EndDate - StartDate).Days);
            int dayXLoc = startX;
            DateTime curDay = StartDate;
            while (curDay < EndDate)
            {
                int dayStart = dayXLoc;
                int dayEnd = dayXLoc + dayDivision;

                int timeDivision = (int)Math.Round((double)(dayEnd - dayStart) / (EndHourInDay - StartHourInDay));
                int xLoc = dayStart;
                int curHour = StartHourInDay;
                while (curHour <= EndHourInDay)
                {
                    DateTime dateWithHour = curDay.AddHours(curHour);

                    if (curHour == StartHourInDay)
                    {
                        StringFormat startFormat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far };
                        TimeXLocations.Add(new Tuple<DateTime, int, StringFormat>(dateWithHour, dayStart, startFormat));
                    }
                    else if (curHour == EndHourInDay)
                    {
                        StringFormat endFormat = new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
                        TimeXLocations.Add(new Tuple<DateTime, int, StringFormat>(dateWithHour, dayEnd, endFormat));
                    }
                    else
                    {
                        xLoc += timeDivision;
                        StringFormat otherFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
                        TimeXLocations.Add(new Tuple<DateTime, int, StringFormat>(dateWithHour, xLoc, otherFormat));
                    }

                    curHour++;
                }

                DayXLocations.Add(new Tuple<DateTime, int, int>(curDay, dayStart, dayEnd));
                dayXLoc = dayEnd;

                curDay = curDay.AddDays(1);
            }
        }

        private void DrawChart()
        {
            ResetGraphics();

            if (AreHoursValid() && AreDatesValid())
            {
                RecaculateFormatDictionaries();

                Rectangle leftRect = DrawLeftHeaders(0, TopHeaderHeight);

                PopulateDateTimeXLocs(leftRect.Right, autoScrollMinSize.Width);

                Rectangle topRect = DrawTopHeaders(leftRect.Right, 0, autoScrollMinSize.Width, TopHeaderHeight + 1);

                Rectangle mainCanvasRect = DrawMainCanvas(leftRect.Right, topRect.Bottom, autoScrollMinSize.Width, leftRect.Bottom + 1);
                MainCanvas = mainCanvasRect;

                DrawTimeBlocks(mainCanvasRect.Left, mainCanvasRect.Top, mainCanvasRect.Right, mainCanvasRect.Bottom);

                DrawHolidays(MainCanvas.Top, MainCanvas.Bottom + 2);

                DrawNowIndicator(topRect.Top, MainCanvas.Bottom + 2);
            }

            graphics.Flush();
        }

        private void ResetGraphics()
        {
            graphics.Clear(BackgroundColor);
            graphics.TranslateTransform(this.autoScrollPosition.X, this.autoScrollPosition.Y);

            DayXLocations.Clear();
            TimeXLocations.Clear();
        }

        private Rectangle DrawLeftHeaders(int startX, int startY)
        {
            int headerWidth = 100;
            int headerHeight = 30;

            if (Rows.Count == 0)
                return new Rectangle(startX, startY, 0, 0);

            //Increase width if it will not fit the largest string
            int maxStringWidth = Rows.Select(p => { return (int)Math.Round(graphics.MeasureString(p.Text, this.font).Width); }).Max();
            if (maxStringWidth > headerWidth)
                headerWidth = maxStringWidth + 10; //10 for a "margin"

            int x = startX;
            int y = startY;
            foreach (Row row in Rows.Where(p => p.IsVisible))
            {
                Rectangle headerRect = new Rectangle(x, y, headerWidth, headerHeight);
                DrawRect(headerRect, HeaderBackgroundColor, Color.Black);
                DrawTextCenter(headerRect, row.Text);

                if (row.Icon != null)
                {
                    Rectangle iconRect = GetIconRect(headerRect);
                    graphics.DrawImage(row.Icon, iconRect);
                    row.IconRect = iconRect;
                }

                row.Rect = headerRect;

                y += headerRect.Height;

                //Increase AutoScrollMinSize (chart total area) if we overflow the end of the current area
                if (y > autoScrollMinSize.Height)
                    autoScrollMinSize = new Size(autoScrollMinSize.Width, y + 1); //Add 1 for pen width
            }

            return new Rectangle(startX, startY, headerWidth - startX, y - startY); //Return the total rect that the rows take up
        }

        private Rectangle GetIconRect(Rectangle headerRect)
        {
            int x, y;

            switch (RowIconLocation)
            {
                case Corner.NW:
                    x = headerRect.Left;
                    y = headerRect.Top;
                    break;
                case Corner.NE:
                    x = headerRect.Right - RowIconSize.Width;
                    y = headerRect.Top;
                    break;
                case Corner.SE:
                    x = headerRect.Right - RowIconSize.Width;
                    y = headerRect.Bottom - RowIconSize.Height;
                    break;
                default:
                case Corner.SW:
                    x = headerRect.Left;
                    y = headerRect.Bottom - RowIconSize.Height;
                    break;
            }

            Rectangle iconRect = new Rectangle(x, y,
                                   RowIconSize.Width,
                                   RowIconSize.Height);

            return iconRect;
        }

        private Rectangle DrawTopHeaders(int startX, int startY, int endX, int endY)
        {
            Pen outlinePen = new Pen(Color.Black, 1);

            foreach (Tuple<DateTime, int, int> day in DayXLocations)
            {
                //Draw day rect
                Rectangle dayRect = new Rectangle(day.Item2, startY, day.Item3 - day.Item2, endY - startY);
                dayRect.Height -= (int)outlinePen.Width;
                graphics.DrawRectangle(outlinePen, dayRect);

                //Draw day header
                Point dayCenter = new Point(day.Item2 + dayRect.Width / 2, startY + dayRect.Height / 4);
                graphics.DrawString(day.Item1.ToString(DayLabelFormats[day.Item1]), this.font, TextBrush, dayCenter, alignAllCenter);
            }

            foreach (Tuple<DateTime, int, StringFormat> timeLabelInfo in TimeXLocations)
            {
                string timeLabel = GetLabelForTime(timeLabelInfo.Item1);
                Point labelLoc = new Point(timeLabelInfo.Item2, endY);
                graphics.DrawString(timeLabel, this.font, TextBrush, labelLoc, timeLabelInfo.Item3);
            }

            //Adjust rect for return
            Rectangle topRect = new Rectangle(startX, startY, endX - startX, endY - startY);
            topRect.Height -= (int)outlinePen.Width;

            return topRect;
        }

        private Rectangle DrawMainCanvas(int startX, int startY, int endX, int endY)
        {
            Pen outlinePen = new Pen(Color.Black, 1);
            Rectangle canvasRect = new Rectangle(startX, startY, endX - startX, endY - startY);
            graphics.DrawRectangle(outlinePen, startX, startY, canvasRect.Width - outlinePen.Width, canvasRect.Height - outlinePen.Width);

            if (VerticalGridLinesVisible)
                DrawVerticalGridLines(startX, startY, endX, endY);

            if (HorizontalGridLinesVisible)
                DrawHorizontalGridLines(endX);

            //Adjust canvas for return (so that anything drawn to the canvas is inside the "outline")
            canvasRect.X += (int)outlinePen.Width;
            canvasRect.Width -= (int)outlinePen.Width * 3;
            canvasRect.Y += (int)outlinePen.Width;
            canvasRect.Height -= (int)outlinePen.Width * 3;

            return canvasRect;
        }

        private void DrawTimeBlocks(int startX, int startY, int endX, int endY)
        {
            foreach (Row row in Rows.Where(p => p.IsVisible))
            {
                foreach (TimeBlock timeBlock in row.TimeBlocks.Where(p => p.IsVisible))
                {
                    //If timeblock is not within visible range, don't draw it
                    if (timeBlock.StartTime < StartDate ||
                        timeBlock.StartTime.Hour < StartHourInDay ||
                        timeBlock.EndTime.Hour > EndHourInDay ||
                        timeBlock.EndTime >= EndDate)
                    {
                        continue;
                    }

                    //If timeblock is on a holiday, don't draw it
                    if (Holidays.Keys.ToList().Find(p => p.Date == timeBlock.StartTime.Date) != DateTime.MinValue)
                        continue;

                    int timeBlockStartX = GetXLocationForTime(timeBlock.StartTime) + 1;
                    int timeBlockEndX = GetXLocationForTime(timeBlock.EndTime) - 1;
                    int timeBlockY = row.Rect.Y + 5;
                    Rectangle rect = new Rectangle(timeBlockStartX, timeBlockY, timeBlockEndX - timeBlockStartX, 20);
                    DrawRect(rect, timeBlock.Color, timeBlock.Color, hatch: timeBlock.Hatch);

                    timeBlock.Rect = rect;

                    DrawTextLeft(rect, timeBlock.Text, GetContrastingTextColor(timeBlock.Color));
                }
            }
        }

        private void DrawHolidays(int startY, int endY)
        {
            //Don't draw holidays if there are no rows (because the holiday "block" takes up the height of the row)
            if (Rows.Count == 0)
                return;

            foreach (KeyValuePair<DateTime, string> holiday in Holidays)
            {
                //If holiday is not within visible range, don't draw it
                if (holiday.Key < StartDate ||
                    holiday.Key >= EndDate)
                {
                    continue;
                }

                Tuple<DateTime, int, int> dayLocInfo = DayXLocations.Find(p => p.Item1 == holiday.Key.Date);
                if (dayLocInfo != null)
                {
                    Rectangle holidayRect = new Rectangle(dayLocInfo.Item2,
                                                               startY,
                                                               dayLocInfo.Item3 - dayLocInfo.Item2,
                                                               endY - startY);

                    graphics.FillRectangle(new HatchBrush(HatchStyle.ForwardDiagonal, Color.White, Color.DimGray), holidayRect);

                    //Write holiday name
                    if (!string.IsNullOrEmpty(holiday.Value))
                    {
                        Font holidayNameFont = new Font(this.font, FontStyle.Bold);
                        Point holidayRectCenter = new Point(holidayRect.X + holidayRect.Width / 2, startY + 5);
                        int textWidth = (int)Math.Round(graphics.MeasureString(holiday.Value, holidayNameFont).Width);
                        int textHeight = (int)Math.Round(graphics.MeasureString(holiday.Value, holidayNameFont).Height);
                        Rectangle holidayNameRect = new Rectangle(holidayRectCenter.X - textWidth / 2 - 2, //"-2" for left margin of 2
                                                                  holidayRectCenter.Y,
                                                                  textWidth + 4, //"+4" for right margin of 2 (width also includes left margin)
                                                                  textHeight);

                        graphics.FillRectangle(new SolidBrush(Color.White), holidayNameRect);
                        Point stringLoc = new Point(holidayRectCenter.X, holidayRectCenter.Y + 7);
                        graphics.DrawString(holiday.Value, holidayNameFont, new SolidBrush(Color.Black), stringLoc, alignAllCenter);
                    }
                }
            }
        }

        private void DrawNowIndicator(int startY, int endY)
        {
            if (!ShowNowIndicator ||
                Rows.Count == 0 || //No rows
                !Rows.Any(p => p.TimeBlocks.Count > 0 && p.IsVisible)) //No timeblocks (and isn't invisible)
                return;

            DateTime now = DateTime.Now;
            now = now.AddHours(NowIndicatorHourOffset);

            //Ensure that "now" should be visible on chart
            if (now.Hour < StartHourInDay ||
                now.Hour > EndHourInDay ||
                now < StartDate ||
                now > EndDate)
                return;

            Tuple<DateTime, int, StringFormat> timeBehind = TimeXLocations.LastOrDefault(p => p.Item1 <= now);
            Tuple<DateTime, int, StringFormat> timeAhead = TimeXLocations.FirstOrDefault(p => p.Item1 >= now);

            double percentageOfTimeBlock = (double)(now.Ticks - timeBehind.Item1.Ticks) / (timeAhead.Item1.Ticks - timeBehind.Item1.Ticks);
            int nowXLoc = (int)Math.Round(percentageOfTimeBlock * (timeAhead.Item2 - timeBehind.Item2) + timeBehind.Item2);

            //Draw line
            Point topPoint = new Point(nowXLoc, startY);
            Point bottomPoint = new Point(nowXLoc, endY);
            graphics.DrawLine(new Pen(Color.Red), topPoint, bottomPoint);

            //Draw top triangle
            Point t1 = new Point(topPoint.X - 5, topPoint.Y);
            Point t2 = new Point(topPoint.X + 5, topPoint.Y);
            Point t3 = new Point(topPoint.X, topPoint.Y + 10);
            graphics.FillPolygon(new SolidBrush(Color.Red), new[] { t1, t2, t3 });

            //Draw bottom triangle
            Point b1 = new Point(bottomPoint.X - 5, bottomPoint.Y);
            Point b2 = new Point(bottomPoint.X + 5, bottomPoint.Y);
            Point b3 = new Point(bottomPoint.X, bottomPoint.Y - 10);
            graphics.FillPolygon(new SolidBrush(Color.Red), new[] { b1, b2, b3 });
        }

        private string GetLabelForTime(DateTime dateAndHour)
        {
            return dateAndHour.ToString(TimeLabelFormats[dateAndHour]).ToLower();
        }

        private bool AreHoursValid()
        {
            if (StartHourInDay < 0 ||
                StartHourInDay > 23)
            {
                throw new Exception("StartHourInDay is not valid");
            }
            else if (EndHourInDay < 0 ||
                    EndHourInDay > 24 ||
                    EndHourInDay <= StartHourInDay)
            {
                throw new Exception("EndHourInDay is not valid");
            }
            else
                return true;
        }

        private bool AreDatesValid()
        {
            if (EndDate <= StartDate)
                throw new Exception("EndDate cannot be before or the same as StartDate");
            else
                return true;
        }

        private int GetXLocationForDay(DateTime date)
        {
            Tuple<DateTime, int, int> foundGroup = DayXLocations.Find(p => p.Item1 == date.Date);
            if (foundGroup != null)
                return foundGroup.Item2;
            else
                return -1;
        }

        private int GetXLocationForTime(DateTime dateAndTime)
        {
            Tuple<DateTime, int, StringFormat> foundGroup = TimeXLocations.Find(p => p.Item1 == dateAndTime);
            if (foundGroup != null)
                return foundGroup.Item2;
            else
                return -1;
        }

        private void DrawVerticalGridLines(int startX, int startY, int endX, int endY)
        {
            foreach (Tuple<DateTime, int, StringFormat> time in TimeXLocations)
            {
                Point p1 = new Point(time.Item2, startY);
                Point p2 = new Point(time.Item2, endY - 1);
                graphics.DrawLine(new Pen(Color.Black), p1, p2);
            }
        }

        private void DrawHorizontalGridLines(int endX)
        {
            foreach (Row row in Rows)
            {
                //Draw a line from the bottom of each row rect (except for the last one)
                if (Rows.IndexOf(row) != Rows.Count - 1)
                {
                    Rectangle rowRect = row.Rect;

                    Point p1 = new Point(rowRect.Right, rowRect.Bottom);
                    Point p2 = new Point(endX, rowRect.Bottom);
                    graphics.DrawLine(new Pen(Color.Black), p1, p2);
                }
            }
        }

        private Color GetContrastingTextColor(Color backgroundColor)
        {
            int avg = (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3;
            if (avg > 255 / 2)
                return Color.Black;
            else
                return Color.White;
        }

        private void DrawRect(Rectangle rect, Color insideColor, Color borderColor, bool offsetForPen = false, bool hatch = false)
        {
            Brush fillBrush = new SolidBrush(insideColor);
            if (hatch)
                fillBrush = new HatchBrush(HatchStyle.ForwardDiagonal, Color.White, insideColor);

            Pen outlinePen = new Pen(borderColor);

            graphics.FillRectangle(fillBrush, rect);

            if (offsetForPen)
                graphics.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width - outlinePen.Width, rect.Height - outlinePen.Width);
            else
                graphics.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void DrawTextCenter(Rectangle rect, string text, Color? textColor = null)
        {
            int stringLengthPixels = (int)Math.Round(graphics.MeasureString(text, this.font).Width);
            if (stringLengthPixels > rect.Width - 3) //"3" leaves room for margin on both sides
            {
                int convertedLength = (int)((double)(rect.Width - 5) / stringLengthPixels * text.Length);
                if (convertedLength < 0)
                    text = "";
                else
                    text = text.Substring(0, convertedLength) + "..";
            }

            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            if (textColor.HasValue)
                graphics.DrawString(text, this.font, new SolidBrush(textColor.Value), center.X, center.Y, alignAllCenter);
            else
                graphics.DrawString(text, this.font, TextBrush, center.X, center.Y, alignAllCenter);
        }

        private void DrawTextLeft(Rectangle rect, string text, Color? textColor = null)
        {
            int stringLengthPixels = (int)Math.Round(graphics.MeasureString(text, this.font).Width);
            if (stringLengthPixels > rect.Width - 3) //"3" leaves room for margin on both sides
            {
                int convertedLength = (int)((double)(rect.Width - 5) / stringLengthPixels * text.Length);
                if (convertedLength < 0)
                    text = "";
                else
                    text = text.Substring(0, convertedLength) + "..";
            }

            Point center = new Point(rect.X + 3, rect.Y + rect.Height / 2);

            if (textColor.HasValue)
                graphics.DrawString(text, this.font, new SolidBrush(textColor.Value), center.X, center.Y, alignVertCenter);
            else
                graphics.DrawString(text, this.font, TextBrush, center.X, center.Y, alignVertCenter);
        }

        private void RecaculateFormatDictionaries(bool overwrite = false)
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
        #endregion Private Methods

        #region Public Properties
        public Rectangle MainCanvas;
        public SolidBrush TextBrush { get { return new SolidBrush(TextColor); } }
        public Color TextColor { get; set; }
        public Color HeaderBackgroundColor { get; set; }
        public Color BackgroundColor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int StartHourInDay { get; set; }
        public int EndHourInDay { get; set; }
        public List<Tuple<DateTime, int, int>> DayXLocations { get; set; } //Format is "Date (without hour), xStartLocation, xEndLocation"
        public List<Tuple<DateTime, int, StringFormat>> TimeXLocations { get; set; } //Format is "Date (with hour), xLocation, alignment"
        public Dictionary<DateTime, string> TimeLabelFormats { get; set; }
        public Dictionary<DateTime, string> DayLabelFormats { get; set; }
        public Dictionary<DateTime, string> Holidays { get; set; }
        public List<Row> Rows { get; set; }
        public int TopHeaderHeight { get; set; }
        public string DefaultTimeLabelFormat { get; set; }
        public string DefaultDayLabelFormat { get; set; }
        public Size RowIconSize { get; set; }
        public Corner RowIconLocation { get; set; }
        public bool HorizontalGridLinesVisible { get; set; }
        public bool VerticalGridLinesVisible { get; set; }
        public bool ShowNowIndicator { get; set; }
        public int NowIndicatorHourOffset { get; set; }
        public int MinTimeIntervalWidth { get; set; }
        #endregion Public Properties
    }
}
