using GameFinder.StoreHandlers.Steam;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HadesUltrawideGUIPatcher
{
    public class Hades
    {
        private PrimaryScreen _screen;
        public Hades(PrimaryScreen screen)
        {
            _screen = screen;
            NewInternalWidth = screen.Width / screen.Height * OriginalInternalHeight;
            NewCenterX = NewInternalWidth / 2;

            var steamHandler = new SteamHandler();
            steamHandler.FindAllGames();
            Game = steamHandler.Games.Find(x => x.Name == "Hades");
        }

        public void PatchForUltrawide()
        {
            PatchGUIConfigs();
            PatchScripts();
            PatchAnimations();
            PatchDebugKeyScreen();
            PatchAsphodel();
            PatchObstacles();
            // Patch functions below touch same files as PatchScripts()
            PatchCreateScreenObstacle();
            PatchVignette();
        }

        private void PatchGUIConfigs()
        {

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
                            string trimmedLine = line.Trim();
                            if (trimmedLine.StartsWith("X = "))// || line.Trim(' ').StartsWith("SpacingX = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);

                                // Right aligned items
                                if (num >= OriginalInternalWidth - SideAlignmentThreshold)
                                {
                                    num = Convert.ToInt32(NewInternalWidth) + (num - OriginalInternalWidth);
                                }
                                // Center aligned items
                                else if (num >= SideAlignmentThreshold)
                                {
                                    num = Convert.ToInt32(NewCenterX) + (num - OriginalCenterX);
                                }
                                // The rest is left aligned, not adjusting those

                                splitSentence[1] = num.ToString();
                                line = string.Join("= ", splitSentence);
                            }

                            else if (trimmedLine.StartsWith("Width = ") || trimmedLine.StartsWith("SpacingX = ") || trimmedLine.StartsWith("FreeFormSelectMaxGridDistance ="))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);

                                if (num == OriginalInternalWidth)
                                {
                                    num = Convert.ToInt32(NewInternalWidth);
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

        private void PatchScripts()
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
                            line = $"ScreenCenterX = {NewInternalWidth}/2";
                        }
                        else if (line.StartsWith("ScreenWidth"))
                        {
                            line = $"ScreenWidth = {NewInternalWidth}";
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

            sb.Clear();
            var roomPresentationPath = Path.Combine(Game.Path, "Content/Scripts/RoomPresentation.lua");
            try
            {
                using (StreamReader sr = new StreamReader(roomPresentationPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("X = "))
                        {
                            var splitSentence = line.Split('=');
                            if (int.TryParse(splitSentence[1], out int num))
                            {
                                line = line.Remove(line.Length - 1);
                                // Right aligned items
                                if (num >= OriginalInternalWidth - SideAlignmentThreshold)
                                {
                                    num = Convert.ToInt32(NewInternalWidth) + (num - OriginalInternalWidth);
                                }
                                // Center aligned items
                                else if (num >= SideAlignmentThreshold)
                                {
                                    num = Convert.ToInt32(NewCenterX) + (num - OriginalCenterX);
                                }
                                // The rest is left aligned, not adjusting those

                                splitSentence[1] = num.ToString();
                                var splitSentenceString = string.Join("= ", splitSentence);
                                splitSentenceString += ',';
                                line = splitSentenceString;
                            }
                        }

                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {roomPresentationPath} could not be read!");
                Console.WriteLine(e.ToString());
            }

            File.WriteAllText(roomPresentationPath, sb.ToString());

            sb.Clear();
            var traitTrayScriptsPath = Path.Combine(Game.Path, "Content/Scripts/TraitTrayScripts.lua");
            try
            {
                using (StreamReader sr = new StreamReader(traitTrayScriptsPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.TraitTrayScreen.BackingDecor = CreateScreenObstacle({ Name = cosmeticData.TraitBackingDecor, Group = \"Combat_Menu_TraitTray_Backing\", X = 960, Y = 1015 })"))
                        {
                            line = line.Replace("960", NewCenterX.ToString());
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {roomPresentationPath} could not be read!");
                Console.WriteLine(e.ToString());
            }

            File.WriteAllText(traitTrayScriptsPath, sb.ToString());
        }

        private void PatchAnimations()
        {
            string[] animationFilesToPatch = { Path.Combine(Game.Path, "Content/Game/Animations/Fx.sjson"), Path.Combine(Game.Path, "Content/Game/Animations/GUIAnimations.sjson"), Path.Combine(Game.Path, "Content/Game/Animations/ObstacleAnimations.sjson") };
            foreach (var file in animationFilesToPatch)
            {
                var sb = new StringBuilder();
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string trimmedLine = line.Trim();
                            if (trimmedLine.StartsWith("X = "))
                            {
                                line = ApplyUltrawideXOffsetForInteger(line);
                            }
                            else if (trimmedLine.StartsWith("OffsetX = "))
                            {
                                line = ApplyUltrawideXOffsetForInteger(line);
                            }
                            else if (trimmedLine.StartsWith("OriginX = "))
                            {
                                line = ApplyUltrawideXOffsetForInteger(line);
                            }
                            else if (trimmedLine.StartsWith("RandomOffsetX ="))
                            {
                                line = ApplyUltrawideXOffsetForDouble(line);
                            }
                            else if (trimmedLine.StartsWith("ScaleRadius ="))
                            {
                                line = ApplyUltrawideXOffsetForInteger(line);
                            }
                            else if (trimmedLine.StartsWith("RandomOffsetX ="))
                            {
                                line = ApplyUltrawideXOffsetForInteger(line);
                            }
                            else if (trimmedLine.StartsWith("ScaleRadius ="))
                            {
                                line = ApplyUltrawideXOffsetForDouble(line);
                            }
                            sb.AppendLine(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"File {file} could not be read!");
                    Console.WriteLine(e.ToString());
                }
                File.WriteAllText(file, sb.ToString());
            }
        }
        private void PatchDebugKeyScreen()
        {
            var debugKeyScreenPath = Path.Combine(Game.Path, "Content/Game/GUI/DebugKeyScreen.sjson");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(debugKeyScreenPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("SpacingX = "))
                        {
                            line = ApplyUltrawideXOffsetForInteger(line);
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {debugKeyScreenPath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(debugKeyScreenPath, sb.ToString());
        }
        private void PatchAsphodel()
        {
            var filePath = Path.Combine(Game.Path, "Content/Game/Obstacles/Asphodel.sjson");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("{ X = 960 Y = -244 }"))
                        {
                            line = line.Replace("960", NewCenterX.ToString());
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {filePath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        private void PatchObstacles()
        {
            var filePath = Path.Combine(Game.Path, "Content/Game/Obstacles/1_DevObstacles.sjson");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("{ X = 1920") || line.Contains("{ X = -1920"))
                        {
                            line = line.Replace("1920", NewInternalWidth.ToString());
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {filePath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(filePath, sb.ToString());
        }
        public void PatchCreateScreenObstacle()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/UIScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("local obstacleId = S"))
                        {
                            line = line.Replace("OffsetX = params.X", $"OffsetX = params.X + {NewCenterX - OriginalCenterX}");
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {filePath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        public void PatchVignette()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/RoomManager.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.Vignette = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY, ", $"Y = ScreenCenterY, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)},");
                        }
                        sb.AppendLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"File {filePath} could not be read!");
                Console.WriteLine(e.ToString());
            }
            File.WriteAllText(filePath, sb.ToString());

        }
        private string ApplyUltrawideXOffsetForDouble(string line)
        {
            var splitSentence = line.Split('=');
            double.TryParse(splitSentence[1], out double num);
            if (num == OriginalCenterX)
            {
                splitSentence[1] = (NewCenterX + (num - OriginalCenterX)).ToString();
                var newLine = string.Join("= ", splitSentence);
                line = newLine;
            }

            return line;
        }
        private string ApplyUltrawideXOffsetForInteger(string line)
        {
            var splitSentence = line.Split('=');
            int.TryParse(splitSentence[1], out int num);
            if (num == OriginalCenterX)
            {
                splitSentence[1] = (NewCenterX + (num - OriginalCenterX)).ToString();
                var newLine = string.Join("= ", splitSentence);
                line = newLine;
            }

            return line;
        }

        public SteamGame Game { get; }

        public const int OriginalInternalWidth = 1920;
        public const int OriginalInternalHeight = 1080;
        public const int OriginalCenterX = OriginalInternalWidth / 2;
        public const int SideAlignmentThreshold = 120;

        public readonly double NewInternalWidth;
        public readonly double NewCenterX;
    }
}
