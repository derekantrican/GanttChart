using System;
using System.Drawing;

namespace GanttChart
{
    public class RowClickedEventArgs
    {
        public Row ClickedRow { get; set; }
        public Point CursorLocation { get; set; }
    }
}