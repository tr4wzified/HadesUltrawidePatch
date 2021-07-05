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

        public void PatchCreateScreenObstacle()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/UIScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    int i = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        i++;
                        
                        if (i == 2356)
                        {
                                line = line.Insert(0, $@"
--- trawzifieds Ultrawide Patch ---
	local sideAlignmentThreshold = {SideAlignmentThreshold}
	local originalInternalWidth = ScreenCenterX * 2
	local newInternalWidth = {NewInternalWidth}
	local newCenterX = {NewInternalWidth / 2}
	local maxCenterPosition = originalInternalWidth - sideAlignmentThreshold
	if params.Position == nil then
		if params.X >= sideAlignmentThreshold and params.X <= maxCenterPosition then
			params.X = params.X - ScreenCenterX + newCenterX
		elseif params.X > originalInternalWidth - sideAlignmentThreshold then
			params.X = newInternalWidth + params.X - originalInternalWidth
		end
	elseif params.Position == " + "\"" + "Center" + "\"" + @"then
		params.X = params.X - ScreenCenterX + newCenterX
	elseif params.Position == " + "\"" + "Right" + "\"" + @"then
		params.X = newInternalWidth + params.X - originalInternalWidth
	end
	--- End Ultrawide Patch ---
");
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
                            line = line.Replace("X = ScreenCenterX, Y = ScreenCenterY, ", $"X = ScreenCenterX, Y = ScreenCenterY, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}, Position = \"Center\", ");
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
        public SteamGame Game { get; }

        public const int OriginalInternalWidth = 1920;
        public const int OriginalInternalHeight = 1080;
        public const int OriginalCenterX = OriginalInternalWidth / 2;
        public const int SideAlignmentThreshold = 120;

        public readonly double NewInternalWidth;
        public readonly double NewCenterX;
    }
}
