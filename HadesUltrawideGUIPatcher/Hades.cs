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
            PatchUIScripts();
            PatchRoomManager();
            PatchRoomPresentation();
            PatchCombatPresentation();
            PatchEventPresentation();
            PatchTraitTrayScripts();
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

        private void PatchUIScripts()
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
                        else if (line.Contains("ScreenAnchors.GunUI = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = GunUI.StartY", "Y = GunUI.StartY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.Shadow = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenHeight", "Y = ScreenHeight, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.BadgeId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Scale = 0.5", "Scale = 0.5, Position = \"Left\"");
                        }
                        else if (line.Contains("local screenId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenHeight - 50 + offsetY", "Y = ScreenHeight - 50 + offsetY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.SuperMeterIcon = CreateScreenObstacle") || line.Contains("ScreenAnchors.SuperMeterCap = CreateScreenObstacle") || line.Contains("ScreenAnchors.SuperMeterHint = CreateScreenObstacle"))
                        {
                            line = line.Replace("Name = \"BlankObstacle\"", "Name = \"BlankObstacle\", Position = \"Left\"");
                        }
                        else if (line.Contains("table.insert( ScreenAnchors.SuperPipBackingIds,"))
                        {
                            line = line.Replace("Y = SuperUI.PipY", "Y = SuperUI.PipY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.AmmoIndicatorUI = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenHeight - 62", "Y = ScreenHeight - 62, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.HealthShroudAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_UI_World\"", $"Group = \"Combat_UI_World\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
                        }
                        else if (line.Contains("ScreenAnchors.MoneyIcon = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = offsetY", "Y = offsetY, Position = \"Right\"");
                        }
                        else if (line.Contains("local tempObstacleId = CreateScreenObstacle({ Name = \"BlankObstacle\", Group = \"Combat_UI\", X = ConsumableUI.StartX"))
                        {
                            line = line.Replace("Y = offsetY or 0", "Y = offsetY or 0, Position = \"Right\"");
                        }
                        else if (line.Contains("anchorId = CreateScreenObstacle({ Name = \"BlankObstacle\", Group = \"Combat_UI\""))
                        {
                            line = line.Replace("Y = offsetY or 0", "Y = offsetY or 0, Position = \"Right\"");
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

        private void PatchRoomManager()
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
                        if (line.Contains("ScreenAnchors.Vignette = CreateScreenObstacle") || line.Contains("CreateScreenObstacle({ Name = \"BlankObstacle\""))
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

        private void PatchRoomPresentation()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/RoomPresentation.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.Transition ="))
                        {
                            line = line.Replace("Y = ScreenCenterY, ", $"Y = ScreenCenterY, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}, Position = \"Center\", ");
                        }
                        else if (line.Contains("ScreenAnchors.DeathBackground = "))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}, Position = \"Center\"");
                        }
                        else if (line.Contains("ScreenAnchors.FullscreenAlertFxAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}, Position = \"Center\"");
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
        private void PatchTraitTrayScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/TraitTrayScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.RunDepthId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_Menu_Overlay\"", $"Group = \"Combat_Menu_Overlay\", Position = \"Right\"");
                        }
                        else if (line.Contains("ScreenAnchors.TraitTrayScreen.BackingDecor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = 1015", $"Y = 1015, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.TraitTrayScreen.PrimaryFrameBacking = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = locationY", $"Y = locationY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.TraitTrayScreen.BadgeNamePlateId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = 1052", "Y = 1052, Position = \"Left\"");
                        }
                        else if (line.Contains("local attackIcon = ") || line.Contains("local secondaryIcon = ") || line.Contains("local rangedIcon = ") || line.Contains("local dashIcon = ") || line.Contains("local wrathIcon = ") || line.Contains("local frameIcon = "))
                        {
                            line = line.Replace("Y = locationY", "Y = locationY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.TraitTrayScreen.HoverFrame = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_Menu_TraitTray_Additive\"", "Group = \"Combat_Menu_TraitTray_Additive\", Position = \"Left\"");
                        }
                        else if (line.Contains("components.BackgroundTint = CreateScreenComponent") || 
                                 line.Contains("components.TitleBox = CreateScreenComponent") ||
                                 line.Contains("components.RarityBox = CreateScreenComponent") ||
                                 line.Contains("components.Patch = CreateScreenComponent") ||
                                 line.Contains("components.Frame = CreateScreenComponent") ||
                                 line.Contains("components.Icon = CreateScreenComponent") ||
                                 line.Contains("components.PinIndicator = CreateScreenComponent") ||
                                 line.Contains("components.PinIndicatorDetails = CreateScreenComponent") ||
                                 line.Contains("components.ShopBackground = CreateScreenComponent") ||
                                 line.Contains("components.CenterColumn = CreateScreenComponent") ||
                                 line.Contains("components.EndColumn = CreateScreenComponent") ||
                                 line.Contains("components.BackingTop = CreateScreenComponent") ||
                                 line.Contains("components.BackingBottom = CreateScreenComponent") ||
                                 line.Contains("components.CloseButton = CreateScreenComponent") ||
                                 line.Contains("local newColumnObject = CreateScreenComponent") ||
                                 line.Contains("local traitFrameId = CreateScreenObstacle") ||
                                 line.Contains("components.ShrineUpgradeBacking = CreateScreenComponent") ||
                                 line.Contains("components[\"ShrineIcon\"..k] = CreateScreenComponent") ||
                                 line.Contains("components.MetaUpgradeBacking = CreateScreenComponent") ||
                                 line.Contains("components[\"MetaIcon\"..k] = CreateScreenComponent") ||
                                 line.Contains("traitIcon = CreateScreenComponent") ||
                                 line.Contains("components.DetailsBacking = CreateScreenComponent"))
                        {
                            line = line.Replace("Group = \"Combat_Menu_TraitTray\"", "Group = \"Combat_Menu_TraitTray\", Position = \"Left\"");
                            line = line.Replace("Group = \"Combat_Menu_TraitTray_Backing\"", "Group = \"Combat_Menu_TraitTray_Backing\", Position = \"Left\"");
                            line = line.Replace("Group = \"Combat_UI_Backing\"", "Group = \"Combat_UI_Backing\", Position = \"Left\"");
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
        private void PatchCombatPresentation()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/CombatPresentation.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.PoisonVignetteAnchor = CreateScreenObstacle") || line.Contains("ScreenAnchors.LavaVignetteAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Position = \"Center\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
                        }
                        else if (line.Contains("CreateScreenObstacle({ Name = \"GunReloadIndicator\""))
                        {
                            line = line.Replace("Y = GunUI.StartY + GunUI.ReloadingOffsetY", $"Y = GunUI.StartY + GunUI.ReloadingOffsetY, Position = \"Left\"");
                        }
                        else if (line.Contains("state.GunReloadDisplayId = "))
                        {
                            line = line.Replace("Y = GunUI.StartY + GunUI.ReloadingOffsetY", "Y = GunUI.StartY + GunUI.ReloadingOffsetY, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.hadesBloodstoneVignetteAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Position = \"Center\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
                        }
                        else if (line.Contains("ScreenAnchors.FullscreenAlertFxAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Position = \"Center\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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

        private void PatchEventPresentation()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/EventPresentation.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ScreenAnchors.MoneyDelta = "))
                        {
                            line = line.Replace("Y = ConsumableUI.StartY + ConsumableUI.SpacerY * 0", $"Y = ConsumableUI.StartY + ConsumableUI.SpacerY * 0, Position = \"Right\"");
                        }
                        else if (line.Contains("ScreenAnchors.DialogueBackgroundId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_Menu\"", $"Group = \"Combat_Menu\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
                        }
                        else if (line.Contains("ScreenAnchors.FullscreenAlertFxAnchor = CreateScreenObstacle"))
                        {
                            line = line.Replace("Y = ScreenCenterY", $"Y = ScreenCenterY, Position = \"Center\", Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        public const int SideAlignmentThreshold = 0;

        public readonly double NewInternalWidth;
        public readonly double NewCenterX;
    }
}
