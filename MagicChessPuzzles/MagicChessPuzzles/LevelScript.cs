using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace MagicChessPuzzles
{
    class LevelType
    {
        public readonly Point levelSize;
        public readonly List<Point> spawnPoint;
        public readonly Point wizardPos;
        public readonly List<ResourceAmount> startingResources;
        public readonly Texture2D floorTexture;
        public readonly Texture2D pathTexture;

        public static Dictionary<string, LevelType> levelTypes;

        public LevelType(JSONTable template, ContentManager content)
        {
            spawnPoint = new List<Point>();
            foreach(JSONArray spawnTemplate in template.getArray("spawnPoint").asJSONArrays())
            {
                spawnPoint.Add(spawnTemplate.toPoint());
            }
            wizardPos = template.getArray("wizardPos").toPoint();
            levelSize = template.getArray("levelSize").toPoint();
            startingResources = ResourceAmount.createList(template.getJSON("startingResources"));
            floorTexture = content.Load<Texture2D>(template.getString("floorTexture"));
            pathTexture = content.Load<Texture2D>(template.getString("pathTexture"));
        }

        public void InitState(GameState gameState)
        {
            gameState.GainResources(startingResources);
            gameState.CreateMinions(MinionType.get("wizard"), false, wizardPos);
        }

        public static void load(JSONTable template, ContentManager content)
        {
            levelTypes = new Dictionary<string, LevelType>();
            foreach (string key in template.Keys)
            {
                levelTypes[key] = new LevelType(template.getJSON(key), content);
            }
        }

        public static LevelType get(string name)
        {
            return levelTypes[name];
        }
    }

    public class LevelScript
    {
        List<List<MinionType>> spawns;
        LevelType levelType;

        public Point levelSize { get { return levelType.levelSize; } }
        public List<Point> spawnPoint { get { return levelType.spawnPoint; } }
        public Point wizardPos { get { return levelType.wizardPos; } }
        public List<KeyValuePair<MinionType, Point>> scenery;

        public LevelScript(JSONTable template)
        {
            spawns = new List<List<MinionType>>();
            JSONArray spawnTemplate = template.getArray("monsters");
            for (int Idx = 0; Idx < spawnTemplate.Length; ++Idx)
            {
                List<MinionType> thisTurn = new List<MinionType>();
                foreach (string minion in spawnTemplate.getArray(Idx).asStrings())
                {
                    MinionType type = MinionType.get(minion);
                    thisTurn.Add(type);
                }
                spawns.Add(thisTurn);
            }

            scenery = new List<KeyValuePair<MinionType, Point>>();
            foreach (JSONArray entry in template.getArray("scenery", JSONArray.empty).asJSONArrays())
            {
                scenery.Add(new KeyValuePair<MinionType,Point>(MinionType.get(entry.getString(0)), new Point(entry.getInt(1), entry.getInt(2))));
            }

            levelType = LevelType.get(template.getString("type"));
        }

        public static List<LevelScript> load(JSONArray levels)
        {
            List<LevelScript> levelScripts = new List<LevelScript>();
            foreach (JSONTable scriptTemplate in levels.asJSONTables())
            {
                levelScripts.Add(new LevelScript(scriptTemplate));
            }
            return levelScripts;
        }

        public void InitState(GameState gameState)
        {
            levelType.InitState(gameState);
            foreach (KeyValuePair<MinionType, Point> kv in scenery)
            {
                gameState.CreateMinions(kv.Key, false, kv.Value);
            }
        }

        public bool FinishedSpawning(GameState gameState)
        {
            return (spawns.Count+1 < gameState.turnNumber);
        }

        public void Apply(GameState gameState, MinionAnimationBatch moveAnim)
        {
            for (int Idx = 0; Idx < spawns.Count; ++Idx)
            {
                int index = gameState.spawnIndices[Idx];
                if (index < spawns[Idx].Count)
                {
                    MinionType spawntype = spawns[Idx][index];
                    if (spawntype != null)
                    {
                        if (gameState.getPermanentAt(levelType.spawnPoint[Idx]) == null)
                        {
                            gameState.CreateEnemy(spawntype, levelType.spawnPoint[Idx], moveAnim);
                            gameState.spawnIndices[Idx]++;
                        }
                    }
                    else
                    {
                        gameState.spawnIndices[Idx]++;
                    }
                }
            }
        }

        public bool Blocks(Point position, TargetType targetType)
        {
            if (position.X < 0 || position.Y < 0 | position.X >= levelType.levelSize.X || position.Y >= levelType.levelSize.Y)
                return true;

            switch (targetType)
            {
                case TargetType.empty:
                    return position.X >= levelType.spawnPoint[0].X;
                default:
                    return false;
            }
        }

        public void DrawBackground(SpriteBatch spriteBatch)
        {
            for (int X = 0; X < levelType.levelSize.X; X++)
            {
                for (int Y = 0; Y < levelType.levelSize.Y; Y++)
                {
                    spriteBatch.Draw(levelType.floorTexture, GameState.GridToScreenPos(new Point(X, Y)), Color.White);
                }
            }
            for (int X = 0; X < levelType.levelSize.X; X++)
            {
                foreach (Point pos in levelType.spawnPoint)
                {
                    spriteBatch.Draw(levelType.pathTexture, GameState.GridToScreenPos(new Point(X, pos.Y)), Color.White);
                }
            }

            Point mouseOverPos = GameState.ScreenToGridPos(Game1.inputState.MousePos);
            if (mouseOverPos.X >= 0 && mouseOverPos.Y >= 0 && mouseOverPos.X < levelType.levelSize.X && mouseOverPos.Y < levelType.levelSize.Y)
            {
                spriteBatch.Draw(Game1.highlightSquare, GameState.GridToScreenPos(mouseOverPos), Color.Yellow);
            }
        }
    }
}
