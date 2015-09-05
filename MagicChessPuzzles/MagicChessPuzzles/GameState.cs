using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public abstract class Permanent
    {
        public PermanentType type;
        public Point position;
        public bool isEnemy;

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

        public Vector2 screenPos
        {
            get { return GameState.GridToScreenPos(position); }
        }

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract void DrawMouseOver(SpriteBatch spriteBatch);
        public abstract Permanent Clone();
        
        public virtual void ResetTemporaryEffects()
        {
        }

        public virtual void WhenDies(GameState gameState, Point position)
        {
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

        public void ApplyOngoingEffects(GameState gameState)
        {
            gameState.ApplyEffect(type.ongoing,this,position);
        }

        public void ApplyOngoingLateEffects(GameState gameState)
        {
            gameState.ApplyEffect(type.ongoing_late, this, position);
        }
    }

    class Ongoing: Permanent
    {
        Card baseCard;

        public Ongoing(Card baseCard, Point p): base(baseCard.ongoingType, p, false)
        {
            this.baseCard = baseCard;
        }

        public Ongoing(Ongoing basis): base(basis)
        {
            this.baseCard = basis.baseCard;
        }

        public override Permanent Clone()
        {
            return new Ongoing(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(baseCard.image, screenPos, Color.White);
//            spriteBatch.DrawString(Game1.font, baseCard.name, screenPos, Color.White);
        }

        public override void DrawMouseOver(SpriteBatch spriteBatch)
        {

        }
    }

    public class GameState
    {
        public enum GameEndState
        {
            GameRunning,
            GameOver,
            GameWon,
        }

        GameState parentState;
        public int turnNumber;
        public GameEndState gameEndState;
        Dictionary<Point, Permanent> permanents;
        Dictionary<ResourceType, int> resources;
        HashSet<Card> playableCards;
        LevelScript levelScript;
        List<Point> killed = new List<Point>();

        public GameState(LevelScript levelScript)
        {
            this.levelScript = levelScript;

            parentState = null;
            permanents = new Dictionary<Point, Permanent>();
            resources = new Dictionary<ResourceType, int>();
            turnNumber = 1;
            playableCards = new HashSet<Card>();
            gameEndState = GameEndState.GameRunning;

            levelScript.InitState(this);
        }

        public GameState(GameState parentState)
        {
            this.gameEndState = parentState.gameEndState;
            this.parentState = parentState;
            this.levelScript = parentState.levelScript;
            turnNumber = parentState.turnNumber;

            permanents = new Dictionary<Point, Permanent>();
            foreach (KeyValuePair<Point, Permanent> p in parentState.permanents)
            {
                if (p.Value != null)
                {
                    Permanent newP = p.Value.Clone();
                    permanents[newP.position] = newP;
                }
            }

            resources = new Dictionary<ResourceType, int>(parentState.resources);
            playableCards = new HashSet<Card>();
        }

        public bool WonLevel()
        {
            if (!levelScript.FinishedSpawning(this))
                return false;

            Point levelSize = levelScript.levelSize;
            for (int x = 0; x < levelSize.X; x++)
            {
                for (int y = levelSize.Y - 1; y >= 0; y--)
                {
                    Minion m = getMinionAt(new Point(x, y));
                    if (m != null && m.isEnemy)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void LoseGame()
        {
            gameEndState = GameEndState.GameOver;
        }

        public void AddKilled(Point pos)
        {
            killed.Add(pos);
        }

        public bool CanPlayCard(Card card, Point caster, Point position)
        {
            if (permanents.ContainsKey(position))
            {
                Permanent p = permanents[position];

                switch (card.targetType)
                {
                    case TargetType.empty:
                    case TargetType.path:
                        return false;
                    case TargetType.friend:
                        if (permanents[position].isEnemy) return false;
                        break;
                    case TargetType.enemy:
                        if (!permanents[position].isEnemy) return false;
                        break;
                }
            }
            else
            {
                switch (card.targetType)
                {
                    case TargetType.enemy:
                        return false;
                }
            }

            if (levelScript.Blocks(position, card.targetType))
                return false;

            return CanPlayCard(card);
        }

        public bool CanPlayCard(Card card)
        {
            if (gameEndState == GameEndState.GameOver && (card.effect == null || !(card.effect is Effect_Rewind)))
            {
                return false;
            }

            if (playableCards.Contains(card))
            {
                return true;
            }

            if (card.ongoingType != null)
            {
                if (!CanPayCost(CombineCosts(card.cost, card.ongoingType.upkeep)))
                {
                    return false;
                }
            }
            else
            {
                if (!CanPayCost(card.cost))
                {
                    return false;
                }
            }

            // can't rewind on turn 1
            if (parentState == null && card.effect != null && card.effect is Effect_Rewind)
            {
                return false;
            }

            playableCards.Add(card);
            return true;
        }

        public Dictionary<ResourceType, int> CombineCosts(List<ResourceAmount> cost1, List<ResourceAmount> cost2)
        {
            Dictionary<ResourceType, int> result = new Dictionary<ResourceType, int>();
            foreach (ResourceAmount ra in cost1)
            {
                result[ra.type] = ra.amount;
            }
            foreach (ResourceAmount ra in cost2)
            {
                if (result.ContainsKey(ra.type))
                {
                    result[ra.type] += ra.amount;
                }
                else
                {
                    result[ra.type] = ra.amount;
                }
            }
            return result;
        }

        public bool CanPayCost(Dictionary<ResourceType, int> cost)
        {
            foreach (KeyValuePair<ResourceType, int> kv in cost)
            {
                if (!resources.ContainsKey(kv.Key) || resources[kv.Key] < kv.Value)
                    return false;
            }

            return true;
        }

        public bool CanPayCost(List<ResourceAmount> cost)
        {
            foreach (ResourceAmount costRa in cost)
            {
                if (!resources.ContainsKey(costRa.type) || resources[costRa.type] < costRa.amount)
                    return false;
            }

            return true;
        }

        public void PayCost(List<ResourceAmount> cost)
        {
            foreach (ResourceAmount costRa in cost)
            {
                if (!resources.ContainsKey(costRa.type) || resources[costRa.type] < costRa.amount)
                    throw new InvalidCastException();

                resources[costRa.type] -= costRa.amount;
            }
        }

        public void PlayCard(Card c, Point caster, Point p)
        {
            PayCost(c.cost);
            ApplyEffect(c.effect, getMinionAt(caster), p);
            if (c.ongoingType != null)
            {
                permanents[p] = new Ongoing(c,p);
            }
            TurnEffects();
        }

        public void TurnEffects()
        {
            List<Permanent> savedPermanents = new List<Permanent>(permanents.Values);
            foreach (Permanent p in savedPermanents)
            {
                p.ResetTemporaryEffects();
            }

            foreach (Permanent p in savedPermanents)
            {
                p.ApplyOngoingEffects(this);
            }

            foreach(KeyValuePair<Point, Permanent> entry in permanents )
            {
                if (!entry.Value.TryPayUpkeep(this))
                {
                    AddKilled(entry.Key);
                }
            }

            MinionsAttack();
            CleanUpKilled();
            MoveEnemies();

            var newPermanents = permanents.Values.ToList();
            foreach (Permanent p in newPermanents)
            {
                p.ApplyOngoingLateEffects(this);
            }

            levelScript.Apply(this);

            if (WonLevel())
            {
                gameEndState = GameEndState.GameWon;
            }

            turnNumber++;
        }

        public void CleanUpKilled()
        {
            List<KeyValuePair<Permanent, Point>> deadPermanents = new List<KeyValuePair<Permanent, Point>>();
            foreach (Point pos in killed)
            {
                Permanent p = permanents[pos];
                permanents.Remove(pos);
                deadPermanents.Add(new KeyValuePair<Permanent, Point>(p, pos));
            }

            foreach (KeyValuePair<Permanent, Point> kv in deadPermanents)
            {
                kv.Key.WhenDies(this, kv.Value);
            }

            killed.Clear();
        }

        public void MoveEnemies()
        {
            // enemies walk forward
            Point levelSize = levelScript.levelSize;
            for (int x = 0; x < levelSize.X; x++)
            {
                for (int y = levelSize.Y - 1; y >= 0; y--)
                {
                    Point position = new Point(x, y);
                    if (!permanents.ContainsKey(position))
                        continue;

                    Permanent p = permanents[position];
                    if (p is Minion && ((Minion)p).isEnemy)
                    {
                        Minion m = (Minion)p;
                        if (m.TakeAStep())
                        {
                            TryMove(m, new Point(x-1, y));
                        }
                    }
                }
            }
        }

        public void MinionsAttack()
        {
            for (int y = levelScript.levelSize.Y-1; y >= 0; y--)
            {
                for (int x = 0; x < levelScript.levelSize.X; x++)
                {
                    Minion minion = getMinionAt(new Point(x, y));
                    if(minion != null)
                        minion.CheckAttacks(this);
                }
            }
        }

        public Permanent getPermanentAt(Point pos)
        {
            if (!permanents.ContainsKey(pos))
                return null;

            return permanents[pos];
        }

        public Minion getMinionAt(Point pos)
        {
            Permanent p = getPermanentAt(pos);
            if (p == null || !(p is Minion))
            {
                return null;
            }

            return (Minion)p;
        }

        public List<Minion> getMinionsAt(Point basePos, Point[] offsets)
        {
            List<Minion> result = new List<Minion>();
            foreach (Point offset in offsets)
            {
                Point currentPos = new Point(basePos.X + offset.X, basePos.Y + offset.Y);
                if (permanents.ContainsKey(currentPos))
                {
                    Permanent found = permanents[currentPos];
                    if (found is Minion)
                    {
                        result.Add((Minion)found);
                    }
                }
            }
            return result;
        }

        public List<Minion> getMinionsInRange(Range range, Point pos)
        {
            return getMinionsAt(pos, getOffsetsForRange(range));
        }

        public Point[] getOffsetsForRange(Range range)
        {
            switch (range)
            {
                case Range.self:
                    return new Point[]{new Point(0,0)};
                case Range.adjacent:
                    return adjacentOffsets;
                case Range.nearby:
                    return nearbyOffsets;
                case Range.knight:
                    return knightRangeOffsets;
                default:
                    throw new ArgumentException("Unknown Range value");
            }
        }

        public static readonly Point[] adjacentOffsets = new Point[]
        {
            new Point(-1,0), new Point(1,0),
            new Point(0,-1), new Point(0,1)
        };
        
        public static readonly Point[] nearbyOffsets = new Point[]
        {
            new Point(-1, -1), new Point(-1,0), new Point(-1, 1),
            new Point(0,-1), new Point(0, 1),
            new Point(1, -1), new Point(1, 0), new Point(1, 1)
        };

        public static readonly Point[] knightRangeOffsets = new Point[]
        {
            new Point(-1,0), new Point(1,0),
            new Point(-2, -1), new Point(-2,0), new Point(-2,1),
            new Point(-1, -1), new Point(-1, 1), new Point(-1, -2), new Point(-1,2),
            new Point(0,-1), new Point(0,1), new Point(0,-2), new Point(0,2),
            new Point(1,-1), new Point(1, 1), new Point(1,-2), new Point(1,2),
            new Point(2,-1), new Point(2,0), new Point(2,1),
        };

        public bool TryMove(Permanent p, Point moveTo)
        {
            if( !permanents.ContainsKey(moveTo))
            {
                permanents.Remove(p.position);
                permanents[moveTo] = p;
                p.position = moveTo;
                return true;
            }
            return false;
        }

        public void ApplyEffect(Effect_Base effect, Permanent context, Point pos)
        {
            if( effect != null )
                effect.Apply(this,context,pos);
        }

        public void GainResources(List<ResourceAmount> resourceList)
        {
            foreach (ResourceAmount resource in resourceList)
            {
                if (resources.ContainsKey(resource.type))
                {
                    resources[resource.type] += resource.amount;
                }
                else
                {
                    resources[resource.type] = resource.amount;
                }
            }
        }

        public void CreateMinions(MinionType type, bool isEnemy, Point p)
        {
            if( !permanents.ContainsKey(p) )
                permanents[p] = new Minion(type, p, isEnemy);
        }

        public void CreateEnemy(MinionType type, Point spawnPoint)
        {
            permanents[spawnPoint] = new Minion(type, spawnPoint, true);
        }

        public void Draw(SpriteBatch spriteBatch, GameState gameStateOnSkip)
        {
            levelScript.DrawBackground(spriteBatch);

            spriteBatch.DrawString(Game1.font, "Turn " + turnNumber, new Vector2(200, 20), Color.White);
            DrawResources(spriteBatch, resources, gameStateOnSkip.resources, new Vector2(250, 20));

            for (int X = 0; X < levelScript.levelSize.X; X++)
            {
                for (int Y = 0; Y < levelScript.levelSize.Y; Y++)
                {
                    Point pos = new Point(X, Y);
                    if(permanents.ContainsKey(pos))
                        permanents[pos].Draw(spriteBatch);
                }
            }
        }

        public void DrawTargetCursor(SpriteBatch spriteBatch, Card playing, Point caster, Vector2 mousePos)
        {
            Point p = ScreenToGridPos(mousePos);
            bool canPlay = CanPlayCard(playing, caster, p);
            spriteBatch.Draw(Game1.gridCursor, GridToScreenPos(p), canPlay? Color.LightGreen: Color.Red);
        }

        public static void DrawResources(SpriteBatch spriteBatch, Dictionary<ResourceType, int> resources, Dictionary<ResourceType, int> resourcesOnSkip, Vector2 position)
        {
            int currentX = (int)position.X;
            int resourceIconSize = 16;

            foreach (KeyValuePair<ResourceType, int> ra in resources)
            {
                spriteBatch.Draw(ra.Key.texture, new Rectangle(currentX, (int)position.Y, resourceIconSize, resourceIconSize), Color.White);
                currentX += resourceIconSize + 2;
                string amountLabel = "" + ra.Value;
                spriteBatch.DrawString(Game1.font, amountLabel, new Vector2(currentX, (int)position.Y), Color.Black);

                int nextAmountDelta;
                if (resourcesOnSkip.ContainsKey(ra.Key))
                    nextAmountDelta = resourcesOnSkip[ra.Key] - ra.Value;
                else
                    nextAmountDelta = -ra.Value;

                if (nextAmountDelta > 0)
                    spriteBatch.DrawString(Game1.font, "+" + nextAmountDelta, new Vector2(currentX-10, (int)position.Y + 15), Color.Green);
                else if (nextAmountDelta < 0)
                    spriteBatch.DrawString(Game1.font, "" + nextAmountDelta, new Vector2(currentX-10, (int)position.Y + 15), Color.Red);

                currentX += (int)Game1.font.MeasureString(amountLabel).X + 6;
            }
        }

        public void DrawMouseOver(SpriteBatch spriteBatch, Vector2 mousePos)
        {
            Point gridPos = ScreenToGridPos(mousePos);
            if (permanents.ContainsKey(gridPos))
            {
                permanents[gridPos].DrawMouseOver(spriteBatch);
            }
        }

        public GameState GetGameStateOnSkip()
        {
            GameState result = new GameState(this);
            result.TurnEffects();
            return result;
        }

        const int gridSize = 32;
        static Vector2 gridOrigin = new Vector2(200,100);

        public static Vector2 GridToScreenPos(Point position)
        {
            return new Vector2(gridOrigin.X + position.X*gridSize, gridOrigin.Y+position.Y*gridSize);
        }

        public static Point ScreenToGridPos(Vector2 screenPos)
        {
            return new Point((int)((screenPos.X - gridOrigin.X)/gridSize), (int)((screenPos.Y - gridOrigin.Y)/gridSize));
        }
    }
}
