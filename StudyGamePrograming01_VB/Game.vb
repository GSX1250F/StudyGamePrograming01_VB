Imports System.Security.Cryptography

Structure Vector2
    Dim x As Single
    Dim y As Single
End Structure

Public Class Game
    'Public
    Public Sub New()    'コンストラクタ
        InitializeComponent()       ' この呼び出しはデザイナーで必要です。
        mIsRunning = True
    End Sub

    Public Function Initialize() As Boolean
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

    'Private
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

    Private mWindow As Bitmap
    Private mRenderer As Graphics
    Private mTicksCount As New System.Diagnostics.Stopwatch()   'ゲーム開始時からの経過時間
    Private mIsRunning As Boolean

    'Game Specific
    Private mPaddleDir As Integer
    Private mPaddlePos As Vector2
    Private mBallPos As Vector2
    Private mBallVel As Vector2
    Private Const thickness As Integer = 15
    Private Const paddleH As Integer = 150
    Private mWindowW As Integer = 1024
    Private mWindowH As Integer = 768
    Private paddleImage As Image


End Class
