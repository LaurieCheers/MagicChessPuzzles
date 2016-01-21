using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using DragonGfx;

namespace MagicChessPuzzles
{
    public enum Range
    {
        square,
        adjacent,
        nearby,
        knight,
        ahead,
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
        public bool unlocked;
        public bool defaultUnlocked;
        public TriggerItemTest targetTest;
        public string spellSet;

        static Dictionary<string, Card> cardsById = new Dictionary<string, Card>();

        public Card(JSONTable template, ContentManager content)
        {
            name = template.getString("name");
            cost = ResourceAmount.createList(template.getJSON("cost", null));
            effect = Effect_Base.create(template.getArray("effect", null));
            if (template.hasKey("ongoing") || template.hasKey("triggers"))
            {
                ongoingType = new PermanentType(template, content);
            }
            image = content.Load<Texture2D>(template.getString("texture"));
            smallFrameTexture = content.Load<Texture2D>("square");
            Enum.TryParse<TargetType>(template.getString("target", "none"), out targetType);
            frameTexture = content.Load<Texture2D>("cardframe_large");
            description = DragonGfx.Tooltip.StringToLines(template.getString("description", ""), Game1.font, 100);
            upgradeCost = ResourceAmount.createList(template.getJSON("upgradeCost", null));
            targetTest = TriggerItemTest.create(template.getArray("targetTest", null));
            spellSet = template.getString("spellSet", null);
            
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

            string id = template.getString("id", null);
            if (id != null)
            {
                cardsById.Add(id, this);
            }

            defaultUnlocked = template.getBool("unlocked", false);
            unlocked = defaultUnlocked;
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

        public static Card get(string id)
        {
            return cardsById[id];
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle frameRect, CardState state, bool selected)
        {
            Color offwhite = new Color(240, 240, 240);

            if (state == CardState.Playable || state == CardState.Targetable)
                Game1.activeCardBG.Draw(spriteBatch, frameRect, selected ? frameColor: frameColor.Multiply(offwhite));
            else if(state == CardState.BeingPlayed)
                Game1.activeCardBG.Draw(spriteBatch, frameRect, frameColor);
            else
                spriteBatch.Draw(frameTexture, frameRect, Color.Gray);

            DrawIcon(spriteBatch, image, new Vector2(frameRect.Left, frameRect.Top), state);

            Color textColor;
            if(state == CardState.BeingPlayed)
            {
                textColor = Color.Black;
            }
            else if(selected)
            {
                textColor = (state == CardState.Playable||state == CardState.Targetable)? Color.Yellow : Color.Red;
            }
            else
            {
                textColor = Color.Black;
            }
            spriteBatch.DrawString(Game1.font, name, new Vector2(frameRect.Left + image.Width, frameRect.Top), textColor);
            ResourceAmount.Draw(spriteBatch, cost, new Vector2(frameRect.Left + image.Width, frameRect.Top + 15));
        }

        void DrawIcon(SpriteBatch spriteBatch, Texture2D icon, Vector2 pos, CardState state)
        {
            Rectangle iconRect = new Rectangle((int)pos.X, (int)(pos.Y + 32.0f - icon.Height), icon.Width, icon.Height);
            spriteBatch.Draw(icon, iconRect, (state == CardState.Playable || state == CardState.Targetable || state == CardState.BeingPlayed) ? Color.White : Color.Gray);
        }

        public bool HasANumber() { if (effect == null) return false; else return effect.HasANumber(); }
        public bool HasAnArea() { if (effect == null) return false; else return effect.HasAnArea(); }
    }
}
