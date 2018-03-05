Imports Windows.UI.Input.Inking
Imports Windows.Storage.Streams
Imports Windows.Storage
Imports System.Xml.Serialization

Module OneInkFileOperation
    Private RootFolder As StorageFolder = ApplicationData.Current.LocalFolder
    '1.Windows.ApplicationModel.Package.Current.InstalledLocation
    '2.ApplicationData.Current.*


    Public Async Function InkCanvasSaveTo(TargetCanvas As InkCanvas) As Task
        '获取所有墨迹
        Dim CurrentStrokes As IReadOnlyList(Of InkStroke) = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
        If CurrentStrokes.Count > 0 Then
            '设置文件选取器
            '-----------------文件选取器默认位置设置-----------------
            Dim Picker As New Windows.Storage.Pickers.FileSavePicker With {
                .SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
            }
            '----------------------------------------------------

            '-----------------文件选取器默认格式设置-----------------
            Dim ValidSuffixList As New List(Of String) From {
                ".gif" '向List添加后缀，相当于ValidSuffixList.Add(".gif")
                } '保存类型为一个List类型对象。先声明一个List。
            Picker.FileTypeChoices.Add("支持ISF嵌入的GIF图像", ValidSuffixList) '向选取器添加后缀名注释
            Picker.DefaultFileExtension = ".gif" '向选取器添加后缀名
            Picker.SuggestedFileName = "OneInk" + DateTime.Now() '向选取器添加默认命名格式
            '-----------------------------------------------------

            Dim File As Windows.Storage.StorageFile = Await Picker.PickSaveFileAsync() '打开文件选取器，并异步等待用户选定文件，返回给File。
            '-----------------向File写入数据：.NET流操作-----------------
            If File IsNot Nothing Then
                '锁定File文件 避免意外的外部更改
                Windows.Storage.CachedFileManager.DeferUpdates(File)
                '新建文件流
                Dim Stream As IRandomAccessStream = Await File.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)

                '-----------------向流写入数据-----------------
                Using outputStream As IOutputStream = Stream.GetOutputStreamAt(0)
                    Dim i As Integer = Await TargetCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream) '“等待返回无值”有问题，应是“等待返回32位整数”
                    Dim r As Boolean = Await outputStream.FlushAsync()
                End Using
                Stream.Dispose() '吃饭擦嘴
                '--------------------------------------------

                '自动保存的最近文件
                MainPage.CurrentSettings.RecentPicNum = MainPage.CurrentSettings.RecentPicNum + 1
                If MainPage.CurrentSettings.RecentPicNum = 4 Then
                    MainPage.CurrentSettings.RecentPicNum = 1
                End If
                Await File.CopyAsync(RootFolder, MainPage.CurrentSettings.RecentPicNum & ".gif", CreationCollisionOption.ReplaceExisting)

                '解锁File
                Dim status As Windows.Storage.Provider.FileUpdateStatus = Await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(File)

                If status = Windows.Storage.Provider.FileUpdateStatus.Complete Then
                    'File saved.
                Else
                    'File couldn't be saved.
                End If


            Else '文件选取器中 当用户点了取消...

            End If
            '-----------------------------------------------------
        End If
    End Function

    Public Async Sub InkCanvasOpenFrom(TargetCanvas As InkCanvas)
        Dim Picker As New Windows.Storage.Pickers.FileOpenPicker With {
            .SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
        }
        Picker.FileTypeFilter.Add(".gif")
        Dim File As Windows.Storage.StorageFile = Await Picker.PickSingleFileAsync()
        If File IsNot Nothing Then
            Dim Stream As IRandomAccessStream = Await File.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Using inputStream As Object = Stream.GetInputStreamAt(0)
                Await TargetCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream)
            End Using
            Stream.Dispose()
        Else
            'Operation cancelled
        End If

    End Sub

    Public Sub CopyToClipboard(TargetCanvas As InkCanvas)
        Dim Rectangle As Rect = TargetCanvas.InkPresenter.StrokeContainer.BoundingRect
        '_rect = Rect;

        Dim strokes As IReadOnlyList(Of InkStroke) = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
        For Each stroke As Object In strokes
            stroke.Selected = True
        Next

        'DrawBoundingRect()
        TargetCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard()
        'TODO:Pop hint.
    End Sub

    Public Async Function SeqEnumRecentPic(TargetCanvas As InkCanvas) As Task(Of IReadOnlyList(Of BitmapImage))
        Dim EnumPicFileList As IReadOnlyList(Of StorageFile) = Await RootFolder.GetFilesAsync()
        Dim RecentPicFileList As New List(Of StorageFile)
        Dim SeqRecentPicList As New List(Of BitmapImage)

        '因为RootFolder.GetFilesAsync()返回的是IReadOnly
        '先把这操蛋的玩意转换成可修改的
        For Each file As StorageFile In EnumPicFileList
            If file.FileType = ".gif" Then
                RecentPicFileList.Add(file)
            End If
        Next

        '对List按创建时间排序：数据结构好好学orz
        '获取文件属性，如创建日期
        If RecentPicFileList.Count <> 0 Then
            For current = 0 To RecentPicFileList.Count - 2
                Dim newer As Integer = current
                Dim pptr1 As Windows.Storage.FileProperties.BasicProperties = Await RecentPicFileList.Item(newer).GetBasicPropertiesAsync()

                For index = current + 1 To RecentPicFileList.Count - 1
                    Dim pptr2 As Windows.Storage.FileProperties.BasicProperties = Await RecentPicFileList.Item(index).GetBasicPropertiesAsync()
                    If pptr2.DateModified.CompareTo(pptr1.DateModified) = 1 Then ' If "pptr2.DateModified" earlier than "pptr1.DateModified" Then
                        newer = index
                    End If
                Next

                If newer <> current Then
                    Dim temp As StorageFile = Nothing
                    temp = RecentPicFileList.Item(current)
                    RecentPicFileList.Item(current) = RecentPicFileList.Item(newer)
                    RecentPicFileList.Item(newer) = temp
                End If
            Next
        End If

        '然后丢进PicList
        For Each RecentPic As StorageFile In RecentPicFileList
            Dim PicStream As IRandomAccessStream = Await RecentPic.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Dim pic As New BitmapImage
            pic.SetSource(PicStream)
            SeqRecentPicList.Add(pic)
        Next


        '用Nothing补齐FileList中没有的到PicList中
        For i = 0 To (3 - RecentPicFileList.Count)
            SeqRecentPicList.Add(Nothing)
        Next

        '其实把自动加载画布的功能写在这里不好...
        '除非能找到BitmapImage对象写入流的方法
        If MainPage.CurrentSettings.IsAutoReload = True Then
            If RecentPicFileList.Count <> 0 Then
                Dim Stream As IRandomAccessStream = Await RecentPicFileList(0).OpenAsync(Windows.Storage.FileAccessMode.Read)
                Using inputStream As Object = Stream.GetInputStreamAt(0)
                    Await TargetCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream)
                End Using
                Stream.Dispose()
            End If
        End If
        Return SeqRecentPicList
    End Function

    Public Async Sub AttributeOnSerialize(attr As OneInkUIAttributes)
        Using MemStream As MemoryStream = New MemoryStream
            Dim Serializer As New XmlSerializer(MainPage.CurrentSettings.GetType)
            Serializer.Serialize(MemStream, MainPage.CurrentSettings)

            '1.绑定文件流到文件
            Dim AttrFile As StorageFile = Await RootFolder.CreateFileAsync("DefaultSetting.xml", CreationCollisionOption.ReplaceExisting)
            Dim AttrStream As IRandomAccessStream = Await AttrFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)

            '2.指定“光标”从文件流的起始位置开始
            Dim outputStream As IOutputStream = AttrStream.GetOutputStreamAt(0)

            '3.向流中写入数据
            Dim AttrToString As String
            MemStream.Position = 0
            Dim Reader As StreamReader = New StreamReader(MemStream)
            AttrToString = Reader.ReadToEnd()

            Dim c() As Byte = System.Text.Encoding.ASCII.GetBytes(AttrToString)
            'temp.Write只能接受byte()做参数
            '这里做了如下转换：
            'String -> Byte() -> IBuffer
            '能不简单点？

            Dim dtwriter As New DataWriter
            dtwriter.WriteBytes(c)
            Await outputStream.WriteAsync(dtwriter.DetachBuffer())

            '4.刷新缓冲区
            Dim r As Boolean = Await outputStream.FlushAsync()

            '5.吃饭擦嘴
            outputStream.Dispose()
            AttrStream.Dispose()
            MemStream.Dispose()
        End Using

    End Sub

    Public Async Function AutoAttrDeSerialize() As Task(Of OneInkUIAttributes)
        Dim DeSerilizedAttr As New OneInkUIAttributes
        Dim Serializer As New XmlSerializer(DeSerilizedAttr.GetType())
        Dim AttrFile As StorageFile = Await RootFolder.TryGetItemAsync("DefaultSetting.xml")

        If AttrFile IsNot Nothing Then
            Dim AttrStream As IRandomAccessStream = Await AttrFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Dim OnProceedingStream As Stream = AttrStream.AsStream()

            DeSerilizedAttr = Serializer.Deserialize(OnProceedingStream)

            AttrStream.Dispose()
            OnProceedingStream.Dispose()
        End If


        Return DeSerilizedAttr
    End Function

End Module
