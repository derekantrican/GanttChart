# Gantt Chart control for WinForms

This is a highly-customizable Gantt Chart control for WinForms

![screenshot](https://i.imgur.com/K5YFvIE.png)

### How to use:

```csharp
Chart ganttChart = new Chart(); //Can also be added via the designer
ganttChart.StartDate = DateTime.Today;
ganttChart.EndDate = DateTime.Today.AddDays(5);
Row row = new Row("Row 1");
row.TimeBlocks.Add(new TimeBlock("Shift 1", DateTime.Today.AddHours(8), DateTime.Today.AddHours(17)));
ganttChart.Rows.add(row);
ganttChart.UpdateView();
```

### Features:

- Highly-customizable rendering, allowing control over
  - Order of rows
  - Days shown over chart (along with start and end hour of each day)
  - Whether or not to show the "now indicator" (red line indicating the current time)
  - Fully customizable formats for displayed time and day labels (above the chart) using [DateTime format strings](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings)
  - Whether or not to show the horizontal or vertical grid lines
  - Support for custom Holidays that "blocks out" the entire chart on that day
- Events triggered based on clicking or doubleclicking on Row headers, TimeBlocks, or the canvas itself
