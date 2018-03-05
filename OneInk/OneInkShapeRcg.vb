Imports Windows.UI
Imports Windows.UI.Input.Inking
Imports Windows.UI.Input.Inking.Analysis
Imports Windows.UI.Xaml.Shapes

Module OneInkShapeRcg

    Private ThemeColor As Color = MainPage.CurrentSettings.ThemeColor
    Dim TextAnalyzer As InkAnalyzer = New InkAnalyzer()
    Dim InkText As IReadOnlyList(Of InkStroke)
    Dim ResultText As InkAnalysisResult
    Dim Words As IReadOnlyList(Of IInkAnalysisNode)

    Public Async Function WritingRecognize(Strokes As IReadOnlyList(Of InkStroke), TargetCanvas As InkCanvas) As Task(Of String)
        Dim analyzerText As InkAnalyzer = New InkAnalyzer()
        Dim strokesText As IReadOnlyList(Of InkStroke) = Nothing
        Dim resultText As InkAnalysisResult = Nothing
        Dim words As IReadOnlyList(Of IInkAnalysisNode) = Nothing
        Dim ResultStr As String = ""

        strokesText = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
        'Ensure an ink stroke Is present.
        If strokesText.Count > 0 Then
            analyzerText.AddDataForStrokes(strokesText)

            'Force analyzer to process strokes as handwriting.
            For Each stroke As InkStroke In strokesText 'VB.NET允许指明一组对象中的每一个：each关键字
                analyzerText.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Writing)
            Next
            'Clear recognition results string.
            ResultStr = ""

            resultText = Await analyzerText.AnalyzeAsync()

            If resultText.Status = InkAnalysisStatus.Updated Then 'Analyze start.
                Dim Text As String = analyzerText.AnalysisRoot.RecognizedText
                words = analyzerText.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord)
                For Each word As IInkAnalysisNode In words
                    Dim concreteWord As InkAnalysisInkWord = word '(InkAnalysisInkWord) word in C#
                    For Each s As String In concreteWord.TextAlternates
                        ResultStr += s + " "
                    Next
                    ResultStr += " / "
                Next
                analyzerText.ClearDataForAllStrokes()
            End If
        End If

        Return ResultStr
    End Function

    Public Async Function ShapeRcn(TargetCanvas As InkCanvas, TargetInkToolBar As InkToolbar) As Task
        Dim analyzerShape As InkAnalyzer = New InkAnalyzer
        Dim strokesShape As IReadOnlyList(Of InkStroke) = Nothing
        Dim resultShape As InkAnalysisResult = Nothing

        strokesShape = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()

        If strokesShape.Count > 0 Then
            analyzerShape.AddDataForStrokes(strokesShape)

            resultShape = Await analyzerShape.AnalyzeAsync()

            If resultShape.Status = InkAnalysisStatus.Updated Then
                Dim drawings As List(Of IInkAnalysisNode) = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing)

                For Each drawing As IInkAnalysisNode In drawings
                    Dim Shape As InkAnalysisInkDrawing = drawing '!!!!!"(InkAnalysisInkDrawing) drawing"
                    If Shape.DrawingKind = InkAnalysisDrawingKind.Drawing Then
                        Debug.WriteLine("Unsupported drawing!")
                    Else
                        If Shape.DrawingKind <> InkAnalysisDrawingKind.Circle And Shape.DrawingKind <> InkAnalysisDrawingKind.Ellipse Then
                            'DrawEllipse(Shape)
                            'Else
                            Dim RcgedPolygon As Polygon = DrawPolygon(Shape)
                            PolygonRedrawToCanvas(RcgedPolygon, TargetCanvas, TargetInkToolBar)
                            For Each strokeid As UInteger In Shape.GetStrokeIds()
                                Dim stroke As InkStroke = TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeid)
                                stroke.selected = True
                            Next
                        End If
                    End If
                    analyzerShape.RemoveDataForStrokes(Shape.GetStrokeIds())
                Next
                TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected()
            End If
        End If

    End Function

    Public Function DrawEllipse(shape As InkAnalysisInkDrawing)
        Dim points As IReadOnlyList(Of Point) = shape.Points

        'Instead of "Dim points As Object"
        'var关键字是C#3.0开始新增的特性，称为推断类型（其实也就是弱化类型的定义）。
        'VAR可代替任何类型，编译器会根据上下文来判断你到底是想用什么类型，类似 Object，但是效率比OBJECT高点。
        '.NET中所有类型都派生自Object，因此可以根据类型兼容规则把任意类型强制转换为Object使用
        '...当然只能使用继承下去的那一部分而已了。
        'HERE:http://blog.csdn.net/mrobama/article/details/53812898

        Dim Ellipse As New Ellipse With {
            .Width = Math.Sqrt((points(0).X - points(2).X) * (points(0).X - points(2).X) +
                                (points(0).Y - points(2).Y) * (points(0).Y - points(2).Y)),
            .Height = Math.Sqrt((points(1).X - points(3).X) * (points(1).X - points(3).X) +
                                (points(1).Y - points(3).Y) * (points(1).Y - points(3).Y))
        }

        Dim rotAngle As Double = Math.Atan2(points(2).Y - points(0).Y, points(2).X - points(0).X)
        Dim RotateTransform As New RotateTransform With {
            .Angle = rotAngle * 180 / Math.PI,
            .CenterX = Ellipse.Width / 2.0,
            .CenterY = Ellipse.Height / 2.0
        }

        Dim TranslateTransform As New TranslateTransform With {
            .X = shape.Center.X - Ellipse.Width / 2.0,
            .Y = shape.Center.Y - Ellipse.Height / 2.0
        }

        Dim TransformGroup As New TransformGroup
        TransformGroup.Children.Add(RotateTransform)
        TransformGroup.Children.Add(TranslateTransform)
        Ellipse.RenderTransform = TransformGroup

        Dim Brush As New SolidColorBrush(ThemeColor)
        Ellipse.Stroke = Brush
        Ellipse.StrokeThickness = 2

        Return Ellipse
        '重绘至drawingcanvas???
        'Dim strokefromshape As New InkStrokeBuilder
        'Dim shapercged As InkStroke = strokefromshape.CreateStroke(shape.Points) '如何获得形状的点迹？

        'DrawingCanvas.InkPresenter.StrokeContainer.AddStroke(shapercged)

        'ShapeRcgLayer.Children.Clear()

    End Function

    Public Function DrawPolygon(shape As InkAnalysisInkDrawing)
        Dim points As List(Of Point) = shape.Points
        Dim Polygon As New Polygon

        For Each point As Point In points
            Polygon.Points.Add(point)
        Next

        Dim Brush As New SolidColorBrush(ThemeColor)
        Polygon.Stroke = Brush
        Polygon.StrokeThickness = 2

        Return Polygon
    End Function

    Public Sub PolygonRedrawToCanvas(TargetShape As Polygon, TargetCanvas As InkCanvas, TargetInkToolBar As InkToolbar)
        '重绘至DrawingCanvas
        Dim StrokeFromShape As New InkStrokeBuilder
        Dim CurrntInkPenAttr As InkDrawingAttributes = TargetInkToolBar.InkDrawingAttributes
        StrokeFromShape.SetDefaultDrawingAttributes(CurrntInkPenAttr)

        Dim Lines(TargetShape.Points.Count - 1) As InkStroke

        For i As Integer = 0 To TargetShape.Points.Count - 1
            Dim CurrentLinePoints() As Point = {TargetShape.Points(i), TargetShape.Points(i + 1)}
            Lines(i) = StrokeFromShape.CreateStroke(CurrentLinePoints)
        Next
        Dim CappingLinePoints() As Point = {TargetShape.Points(0), TargetShape.Points(TargetShape.Points.Count - 1)}
        Lines(TargetShape.Points.Count - 1) = StrokeFromShape.CreateStroke(CappingLinePoints)

        TargetCanvas.InkPresenter.StrokeContainer.AddStrokes(Lines)

        'ShapeRcgLayer.Children.Clear()

        Dim a = 0
        For Each st As InkStroke In TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
            a = a + 1
            Debug.WriteLine(a)
        Next
        Debug.WriteLine("+++++++++++++1s")

    End Sub

End Module
