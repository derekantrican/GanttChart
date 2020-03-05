using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GanttChart
{
    public class TimeBlockClickedEventArgs
    {
        public TimeBlock ClickedTimeBlock { get; set; }
        public Row RelatedRow { get; set; }
        public Point CursorLocation { get; set; }
    }
}