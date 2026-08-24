using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PSortPlus.Configuration
{
    public class Profile
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        public List<ApplyRule> Rules = [];
        public List<Preset> Presets = [];

        // UI state only. Serializing these would turn SelectedPreset into a detached
        // clone on load, disconnecting preset edits in the GUI from the actual Presets list.
        [JsonIgnore] public Preset? SelectedPreset = null;
        [JsonIgnore] public bool isEditingPresetName = false;
    }
}
