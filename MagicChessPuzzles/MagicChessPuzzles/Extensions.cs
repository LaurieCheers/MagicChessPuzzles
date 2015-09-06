using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MagicChessPuzzles
{
    public static class Extensions
    {
        public static bool Contains(this Rectangle self, Vector2 pos)
        {
            return self.Left <= pos.X && self.Top <= pos.Y && self.Right >= pos.X && self.Bottom >= pos.Y;
        }

        public static Rectangle Lerp(this Rectangle start, Rectangle end, float lerp)
        {
            return new Rectangle(
                (int)(start.X + lerp * (end.X - start.X)),
                (int)(start.Y + lerp * (end.Y - start.Y)),
                (int)(start.Width + lerp * (end.Width - start.Width)),
                (int)(start.Height + lerp * (end.Height - start.Height))
            );
        }
    }
}
