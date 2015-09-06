using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace MagicChessPuzzles
{
    public class AnimationPhase
    {
        public const int Attack = 0;
        public const int Recover = 1;
        public const int Move = 2;
        public const int OngoingLate = 3;
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

            if (phases.Count > 0)
            {
                if (timeElapsed > phases[0].duration)
                {
                    timeElapsed -= phases[0].duration;
                    phases.RemoveAt(0);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (phases.Count > 0)
            {
                phases[0].Draw(spriteBatch, timeElapsed);
            }
        }
    }
}
