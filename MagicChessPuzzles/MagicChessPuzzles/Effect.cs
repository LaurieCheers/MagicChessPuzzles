﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public enum DamageType
    {
        attack,
        fire, // interact with fireproof/flammable
        acid, // destroy armor
        lightning, // ignore armor
    }

    public class EffectContext
    {
        public MinionAnimationSequence animation;
        public MinionAnimationBatch attackAnim;
        public MinionAnimationBatch recoverAnim;
        public GameState gameState;
        public TriggerItem target;
        public Permanent self;
        public TriggerItem trigger_source;
        public TriggerItem trigger_target;
        public TextChanges textChanges;

        public EffectContext(GameState gameState, Permanent self, Point targetPos, TriggerEvent evt, MinionAnimationBatch attackAnim, MinionAnimationBatch recoverAnim, MinionAnimationSequence animation)
        {
            this.gameState = gameState;
            this.self = self;
            this.target = gameState.getItemAt(targetPos);
            if (evt != null)
            {
                this.trigger_source = evt.source;
                this.trigger_target = evt.target;
            }
            this.attackAnim = attackAnim;
            this.recoverAnim = recoverAnim;
            this.animation = animation;
        }

        public EffectContext(GameState gameState, Permanent self, TriggerEvent evt, MinionAnimationSequence animation)
        {
            this.gameState = gameState;
            this.self = self;
            this.target = TriggerItem.create(self);
            if (evt != null)
            {
                this.trigger_source = evt.source;
                this.trigger_target = evt.target;
            }
            this.animation = animation;
        }

        public EffectContext(GameState gameState, TextChanges textChanges, Permanent self, TriggerItem target, TriggerEvent evt, MinionAnimationSequence animation)
        {
            this.gameState = gameState;
            this.textChanges = textChanges;
            this.self = self;
            this.target = target;
            if (evt != null)
            {
                this.trigger_source = evt.source;
                this.trigger_target = evt.target;
            }
            this.animation = animation;
        }

        public EffectContext(EffectContext basis, Point newTarget)
        {
            this.gameState = basis.gameState;
            this.self = basis.self;
            this.target = gameState.getItemAt(newTarget);
            this.trigger_source = basis.trigger_source;
            this.trigger_target = basis.trigger_target;
            this.animation = basis.animation;
        }

        public EffectContext(EffectContext basis, Permanent p)
        {
            this.gameState = basis.gameState;
            this.self = basis.self;
            this.target = TriggerItem.create(p);
            this.trigger_source = basis.trigger_source;
            this.trigger_target = basis.trigger_target;
            this.animation = basis.animation;
        }
    }

    public abstract class Effect_Base
    {
        public static Effect_Base createSingle(JSONArray template)
        {
            string effectType = template.getString(0);
            switch (effectType.ToLower())
            {
                case "rewind":
                    return new Effect_Rewind(template);
                case "gain":
                    return new Effect_GainResources(template);
                case "lose":
                    return new Effect_LoseResources(template);
                case "minion":
                    return new Effect_CreateMinion(template);
                case "monster":
                    return new Effect_Monster(template);
                case "attack":
                    return new Effect_Attack(template);
                case "aura":
                    return new Effect_Aura(template);
                case "damage":
                    return new Effect_Damage(template);
                case "kill":
                    return new Effect_Kill();
                case "area":
                    return new Effect_Area(template);
                case "transform":
                    return new Effect_Transform(template);
                case "losegame":
                    return new Effect_LoseGame(template);
                case "sequence":
                    return new Effect_Sequence(template);
                case "rewrite_add":
                    return new Effect_Rewrite_Add(template);
                case "upgrade":
                    return new Effect_Upgrade(template);
                case "all":
                    return new Effect_All(template);
                case "heal":
                    return new Effect_Heal(template);
                case "cast":
                    return new Effect_Cast(template);
                case "awaken":
                    return new Effect_Awaken(template);
                case "pattern":
                    return new Effect_Pattern(template);
                default:
                    throw new ArgumentException("Unknown effect type: " + effectType);
            }
        }

        public static Effect_Base create(JSONArray template)
        {
            if (template != null)
            {
                return Effect_Base.createSingle(template);
            }
            return null;
        }

        public abstract void Apply(EffectContext context);
        public virtual bool HasANumber() { return false; }
        public virtual bool HasAnArea() { return false; }
    }

    class Effect_GainResources : Effect_Base
    {
        ResourceType type;
        Property_int amount;

        public Effect_GainResources(JSONArray template)
        {
            type = ResourceType.get(template.getString(1));
            amount = Property_int.create(template.getProperty(2));
        }

        public override void Apply(EffectContext context)
        {
            context.gameState.GainResource(type,amount.get(context));
        }
    }

    class Effect_LoseResources : Effect_Base
    {
        ResourceType type;
        Property_int amount;

        public Effect_LoseResources(JSONArray template)
        {
            type = ResourceType.get(template.getString(1));
            amount = Property_int.create(template.getProperty(2));
        }

        public override void Apply(EffectContext context)
        {
            context.gameState.GainResource(type, -amount.get(context));
        }
    }

    class Effect_CreateMinion : Effect_Base
    {
        Property_TriggerItem type;

        public Effect_CreateMinion(JSONArray template)
        {
            type = Property_TriggerItem.create_MinionType(template.getString(1));
        }

        public override void Apply(EffectContext context)
        {
            context.gameState.CreateMinion
            (
                type.get(context).minionType,
                context.self == null ? false : context.self.isEnemy,
                context.target.position
            );
        }
    }
    class Effect_Monster : Effect_Base
    {
        Property_TriggerItem type;

        public Effect_Monster(JSONArray template)
        {
            type = Property_TriggerItem.create_MinionType(template.getString(1));
        }

        public override void Apply(EffectContext context)
        {
            context.gameState.CreateMinion
            (
                type.get(context).minionType,
                true,
                context.target.position
            );
        }
    }

    class Effect_Transform : Effect_Base
    {
        Property_TriggerItem type;

        public Effect_Transform(JSONArray template)
        {
            type = Property_TriggerItem.create_MinionType(template.getString(1));
        }

        public override void Apply(EffectContext context)
        {
            Minion m = context.target.minion;
            if (m != null)
            {
                m.Transform(type.get(context).minionType, context.self.isEnemy);
            }
        }
    }

    class Effect_Awaken : Effect_Base
    {
        public Effect_Awaken(JSONArray template)
        {
        }

        public override void Apply(EffectContext context)
        {
            Minion m = context.target.minion;
            if (m != null && m.mtype.awakenType != null)
            {
                m.Transform(m.mtype.awakenType, context.self.isEnemy);
            }
        }
    }

    class Effect_Sequence : Effect_Base
    {
        List<Effect_Base> effects;

        public Effect_Sequence(JSONArray template)
        {
            effects = new List<Effect_Base>();
            for (int Idx = 1; Idx < template.Length; Idx++)
            {
                effects.Add(Effect_Base.createSingle(template.getArray(Idx)));
            }
        }

        public override void Apply(EffectContext context)
        {
            foreach (Effect_Base effect in effects)
            {
                effect.Apply(context);
                context.gameState.CleanUp(context.animation);
            }
        }

        public override bool HasANumber()
        {
            foreach (Effect_Base effect in effects)
            {
                if (effect.HasANumber())
                    return true;
            }
            return false;
        }

        public override bool HasAnArea()
        {
            foreach (Effect_Base effect in effects)
            {
                if (effect.HasAnArea())
                    return true;
            }
            return false;
        }
    }

    class Effect_LoseGame : Effect_Base
    {
        public Effect_LoseGame(JSONArray template)
        {
        }

        public override void Apply(EffectContext context)
        {
            context.gameState.LoseGame();
        }
    }

    class Effect_Attack : Effect_Base
    {
        Property_int bonusDamage;

        public Effect_Attack(JSONArray template)
        {
            if(template.Length > 1)
                bonusDamage = Property_int.create( template.getArray(1) );
        }

        public override void Apply(EffectContext context)
        {
            Minion attacker = context.target.minion;
            if(attacker != null)
            {
                MinionAnimationBatch attackAnim;
                MinionAnimationBatch recoverAnim;
                if (context.attackAnim != null)
                {
                    attackAnim = context.attackAnim;
                    recoverAnim = context.recoverAnim;
                }
                else
                {
                    attackAnim = context.animation.AddBatch(new GameState(context.gameState), Game1.ATTACK_ANIM_DURATION);
                    recoverAnim = context.animation.AddBatch(context.gameState, Game1.RECOVER_ANIM_DURATION);
                }

                attacker.CheckAttacks(context.gameState, bonusDamage != null? bonusDamage.get(context): 0, attackAnim, recoverAnim, context.animation);

                recoverAnim.SetInitialGameState(new GameState(context.gameState));
            }
        }
    }

    class Effect_Area : Effect_Base
    {
        Range range;
        TriggerItemTest test;
        Effect_Base effect;

        public Effect_Area(JSONArray template)
        {
            Enum.TryParse<Range>(template.getString(1), out range);
            if (template.Length == 3)
            {
                effect = Effect_Base.create(template.getArray(2));
            }
            else
            {
                test = TriggerItemTest.create(template.getArray(2));
                effect = Effect_Base.create(template.getArray(3));
            }
        }

        public override void Apply(EffectContext context)
        {
            Point basePos = context.target.position;
            foreach (Point offset in context.gameState.getOffsetsForRange(range))
            {
                EffectContext newContext = new EffectContext(context, new Point(basePos.X+offset.X, basePos.Y+offset.Y));
                if (test == null || test.Test(newContext.target, context))
                {
                    effect.Apply(newContext);
                }
            }
        }

        public override bool HasANumber() { return effect.HasANumber(); }
        public override bool HasAnArea() {return true;}
    }

    class Effect_Pattern : Effect_Base
    {
        TriggerItemTest test;
        Effect_Base effect;
        Property_int amount;

        public Effect_Pattern(JSONArray template)
        {
            amount = new Property_Literal_int(template.getInt(1));
            if (template.Length == 3)
            {
                effect = Effect_Base.create(template.getArray(2));
            }
            else
            {
                test = TriggerItemTest.create(template.getArray(2));
                effect = Effect_Base.create(template.getArray(3));
            }
        }

        static Point[] patternElements = new Point[] { new Point(-1, 0), new Point(1, 0), new Point(0, 1), new Point(0, -1),
         new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1),
        new Point(-2, 0), new Point(2, 0), new Point(0, 2), new Point(0, -2)};

        public override void Apply(EffectContext context)
        {
            Point basePos = context.target.position;
            int number = amount.get(context);
            if (number % 2 == 1)
            {
                Apply(context, basePos);
                number--;
            }

            for (int Idx = 0; Idx < number && Idx < patternElements.Count(); ++Idx)
            {
                Point offset = patternElements[Idx];
                Apply(context, new Point(basePos.X + offset.X, basePos.Y + offset.Y));
            }
        }

        public void Apply(EffectContext baseContext, Point pos)
        {
            EffectContext newContext = new EffectContext(baseContext, pos);
            if (test == null || test.Test(newContext.target, baseContext))
            {
                effect.Apply(newContext);
            }
        }

        public override bool HasANumber() { return amount is Property_Literal_int || effect.HasANumber(); }
    }

    class Effect_All : Effect_Base
    {
        TriggerItemTest test;
        Effect_Base effect;

        public Effect_All(JSONArray template)
        {
            test = TriggerItemTest.create(template.getArray(1));
            effect = Effect_Base.create(template.getArray(2));
        }

        public override void Apply(EffectContext context)
        {
            Point basePos = context.target.position;
            foreach(Minion m in context.gameState.getMinions(test, context))
            {
                effect.Apply(new EffectContext(context, m));
            }
        }

        public override bool HasANumber() { return effect.HasANumber(); }
        public override bool HasAnArea() { return true; }
    }

    class Effect_Kill : Effect_Base
    {
        public override void Apply(EffectContext context)
        {
            Minion m = context.target.minion;
            context.gameState.Destroyed(m.position, context.self);
        }
    }

    class Effect_Damage : Effect_Base
    {
        DamageType type;
        Property_int amount;

        public Effect_Damage(JSONArray template)
        {
            amount = new Property_Literal_int(template.getInt(1));
            Enum.TryParse<DamageType>(template.getString(2), out type);
        }

        public override void Apply(EffectContext context)
        {
            Minion m = context.target.minion;
            if (m != null)
                m.TakeDamage(context.gameState, amount.get(context), type, context.self);
        }

        public override bool HasANumber() { return amount is Property_Literal_int; }
    }

    class Effect_Heal : Effect_Base
    {
        Property_int amount;

        public Effect_Heal(JSONArray template)
        {
            if(template.Length >= 2)
                amount = new Property_Literal_int(template.getInt(1));
        }

        public override void Apply(EffectContext context)
        {
            Minion m = context.target.minion;
            if (m != null)
            {
                if (amount == null)
                    m.Heal(m.stats.maxHealth);
                else
                    m.Heal(amount.get(context));
            }
        }

        public override bool HasANumber() { return amount is Property_Literal_int; }
    }

    class Effect_Cast : Effect_Base
    {
        Property_TriggerItem cardProperty;
        Range range;

        public Effect_Cast(JSONArray template)
        {
            cardProperty = Property_TriggerItem.create(template.getArray(1));
            Enum.TryParse<Range>(template.getString(2), out range);
        }

        public override void Apply(EffectContext context)
        {
            Card card = cardProperty.get(context).card;
            List<TriggerItem> positions = context.gameState.getItemsInRange(range, context.self.position);
            foreach (TriggerItem possibleTarget in positions)
            {
                if(context.gameState.CanPlayCard(card, possibleTarget))
                {
                    context.gameState.ApplyCardEffect(card, (Minion)context.self, possibleTarget, context.animation);
                    break;
                }
            }
        }
    }

    class Effect_Rewrite_Add : Effect_Base
    {
        Property_int amount;
        public Effect_Rewrite_Add(JSONArray template)
        {
            amount = new Property_Literal_int(template.getInt(1));
        }
        public override void Apply(EffectContext context)
        {
            context.gameState.AddTextChange(context.target.card, new TextChanges.TextChange_Add(amount.get(context)));
        }
        public override bool HasANumber() { return amount is Property_Literal_int; }
    }

    class Effect_Aura : Effect_Base
    {
        enum AuraStat
        {
            attack,
            health
        };
        AuraStat stat;

        enum AuraOperator
        {
            mul,
            set,
            lowerTo,
            raiseTo,
            add
        };
        AuraOperator op;
        float value;

        public Effect_Aura(JSONArray template)
        {
            Enum.TryParse<AuraOperator>(template.getString(1), out op);
            Enum.TryParse<AuraStat>(template.getString(2), out stat);
            value = template.getFloat(3);
        }

        public override void Apply(EffectContext context)
        {
            List<Minion> minions = context.gameState.getMinionsInRange(Range.nearby, context.target.position);
            foreach (Minion m in minions)
            {
                if (m.isEnemy == context.self.isEnemy)
                {
                    float oldValue = GetStat(m);
                    float newValue = ApplyOperator(oldValue);
                    SetStat(m, newValue);
                }
            }
        }

        public float ApplyOperator(float oldValue)
        {
            switch (op)
            {
                case AuraOperator.add: return oldValue + value;
                case AuraOperator.mul: return oldValue * value;
                case AuraOperator.set: return value;
                case AuraOperator.lowerTo: return (oldValue < value) ? oldValue : value;
                case AuraOperator.raiseTo: return (oldValue < value) ? value : oldValue;
                default: return oldValue;
            }
        }

        public float GetStat(Minion m)
        {
            switch (stat)
            {
                case AuraStat.attack: return m.stats.attack;
                case AuraStat.health: return m.stats.health;
                default:
                    return 0;
            }
        }

        public void SetStat(Minion m, float value)
        {
            switch (stat)
            {
                case AuraStat.attack: m.stats.attack = (int)value;
                    break;
                case AuraStat.health: m.stats.health = (int)value;
                    break;
            }
        }
    }

    class Effect_Rewind : Effect_Base
    {
        public Effect_Rewind(JSONArray template)
        {
        }

        public override void Apply(EffectContext context)
        {
            throw new InvalidOperationException("Rewind effect should not be executed");
        }
    }

    class Effect_Upgrade : Effect_Base
    {
        int attack;
        int health;
        List<Keyword> keywords;
        List<TriggeredAbility> triggers;

        public Effect_Upgrade(JSONArray template)
        {
            attack = template.getInt(1);
            health = template.getInt(2);
            keywords = new List<Keyword>();
            foreach (string s in template.getArray(3).asStrings())
            {
                Keyword k;
                if (!Enum.TryParse<Keyword>(s, out k))
                    throw new ArgumentException();
                keywords.Add(k);
            }

            triggers = new List<TriggeredAbility>();
            foreach (JSONArray abilityTemplate in template.getArray(4).asJSONArrays())
            {
                triggers.Add(new TriggeredAbility(abilityTemplate));
            }
        }

        public override void Apply(EffectContext context)
        {
            
        }
    }

    public class TextChanges
    {
        public static TextChanges none = new TextChanges();

        public abstract class TextChange<T>
        {
            public abstract T Apply(T original);
        }

        public class TextChange_Add : TextChange<int>
        {
            int amount;
            public TextChange_Add(int amount) { this.amount = amount; }
            public override int Apply(int original) { return original + amount; }
        }

        public TextChanges()
        {
            intChanges = new List<TextChange<int>>();
        }

        readonly List<TextChange<int>> intChanges = new List<TextChange<int>>();
        public TextChanges(TextChange<int> change)
        {
            intChanges = new List<TextChange<int>>(){change};
        }

        public TextChanges(TextChanges basis, TextChange<int> newIntChange)
        {
            intChanges = new List<TextChange<int>>();
            foreach (TextChange<int> change in basis.intChanges)
            {
                intChanges.Add(change);
            }
            intChanges.Add(newIntChange);
        }

        public int Apply_int(int original)
        {
            int current = original;
            foreach (TextChange<int> change in intChanges)
            {
                current = change.Apply(current);
            }
            return current;
        }

        Dictionary<List<string>, List<string>> cachedChanges = new Dictionary<List<string>,List<string>>();
        public List<string> Apply(List<string> original)
        {
            if (cachedChanges.ContainsKey(original))
                return cachedChanges[original];

            List<string> result = new List<string>();
            foreach (string origLine in original)
            {
                result.Add(Apply(origLine));
            }
            cachedChanges[original] = result;
            return result;
        }

        public string Apply(string original)
        {
            string result = "";
            int lastStringIdx = 0;
            for (int Idx = 0; Idx < original.Length; Idx++)
            {
                int startIdx = Idx;
                char c = original[Idx];
                int numberFound = 0;
                while (c >= '0' && c <= '9')
                {
                    numberFound = numberFound * 10 + c - '0';
                    Idx++;

                    if (Idx >= original.Length)
                        break;

                    c = original[Idx];
                }
                if (startIdx < Idx)
                {
                    // we found a number
                    result += original.Substring(lastStringIdx, startIdx - lastStringIdx);
                    foreach(TextChange<int> change in intChanges)
                    {
                        numberFound = change.Apply(numberFound);
                    }
                    result += numberFound;
                    lastStringIdx = Idx;
                }
            }
            result += original.Substring(lastStringIdx, original.Length - lastStringIdx);
            return result;
        }
    }
}
