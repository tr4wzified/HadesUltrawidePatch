using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HadesUltrawideGUIPatcher
{
    public static class GDI32
    {
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        private enum DeviceCap
        {
            HORZRES = 8,
            VERTRES = 10,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }
        public static int GetScreenWidth() => GetDeviceCaps(Graphics.FromHwnd(IntPtr.Zero).GetHdc(), (int)DeviceCap.HORZRES);
        public static int GetScreenHeight() => GetDeviceCaps(Graphics.FromHwnd(IntPtr.Zero).GetHdc(), (int)DeviceCap.VERTRES);
    }
}
