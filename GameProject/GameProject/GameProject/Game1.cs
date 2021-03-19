using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameState gameState = GameState.PLAY;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.ScorePrefix + 0;

        // health support
        string healthString = GameConstants.HealthPrefix +
            GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosionSound;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            burgerDamage = Content.Load<SoundEffect>(@"audio\BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>(@"audio\BurgerDeath");
            burgerShot = Content.Load<SoundEffect>(@"audio\BurgerShot");
            explosionSound = Content.Load<SoundEffect>(@"audio\explosion");
            teddyBounce = Content.Load<SoundEffect>(@"audio\TeddyBounce");
            teddyShot = Content.Load<SoundEffect>(@"audio\TeddyShot");

            // load sprite font
            font = Content.Load<SpriteFont>(@"fonts\Arial20");

            // load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>(@"graphics\teddybearprojectile");
            explosionSpriteStrip = Content.Load<Texture2D>(@"graphics\explosion");
            frenchFriesSprite = Content.Load<Texture2D>(@"graphics\frenchfries");

            // add initial game objects
            burger = new Burger(Content, @"graphics\burger", GameConstants.WindowWidth / 2, 
                (GameConstants.WindowHeight * 7) / 8, burgerShot);
            for (int i = 0; i <= GameConstants.MaxBears; i++)
            {
                // spawn multiple bears
                SpawnBear();
            }

            // set initial health and score strings
            healthString = GameConstants.HealthPrefix + burger.Health;
            scoreString = GameConstants.ScorePrefix + score;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, keyboard);

            if (burger.Health <= 0)
            {
                gameState = GameState.END;
            }

            if (keyboard.IsKeyDown(Keys.P) && gameState != GameState.END)
            {
                // pause or unpause
                if (gameState == GameState.PLAY)
                {
                    gameState = GameState.PAUSED;
                } else
                {
                    gameState = GameState.PLAY;
                }
            }

            if (gameState == GameState.PLAY)
            {
                // update other game objects
                foreach (TeddyBear bear in bears)
                {
                    bear.Update(gameTime);
                }
                foreach (Projectile projectile in projectiles)
                {
                    projectile.Update(gameTime);
                }
                foreach (Explosion explosion in explosions)
                {
                    explosion.Update(gameTime);
                }

                // check and resolve collisions between teddy bears
                for (int i = 0; i < bears.Count; i++)
                {
                    for (int j = i + 1; j < bears.Count; j++)
                    {
                        if (bears[i].Active && bears[j].Active && bears[i].CollisionRectangle.Intersects(bears[j].CollisionRectangle))
                        {
                            teddyBounce.Play();
                            CollisionResolutionInfo bearCollsionInfo = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds,
                                GameConstants.WindowWidth, GameConstants.WindowHeight, bears[i].Velocity, bears[i].CollisionRectangle,
                                bears[j].Velocity, bears[j].CollisionRectangle);
                            if (bearCollsionInfo != null)
                            {
                                if (bearCollsionInfo.FirstOutOfBounds)
                                {
                                    bears[i].Active = false;
                                }
                                else
                                {
                                    bears[i].Velocity = bearCollsionInfo.FirstVelocity;
                                    bears[i].DrawRectangle = bearCollsionInfo.FirstDrawRectangle;
                                }
                                if (bearCollsionInfo.SecondOutOfBounds)
                                {
                                    bears[j].Active = false;
                                }
                                else
                                {
                                    bears[j].Velocity = bearCollsionInfo.SecondVelocity;
                                    bears[j].DrawRectangle = bearCollsionInfo.SecondDrawRectangle;
                                }
                            }
                        }
                    }
                }
                // check and resolve collisions between burger and teddy bears
                foreach (TeddyBear bear in bears)
                {
                    if (bear.Active && bear.CollisionRectangle.Intersects(burger.CollisionRectangle))
                    {
                        burger.Health -= GameConstants.BearDamage;
                        // check if burger is dead
                        CheckBurgerKill();
                        bear.Active = false;
                        // play sound
                        burgerDamage.Play();
                        explosions.Add(new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosionSound));
                    }
                }
                // check and resolve collisions between burger and projectiles
                foreach (Projectile projectile in projectiles)
                {
                    if (projectile.Type == ProjectileType.TeddyBear)
                    {
                        if (projectile.Active
                        && projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                        {
                            projectile.Active = false;
                            burger.Health -= GameConstants.TeddyBearProjectileDamage;
                            // check if burger is dead
                            CheckBurgerKill();

                            // update health of burger
                            healthString = GameConstants.HealthPrefix + burger.Health;
                            // play sound
                            burgerDamage.Play();
                        }
                    }
                }
                // check and resolve collisions between teddy bears and projectiles
                foreach (TeddyBear bear in bears)
                {
                    foreach (Projectile projectile in projectiles)
                    {
                        // check to see if projectile is french fries (teddy bears dont kill teddy bears)
                        if (projectile.Type == ProjectileType.FrenchFries)
                        {
                            // check for collision
                            if (bear.Active && projectile.Active && bear.CollisionRectangle.Intersects(projectile.CollisionRectangle))
                            {
                                bear.Active = false;
                                projectile.Active = false;

                                // add explosions at the center of the bear
                                Explosion explosion = new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosionSound);
                                explosions.Add(explosion);

                                // update score
                                score += GameConstants.BearPoints;
                                scoreString = GameConstants.ScorePrefix + score;

                                // play sound
                                explosionSound.Play();
                            }
                        }
                    }
                }
                // clean out inactive teddy bears and add new ones as necessary
                for (int i = bears.Count - 1; i >= 0; i--)
                {
                    if (!bears[i].Active)
                    {
                        bears.RemoveAt(i);
                    }
                }
                // add bears once one or more teddy bears are inactive
                while (bears.Count < GameConstants.MaxBears)
                {
                    SpawnBear();
                }
                // clean out inactive projectiles
                for (int i = projectiles.Count - 1; i >= 0; i--)
                {
                    if (!projectiles[i].Active)
                    {
                        projectiles.RemoveAt(i);
                    }
                }
                // clean out finished explosions
                for (int i = explosions.Count - 1; i >= 0; i--)
                {
                    if (explosions[i].Finished)
                    {
                        explosions.RemoveAt(i);
                    }
                }
            } else
            {
            }

            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (gameState == GameState.PLAY)
            {
                // draw game objects
                burger.Draw(spriteBatch);
                foreach (TeddyBear bear in bears)
                {
                    bear.Draw(spriteBatch);
                }
                foreach (Projectile projectile in projectiles)
                {
                    projectile.Draw(spriteBatch);
                }
                foreach (Explosion explosion in explosions)
                {
                    explosion.Draw(spriteBatch);
                }

                // draw score and health
                spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);
                spriteBatch.DrawString(font, scoreString, GameConstants.ScoreLocation, Color.White);
                spriteBatch.End();
            } else if (gameState == GameState.PAUSED)
            {
                string pauseMessage = "P to Unpause!";
                spriteBatch.DrawString(font, pauseMessage, new Vector2(GameConstants.WindowWidth / 2, GameConstants.WindowHeight / 2), Color.White);
                spriteBatch.End();
            } else
            {
                string loseMessage = "You Lost! Final Score: " + score;
                spriteBatch.DrawString(font, loseMessage, new Vector2(GameConstants.WindowWidth / 2, GameConstants.WindowHeight / 2), Color.White);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.FrenchFries)
            {
                return frenchFriesSprite;
            }
            else
            {
                return teddyBearProjectileSprite;
            }

        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location
            int locationX = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth - GameConstants.SpawnBorderSize);
            int locationY = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight - GameConstants.SpawnBorderSize);

            // generate random velocity
            float speed = RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange) + GameConstants.MinBearSpeed;
            float angle = RandomNumberGenerator.NextFloat(2 * (float)Math.PI);

            float velocityX = (float)Math.Cos(angle) * speed;
            float velocityY = (float)Math.Sin(angle) * speed;
            Vector2 velocity = new Vector2(velocityX, velocityY);

            // create new bear
            TeddyBear newBear = new TeddyBear(Content, @"graphics\teddybear", locationX, locationY, velocity, teddyBounce, teddyShot);

            // make sure we don't spawn into a collision
            List<Rectangle> collisionRectangles = GetCollisionRectangles();
            while (!CollisionUtils.IsCollisionFree(newBear.CollisionRectangle, collisionRectangles))
            {
                newBear.X = GetRandomLocation(0, GameConstants.WindowWidth - newBear.CollisionRectangle.Width);
                newBear.Y = GetRandomLocation(0, GameConstants.WindowHeight - newBear.CollisionRectangle.Height);
            }
            // add new bear to list
            bears.Add(newBear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health == 0)
            {
                burgerDead = true;
                burgerDeath.Play();
            }
        }

        #endregion
    }
}
