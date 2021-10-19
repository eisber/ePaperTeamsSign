using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ePaperTeamsPresence.Desktop
{
    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public class IntPoint
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    public static class PointExtensions
    {
        public static IntPoint ToIntPoint(this Point p)
        {
            return new IntPoint {
                X = (int)Math.Round(p.X, 0),
                Y = (int)Math.Round(p.Y, 0)
            };
        }
    }
}
