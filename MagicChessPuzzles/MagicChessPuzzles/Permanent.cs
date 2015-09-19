using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public class PermanentType
    {
        public string name;
        public Texture2D texture;
        public List<ResourceAmount> upkeep;
        public Effect_Base ongoing;
        public Effect_Base ongoing_late;
        public List<TriggeredAbility> triggers;

        public PermanentType(JSONTable template, ContentManager content)
        {
            name = template.getString("name");

            string textureName = template.getString("texture");
            texture = (textureName != null) ? content.Load<Texture2D>(textureName) : null;

            upkeep = ResourceAmount.createList(template.getJSON("upkeep", null));
            ongoing = Effect_Base.create(template.getArray("ongoing", null));
            ongoing_late = Effect_Base.create(template.getArray("ongoing_late", null));
            triggers = TriggeredAbility.createList(template.getArray("triggers", null));
        }
    }

    public abstract class Permanent
    {
        public Permanent killedBy;
        public PermanentType type;
        public Point position;
        public bool isEnemy;
        public bool deleted;

        public Permanent(PermanentType type, Point p, bool isEnemy)
        {
            this.type = type;
            position = p;
            this.isEnemy = isEnemy;
        }

        public Permanent(Permanent basis)
        {
            type = basis.type;
            position = basis.position;
            isEnemy = basis.isEnemy;
        }

        public virtual Vector2 drawPos
        {
            get { return GameState.GridToScreenPos(position); }
        }

        public virtual SpriteEffects spriteEffects
        {
            get { return SpriteEffects.None; }
        }

        public abstract void Draw(SpriteBatch spriteBatch, MinionAnimationBatch animation);
        public abstract void DrawMouseOver(SpriteBatch spriteBatch);
        public abstract Permanent Clone();

        public virtual void ResetTemporaryEffects()
        {
        }

        public virtual void WhenDies(GameState gameState, Point position)
        {
        }

        public void CheckTriggers(TriggerEvent evt, GameState gameState, List<WaitingTrigger> TriggersFound)
        {
            foreach (TriggeredAbility ability in type.triggers)
            {
                if (ability.WillTrigger(evt, this))
                {
                    TriggersFound.Add(new WaitingTrigger(evt, ability, this, position));
                }
            }
        }

        public bool TryPayUpkeep(GameState gameState)
        {
            if (gameState.CanPayCost(type.upkeep))
            {
                gameState.PayCost(type.upkeep);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ApplyOngoingEffects(GameState gameState, MinionAnimationSequence animation)
        {
            gameState.ApplyEffect(type.ongoing, new EffectContext(gameState, this, null, animation));
        }

        public virtual void ApplyOngoingLateEffects(GameState gameState, MinionAnimationSequence animation)
        {
            gameState.ApplyEffect(type.ongoing_late, new EffectContext(gameState, this, null, animation));
        }
    }

    class Ongoing : Permanent
    {
        Card baseCard;

        public Ongoing(Card baseCard, Point p)
            : base(baseCard.ongoingType, p, false)
        {
            this.baseCard = baseCard;
        }

        public Ongoing(Ongoing basis)
            : base(basis)
        {
            this.baseCard = basis.baseCard;
        }

        public override Permanent Clone()
        {
            return new Ongoing(this);
        }

        public override void Draw(SpriteBatch spriteBatch, MinionAnimationBatch animation)
        {
            spriteBatch.Draw(baseCard.image, drawPos, Color.White);
            //            spriteBatch.DrawString(Game1.font, baseCard.name, screenPos, Color.White);
        }

        public override void DrawMouseOver(SpriteBatch spriteBatch)
        {

        }
    }
}
