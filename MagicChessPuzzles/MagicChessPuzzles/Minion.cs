using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MagicChessPuzzles
{
    public struct MinionStats
    {
        public enum Range
        {
            adjacent,
            nearby,
            knight
        };

        public int maxHealth;
        public int health;
        public int attack;
        public Range range;
        public bool slow;
    }

    public class MinionType : PermanentType
    {
        public Effect_Base whenDies;
        public MinionStats stats;
        public string description;
        public string spells;

        public MinionType(JSONTable template, ContentManager content)
            : base(template, content)
        {
            upkeep = ResourceAmount.createList(template.getJSON("upkeep", null));
            whenDies = Effect_Base.create(template.getArray("whenDies", null));
            stats.maxHealth = template.getInt("health", 1);
            stats.health = stats.maxHealth;
            stats.attack = template.getInt("attack", 1);
            Enum.TryParse<MinionStats.Range>(template.getString("range", "adjacent"), out stats.range);
            stats.slow = template.getBool("slow", false);
            spells = template.getString("spells", null);
            description = template.getString("description", "Just a guy");
        }

        static Dictionary<string, MinionType> types;

        public static void load(JSONTable template, ContentManager content)
        {
            types = new Dictionary<string, MinionType>();
            foreach (string key in template.Keys)
            {
                types.Add(key, new MinionType(template.getJSON(key), content));
            }
        }

        public static MinionType get(string name)
        {
            return types[name];
        }
    }

    public class Minion : Permanent
    {
        public MinionType mtype;
        public bool slow_movedHalfWay;
        public MinionStats stats;
        public MinionStats permanentStats; // excludes effects from auras
        public Permanent killedBy;
        public string spells{ get{ return mtype.spells; } }

        public Minion(MinionType type, Point p, bool isEnemy)
            : base(type, p, isEnemy)
        {
            this.mtype = type;
            this.permanentStats = type.stats;
            this.stats = this.permanentStats;
            this.slow_movedHalfWay = false;
        }

        public Minion(Minion basis)
            : base(basis)
        {
            this.mtype = basis.mtype;
            this.permanentStats = basis.permanentStats;
            this.stats = basis.stats;
            this.slow_movedHalfWay = basis.slow_movedHalfWay;
        }

        public override Permanent Clone()
        {
            return new Minion(this);
        }

        public override void WhenDies(GameState gameState, Point pos)
        {
            if (mtype.whenDies != null)
            {
                gameState.ApplyEffect(mtype.whenDies, this, pos);
            }
        }

        public override void ResetTemporaryEffects()
        {
            stats = permanentStats;
        }

        public void Transform(MinionType newtype)
        {
            mtype = newtype;
            type = newtype;
            this.permanentStats = mtype.stats;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(type.texture, new Rectangle((int)(screenPos.X - (slow_movedHalfWay ? 8.0f : (stats.slow? -8.0f: 0.0f))), (int)(screenPos.Y + 20.0f - type.texture.Height), type.texture.Width, type.texture.Height), null, Color.White, 0.0f, Vector2.Zero, isEnemy ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0.0f);
            //            spriteBatch.DrawString(Game1.font, type.name, pos, Color.Blue);
            spriteBatch.DrawString(Game1.font, "" + this.stats.health, screenPos + new Vector2(16, 20), Color.LightGreen);
        }

        public override void DrawMouseOver(SpriteBatch spriteBatch)
        {
            List<string> tooltipText = new List<string>();
            tooltipText.Add(type.name);

            int attackDiff = stats.attack - permanentStats.attack;
            if(attackDiff > 0)
                tooltipText.Add("Attack " + permanentStats.attack + " + bonus " + (stats.attack - permanentStats.attack));
            else if(attackDiff < 0)
                tooltipText.Add("Attack " + stats.attack + "(was " + permanentStats.attack + ")");
            else
                tooltipText.Add("Attack " + permanentStats.attack);
            
            tooltipText.Add("Health " + stats.health + " (of " + stats.maxHealth + ")");
            tooltipText.Add(mtype.description);

            Vector2 popupPos = screenPos + new Vector2(35, -14);
            LayeredImageGfx.Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, tooltipText, popupPos);
        }

        public void TakeDamage(GameState state, int attack, Permanent attacker)
        {
            if (stats.health > 0)
            {
                stats.health -= attack;
                permanentStats.health = stats.health;
                if (stats.health <= 0)
                {
                    state.AddKilled(position);
                    killedBy = attacker;
                }
            }
        }

        public void Attack(GameState gameState, Minion target)
        {
            target.TakeDamage(gameState, stats.attack, this);
        }

        public bool CheckAttack(GameState gameState, Point attackPos)
        {
            Minion m = gameState.getMinionAt(attackPos);
            if (m == null || isEnemy == m.isEnemy)
                return false;

            Attack(gameState, m);
            return true;
        }

        public void CheckAttacks(GameState gameState)
        {
            Point[] attackOffsets = GameState.adjacentOffsets;
            switch (stats.range)
            {
                case MinionStats.Range.nearby:
                    attackOffsets = GameState.nearbyOffsets;
                    break;
                case MinionStats.Range.knight:
                    attackOffsets = GameState.knightRangeOffsets;
                    break;
            }

            foreach (Point p in attackOffsets)
            {
                if (CheckAttack(gameState, new Point(position.X + p.X, position.Y + p.Y)))
                {
                    return;
                }
            }
        }

        public bool TakeAStep()
        {
            if (!stats.slow)
                return true;

            bool willArrive = slow_movedHalfWay;
            slow_movedHalfWay = !slow_movedHalfWay;

            return willArrive;
        }
    }
}
