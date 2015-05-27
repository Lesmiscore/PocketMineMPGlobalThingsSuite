Imports AnythingTools.Servers
Imports System.IO
Imports System.Text

Module Module1
    Dim money As New Dictionary(Of String, Integer)
    Dim bank As New Dictionary(Of String, Long)
    Dim players As New List(Of String)
    Sub Main()
        Console.WriteLine("Global Data Sync Server for PocketMine-MP")
        Console.WriteLine("Loading money.xml")
        If File.Exists("money.xml") Then
            Dim xd As XDocument
            Using sr As New StreamReader(New FileStream("money.xml", FileMode.Open), Encoding.UTF32)
                xd = XDocument.Load(sr)

            End Using
        Else

        End If

    End Sub
    Class PluginServiceServer
        Inherits HttpServer
        Public Overrides Sub OnRespose(sender As Object, e As HttpServer.OnResponseEventArgs)
            Dim writer As New StreamWriter(e.Response.OutputStream)
            Dim query As IDictionary(Of String, String) = QueryToDictionary(e.Request.RawUrl.Split("&").Last)
            Dim process As String = e.Request.RawUrl.Split("&").First.Split("/\").First.ToLower
            Dim player As String = query("player").ToLower 'same as PocketMine-MP's standard
            Select Case process
                Case "money" 'Economy & PocketMoney
                    SyncLock money
                        Select Case query("mode")
                            Case "get"
                                If money.ContainsKey(player) Then
                                    writer.WriteLine(money(player).ToString)
                                Else
                                    money(player) = 0
                                    writer.WriteLine("0")
                                End If
                            Case "set"
                                Dim transactionComplete = False
                                Dim transactionMoney = -1
                                Try
                                    transactionMoney = Integer.Parse(query("value"))
                                    If transactionMoney >= 0 Then
                                        transactionComplete = True
                                    End If
                                Catch ex As FormatException
                                    writer.WriteLine("FORMAT_ERROR")
                                Catch ex As OverflowException
                                    writer.WriteLine("OVERFLOW_ERROR")
                                Finally
                                    If transactionComplete Then
                                        money(player) = transactionMoney
                                        writer.WriteLine("TRANSACTION_COMPLETE")
                                    Else
                                        writer.WriteLine("TRANSACTION_ERROR")
                                    End If
                                End Try
                        End Select
                    End SyncLock
                Case "bank"
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
                        End Select
                    End SyncLock
            End Select
        End Sub
        Function QueryToDictionary(query As String) As IDictionary(Of String, String)
            Dim dic As New Dictionary(Of String, String)
            Dim splitted As String() = query.Trim("&").Split("?")
            For Each i In splitted
                Dim name = i.Split("=").First
                Dim value = i.Split("=").Last
                dic(name) = value
            Next
            Return dic
        End Function
    End Class
End Module
