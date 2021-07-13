Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports GanttChart.Enums

Namespace GanttChart
    Public Class Renderer
#Region "Private Properties"
        Private graphics As Graphics
        Private autoScrollPosition As Point
        Private autoScrollMinSize As Size
        Private alignAllCenter As StringFormat = New StringFormat() With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }
        Private alignVertCenter As StringFormat = New StringFormat() With {
            .LineAlignment = StringAlignment.Center
        }
        Private font As Font
#End Region

#Region "Constructor"
        Public Sub New()
            InitDefaultValues()
        End Sub
#End Region

#Region "Public Methods"
        Public Sub Render(g As Graphics, autoScrollPosition As Point, autoScrollSize As Size, font As Font)
            graphics = g
            Me.autoScrollPosition = autoScrollPosition
            autoScrollMinSize = autoScrollSize
            Me.font = font
            DrawChart()
        End Sub

        Public Function CalculateAutoScrollSize(autoScrollMinSize As Size, size As Size, verticalScroll As VScrollProperties) As Size
            Dim autoScrollSize = Me.autoScrollMinSize

            'Dynamically adjust width
            If verticalScroll.Visible Then
                autoScrollSize.Width = size.Width - SystemInformation.VerticalScrollBarWidth
            Else
                autoScrollSize.Width = size.Width
            End If

            Dim timeDivisions = (EndHourInDay - StartHourInDay) * (EndDate - StartDate).Days
            Dim pixelsPerDivision As Integer = autoScrollSize.Width / timeDivisions

            If pixelsPerDivision < MinTimeIntervalWidth Then
                If verticalScroll.Visible Then
                    autoScrollSize.Width = timeDivisions * MinTimeIntervalWidth - SystemInformation.VerticalScrollBarWidth
                Else
                    autoScrollSize.Width = timeDivisions * MinTimeIntervalWidth
                End If
            End If

            'Dynamically adjust height
            If Rows.Count > 0 AndAlso Rows.All(Function(p) p.Rect <> Nothing) Then
                Dim highestYValue As Integer = Enumerable.Select(Of Row, Global.System.Int32)(Rows, CType(Function(p) CInt(p.Rect.Bottom), Func(Of Row, Integer))).Max()
                autoScrollSize.Height = highestYValue + 1
            Else
                autoScrollSize.Height = size.Height
            End If

            If autoScrollMinSize <> autoScrollSize Then autoScrollMinSize = autoScrollSize
            Return autoScrollMinSize
        End Function
#End Region

#Region "Private Methods"
        Private Sub InitDefaultValues()
            TextColor = Color.Black
            HeaderBackgroundColor = Color.LightBlue
            BackgroundColor = Color.LightGray
            TopHeaderHeight = 40
            DefaultTimeLabelFormat = "htt"
            DefaultDayLabelFormat = "dddd"
            StartHourInDay = 0
            EndHourInDay = 24
            RowIconSize = New Size(15, 15)
            RowIconLocation = Corner.SW
            HorizontalGridLinesVisible = False
            VerticalGridLinesVisible = True
            ShowNowIndicator = False
            NowIndicatorHourOffset = 0
            MinTimeIntervalWidth = 0
            DayXLocations = New List(Of Tuple(Of Date, Integer, Integer))()
            TimeXLocations = New List(Of Tuple(Of Date, Integer, StringFormat))()
            TimeLabelFormats = New Dictionary(Of Date, String)()
            DayLabelFormats = New Dictionary(Of Date, String)()
            Rows = New List(Of Row)()
            Holidays = New Dictionary(Of Date, String)()
        End Sub

        Private Sub PopulateDateTimeXLocs(startX As Integer, endX As Integer)
            Dim dayDivision As Integer = Math.Round((endX - startX) / (EndDate - StartDate).Days)
            Dim dayXLoc = startX
            Dim curDay = StartDate

            While curDay < EndDate
                Dim dayStart = dayXLoc
                Dim dayEnd = dayXLoc + dayDivision
                Dim timeDivision As Integer = Math.Round((dayEnd - dayStart) / (EndHourInDay - StartHourInDay))
                Dim xLoc = dayStart
                Dim curHour = StartHourInDay

                While curHour <= EndHourInDay
                    Dim dateWithHour = curDay.AddHours(curHour)

                    If curHour = StartHourInDay Then
                        Dim startFormat As StringFormat = New StringFormat() With {
                            .Alignment = StringAlignment.Near,
                            .LineAlignment = StringAlignment.Far
                        }
                        TimeXLocations.Add(New Tuple(Of Date, Integer, StringFormat)(dateWithHour, dayStart, startFormat))
                    ElseIf curHour = EndHourInDay Then
                        Dim endFormat As StringFormat = New StringFormat() With {
                            .Alignment = StringAlignment.Far,
                            .LineAlignment = StringAlignment.Far
                        }
                        TimeXLocations.Add(New Tuple(Of Date, Integer, StringFormat)(dateWithHour, dayEnd, endFormat))
                    Else
                        xLoc += timeDivision
                        Dim otherFormat As StringFormat = New StringFormat() With {
                            .Alignment = StringAlignment.Center,
                            .LineAlignment = StringAlignment.Far
                        }
                        TimeXLocations.Add(New Tuple(Of Date, Integer, StringFormat)(dateWithHour, xLoc, otherFormat))
                    End If

                    curHour += 1
                End While

                DayXLocations.Add(New Tuple(Of Date, Integer, Integer)(curDay, dayStart, dayEnd))
                dayXLoc = dayEnd
                curDay = curDay.AddDays(1)
            End While
        End Sub

        Private Sub DrawChart()
            ResetGraphics()

            If AreHoursValid() AndAlso AreDatesValid() Then
                RecaculateFormatDictionaries()
                Dim leftRect = DrawLeftHeaders(0, TopHeaderHeight)
                PopulateDateTimeXLocs(leftRect.Right, autoScrollMinSize.Width)
                Dim topRect = DrawTopHeaders(leftRect.Right, 0, autoScrollMinSize.Width, TopHeaderHeight + 1)
                Dim mainCanvasRect = DrawMainCanvas(leftRect.Right, topRect.Bottom, autoScrollMinSize.Width, leftRect.Bottom + 1)
                MainCanvas = mainCanvasRect
                DrawTimeBlocks(mainCanvasRect.Left, mainCanvasRect.Top, mainCanvasRect.Right, mainCanvasRect.Bottom)
                DrawHolidays(MainCanvas.Top, MainCanvas.Bottom + 2)
                DrawNowIndicator(topRect.Top, MainCanvas.Bottom + 2)
            End If

            graphics.Flush()
        End Sub

        Private Sub ResetGraphics()
            graphics.Clear(BackgroundColor)
            graphics.TranslateTransform(autoScrollPosition.X, autoScrollPosition.Y)
            DayXLocations.Clear()
            TimeXLocations.Clear()
        End Sub

        Private Function DrawLeftHeaders(startX As Integer, startY As Integer) As Rectangle
            Dim headerWidth = 100
            Dim headerHeight = 30
            If Rows.Count = 0 Then Return New Rectangle(startX, startY, 0, 0)

            'Increase width if it will not fit the largest string
            Dim maxStringWidth As Integer = Enumerable.Select(Of Row, Global.System.Int32)(Rows, CType(Function(p) CInt(CInt(Math.Round(CDbl(graphics.MeasureString(CStr(p.Text), CType(font, Font)).Width)))), Func(Of Row, Integer))).Max()
            If maxStringWidth > headerWidth Then headerWidth = maxStringWidth + 10 '10 for a "margin"
            Dim x = startX
            Dim y = startY

            For Each row In Rows.Where(Function(p) p.IsVisible)
                Dim headerRect As Rectangle = New Rectangle(x, y, headerWidth, headerHeight)
                DrawRect(headerRect, HeaderBackgroundColor, Color.Black)
                DrawTextCenter(headerRect, row.Text)

                If row.Icon IsNot Nothing Then
                    Dim iconRect = GetIconRect(headerRect)
                    graphics.DrawImage(row.Icon, iconRect)
                    row.IconRect = iconRect
                End If

                row.Rect = headerRect
                y += headerRect.Height

                'Increase AutoScrollMinSize (chart total area) if we overflow the end of the current area
                If y > autoScrollMinSize.Height Then autoScrollMinSize = New Size(autoScrollMinSize.Width, y + 1) 'Add 1 for pen width
            Next

            Return New Rectangle(startX, startY, headerWidth - startX, y - startY) 'Return the total rect that the rows take up
        End Function

        Private Function GetIconRect(headerRect As Rectangle) As Rectangle
            Dim x, y As Integer

            Select Case RowIconLocation
                Case Corner.NW
                    x = headerRect.Left
                    y = headerRect.Top
                Case Corner.NE
                    x = headerRect.Right - RowIconSize.Width
                    y = headerRect.Top
                Case Corner.SE
                    x = headerRect.Right - RowIconSize.Width
                    y = headerRect.Bottom - RowIconSize.Height
                Case Else
                    x = headerRect.Left
                    y = headerRect.Bottom - RowIconSize.Height
            End Select

            Dim iconRect As Rectangle = New Rectangle(x, y, RowIconSize.Width, RowIconSize.Height)
            Return iconRect
        End Function

        Private Function DrawTopHeaders(startX As Integer, startY As Integer, endX As Integer, endY As Integer) As Rectangle
            Dim outlinePen As Pen = New Pen(Color.Black, 1)

            For Each day In DayXLocations
                'Draw day rect
                Dim dayRect As Rectangle = New Rectangle(day.Item2, startY, day.Item3 - day.Item2, endY - startY)
                dayRect.Height -= CInt(outlinePen.Width)
                graphics.DrawRectangle(outlinePen, dayRect)

                'Draw day header
                Dim dayCenter As Point = New Point(day.Item2 + dayRect.Width / 2, startY + dayRect.Height / 4)
                graphics.DrawString(day.Item1.ToString(DayLabelFormats(day.Item1)), font, TextBrush, dayCenter, alignAllCenter)
            Next

            For Each timeLabelInfo In TimeXLocations
                Dim timeLabel = GetLabelForTime(timeLabelInfo.Item1)
                Dim labelLoc As Point = New Point(timeLabelInfo.Item2, endY)
                graphics.DrawString(timeLabel, font, TextBrush, labelLoc, timeLabelInfo.Item3)
            Next

            'Adjust rect for return
            Dim topRect As Rectangle = New Rectangle(startX, startY, endX - startX, endY - startY)
            topRect.Height -= CInt(outlinePen.Width)
            Return topRect
        End Function

        Private Function DrawMainCanvas(startX As Integer, startY As Integer, endX As Integer, endY As Integer) As Rectangle
            Dim outlinePen As Pen = New Pen(Color.Black, 1)
            Dim canvasRect As Rectangle = New Rectangle(startX, startY, endX - startX, endY - startY)
            graphics.DrawRectangle(outlinePen, startX, startY, canvasRect.Width - outlinePen.Width, canvasRect.Height - outlinePen.Width)
            If VerticalGridLinesVisible Then DrawVerticalGridLines(startX, startY, endX, endY)
            If HorizontalGridLinesVisible Then DrawHorizontalGridLines(endX)

            'Adjust canvas for return (so that anything drawn to the canvas is inside the "outline")
            canvasRect.X += CInt(outlinePen.Width)
            canvasRect.Width -= CInt(outlinePen.Width) * 3
            canvasRect.Y += CInt(outlinePen.Width)
            canvasRect.Height -= CInt(outlinePen.Width) * 3
            Return canvasRect
        End Function

        Private Sub DrawTimeBlocks(startX As Integer, startY As Integer, endX As Integer, endY As Integer)
            For Each row In Rows.Where(Function(p) p.IsVisible)

                For Each timeBlock In row.TimeBlocks.Where(Function(p) p.IsVisible)
                    'If timeblock is not within visible range, don't draw it
                    If timeBlock.StartTime < StartDate OrElse timeBlock.StartTime.Hour < StartHourInDay OrElse timeBlock.EndTime.Hour > EndHourInDay OrElse timeBlock.EndTime >= EndDate Then
                        Continue For
                    End If

                    'If timeblock is on a holiday, don't draw it
                    If Enumerable.ToList(Holidays.Keys).Find(Function(p) p.Date = timeBlock.StartTime.Date) <> Date.MinValue Then Continue For
                    Dim timeBlockStartX = GetXLocationForTime(timeBlock.StartTime) + 1
                    Dim timeBlockEndX = GetXLocationForTime(timeBlock.EndTime) - 1
                    Dim timeBlockY = row.Rect.Y + 5
                    Dim rect As Rectangle = New Rectangle(timeBlockStartX, timeBlockY, timeBlockEndX - timeBlockStartX, 20)
                    DrawRect(rect, timeBlock.Color, timeBlock.Color, hatch:=timeBlock.Hatch)
                    timeBlock.Rect = rect
                    DrawTextLeft(rect, timeBlock.Text, GetContrastingTextColor(timeBlock.Color))
                Next
            Next
        End Sub

        Private Sub DrawHolidays(startY As Integer, endY As Integer)
            'Don't draw holidays if there are no rows (because the holiday "block" takes up the height of the row)
            If Rows.Count = 0 Then Return

            For Each holiday In Holidays
                'If holiday is not within visible range, don't draw it
                If holiday.Key < StartDate OrElse holiday.Key >= EndDate Then
                    Continue For
                End If

                Dim dayLocInfo = DayXLocations.Find(Function(p) p.Item1 = holiday.Key.Date)

                If dayLocInfo IsNot Nothing Then
                    Dim holidayRect As Rectangle = New Rectangle(dayLocInfo.Item2, startY, dayLocInfo.Item3 - dayLocInfo.Item2, endY - startY)
                    graphics.FillRectangle(New HatchBrush(HatchStyle.ForwardDiagonal, Color.White, Color.DimGray), holidayRect)

                    'Write holiday name
                    If Not String.IsNullOrEmpty(holiday.Value) Then
                        Dim holidayNameFont As Font = New Font(font, FontStyle.Bold)
                        Dim holidayRectCenter As Point = New Point(holidayRect.X + holidayRect.Width / 2, startY + 5)
                        Dim textWidth As Integer = Math.Round(graphics.MeasureString(holiday.Value, holidayNameFont).Width)
                        Dim textHeight As Integer = Math.Round(graphics.MeasureString(holiday.Value, holidayNameFont).Height)
                        Dim holidayNameRect As Rectangle = New Rectangle(holidayRectCenter.X - textWidth / 2 - 2, holidayRectCenter.Y, textWidth + 4, textHeight) '"-2" for left margin of 2
                        '"+4" for right margin of 2 (width also includes left margin)
                        graphics.FillRectangle(New SolidBrush(Color.White), holidayNameRect)
                        Dim stringLoc As Point = New Point(holidayRectCenter.X, holidayRectCenter.Y + 7)
                        graphics.DrawString(holiday.Value, holidayNameFont, New SolidBrush(Color.Black), stringLoc, alignAllCenter)
                    End If
                End If
            Next
        End Sub

        Private Sub DrawNowIndicator(startY As Integer, endY As Integer)
            If Not ShowNowIndicator OrElse Rows.Count = 0 OrElse Not Rows.Any(Function(p) p.TimeBlocks.Count > 0 AndAlso p.IsVisible) Then Return 'No rows
            'No timeblocks (and isn't invisible)
            Dim now = Date.Now
            now = now.AddHours(NowIndicatorHourOffset)

            'Ensure that "now" should be visible on chart
            If now.Hour < StartHourInDay OrElse now.Hour > EndHourInDay OrElse now < StartDate OrElse now > EndDate Then Return
            Dim timeBehind = TimeXLocations.LastOrDefault(Function(p) p.Item1 <= now)
            Dim timeAhead = TimeXLocations.FirstOrDefault(Function(p) p.Item1 >= now)
            Dim percentageOfTimeBlock = (now.Ticks - timeBehind.Item1.Ticks) / (timeAhead.Item1.Ticks - timeBehind.Item1.Ticks)
            Dim nowXLoc As Integer = Math.Round(percentageOfTimeBlock * (timeAhead.Item2 - timeBehind.Item2) + timeBehind.Item2)

            'Draw line
            Dim topPoint As Point = New Point(nowXLoc, startY)
            Dim bottomPoint As Point = New Point(nowXLoc, endY)
            graphics.DrawLine(New Pen(Color.Red), topPoint, bottomPoint)

            'Draw top triangle
            Dim t1 As Point = New Point(topPoint.X - 5, topPoint.Y)
            Dim t2 As Point = New Point(topPoint.X + 5, topPoint.Y)
            Dim t3 As Point = New Point(topPoint.X, topPoint.Y + 10)
            graphics.FillPolygon(New SolidBrush(Color.Red), {t1, t2, t3})

            'Draw bottom triangle
            Dim b1 As Point = New Point(bottomPoint.X - 5, bottomPoint.Y)
            Dim b2 As Point = New Point(bottomPoint.X + 5, bottomPoint.Y)
            Dim b3 As Point = New Point(bottomPoint.X, bottomPoint.Y - 10)
            graphics.FillPolygon(New SolidBrush(Color.Red), {b1, b2, b3})
        End Sub

        Private Function GetLabelForTime(dateAndHour As Date) As String
            Return dateAndHour.ToString(TimeLabelFormats(dateAndHour)).ToLower()
        End Function

        Private Function AreHoursValid() As Boolean
            If StartHourInDay < 0 OrElse StartHourInDay > 23 Then
                Throw New Exception("StartHourInDay is not valid")
            ElseIf EndHourInDay < 0 OrElse EndHourInDay > 24 OrElse EndHourInDay <= StartHourInDay Then
                Throw New Exception("EndHourInDay is not valid")
            Else
                Return True
            End If
        End Function

        Private Function AreDatesValid() As Boolean
            If EndDate <= StartDate Then
                Throw New Exception("EndDate cannot be before or the same as StartDate")
            Else
                Return True
            End If
        End Function

        Private Function GetXLocationForDay([date] As Date) As Integer
            Dim foundGroup = DayXLocations.Find(Function(p) p.Item1 = [date].Date)

            If foundGroup IsNot Nothing Then
                Return foundGroup.Item2
            Else
                Return -1
            End If
        End Function

        Private Function GetXLocationForTime(dateAndTime As Date) As Integer
            Dim foundGroup = TimeXLocations.Find(Function(p) p.Item1 = dateAndTime)

            If foundGroup IsNot Nothing Then
                Return foundGroup.Item2
            Else
                Return -1
            End If
        End Function

        Private Sub DrawVerticalGridLines(startX As Integer, startY As Integer, endX As Integer, endY As Integer)
            For Each time In TimeXLocations
                Dim p1 As Point = New Point(time.Item2, startY)
                Dim p2 As Point = New Point(time.Item2, endY - 1)
                graphics.DrawLine(New Pen(Color.Black), p1, p2)
            Next
        End Sub

        Private Sub DrawHorizontalGridLines(endX As Integer)
            For Each row In Rows
                'Draw a line from the bottom of each row rect (except for the last one)
                If Rows.IndexOf(row) <> Rows.Count - 1 Then
                    Dim rowRect = row.Rect
                    Dim p1 As Point = New Point(rowRect.Right, rowRect.Bottom)
                    Dim p2 As Point = New Point(endX, rowRect.Bottom)
                    graphics.DrawLine(New Pen(Color.Black), p1, p2)
                End If
            Next
        End Sub

        Private Function GetContrastingTextColor(backgroundColor As Color) As Color
            Dim avg As Double = Math.Round((CInt(backgroundColor.R) + CInt(backgroundColor.G) + CInt(backgroundColor.B)) / 3)
            'Dim avg As Double = (255 + 255 + 0) \ 3

            If avg > 255 \ 2 Then
                Return Color.Black
            Else
                Return Color.White
            End If
        End Function

        Private Sub DrawRect(rect As Rectangle, insideColor As Color, borderColor As Color, Optional offsetForPen As Boolean = False, Optional hatch As Boolean = False)
            Dim fillBrush As Brush = New SolidBrush(insideColor)
            If hatch Then fillBrush = New HatchBrush(HatchStyle.ForwardDiagonal, Color.White, insideColor)
            Dim outlinePen As Pen = New Pen(borderColor)
            graphics.FillRectangle(fillBrush, rect)

            If offsetForPen Then
                graphics.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width - outlinePen.Width, rect.Height - outlinePen.Width)
            Else
                graphics.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width, rect.Height)
            End If
        End Sub

        Private Sub DrawTextCenter(rect As Rectangle, text As String, Optional textColor As Color? = Nothing)
            Dim stringLengthPixels As Integer = Math.Round(graphics.MeasureString(text, font).Width)

            If stringLengthPixels > rect.Width - 3 Then '"3" leaves room for margin on both sides
                Dim convertedLength As Integer = (rect.Width - 5) / stringLengthPixels * text.Length

                If convertedLength < 0 Then
                    text = ""
                Else
                    text = text.Substring(0, convertedLength) & ".."
                End If
            End If

            Dim center As Point = New Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2)

            If textColor.HasValue Then
                graphics.DrawString(text, font, New SolidBrush(textColor.Value), center.X, center.Y, alignAllCenter)
            Else
                graphics.DrawString(text, font, TextBrush, center.X, center.Y, alignAllCenter)
            End If
        End Sub

        Private Sub DrawTextLeft(rect As Rectangle, text As String, Optional textColor As Color? = Nothing)
            Dim stringLengthPixels As Integer = Math.Round(graphics.MeasureString(text, font).Width)

            If stringLengthPixels > rect.Width - 3 Then '"3" leaves room for margin on both sides
                Dim convertedLength As Integer = (rect.Width - 5) / stringLengthPixels * text.Length

                If convertedLength < 0 Then
                    text = ""
                Else
                    text = text.Substring(0, convertedLength) & ".."
                End If
            End If

            Dim center As Point = New Point(rect.X + 3, rect.Y + rect.Height / 2)

            If textColor.HasValue Then
                graphics.DrawString(text, font, New SolidBrush(textColor.Value), center.X, center.Y, alignVertCenter)
            Else
                graphics.DrawString(text, font, TextBrush, center.X, center.Y, alignVertCenter)
            End If
        End Sub

        Private Sub RecaculateFormatDictionaries(Optional overwrite As Boolean = False)
            If overwrite Then
                DayLabelFormats.Clear()
                TimeLabelFormats.Clear()
            End If

            Dim curDay = StartDate

            While curDay < EndDate
                Dim curHour = StartHourInDay

                While curHour <= EndHourInDay
                    Dim dateWithHour = curDay.AddHours(curHour)
                    If Not TimeLabelFormats.ContainsKey(dateWithHour) Then TimeLabelFormats.Add(dateWithHour, DefaultTimeLabelFormat)
                    curHour += 1
                End While

                If Not DayLabelFormats.ContainsKey(curDay) Then DayLabelFormats.Add(curDay, DefaultDayLabelFormat)
                curDay = curDay.AddDays(1)
            End While
        End Sub
#End Region

#Region "Public Properties"
        Public MainCanvas As Rectangle

        Public ReadOnly Property TextBrush As SolidBrush
            Get
                Return New SolidBrush(TextColor)
            End Get
        End Property

        Public Property TextColor As Color
        Public Property HeaderBackgroundColor As Color
        Public Property BackgroundColor As Color
        Public Property StartDate As Date
        Public Property EndDate As Date
        Public Property StartHourInDay As Integer
        Public Property EndHourInDay As Integer
        Public Property DayXLocations As List(Of Tuple(Of Date, Integer, Integer)) 'Format is "Date (without hour), xStartLocation, xEndLocation"
        Public Property TimeXLocations As List(Of Tuple(Of Date, Integer, StringFormat)) 'Format is "Date (with hour), xLocation, alignment"
        Public Property TimeLabelFormats As Dictionary(Of Date, String)
        Public Property DayLabelFormats As Dictionary(Of Date, String)
        Public Property Holidays As Dictionary(Of Date, String)
        Public Property Rows As List(Of Row)
        Public Property TopHeaderHeight As Integer
        Public Property DefaultTimeLabelFormat As String
        Public Property DefaultDayLabelFormat As String
        Public Property RowIconSize As Size
        Public Property RowIconLocation As Corner
        Public Property HorizontalGridLinesVisible As Boolean
        Public Property VerticalGridLinesVisible As Boolean
        Public Property ShowNowIndicator As Boolean
        Public Property NowIndicatorHourOffset As Integer
        Public Property MinTimeIntervalWidth As Integer
#End Region
    End Class
End Namespace
