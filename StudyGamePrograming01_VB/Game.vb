Imports System.Security.Cryptography

Structure Vector2
    Dim x As Single
    Dim y As Single
End Structure

Public Class Game
    'Public
    Public Sub New()    'コンストラクタ
        InitializeComponent()       ' この呼び出しはデザイナーで必要です。
        mWindow = Nothing
        mRenderer = Nothing
        mIsRunning = True
        mTicksCount = 0
        stopwatch.Start()
    End Sub

    Public Function Initialize() As Boolean
        'フォームを初期化
        Me.SetBounds(100, 100, mWindowW + 26, mWindowH + 49)
        Me.DoubleBuffered = Enabled
        PictureBox.SetBounds(5, 5, mWindowW, mWindowH)
        PictureBox.BackColor = Color.DarkGray
        'mWindowを初期化
        mWindow = New Bitmap(mWindowW, mWindowH)
        mRenderer = Graphics.FromImage(mWindow)
        'パドルのスプライト用画像を読み込み
        paddleImage = Image.FromFile(Application.StartupPath & "\Assets\paddle.png")

        'テキスト表示を用意
        'ポーズのテキスト
        mText.Add("Press S to Pause")       'mText[0]がポーズのテキスト
        Dim tstyle As New Font("Yu Gothic UI", CInt(mWindowW * 0.06))
        mTextStyle.Add(tstyle)
        Dim pos As Vector2
        pos.x = CInt(mWindowW * 0.15)
        pos.y = CInt(mWindowH * 0.33)
        mTextPos.Add(pos)
        'ゲームオーバーのテキスト
        mText.Add("Game Over")       'mText[1]がゲームオーバーのテキスト
        tstyle = New Font("Yu Gothic UI", CInt(mWindowW * 0.1))
        mTextStyle.Add(tstyle)
        pos.x = CInt(mWindowW * 0.15)
        pos.y = CInt(mWindowH * 0.33)
        mTextPos.Add(pos)
        'リスタートのテキスト
        mText.Add("Press R to restart")       'mText[1]がゲームオーバーのテキスト
        tstyle = New Font("Yu Gothic UI", CInt(mWindowW * 0.06))
        mTextStyle.Add(tstyle)
        pos.x = CInt(mWindowW * 0.18)
        pos.y = CInt(mWindowH * 0.56)
        mTextPos.Add(pos)

        ResetGame()

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
        mRenderer = Nothing
        mWindow = Nothing
        Me.Close()
    End Sub

    'Private
    Private Sub ProcessInput()

    End Sub

    Private Sub UpdateGame()
        '16ms経過までは待つ（フレーム制限）。約60fps
        Do While (stopwatch.ElapsedMilliseconds - mTicksCount) < 16
        Loop
        Dim deltaTime = (stopwatch.ElapsedMilliseconds - mTicksCount) / 1000    'deltaTime計算。単位は秒にする。
        If (deltaTime > 0.05) Then deltaTime = 0.05     '更新が遅すぎても最低のfpsを確保。50ms (20Fps)
        mTicksCount = stopwatch.ElapsedTicks        '次のフレームのためtick countsを更新

        If (scene = 0) Then
            'パドル位置の更新
            mPaddlePos.y += mPaddleDir * mPaddleSpeed * deltaTime
            If (mPaddlePos.y < (paddleH / 2.0 + thickness)) Then
                mPaddlePos.y = paddleH / 2.0 + thickness
            End If
            If (mPaddlePos.y > (mWindowH - paddleH / 2.0 - thickness)) Then
                mPaddlePos.y = mWindowH - paddleH / 2.0 - thickness
            End If

            '更新後のボール位置を計算
            Dim mBallPosPost As Vector2
            mBallPosPost.x = mBallPos.x + mBallVel.x * deltaTime
            mBallPosPost.y = mBallPos.y + mBallVel.y * deltaTime
            'ボールが壁に当たったら跳ね返る
            If (mBallPosPost.x + thickness * 0.5 >= (mWindowW - thickness) And mBallVel.x > 0.0) Then
                mBallVel.x *= -1.0
                mBallPosPost.x = mWindowW - thickness - thickness * 0.5
            End If
            If (mBallPosPost.y - thickness * 0.5 <= thickness And mBallVel.y < 0.0) Then
                mBallVel.y *= -1.0
                mBallPosPost.y = thickness + thickness * 0.5
            End If
            If (mBallPosPost.y + thickness * 0.5 >= mWindowH - thickness And mBallVel.y > 0.0) Then
                mBallVel.y *= -1.0
                mBallPosPost.y = mWindowH - thickness - thickness * 0.5
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
        mRenderer.Clear(Color.DarkGray)    '背景の色を灰色でクリア

        '壁の描画
        Dim brush As New SolidBrush(Color.FromArgb(255, 200, 200, 200))     'Brushオブジェクトの作成
        Dim wall As Rectangle
        '上壁を描画
        With wall
            .X = 0
            .Y = 0
            .Width = mWindowW
            .Height = thickness
        End With
        mRenderer.FillRectangle(brush, wall)
        '下壁を描画
        With wall
            .Y = mWindowH - thickness
        End With
        mRenderer.FillRectangle(brush, wall)
        '右壁を描画
        With wall
            .X = mWindowW - thickness
            .Y = 0
            .Width = thickness
            .Height = mWindowH
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

    Private Sub ResetGame()
        'パドルとボール位置・速さ・方向をリセット
        mPaddlePos.x = thickness * 2
        mPaddlePos.y = thickness * 0.5
        mPaddlePos.x = thickness * 2
        mPaddlePos.y = mWindowH * 0.5
        mPaddleDir = 0
        mPaddleSpeed = 200.0
        mBallPos.x = mWindowW * 0.5
        mBallPos.y = mWindowH * 0.5
        Dim random As New Random()
        Dim angle As Integer = random.Next(15, 75)
        Dim pmx As Integer = 2 * random.Next(0, 2) - 1
        Dim pmy As Integer = 2 * random.Next(0, 2) - 1
        mBallVel.x = pmx * mWindowH * 0.4 * Math.Cos(angle / 180 * Math.PI)
        mBallVel.y = pmy * mWindowH * 0.4 * Math.Sin(angle / 180 * Math.PI)

        scene = 0
    End Sub

    Private Sub Game_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Q, Keys.Escape
                mIsRunning = False
        End Select
    End Sub

    Private mWindow As Bitmap           'PictureBoxと同じサイズ
    Private mRenderer As Graphics       '2D描画用レンダラ
    Private stopwatch As New System.Diagnostics.Stopwatch()   'ゲーム開始時からの経過時間
    Private mTicksCount As Integer      'ゲーム開始時からの経過時間
    Private mIsRunning As Boolean       'ゲーム実行中か否か
    Private mKeyInputs As New List(Of System.Windows.Forms.KeyEventArgs)    'キー入力の配列
    Private mWindowW As Integer = 1024      'ウィンドウの横幅
    Private mWindowH As Integer = 768       'ウィンドウの縦幅

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
    Private pause As Boolean                    'true:ポーズ中
    Private Const mFontSize As Integer = 100    'テキストのフォントサイズ
    Private mText As New List(Of String)        'テキスト
    Private mTextPos As New List(Of Vector2)    'テキスト表示位置
    Private mTextStyle As New List(Of Font)     'テキストスタイル


End Class
