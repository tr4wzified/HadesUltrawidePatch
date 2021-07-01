using System;
using GameFinder;
using GameFinder.StoreHandlers.Steam;

namespace HadesUltrawideGUIPatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var hades = new Hades();
            hades.PatchForUltrawide();
        }
    }
}
