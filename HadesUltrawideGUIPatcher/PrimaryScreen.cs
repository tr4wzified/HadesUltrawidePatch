using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HadesUltrawideGUIPatcher
{
    public class PrimaryScreen
    {
        public PrimaryScreen()
        {
            Width = GDI32.GetScreenWidth();
            Height = GDI32.GetScreenHeight();
            SixteenNineWidth = Convert.ToInt32(Math.Ceiling((double)Height * 16 / 9));
            SixteenNineScaleFactor = Width / SixteenNineWidth;
        }

        public double Width;
        public double Height;
        public double SixteenNineWidth;
        public double SixteenNineScaleFactor;
    }
}
