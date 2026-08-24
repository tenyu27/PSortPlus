using Dalamud.Plugin;
using ECommons.SimpleGui;
using ECommons.Configuration;
using PSortPlus.Configuration;
using ECommons.Automation.LegacyTaskManager;
using ECommons;
using ECommons.DalamudServices;
using System;
using ECommons.Schedulers;
using ECommons.Logging;
using System.IO.Compression;
using System.IO;
using PSortPlus.GUI;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace PSortPlus;

/**
 * TODO LIST: 
 * - Add in usage of base classes for jobs when sorting.
 * - Figure out drag and drop issue.
 */
public unsafe class PSortPlus: IDalamudPlugin
{
    public static PSortPlus? P;
    public static Config? C;

    public TaskManager? TaskManager;

    public bool SoftForceUpdate = false;
    public bool ForceUpdate = false;

    public PSortPlus(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this, ECommons.Module.DalamudReflector);

        _ = new TickScheduler(() =>
        {
            C = EzConfig.Init<Config>();
            var ver = GetType().Assembly.GetName().Version?.ToString();
            if (C != null && C.LastVersion != ver)
            {
                try
                {
                    using (var fs = new FileStream(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.zip"), FileMode.Create))
                    using (var arch = new ZipArchive(fs, ZipArchiveMode.Create))
                    {
                        arch.CreateEntryFromFile(EzConfig.DefaultConfigurationFileName, EzConfig.DefaultSerializationFactory.DefaultConfigFileName);
                    }
                    C.LastVersion = ver ?? string.Empty; // Ensure non-null assignment
                    DuoLog.Information($"Because plugin version was changed, a backup of your current configuraton has been created.");
                }
                catch (Exception e)
                {
                    e.Log();
                }
            }

            EzConfigGui.Init(UI.DrawMain);
            EzCmd.Add("/psp", OnCommand, "Run a party list sort!"
                + "\n/psp ui → Show the configuration window."
                + "\n/psp config → Show the configuration window."
                + "\n/psp tutorial → Show the tutorial window again."
            );

            new EzFrameworkUpdate(OnUpdate);
            new EzTerritoryChanged(TerritoryChanged);
            TaskManager = new TaskManager()
            {
                TimeLimitMS = 2000,
                AbortOnTimeout = true,
                TimeoutSilently = false,
            };
        });
    }

    public void Dispose()
    {
        EzConfig.Save();
        ECommonsMain.Dispose();
        P = null;
        C = null;
    }

    private void TerritoryChanged(uint id)
    {
        SoftForceUpdate = true;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.EqualsIgnoreCaseAny("debug"))
        {
            if (C != null)
            {
                C.Debug = !C.Debug;
                DuoLog.Information($"Debug mode is now {(C.Debug ? "enabled" : "disabled")}");
            }
        } 
        else if(arguments.EqualsIgnoreCaseAny("ui") || arguments.EqualsIgnoreCaseAny("config"))
        {
            if (EzConfigGui.Window != null)
                EzConfigGui.Window.IsOpen ^= true;
        }
        else if(arguments.EqualsIgnoreCaseAny("tutorial"))
        {
            if (C != null)
            {
                C.ShowTutorial = !C.ShowTutorial;
                DuoLog.Information($"Tutorial mode is now {(C.ShowTutorial ? "enabled" : "disabled")}");
            }
        }
        else
        {
            ForceUpdate = true;
        }
    }

    private void OnUpdate()
    {
        if (C == null) return;
        if (TaskManager == null) return;

        if (Player.Interactable)
        {
            if (!TaskManager.IsBusy)
            {
                List<ApplyRule> newRule = [];
                if (C.Enable)
                {
                    foreach (var r in C.GlobalProfile.Rules)
                    {
                        if (r.Enabled)
                        {
                            newRule.Add(r);
                        }
                    }
                    if (ForceUpdate || (SoftForceUpdate && newRule.Count > 0))
                    {
                        SoftForceUpdate = false;
                        ForceUpdate = false;
                        PluginLog.Debug($"Force updating party list with {newRule.Count} rules.");

                        PluginLog.Debug($"Current territory: {Player.Territory}");
                        PluginLog.Debug($"Current job: {Player.Job}");

                        foreach (ref var partyMember in AgentHUD.Instance()->PartyMembers)
                        {
                            if (partyMember.Object == null) { continue; }
                            PluginLog.Debug($"Current party member: {partyMember.Name}");
                            PluginLog.Debug($"Current party member job: {partyMember.Object->ClassJob}");
                        }

                        foreach (var rule in newRule)
                        {
                            bool skipTerritoryCheck = rule.Territories.Count == 0 || !C.Cond_Territory;
                            bool skipJobCheck = rule.Jobs.Count == 0 || !C.Cond_Jobs;
                            bool skipPartyJobCheck = rule.PartyJobs.Count == 0 || !C.Cond_PartyJobs;

                            PluginLog.Debug($"Checking rule: {rule.GUID}");

                            if (skipTerritoryCheck) { PluginLog.Debug($"Territory check skipped."); }
                            if (skipJobCheck) { PluginLog.Debug($"Job check skipped."); }
                            if (skipPartyJobCheck) { PluginLog.Debug($"Party job check skipped."); }

                            if (!skipTerritoryCheck)
                            {
                                if (!rule.Territories.Contains(Svc.ClientState.TerritoryType)
                                    || (C.AllowNegativeConditions && rule.Not.Territories.Contains(Svc.ClientState.TerritoryType)))
                                {
                                    PluginLog.Debug($"Territory check failed.");
                                    continue;
                                }
                                PluginLog.Debug($"Territory check passed.");
                            }

                            if (!skipJobCheck)
                            {
                                if (!rule.Jobs.Contains(Player.Job)
                                    || (C.AllowNegativeConditions && rule.Not.Jobs.Contains(Player.Job)))
                                {
                                    PluginLog.Debug($"Job check failed.");
                                    continue;
                                }
                                PluginLog.Debug($"Job check passed.");
                            }

                            if (!skipPartyJobCheck)
                            {
                                var partyJobs = GetPartyMemberJobs();
                                bool allRequiredPresent = true;

                                foreach (var requiredJob in rule.PartyJobs)
                                {
                                    if (!partyJobs.Contains(requiredJob.ToString()))
                                    {
                                        allRequiredPresent = false;
                                        PluginLog.Debug($"Party job check failed: required job {requiredJob} not present.");
                                        break;
                                    }
                                }

                                bool forbiddenPresent = false;
                                if (allRequiredPresent && C.AllowNegativeConditions)
                                {
                                    foreach (var forbiddenJob in rule.Not.PartyJobs)
                                    {
                                        if (partyJobs.Contains(forbiddenJob.ToString()))
                                        {
                                            forbiddenPresent = true;
                                            PluginLog.Debug($"Party job check failed: forbidden job {forbiddenJob} present.");
                                            break;
                                        }
                                    }
                                }

                                if (!allRequiredPresent || forbiddenPresent)
                                {
                                    continue;
                                }

                                PluginLog.Debug($"Party job check passed.");
                            }

                            if (rule.SelectedPresets.Count == 0)
                            {
                                PluginLog.Error($"Rule {rule.GUID} has no selected presets.");
                                continue;
                            }

                            string presetNameToUse = rule.SelectedPresets[0];
                            var presetCandidates = C.GlobalProfile.Presets;
                            int presetIndex = presetCandidates.FindIndex(x => x.GUID.EqualsIgnoreCase(presetNameToUse));
                            if (presetIndex == -1)
                            {
                                presetIndex = presetCandidates.FindIndex(x => x.Name.EqualsIgnoreCase(presetNameToUse));
                            }

                            if (presetIndex == -1)
                            {
                                PluginLog.Error($"Preset '{presetNameToUse}' not found for rule {rule.GUID}.");
                                continue;
                            }

                            try
                            {
                                Preset presetToUse = C.GlobalProfile.Presets[presetIndex];
                                SortPartyList(presetToUse);
                                PluginLog.Information($"Processed rule {rule.GUID}. All other rules skipped.");
                            }
                            catch (Exception ex)
                            {
                                PluginLog.Error($"Exception while processing rule {rule.GUID}: {ex.Message}");
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    private void SortPartyList(Preset presetToUse)
    {
        PluginLog.Information($"Sorting party list with preset {presetToUse.Name}.");

        var members = GetPartyMembers();
        if (members.Count == 0)
        {
            return;
        }

        var used = new bool[members.Count];
        var desiredOrder = new List<int>(members.Count);

        foreach (var job in presetToUse.JobOrder)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (!used[i] && members[i].Job.Equals(job.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    desiredOrder.Add(members[i].Index);
                    used[i] = true;
                }
            }
        }

        for (int i = 0; i < members.Count; i++)
        {
            if (!used[i])
            {
                desiredOrder.Add(members[i].Index);
            }
        }

        var jobByIndex = members.ToDictionary(m => m.Index, m => m.Job);
        PluginLog.Debug($"Target job order: {string.Join(", ", desiredOrder.Select(i => jobByIndex.TryGetValue(i, out var j) ? j : "?"))}");
        PluginLog.Debug($"Current job order: {string.Join(", ", members.Select(m => m.Job))}");

        var order = members.Select(m => m.Index).ToList();

        for (int pos = 0; pos < desiredOrder.Count; pos++)
        {
            int cur = order.IndexOf(desiredOrder[pos]);
            if (cur == pos) continue;

            if (cur > pos)
            {
                for (int k = cur - 1; k >= pos; k--)
                {
                    ApplyMove(order, k, k + 1);
                }
            }
            else
            {
                ApplyMove(order, cur, pos);
            }
        }
    }

    private void ApplyMove(List<int> order, int selectedIndex, int targetIndex)
    {
        PluginLog.Debug($"ChangeOrder({selectedIndex} -> {targetIndex})");
        InfoProxyPartyMember.Instance()->ChangeOrder(selectedIndex, targetIndex);
        var moved = order[selectedIndex];
        order.RemoveAt(selectedIndex);
        order.Insert(targetIndex, moved);
    }

    private List<(int Index, string Job)> GetPartyMembers()
    {
        var members = new List<(int Index, string Job)>();

        foreach (ref var partyMember in AgentHUD.Instance()->PartyMembers)
        {
            if (partyMember.Object != null)
            {
                var job = ECommons.ExcelServices.ExcelJobHelper.GetJobById(partyMember.Object->ClassJob);
                if (job.HasValue)
                {
                    members.Add((partyMember.Index, job.Value.Abbreviation.ToString()));
                }
            }
        }

        members.Sort((a, b) => a.Index.CompareTo(b.Index));
        return members;
    }

    private List<string> GetPartyMemberJobs()
    {
        return GetPartyMembers().Select(m => m.Job).ToList();
    }
}
