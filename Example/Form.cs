using GanttChart;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Example
{
    public partial class Form : System.Windows.Forms.Form
    {
        public Form()
        {
            InitializeComponent();

            InitChart();
        }

        public void InitChart()
        {
            Chart ganttChart = new Chart(); //Can also be added via the designer
            ganttChart.StartDate = DateTime.Today;
            ganttChart.EndDate = DateTime.Today.AddDays(5);
            ganttChart.StartHourInDay = 8;
            ganttChart.EndHourInDay = 17;

            this.Controls.Add(ganttChart); //Add the chart to the form
            ganttChart.Dock = DockStyle.Fill; //Expand the chart to fill the form

            //Add data
            Row row1 = new Row("Row 1");
            row1.TimeBlocks.Add(new TimeBlock("Shift 1", DateTime.Today.AddHours(8), DateTime.Today.AddHours(13)) { Color = Color.Red });
            ganttChart.Rows.Add(row1);

            Row row2 = new Row("Row 2");
            row2.TimeBlocks.Add(new TimeBlock("Shift 1", DateTime.Today.AddHours(8), DateTime.Today.AddHours(10)) { Color = Color.Yellow });
            row2.TimeBlocks.Add(new TimeBlock("Shift 2", DateTime.Today.AddHours(13), DateTime.Today.AddHours(17)) { Color = Color.Purple });
            ganttChart.Rows.Add(row2);


            ganttChart.UpdateView();
        }
    }
}
