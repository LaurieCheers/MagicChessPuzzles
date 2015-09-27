using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using DragonGfx;

namespace MagicChessPuzzles
{
    public class LevelState
    {
        public bool unlocked;
        public bool done;
        public bool starred;
        public LevelScript script;

        public string name { get { return script.name; } }

        public LevelState(JSONTable template)
        {
            unlocked = true;// false;
            //done = true;
            //starred = true;
            script = new LevelScript(template);
        }
    }

    class LevelScreen
    {
        LevelState hovering;
        bool hoveringStar;
        List<List<LevelState>> chapters;
        readonly Vector2 levelSpacing = new Vector2(150,48);
        readonly Vector2 levelBasePos = new Vector2(100, 100);
        readonly Vector2 starOffset = new Vector2(16, 0);
        readonly Vector2 titleOffset = new Vector2(38, 0);

        public LevelState selectedLevel;

        public LevelScreen(JSONArray chaptersTemplate)
        {
            chapters = new List<List<LevelState>>();
            foreach (JSONArray levelListTemplate in chaptersTemplate.asJSONArrays())
            {
                List<LevelState> levels = new List<LevelState>();
                chapters.Add(levels);

                foreach (JSONTable levelTemplate in levelListTemplate.asJSONTables())
                {
                    levels.Add(new LevelState(levelTemplate));
                }
            }

            chapters[0][0].unlocked = true;
        }

        public void Update(Input.InputState inputState)
        {
            selectedLevel = null;

            float fCol = (inputState.MousePos.X - levelBasePos.X) / levelSpacing.X;
            int Col = (int)fCol;
            if (Col < 0 || Col >= chapters.Count)
            {
                hovering = null;
                hoveringStar = false;
                return;
            }

            List<LevelState> levels = chapters[Col];
            float fRow = (inputState.MousePos.Y + levelSpacing.Y/3 - levelBasePos.Y) / levelSpacing.Y;
            int Row = (int)fRow;
            if (Row < 0 || Row >= levels.Count)
            {
                hovering = null;
                hoveringStar = false;
                return;
            }

            hovering = levels[Row];
            hoveringStar = (fCol - Col) > 0.1f;

            if (inputState.WasMouseLeftJustPressed())
            {
                selectedLevel = hovering;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 currentPos = new Vector2(levelBasePos.X, levelBasePos.Y);
            foreach (List<LevelState> chapter in chapters)
            {
                currentPos.Y = levelBasePos.Y;
                foreach (LevelState level in chapter)
                {
                    if (!level.unlocked)
                        continue; // draw a little dot here?

                    spriteBatch.Draw(level.done ? Game1.levelDoneTexture : Game1.levelOpenTexture, currentPos, Color.White);
                    if (hovering == level && (!hoveringStar || !level.done))
                    {
                        spriteBatch.Draw(Game1.levelHoverTexture, currentPos, Color.White);
                    }
                    if (level.done)
                    {
                        spriteBatch.Draw(level.starred ? Game1.levelStarDoneTexture : Game1.levelStarOpenTexture, currentPos + starOffset, Color.White);
                        if (hovering == level && hoveringStar)
                        {
                            spriteBatch.Draw(Game1.levelStarHoverTexture, currentPos + starOffset, Color.White);
                        }
                    }
                    spriteBatch.DrawString(Game1.font, level.name, currentPos + titleOffset, Color.White);

                    currentPos.Y += levelSpacing.Y;
                }
                currentPos.X += levelSpacing.X;
            }
        }

        public void CheatAllBasic()
        {
            foreach (List<LevelState> chapter in chapters)
            {
                foreach (LevelState level in chapter)
                {
                    level.done = true;
                    if (level.script.unlocksCard != null)
                        level.script.unlocksCard.unlocked = true;
                }
            }
        }

        public void CheatAllSpells()
        {
            foreach (List<LevelState> chapter in chapters)
            {
                foreach (LevelState level in chapter)
                {
                    if (level.script.unlocksCard != null)
                        level.script.unlocksCard.unlocked = true;
                }
            }
        }

        public void CheatRestart()
        {
            foreach (List<LevelState> chapter in chapters)
            {
                foreach (LevelState level in chapter)
                {
                    level.done = false;
                    level.starred = false;
                    Card unlocksCard = level.script.unlocksCard;
                    if (unlocksCard != null)
                        unlocksCard.unlocked = unlocksCard.defaultUnlocked;
                }
            }
        }

        public LevelState GetNextLevel(LevelState currentLevel)
        {
            bool returnNext = false;
            foreach (List<LevelState> chapter in chapters)
            {
                foreach (LevelState level in chapter)
                {
                    if (returnNext)
                    {
                        return level;
                    }
                    else if (currentLevel == level)
                    {
                        returnNext = true;
                    }
                }
            }
            return null;
        }
    }
}
