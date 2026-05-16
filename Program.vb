Imports VbPixelGameEngine
Imports VbPixelGameEngine.PixelType
Imports VbPixelGameEngine.Color

Public NotInheritable Class Asteroids
    Inherits PixelGameEngine

    Private Enum GameState
        Title = 0
        Playing = 1
        Paused = 2
        GameOver = 3
    End Enum

    Private ReadOnly m_asteroids As New List(Of SpaceObject)
    Private ReadOnly m_bullets As New List(Of SpaceObject)
    Private m_player As SpaceObject
    Private m_score As Integer = 0
    Private m_modelShip As List(Of (Single, Single))
    Private m_modelAstroid As List(Of (Single, Single))
    Private m_gameState As GameState = GameState.Title
    Private m_lives As Integer = 3

    <DisposeField> Private sndPlayerShooting As SoundPlayer
    <DisposeField> Private sndAsteroidShot As SoundPlayer
    <DisposeField> Private sndLifeLost As SoundPlayer
    <DisposeField> Private sndMainTheme As SoundPlayer

    Protected Overrides Function OnUserCreate() As Boolean

        m_modelShip = New List(Of (Single, Single)) From {(0.0F, -5.0F),
                                                          (-2.5F, +2.5F),
                                                          (+2.5F, +2.5F)} ' A simple Isoceles Triangle

        m_modelAstroid = New List(Of (Single, Single))
        Dim verts = 20
        For i = 0 To verts - 1
            Dim radius = CSng((Rand / RAND_MAX * 0.4) + 0.8)
            Dim a = i / verts * 6.28318
            m_modelAstroid.Add((CSng(radius * Math.Sin(a)), CSng(radius * Math.Cos(a))))
        Next

        sndPlayerShooting = New SoundPlayer("Assets/player_shooting.wav")
        sndAsteroidShot = New SoundPlayer("Assets/asteroid_shot.wav")
        sndLifeLost = New SoundPlayer("Assets/life_lost.wav")
        sndMainTheme = New SoundPlayer("Assets/main_theme.mp3")
        sndMainTheme.PlayLooping()

        Return True
    End Function

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        Clear()

        Select Case m_gameState
            Case GameState.Title
                DrawString(ScreenWidth() \ 2 - 100, ScreenHeight() \ 2 - 20, "* Asteroids *", Presets.Yellow, 2)
                DrawString(ScreenWidth() \ 2 - 80, ScreenHeight() \ 2 + 20, "Press SPACE to begin", Presets.Beige)
                If GetKey(Key.SPACE).Released Then
                    ResetGame()
                    m_gameState = GameState.Playing
                End If
                GoTo CheckExit

            Case GameState.Paused
                DrawString(ScreenWidth() \ 2 - 100, ScreenHeight() \ 2 - 20, "GAME PAUSED", Presets.White, 2)
                DrawString(50, ScreenHeight() \ 2 + 20, "Press 'P' to resume, or ESC to quit")
                If GetKey(Key.P).Released Then m_gameState = GameState.Playing
                GoTo CheckExit

            Case GameState.GameOver
                DrawString(ScreenWidth() \ 2 - 100, ScreenHeight() \ 2 - 35, "- GAME OVER -", Presets.White, 2)
                DrawString(ScreenWidth() \ 2 - 80, ScreenHeight() \ 2 - 5, $"Final Score: {m_score}")
                DrawString(ScreenWidth() \ 2 - 120, ScreenHeight() \ 2 + 20, "* Press SPACE for title screen")
                DrawString(ScreenWidth() \ 2 - 120, ScreenHeight() \ 2 + 40, "* Press ESC to exit the game")
                If GetKey(Key.SPACE).Released Then m_gameState = GameState.Title
                GoTo CheckExit
        End Select

        ' Steer
        If GetKey(Key.LEFT).Held Then m_player.Angle -= 5.0F * elapsedTime
        If GetKey(Key.RIGHT).Held Then m_player.Angle += 5.0F * elapsedTime

        ' Thrust
        If GetKey(Key.UP).Held Then
            m_player.Dx += CSng(Math.Sin(m_player.Angle) * 20 * elapsedTime)
            m_player.Dy -= CSng(Math.Cos(m_player.Angle) * 20 * elapsedTime)
        End If

        m_player.X += m_player.Dx * elapsedTime
        m_player.Y += m_player.Dy * elapsedTime

        WrapCoordinates(m_player.X, m_player.Y, m_player.X, m_player.Y)

        ' Check ship collision with asteroids
        For Each ast In m_asteroids
            If IsPointInsideCircle(ast.X, ast.Y, ast.Size, m_player.X, m_player.Y) Then
                m_lives -= 1
                sndLifeLost.Play()
                If m_lives <= 0 Then
                    m_gameState = GameState.GameOver
                Else
                    m_player = New SpaceObject(ScreenWidth \ 2, ScreenHeight \ 2)
                End If
                Exit For
            End If
        Next

        If GetKey(Key.P).Released Then m_gameState = GameState.Paused

        ' Fire bullet, and accelerate bullets when player is moving faster
        If GetKey(Key.SPACE).Released Then
            m_bullets.Add(New SpaceObject(
                m_player.X, m_player.Y,
                50.0F * CSng(Math.Sin(m_player.Angle)) + m_player.Dx,
                -50.0F * CSng(Math.Cos(m_player.Angle)) + m_player.Dy
            ))
            sndPlayerShooting.Play()
        End If

        ' Update and draw asteroids
        For Each ast In m_asteroids
            ast.X += ast.Dx * elapsedTime
            ast.Y += ast.Dy * elapsedTime
            ast.Angle += 0.5F * elapsedTime
            WrapCoordinates(ast.X, ast.Y, ast.X, ast.Y)
            DrawWireFrameModel(m_modelAstroid, ast.X, ast.Y, ast.Angle, ast.Size, FgYellow)
        Next

        Dim newAstroids As New List(Of SpaceObject)

        ' Update and draw bullets
        For Each bullet In m_bullets
            bullet.X += bullet.Dx * elapsedTime
            bullet.Y += bullet.Dy * elapsedTime
            WrapCoordinates(bullet.X, bullet.Y, bullet.X, bullet.Y)
            Draw(bullet.X, bullet.Y)

            For Each ast In m_asteroids
                If IsPointInsideCircle(ast.X, ast.Y, ast.Size, bullet.X, bullet.Y) Then
                    bullet.X = -100
                    sndAsteroidShot.Play()
                    If ast.Size > 4 Then
                        Dim angle1 = Rand / RAND_MAX * 6.283185
                        Dim angle2 = Rand / RAND_MAX * 6.283185
                        newAstroids.Add(New SpaceObject(ast.X, ast.Y,
                            15 * CSng(Math.Sin(angle1)),
                            15 * CSng(Math.Cos(angle1)),
                            ast.Size >> 1, 0.0))
                        newAstroids.Add(New SpaceObject(ast.X, ast.Y,
                            15 * CSng(Math.Sin(angle2)),
                            15 * CSng(Math.Cos(angle2)),
                            ast.Size >> 1, 0.0))
                    End If
                    ast.X = -100
                    m_score += 50
                End If
            Next ast
        Next bullet

        ' Append new astroids to existing vector
        newAstroids.ForEach(Sub(ast) m_asteroids.Add(ast))

        ' Remove bullets that have gone off screen
        If m_bullets.Count <> 0 Then
            m_bullets.RemoveAll(Function(o) o.X < 1 OrElse o.Y < 1 OrElse
                                    o.X >= ScreenWidth() - 1 OrElse o.Y >= ScreenHeight() - 1)
        End If

        If m_asteroids.Count <> 0 Then
            m_asteroids.RemoveAll(Function(o) o.X < 0)
        Else
            m_score += 200 + Random.Shared.Next(3) * 100

            ' add them 90 degress left and right to the player, their coordinates
            ' be wrapped by the next astroid update
            m_asteroids.Add(New SpaceObject(30 * CSng(Math.Sin(m_player.Angle - 3.14159 / 2)),
                            30 * CSng(Math.Cos(m_player.Angle - 3.14159 / 2)),
                            10 * CSng(Math.Sin(m_player.Angle)),
                            10 * CSng(Math.Cos(m_player.Angle)),
                            16, 0))
            m_asteroids.Add(New SpaceObject(30 * CSng(Math.Sin(m_player.Angle + 3.14159 / 2)),
                            30 * CSng(Math.Cos(m_player.Angle + 3.14159 / 2)),
                            10 * CSng(Math.Sin(-m_player.Angle)),
                            10 * CSng(Math.Cos(-m_player.Angle)),
                            16, 0))
            m_asteroids.Add(New SpaceObject(30 * CSng(Math.Sin(m_player.Angle + 3.14159 / 2)),
                            30 * CSng(Math.Cos(m_player.Angle + 3.14159 / 2)),
                            10 * CSng(Math.Sin(m_player.Angle)),
                            10 * CSng(Math.Cos(-m_player.Angle)),
                            16, 0))
        End If
        DrawWireFrameModel(m_modelShip, m_player.X, m_player.Y, m_player.Angle)

        ' Draw score and lives
        DrawString(2, 2, $"Score: {m_score}")
        DrawString(ScreenWidth() - 100, 2, $"Lives: {m_lives}")

CheckExit:
        Return Not GetKey(Key.ESCAPE).Pressed
    End Function

    Private Sub ResetGame()
        m_asteroids.Clear()
        m_bullets.Clear()
        m_asteroids.Add(New SpaceObject(20, 20, 8, -6, 16, 0.0))
        m_asteroids.Add(New SpaceObject(100, 20, -5, 3, 16, 0.0))
        m_asteroids.Add(New SpaceObject(ScreenWidth - 50, ScreenHeight - 50, -3, -4, 16, 0.0))
        m_player = New SpaceObject(ScreenWidth \ 2, ScreenHeight \ 2)
        m_lives = 3
        m_score = 0
    End Sub

    Private Shared Function IsPointInsideCircle(cx As Double, cy As Double, radius As Double, x As Double, y As Double) As Boolean
        Return Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) < radius
    End Function

    Private Sub WrapCoordinates(ix As Single, iy As Single, ByRef ox As Single, ByRef oy As Single)
        ox = ix
        oy = iy
        If ix < 0.0 Then ox = ix + ScreenWidth()
        If ix >= ScreenWidth() Then ox = ix - ScreenWidth()
        If iy < 0.0 Then oy = iy + ScreenHeight()
        If iy >= ScreenHeight() Then oy = iy - ScreenHeight()
    End Sub

    Private Overloads Function Draw(x As Single, y As Single) As Boolean
        Return Draw(CInt(Fix(x)), CInt(Fix(y)))
    End Function

    Protected Overrides Function Draw(x As Integer, y As Integer) As Boolean
        Dim fx, fy As Single
        WrapCoordinates(x, y, fx, fy)
        x = CInt(fx) : y = CInt(fy)
        Return MyBase.Draw(x, y)
    End Function

    Private Sub DrawWireFrameModel(vecModelCoordinates As List(Of (X As Single, Y As Single)), x As Single, y As Single, Optional r As Single = 0.0F, Optional s As Single = 1.0F, Optional col As Color = FgWhite)
        ' Create translated model vector of coordinate pairs
        Dim transformedCoordinates As New List(Of (X As Single, Y As Single))
        Dim verts = vecModelCoordinates.Count
        For Each entry In vecModelCoordinates
            transformedCoordinates.Add((entry.X, entry.Y))
        Next

        ' Rotate
        For i = 0 To verts - 1
            transformedCoordinates(i) = (vecModelCoordinates(i).X * CSng(Math.Cos(r)) - vecModelCoordinates(i).Y * CSng(Math.Sin(r)),
                                               vecModelCoordinates(i).X * CSng(Math.Sin(r)) + vecModelCoordinates(i).Y * CSng(Math.Cos(r)))
        Next

        ' Scale
        For i = 0 To verts - 1
            transformedCoordinates(i) = (transformedCoordinates(i).X * s,
                                               transformedCoordinates(i).Y * s)
        Next

        ' Translate
        For i = 0 To verts - 1
            transformedCoordinates(i) = (transformedCoordinates(i).X + x,
                                               transformedCoordinates(i).Y + y)
        Next

        ' Draw Closed Polygon
        For i = 0 To verts - 1
            Dim j = (i + 1) Mod verts
            DrawLine(transformedCoordinates(i).X,
                     transformedCoordinates(i).Y,
                     transformedCoordinates(j).X,
                     transformedCoordinates(j).Y,
                     Solid,
                     col)
        Next

    End Sub

    Friend Shared Sub Main()
        Using game As New Asteroids
            If game.Construct(400, 300, fullScreen:=True) Then game.Start()
        End Using
    End Sub

End Class