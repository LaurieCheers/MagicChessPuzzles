using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LayeredImageGfx;

namespace MagicChessPuzzles
{
    public class CardCatalog
    {
        Rectangle rect;
        int cardWidth;
        int cardHeight;
        int selectedCardIdx;
        bool selectedCardUpgrade;
        List<Card> cardList;
        Card choosingCardForCard;
        Card choosingPositionForCard;
        public Card playing;
        public Point playPosition;
        public Card playTargetCard;
        bool dynamic;
        public Point caster;
        public Dictionary<Card, int> upgrades;

        public CardCatalog(Rectangle rect, Dictionary<Card, int> upgrades)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = null;
            this.rect = rect;
            this.dynamic = true;
            this.upgrades = upgrades;
        }

        public CardCatalog(Rectangle rect, List<Card> cardList, Dictionary<Card, int> upgrades)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = cardList;
            this.rect = rect;
            this.dynamic = false;
            this.upgrades = upgrades;
        }

        public void ShowSpells(Point caster, string spellType)
        {
            this.caster = caster;
            this.cardList = Game1.spellBooks[spellType];
        }

        public void Update(GameState playState)
        {
            playing = null;
            playTargetCard = null;
            selectedCardUpgrade = false;

            if (cardList == null || !rect.Contains(Game1.inputState.MousePos))
            {
                selectedCardIdx = -1;
            }
            else
            {
                int cardIdx = 0;
                int cardBottom = rect.Top;

                foreach (Card baseCard in cardList)
                {
                    Card c = GetUpgrade(baseCard);

                    cardBottom += cardHeight;

                    if (Game1.inputState.MousePos.Y < cardBottom)
                    {
                        selectedCardIdx = cardIdx;
                        if ( Game1.inputState.MousePos.X > rect.Right - 16 && CanUpgrade(baseCard) )
                        {
                            selectedCardUpgrade = true;
                        }
                        break;
                    }
                    cardIdx++;
                }

                if (Game1.inputState.MousePos.Y > cardBottom)
                {
                    selectedCardIdx = -1;
                }
            }

            if (Game1.inputState.WasMouseLeftJustPressed())
            {
                if (choosingCardForCard != null)
                {
                    if (selectedCardIdx != -1)
                    {
                        playing = choosingCardForCard;
                        playTargetCard = cardList[selectedCardIdx];
                    }
                    choosingCardForCard = null;
                }
                else if (choosingPositionForCard != null)
                {
                    Point targetPos = GameState.ScreenToGridPos(Game1.inputState.MousePos);
                    if (playState.CanPlayCard(choosingPositionForCard, caster, playState.getItemAt(targetPos)))
                    {
                        playing = choosingPositionForCard;
                        playPosition = targetPos;
                        playTargetCard = null;
                    }
                    choosingPositionForCard = null;
                }
                else if (selectedCardIdx != -1)
                {
                    Card baseCardClicked = cardList[selectedCardIdx];
                    Card cardClicked = GetUpgrade(baseCardClicked);
                    if (selectedCardUpgrade)
                        AddUpgrade(cardClicked);
                    else if (cardClicked.targetType == TargetType.numeric_spell ||
                            cardClicked.targetType == TargetType.area_spell)
                        choosingCardForCard = cardClicked;
                    else if (cardClicked.targetType != TargetType.none)
                        choosingPositionForCard = cardClicked;
                    else
                        playing = cardClicked;
                }
                else if (dynamic)
                {
                    Minion clickedMinion = playState.getMinionAt(GameState.ScreenToGridPos(Game1.inputState.MousePos));
                    if (clickedMinion != null && clickedMinion.spells != null)
                    {
                        ShowSpells(clickedMinion.position, clickedMinion.spells);
                    }
                }
            }
        }

        public void AddUpgrade(Card c)
        {
            if (upgrades.ContainsKey(c))
            {
                upgrades[c]++;
            }
            else
            {
                upgrades[c] = 1;
            }
        }

        public bool CanUpgrade(Card c)
        {
            if (c.upgrades == null)
                return false;

            if (upgrades.ContainsKey(c))
            {
                return c.upgrades.Count > upgrades[c];
            }
            return true;
        }

        public Card GetUpgrade(Card c)
        {
            if (upgrades.ContainsKey(c))
            {
                return c.upgrades[upgrades[c]-1];
            }
            else
            {
                return c;
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameState playState)
        {
            int cardIdx = 0;
//            int selectSize = 5;
            Color offwhite = new Color(240,240,240);

            if (cardList == null)
                return;

            foreach (Card baseCard in cardList)
            {
                Card c = GetUpgrade(baseCard);
                bool canUpgrade = CanUpgrade(baseCard);

                bool canPlay;
                if (choosingCardForCard != null)
                {
                    canPlay = playState.CanPlayCard(choosingCardForCard, new Point(), TriggerItem.create(baseCard));
                }
                else
                {
                    canPlay = playState.CanPlayCard(c);
                }

                Rectangle frameRect = new Rectangle(rect.Left, rect.Top + cardIdx * cardHeight, c.frameTexture.Width - (canUpgrade ? 16 : 0), cardHeight);

                if( canPlay )
                    Game1.activeCardBG.Draw(spriteBatch, frameRect, (selectedCardIdx != cardIdx) ? c.frameColor.Multiply(offwhite) : c.frameColor);
                else
                    spriteBatch.Draw(c.frameTexture, frameRect, Color.Gray);

                DrawIcon(spriteBatch, c.image, new Vector2(rect.Left, rect.Top + cardIdx * cardHeight), canPlay);

                spriteBatch.DrawString(Game1.font, c.name, new Vector2(rect.Left + c.image.Width, rect.Top + cardIdx * cardHeight), (selectedCardIdx != cardIdx)? Color.Black: (canPlay? Color.Yellow: Color.Red));
                ResourceAmount.Draw(spriteBatch, c.cost, new Vector2(rect.Left + c.image.Width, rect.Top + cardIdx * cardHeight + 15));

                if (canUpgrade)
                {
                    Rectangle upgradeRect = new Rectangle(rect.Right-16, rect.Top + cardIdx * cardHeight, 16, cardHeight);
                    bool selectedThisUpgrade = selectedCardUpgrade && (selectedCardIdx == cardIdx);
                    spriteBatch.Draw(Game1.upgradeTexture, upgradeRect, selectedThisUpgrade? Color.Red: Color.White);
                }

/*                Rectangle cardRect = new Rectangle(rect.Left, rect.Top + cardIdx * cardHeight, cardWidth, cardHeight);
                if (selectedCardIdx != cardIdx)
                {
                    bool canPlay = playState.CanPlayCard(c);
                    spriteBatch.Draw(c.smallFrameTexture, cardRect, playState.CanPlayCard(c) ? c.frameColor : Color.Gray);
                    spriteBatch.Draw(c.image, cardRect, canPlay? Color.White: Color.Gray);
                    DrawIcon(spriteBatch, c.image, new Vector2(cardRect.X, cardRect.Y), canPlay);
                }*/
                cardIdx++;
            }

            if (selectedCardIdx != -1 && !selectedCardUpgrade)
            {
                Card baseSelectedCard = cardList[selectedCardIdx];
                Card selectedCard = GetUpgrade(baseSelectedCard);
                TextChanges changes = playState.getTextChanges(baseSelectedCard);
                Vector2 tooltipPos = new Vector2(rect.Left + selectedCard.frameTexture.Width, rect.Top + selectedCardIdx * cardHeight);
                Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, changes.Apply(selectedCard.description), tooltipPos);
            }

            if (choosingPositionForCard != null)
            {
                playState.DrawTargetCursor(spriteBatch, choosingPositionForCard, caster, Game1.inputState.MousePos);
            }
        }

        void DrawIcon(SpriteBatch spriteBatch, Texture2D icon, Vector2 pos, bool canPlay)
        {
            Rectangle iconRect = new Rectangle((int)pos.X, (int)(pos.Y + 32.0f - icon.Height), icon.Width, icon.Height);
            spriteBatch.Draw(icon, iconRect, canPlay ? Color.White : Color.Gray);
        }
    }
}
