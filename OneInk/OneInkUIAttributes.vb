Imports Windows.UI


Public Class OneInkUIAttributes

    Private CurrentUI As New UISettings

    Property IsNightModeEnabled = False
    Property IsNightModeAutoEnabled = False
    ReadOnly Property NightTimeLine As New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 0, 0)
    Property NightModeDarkness As Double = 0

    Property IsAutoThemeEnabled = True
    Property ThemeColor As Color = CurrentUI.GetColorValue(UIColorType.Accent)

    Property UITheme As ElementTheme = ElementTheme.Default
    Property ToolBarTheme As ElementTheme = ElementTheme.Default

    Property IsBlurEnable As Boolean = False
    Property BlurDepth As Double = 16

    Property IsAutoReload As Boolean = False
    Property RecentPicNum As Integer = 0

End Class
