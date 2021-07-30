using GameFinder.StoreHandlers.Steam;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HadesUltrawidePatch
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
            PatchViewportWithHephaistos();
            PatchGUIConfigs();
            PatchUIScripts();
            PatchRoomManager();
            PatchRoomPresentation();
            PatchCombatPresentation();
            PatchEventPresentation();
            PatchTraitTrayScripts();
            PatchAwardMenuScripts();
            PatchGameStatsScreen();
            PatchMarketScreen();
            PatchBoonInfoScreenScripts();
            PatchGhostAdminScreen();
            PatchMetaUpgrades();
            PatchMusicPlayerScreen();
            PatchQuestLogScreen();
            PatchSeedControlScreen();
            PatchSellTraitScripts();
            PatchStoreScripts();
            PatchUpgradeChoice();
            PatchWeaponUpgradeScripts();
            PatchRunClearScreen();
            PatchRunHistoryScreen();
        }

        private void PatchGUIConfigs()
        {

            var guiPath = Path.Combine(Game.Path, "Content/Game/GUI");
            var files = Directory.GetFiles(guiPath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var stringBuilder = new StringBuilder();
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string line;
                        int lineNumber = 1;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string trimmedLine = line.Trim();
                            if (trimmedLine.StartsWith("X = "))// || line.Trim(' ').StartsWith("SpacingX = "))
                            {
                                var splitSentence = line.Split('=');
                                int.TryParse(splitSentence[1], out int num);

                                // BuildNumberText, SaveAnim and ElapsedRunTimeText are right aligned
                                if (fileName == "InGameUI.sjson" && (lineNumber == 261 || lineNumber == 75 || lineNumber == 110))
                                {
                                    num = Convert.ToInt32(NewInternalWidth) + (num - OriginalInternalWidth);
                                }
                                // Right aligned items
                                else if (num >= OriginalInternalWidth - SideAlignmentThreshold)
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
                            lineNumber++;
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
                        else if (line.Contains("trait.AnchorId = CreateScreenObstacle") || line.Contains("local traitFrameId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_UI\"", "Group = \"Combat_UI\", Position = \"Left\"");
                        }
                        else if (line.Contains("Y = TraitUI.StartY + TraitUI.SpacerY * (-2 + TableLength(CurrentRun.Hero.RecentTraits))"))
                        {
                            line = line.Replace("Y = TraitUI.StartY + TraitUI.SpacerY * (-2 + TableLength(CurrentRun.Hero.RecentTraits))", "Y = TraitUI.StartY + TraitUI.SpacerY * (-2 + TableLength(CurrentRun.Hero.RecentTraits)), Position = \"Left\"");
                        }
                        else if (line.Contains("local obstacleId = CreateScreenObstacle({Name = \"BlankObstacle\", Group = \"Combat_UI\", X = 70 + i * 32, Y = ScreenHeight - 95})"))
                        {
                            line = line.Replace("Y = ScreenHeight - 95", "Y = ScreenHeight - 95, Position = \"Left\"");
                        }
                        else if (line.Contains("ScreenAnchors.ShrinePointIconId = CreateScreenObstacle"))
                        {
                            line = line.Replace("Group = \"Combat_Menu_TraitTray\"", "Group = \"Combat_Menu_TraitTray\", Position = \"Left\"");
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
                                 line.Contains("components.DetailsBacking = CreateScreenComponent") ||
                                 line.Contains("components[\"ShrineIcon\"..k] = CreateScreenComponent({ Name = \"TraitTrayMetaUpgradeIconButton\""))
                        {
                            line = line.Replace("Group = \"Combat_Menu_TraitTray\"", "Group = \"Combat_Menu_TraitTray\", Position = \"Left\"");
                            line = line.Replace("Group = \"Combat_Menu_TraitTray_Backing\"", "Group = \"Combat_Menu_TraitTray_Backing\", Position = \"Left\"");
                            line = line.Replace("Group = \"Combat_UI_Backing\"", "Group = \"Combat_UI_Backing\", Position = \"Left\"");
                        }
                        else if (line.Contains("SetScale({ Id = components.BackgroundTint.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
                        else if (line.Contains("CreateAnimation({ Name = \"BloodFrame\""))
                        {
                            line = line.Replace("OffsetX = ScreenCenterX", $"OffsetX = {NewCenterX}, Scale = {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchAwardMenuScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/AwardMenuScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction ="))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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

        private void PatchGameStatsScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/GameStatsScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.Blackout.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchMarketScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/MarketScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchBoonInfoScreenScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/BoonInfoScreenScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchGhostAdminScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/GhostAdminScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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

        private void PatchMetaUpgrades()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/MetaUpgrades.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchMusicPlayerScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/MusicPlayerScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchQuestLogScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/QuestLogScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchSeedControlScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/SeedControlScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchSellTraitScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/SellTraitScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchStoreScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/StoreScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchUpgradeChoice()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/UpgradeChoice.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 4 })"))
                        {
                            line = line.Replace("Fraction = 4", $"Fraction = 4 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchWeaponUpgradeScripts()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/WeaponUpgradeScripts.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.ShopBackgroundDim.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchRunClearScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/RunClearScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.Blackout.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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
        private void PatchRunHistoryScreen()
        {
            var filePath = Path.Combine(Game.Path, "Content/Scripts/RunHistoryScreen.lua");
            var sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SetScale({ Id = components.Blackout.Id, Fraction = 10 })"))
                        {
                            line = line.Replace("Fraction = 10", $"Fraction = 10 * {_screen.SixteenNineScaleFactor.ToString(CultureInfo.InvariantCulture)}");
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

        private void PatchViewportWithHephaistos()
        {
            string githubURL = "https://github.com/nbusseneau/hephaistos/releases/download/v0.1.0/hephaistos.exe";
            string fileName = "hephaistos.exe";
            string downloadPath = Path.Combine(Game.Path, fileName);

            if (!File.Exists(downloadPath)) {
                try
                {
                    Console.WriteLine("Downloading Hephaistos to patch the Hades viewport...");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(githubURL, downloadPath);
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine("An error occurred when attempted to download Hephaistos! Check your internet connection or try placing the executable directly in your Hades folder.");
                    Console.WriteLine(ex.Message);
                    return;
                }
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = downloadPath;
                startInfo.Arguments = $"patch {_screen.Width} {_screen.Height} -v --force";
                startInfo.WorkingDirectory = Game.Path;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = false;

                using (Process hephaistos = new Process())
                {
                    hephaistos.StartInfo = startInfo;
                    hephaistos.Start();
                    string result = hephaistos.StandardOutput.ReadToEnd();
                    hephaistos.WaitForExit();
                    Console.WriteLine(hephaistos.ExitCode);
                    Console.WriteLine(result);
                    if (!result.ToLower().Contains("error"))
                        Console.WriteLine("Succesfully patched Hades viewport with Hephaistos!");
                    else
                        Console.WriteLine("An error occurred when running Hephaistos!");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred when running Hephaistos!");
                Console.WriteLine(ex.Message);
            }
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
