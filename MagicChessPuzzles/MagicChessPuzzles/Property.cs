using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicChessPuzzles
{
    public abstract class Property
    {
        public static Property<TriggerItem> create_TriggerItem(JSONArray template)
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
    }

    public abstract class Property<T>: Property
    {
        public abstract T get(EffectContext context);
    }

    class Property_Literal_TriggerItem : Property<TriggerItem>
    {
        TriggerItem value;
        public Property_Literal_TriggerItem(TriggerItem value) { this.value = value; }
        public override TriggerItem get(EffectContext context) { return value; }
    }

    class Property_Literal_int : Property<int>
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

    class Property_Self : Property<TriggerItem>
    {
        public override TriggerItem get(EffectContext context) { return TriggerItem.create(context.self); }
    }

    class Property_Target : Property<TriggerItem>
    {
        public override TriggerItem get(EffectContext context) { return context.target; }
    }

    class Property_TriggerSource : Property<TriggerItem>
    {
        public override TriggerItem get(EffectContext context) { return context.trigger_source; }
    }

    class Property_TriggerTarget : Property<TriggerItem>
    {
        public override TriggerItem get(EffectContext context) { return context.trigger_target; }
    }
}
