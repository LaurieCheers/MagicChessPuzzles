using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicChessPuzzles
{
    public abstract class Property_int
    {
        public abstract int get(EffectContext context);

        public static Property_int create(object template)
        {
            if(template is int)
            {
                return new Property_Literal_int((int)template);
            }
            else if (template is double)
            {
                return new Property_Literal_int((int)(double)template);
            }
            else if (template is JSONArray)
            {
                return Property_int.create((JSONArray)template);
            }
            else if (template is object[])
            {
                return Property_int.create(new JSONArray((object[])template));
            }
            else
            {
                throw new ArgumentException("Invalid property template");
            }
        }

        public static Property_int create(JSONArray template)
        {
            switch (template.getString(0))
            {
                case "count": return new Property_Count(template.getArray(1));
                case "amountOf": return new Property_AmountOf(ResourceType.get(template.getString(1)));
                default: throw new ArgumentException("Invalid property " + template.getString(0));
            }
        }
    }

    class Property_Literal_int : Property_int
    {
        int value;
        public Property_Literal_int(int value) { this.value = value; }
        public override int get(EffectContext context)
        {
            if (context.textChanges == null)
                return value;
            else
                return context.textChanges.Apply_int(value);
        }
    }

    class Property_Count : Property_int
    {
        TriggerItemTest test;
        public Property_Count(JSONArray template)
        {
            this.test = TriggerItemTest.create(template);
        }
        public override int get(EffectContext context)
        {
            List<Minion> list = context.gameState.getMinions(test, context);
            return list.Count;
        }
    }

    class Property_AmountOf : Property_int
    {
        ResourceType type;
        public Property_AmountOf(ResourceType type)
        {
            this.type = type;
        }
        public override int get(EffectContext context)
        {
            return context.gameState.GetResourceAmount(type);
        }
    }


    public abstract class Property_TriggerItem
    {
        public abstract TriggerItem get(EffectContext context);

        public static Property_TriggerItem create(JSONArray template)
        {
            switch (template.getString(0))
            {
                case "SELF": return new Property_Self();
                case "TARGET": return new Property_Target();
                case "TRIGGERSOURCE": return new Property_TriggerSource();
                case "TRIGGERTARGET": return new Property_TriggerTarget();
                default: throw new ArgumentException("Invalid property " + template.getString(0));
            }
        }

        public static Property_TriggerItem create_MinionType(string template)
        {
            switch (template)
            {
                case "SELF": return new Property_Self();
                case "TARGET": return new Property_Target();
                case "TRIGGERSOURCE": return new Property_TriggerSource();
                case "TRIGGERTARGET": return new Property_TriggerTarget();
                default: return new Property_Literal_TriggerItem(TriggerItem.create(MinionType.get(template)));
            }
        }
    }

    class Property_Literal_TriggerItem : Property_TriggerItem
    {
        TriggerItem value;
        public Property_Literal_TriggerItem(TriggerItem value) { this.value = value; }
        public override TriggerItem get(EffectContext context) { return value; }
    }


    class Property_Self : Property_TriggerItem
    {
        public override TriggerItem get(EffectContext context) { return TriggerItem.create(context.self); }
    }

    class Property_Target : Property_TriggerItem
    {
        public override TriggerItem get(EffectContext context) { return context.target; }
    }

    class Property_TriggerSource : Property_TriggerItem
    {
        public override TriggerItem get(EffectContext context) { return context.trigger_source; }
    }

    class Property_TriggerTarget : Property_TriggerItem
    {
        public override TriggerItem get(EffectContext context) { return context.trigger_target; }
    }
}
