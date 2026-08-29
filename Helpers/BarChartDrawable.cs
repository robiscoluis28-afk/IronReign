namespace IronReign.Helpers;

public class BarChartDrawable : IDrawable
{
    public List<double> Values { get; set; } = new();

    public Color BarColor { get; set; } = Colors.Gray;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count == 0)
            return;

        var max = Values.Max();
        if (max <= 0)
            max = 1;

        var spacing = 6f;
        var barWidth = (dirtyRect.Width - spacing * (Values.Count - 1)) / Values.Count;

        for (var i = 0; i < Values.Count; i++)
        {
            var ratio = Values[i] / max;
            var barHeight = Math.Max((float)(dirtyRect.Height * ratio), 3f);
            var x = i * (barWidth + spacing);
            var y = dirtyRect.Height - barHeight;

            canvas.FillColor = BarColor;
            canvas.FillRoundedRectangle(x, y, barWidth, barHeight, 3);
        }
    }
}
