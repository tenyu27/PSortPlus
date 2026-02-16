using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using PSortPlus.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using PSortPlus.Checkers;

namespace PSortPlus
{
    public static unsafe class Utils
    {
        public static Vector2 CellPadding => ImGui.GetStyle().CellPadding + new Vector2(0, 2);

        public static Profile GetProfile()
        {
            if (PSortPlus.C != null)
            {
                return PSortPlus.C.GlobalProfile;
            }
            else
            {
                throw new InvalidOperationException("PSortPlus.C is null. Cannot retrieve profile.");
            }
        }

        public static string PrintRange(this IEnumerable<string> s, out string? FullList, string noneStr = "Any")
        {
            FullList = null;
            var list = s.ToArray();
            if (list.Length == 0) return noneStr;
            if (list.Length == 1) return list[0].ToString();
            FullList = list.Select(x => x.ToString()).Join("\n");
            return $"{list.Length} selected";
        }
    }
}
