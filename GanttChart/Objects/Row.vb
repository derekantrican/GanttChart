Imports System.Collections.Generic
Imports System.Drawing

Namespace GanttChart
    Public Class Row
        Public Sub New()
            IsVisible = True
            TimeBlocks = New List(Of TimeBlock)()
        End Sub

        Public Sub New(text As String)
            Me.Text = text
            IsVisible = True
            TimeBlocks = New List(Of TimeBlock)()
        End Sub

        Public Overridable Property Text As String
        Public Overridable Property TimeBlocks As List(Of TimeBlock)
        Public Overridable Property Icon As Image
        Public Overridable Property IsVisible As Boolean
        Public Overridable Property Rect As Rectangle
        Public Overridable Property IconRect As Rectangle
    End Class
End Namespace
