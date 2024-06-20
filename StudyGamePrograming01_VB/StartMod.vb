Module StartMod
    Sub Main()
        Dim game As New Game
        Dim success As Boolean = game.Initialize()
        If success = True Then
            game.RunLoop()
        End If
        game.Shutdown()
    End Sub
End Module
