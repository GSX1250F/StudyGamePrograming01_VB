Imports System.Security.Cryptography

Public Class Game
    Public mIsRunning As Boolean
    Public Sub New()    'コンストラクタ
        InitializeComponent()       ' この呼び出しはデザイナーで必要です。

        mIsRunning = True

    End Sub

    Public Function Initialize()
        Return True
    End Function

    Public Sub RunLoop()
        Do While mIsRunning = True
            ProcessInput()
            UpdateGame()
            GenerateOutput()
            Application.DoEvents()
        Loop
    End Sub
    Public Sub Shutdown()
        Me.Close()
    End Sub

    Private Sub ProcessInput()

    End Sub
    Private Sub UpdateGame()

    End Sub
    Private Sub GenerateOutput()

    End Sub

    Private Sub Game_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Q, Keys.Escape
                mIsRunning = False
        End Select
    End Sub
End Class
