Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Linq
Imports System
Imports GanttChart.Enums
Imports System.ComponentModel

Namespace GanttChart
    Public Class Chart
        Inherits UserControl
#Region "Initializations"
        Public Sub New()
            Init()
        End Sub

        Private Sub Init()
            AutoScroll = True
            DoubleBuffered = True
            ResizeRedraw = True
            AddHandler doubleClickTimer.Tick, AddressOf doubleClickTimer_Tick
            NowIndicatorTimer = New Timers.Timer() With {
                .Interval = 1000
            } 'Don't really need such a quick interval but it helps with render issues by refreshing the chart often
            AddHandler NowIndicatorTimer.Elapsed, AddressOf NowIndicatorTimer_Elapsed
            NowIndicatorTimer.Start()
            NowIndicatorTimer.Enabled = True
        End Sub

        Private Sub NowIndicatorTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs)
            If IsHandleCreated AndAlso Not IsDisposed Then Invoke(CType(Sub() UpdateView(), MethodInvoker))
            RaiseEvent NowTick()
        End Sub
#End Region

#Region "Interaction"
        'Double click vs Single click code from https://docs.microsoft.com/en-us/dotnet/framework/winforms/how-to-distinguish-between-clicks-and-double-clicks
        Private doubleClickRectangle As Rectangle = New Rectangle()
        Private doubleClickTimer As Timer = New Timer() With {
            .Interval = 100
        }
        Private isFirstClick As Boolean = True
        Private isDoubleClick As Boolean = False
        Private milliseconds As Integer = 0
        Private doubleClickLocation As Point = New Point()

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            'This is the first mouse click.
            If isFirstClick Then
                isFirstClick = False
                doubleClickLocation = e.Location

                'Determine the location and size of the double click 
                'rectangle area to draw around the cursor point.
                doubleClickRectangle = New Rectangle(e.X - SystemInformation.DoubleClickSize.Width / 2, e.Y - SystemInformation.DoubleClickSize.Height / 2, SystemInformation.DoubleClickSize.Width, SystemInformation.DoubleClickSize.Height)
                Invalidate()

                'Start the double click timer.
                doubleClickTimer.Start() 'This is the second mouse click.
            Else
                'Verify that the mouse click is within the double click
                'rectangle and is within the system-defined double 
                'click period.
                If doubleClickRectangle.Contains(e.Location) AndAlso milliseconds < SystemInformation.DoubleClickTime Then
                    isDoubleClick = True
                End If
            End If
        End Sub

        Private Sub doubleClickTimer_Tick(sender As Object, e As EventArgs)
            milliseconds += 100

            'The timer has reached the double click time limit.
            If milliseconds >= SystemInformation.DoubleClickTime Then
                doubleClickTimer.Stop()
                Dim clickedRow As Row = Nothing
                Dim relatedRow As Row = Nothing
                Dim clickedTimeBlock As TimeBlock = Nothing

                For Each row In Rows.Where(Function(p) p.IsVisible)

                    If row.Rect.Contains(doubleClickLocation) Then
                        clickedRow = row
                        Exit For
                    ElseIf row.TimeBlocks.Find(Function(p) p.Rect.Contains(doubleClickLocation)) IsNot Nothing Then
                        relatedRow = row
                        clickedTimeBlock = row.TimeBlocks.Find(Function(p) p.Rect.Contains(doubleClickLocation))
                        Exit For
                    End If
                Next

                Dim eventGlobalLocation As Point = New Point(doubleClickLocation.X + Left, doubleClickLocation.Y + Top)

                If isDoubleClick Then
                    If clickedRow IsNot Nothing Then
                        If clickedRow.IconRect.Contains(doubleClickLocation) Then
                            RaiseEvent RowIconDoubleClick(New RowClickedEventArgs() With {
                                .ClickedRow = clickedRow,
                                .CursorLocation = eventGlobalLocation
                            })
                        Else
                            RaiseEvent RowDoubleClick(New RowClickedEventArgs() With {
                                .ClickedRow = clickedRow,
                                .CursorLocation = eventGlobalLocation
                            })
                        End If
                    ElseIf clickedTimeBlock IsNot Nothing AndAlso clickedTimeBlock.Clickable Then
                        RaiseEvent TimeBlockDoubleClick(New TimeBlockClickedEventArgs() With {
                            .ClickedTimeBlock = clickedTimeBlock,
                            .RelatedRow = relatedRow,
                            .CursorLocation = eventGlobalLocation
                        })
                    ElseIf renderer.MainCanvas.Contains(doubleClickLocation) Then
                        Dim horizontalRow = Rows.FirstOrDefault(Function(p) p.Rect.Contains(1, doubleClickLocation.Y))
                        Dim clickedTime As Date? = Nothing

                        For i = 0 To renderer.TimeXLocations.Count - 1 - 1 'Loop to 1 less than the end so we don't get index exceptions
                            Dim time = renderer.TimeXLocations(i)
                            Dim rowRect = horizontalRow.Rect
                            rowRect.X = time.Item2
                            rowRect.Width = renderer.TimeXLocations(i + 1).Item2 - rowRect.X

                            If rowRect.Contains(doubleClickLocation) Then
                                clickedTime = time.Item1
                                Exit For
                            End If
                        Next

                        RaiseEvent MainCanvasDoubleClick(New CanvasClickedEventArgs() With {
                            .RelatedRow = horizontalRow,
                            .ClickedLocation = clickedTime,
                            .CursorLocation = eventGlobalLocation
                        })
                    End If
                Else

                    If clickedRow IsNot Nothing Then
                        If clickedRow.IconRect.Contains(doubleClickLocation) Then
                            RaiseEvent RowIconSingleClick(New RowClickedEventArgs() With {
                                .ClickedRow = clickedRow,
                                .CursorLocation = eventGlobalLocation
                            })
                        Else
                            RaiseEvent RowSingleClick(New RowClickedEventArgs() With {
                                .ClickedRow = clickedRow,
                                .CursorLocation = eventGlobalLocation
                            })
                        End If
                    ElseIf clickedTimeBlock IsNot Nothing AndAlso clickedTimeBlock.Clickable Then
                        RaiseEvent TimeBlockSingleClick(New TimeBlockClickedEventArgs() With {
                            .ClickedTimeBlock = clickedTimeBlock,
                            .RelatedRow = relatedRow,
                            .CursorLocation = eventGlobalLocation
                        })
                    ElseIf renderer.MainCanvas.Contains(doubleClickLocation) Then
                        Dim horizontalRow = Rows.FirstOrDefault(Function(p) p.Rect.Contains(1, doubleClickLocation.Y))
                        Dim clickedTime As Date? = Nothing

                        For i = 0 To renderer.TimeXLocations.Count - 1 - 1 'Loop to 1 less than the end so we don't get index exceptions
                            Dim time = renderer.TimeXLocations(i)
                            Dim rowRect = horizontalRow.Rect
                            rowRect.X = time.Item2
                            rowRect.Width = renderer.TimeXLocations(i + 1).Item2 - rowRect.X

                            If rowRect.Contains(doubleClickLocation) Then
                                clickedTime = time.Item1
                                Exit For
                            End If
                        Next

                        RaiseEvent MainCanvasSingleClick(New CanvasClickedEventArgs() With {
                            .RelatedRow = horizontalRow,
                            .ClickedLocation = clickedTime,
                            .CursorLocation = eventGlobalLocation
                        })
                    End If
                End If

                'Allow the MouseDown event handler to process clicks again.
                isFirstClick = True
                isDoubleClick = False
                milliseconds = 0
            End If
        End Sub
#End Region

#Region "Draw Interface"
        Private NowIndicatorTimer As Timers.Timer
        Private renderer As Renderer = New Renderer()

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            'Don't paint if in design mode
            If IsDesignerHosted OrElse DesignMode Then Return
            AutoScrollMinSize = renderer.CalculateAutoScrollSize(AutoScrollMinSize, Size, VerticalScroll)
            renderer.Render(e.Graphics, AutoScrollPosition, AutoScrollMinSize, Font)
        End Sub

        'Check for "DesignMode" if inside a UserControl
        'https://stackoverflow.com/a/708594/2246411
        Private ReadOnly Property IsDesignerHosted As Boolean
            Get
                Dim ctrl As Control = Me

                While ctrl IsNot Nothing
                    If ctrl.Site IsNot Nothing AndAlso ctrl.Site.DesignMode Then Return True
                    ctrl = ctrl.Parent
                End While

                Return False
            End Get
        End Property
#End Region

#Region "Public Events"
        Public Delegate Sub MainCanvasSingleClickDelegate(e As CanvasClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event MainCanvasSingleClick As MainCanvasSingleClickDelegate
        Public Delegate Sub MainCanvasDoubleClickDelegate(e As CanvasClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event MainCanvasDoubleClick As MainCanvasDoubleClickDelegate
        Public Delegate Sub RowSingleClickDelegate(e As RowClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event RowSingleClick As RowSingleClickDelegate
        Public Delegate Sub RowDoubleClickDelegate(e As RowClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event RowDoubleClick As RowDoubleClickDelegate
        Public Delegate Sub RowIconSingleClickDelegate(e As RowClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event RowIconSingleClick As RowIconSingleClickDelegate
        Public Delegate Sub RowIconDoubleClickDelegate(e As RowClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event RowIconDoubleClick As RowIconDoubleClickDelegate
        Public Delegate Sub TimeBlockSingleClickDelegate(e As TimeBlockClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event TimeBlockSingleClick As TimeBlockSingleClickDelegate
        Public Delegate Sub TimeBlockDoubleClickDelegate(e As TimeBlockClickedEventArgs)
        <Category("Gantt Chart")>
        Public Event TimeBlockDoubleClick As TimeBlockDoubleClickDelegate
#End Region

#Region "Public Methods"
        Public Sub UpdateView()
            Invalidate() 'Force chart to redraw
        End Sub

        Public Function GetNowTimeBlocks() As Dictionary(Of Row, List(Of TimeBlock))
            Dim now = Date.Now
            now = now.AddHours(NowIndicatorHourOffset)
            Dim result As Dictionary(Of Row, List(Of TimeBlock)) = New Dictionary(Of Row, List(Of TimeBlock))()

            For Each row In Rows

                For Each timeBlock In row.TimeBlocks

                    If now >= timeBlock.StartTime AndAlso now <= timeBlock.EndTime Then
                        If result.ContainsKey(row) Then
                            result(row).Add(timeBlock)
                        Else
                            result.Add(row, New List(Of TimeBlock)() From {
                                timeBlock
                            })
                        End If
                    End If
                Next
            Next

            Return result
        End Function

        Public Sub RecaculateFormatDictionaries(Optional overwrite As Boolean = False)
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
        ''' <summary>
        ''' The main data for the chart
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property Rows As List(Of Row)
            Get
                Return renderer.Rows
            End Get
            Set(value As List(Of Row))
                renderer.Rows = value
            End Set
        End Property

        ''' <summary>
        ''' The height of the top header (where the horizontal axis labels are)
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property TopHeaderHeight As Integer
            Get
                Return renderer.TopHeaderHeight
            End Get
            Set(value As Integer)
                renderer.TopHeaderHeight = value
            End Set
        End Property

        ''' <summary>
        ''' Controls the minimum width (in pixels) of each timeblock. When the chart is resized, all timeblocks will compress down to this size. 
        ''' Then a horizontal scrollbar will start to appear.
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property MinTimeIntervalWidth As Integer '= 0; //In pixels. If left to 0, the chart width with always match the width of the parent control (no horizontal scrollbar)
            Get
                Return renderer.MinTimeIntervalWidth
            End Get
            Set(value As Integer)
                renderer.MinTimeIntervalWidth = value
            End Set
        End Property

        ''' <summary>
        ''' Controls whether or not the "now indicator" (a red line drawn vertically across the chart) should be shown
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property ShowNowIndicator As Boolean
            Get
                Return renderer.ShowNowIndicator
            End Get
            Set(value As Boolean)
                renderer.ShowNowIndicator = value
            End Set
        End Property

        ''' <summary>
        ''' Controls the offset for the now indicator. Useful for displaying the now indicator at a different timezone than the computer's current timezone
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property NowIndicatorHourOffset As Integer
            Get
                Return renderer.NowIndicatorHourOffset
            End Get
            Set(value As Integer)
                renderer.NowIndicatorHourOffset = value
            End Set
        End Property

        ''' <summary>
        ''' Sets whether or not horizontal gridlines (the lines between rows) should be visible
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property HorizontalGridLinesVisible As Boolean
            Get
                Return renderer.HorizontalGridLinesVisible
            End Get
            Set(value As Boolean)
                renderer.HorizontalGridLinesVisible = value
            End Set
        End Property

        ''' <summary>
        ''' Sets whether or not vertical gridlines (the lines between days) should be shown
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property VerticalGridLinesVisible As Boolean
            Get
                Return renderer.VerticalGridLinesVisible
            End Get
            Set(value As Boolean)
                renderer.VerticalGridLinesVisible = value
            End Set
        End Property

        ''' <summary>
        ''' StartDate is inclusive (chart starts at 00:00 of this day)
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property StartDate As Date
            Get
                Return renderer.StartDate.Date
            End Get
            Set(value As Date)
                renderer.StartDate = value.Date
            End Set
        End Property

        ''' <summary>
        ''' EndDate is exclusive (chart ends at 00:00 of this day)
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property EndDate As Date
            Get
                Return renderer.EndDate.Date
            End Get
            Set(value As Date)
                renderer.EndDate = value.Date
            End Set
        End Property

        ''' <summary>
        ''' StartHourInDay is inclusive (first hour on the chart for the day will be StartHourInDay). Note: this is only relevant for TimeInterval.Hour
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property StartHourInDay As Integer
            Get
                Return renderer.StartHourInDay
            End Get
            Set(value As Integer)
                renderer.StartHourInDay = value
            End Set
        End Property

        ''' <summary>
        ''' EndHourInDay is exclusive (last hour on the chart for the day will be EndHourInDay). Note: this is only relevant for TimeInterval.Hour
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property EndHourInDay As Integer
            Get
                Return renderer.EndHourInDay
            End Get
            Set(value As Integer)
                renderer.EndHourInDay = value
            End Set
        End Property

        'Todo: need to support this in the future. IMPORTANT: this may also require a lot of renaming of other properties or functionality
        'public TimeInterval TimeInterval = TimeInterval.Hour; //Todo: need to support other time intervals

        ''' <summary>
        ''' The format for the time labels
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property DefaultTimeLabelFormat As String 'Here is where someone can display military time instead
            Get
                Return renderer.DefaultTimeLabelFormat
            End Get
            Set(value As String)
                Dim oldFormat = renderer.DefaultTimeLabelFormat
                renderer.DefaultTimeLabelFormat = value

                For Each key In TimeLabelFormats.Keys.ToList()
                    If Equals(TimeLabelFormats(key), oldFormat) Then TimeLabelFormats(key) = renderer.DefaultTimeLabelFormat
                Next
            End Set
        End Property

        ''' <summary>
        ''' The format for the date labels
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property DefaultDayLabelFormat As String
            Get
                Return renderer.DefaultDayLabelFormat
            End Get
            Set(value As String)
                Dim oldFormat = renderer.DefaultDayLabelFormat
                renderer.DefaultDayLabelFormat = value

                For Each key In DayLabelFormats.Keys.ToList()
                    If Equals(DayLabelFormats(key), oldFormat) Then DayLabelFormats(key) = renderer.DefaultDayLabelFormat
                Next
            End Set
        End Property

        ''' <summary>
        ''' Controls the color of the text of most of the form (timeblock text color automatically adjusts based on timeblock color)
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property TextColor As Color
            Get
                Return renderer.TextColor
            End Get
            Set(value As Color)
                renderer.TextColor = value
            End Set
        End Property

        ''' <summary>
        ''' Controls the background of the row headers
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property HeaderBackgroundColor As Color
            Get
                Return renderer.HeaderBackgroundColor
            End Get
            Set(value As Color)
                renderer.HeaderBackgroundColor = value
            End Set
        End Property

        ''' <summary>
        ''' Controls the background color of the chart canvas area
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property BackgroundColor As Color
            Get
                Return renderer.BackgroundColor
            End Get
            Set(value As Color)
                renderer.BackgroundColor = value
            End Set
        End Property

        ''' <summary>
        ''' Location of the icon in the row header
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property RowIconLocation As Corner
            Get
                Return renderer.RowIconLocation
            End Get
            Set(value As Corner)
                renderer.RowIconLocation = value
            End Set
        End Property

        ''' <summary>
        ''' Size of the icon in the row header
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property RowIconSize As Size
            Get
                Return renderer.RowIconSize
            End Get
            Set(value As Size)
                renderer.RowIconSize = value
            End Set
        End Property

        Public Delegate Sub NowTickDelegate()
        ''' <summary>
        ''' This event is fired whenver the chart updates its now indicator (about every second). NOTE: this
        ''' event does not run on the UI thread
        ''' </summary>
        <Category("Gantt Chart")>
        Public Event NowTick As NowTickDelegate

        ''' <summary>
        ''' Each date shown in the chart along with its X location
        ''' </summary>
        <Category("Gantt Chart")>
        Public ReadOnly Property DayLocations As IReadOnlyDictionary(Of Date, Integer)
            Get
                Dim result As Dictionary(Of Date, Integer) = New Dictionary(Of Date, Integer)()
                renderer.DayXLocations.ForEach(Sub(p) result.Add(p.Item1, p.Item2))
                Return result
            End Get
        End Property

        ''' <summary>
        ''' Each time shown in the chart along with its X location
        ''' </summary>
        <Category("Gantt Chart")>
        Public ReadOnly Property TimeLocations As IReadOnlyDictionary(Of Date, Integer)
            Get
                Dim result As Dictionary(Of Date, Integer) = New Dictionary(Of Date, Integer)()
                renderer.TimeXLocations.ForEach(Sub(p) result.Add(p.Item1, p.Item2))
                Return result
            End Get
        End Property

        ''' <summary>
        ''' <para>Controls the time display format for each individual time label</para>
        ''' <para>Keys are DateTime and Values are string (the DateTime format string)</para>
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property TimeLabelFormats As Dictionary(Of Date, String)
            Get
                Return renderer.TimeLabelFormats
            End Get
            Set(value As Dictionary(Of Date, String))
                renderer.TimeLabelFormats = value
            End Set
        End Property

        ''' <summary>
        ''' <para>Controls the day display format for each individual day label</para>
        ''' <para>Keys are DateTime and Values are string (the DateTime format string)</para>
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property DayLabelFormats As Dictionary(Of Date, String)
            Get
                Return renderer.DayLabelFormats
            End Get
            Set(value As Dictionary(Of Date, String))
                renderer.DayLabelFormats = value
            End Set
        End Property

        ''' <summary>
        ''' <para>Controls holidays for the form. On each holiday, a cross hatch is drawn through all rows and no timeblocks are shown.</para>
        ''' <para>Keys are DateTime and Values are string (the name of the Holiday)</para>
        ''' </summary>
        <Category("Gantt Chart")>
        Public Property Holidays As Dictionary(Of Date, String)
            Get
                Return renderer.Holidays
            End Get
            Set(value As Dictionary(Of Date, String))
                renderer.Holidays = value
            End Set
        End Property
#End Region
    End Class
End Namespace
