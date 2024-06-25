Imports System.Runtime.InteropServices
Imports System.Security.Cryptography

Structure Vector2
    Dim x As Single
    Dim y As Single
End Structure

Public Class Game
    'キーボード入力用Windows API導入
    <DllImport("user32.dll", ExactSpelling:=True)>
    Private Shared Function GetKeyboardState(ByVal keyStates() As Byte) As Boolean
    End Function

    '変数宣言
    Private mWindow As Bitmap       'PictureBoxと同じサイズのビットマップ
    Private mRenderer As Graphics   '描画用レンダラー
    Private mWindowWidth As Integer      'PictureBoxの横幅
    Private mWindowHeight As Integer     'PictureBoxの縦幅
    Private Ticks As New System.Diagnostics.Stopwatch()     'ゲーム開始からの経過時間
    Private mTicksCount As Integer     '時間管理（秒）
    Private mIsRunning As Boolean   'ゲーム実行中
    Private mKeyStateByte(255) As Byte      'キーボード入力検知
    Private mKeyState(255) As Boolean      'キーボード状態

    'Game Specific
    Private mPaddleDir As Integer               'パドルの動作方向。+が下方向、-が上方向。
    Private mPaddlePos As New Vector2           'パドルの位置（2次元ベクトル形式）
    Private mPaddleSpeed As Single              'パドルの動作速度
    Private mBallPos As New Vector2             'パドルの位置（2次元ベクトル形式）
    Private mBallVel As New Vector2             'ボールの速度（2次元ベクトル形式）
    Private Const thickness As Integer = 15     '壁・ボール・パドルの厚み
    Private Const paddleH As Integer = 150      'パドルの高さ
    Private paddleImage As Image                'パドルのテクスチャ
    Private scene As Integer                    '0:ゲーム中 , 1:ポーズ中 , 2:ゲームオーバー
    Private pause(2) As Boolean                    'ポーズ中のフラグ
    Private mFontSize As Integer = 100    'テキストのフォントサイズ
    Private mText As New List(Of String)        'テキスト
    Private mTextPos As New List(Of Vector2)    'テキスト表示位置
    Private mTextStyle As New List(Of Font)     'テキストスタイル


    Private Sub Game_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        mWindow = Nothing
        mRenderer = Nothing
        mWindowWidth = 1024
        mWindowHeight = 768

        Dim success = Initialize()
        If success = True Then
            mIsRunning = True
            LoadData()
            InitGame()
        Else
            Shutdown()
        End If
    End Sub

    Private Function Initialize() As Boolean
        'フォームを初期化
        Me.SetDesktopBounds(100, 100, mWindowWidth + 26, mWindowHeight + 49)
        Me.DoubleBuffered = Enabled
        PictureBox.SetBounds(5, 5, mWindowWidth, mWindowHeight)
        PictureBox.BackColor = Color.Black
        '描画を初期化
        mWindow = New Bitmap(mWindowWidth, mWindowHeight)
        mRenderer = Graphics.FromImage(mWindow)
        'ストップウォッチ開始
        Ticks.Start()
        'タイマー開始
        RunLoop.Interval = 16
        RunLoop.Enabled = True
        mTicksCount = Ticks.ElapsedMilliseconds

        Return True
    End Function
    Private Sub RunLoop_Tick(sender As Object, e As EventArgs) Handles RunLoop.Tick
        If mIsRunning Then
            ProcessInput()
            UpdateGame()
            GenerateOutput()
        Else
            Shutdown()
        End If
    End Sub
    Private Sub ProcessInput()
        GetKeyboardState(mKeyStateByte)
        For i As Integer = 0 To mKeyStateByte.Count - 1
            'キー入力状態を、ON=True, OFF=Falseに変換
            mKeyState(i) = CBool(mKeyStateByte(i) And &H80)
        Next

        'ゲーム終了
        If mKeyState(Keys.Escape) = True Then
            mIsRunning = False
        End If

        'パドル移動
        mPaddleDir = 0
        If mKeyState(Keys.Up) = True Then
            mPaddleDir = -1
        ElseIf mKeyState(Keys.Down) = True Then
            mPaddleDir = 1
        End If

        'ポーズ機能
        'チャタリング防止とKeyDown/Upで使う方がやりやすいので、イベントハンドラで実装

        'リスタート機能
        If mKeyState(Keys.R) = True Then
            If scene > 0 Then
                InitGame()
            End If
        End If
    End Sub
    Private Sub UpdateGame()
        '前のフレームから16ms経つまで待つ(≒60fps)
        While Ticks.ElapsedMilliseconds < mTicksCount + 16
        End While
        'デルタタイムの計算
        Dim deltaTime As Single = (Ticks.ElapsedMilliseconds - mTicksCount) / 1000

        'デルタタイムを最大値で制限する(=20fps)
        If deltaTime > 0.05 Then
            deltaTime = 0.05
        End If
        mTicksCount = Ticks.ElapsedMilliseconds

        If (scene = 0) Then
            'パドル位置の更新
            mPaddlePos.y += mPaddleDir * mPaddleSpeed * deltaTime
            If (mPaddlePos.y < (paddleH / 2.0 + thickness)) Then
                mPaddlePos.y = paddleH / 2.0 + thickness
            End If
            If (mPaddlePos.y > (mWindowHeight - paddleH / 2.0 - thickness)) Then
                mPaddlePos.y = mWindowHeight - paddleH / 2.0 - thickness
            End If

            '更新後のボール位置を計算
            Dim mBallPosPost As Vector2
            mBallPosPost.x = mBallPos.x + mBallVel.x * deltaTime
            mBallPosPost.y = mBallPos.y + mBallVel.y * deltaTime
            'ボールが壁に当たったら跳ね返る
            If (mBallPosPost.x + thickness * 0.5 >= (mWindowWidth - thickness) And mBallVel.x > 0.0) Then
                mBallVel.x *= -1.0
                mBallPosPost.x = mWindowWidth - thickness - thickness * 0.5
            End If
            If (mBallPosPost.y - thickness * 0.5 <= thickness And mBallVel.y < 0.0) Then
                mBallVel.y *= -1.0
                mBallPosPost.y = thickness + thickness * 0.5
            End If
            If (mBallPosPost.y + thickness * 0.5 >= mWindowHeight - thickness And mBallVel.y > 0.0) Then
                mBallVel.y *= -1.0
                mBallPosPost.y = mWindowHeight - thickness - thickness * 0.5
            End If
            'パドルでボールが跳ね返る判定
            '更新後のボール左端がパドル右端より小さく、ボールが左向きであるときに、
            'ボール更新前と更新後の直線を求め、パドルのx座標でのy座標がパドル範囲内であるか
            If (mBallPosPost.x < mPaddlePos.x + thickness And mBallVel.x < 0.0) Then
                Dim intersection_y As Single = (mBallPos.y - mBallPosPost.y) / (mBallPos.x - mBallPosPost.x) * (mPaddlePos.x - mBallPosPost.x) + mBallPosPost.y
                If (intersection_y >= mPaddlePos.y - paddleH * 0.5 And intersection_y <= mPaddlePos.y + paddleH * 0.5) Then
                    mBallVel.x *= -1.1      '横方向ボールスピードup
                    If (mBallVel.x < -2500.0) Then mBallVel.x = -2500
                    If (mBallVel.x > 2500.0) Then mBallVel.x = 2500.0
                    mBallPosPost.x = mPaddlePos.x + thickness
                End If
            End If
            'ボール位置を更新
            mBallPos.x = mBallPosPost.x
            mBallPos.y = mBallPosPost.y

            'ボールが左端にいってしまったらゲームオーバー。
            If (mBallPos.x <= 0.0F) Then
                scene = 2
            End If
        End If
    End Sub

    Private Sub GenerateOutput()
        mRenderer.Clear(Color.Black)    '背景の色を黒色でクリア

        '壁の描画
        Dim brush As New SolidBrush(Color.FromArgb(255, 200, 200, 200))     'Brushオブジェクトの作成
        Dim wall As Rectangle
        '上壁を描画
        With wall
            .X = 0
            .Y = 0
            .Width = mWindowWidth
            .Height = thickness
        End With
        mRenderer.FillRectangle(brush, wall)
        '下壁を描画
        With wall
            .Y = mWindowHeight - thickness
        End With
        mRenderer.FillRectangle(brush, wall)
        '右壁を描画
        With wall
            .X = mWindowWidth - thickness
            .Y = 0
            .Width = thickness
            .Height = mWindowHeight
        End With
        mRenderer.FillRectangle(brush, wall)

        If (scene = 0 Or scene = 1) Then      'ゲーム実行中またはゲームポーズ中
            'パドルを描画
            brush.Color = Color.FromArgb(255, 255, 255, 255)
            mRenderer.DrawImage(paddleImage, CInt(mPaddlePos.x - thickness / 2), CInt(mPaddlePos.y - paddleH / 2))       'スプライトをmRendererに出力
            'Dim paddle As Rectangle
            'With paddle
            '    .X = CInt(mPaddlePos(0) - thickness / 2)
            '    .Y = CInt(mPaddlePos(1) - mPaddleH / 2)
            '    .Width = thickness
            '    .Height = mPaddleH
            'End With
            'mRenderer.FillRectangle(brush, paddle)
            'ボールの描画
            Dim ball As Rectangle
            With ball
                .X = CInt(mBallPos.x - thickness / 2)
                .Y = CInt(mBallPos.y - thickness / 2)
                .Width = thickness
                .Height = thickness
            End With
            mRenderer.FillEllipse(brush, ball)

            brush.Dispose()

            'テキスト表示
            mRenderer.DrawString(mText(0), mTextStyle(0), Brushes.White, mTextPos(0).x, mTextPos(0).y)

        Else
            'ゲームオーバー中
            For i As Integer = 1 To 2
                'テキスト表示
                mRenderer.DrawString(mText(i), mTextStyle(i), Brushes.White, mTextPos(i).x, mTextPos(i).y)
            Next
        End If

        PictureBox.Image = mWindow
    End Sub

    Private Sub Shutdown()
        mRenderer = Nothing
        mWindow = Nothing
        Me.Close()
    End Sub

    Private Sub LoadData()
        'パドルのスプライト用画像を読み込み
        paddleImage = Image.FromFile(Application.StartupPath & "\Assets\paddle.png")

        'テキスト表示を用意
        'ポーズのテキスト
        mText.Add("Press S to Pause")       'mText[0]がポーズのテキスト
        Dim tstyle As New Font("Yu Gothic UI", CInt(mWindowWidth * 0.06))
        mTextStyle.Add(tstyle)
        Dim pos As Vector2
        pos.x = CInt(mWindowWidth * 0.15)
        pos.y = CInt(mWindowHeight * 0.33)
        mTextPos.Add(pos)
        'ゲームオーバーのテキスト
        mText.Add("Game Over")       'mText[1]がゲームオーバーのテキスト
        tstyle = New Font("Yu Gothic UI", CInt(mWindowWidth * 0.1))
        mTextStyle.Add(tstyle)
        pos.x = CInt(mWindowWidth * 0.15)
        pos.y = CInt(mWindowHeight * 0.33)
        mTextPos.Add(pos)
        'リスタートのテキスト
        mText.Add("Press R to restart")       'mText[1]がゲームオーバーのテキスト
        tstyle = New Font("Yu Gothic UI", CInt(mWindowWidth * 0.06))
        mTextStyle.Add(tstyle)
        pos.x = CInt(mWindowWidth * 0.18)
        pos.y = CInt(mWindowHeight * 0.56)
        mTextPos.Add(pos)

    End Sub

    Private Sub InitGame()
        'パドルとボール位置・速さ・方向をリセット
        mPaddlePos.x = thickness * 2
        mPaddlePos.y = thickness * 0.5
        mPaddlePos.x = thickness * 2
        mPaddlePos.y = mWindowHeight * 0.5
        mPaddleDir = 0
        mPaddleSpeed = 200.0
        mBallPos.x = mWindowWidth * 0.5
        mBallPos.y = mWindowHeight * 0.5
        Dim random As New Random()
        Dim angle As Integer = random.Next(15, 75)
        Dim pmx As Integer = 2 * random.Next(0, 2) - 1
        Dim pmy As Integer = 2 * random.Next(0, 2) - 1
        mBallVel.x = pmx * 300 * Math.Cos(angle / 180 * Math.PI)
        mBallVel.y = pmy * 300 * Math.Sin(angle / 180 * Math.PI)

        scene = 0
        pause(0) = False
        pause(1) = False
    End Sub

    Private Sub Game_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        'ポーズ機能
        If e.KeyCode = Keys.S Then
            If (pause(0) = False) And (pause(1) = False) Then
                pause(0) = True
                scene = 1
            End If
            If (pause(0) = True) And (pause(1) = True) Then
                pause(0) = False
            End If
        End If

    End Sub
    Private Sub Game_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyUp
        'ポーズ機能
        If e.KeyCode = Keys.S Then
            If (pause(0) = True) And (pause(1) = False) Then
                pause(1) = True
            End If
            If (pause(0) = False) And (pause(1) = True) Then
                pause(1) = False
                scene = 0
            End If
        End If
    End Sub




End Class
