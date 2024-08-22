using Raylib_cs;
using System.Collections.Generic;
using System;
using System.Numerics;

class Program
{
    enum GameState
    {
        MainMenu,
        InGame,
        GameOver
    }

    static GameState gameState = GameState.MainMenu;

    public static void Main()
    {
        Raylib.InitWindow(800, 480, "Space Shooter");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        Music music = Raylib.LoadMusicStream("music.ogg");
        Raylib.SetMusicVolume(music, 0.2f);
        Raylib.PlayMusicStream(music);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateMusicStream(music);
            switch (gameState)
            {
                case GameState.MainMenu:
                    DrawMainMenu();
                    UpdateMainMenu();
                    break;
                case GameState.InGame:
                    PlayGame();
                    break;
                case GameState.GameOver:
                    DrawGameOver();
                    UpdateGameOver();
                    break;
            }
        }
        Raylib.UnloadMusicStream(music);
        Raylib.CloseWindow();
    }

    private static void DrawMainMenu()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);


       
        Raylib.DrawText("Spiel starten", 320, 200, 30, Color.WHITE);

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
        {
            gameState = GameState.InGame;
        }

        Raylib.EndDrawing();
    }

    private static void UpdateMainMenu()
    {
        // Handle input or any other logic specific to the main menu
    }

    private static void PlayGame()
    {
        Game game = new Game();

        while (gameState == GameState.InGame && !Raylib.WindowShouldClose())
        {
            game.Update();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            game.Draw();
            Raylib.EndDrawing();
        }

        game.Unsubscribe();
        gameState = GameState.GameOver;
    }

    private static void DrawGameOver()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);

        
        Raylib.DrawText("Spiel vorbei", 320, 200, 30, Color.WHITE);

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
        {
            gameState = GameState.MainMenu;
        }

        Raylib.EndDrawing();
    }

    private static void UpdateGameOver()
    {
        
    }

    public class Game
    {
        int anzahlGegner = 0;
        List<Bullet> bullets = new List<Bullet>(32);
        List<Enemy> enemies = new List<Enemy>();
        Player player;
        float enemyTimer;
        int collisionCounter = 0;
        private int currentLevel = 1;
        private bool levelIncreased = false;
        bool isFasterShootingActive = false;

        public Game()
        {
            player = new Player(bullets);
            player.BulletSpawned += OnBulletSpawned;
            Bullet.LeftScreen += OnLeftScreen;
        }

        public void Unsubscribe()
        {
            Bullet.LeftScreen -= OnLeftScreen;
            player.BulletSpawned -= OnBulletSpawned;
        }

        public void OnLeftScreen(Bullet bullet)
        {
            bullets.Remove(bullet);
        }

        public void OnBulletSpawned(float x, float y, Vector2 dir)
        {
            Bullet b = new Bullet(x, y, dir, isFasterShootingActive);
            bullets.Add(b);
        }

        public void Update()
        {
            enemyTimer += Raylib.GetFrameTime();
            if (enemyTimer >= (1.5f - currentLevel*0.1f))
            {
                enemyTimer = 0f;
                int randX = Raylib.GetRandomValue(0, 800);
                Enemy enemy = new Enemy(randX, -50, currentLevel);
                enemies.Add(enemy);
            }

            CheckCollisions();

            player.Update();
            for (int i = 0; i < bullets.Count; i++)
            {
                bullets[i].Update();
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Update();
            }
        }

        public void Draw()
        {
            player.Draw();
            for (int i = 0; i < bullets.Count; i++)
            {
                bullets[i].Draw();
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Draw();
            }

            Raylib.DrawText($"Level:{currentLevel}", 370, 12, 20, Color.WHITE);

            Raylib.DrawText($"Punktzahl: {anzahlGegner}", 12, 12, 20, Color.WHITE);

            if (isFasterShootingActive)
            {
                Raylib.DrawText("Schnelleres Schießen aktiv", 12, 50, 20, Color.GREEN);
            }

        }

        private void CheckCollisions()
        {
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                for (int e = enemies.Count - 1; e >= 0; e--)
                {
                    Vector2 bPos = bullets[b].Pos;
                    Vector2 ePos = enemies[e].Pos;
                    float bRad = bullets[b].Radius;
                    float eRad = enemies[e].Radius;

                    if (Raylib.CheckCollisionCircles(bPos, bRad, ePos, eRad))
                    {
                        bullets.Remove(bullets[b]);
                        enemies.Remove(enemies[e]);
                        
                        anzahlGegner++;
                        collisionCounter++;

                        if (collisionCounter % 5 == 0 && !levelIncreased)
                        {
                            IncreaseEnemySpeed();
                            IncreaseLevel();
                            levelIncreased = true;
                        }
                        else if (collisionCounter % 10 != 0)
                        {
                            levelIncreased = false;
                        }

                        if (collisionCounter >= 15 && !isFasterShootingActive)
                        {
                            collisionCounter = 0;
                            ActivatePowerUp(PowerUpType.FasterShooting);
                        }

                        break;
                    }
                }
            }
        }

        private void IncreaseLevel()
        {
            currentLevel++;
        }

        private void ActivatePowerUp(PowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case PowerUpType.FasterShooting:
                    isFasterShootingActive = true;
                    break;
            }
        }

        private void IncreaseEnemySpeed()
        {
            foreach (var enemy in enemies)
            {
                enemy.IncreaseSpeed();
            }
        }
    }

    public class Player
    {
        List<Bullet> bullets = new List<Bullet>();
        int sizeX = 15;
        int sizeY = 30;
        float posX = 400f;
        float posY = 240f;
        float angle;
        Vector2 startDir = new Vector2(0f, -1f);
        Vector2 dir = new Vector2(0f, -1f);
        float speed;

        public event Action<float, float, Vector2>? BulletSpawned;

        public Player(List<Bullet> bullets)
        {
            this.bullets = bullets;
        }

        public void Update()
        {
            if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
            {
                posX += dir.X;
                posY += dir.Y;
                if (posY < 0) posY = 0;
            }

            if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            {
                speed = -3f;
                angle -= 3f;
                dir = Raymath.Vector2Rotate(startDir, Raylib.DEG2RAD * angle);
            }
            else if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            {
                angle += 3f;
                dir = Raymath.Vector2Rotate(startDir, Raylib.DEG2RAD * angle);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
            {
                BulletSpawned?.Invoke(posX, posY, dir);
                
            }
        }

        public void Draw()
        {
            Rectangle rect = new Rectangle(posX, posY, sizeX, sizeY);
            Vector2 origin = new Vector2(sizeX / 2, sizeY / 2);
            Raylib.DrawRectanglePro(rect, origin, angle, Color.BLUE);
            Raylib.DrawCircle((int)posX, (int)posY, 3f, Color.RED);
        }
    }

    public class Bullet
    {
        public Vector2 Pos => new Vector2(posX, posY);
        float posX;
        float posY;
        Vector2 dir;
        float speed;
        float baseSpeed = 5f;

        public float Radius => radius;
        float radius = 5f;

        public bool isAlive = true;

        public static event Action<Bullet>? LeftScreen;

        public Bullet(float x, float y, Vector2 dir, bool isFasterShootingActive)
        {
            posX = x;
            posY = y;
            this.dir = dir;


            if (isFasterShootingActive)
            {
                speed = baseSpeed * 1.5f;
            }
            else
            {
                speed = baseSpeed;
            }
        }

        public void Update()
        {
            posX += dir.X * speed;
            posY += dir.Y * speed;

            if (posY < 0 || posY > 480 || posX < 0 || posX > 800)
            {
                LeftScreen?.Invoke(this);
            }
        }

        public void Draw()
        {
            Raylib.DrawCircle((int)posX, (int)posY, radius, Color.RED);
        }
    }

    public class Enemy
    {
        public float Radius => sizeX / 2;
        int sizeX = 20;
        int sizeY = 20;
        float speed;
        int posX;
        int posY;

        public Vector2 Pos => new Vector2(posX, posY);

        public Enemy(int x, int y, int level)
        {
            posX = x;
            posY = y;
            SetSpeed(level);
        }

        public void Update()
        {
            posY += (int)speed;
        }

        public void Draw()
        {
            Raylib.DrawRectangle(posX - sizeX / 2, posY - sizeY / 2, sizeX, sizeY, Color.ORANGE);
        }

        public void IncreaseSpeed()
        {
            speed *= 1.07f;
        }

        private void SetSpeed(int level)
        {
            speed = 1f + (level - 1) * 0.2f;
        }
    }

    public enum PowerUpType
    {
        FasterShooting,
    }
}
