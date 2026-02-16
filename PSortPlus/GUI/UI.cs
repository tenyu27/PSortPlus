using Dalamud.Interface.Colors;
using ECommons;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.SimpleGui;
using System;
using System.Numerics;

namespace PSortPlus.GUI
{
    public static unsafe class UI
    {
        public static string? RequestTab = null;
        public const string AnyNotice = "Meeting any of the following conditions will result in rule being triggered:\n";

        public static void DrawMain()
        {
            // Ensure 'resolution' is defined and initialized
            string resolution = "Configuration"; // Replace with actual logic to determine resolution if needed

            // Ensure PSortPlus.P is not null before accessing its properties
            if (PSortPlus.P != null && EzConfigGui.Window != null)
            {
                var version = PSortPlus.P.GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0";
                EzConfigGui.Window.WindowName = $"{DalamudReflector.GetPluginName()} v{version} [{resolution}]###{DalamudReflector.GetPluginName()}";
            }
            else if (EzConfigGui.Window != null)
            {
                EzConfigGui.Window.WindowName = $"{DalamudReflector.GetPluginName()} [Plugin Not Initialized]";
            }

            if (EzConfigGui.Window != null)
            {
                EzConfigGui.Window.RespectCloseHotkey = true;
                EzConfigGui.Window.SetSizeConstraints(new Vector2(800, 900), new Vector2(1200, 1200));
            }

            if (PSortPlus.C != null)
            {
                ImGuiEx.EzTabBar("TabsNR2", tabs: new (string? name, Action function, Vector4? color, bool child)[]
                {
                        (PSortPlus.C.ShowTutorial || PSortPlus.C.Debug ? "Tutorial" : null, UITutorial.Draw, null, true),
                        ("Rules", GuiRules.Draw, ImGuiColors.DalamudViolet, true),
                        ("Presets", GuiPresets.Draw, ImGuiColors.DalamudViolet, true),
                        (PSortPlus.C.Debug ? "Overrides" : null, GuiOverrides.Draw, ImGuiColors.DalamudYellow, true),
                        (PSortPlus.C.Debug ? "Settings" : null, GuiSettings.Draw, null, true),
                        InternalLog.ImGuiTab(),
                        (PSortPlus.C.Debug ? "Debug" : null, GuiDebug.Draw, ImGuiColors.DalamudGrey3, true),
                });
            }

            RequestTab = null;
        }
    }
}
