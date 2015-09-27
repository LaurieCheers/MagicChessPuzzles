using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DragonGfx;

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
        public Dictionary<Card, int> upgrades;

        public CardCatalog(Rectangle rect, Dictionary<Card, int> upgrades)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = null;
            this.rect = rect;
            this.upgrades = upgrades;
        }

        public CardCatalog(Rectangle rect, List<Card> cardList, Dictionary<Card, int> upgrades)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = cardList;
            this.rect = rect;
            this.upgrades = upgrades;
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
                int cardListIdx = 0;
                int cardBottom = rect.Top;

                foreach (Card baseCard in cardList)
                {
                    if (!baseCard.unlocked || !playState.HasSpellSet(baseCard.spellSet))
                    {
                        cardListIdx++;
                        continue;
                    }

                    Card c = GetUpgrade(baseCard);

                    cardBottom += cardHeight;

                    if (Game1.inputState.MousePos.Y < cardBottom)
                    {
                        selectedCardIdx = cardListIdx;
                        if ( Game1.inputState.MousePos.X > rect.Right - 16 && CanUpgrade(baseCard) )
                        {
                            selectedCardUpgrade = true;
                        }
                        break;
                    }
                    cardListIdx++;
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
                    if (playState.CanPlayCard(choosingPositionForCard, playState.getItemAt(targetPos)))
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
                    {
                        playing = cardClicked;
                        playPosition = playState.wizard.position;
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
            int cardListIdx = 0;
            int visibleCardIdx = 0;
//            int selectSize = 5;

            if (cardList == null)
                return;

            foreach (Card baseCard in cardList)
            {
                if (!baseCard.unlocked || !playState.HasSpellSet(baseCard.spellSet))
                {
                    cardListIdx++;
                    continue;
                }

                Card c = GetUpgrade(baseCard);
                bool canUpgrade = CanUpgrade(baseCard);

                bool canPlay;
                if (choosingCardForCard != null)
                {
                    canPlay = playState.CanPlayCard(choosingCardForCard, TriggerItem.create(baseCard));
                }
                else
                {
                    canPlay = playState.CanPlayCard(c);
                }

                Rectangle frameRect = new Rectangle(rect.Left, rect.Top + visibleCardIdx * cardHeight, c.frameTexture.Width - (canUpgrade ? 16 : 0), cardHeight);

                c.Draw(spriteBatch, frameRect, canPlay, (selectedCardIdx == cardListIdx));

                if (canUpgrade)
                {
                    Rectangle upgradeRect = new Rectangle(rect.Right-16, rect.Top + visibleCardIdx * cardHeight, 16, cardHeight);
                    bool selectedThisUpgrade = selectedCardUpgrade && (selectedCardIdx == cardListIdx);
                    spriteBatch.Draw(Game1.upgradeTexture, upgradeRect, selectedThisUpgrade? Color.Red: Color.White);
                }

                if (selectedCardIdx == cardListIdx && !selectedCardUpgrade)
                {
                    Card baseSelectedCard = cardList[selectedCardIdx];
                    Card selectedCard = GetUpgrade(baseSelectedCard);
                    TextChanges changes = playState.getTextChanges(baseSelectedCard);

                    Vector2 tooltipPos;
                    Tooltip.Align alignment;
                    if(rect.Left == 0)
                    {
                        tooltipPos = new Vector2(rect.Left + selectedCard.frameTexture.Width, rect.Top + visibleCardIdx * cardHeight);
                        alignment = Tooltip.Align.LEFT;
                    }
                    else
                    {
                        tooltipPos = new Vector2(rect.Left, rect.Top + visibleCardIdx * cardHeight);
                        alignment = Tooltip.Align.RIGHT;
                    }

                    Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, changes.Apply(selectedCard.description), tooltipPos, alignment);
                }

                visibleCardIdx++;
                cardListIdx++;
            }

            if (choosingPositionForCard != null)
            {
                playState.DrawTargetCursor(spriteBatch, choosingPositionForCard, Game1.inputState.MousePos);
            }
        }
    }
}
