Imports System.Security.Cryptography

Public Class Game
    Private mWindowW As Integer     'PictureBoxの横幅
    Private mWindowH As Integer     'PictureBoxの縦幅
    Private mIsRunning As Boolean   'ゲームが実行中かどうか
    Private mWindow As Bitmap      'PictureBoxに表示するためのBitmapオブジェクト作成
    Private mRenderer As Graphics      'ImageオブジェクトのGraphicsオブジェクトを作成する
    Private mTicksCount As New System.Diagnostics.Stopwatch()   'ゲーム開始時からの経過時間
    Private mTicksCountPre As Long       'TicksCountの一時保持用。
    Private mKeyInputs As New List(Of System.Windows.Forms.KeyEventArgs)    'キー入力の配列

    Private mPaddlePos(2) As Single
    Private mBallPos(2) As Single
    Private mPaddleDir As Integer       'パドルの動く方向(+が下方向)
    Private mPaddleSpeed As Single      'パドルの速さの絶対値
    Private mPaddleH As Integer         'パドルの縦幅
    Private thickness As Integer        'ボールとパドルの横幅
    Private mBallSpeed(2) As Single      'ボールの速さ
    Private paddleImage As Bitmap        'パドルのビットマップイメージ（ストライプ）
    Private scene As Integer        '0 = playing , 1=stopping ,  2=gameover

    'フォーム作成時に最初に実行される
    Public Function Initialize() As Boolean
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = New Point(50, 50)         'ディスプレイの決まった位置でフォームが表示されるようにする。
        mWindowW = PictureBox.Width
        mWindowH = PictureBox.Height

        mWindow = New Bitmap(mWindowW, mWindowH)      'PictureBoxと同じ大きさの画像を作る
        mRenderer = Graphics.FromImage(mWindow)       '画像のGraphicsクラスを生成
        Timer.Enabled = True        'Timer有効化

        LoadData()      'ファイルを読み込んだり、初期設定するサブプログラム

        mTicksCount.Start()        'ストップウォッチを開始
        mTicksCountPre = mTicksCount.ElapsedMilliseconds        '前のフレームのTicksCountを初期化

        InitGame()      'ゲームの最初の状態を作る

        Return True
    End Function
    Public Sub LoadData()
        paddleImage = Image.FromFile(Application.StartupPath & "\Assets\paddle.png")
        mPaddleH = CInt(mWindowH * 0.196)
        mPaddleSpeed = CInt(mWindowH * 0.391)
        thickness = CInt(mPaddleH * 0.1)
    End Sub
    Public Sub InitGame()
        '各変数を初期化
        mPaddlePos = {thickness * 3, mWindowH / 2}
        mPaddleDir = 0
        mBallPos = {mWindowW / 2, mWindowH / 2}
        Dim rnd As New Random()
        Dim angle As Integer = rnd.Next(15, 75)
        Dim pmx As Integer = 2 * rnd.Next(0, 2) - 1
        Dim pmy As Integer = 2 * rnd.Next(0, 2) - 1
        mBallSpeed = {pmx * mWindowH * 0.4 * Math.Cos(angle / 180 * Math.PI), pmy * 300 * Math.Sin(angle / 180 * Math.PI)}
        scene = 0
    End Sub
    Public Sub Shutdown()
        mTicksCount.Stop()
        Me.Close()
    End Sub
    Private Sub Game_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        mIsRunning = True
        Dim success = Initialize()      '各変数を初期化
    End Sub
    Private Sub Timer_Tick(sender As Object, e As EventArgs) Handles Timer.Tick
        If mIsRunning = True Then
            ProcessInput()      '入力サブプログラム
            UpdateGame()        '更新サブプログラム
            GenerateOutput()    '出力サブプログラム
            mTicksCountPre = mTicksCount.ElapsedMilliseconds
        Else
            Shutdown()          '終了サブプログラム
        End If
    End Sub


    Private Sub ProcessInput()
        If mKeyInputs.Count = 0 Then
            mKeyInputs.Add(Nothing)     'キーイベントが無かったとき、Nothingを配列に格納
        End If

        mPaddleDir = 0
        For i As Integer = 0 To mKeyInputs.Count - 1
            If Not mKeyInputs(i) Is Nothing Then
                Select Case mKeyInputs(i).KeyCode
                    Case Keys.Escape    'ESCキーでゲーム終了
                        mIsRunning = False
                    Case Keys.Up
                        mPaddleDir = -1
                    Case Keys.Down
                        mPaddleDir = 1
                    Case Keys.S
                        If scene = 0 Then
                            scene = 1
                        Else
                            scene = 0
                        End If
                    Case Keys.R
                        If scene > 0 Then
                            InitGame()
                        End If
                End Select
            Else
                'キー入力がない時の処理
            End If
        Next
        mKeyInputs.Clear()
    End Sub
    Private Sub UpdateGame()
        'デルタタイムの計算
        Dim deltaTime As Single = (mTicksCount.ElapsedMilliseconds - mTicksCountPre) / 1000
        If scene = 0 Then
            'パドルを移動
            mPaddlePos(1) += mPaddleDir * mPaddleSpeed * deltaTime
            If mPaddlePos(1) < 0 + mPaddleH / 2 Then
                mPaddlePos(1) = 0 + mPaddleH / 2
            End If
            If mPaddlePos(1) > mWindowH - mPaddleH / 2 Then
                mPaddlePos(1) = mWindowH - mPaddleH / 2
            End If

            'ボールが左端にいったとき
            If (mBallPos(0) < 0) Then
                scene = 2
            End If

            'ボールの壁での跳ね返り
            If (mBallPos(0) + mBallSpeed(0) * deltaTime > mWindowW) Then
                mBallSpeed(0) *= -1
            End If
            If (mBallPos(1) + mBallSpeed(1) * deltaTime < 0) _
               Or (mBallPos(1) + mBallSpeed(1) * deltaTime > mWindowH) Then
                mBallSpeed(1) *= -1
            End If

            'ボールのパドルでの跳ね返り
            If (mBallPos(1) + mBallSpeed(1) * deltaTime > mPaddlePos(1) - mPaddleH / 2) _
               And (mBallPos(1) + mBallSpeed(1) * deltaTime < mPaddlePos(1) + mPaddleH / 2) _
               And (Math.Abs(mBallPos(0) + mBallSpeed(0) * deltaTime - mPaddlePos(0)) <= thickness) Then
                mBallSpeed(0) *= -1.1      '横方向ボールスピードup
            End If
            If (mBallPos(0) + mBallSpeed(0) * deltaTime > mPaddlePos(0) - thickness / 2) _
               And (mBallPos(0) + mBallSpeed(0) * deltaTime < mPaddlePos(0) + thickness / 2) _
               And (Math.Abs(mBallPos(1) + mBallSpeed(1) * deltaTime - mPaddlePos(1)) <= thickness / 2 + mPaddleH / 2) Then
                mBallSpeed(1) *= -1
            End If

            mBallPos(0) += mBallSpeed(0) * deltaTime
            mBallPos(1) += mBallSpeed(1) * deltaTime
        End If
    End Sub
    Private Sub GenerateOutput()
        mRenderer.Clear(Color.Black)    '画面のクリア
        If scene < 2 Then
            Dim brush As New SolidBrush(Color.FromArgb(255, 255, 255, 255))     'Brushオブジェクトの作成
            mRenderer.DrawImage(paddleImage, CInt(mPaddlePos(0) - thickness / 2), CInt(mPaddlePos(1) - mPaddleH / 2))       'スプライトをmRendererに出力
            'Dim paddle As Rectangle
            'With paddle
            '    .X = CInt(mPaddlePos(0) - thickness / 2)
            '    .Y = CInt(mPaddlePos(1) - mPaddleH / 2)
            '    .Width = thickness
            '    .Height = mPaddleH
            'End With
            'mRenderer.FillRectangle(brush, paddle)
            Dim ball As Rectangle
            With ball
                .X = CInt(mBallPos(0) - thickness / 2)
                .Y = CInt(mBallPos(1) - thickness / 2)
                .Width = thickness
                .Height = thickness
            End With
            mRenderer.FillEllipse(brush, ball)

            brush.Dispose()     'リソースを解放する

        Else
            Dim fnt As New Font("Yu Gothic UI", CInt(mWindowW * 0.1))
            Dim s As String
            s = "Game Over"
            mRenderer.DrawString(s, fnt, Brushes.White, CInt(mWindowW * 0.15), CInt(mWindowH * 0.33))

            fnt = New Font("Yu Gothic UI", CInt(mWindowW * 0.06))
            s = "Press R to restart"
            mRenderer.DrawString(s, fnt, Brushes.White, CInt(mWindowW * 0.18), CInt(mWindowH * 0.56))

            fnt.Dispose()       'リソースを解放する
        End If

        PictureBox.Image = mWindow      'PictureBoxに表示する
    End Sub

    Private Sub KeyState(sender As Object, keyState As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown    'Keyイベントハンドラ
        mKeyInputs.Add(keyState)    'キーコードを配列に入れる。
    End Sub
End Class
