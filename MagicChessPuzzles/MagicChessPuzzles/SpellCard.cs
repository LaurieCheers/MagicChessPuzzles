using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public enum Range
    {
        square,
        adjacent,
        nearby,
        knight,
        global
    };

    public enum TargetType
    {
        none,
        empty,
        path,
        friend,
        enemy,
        minion,
        ongoing,
        numeric_spell,
        area_spell,
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
        public List<Card> upgrades;
        public List<ResourceAmount> upgradeCost;

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
            upgradeCost = ResourceAmount.createList(template.getJSON("upgradeCost", null));
            
            foreach (JSONTable upgradeTemplate in template.getArray("upgrades", JSONArray.empty).asJSONTables())
            {
                if(upgrades == null)
                    upgrades = new List<Card>();

                upgrades.Add(new Card(upgradeTemplate, content));
            }

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
                case "modifier":
                    frameColor = new Color(255, 190, 200);
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

        public bool HasANumber() { if (effect == null) return false; else return effect.HasANumber(); }
        public bool HasAnArea() { if (effect == null) return false; else return effect.HasAnArea(); }
    }
}
