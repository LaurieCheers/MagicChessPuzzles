using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public enum TriggerType
    {
        onDamage,
        beforeCombat,
        onAttack,
        onKill,
        onGain,
        beforeMove,
        onMove,
        onBlock,
        onSummon,
        beforeSpells,
        afterSpells,
        onSpell,
        beforeActualSpell,
        afterActualSpell,
    }

    public class TriggerEvent
    {
        public TriggerType type;
        public TriggerItem source;
        public TriggerItem target;

        public TriggerEvent(TriggerType type)
        {
            this.type = type;
        }

        public TriggerEvent(TriggerType type, Permanent a)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
        }

        public TriggerEvent(TriggerType type, Point a)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
        }

        public TriggerEvent(TriggerType type, Permanent a, Permanent b)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
            this.target = TriggerItem.create(b);
        }

        public TriggerEvent(TriggerType type, Point a, Permanent b)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
            this.target = TriggerItem.create(b);
        }

        public TriggerEvent(TriggerType type, Permanent a, Point b)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
            this.target = TriggerItem.create(b);
        }

        public TriggerEvent(TriggerType type, Point a, Point b)
        {
            this.type = type;
            this.source = TriggerItem.create(a);
            this.target = TriggerItem.create(b);
        }
    }

    public class WaitingTrigger
    {
        public TriggerEvent evt;
        public TriggeredAbility ability;
        public Permanent permanent;
        public Point position;

        public WaitingTrigger(TriggerEvent evt, TriggeredAbility ability, Permanent permanent, Point position)
        {
            this.evt = evt;
            this.ability = ability;
            this.permanent = permanent;
            this.position = position;
        }
    }

    public abstract class TriggerItem
    {
        public virtual Point position { get { return new Point(0, 0); } }
        public virtual MinionType minionType { get { return null; } }
        public virtual Permanent permanent { get { return null; } }
        public virtual Minion minion
        {
            get
            {
                Permanent p = permanent;
                if (p is Minion)
                    return (Minion)p;
                else
                    return null;
            }
        }
        public virtual Card card { get { return null; } }

        public static TriggerItem create(Permanent permanent) { return new TriggerItem_Permanent(permanent); }
        public static TriggerItem create(Point pos) { return new TriggerItem_Position(pos); }
        public static TriggerItem create(MinionType mtype) { return new TriggerItem_MinionType(mtype); }
        public static TriggerItem create(Card card) { return new TriggerItem_Card(card); }

        class TriggerItem_Permanent : TriggerItem
        {
            Permanent p;
            public TriggerItem_Permanent(Permanent permanent) { this.p = permanent; }
            public override Permanent permanent { get { return p; } }
            public override Point position { get { return p.position; } }
            public override MinionType minionType
            {
                get
                {
                    if (p is Minion)
                        return ((Minion)p).mtype;
                    else
                        return null;
                }
            }
        }

        class TriggerItem_Position : TriggerItem
        {
            Point pos;
            public TriggerItem_Position(Point pos) { this.pos = pos; }
            public override Point position { get { return pos; } }
        }

        class TriggerItem_MinionType : TriggerItem
        {
            MinionType mtype;
            public TriggerItem_MinionType(MinionType mtype) { this.mtype = mtype; }
            public override Point position { get { return new Point(); } }
            public override MinionType minionType { get { return mtype; } }
        }

        class TriggerItem_Card : TriggerItem
        {
            Card mcard;
            public TriggerItem_Card(Card card) { this.mcard = card; }
            public override Card card { get { return mcard; } }
        }
    }

    public class TriggeredAbility
    {
        TriggerType type;
        TriggerItemTest sourceTest;
        TriggerItemTest targetTest;
        Effect_Base effect;
        public readonly bool isAttackTrigger;

        public TriggeredAbility(TriggerType type)
        {
            this.type = type;
        }

        public TriggeredAbility(JSONArray template)
        {
            Enum.TryParse<TriggerType>(template.getString(0), out type);
            if(template.Length > 2)
                sourceTest = TriggerItemTest.create(template, 1);
            if(template.Length > 3)
                targetTest = TriggerItemTest.create(template, 2);

            effect = Effect_Base.createSingle(template.getArray(template.Length - 1));
            if (template.Length > 4)
            {
                isAttackTrigger = template.getBool(3);
            }
        }

        public static List<TriggeredAbility> createList(JSONArray listTemplate)
        {
            List<TriggeredAbility> result = new List<TriggeredAbility>();
            if (listTemplate != null)
            {
                foreach (JSONArray template in listTemplate.asJSONArrays())
                {
                    result.Add(new TriggeredAbility(template));
                }
            }
            return result;
        }

        public bool WillTrigger(TriggerType triggerType, EffectContext context)
        {
            if (type != triggerType)
                return false;

            if (sourceTest != null && !sourceTest.Test(context.trigger_source, context))
                return false;

            if (targetTest != null && !targetTest.Test(context.trigger_target, context))
                return false;

            return true;
        }

        public void Apply(EffectContext context)
        {
            effect.Apply(context);
        }
    }

    public abstract class TriggerItemTest
    {
        public abstract bool Test(TriggerItem item, EffectContext context);
        
        public static TriggerItemTest create(JSONArray template)
        {
            if (template == null)
                return null;
            else
                return create(template.getString(0), template);
        }

        public static TriggerItemTest create(string testType, JSONArray template)
        {
            switch (testType)
            {
                case "SELF": return new TriggerItemTest_SELF();
                case "TARGET": return new TriggerItemTest_TARGET();
                case "isEnemy": return new TriggerItemTest_IsEnemy();
                case "isAlly": return new TriggerItemTest_IsAlly();
                case "isType":
                case "ofType": return new TriggerItemTest_MinionType(template);
            }

            return null;
        }

        public static TriggerItemTest create(JSONArray template, int Idx)
        {
            Object obj = template.getProperty(Idx);
            if (obj is string)
            {
                return TriggerItemTest.create((string)obj, null);
            }
            else
            {
                return TriggerItemTest.create(new JSONArray((object[])obj));
            }
        }

        class TriggerItemTest_SELF : TriggerItemTest
        {
            public override bool Test(TriggerItem item, EffectContext context) { return item.permanent == context.self; }
        }

        class TriggerItemTest_TARGET : TriggerItemTest
        {
            public override bool Test(TriggerItem item, EffectContext context) { return item.position == context.target.position; }
        }

        class TriggerItemTest_IsEnemy : TriggerItemTest
        {
            public override bool Test(TriggerItem item, EffectContext context) { return item.permanent.isEnemy != context.self.isEnemy; }
        }

        class TriggerItemTest_IsAlly : TriggerItemTest
        {
            public override bool Test(TriggerItem item, EffectContext context) { return item.permanent.isEnemy == context.self.isEnemy; }
        }

        class TriggerItemTest_MinionType : TriggerItemTest
        {
            List<Property_TriggerItem> mtypes;
            public TriggerItemTest_MinionType(JSONArray template)
            {
                mtypes = new List<Property_TriggerItem>();
                for (int Idx = 1; Idx < template.Length; ++Idx)
                {
                    mtypes.Add(Property_TriggerItem.create_MinionType(template.getString(Idx)));
                }
            }
            public override bool Test(TriggerItem item, EffectContext context)
            {
                foreach(Property_TriggerItem mtype in mtypes)
                {
                    if (item.permanent.type == mtype.get(context).minionType)
                        return true;
                }
                return false;
            }
        }
    }
}
