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
        List<Card> cardList;
        Card choosingPosition;
        public Card playing;
        public Point playPosition;
        bool dynamic;
        public Point caster;

        public CardCatalog(Rectangle rect)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = null;
            this.rect = rect;
            this.dynamic = true;
        }

        public CardCatalog(Rectangle rect, List<Card> cardList)
        {
            this.cardWidth = rect.Width;
            this.cardHeight = 32;
            this.cardList = cardList;
            this.rect = rect;
            this.dynamic = false;
        }

        public void ShowSpells(Point caster, string spellType)
        {
            this.caster = caster;
            this.cardList = Game1.spellBooks[spellType];
        }

        public void Update(GameState playState)
        {
            playing = null;

            if (cardList == null || !rect.Contains(Game1.inputState.MousePos))
            {
                selectedCardIdx = -1;
            }
            else
            {
                int cardIdx = 0;
                int cardBottom = rect.Top;

                foreach (Card c in cardList)
                {
                    cardBottom += cardHeight;

                    if (Game1.inputState.MousePos.Y < cardBottom)
                    {
                        selectedCardIdx = cardIdx;
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
                if (choosingPosition != null)
                {
                    Point targetPos = GameState.ScreenToGridPos(Game1.inputState.MousePos);
                    if (playState.CanPlayCard(choosingPosition, caster, targetPos))
                    {
                        playing = choosingPosition;
                        playPosition = targetPos;
                    }
                    choosingPosition = null;
                }
                else if (selectedCardIdx != -1)
                {
                    Card cardClicked = cardList[selectedCardIdx];
                    if( cardClicked.targetType != TargetType.none )
                        choosingPosition = cardClicked;
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

        public void Draw(SpriteBatch spriteBatch, GameState playState)
        {
            int cardIdx = 0;
//            int selectSize = 5;
            Color offwhite = new Color(240,240,240);

            if (cardList == null)
                return;

            foreach (Card c in cardList)
            {
                bool canPlay = playState.CanPlayCard(c);
                Rectangle frameRect = new Rectangle(rect.Left, rect.Top + cardIdx * cardHeight, c.frameTexture.Width, cardHeight);

                if( canPlay )
                    Game1.activeCardBG.Draw(spriteBatch, frameRect, (selectedCardIdx != cardIdx) ? c.frameColor.Multiply(offwhite) : c.frameColor);
                else
                    spriteBatch.Draw(c.frameTexture, frameRect, Color.Gray);

                DrawIcon(spriteBatch, c.image, new Vector2(rect.Left, rect.Top + cardIdx * cardHeight), canPlay);

                spriteBatch.DrawString(Game1.font, c.name, new Vector2(rect.Left + c.image.Width, rect.Top + cardIdx * cardHeight), (selectedCardIdx != cardIdx)? Color.Black: (canPlay? Color.Yellow: Color.Red));
                ResourceAmount.Draw(spriteBatch, c.cost, new Vector2(rect.Left + c.image.Width, rect.Top + cardIdx * cardHeight + 15));

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

            if (selectedCardIdx != -1)
            {
                Card selectedCard = cardList[selectedCardIdx];
                Vector2 tooltipPos = new Vector2(rect.Left + selectedCard.frameTexture.Width, rect.Top + selectedCardIdx * cardHeight);
                Tooltip.DrawTooltip(spriteBatch, Game1.font, Game1.tooltipBG, selectedCard.description, tooltipPos);
            }

            if (choosingPosition != null)
            {
                playState.DrawTargetCursor(spriteBatch, choosingPosition, caster, Game1.inputState.MousePos);
            }
        }

        void DrawIcon(SpriteBatch spriteBatch, Texture2D icon, Vector2 pos, bool canPlay)
        {
            Rectangle iconRect = new Rectangle((int)pos.X, (int)(pos.Y + 32.0f - icon.Height), icon.Width, icon.Height);
            spriteBatch.Draw(icon, iconRect, canPlay ? Color.White : Color.Gray);
        }
    }
}
