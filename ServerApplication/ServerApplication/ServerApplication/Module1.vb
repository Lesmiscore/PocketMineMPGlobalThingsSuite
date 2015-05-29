Imports AnythingTools.Servers
Imports System.IO
Imports System.Text
Imports System.IO.Compression
Imports System.Timers
Imports AnythingTools

Module Module1
    Dim money As New Dictionary(Of String, Integer)
    Dim bank As New Dictionary(Of String, Long)
    Dim config As New Dictionary(Of String, String)
    Dim commands As New Dictionary(Of String, Action(Of String()))
    Dim playerSync As New Object
    Dim timer As New Timer
    Dim server As PluginServiceServer
    Sub Main()
        Console.WriteLine("Global Data Sync Server for PocketMine-MP")
        Console.WriteLine("Loading money.dat")
        If File.Exists("money.dat") Then
            Dim xd As XDocument
            Using sr As New StreamReader(New GZipStream(New FileStream("money.dat", FileMode.Open), CompressionMode.Decompress), Encoding.UTF32)
                xd = XDocument.Load(sr)
            End Using
            For Each i In xd.<Money>.<Player>
                money(i.@player) = Integer.Parse(i.@value)
            Next
        End If
        Console.WriteLine("Loading bank.dat")
        If File.Exists("bank.dat") Then
            Dim xd As XDocument
            Using sr As New StreamReader(New GZipStream(New FileStream("bank.dat", FileMode.Open), CompressionMode.Decompress), Encoding.UTF32)
                xd = XDocument.Load(sr)
            End Using
            For Each i In xd.<Bank>.<Player>
                bank(i.@player) = Long.Parse(i.@value)
            Next
        End If
        Console.WriteLine("Loading config.xml")
        If File.Exists("config.xml") Then
            Dim xd As XDocument
            Using sr As New StreamReader(New FileStream("config.xml", FileMode.Open), Encoding.UTF8)
                xd = XDocument.Load(sr)
            End Using
            For Each i In xd.<Config>.<Entry>
                config(i.@name) = i.@value
            Next
        Else
            config("initialMoney") = 0
            config("disableBank") = False
        End If
        Console.WriteLine("Starting server...")
        server = New PluginServiceServer
        server.Ports.Clear()
        server.Ports.Add(20200)
        server.StartServer()
        Console.WriteLine("Preparing saving timer...")
        timer.Interval = 1000 * 60 * 5
        AddHandler timer.Elapsed, Sub(sender, e)
                                      SaveConfigs()
                                  End Sub
        timer.Start()
        Console.WriteLine("Done! You can start PocketMine-MP!")
        commands("stop") = Sub(args)
                               Console.WriteLine("Stopping application...")
                               Console.WriteLine("Stopping server...")
                               server.StopServer()
                               Console.WriteLine("Stopping timer...")
                               timer.Stop()
                               Console.WriteLine("Saving files...")
                               SaveConfigs()
                               Console.WriteLine("Stopping process...")
                               Process.GetCurrentProcess.Kill()
                           End Sub
        commands("reload") = Sub(args)
                                 Console.WriteLine("Reloading config...")
                                 If File.Exists("config.xml") Then
                                     Dim xd As XDocument
                                     Using sr As New StreamReader(New FileStream("config.xml", FileMode.Open), Encoding.UTF8)
                                         xd = XDocument.Load(sr)
                                     End Using
                                     For Each i In xd.<Config>.<Entry>
                                         config(i.@name) = i.@value
                                     Next
                                 Else
                                     config("initialMoney") = 0
                                     config("disableBank") = False
                                     config("acceptGiveMoney") = False
                                 End If
                             End Sub
        commands("config") = Sub(args)
                                 If args.Length < 2 Then
                                     Console.WriteLine("Usage: config <NAME> <VALUE>")
                                     For Each i In config
                                         Console.WriteLine(i.Key + "=" + i.Value)
                                     Next
                                     Return
                                 End If
                                 config(args(0)) = args(1)
                             End Sub
        'commands("stop") = Sub(args)

        '                   End Sub
        While True
            Dim command = Console.ReadLine
            Try
                Dim splitted = command.Split(" ")
                commands(splitted.First)(splitted.Skip(1).ToArray)
            Catch ex As Exception
                Tools.PrintException(ex)
            End Try
        End While
    End Sub
    Function SaveConfigs() As Boolean
        Dim result = True
        Try
            SyncLock money
                Dim moneyXml As XDocument = XDocument.Parse("<Money></Money>")
                For Each i In money
                    Dim node = <Player player="" value=""/>
                    node.@player = i.Key
                    node.@value = i.Value
                    moneyXml.Root.Add(node)
                Next
                Using sw As New StreamWriter(New GZipStream(New FileStream("money.dat", FileMode.Create), CompressionMode.Compress))
                    moneyXml.Save(sw)
                End Using
            End SyncLock
        Catch ex As Exception
            Tools.PrintExceptionD(ex)
            result = False
        End Try
        Try
            SyncLock bank
                Dim bankXml As XDocument = XDocument.Parse("<Bank></Bank>")
                For Each i In bank
                    Dim node = <Player player="" value=""/>
                    node.@player = i.Key
                    node.@value = i.Value
                    bankXml.Root.Add(node)
                Next
                Using sw As New StreamWriter(New GZipStream(New FileStream("bank.dat", FileMode.Create), CompressionMode.Compress))
                    bankXml.Save(sw)
                End Using
            End SyncLock
        Catch ex As Exception
            Tools.PrintExceptionD(ex)
            result = False
        End Try
        Try
            SyncLock config
                Dim configXml As XDocument = XDocument.Parse("<Config></Config>")
                For Each i In config
                    Dim node = <Entry name="" value=""/>
                    node.@name = i.Key
                    node.@value = i.Value
                    configXml.Root.Add(node)
                Next
                Using sw As New StreamWriter(New FileStream("config.xml", FileMode.Create))
                    configXml.Save(sw)
                End Using
            End SyncLock
        Catch ex As Exception
            Tools.PrintExceptionD(ex)
            result = False
        End Try
        Return result
    End Function
    Class PluginServiceServer
        Inherits HttpServer
        Public Overrides Sub OnRespose(sender As Object, e As HttpServer.OnResponseEventArgs)
            Dim writer As New StreamWriter(e.Response.OutputStream)
            Dim query As IDictionary(Of String, String) = QueryToDictionary(e.Request.RawUrl.Split("&").Last)
            Dim process As String = e.Request.RawUrl.Split("&").First.Split("/\").First.ToLower
            Dim player As String = query("player").ToLower 'same as PocketMine-MP's standard
            Select Case process
                Case "ping"
                    writer.WriteLine("RETURN_PONG")
                Case "money" 'Economy & PocketMoney
                    SyncLock money
                        Select Case query("mode")
                            Case "get"
                                If money.ContainsKey(player) Then
                                    writer.WriteLine(money(player).ToString)
                                Else
                                    money(player) = Integer.Parse(config("initialMoney"))
                                    writer.WriteLine(config("initialMoney"))
                                End If
                            Case "set"
                                Dim transactionComplete = False
                                Dim transactionMoney = -1
                                Try
                                    transactionMoney = Integer.Parse(query("value"))
                                    If transactionMoney >= 0 Then
                                        money(player) = transactionMoney
                                        transactionComplete = True
                                    End If
                                Catch ex As FormatException
                                    writer.WriteLine("FORMAT_ERROR")
                                Catch ex As OverflowException
                                    writer.WriteLine("OVERFLOW_ERROR")
                                Finally
                                    If transactionComplete Then
                                        writer.WriteLine("TRANSACTION_COMPLETE")
                                    Else
                                        writer.WriteLine("TRANSACTION_ERROR")
                                    End If
                                End Try
                            Case "givemoney"
                                If Not StrToBool(config("acceptGiveMoney")) Then
                                    writer.WriteLine("DENIED_UNFAIR")
                                    Return
                                End If
                                Dim transactionComplete = False
                                Dim transactionMoney = -1
                                Try
                                    transactionMoney = Integer.Parse(query("value"))
                                    money(player) += transactionMoney
                                    transactionComplete = True
                                Catch ex As FormatException
                                    writer.WriteLine("FORMAT_ERROR")
                                Catch ex As OverflowException
                                    writer.WriteLine("OVERFLOW_ERROR")
                                Finally
                                    If transactionComplete Then
                                        writer.WriteLine("TRANSACTION_COMPLETE")
                                    Else
                                        writer.WriteLine("TRANSACTION_ERROR")
                                    End If
                                End Try
                            Case "takemoney"
                                If Not StrToBool(config("acceptGiveMoney")) Then
                                    writer.WriteLine("DENIED_UNFAIR")
                                    Return
                                End If
                                Dim transactionComplete = False
                                Dim transactionMoney = -1
                                Try
                                    transactionMoney = Integer.Parse(query("value"))
                                    money(player) -= transactionMoney
                                    transactionComplete = True
                                Catch ex As FormatException
                                    writer.WriteLine("FORMAT_ERROR")
                                Catch ex As OverflowException
                                    writer.WriteLine("OVERFLOW_ERROR")
                                Finally
                                    If transactionComplete Then
                                        writer.WriteLine("TRANSACTION_COMPLETE")
                                    Else
                                        writer.WriteLine("TRANSACTION_ERROR")
                                    End If
                                End Try
                            Case "existAccount"
                                If money.ContainsKey(player) Then
                                    writer.WriteLine("ACCOUNT_EXISTS")
                                Else
                                    writer.WriteLine("ACCOUNT_NOT_EXISTS")
                                End If
                            Case "deleteAccount"
                                If money.ContainsKey(player) Then
                                    Try
                                        If money.Remove(player) Then
                                            writer.WriteLine("ACCOUNT_DELETE_COMPLETE")
                                        Else
                                            writer.WriteLine("ACCOUNT_DELETE_ERROR")
                                        End If
                                    Catch ex As Exception
                                        writer.WriteLine("ACCOUNT_DELETE_ERROR")
                                    End Try
                                Else
                                    writer.WriteLine("ACCOUNT_NOT_EXISTS")
                                End If
                        End Select
                    End SyncLock
                Case "bank"
                    If Not StrToBool(config("disableBank")) Then
                        writer.WriteLine("BANK_DISABLED")
                        Return
                    End If
                    SyncLock bank
                        Select Case query("mode")
                            Case "deposit"
                                Dim transactionComplete = False
                                Dim transactionMoney = -1
                                Try
                                    transactionMoney = Long.Parse(query("value"))
                                    If Not bank.ContainsKey(player) Then
                                        Return
                                    End If
                                    If (bank(player) - transactionMoney) < 0 Then
                                        Return
                                    End If
                                    If transactionMoney >= 0 Then
                                        SyncLock money
                                            money(player) += transactionMoney
                                            bank(player) -= transactionMoney
                                        End SyncLock
                                        transactionComplete = True
                                    Else
                                        transactionComplete = False
                                    End If
                                Catch ex As FormatException
                                    writer.WriteLine("FORMAT_ERROR")
                                Catch ex As OverflowException
                                    writer.WriteLine("OVERFLOW_ERROR")
                                Finally
                                    If transactionComplete Then
                                        writer.WriteLine("TRANSACTION_COMPLETE")
                                    Else
                                        writer.WriteLine("TRANSACTION_ERROR")
                                    End If
                                End Try
                            Case "get"
                                If bank.ContainsKey(player) Then
                                    writer.WriteLine(bank(player).ToString)
                                Else
                                    bank(player) = 0
                                    writer.WriteLine("0")
                                End If
                            Case "enabled"
                                writer.WriteLine("BANK_ENABLED")
                        End Select
                    End SyncLock
                Case "player"
                    SyncLock playerSync
                        If File.Exists("playersInfo.txt") Then
                            File.AppendAllLines("playersInfo.txt", {query("name") & "|" &
                                                                    query("address") & "|" &
                                                                    query("cid") & "|" &
                                                                    e.Request.RemoteEndPoint.Address.ToString}, Encoding.UTF8)
                        Else
                            File.WriteAllLines("playersInfo.txt", {query("name") & "|" &
                                                                    query("address") & "|" &
                                                                    query("cid") & "|" &
                                                                    e.Request.RemoteEndPoint.Address.ToString}, Encoding.UTF8)
                        End If
                    End SyncLock
                Case "config"
                    SyncLock config
                        Select Case query("mode")
                            Case "get"
                                writer.WriteLine(config(query("key")))
                        End Select
                    End SyncLock
            End Select
        End Sub
        Function QueryToDictionary(query As String) As IDictionary(Of String, String)
            Dim dic As New Dictionary(Of String, String)
            Dim splitted As String() = query.Trim("&").Split("?")
            For Each i In splitted
                If Not i.Contains("=") Then
                    Continue For
                End If
                Dim name = i.Split("=").First
                Dim value = i.Split("=").Last
                dic(name) = value
            Next
            Return dic
        End Function
        Function StrToBool(s As String) As Boolean
            Select Case s
                Case "on", "true", Boolean.TrueString, "yes", "1"
                    Return True
                Case "", "off", "false", Boolean.FalseString, "no", "0"
                    Return False
                Case Else
                    Return False
            End Select
        End Function
    End Class
End Module
