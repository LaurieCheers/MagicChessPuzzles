using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
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
        Dictionary<Card, TextChanges> cardTextChanges;
        LevelScript levelScript;
        List<Point> killed = new List<Point>();
        List<WaitingTrigger> triggersResolving = new List<WaitingTrigger>();
        List<WaitingTrigger> triggersWaiting = new List<WaitingTrigger>();
        public List<int> spawnIndices;

        public GameState(LevelScript levelScript)
        {
            this.levelScript = levelScript;
            spawnIndices = new List<int>();
            for (int Idx = this.levelScript.spawnPoint.Count; Idx > 0; --Idx)
            {
                spawnIndices.Add(0);
            }

            parentState = null;
            permanents = new Dictionary<Point, Permanent>();
            resources = new Dictionary<ResourceType, int>();
            turnNumber = 1;
            playableCards = new HashSet<Card>();
            gameEndState = GameEndState.GameRunning;
            cardTextChanges = new Dictionary<Card, TextChanges>();

            levelScript.InitState(this);
        }

        public GameState(GameState parentState)
        {
            this.gameEndState = parentState.gameEndState;
            this.parentState = parentState;
            this.levelScript = parentState.levelScript;
            turnNumber = parentState.turnNumber;

            spawnIndices = new List<int>();
            foreach (int spawnIndex in parentState.spawnIndices)
            {
                spawnIndices.Add(spawnIndex);
            }
            
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
            cardTextChanges = new Dictionary<Card, TextChanges>();
            foreach (KeyValuePair<Card, TextChanges> kv in parentState.cardTextChanges)
            {
                cardTextChanges.Add(kv.Key, kv.Value);
            }
            playableCards = new HashSet<Card>();
        }

        public void AddTextChange(Card c, TextChanges.TextChange<int> change)
        {
            if (cardTextChanges.ContainsKey(c))
            {
                cardTextChanges[c] = new TextChanges(cardTextChanges[c], change);
            }
            else
            {
                cardTextChanges[c] = new TextChanges(change);
            }
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

        public void Destroyed(Point pos, Permanent killer)
        {
            killed.Add(pos);
            getPermanentAt(pos).killedBy = killer;
        }

        public bool CanPlayCard(Card card, Point caster, TriggerItem target)
        {
            switch (card.targetType)
            {
                case TargetType.numeric_spell:
                    if (!target.card.HasANumber())
                        return false;
                    break;
                case TargetType.area_spell:
                    if (!target.card.HasAnArea())
                        return false;
                    break;
                case TargetType.empty:
                case TargetType.path:
                    if(target.permanent != null)
                        return false;
                    break;
                case TargetType.minion:
                    if (target.permanent == null)
                        return false;
                    break;
                case TargetType.friend:
                    if (target.permanent == null || target.permanent.isEnemy)
                        return false;
                    break;
                case TargetType.enemy:
                    if (target.permanent == null)
                    {
                        return false;
                    }
                    else
                    {
                        if (!target.permanent.isEnemy)
                            return false;
                    }
                    break;
            }

            if (target.card == null && levelScript.Blocks(target.position, card.targetType))
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

        public TextChanges getTextChanges(Card c)
        {
            if (cardTextChanges.ContainsKey(c))
                return cardTextChanges[c];
            else
                return TextChanges.none;
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

        public void PlayCard(Card c, Point caster, TriggerItem target, MinionAnimationSequence animation)
        {
            PayCost(c.cost);
            TextChanges changes = null;
            if (cardTextChanges.ContainsKey(c))
                changes = cardTextChanges[c];

            ApplyEffect(c.effect, new EffectContext(this, changes, getPermanentAt(caster), target, null, animation));

            if (c.ongoingType != null)
            {
                Point pos = target.position;
                permanents[pos] = new Ongoing(c, pos);
            }

            TurnEffects(animation);
        }

        public void TurnEffects(MinionAnimationSequence animation)
        {
            CleanUp(animation);

            List<Permanent> savedPermanents = new List<Permanent>(permanents.Values);
            foreach (Permanent p in savedPermanents)
            {
                p.ResetTemporaryEffects();
            }

            foreach (Permanent p in savedPermanents)
            {
                p.ApplyOngoingEffects(this, animation);
            }

            foreach(KeyValuePair<Point, Permanent> entry in permanents )
            {
                if (!entry.Value.TryPayUpkeep(this))
                {
                    Destroyed(entry.Key, entry.Value);
                }
            }

            MinionsAttack(animation);
            CleanUp(animation);

            MoveEnemies(animation);

            var finalPermanents = permanents.Values.ToList();
            foreach (Permanent p in finalPermanents)
            {
                p.ApplyOngoingLateEffects(this, animation);
            }

            CleanUp(animation);

            if (gameEndState == GameEndState.GameRunning && WonLevel())
            {
                gameEndState = GameEndState.GameWon;
            }

            turnNumber++;
        }

        public void CleanUp(MinionAnimationSequence animation)
        {
            DeadMinionsDie();
            ResolveWaitingTriggers(animation);
        }

        public void DeadMinionsDie()
        {
            foreach (Point pos in killed)
            {
                Minion m = getMinionAt(pos);
                HandleTriggers(new TriggerEvent(TriggerType.onKill, m.killedBy, m));
            }

            List<KeyValuePair<Permanent, Point>> deadPermanents = new List<KeyValuePair<Permanent, Point>>();
            foreach (Point pos in killed)
            {
                Permanent p = permanents[pos];
                p.deleted = true;
                permanents.Remove(pos);
                deadPermanents.Add(new KeyValuePair<Permanent, Point>(p, pos));
            }

            killed.Clear();
        }

        public void ResolveWaitingTriggers(MinionAnimationSequence animation)
        {
            int loopCount = 0;
            while(triggersWaiting.Count > 0)
            {
                loopCount++;
                triggersResolving = triggersWaiting;
                triggersWaiting = new List<WaitingTrigger>();

                bool hasAttackTriggers = false;
                foreach(WaitingTrigger trigger in triggersResolving)
                {
                    if (trigger.ability.isAttackTrigger)
                        hasAttackTriggers = true;
                    else
                        trigger.ability.Apply(new EffectContext(this, null, trigger.permanent, TriggerItem.create(trigger.position), trigger.evt, animation));
                }

                if (hasAttackTriggers)
                {
                    MinionAnimationBatch attackAnim = animation.AddBatch(new GameState(this), Game1.ATTACK_ANIM_DURATION);
                    MinionAnimationBatch recoverAnim = animation.AddBatch(this, Game1.RECOVER_ANIM_DURATION);
                    foreach (WaitingTrigger trigger in triggersResolving)
                    {
                        if (trigger.ability.isAttackTrigger)
                            trigger.ability.Apply(new EffectContext(this, trigger.permanent, trigger.position, trigger.evt, attackAnim, recoverAnim, animation));
                    }
                    recoverAnim.SetInitialGameState(new GameState(this));
                }

                DeadMinionsDie();

                if (loopCount > 100)
                {
                    triggersWaiting.Clear();
                }
            }

            triggersResolving.Clear();
        }

        class MinionMove
        {
            public Minion minion;
            public bool moveSpent;
            public int movesLeft;
            public Vector2 currentAnimPos;
            public Point moveTo { get { return new Point(minion.position.X - 1, minion.position.Y); } }
            public MinionMove(Minion m)
            {
                this.currentAnimPos = m.drawPos;
                this.minion = m;
                this.movesLeft = m.stats.move;
            }
        }

        public void MoveEnemies(MinionAnimationSequence animation)
        {
            List<MinionMove> minionMoves = new List<MinionMove>();
            HashSet<Minion> minionsStillMoving = new HashSet<Minion>();

            MinionAnimationBatch stepBatch = animation.AddBatch(this, Game1.WALK_ANIM_DURATION);
            // enemies walk forward
            Point levelSize = levelScript.levelSize;
            for (int x = 0; x < levelSize.X; x++) //NB processing these in ascending X order - this matters
            {
                for (int y = levelSize.Y - 1; y >= 0; y--)
                {
                    Point position = new Point(x, y);
                    if (!permanents.ContainsKey(position))
                        continue;

                    Permanent p = permanents[position];

                    if (!(p is Minion))
                        continue;

                    Minion m = (Minion)p;
                    if(m.isEnemy && m.stats.move > 0)
                    {
                        HandleTriggers(new TriggerEvent(TriggerType.beforeMove, m));
                        
                        Vector2 oldDrawPos = m.drawPos;
                        if (m.stats.move == 1 && m.stats.hasKeyword(Keyword.slow) && !m.slow_movedHalfWay)
                        {
                            m.slow_movedHalfWay = true;
                            stepBatch.AddAnimation(m, oldDrawPos, m.drawPos);
                        }
                        else
                        {
                            MinionMove mmove = new MinionMove(m);
                            minionMoves.Add(mmove);
                            minionsStillMoving.Add(m);
                        }
                    }
                }
            }

            CleanUp(animation);

            bool hasMoreMoves = true;
            bool firstPass = true;
            while(hasMoreMoves)
            {
                foreach (MinionMove mmove in minionMoves)
                {
                    if (mmove.movesLeft <= 0)
                        continue;

                    if (levelScript.Blocks(mmove.moveTo, TargetType.empty))
                    {
                        mmove.movesLeft = 0;
                        mmove.moveSpent = true;
                        minionsStillMoving.Remove(mmove.minion);
                    }
                    else if (permanents.ContainsKey(mmove.moveTo) && !minionsStillMoving.Contains(getMinionAt(mmove.moveTo)))
                    {
                        // If I'm blocked:
                        if (mmove.minion.stats.hasKeyword(Keyword.unstoppable))
                        {
                            Destroyed(mmove.moveTo, mmove.minion);
                        }
                        mmove.moveSpent = true;
                    }
                }
                CleanUp(animation);

                hasMoreMoves = false;
                foreach (MinionMove mmove in minionMoves)
                {
                    if (mmove.movesLeft > 0)
                    {
                        if (TryMove(mmove, mmove.moveTo))
                        {
                            mmove.moveSpent = true;
                            stepBatch.AddAnimation(mmove.minion, mmove.currentAnimPos, mmove.minion.drawPos);
                            mmove.currentAnimPos = mmove.minion.drawPos;
                        }

                        if (mmove.moveSpent)
                        {
                            mmove.movesLeft--;
                            mmove.moveSpent = false;
                        }

                        if (mmove.movesLeft > 0)
                        {
                            hasMoreMoves = true;
                        }
                        else
                        {
                            minionsStillMoving.Remove(mmove.minion);
                        }
                    }
                }

                if (firstPass)
                {
                    levelScript.Apply(this, stepBatch);
                    firstPass = false;
                }
                stepBatch.SetInitialGameState(new GameState(this));

                stepBatch = animation.AddBatch(this, Game1.WALK_ANIM_DURATION);
            }
        }

        public void HandleTriggers(TriggerType eventType, Permanent source)
        {
            HandleTriggers(new TriggerEvent(eventType, source));
        }

        public void HandleTriggers(TriggerType eventType, Permanent source, Permanent obj)
        {
            HandleTriggers(new TriggerEvent(eventType, source, obj));
        }

        public void HandleTriggers(TriggerEvent evt)
        {
            foreach (KeyValuePair<Point, Permanent> kv in permanents)
            {
                kv.Value.CheckTriggers(evt, this, this.triggersWaiting);
            }
        }

        public void MinionsAttack(MinionAnimationSequence animation)
        {
            MinionAnimationBatch attack = null;
            MinionAnimationBatch recover = null;
            if (animation != null)
            {
                attack = animation.AddBatch(new GameState(this), Game1.ATTACK_ANIM_DURATION);
                recover = animation.AddBatch(this, Game1.RECOVER_ANIM_DURATION);
            }

            for (int y = levelScript.levelSize.Y - 1; y >= 0; y--)
            {
                for (int x = 0; x < levelScript.levelSize.X; x++)
                {
                    Permanent p = getPermanentAt(new Point(x, y));
                    if (p != null)
                    {
                        if( p is Minion )
                        {
                            ((Minion)p).CheckAttacks(this, attack, recover, animation);
                        }
                    }
                }
            }

            recover.SetInitialGameState(new GameState(this));
        }

        public TriggerItem Adapt(TriggerItem item)
        {
            if (item.card != null)
            {
                return item;
            }
            else
            {
                return getItemAt(item.position);
            }
        }

        public TriggerItem getItemAt(Point pos)
        {
            if (!permanents.ContainsKey(pos))
                return TriggerItem.create(pos);

            return TriggerItem.create(permanents[pos]);
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

        public List<Minion> getMinionsAt(Point[] positions)
        {
            List<Minion> result = new List<Minion>();
            foreach (Point currentPos in positions)
            {
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
            return getMinionsAt(getPositionsForRange(pos, range));
        }

        public Point[] getPositionsForRange(Point pos, Range range)
        {
            switch (range)
            {
                case Range.square:
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

        bool TryMove(MinionMove moveState, Point moveTo)
        {
            if (!permanents.ContainsKey(moveTo))
            {
                permanents.Remove(moveState.minion.position);
                permanents[moveTo] = moveState.minion;
                moveState.minion.position = moveTo;
                return true;
            }
            return false;
        }

        public void ApplyEffect(Effect_Base effect, EffectContext context)
        {
            if( effect != null )
                effect.Apply(context);
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
            if (!permanents.ContainsKey(p))
            {
                Minion spawned = new Minion(type, p, isEnemy);
                permanents[p] = spawned;
            }
        }

        public void CreateEnemy(MinionType type, Point spawnPoint, MinionAnimationBatch moveAnim)
        {
            Minion spawned = new Minion(type, spawnPoint, true);
            permanents[spawnPoint] = spawned;
            moveAnim.AddAnimation(spawned, spawned.drawPos + new Vector2(32.0f, 0.0f), spawned.drawPos);
        }

        public void Draw(SpriteBatch spriteBatch, GameState gameStateOnSkip, MinionAnimationBatch animation)
        {
            levelScript.DrawBackground(spriteBatch);

            spriteBatch.DrawString(Game1.font, "Turn " + turnNumber, new Vector2(200, 20), Color.White);
            DrawResources(spriteBatch, resources, gameStateOnSkip.resources, new Vector2(250, 20));

            for (int X = 0; X < levelScript.levelSize.X; X++)
            {
                for (int Y = 0; Y < levelScript.levelSize.Y; Y++)
                {
                    Point pos = new Point(X, Y);
                    if (permanents.ContainsKey(pos))
                        permanents[pos].Draw(spriteBatch, animation);
                }
            }
        }

        public void DrawTargetCursor(SpriteBatch spriteBatch, Card playing, Point caster, Vector2 mousePos)
        {
            Point p = ScreenToGridPos(mousePos);
            bool canPlay = CanPlayCard(playing, caster, getItemAt(p));
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
            result.TurnEffects(new MinionAnimationSequence());
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
