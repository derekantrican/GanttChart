Imports System.Drawing

Namespace GanttChart
    Public Class CanvasClickedEventArgs
        Public Property ClickedLocation As Date?
        Public Property RelatedRow As Row
        Public Property CursorLocation As Point
    End Class
End Namespace
