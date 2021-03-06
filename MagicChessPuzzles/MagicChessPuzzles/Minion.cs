﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace MagicChessPuzzles
{
    public class MinionId
    {
    }

    [Flags]
    public enum Keyword
    {
        none = 0,
        slow = 1,
        unstoppable = 2,
        fireproof = 4,
        attackproof = 8,
        flammable = 16,
        corrosive = 32,
        alive = 64,
        fireform = 128,
        intangible = 256,
    }

    public struct MinionStats
    {
        public int maxHealth;
        public int health;
        public int attack;
        public int armor;
        public int move;
        public Range range;
        public Keyword keywords;

        public int burning;
        public int burningNextTurn;

        public bool hasKeyword(Keyword keyword)
        {
            return (keywords & keyword) == keyword;
        }
    }

    public class MinionType : PermanentType
    {
        public Effect_Base whenDies;
        public MinionStats stats;
        public string description;
        public string spells;
        public List<ResourceAmount> attackCost;
        public Texture2D onFireTexture;
        string awakenTypeName;
        MinionType awakenTypeCached;
        public MinionType awakenType
        {
            get
            {
                if (awakenTypeName != null && awakenTypeCached == null)
                    awakenTypeCached = MinionType.get(awakenTypeName);

                return awakenTypeCached;
            }
        }

        public MinionType(JSONTable template, ContentManager content)
            : base(template, content)
        {
            upkeep = ResourceAmount.createList(template.getJSON("upkeep", null));
            attackCost = ResourceAmount.createList(template.getJSON("attackCost", null));
            whenDies = Effect_Base.create(template.getArray("whenDies", null));
            awakenTypeName = template.getString("awakenType", null);
            string onFireTextureName = template.getString("onFireTexture", null);
            if (onFireTextureName != null)
                onFireTexture = content.Load<Texture2D>(onFireTextureName);
            stats.maxHealth = template.getInt("health", 1);
            stats.health = stats.maxHealth;
            stats.attack = template.getInt("attack", 0);
            stats.move = template.getInt("move", 1);
            stats.armor = template.getInt("armor", 0);
            Enum.TryParse<Range>(template.getString("range", "adjacent"), out stats.range);
            foreach (string abilityName in template.getArray("keywords", JSONArray.empty).asStrings())
            {
                Keyword keyword;
                bool ok = Enum.TryParse<Keyword>(abilityName, out keyword);
                Debug.Assert(ok);
                stats.keywords |= keyword;
            }
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
            if (name == "")
                return null;

            return types[name];
        }
    }

    public class Minion : Permanent
    {
        public MinionId minionId;
        public MinionType mtype;
        public bool slow_movedHalfWay;
        public MinionStats stats;
        public MinionStats permanentStats; // excludes effects from auras
        public string spells{ get{ return mtype.spells; } }

        public Minion(MinionType type, Point p, bool isEnemy)
            : base(type, p, isEnemy)
        {
            this.minionId = new MinionId();
            this.mtype = type;
            this.permanentStats = type.stats;
            this.stats = this.permanentStats;
            this.slow_movedHalfWay = false;
        }

        public Minion(Minion basis)
            : base(basis)
        {
            this.minionId = basis.minionId;
            this.mtype = basis.mtype;
            this.permanentStats = basis.permanentStats;
            this.stats = basis.stats;
            this.slow_movedHalfWay = basis.slow_movedHalfWay;
        }

        public override Permanent Clone()
        {
            return new Minion(this);
        }

        public override void ResetTemporaryEffects()
        {
            stats = permanentStats;
        }

        public void Transform(MinionType newtype, bool isEnemy)
        {
            mtype = newtype;
            type = newtype;
            int damage = stats.maxHealth - stats.health;
            this.permanentStats = mtype.stats;
            this.permanentStats.health = this.permanentStats.maxHealth - damage;
            this.isEnemy = isEnemy;
        }

        public override SpriteEffects spriteEffects
        {
            get
            {
                return isEnemy? SpriteEffects.FlipHorizontally: SpriteEffects.None;
            }
        }

        public override Vector2 drawPos
        { get {
            return new Vector2
            (
                base.drawPos.X - (slow_movedHalfWay ? 8.0f : (stats.hasKeyword(Keyword.slow) ? -8.0f : 0.0f)),
                base.drawPos.Y + 20.0f - type.texture.Height
            );
        }}

        public override void Draw(SpriteBatch spriteBatch, MinionAnimationBatch animation)
        {
            Vector2 pos = animation.GetPosition(this);
            
            Texture2D texture = mtype.texture;
            if (stats.burning + stats.burningNextTurn > 0 && mtype.onFireTexture != null)
                texture = mtype.onFireTexture;

            spriteBatch.Draw(texture, new Rectangle((int)pos.X, (int)pos.Y, type.texture.Width, type.texture.Height), null, Color.White, 0.0f, Vector2.Zero, isEnemy ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0.0f);

            string healthString = "" + this.stats.health;
            Vector2 healthStringSize = Game1.font.MeasureString(healthString);
            spriteBatch.DrawString(Game1.font, healthString, pos + new Vector2(32 - healthStringSize.X, type.texture.Height), Color.Red);
        }

        public override void DrawMouseOver(SpriteBatch spriteBatch)
        {
            List<string> tooltipText = new List<string>();
            tooltipText.Add(type.name);

            if (stats.attack > 0)
            {
                int attackDiff = stats.attack - permanentStats.attack;
                if (attackDiff > 0)
                    tooltipText.Add("Attack " + permanentStats.attack + " + bonus " + (stats.attack - permanentStats.attack));
                else if (attackDiff < 0)
                    tooltipText.Add("Attack " + stats.attack + "(was " + permanentStats.attack + ")");
                else
                    tooltipText.Add("Attack " + permanentStats.attack);
            }

            if (stats.armor > 0)
            {
                tooltipText.Add("Armor " + stats.armor + " (take " + stats.armor + " less damage)");
            }
            tooltipText.Add("Health " + stats.health + " (of " + stats.maxHealth + ")");
            tooltipText.AddRange(DragonGfx.Tooltip.StringToLines(mtype.description, Game1.font, 150));

            int burnAmount = stats.burning+stats.burningNextTurn;
            if (burnAmount > 0)
            {
                tooltipText.Add("On Fire " + burnAmount + " (" + burnAmount + " fire damage per turn)");
            }

            Vector2 popupPos = drawPos + new Vector2(35, -14);
            DragonGfx.Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, tooltipText, popupPos, DragonGfx.Tooltip.Align.LEFT);
        }

        public void Heal(int amount)
        {
            permanentStats.health += amount;
            if (permanentStats.health > permanentStats.maxHealth)
                permanentStats.health = permanentStats.maxHealth;
        }

        public void TakeDamage(GameState gameState, int amount, DamageType type, Permanent attacker)
        {
            if (type != DamageType.lightning && type != DamageType.acid)
            {
                amount -= stats.armor;
            }

            if (amount <= 0)
                return;

            if (stats.health > 0)
            {
                if (type == DamageType.fire)
                {
                    if (stats.hasKeyword(Keyword.fireproof))
                        return;

                    if (stats.hasKeyword(Keyword.fireform))
                    {
                        // fireform creatures are healed by fire
                        Heal(amount);
                        return;
                    }

                    if (stats.hasKeyword(Keyword.flammable))
                    {
                        permanentStats.burningNextTurn += amount;
                    }
                }

                if (type == DamageType.acid)
                {
                    stats.armor -= amount;
                    if (stats.armor < 0)
                    {
                        stats.health += stats.armor; // reduce health by the (negative) armor
                        stats.armor = 0;
                    }
                    permanentStats.armor = stats.armor;
                }
                else
                {
                    stats.health -= amount;
                }

                if (stats.health < 0)
                    stats.health = 0;
                permanentStats.health = stats.health;
                if (stats.health <= 0)
                {
                    gameState.Destroyed(position, attacker);
                }

                gameState.HandleTriggers(new TriggerEvent(TriggerType.onDamage, attacker, this));
            }
        }

        public void Attack(GameState gameState, Minion target, int bonusDamage, MinionAnimationBatch attackAnim, MinionAnimationBatch recoverAnim)
        {
            if (deleted)
                return;

            gameState.PayCost(mtype.attackCost);

            Vector2 basePos = drawPos;
            Vector2 targetPos = target.drawPos;
            targetPos = new Vector2(targetPos.X, targetPos.Y+target.type.texture.Height - type.texture.Height);
            Vector2 attackPos = basePos + (targetPos - basePos) * 0.5f;
            attackAnim.AddAnimation(this, basePos, attackPos);
            recoverAnim.AddAnimation(this, attackPos, basePos);

            DamageType damageType =
                stats.hasKeyword(Keyword.corrosive)? DamageType.acid:
                stats.hasKeyword(Keyword.fireform)? DamageType.fire:
                DamageType.attack;

            gameState.HandleTriggers(new TriggerEvent(TriggerType.onAttack, this, target));
            target.TakeDamage(gameState, stats.attack+bonusDamage, damageType, this);
        }

        public bool CheckAttack(GameState gameState, int bonusDamage, Point attackPos, MinionAnimationBatch attack, MinionAnimationBatch recover)
        {
            if (deleted)
                return false;

            Minion m = gameState.getMinionAt(attackPos);
            if (m == null || isEnemy == m.isEnemy || m.stats.health <= 0 || m.stats.hasKeyword(Keyword.attackproof))
                return false;

            Attack(gameState, m, bonusDamage, attack, recover);
            return true;
        }

        public bool CheckAttacks(GameState gameState, int bonusDamage, MinionAnimationBatch attack, MinionAnimationBatch recover, MinionAnimationSequence animation)
        {
            if (deleted || (stats.attack+bonusDamage) <= 0 || !gameState.CanPayCost(mtype.attackCost))
                return false;

            if (attack.HasAnimation(this))
                return false;

            Point[] attackOffsets = gameState.getOffsetsForRange(stats.range);

            if ((stats.attack+bonusDamage) > 0)
            {
                foreach (Point p in attackOffsets)
                {
                    if (CheckAttack(gameState, bonusDamage, new Point(position.X + p.X, position.Y + p.Y), attack, recover))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void ApplyOngoingLateEffects(GameState gameState, MinionAnimationSequence animation)
        {
            base.ApplyOngoingLateEffects(gameState, animation);

            TakeDamage(gameState, permanentStats.burning, DamageType.fire, this);
            permanentStats.burning = permanentStats.burningNextTurn;
            permanentStats.burningNextTurn = 0;
        }
    }
}
