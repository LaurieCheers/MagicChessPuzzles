using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public class ResourceType
    {
        public string name;
        public Texture2D texture;

        public ResourceType(JSONTable template, ContentManager content)
        {
            name = template.getString("name");

            string textureName = template.getString("texture");
            texture = (textureName != null) ? content.Load<Texture2D>(textureName) : null;
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
}
