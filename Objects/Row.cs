using System.Collections.Generic;
using System.Drawing;

namespace GanttChart
{
    public class Row
    {
        public Row()
        {
            IsVisible = true;
            TimeBlocks = new List<TimeBlock>();
        }

        public Row(string text)
        {
            this.Text = text;
            IsVisible = true;
            TimeBlocks = new List<TimeBlock>();
        }

        public virtual string Text { get; set; }
        public virtual List<TimeBlock> TimeBlocks { get; set; }
        public virtual Image Icon { get; set; }
        public virtual bool IsVisible { get; set; }
        public virtual Rectangle Rect { get; set; }
        public virtual Rectangle IconRect { get; set; }
    }
}
