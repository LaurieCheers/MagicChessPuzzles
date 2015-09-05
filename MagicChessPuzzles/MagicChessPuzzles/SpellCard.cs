using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
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

        public PermanentType(JSONTable template, ContentManager content)
        {
            name = template.getString("name");
            
            string textureName = template.getString("texture");
            texture = ( textureName != null )? content.Load<Texture2D>(textureName): null;

            upkeep = ResourceAmount.createList(template.getJSON("upkeep", null));
            ongoing = Effect_Base.create(template.getArray("ongoing", null));
            ongoing_late = Effect_Base.create(template.getArray("ongoing_late", null));
        }
    }

    public class ResourceType
    {
        public string name;
        public Texture2D texture;

        public ResourceType(JSONTable template, ContentManager content)
        {
            name = template.getString("name");
            
            string textureName = template.getString("texture");
            texture = ( textureName != null )? content.Load<Texture2D>(textureName): null;
        }

        static Dictionary<string, ResourceType> types;

        public static void load(JSONTable template, ContentManager content)
        {
            types = new Dictionary<string, ResourceType>();
            foreach (string key in template.Keys)
            {
                types.Add(key, new ResourceType(template.getJSON(key), content));
            }
        }

        public static ResourceType get(string name)
        {
            return types[name];
        }
    }

    public class ResourceAmount
    {
        public readonly ResourceType type;
        public int amount;

        public ResourceAmount(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }

        public static List<ResourceAmount> createList(JSONTable template)
        {
            List<ResourceAmount> costs = new List<ResourceAmount>();
            if (template != null)
            {
                foreach (string key in template.Keys)
                {
                    costs.Add(new ResourceAmount(ResourceType.get(key), template.getInt(key)));
                }
            }
            return costs;
        }

        public static void Draw(SpriteBatch spriteBatch, List<ResourceAmount> resources, Vector2 position)
        {
            int currentX = (int)position.X;
            int resourceIconSize = 16;

            foreach (ResourceAmount ra in resources)
            {
                spriteBatch.Draw(ra.type.texture, new Rectangle(currentX, (int)position.Y, resourceIconSize, resourceIconSize), Color.White);
                currentX += resourceIconSize + 4;
                string amountLabel = "" + ra.amount;
                spriteBatch.DrawString(Game1.font, amountLabel, new Vector2(currentX, (int)position.Y), Color.Black);
                currentX += (int)Game1.font.MeasureString(amountLabel).X + 4;
            }
        }
    }


    class Property
    {
        public static Property<MinionType> minionTypeProperty(string template)
        {
            switch (template)
            {
                case "KILLER": return new Property_Killer();
                default: return new Property_Literal<MinionType>(MinionType.get(template));
            }
        }
    }

    abstract class Property<T>
    {
        public abstract T get(Permanent context);
    }

    class Property_Literal<T> : Property<T>
    {
        T value;
        public Property_Literal(T value) { this.value = value; }
        public override T get(Permanent context) { return value; }
    }

    class Property_Killer : Property<MinionType>
    {
        public override MinionType get(Permanent context)
        {
            if (context is Minion)
            {
                Permanent p = ((Minion)context).killedBy;
                if (p is Minion)
                {
                    return ((Minion)p).mtype;
                }
            }
            return null;
        }
    }

    public enum Range
    {
        self,
        adjacent,
        nearby,
        knight
    };

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
                case "minion":
                    return new Effect_CreateMinion(template);
                case "attack":
                    return new Effect_Attack(template);
                case "aura":
                    return new Effect_Aura(template);
                case "damage":
                    return new Effect_Damage(template);
                case "area":
                    return new Effect_Area(template);
                case "transform":
                    return new Effect_Transform(template);
                case "losegame":
                    return new Effect_LoseGame(template);
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

        public abstract void Apply(GameState state, Permanent context, Point pos);
    }

    class Effect_GainResources : Effect_Base
    {
        List<ResourceAmount> resources;

        public Effect_GainResources(JSONArray template)
        {
            resources = ResourceAmount.createList(template.getJSON(1));
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            state.GainResources(resources);
        }
    }

    class Effect_CreateMinion : Effect_Base
    {
        Property<MinionType> type;

        public Effect_CreateMinion(JSONArray template)
        {
            type = Property.minionTypeProperty(template.getString(1));
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            state.CreateMinions(type.get(context), context == null? false: context.isEnemy, pos);
        }
    }

    class Effect_Transform : Effect_Base
    {
        Property<MinionType> type;

        public Effect_Transform(JSONArray template)
        {
            type = Property.minionTypeProperty(template.getString(1));
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            if (context != null && context is Minion)
            {
                ((Minion)context).Transform(type.get(context));
            }
        }
    }

    class Effect_LoseGame : Effect_Base
    {
        public Effect_LoseGame(JSONArray template)
        {
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            state.LoseGame();
        }
    }

    class Effect_Attack : Effect_Base
    {
        public Effect_Attack(JSONArray template)
        {
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
        }
    }

    class Effect_Area : Effect_Base
    {
        Range range;
        Effect_Base effect;

        public Effect_Area(JSONArray template)
        {
            Enum.TryParse<Range>(template.getString(1), out range);
            effect = Effect_Base.create(template.getArray(2));
        }

        public override void Apply(GameState state, Permanent context, Point basePos)
        {
            foreach(Point offset in state.getOffsetsForRange(range))
            {
                effect.Apply(state, context, new Point(basePos.X+offset.X, basePos.Y+offset.Y));
            }
        }
    }

    class Effect_Damage : Effect_Base
    {
        Range range;
        int amount;

        public Effect_Damage(JSONArray template)
        {
            Enum.TryParse<Range>(template.getString(1), out range);
            amount = template.getInt(2);
        }

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            foreach (Minion m in state.getMinionsInRange(range, pos))
            {
                m.TakeDamage(state, amount, context);
            }
        }
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

        public override void Apply(GameState state, Permanent context, Point pos)
        {
            List<Minion> minions = state.getMinionsInRange(Range.nearby, pos);
            foreach (Minion m in minions)
            {
                float oldValue = GetStat(m);
                float newValue = ApplyOperator(oldValue);
                SetStat(m, newValue);
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
                case AuraOperator.raiseTo: return (oldValue < value) ? value: oldValue;
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

        public override void Apply(GameState state, Permanent context, Point pos)
        {
        }
    }

    public enum TargetType
    {
        none,
        empty,
        path,
        friend,
        enemy,
        minion,
        ongoing
    }

    public class Card
    {
        public string name;
        public Texture2D image;
        public Texture2D frameTexture;
        public Texture2D smallFrameTexture;
        public Color frameColor;
        public TargetType targetType;
        public PermanentType ongoingType;
        public List<ResourceAmount> cost;
        public Effect_Base effect;
        public List<string> description;

        public Card(JSONTable template, ContentManager content)
        {
            name = template.getString("name");
            cost = ResourceAmount.createList(template.getJSON("cost", null));
            effect = Effect_Base.create(template.getArray("effect", null));
            if (template.hasKey("ongoing"))
            {
                ongoingType = new PermanentType(template, content);
            }
            image = content.Load<Texture2D>(template.getString("texture"));
            smallFrameTexture = content.Load<Texture2D>("square");
            Enum.TryParse<TargetType>(template.getString("target", "none"), out targetType);
            frameTexture = content.Load<Texture2D>("cardframe_large");
            description = LayeredImageGfx.Tooltip.StringToLines(template.getString("description", ""), Game1.font, 100);
            switch (template.getString("type", null))
            {
                case "special":
                    frameColor = new Color(235, 200, 255);
                    break;
                case "minion":
                    frameColor = new Color(200, 255, 200);
                    break;
                case "production":
                    frameColor = new Color(255, 220, 190);
                    break;
                default:
                    frameColor = Color.White;
                    break;
            }
        }

        public static List<Card> load(JSONArray template, ContentManager content)
        {
            List<Card> cards = new List<Card>();
            for (int Idx = 0; Idx < template.Length; ++Idx)
            {
                cards.Add(new Card(template.getJSON(Idx), content));
            }
            return cards;
        }
    }
}
