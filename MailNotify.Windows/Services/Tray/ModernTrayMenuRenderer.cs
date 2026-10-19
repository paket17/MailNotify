using System.Drawing.Drawing2D;

namespace MailNotify.Services;

public class ModernTrayMenuRenderer(ThemeColors colors) : ToolStripRenderer
{
    public ThemeColors Colors { get; set; } = colors;

    public ModernTrayMenuRenderer(bool isDarkMode)
        : this(isDarkMode ? ThemeColors.Dark : ThemeColors.Light)
    {
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Colors.Background);
        var rect = new Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
        e.Graphics.FillRectangle(brush, rect);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Colors.Border);
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        using var path = CreateRoundedRectangle(rect, 8);
        var prevSmoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
        e.Graphics.SmoothingMode = prevSmoothing;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is not ToolStripMenuItem item)
            return;

        if (item.Selected && item.Enabled)
        {
            using var brush = new SolidBrush(Colors.HoverBackground);
            var clientWidth = e.ToolStrip?.ClientSize.Width ?? item.Width;
            var marginX = 4;
            var width = Math.Min(item.Width, clientWidth) - (marginX * 2);
            var height = item.Height - 4;
            var rect = new Rectangle(marginX, 2, width, height);

            using var path = CreateRoundedRectangle(rect, 5);
            var prevSmoothing = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
            e.Graphics.SmoothingMode = prevSmoothing;
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var isSelected = e.Item.Selected && e.Item.Enabled;
        var color = !e.Item.Enabled
            ? Colors.DisabledText
            : (isSelected ? Colors.HoverText : Colors.Text);

        var font = e.TextFont ?? SystemFonts.MenuFont ?? e.Item.Font;

        if (e.Item is ToolStripMenuItem { Checked: true })
        {
            using var checkFont = new Font(font.FontFamily, font.Size, FontStyle.Bold);
            var checkSize = TextRenderer.MeasureText(e.Graphics, "✓", checkFont);
            var checkY = (e.Item.Height - checkSize.Height) / 2;
            var checkPoint = new Point(10, checkY);
            TextRenderer.DrawText(e.Graphics, "✓", checkFont, checkPoint, Colors.Checkmark, TextFormatFlags.NoPadding);
        }

        var clientWidth = e.ToolStrip?.ClientSize.Width ?? e.Item.Width;
        var textX = 28;
        var textWidth = Math.Max(0, Math.Min(e.Item.Width, clientWidth) - textX - 12);
        var textRect = new Rectangle(textX, 0, textWidth, e.Item.Height);

        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            font,
            textRect,
            color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Colors.Separator);
        var clientWidth = e.ToolStrip?.ClientSize.Width ?? e.Item.Width;
        var marginX = 8;
        var width = Math.Min(e.Item.Width, clientWidth) - (marginX * 2);
        e.Graphics.DrawLine(pen, marginX, y, marginX + width, y);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Suppress default gradient image margin
    }

    internal static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        if (diameter > rect.Width) diameter = rect.Width;
        if (diameter > rect.Height) diameter = rect.Height;

        // Top-left
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);

        // Top-right
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);

        // Bottom-right
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);

        // Bottom-left
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

        path.CloseFigure();
        return path;
    }
}
