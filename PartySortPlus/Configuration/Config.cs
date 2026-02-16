using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartySortPlus.Configuration
{
    [Serializable]
    public class Config: IEzConfig
    {
        public bool Enable = true;
        public bool Debug = false;
        public bool AllowNegativeConditions = false;

        public bool ShowTutorial = true;
        public ImGuiNET.ImGuiComboFlags ComboSize = ImGuiNET.ImGuiComboFlags.HeightLarge;

        public string LastVersion = "0";

        public bool Cond_Enabled = true;
        public bool Cond_Territory = true;
        public bool Cond_Roles = true;
        public bool Cond_Jobs = true;
        public bool Cond_PartyJobs = true;

        public Profile GlobalProfile = new() { Name = "Global Profile" };
    }
}
