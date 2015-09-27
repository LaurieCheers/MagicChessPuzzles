using Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DragonGfx
{
    class UIButtonStyleSet
    {
        public readonly UIButtonStyle normal;
        public readonly UIButtonStyle hover;
        public readonly UIButtonStyle pressed;

        public UIButtonStyleSet(UIButtonStyle normal, UIButtonStyle hover, UIButtonStyle pressed)
        {
            this.normal = normal;
            this.hover = hover;
            this.pressed = pressed;
        }
    }

    class UIButtonStyle
    {
        public readonly SpriteFont font;
        public readonly Color textColor;
        public readonly LayeredImage image;
        public readonly Vector2 textOffset;

        public UIButtonStyle(SpriteFont font, Color textColor, LayeredImage image, Color fillColor)
        {
            this.font = font;
            this.textColor = textColor;
            this.image = image;
        }

        public UIButtonStyle(SpriteFont font, Color textColor, LayeredImage image, Color fillColor, Vector2 textOffset)
        {
            this.font = font;
            this.textColor = textColor;
            this.image = image;
            this.textOffset = textOffset;
        }

        public void Draw(SpriteBatch spriteBatch, string label, Rectangle frame)
        {
            image.Draw(spriteBatch, frame);
//            MagicUI.Draw9Grid(spriteBatch, texture, frame, fillColor);
//            spriteBatch.Draw(texture, frame, fillColor);
            if (font != null)
            {
                Vector2 labelSize = font.MeasureString(label);
                spriteBatch.DrawString(font, label, new Vector2((float)Math.Floor(frame.Center.X + textOffset.X - labelSize.X / 2), (float)Math.Floor(frame.Center.Y + textOffset.Y - labelSize.Y / 2)), textColor);
            }
        }
    }

    class UIButton
    {
        public readonly string label;
        public readonly Rectangle frame;
        public readonly UIButtonStyleSet styles;
        public readonly OnPressDelegate onPress;
        public delegate void OnPressDelegate();
        bool mouseInside;
        bool pressedInside;

        public UIButton(string label, Rectangle frame, UIButtonStyleSet styles, OnPressDelegate onPress)
        {
            this.label = label;
            this.frame = frame;
            this.styles = styles;
            this.onPress = onPress;
        }

        public void Update(InputState inputState)
        {
            mouseInside = frame.Contains(inputState.MousePos);
            if(mouseInside && inputState.WasMouseLeftJustPressed())
            {
                pressedInside = true;
            }

            if (!inputState.mouseLeft.pressed)
            {
                if(mouseInside && pressedInside)
                {
                    onPress();
                }
                pressedInside = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            UIButtonStyle currentStyle;
            if(mouseInside)
            {
                if (pressedInside)
                    currentStyle = styles.pressed;
                else
                    currentStyle = styles.hover;
            }
            else
            {
                currentStyle = styles.normal;
            }

            currentStyle.Draw(spriteBatch, label, frame);
        }
    }
}
