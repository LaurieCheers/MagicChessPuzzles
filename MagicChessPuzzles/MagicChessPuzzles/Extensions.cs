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
    }
}
