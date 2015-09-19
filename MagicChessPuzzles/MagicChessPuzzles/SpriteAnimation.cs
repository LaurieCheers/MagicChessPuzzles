using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace MagicChessPuzzles
{
    public class MinionAnimationBatch
    {
        class MinionAnimationElement
        {
            Vector2 startPos;
            Vector2 offset;

            public MinionAnimationElement(Point a, Point b)
            {
                startPos = GameState.GridToScreenPos(a);
                offset = GameState.GridToScreenPos(b) - startPos;
            }

            public MinionAnimationElement(Vector2 a, Vector2 b)
            {
                startPos = a;
                offset = b - a;
            }

            public Vector2 GetPosition(float lerp)
            {
                return startPos + offset * lerp;
            }
        }

        public MinionAnimationBatch(GameState initialState, float duration)
        {
            this.initialState = initialState;
            animations = new Dictionary<MinionId, MinionAnimationElement>();
            this.duration = new TimeSpan((long)(duration*1E7));
            this.elapsedTime = new TimeSpan(0);
        }

        GameState initialState;
        Dictionary<MinionId, MinionAnimationElement> animations;
        TimeSpan duration;
        TimeSpan elapsedTime;
        public bool finished { get { return animations.Count == 0 || elapsedTime >= duration; } }

        public static MinionAnimationBatch Empty = new MinionAnimationBatch(null, 0.0f);

        public void SetInitialGameState(GameState state)
        {
            initialState = state;
        }

        public void Update(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;
        }

        public void AddAnimation(Minion minion, Vector2 startPos, Vector2 destination)
        {
            animations[minion.minionId] = new MinionAnimationElement(startPos, destination);
        }

        public bool HasAnimation(Minion minion)
        {
            return animations.ContainsKey(minion.minionId);
        }

        public Vector2 GetPosition(Minion minion)
        {
            if (animations.ContainsKey(minion.minionId))
            {
                return animations[minion.minionId].GetPosition((float)(elapsedTime.TotalMilliseconds / duration.TotalMilliseconds));
            }
            else
            {
                return minion.drawPos;
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameState gameStateOnSkip)
        {
            initialState.Draw(spriteBatch, gameStateOnSkip, this);
        }
    }

    public class MinionAnimationSequence
    {
        List<MinionAnimationBatch> batches = new List<MinionAnimationBatch>();
        int currentBatch = 0;
        public bool finished { get { return batches.Count <= currentBatch; } }

        public void Clear()
        {
            batches.Clear();
            currentBatch = 0;
        }

        public MinionAnimationBatch AddBatch(GameState initialState, float duration)
        {
            MinionAnimationBatch newBatch = new MinionAnimationBatch(initialState, duration);
            batches.Add(newBatch);
            return newBatch;
        }

        public void AddAnimation(Minion minion, Vector2 startPos, Vector2 destination)
        {
            batches.Last().AddAnimation(minion, startPos, destination);
        }

        public void Update(GameTime gameTime)
        {
            if (batches.Count > currentBatch)
            {
                MinionAnimationBatch batch = batches[currentBatch];
                batch.Update(gameTime);
                if (batch.finished)
                {
                    currentBatch++;
                }
            }
        }

        public Vector2 GetPosition(Minion minion)
        {
            if (finished)
                return minion.drawPos;

            MinionAnimationBatch batch = batches[currentBatch];
            return batch.GetPosition(minion);
        }

        public void Draw(SpriteBatch spriteBatch, GameState gameStateOnSkip)
        {
            MinionAnimationBatch batch = batches[currentBatch];
            batch.Draw(spriteBatch, gameStateOnSkip);
        }
    }

/*    public class AnimationPhase
    {
        public const int Attack = 1;
        public const int Recover = 2;
        public const int Move = 3;
    }

    public class SpriteAnimation
    {
        class SpriteAnimationEntry
        {
            Rectangle start;
            Rectangle end;
            Texture2D texture;
            Color color;
            SpriteEffects effects;

            public SpriteAnimationEntry(Rectangle start, Rectangle end, Texture2D texture, Color color, SpriteEffects effects)
            {
                this.start = start;
                this.end = end;
                this.texture = texture;
                this.color = color;
                this.effects = effects;
            }

            public SpriteAnimationEntry(Vector2 start, Vector2 end, Texture2D texture, Color color, SpriteEffects effects)
            {
                this.start = new Rectangle((int)start.X, (int)start.Y, texture.Width, texture.Height);
                this.end = new Rectangle((int)end.X, (int)end.Y, texture.Width, texture.Height);
                this.texture = texture;
                this.color = color;
                this.effects = effects;
            }

            public void Draw(SpriteBatch spriteBatch, float lerp)
            {
                spriteBatch.Draw(texture, start.Lerp(end, lerp), null, color, 0.0f, Vector2.Zero, effects, 0);
            }
        }

        public class SpriteAnimationPhase
        {
            public TimeSpan duration;
            List<SpriteAnimationEntry> commands = new List<SpriteAnimationEntry>();

            public SpriteAnimationPhase(TimeSpan duration)
            {
                this.duration = duration;
            }

            public bool IsFinished(TimeSpan elapsedTime)
            {
                return commands.Count == 0 || elapsedTime > duration;
            }

            public void Add(Vector2 start, Vector2 end, Texture2D texture, Color color, SpriteEffects effects)
            {
                commands.Add(new SpriteAnimationEntry(start, end, texture, color, effects));
            }

            public void Draw(SpriteBatch spriteBatch, TimeSpan timeElapsed)
            {
                double lerp = timeElapsed.TotalMilliseconds / duration.TotalMilliseconds;
                if (lerp <= 1.0f)
                {
                    foreach (SpriteAnimationEntry entry in commands)
                    {
                        entry.Draw(spriteBatch, (float)lerp);
                    }
                }
            }
        }

        List<SpriteAnimationPhase> phases = new List<SpriteAnimationPhase>();

        TimeSpan timeElapsed;

        public bool animating { get { return phases.Count > 0; } }

        public void Clear()
        {
            timeElapsed = new TimeSpan(0);
            phases.Clear();
        }

        public void AddPhase(float seconds)
        {
            float SECS_TO_TICKS = 1E7f;
            phases.Add(new SpriteAnimationPhase(new TimeSpan((long)(seconds * SECS_TO_TICKS))));
        }

        public SpriteAnimationPhase GetPhase(int phase)
        {
            return phases[phase];
        }

        public void AddStatic(int phase, Vector2 pos, Texture2D texture, Color color, SpriteEffects effects)
        {
            phases[phase].Add(pos, pos, texture, color, effects);
        }

        public void AddMove(int phase, Vector2 start, Vector2 end, Texture2D texture, Color color, SpriteEffects effects)
        {
            phases[phase].Add(start, end, texture, color, effects);
        }

        public void Update(GameTime gameTime)
        {
            timeElapsed += gameTime.ElapsedGameTime;

            if (phases.Count > 0 && phases[0].IsFinished(timeElapsed))
            {
                phases.RemoveAt(0);
                timeElapsed = new TimeSpan();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (phases.Count > 0)
            {
                phases[0].Draw(spriteBatch, timeElapsed);
            }
        }
    }*/
}
