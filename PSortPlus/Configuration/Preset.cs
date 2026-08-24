using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSortPlus.Configuration
{
    [Serializable]
    public class Preset
    {
        // Serialized and used as the stable identifier rules reference presets by,
        // so renaming or duplicating preset names cannot break rule bindings.
        public string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        public List<Job> JobOrder = new();
        public bool isPlayerAtTop = false;

        public Preset(string name)
        {
            Name = name;
            var jobs = Enum.GetValues<Job>()
            .Where(job =>
                job != Job.ADV && // Exclude ADV
                !job.IsUpgradeable() && // Exclude upgradeable jobs
                !job.IsDoh() && !job.IsDol()) // Exclude crafting and gathering jobs
            .OrderByDescending(job => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)job).Role);
            JobOrder = jobs.ToList();
        }
    }
}
