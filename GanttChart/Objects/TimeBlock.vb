Imports System.Drawing

Namespace GanttChart
    Public Class TimeBlock
        Public Sub New()
            Hatch = False
            Clickable = True
            IsVisible = True
        End Sub

        Public Sub New(text As String)
            Me.Text = text
            Hatch = False
            Clickable = True
            IsVisible = True
        End Sub

        Public Sub New(text As String, startTime As Date, endTime As Date)
            Me.Text = text
            Me.StartTime = startTime
            Me.EndTime = endTime
            Hatch = False
            Clickable = True
            IsVisible = True
        End Sub

        Public Overridable Property Text As String
        Public Overridable Property StartTime As Date
        Public Overridable Property EndTime As Date
        Public Overridable Property Color As Color
        Public Overridable Property Hatch As Boolean
        Public Overridable Property Clickable As Boolean
        Public Overridable Property Rect As Rectangle
        Public Overridable Property IsVisible As Boolean
    End Class
End Namespace
