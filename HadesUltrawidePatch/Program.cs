using System;
using GameFinder;
using GameFinder.StoreHandlers.Steam;

namespace HadesUltrawidePatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var screen = new PrimaryScreen();
            var hades = new Hades(screen);
            hades.PatchForUltrawide();
        }
    }
}
