using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using Input;
using LayeredImageGfx;

namespace MagicChessPuzzles
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public const float ATTACK_ANIM_DURATION = 0.1f;
        public const float RECOVER_ANIM_DURATION = 0.2f;
        public const float WALK_ANIM_DURATION = 0.1f;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public static SpriteFont font;
        public static Texture2D gridCursor;
        public static Texture2D highlightSquare;
        public static Texture2D gameOverTexture;
        public static Texture2D gameWonTexture;
        public static Texture2D upgradeTexture;
        public static InputState inputState = new InputState();
        public static LayeredImage tooltipBG;
        public static LayeredImage activeCardBG;
        public static Dictionary<string, List<Card>> spellBooks = new Dictionary<string, List<Card>>();
        public Dictionary<Card, int> spellUpgrades = new Dictionary<Card,int>();
        public static CardCatalog[] catalogs;
        List<GameState> gameStates;
        List<LevelScript> levelScripts;
        GameState nextGameState;
        GameState gameStateOnSkip;
        int currentLevelIdx;
        MinionAnimationSequence animation = new MinionAnimationSequence();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

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

            font = Content.Load<SpriteFont>("Arial");
            gridCursor = Content.Load<Texture2D>("cursor");
            highlightSquare = Content.Load<Texture2D>("highlightSquare");
            gameOverTexture = Content.Load<Texture2D>("gameover");
            gameWonTexture = Content.Load<Texture2D>("gamewon");
            upgradeTexture = Content.Load<Texture2D>("upgrade");

            StreamReader configReader = new StreamReader(File.OpenRead("Content/config.json"));
            JSONTable config = JSONTable.parse(configReader.ReadToEnd());

            tooltipBG = new LayeredImage(config.getJSON("tooltipBG"), Content);
            activeCardBG = new LayeredImage(config.getJSON("activeCardBG"), Content);
            
            ResourceType.load(config.getJSON("resources"), Content);
            MinionType.load(config.getJSON("minions"), Content);
            LevelType.load(config.getJSON("levelTypes"), Content);

            spellBooks = new Dictionary<string, List<Card>>();
            JSONTable spellsTemplate = config.getJSON("spells");
            foreach (string key in spellsTemplate.Keys)
            {
                spellBooks.Add(key, Card.load(spellsTemplate.getArray(key), Content));
            }

            catalogs = new CardCatalog[]
            {
                new CardCatalog(new Rectangle(0, 5, 128, 600), spellBooks["main"], spellUpgrades),
                new CardCatalog(new Rectangle(800-128, 5, 128, 600), null, spellUpgrades)
            };

            levelScripts = LevelScript.load(config.getArray("levels"));

            StartLevel(0);
        }

        public void StartLevel(int levelIdx)
        {
            LevelScript levelScript = levelScripts[levelIdx];
            gameStates = new List<GameState>();
            GameState newState = new GameState(levelScript);
            gameStateOnSkip = newState.GetGameStateOnSkip();
            gameStates.Add(newState);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
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
            inputState.Update();

            // ignore all input while the animation is in progress
            // (TODO: skip button)
            if (!animation.finished)
            {
                animation.Update(gameTime);
                if (animation.finished)
                {
                    gameStates.Add(nextGameState);
                }
                else
                {
                    return;
                }
            }

            if (gameStates.Last().gameEndState == GameState.GameEndState.GameWon)
            {
                if (inputState.WasMouseLeftJustPressed())
                {
                    currentLevelIdx++;
                    StartLevel(currentLevelIdx);
                }
            }
            else
            {
                foreach (CardCatalog catalog in catalogs)
                {
                    catalog.Update(gameStates.Last());

                    if (catalog.playing != null)
                    {
                        if (catalog.playTargetCard != null)
                        {
                            PlayCard(catalog.playing, catalog.caster, TriggerItem.create(catalog.playTargetCard));
                        }
                        else
                        {
                            PlayCard(catalog.playing, catalog.caster, gameStates.Last().getItemAt(catalog.playPosition));
                        }
                    }
                }
            }

            base.Update(gameTime);
        }

        public void PlayCard(Card c, Point caster, TriggerItem target)
        {
            if (c.effect != null && c.effect is Effect_Rewind)
            {
                if (gameStates.Count <= 1)
                    return;
                // rewind isn't really a spell like everything else
                gameStates.RemoveAt(gameStates.Count - 1);
                gameStateOnSkip = gameStates.Last().GetGameStateOnSkip();
            }
            else
            {
                GameState oldGameState = gameStates.Last();
                if (oldGameState.CanPlayCard(c, caster, target))
                {
                    nextGameState = new GameState(gameStates.Last());

                    animation.Clear();
                    nextGameState.PlayCard(c, caster, nextGameState.Adapt(target), animation);
                    gameStateOnSkip = nextGameState.GetGameStateOnSkip();
                }
            }
        }

        public bool TryPayCost(List<ResourceAmount> cost)
        {
            return true;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            GameState gameState = gameStates.Last();

            if( animation.finished )
                gameState.Draw(spriteBatch, gameStateOnSkip, MinionAnimationBatch.Empty);
            else
                animation.Draw(spriteBatch, gameStateOnSkip);

            foreach (CardCatalog catalog in catalogs)
            {
                catalog.Draw(spriteBatch, gameStates.Last());
            }
            gameState.DrawMouseOver(spriteBatch, inputState.MousePos);

            switch(gameState.gameEndState)
            {
                case GameState.GameEndState.GameWon:
                case GameState.GameEndState.GameOver:
                    spriteBatch.Draw
                    (
                    gameState.gameEndState == GameState.GameEndState.GameOver? gameOverTexture: gameWonTexture,
                        new Vector2(
                            (graphics.GraphicsDevice.Viewport.Width - gameOverTexture.Width)*0.5f,
                            (graphics.GraphicsDevice.Viewport.Height - gameOverTexture.Height)*0.5f
                        ),
                        Color.White
                    );
                    break;
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
