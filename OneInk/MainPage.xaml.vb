Imports Windows.UI
Imports Windows.UI.Xaml.Shapes
Imports Windows.UI.Input.Inking
Imports Windows.Storage
Imports Windows.UI.Xaml.Media.Animation
Imports Windows.UI.Composition
Imports Windows.UI.Xaml.Hosting
Imports Microsoft.Graphics.Canvas.Effects
Imports Windows.ApplicationModel.DataTransfer
Imports Windows.Foundation.Metadata
Imports OneInk.OneInkShapeRcg
Imports OneInk.OneInkFileOperation
Imports Windows.UI.Core
Imports System.Xml.Serialization
Imports OneInk

Public NotInheritable Class MainPage
    Inherits Page

    Private WithEvents OnBlurTimer As New DispatcherTimer With {
        .Interval = TimeSpan.FromMilliseconds(1)
    }
    Private WithEvents EndBlurTimer As New DispatcherTimer With {
        .Interval = TimeSpan.FromMilliseconds(1)
    }
    Private WithEvents PicFlowTimer As New DispatcherTimer With {
        .Interval = TimeSpan.FromMilliseconds(1700)
    }
    Private WithEvents CurrentUI As New UISettings
    Private PageLoaded As Boolean = False
    Public Shared Property CurrentSettings As New OneInkUIAttributes

    Private Async Sub SysThemeColorOnChanged(sender As UISettings, args As Object) Handles CurrentUI.ColorValuesChanged
        CurrentSettings.ThemeColor = sender.GetColorValue(UIColorType.Accent)
        Await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                     UpDateAttributes(CurrentSettings, 2)
                                                                                 End Sub)
        '.NET新特性：Lambda表达式，允许没有名字的函数/过程
        'RunAsync要求两个参数，第二个参数不知道怎么直接用AddressOf UpDateAttributes:参数怎么传？
    End Sub

    Private Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '------------------------------Somehing goes wrong...On phone:"IO.Exception"
        PageLoaded = True
        AttrOnLoad()
        DrawingCanvas.InkPresenter.InputDeviceTypes =
                                   Windows.UI.Core.CoreInputDeviceTypes.Mouse Or
                                   Windows.UI.Core.CoreInputDeviceTypes.Touch Or
                                   Windows.UI.Core.CoreInputDeviceTypes.Pen
        UpDateAttributes(CurrentSettings, 5)
        HideStatusBarOnPhone()
        FlipPicOnLoad()
    End Sub

    Private Async Sub AttrOnLoad()
        CurrentSettings = Await AutoAttrDeSerialize()
        UpDateAttributes(CurrentSettings, 6)
        UpDateAttributes(CurrentSettings, 1)
        UpDateAttributes(CurrentSettings, 2)
        UpDateAttributes(CurrentSettings, 3)
        UpDateAttributes(CurrentSettings, 4)
        UpDateAttributes(CurrentSettings, 5)
    End Sub

    Private Async Sub FlipPicOnLoad()
        Dim PicList As IReadOnlyList(Of BitmapImage) = Await OneInkFileOperation.SeqEnumRecentPic(DrawingCanvas)
        flip1.Source = PicList.Item(0)
        flip2.Source = PicList.Item(1)
        flip3.Source = PicList.Item(2)
    End Sub

    Private Async Sub HideStatusBarOnPhone()
        If ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar") Then
            Dim CurrentStatusBar As StatusBar = StatusBar.GetForCurrentView()
            Await CurrentStatusBar.HideAsync()
        End If
    End Sub

    Private Sub BlurInitialize(TargetBkgdGrid As Grid, BlurAmount As Single)

        SplitMenuPaneBG.Background = Nothing
        SplitSettingsPaneBG.Background = Nothing
        OneInk_CanvasMask.Background = Nothing

        Dim HostVisual As Visual = ElementCompositionPreview.GetElementVisual(TargetBkgdGrid)
        Dim Cpst As Compositor = HostVisual.Compositor

        'VB.NET中对象的列表初始化形式：With. Define a glass effect.
        Dim glassEffect As GaussianBlurEffect = New GaussianBlurEffect With {
            .BlurAmount = BlurAmount,
            .BorderMode = EffectBorderMode.Hard,
            .Source = New ArithmeticCompositeEffect With {
                .Source2 = New ColorSourceEffect With {
                    .Color = CurrentSettings.ThemeColor
                },
                .Source1 = New CompositionEffectSourceParameter("backdropBrush"),
                .MultiplyAmount = 0,
                .Source1Amount = 0.5,
                .Source2Amount = 0.5
                }
            }
        '------------------------------------------------ End

        Dim EFT_Factory As CompositionEffectFactory = Cpst.CreateEffectFactory(glassEffect)
        Dim BackdropBrush As CompositionBackdropBrush = Cpst.CreateBackdropBrush()
        Dim EFT_Brush As CompositionEffectBrush = EFT_Factory.CreateBrush() '用Object也许会出错？
        EFT_Brush.SetSourceParameter("backdropBrush", BackdropBrush)
        Dim GlassVisual As SpriteVisual = Cpst.CreateSpriteVisual()
        GlassVisual.brush = EFT_Brush
        ElementCompositionPreview.SetElementChildVisual(TargetBkgdGrid, GlassVisual)
        Dim BindSizeAnimation As ExpressionAnimation = Cpst.CreateExpressionAnimation("HostVisual.Size")
        BindSizeAnimation.setReferenceParameter("HostVisual", HostVisual)
        GlassVisual.StartAnimation("Size", BindSizeAnimation)

    End Sub

    Private Sub SolidColorBkgdInitialize(TargetBkgdGrid As Grid)
        Dim b As New SolidColorBrush With {
            .Color = CurrentSettings.ThemeColor
        }
        TargetBkgdGrid.Background = b
    End Sub

    Private Sub RcgOnProceeding()
        If OnBlurTimer.IsEnabled = False And EndBlurTimer.IsEnabled = False Then
            OneInk_CanvasMask.Opacity = 0
            OnBlurTimer.Start()
            Ring.IsActive = True
            OneInk_CanvasMask.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub RcgProceeded()
        OnBlurTimer.Stop()
        Ring.IsActive = False
        EndBlurTimer.Start()
    End Sub

    Private Sub CanvasMaskOnBlur() Handles OnBlurTimer.Tick
        OneInk_CanvasMask.Opacity = OneInk_CanvasMask.Opacity + 0.15
        If OneInk_CanvasMask.Opacity > 1 Then
            OnBlurTimer.Stop()
        End If
    End Sub

    Private Sub CanvasMaskEndBlur() Handles EndBlurTimer.Tick
        OneInk_CanvasMask.Opacity = OneInk_CanvasMask.Opacity - 0.15
        If OneInk_CanvasMask.Opacity < 0 Then
            EndBlurTimer.Stop()
            OneInk_CanvasMask.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub PicFlowing() Handles PicFlowTimer.Tick
        Dim i As Integer = RecentPicFlip.Items.IndexOf(RecentPicFlip.SelectedItem)
        i = i + 1
        If i = 3 Then
            i = i - 1
            RecentPicFlip.SelectedItem = RecentPicFlip.Items.Item(i)
            i = i - 1
            RecentPicFlip.SelectedItem = RecentPicFlip.Items.Item(i)
            i = i - 1
        End If
        RecentPicFlip.SelectedItem = RecentPicFlip.Items.Item(i)
    End Sub



    Private Async Sub FILE_SaveToFile()
        Await InkCanvasSaveTo(DrawingCanvas)
        FlipPicOnLoad()
    End Sub

    Private Sub FILE_LoadFromFile()
        InkCanvasOpenFrom(DrawingCanvas)
    End Sub

    Private Sub FILE_Copy()
        CopyToClipboard(DrawingCanvas)
    End Sub

    Private Sub UI_SettingsOnClick()
        SplitSettings.IsPaneOpen = Not SplitSettings.IsPaneOpen
        SplitMenu.IsPaneOpen = False
    End Sub

    Private Sub UI_SplitMenuClosed(sender As SplitView, args As Object) Handles SplitMenu.PaneClosed
        PicFlowTimer.Stop()
        TxtRcnResult.Text = "启用手写识别 (beta)"
        RecentPicFlip.SelectedItem = RecentPicFlip.Items.Item(0) '有一定概率在重绘窗体大小时控件出问题：FlipItem变小、多个同时出现
    End Sub

    Private Sub UI_SplitPaneSwitch()
        PicFlowTimer.Start()
        SplitMenu.IsPaneOpen = Not SplitMenu.IsPaneOpen
    End Sub

    Private Async Sub RCG_TextOnRcg()
        RcgOnProceeding()
        Dim str As String = Await WritingRecognize(DrawingCanvas.InkPresenter.StrokeContainer.GetStrokes(), DrawingCanvas)
        TxtRcnResult.Text = str
        RcgProceeded()
    End Sub

    Private Async Sub RCG_ShapeOnRcg()
        RcgOnProceeding()
        Await ShapeRcn(DrawingCanvas, InkTools)
        RcgProceeded()
    End Sub



    Public Sub UpDateAttributes(attr As OneInkUIAttributes, part As Integer)
        If PageLoaded = True Then
            Select Case part
                Case 1
                    '1.NightMode settings:
                    If attr.IsNightModeEnabled = False Then
                        NightMode_AutoSW.IsEnabled = False
                        NightMode_Slider.IsEnabled = False
                        NightModeMask.Opacity = 0

                        NightMode_AutoSW.IsOn = False
                    Else
                        NightMode_AutoSW.IsEnabled = True
                        NightMode_Slider.IsEnabled = True
                        NightModeMask.Opacity = attr.NightModeDarkness
                    End If

                    If attr.IsNightModeAutoEnabled = True Then
                        NightMode_AutoSW.IsOn = True
                        If DateTime.Now.CompareTo(attr.NightTimeLine) >= 0 Then
                            NightMode_Slider.IsEnabled = True
                            NightMode_Slider.Value = 65
                            NightModeMask.Opacity = attr.NightModeDarkness
                        End If
                    Else
                        NightMode_AutoSW.IsOn = False
                    End If

                Case 2
                    '2.ThemeColor settings:
                    If attr.IsAutoThemeEnabled = True Then
                        attr.ThemeColor = CurrentUI.GetColorValue(UIColorType.Accent)
                    Else
                        attr.ThemeColor = Color.FromArgb(255, 255, 255, 255)
                    End If
                    '-----------------------TODO: Refresh UI.
                    UpDateAttributes(attr, 5)
                Case 3
                    '3.General Theme settings:
                    '这里，通用主题和工具栏主题的设置需要隔开：因为UITheme_General初始化、选了选项的时候
                    'UITheme_Toolbar还未初始化，其选项为空(.SelectedItem = NULL)，会出错。
                    Dim i As Integer = UITheme_General.Items.IndexOf(UITheme_General.SelectedItem)
                    Select Case i
                        Case 0
                            attr.UITheme = ElementTheme.Default
                        Case 1
                            attr.UITheme = ElementTheme.Light
                        Case 2
                            attr.UITheme = ElementTheme.Dark
                    End Select
                    Me.RequestedTheme = attr.UITheme
                Case 4
                    '4.Toolbar Theme settings:
                    Dim i As Integer = UITheme_Toolbar.Items.IndexOf(UITheme_Toolbar.SelectedItem)
                    Select Case i
                        Case 0
                            attr.ToolBarTheme = ElementTheme.Default
                        Case 1
                            attr.ToolBarTheme = ElementTheme.Light
                        Case 2
                            attr.ToolBarTheme = ElementTheme.Dark
                    End Select
                    InkTools.RequestedTheme = attr.ToolBarTheme
                Case 5
                    '5.模糊部分
                    If attr.IsBlurEnable = False Then
                        UIBlur_Slider.IsEnabled = False
                        BlurInitialize(SplitMenuPaneBG, CurrentSettings.BlurDepth)
                        BlurInitialize(SplitSettingsPaneBG, CurrentSettings.BlurDepth)
                        BlurInitialize(OneInk_CanvasMask, CurrentSettings.BlurDepth)
                        '为什么不再Blur一遍就去不掉颜色呢？？？
                        SolidColorBkgdInitialize(SplitMenuPaneBG)
                        SolidColorBkgdInitialize(SplitSettingsPaneBG)
                        SolidColorBkgdInitialize(OneInk_CanvasMask)
                    Else
                        UIBlur_Slider.IsEnabled = True
                        BlurInitialize(SplitMenuPaneBG, CurrentSettings.BlurDepth)
                        BlurInitialize(SplitSettingsPaneBG, CurrentSettings.BlurDepth)
                        BlurInitialize(OneInk_CanvasMask, CurrentSettings.BlurDepth)
                    End If
                Case 6
                    '6.仅用于自动加载时：通知界面UI改变                  
                    NightMode_SW.IsOn = attr.IsNightModeEnabled
                    NightMode_AutoSW.IsOn = attr.IsNightModeAutoEnabled
                    NightMode_Slider.Value = attr.NightModeDarkness / 0.7 * 100
                    AutoTheme_SW.IsOn = attr.IsAutoThemeEnabled
                    UIBlur_SW.IsOn = attr.IsBlurEnable
                    UIBlur_Slider.Value = attr.BlurDepth / 50 * 100
                    AutoLoad_SW.IsOn = attr.IsAutoReload
                    Select Case attr.UITheme
                        Case 0
                            UITheme_General_1.IsSelected = False
                            UITheme_General_2.IsSelected = False
                            UITheme_General_0.IsSelected = True
                        Case 1
                            UITheme_General_0.IsSelected = False
                            UITheme_General_2.IsSelected = False
                            UITheme_General_1.IsSelected = True
                        Case 2
                            UITheme_General_0.IsSelected = False
                            UITheme_General_1.IsSelected = False
                            UITheme_General_2.IsSelected = True
                    End Select
                    Select Case attr.ToolBarTheme
                        Case 0
                            UITheme_Toolbar_1.IsSelected = False
                            UITheme_Toolbar_2.IsSelected = False
                            UITheme_Toolbar_0.IsSelected = True
                        Case 1
                            UITheme_Toolbar_0.IsSelected = False
                            UITheme_Toolbar_2.IsSelected = False
                            UITheme_Toolbar_1.IsSelected = True
                        Case 2
                            UITheme_Toolbar_0.IsSelected = False
                            UITheme_Toolbar_1.IsSelected = False
                            UITheme_Toolbar_2.IsSelected = True
                    End Select

            End Select

        End If
    End Sub

    Private Sub NightModeSW_Toggled(sender As ToggleSwitch, e As RoutedEventArgs)
        CurrentSettings.IsNightModeEnabled = sender.IsOn
        UpDateAttributes(CurrentSettings, 1)
    End Sub

    Private Sub NightMode_AutoSW_Toggled(sender As ToggleSwitch, e As RoutedEventArgs)
        '--------------TODO:Auto NightMode
        CurrentSettings.IsNightModeAutoEnabled = NightMode_AutoSW.IsOn
        UpDateAttributes(CurrentSettings, 1)
    End Sub

    Private Sub NightMode_Slider_ValueChanged(sender As Slider, e As RangeBaseValueChangedEventArgs)
        CurrentSettings.NightModeDarkness = (sender.Value / 100) * 0.7
        UpDateAttributes(CurrentSettings, 1)
    End Sub

    Private Sub AutoTheme_SW_Toggled(sender As ToggleSwitch, e As RoutedEventArgs)
        CurrentSettings.IsAutoThemeEnabled = sender.IsOn
        UpDateAttributes(CurrentSettings, 2)
    End Sub

    Private Sub UITheme_General_SelectionChanged(sender As ComboBox, e As SelectionChangedEventArgs)
        UpDateAttributes(CurrentSettings, 3)
    End Sub

    Private Sub UITheme_Toolbar_SelectionChanged(sender As ComboBox, e As SelectionChangedEventArgs)
        UpDateAttributes(CurrentSettings, 4)
    End Sub

    Private Sub UIBlur_SW_Toggled(sender As ToggleSwitch, e As RoutedEventArgs)
        CurrentSettings.IsBlurEnable = sender.IsOn
        UpDateAttributes(CurrentSettings, 5)
    End Sub

    Private Sub UIBlur_Slider_ValueChanged(sender As Slider, e As RangeBaseValueChangedEventArgs)
        CurrentSettings.BlurDepth = (sender.Value / 100) * 50
        UpDateAttributes(CurrentSettings, 5)
    End Sub

    Private Sub AutoLoad_SW_Toggled(sender As ToggleSwitch, e As RoutedEventArgs)
        CurrentSettings.IsAutoReload = sender.IsOn
    End Sub

    Private Sub SplitSettings_PaneClosed(sender As SplitView, args As Object)
        AttributeOnSerialize(CurrentSettings)
    End Sub

End Class
