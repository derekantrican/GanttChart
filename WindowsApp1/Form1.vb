Imports System.ComponentModel
Imports GanttChart

Public Class Form1
    Public Sub New()
        InitChart()
    End Sub

    Public Sub InitChart()
        Dim ganttChart As Chart = New Chart()
        ganttChart.StartDate = Date.Today
        ganttChart.EndDate = Date.Today.AddDays(5)
        ganttChart.StartHourInDay = 8
        ganttChart.EndHourInDay = 17
        Me.Controls.Add(ganttChart)
        ganttChart.Dock = DockStyle.Fill
        Dim row1 As Row = New Row("Row 1")
        row1.TimeBlocks.Add(New TimeBlock("Shift 1", Date.Today.AddHours(8), Date.Today.AddHours(13)) With {
            .Color = Color.Red
        })
        ganttChart.Rows.Add(row1)
        Dim row2 As Row = New Row("Row 2")
        row2.TimeBlocks.Add(New TimeBlock("Shift 1", Date.Today.AddHours(8), Date.Today.AddHours(10)) With {
            .Color = Color.Yellow
        })
        row2.TimeBlocks.Add(New TimeBlock("Shift 2", Date.Today.AddHours(13), Date.Today.AddHours(17)) With {
            .Color = Color.MediumPurple
        })
        ganttChart.Rows.Add(row2)
        ganttChart.UpdateView()
    End Sub
End Class
