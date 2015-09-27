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
using DragonGfx;

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
        public static Texture2D levelOpenTexture;
        public static Texture2D levelDoneTexture;
        public static Texture2D levelHoverTexture;
        public static Texture2D levelStarOpenTexture;
        public static Texture2D levelStarDoneTexture;
        public static Texture2D levelStarHoverTexture;
        public static Texture2D selectionCircleTexture;
        public static InputState inputState = new InputState();
        public static LayeredImage tooltipBG;
        public static LayeredImage activeCardBG;
        internal static UIButtonStyleSet basicButtonStyle;
        public static Dictionary<string, List<Card>> spellBooks = new Dictionary<string, List<Card>>();
        public Dictionary<Card, int> spellUpgrades = new Dictionary<Card,int>();
        public static CardCatalog wizardCatalog;
        public static CardCatalog dynamicCatalog;
        public static CardCatalog[] catalogs;
        List<GameState> gameStates;
        LevelScreen levelScreen;
        GameState nextGameState;
        GameState gameStateOnSkip;
        LevelState currentLevel;
        LevelState showingWinScreen;
        MinionAnimationSequence animation = new MinionAnimationSequence();

        List<UIButton> gameScreenButtons;
        List<UIButton> winScreenButtons;
        List<UIButton> mapScreenButtons;

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
            levelDoneTexture = Content.Load<Texture2D>("level_pip_done");
            levelOpenTexture = Content.Load<Texture2D>("level_pip_open");
            levelHoverTexture = Content.Load<Texture2D>("level_pip_hover");
            levelStarDoneTexture = Content.Load<Texture2D>("level_star_done");
            levelStarOpenTexture = Content.Load<Texture2D>("level_star_open");
            levelStarHoverTexture = Content.Load<Texture2D>("level_star_hover");
            selectionCircleTexture = Content.Load<Texture2D>("selection_circle");

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

            wizardCatalog = new CardCatalog(new Rectangle(0, 5, 128, 600), spellBooks["main"], spellUpgrades);
            dynamicCatalog = new CardCatalog(new Rectangle(800 - 128, 5, 128, 600), spellBooks["rhs"], spellUpgrades);
            catalogs = new CardCatalog[]{ wizardCatalog, dynamicCatalog };

            levelScreen = new LevelScreen(config.getArray("chapters"));

            basicButtonStyle = new UIButtonStyleSet
            (
                new UIButtonStyle(font, Color.Black, new LayeredImage(config.getJSON("button3d"), Content), Color.White),
                new UIButtonStyle(font, Color.Yellow, new LayeredImage(config.getJSON("button3d_hover"), Content), Color.White),
                new UIButtonStyle(font, Color.Yellow, new LayeredImage(config.getJSON("button3d_pressed"), Content), Color.White)
            );
            gameScreenButtons = new List<UIButton>()
            {
                new UIButton("Back To Map", new Rectangle(GraphicsDevice.Viewport.Width - 240, 10, 100, 35), basicButtonStyle, OnPress_BackToMap),
                new UIButton("Cheat: Win", new Rectangle(GraphicsDevice.Viewport.Width - 240, GraphicsDevice.Viewport.Height - 100, 100, 35), basicButtonStyle, OnPress_CheatWin)
            };
            winScreenButtons = new List<UIButton>()
            {
                new UIButton("Next Level", new Rectangle(GraphicsDevice.Viewport.Width/2 - 120, GraphicsDevice.Viewport.Height/2 + 100, 100, 35), basicButtonStyle, OnPress_NextLevel),
                new UIButton("View Map", new Rectangle(GraphicsDevice.Viewport.Width/2 + 20, GraphicsDevice.Viewport.Height/2 + 100, 100, 35), basicButtonStyle, OnPress_BackToMap)
            };
            mapScreenButtons = new List<UIButton>()
            {
                new UIButton("Cheat: All spells", new Rectangle(GraphicsDevice.Viewport.Width - 160, GraphicsDevice.Viewport.Height - 100, 150, 35), basicButtonStyle, OnPress_CheatAllSpells),
                new UIButton("Cheat: All basic", new Rectangle(GraphicsDevice.Viewport.Width - 160, GraphicsDevice.Viewport.Height - 150, 150, 35), basicButtonStyle, OnPress_CheatAllBasic),
                new UIButton("Cheat: Restart", new Rectangle(GraphicsDevice.Viewport.Width - 160, GraphicsDevice.Viewport.Height - 200, 150, 35), basicButtonStyle, OnPress_CheatRestart),
            };
        }

        public void StartLevel(LevelState levelState)
        {
            currentLevel = levelState;
            showingWinScreen = null;
            if (levelState != null)
            {
                gameStates = new List<GameState>();
                GameState newState = new GameState(levelState.script);
                gameStateOnSkip = newState.GetGameStateOnSkip();
                gameStates.Add(newState);
            }
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

            if (showingWinScreen != null)
            {
                foreach (UIButton button in winScreenButtons)
                {
                    button.Update(inputState);
                }
                return;
            }
            else if (currentLevel == null)
            {
                levelScreen.Update(inputState);

                foreach (UIButton button in mapScreenButtons)
                {
                    button.Update(inputState);
                }

                if (levelScreen.selectedLevel != null)
                {
                    StartLevel(levelScreen.selectedLevel);
                }
                else
                {
                    return;
                }
            }

            foreach (UIButton button in gameScreenButtons)
            {
                button.Update(inputState);
            }

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

            if (gameStates != null)
            {
                if (gameStates.Last().gameEndState == GameState.GameEndState.GameWon)
                {
                    if (inputState.WasMouseLeftJustPressed())
                    {
                        // you won!
                        ReturnToMap(true);
                    }
                }
                else
                {
                    GameState gameState = gameStates.Last();
                    foreach (CardCatalog catalog in catalogs)
                    {
                        catalog.Update(gameState);

                        if (catalog.playing != null)
                        {
                            if (catalog.playTargetCard != null)
                            {
                                PlayTurn(catalog.playing, gameState.wizard, TriggerItem.create(catalog.playTargetCard));
                            }
                            else
                            {
                                PlayTurn(catalog.playing, gameState.wizard, gameState.getItemAt(catalog.playPosition));
                            }
                        }
                    }
                }
            }

            base.Update(gameTime);
        }

        public void OnPress_BackToMap()
        {
            ReturnToMap(false);
        }

        public void OnPress_NextLevel()
        {
            StartLevel(levelScreen.GetNextLevel(showingWinScreen));
        }

        public void OnPress_CheatWin()
        {
            ReturnToMap(true);
        }

        public void OnPress_CheatAllBasic()
        {
            levelScreen.CheatAllBasic();
        }

        public void OnPress_CheatAllSpells()
        {
            levelScreen.CheatAllSpells();
        }

        public void OnPress_CheatRestart()
        {
            levelScreen.CheatRestart();
        }

        public void ReturnToMap(bool won)
        {
            gameStates = null;
            if (won && currentLevel != null)
            {
                currentLevel.done = true;

                if (currentLevel.script.unlocksCard != null)
                    currentLevel.script.unlocksCard.unlocked = true;

                showingWinScreen = currentLevel;
            }
            else
            {
                showingWinScreen = null;
            }
            currentLevel = null;
        }

        public void PlayTurn(Card c, Minion caster, TriggerItem target)
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
                if (oldGameState.CanPlayCard(c, target))
                {
                    nextGameState = new GameState(gameStates.Last());

                    animation.Clear();
                    if (caster == null)
                        caster = nextGameState.wizard;

                    nextGameState.PlayCard(c, caster, nextGameState.Adapt(target), animation);
                    nextGameState.TurnEffects(animation);

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

            if (showingWinScreen != null)
            {
                spriteBatch.Begin();

                Card newSpell = showingWinScreen.script.unlocksCard;
                if (newSpell != null)
                {
                    spriteBatch.DrawString(font, "New Spell Unlocked!", new Vector2(300, 50), Color.Yellow);
                    newSpell.Draw(spriteBatch, new Rectangle(400 - 64, 100, 128, 32), true, false);
                    if (newSpell.spellSet != null)
                    {
                        spriteBatch.DrawString(font, "(Requires a " + newSpell.spellSet + ".)", new Vector2(400-64, 134), Color.Black);
                    }
                    Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, newSpell.description, new Vector2(400, 154), Tooltip.Align.CENTER);
                }
                else
                {
                    spriteBatch.DrawString(font, "Level complete!", new Vector2(300, 50), Color.Yellow);
                }

                foreach (UIButton button in winScreenButtons)
                {
                    button.Draw(spriteBatch);
                }
                spriteBatch.End();
                return;
            }
            else if (currentLevel == null)
            {
                spriteBatch.Begin();
                levelScreen.Draw(spriteBatch);
                foreach (UIButton button in mapScreenButtons)
                {
                    button.Draw(spriteBatch);
                }
                spriteBatch.End();
                return;
            }

            spriteBatch.Begin();

            GameState gameState = gameStates.Last();

            currentLevel.script.DrawBackground(spriteBatch);

            if( animation.finished )
                gameState.Draw(spriteBatch, gameStateOnSkip, MinionAnimationBatch.Empty);
            else
                animation.Draw(spriteBatch, gameStateOnSkip);

            foreach (UIButton button in gameScreenButtons)
            {
                button.Draw(spriteBatch);
            }

            foreach (CardCatalog catalog in catalogs)
            {
                catalog.Draw(spriteBatch, gameStates.Last());
            }

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

            gameState.DrawMouseOver(spriteBatch, inputState.MousePos);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
