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
                            if (line.Trim().StartsWith("X = "))// || line.Trim(' ').StartsWith("SpacingX = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);
                                // Right aligned items
                                if (num >= 1920 - alignmentThreshold)
                                {
                                    num = 2580 + (num - 1920);
                                }
                                // Center aligned items
                                else if (num >= alignmentThreshold)
                                {
                                    num = 1290 + (num - 960);
                                }
                                // The rest is left aligned, not adjusting those

                                splitSentence[1] = num.ToString();
                                line = string.Join("= ", splitSentence);
                            }

                            else if (line.Trim().StartsWith("Width = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);

                                if (num == 1920)
                                {
                                    num = 2580;
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
                                if (num == 960)
                                {
                                    splitSentence[1] = (1290 + (num - 960)).ToString();
                                    var newLine = string.Join("= ", splitSentence);
                                    newLine += ',';
                                    line = newLine;
                                }

                            }
                        }
                        else if (line.StartsWith("ScreenCenterX"))
                        {
                            line = "ScreenCenterX = 2580/2";
                        }
                        else if (line.StartsWith("ScreenWidth"))
                        {
                            line = "ScreenWidth = 2580";
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
    }
}
