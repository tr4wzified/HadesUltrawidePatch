using GameFinder.StoreHandlers.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HadesUltrawideGUIPatcher
{
    public class Hades
    {
        public Hades()
        {
            var steamHandler = new SteamHandler();
            steamHandler.FindAllGames();
            Game = steamHandler.Games.Find(x => x.Name == "Hades");
        }

        public void PatchForUltrawide()
        {
            PatchGUIConfigs();
            PatchUIData();
        }

        private void PatchGUIConfigs()
        {
            int alignmentThreshold = 120;

            var guiPath = Path.Combine(Game.Path, "Content/Game/GUI");
            var files = Directory.GetFiles(guiPath);
            foreach (var file in files)
            {
                var stringBuilder = new StringBuilder();
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Trim().StartsWith("X = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);
                                // Right aligned items
                                if (num >= OriginalResX - alignmentThreshold)
                                {
                                    num = NewResX + (num - OriginalResX);
                                }
                                // Center aligned items
                                else if (num >= alignmentThreshold)
                                {
                                    num = NewCenterX + (num - OriginalCenterX);
                                }
                                // The rest is left aligned, not adjusting those
                                splitSentence[1] = num.ToString();
                                line = string.Join("= ", splitSentence);
                            }

                            else if (line.Trim().StartsWith("Width = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);

                                if (num == OriginalResX)
                                {
                                    num = NewResX;
                                    splitSentence[1] = num.ToString();
                                    line = string.Join("= ", splitSentence);
                                }
                            }
                            stringBuilder.AppendLine(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"File {file} could not be read!");
                    Console.WriteLine(e.ToString());
                }

                File.WriteAllText(file, stringBuilder.ToString());
            }
        }

        private void PatchUIData()
        {
            var sb = new StringBuilder();
            var uiDataPath = Path.Combine(Game.Path, "Content/Scripts/UIData.lua");
            try
            {
                using (StreamReader sr = new StreamReader(uiDataPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("X = "))
                        {
                            if (line.EndsWith(','))
                            {
                                var lineToModify = line.Remove(line.Length - 1);
                                var splitSentence = lineToModify.Split('=');
                                int.TryParse(splitSentence[1], out int num);
                                if (num == OriginalCenterX)
                                {
                                    splitSentence[1] = (NewCenterX + (num - OriginalCenterX)).ToString();
                                    var newLine = string.Join("= ", splitSentence);
                                    newLine += ',';
                                    line = newLine;
                                }

                            }
                        }
                        else if (line.StartsWith("ScreenCenterX"))
                        {
                            line = $"ScreenCenterX = {NewResX}/2";
                        }
                        else if (line.StartsWith("ScreenWidth"))
                        {
                            line = $"ScreenWidth = {NewResX}";
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {uiDataPath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(uiDataPath, sb.ToString());
        }

        public SteamGame Game { get; }

        public int OriginalResX = 1920;
        public int OriginalCenterX = 960;
        public int NewResX = 2580;
        public int NewCenterX = 1290;
    }
}
