namespace MailNotify.Services;

public record ThemeColors(
    Color Background,
    Color Border,
    Color HoverBackground,
    Color Text,
    Color HoverText,
    Color DisabledText,
    Color Separator,
    Color Checkmark)
{
    public static ThemeColors Dark { get; } = new(
        Background: Color.FromArgb(43, 43, 43),
        Border: Color.FromArgb(62, 62, 62),
        HoverBackground: Color.FromArgb(58, 58, 58),
        Text: Color.FromArgb(242, 242, 242),
        HoverText: Color.FromArgb(255, 255, 255),
        DisabledText: Color.FromArgb(120, 120, 120),
        Separator: Color.FromArgb(58, 58, 58),
        Checkmark: Color.FromArgb(242, 242, 242));

    public static ThemeColors Light { get; } = new(
        Background: Color.FromArgb(250, 250, 250),
        Border: Color.FromArgb(218, 218, 218),
        HoverBackground: Color.FromArgb(232, 232, 232),
        Text: Color.FromArgb(24, 24, 24),
        HoverText: Color.FromArgb(0, 0, 0),
        DisabledText: Color.FromArgb(140, 140, 140),
        Separator: Color.FromArgb(226, 226, 226),
        Checkmark: Color.FromArgb(24, 24, 24));
}

public interface IWindowsThemeService
{
    bool IsDarkTheme();
    bool IsTaskbarDark();
    ThemeColors GetColors();
}
