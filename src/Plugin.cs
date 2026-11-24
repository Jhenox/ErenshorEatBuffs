using System;
using System.Collections;
using HarmonyLib;
using Lunaris;
using UnityEngine;

namespace ErenshorEatBuffs
{
    internal static class PluginIdentity
    {
        public const string Guid = "com.github.Jhenox.erenshoreatbuffs";
        public const string Version = "0.3.0";
    }

    [LunarisPlugin("Erenshor Eat Buffs", PluginIdentity.Version, "Jhenox", "Refreshes food and drink buffs with /eatbuffs")]
    [LunarisPermission(LunarisPermission.Harmony)]
    public class Plugin : LunarisPlugin
    {
        internal static Plugin Instance;
        private Harmony harmony;
        private bool running;

        // Consumption order: Bread, Brackfruit Juice, Water, Seafruit, Seaspice.
        private static readonly string[] ItemIds =
        {
            "11823624", "4162410", "382775", "10321560", "10091058"
        };

        private void Awake()
        {
            if (Instance != null)
            {
                Logging.LogWarning("Duplicate Eat Buffs plugin detected.");
                enabled = false;
                return;
            }

            Instance = this;
            try
            {
                harmony = new Harmony(PluginIdentity.Guid);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Logging.LogInfo("Erenshor Eat Buffs v" + PluginIdentity.Version + " loaded.");
            }
            catch (Exception ex)
            {
                Logging.LogError("Eat Buffs initialization failed: " + ex);
                Cleanup();
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            Cleanup();
        }

        private void Cleanup()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(PluginIdentity.Guid);
                harmony = null;
            }
            if (Instance == this) Instance = null;
            running = false;
        }

        internal void RunEatBuffs(float thresholdPercent, bool force)
        {
            if (running)
            {
                Log("Eat Buffs is already running.", "yellow");
                return;
            }
            if (!PlayerReady() || GameData.PlayerStats == null)
            {
                Log("Eat Buffs: Player is not ready.", "yellow");
                return;
            }
            StartCoroutine(ConsumeBuffs(thresholdPercent, force));
        }

        private IEnumerator ConsumeBuffs(float thresholdPercent, bool force)
        {
            running = true;
            int consumed = 0, missing = 0, skipped = 0;
            bool stopped = false;
            Log(force ? "Eat Buffs: Force mode." :
                "Eat Buffs: Refresh at " + thresholdPercent.ToString("0.#") + "% or less.", "yellow");

            try
            {
                foreach (string itemId in ItemIds)
                {
                    while (PlayerReady())
                    {
                        CastSpell waitingCaster = GameData.PlayerControl.GetComponent<CastSpell>();
                        if (waitingCaster == null) { stopped = true; break; }
                        if (!waitingCaster.isCasting()) break;
                        yield return null;
                    }
                    if (stopped || !PlayerReady()) { stopped = true; break; }

                    ItemIcon slot = FindItem(itemId);
                    if (slot == null) { missing++; continue; }
                    Item item = slot.MyItem;
                    Spell spell = item.ItemEffectOnClick;
                    if (spell == null)
                    {
                        Log(item.ItemName + ": No usable spell; skipped.", "yellow");
                        skipped++;
                        continue;
                    }

                    if (!force)
                    {
                        float remaining;
                        if (TryGetRemainingPercent(spell, out remaining))
                        {
                            if (remaining > thresholdPercent)
                            {
                                Log(spell.SpellName + ": " + remaining.ToString("0") + "% remaining - skipped.", "yellow");
                                skipped++;
                                continue;
                            }
                            Log(spell.SpellName + ": " + remaining.ToString("0") + "% remaining - refreshing.", "green");
                        }
                        else
                        {
                            Log(spell.SpellName + ": missing - applying.", "green");
                        }
                    }

                    if (GameData.PlayerControl.Myself.MyStats.Stunned ||
                        GameData.PlayerControl.Myself.MyStats.Feared)
                    {
                        Log("Eat Buffs stopped: Unable to cast.", "yellow");
                        stopped = true;
                        break;
                    }

                    CastSpell caster = GameData.PlayerControl.GetComponent<CastSpell>();
                    if (caster == null || !caster.StartSpell(spell,
                        GameData.PlayerControl.Myself.MyStats, item.SpellCastTime))
                    {
                        Log("Eat Buffs stopped: Could not use " + item.ItemName + ".", "yellow");
                        stopped = true;
                        break;
                    }

                    // Only consume inventory after StartSpell confirms success.
                    if (item.Disposable)
                    {
                        slot.Quantity--;
                        if (slot.Quantity <= 0)
                        {
                            slot.Quantity = 0;
                            slot.MyItem = GameData.PlayerInv.Empty;
                        }
                        slot.UpdateSlotImage();
                    }
                    consumed++;
                    Log("Using " + item.ItemName + "...", "green");

                    yield return null;
                    while (PlayerReady() && caster != null && caster.isCasting())
                        yield return null;
                    if (!PlayerReady()) { stopped = true; break; }
                    yield return new WaitForSeconds(0.15f);
                }

                Log(stopped
                    ? "Eat Buffs stopped. Items used: " + consumed + "."
                    : "Eat Buffs complete: " + consumed + " used, " + skipped +
                      " skipped, " + missing + " missing.", stopped ? "yellow" : "green");
            }
            finally
            {
                running = false;
            }
        }

        private static bool TryGetRemainingPercent(Spell spell, out float remainingPercent)
        {
            remainingPercent = 0f;
            if (spell == null || spell.SpellDurationInTicks <= 0 || GameData.PlayerStats == null)
                return false;

            foreach (StatusEffect effect in GameData.PlayerStats.StatusEffects)
            {
                if (effect == null || effect.Effect == null || effect.Duration <= 0f)
                    continue;
                // Compare the live spell asset, avoiding any dependency on exported JSON.
                if (effect.Effect != spell)
                    continue;

                remainingPercent = Mathf.Clamp(
                    effect.Duration * 100f / spell.SpellDurationInTicks, 0f, 100f);
                return true;
            }
            return false;
        }

        private static bool PlayerReady()
        {
            return GameData.PlayerControl != null &&
                   GameData.PlayerControl.Myself != null &&
                   GameData.PlayerControl.Myself.Alive &&
                   GameData.PlayerInv != null;
        }

        private static ItemIcon FindItem(string itemId)
        {
            if (GameData.PlayerInv == null) return null;
            foreach (ItemIcon slot in GameData.PlayerInv.StoredSlots)
            {
                if (slot == null || slot.MyItem == null ||
                    slot.MyItem == GameData.PlayerInv.Empty || slot.Quantity <= 0)
                    continue;
                if (slot.MyItem.Id == itemId) return slot;
            }
            return null;
        }

        private static void Log(string message, string color)
        {
            UpdateSocialLog.LogAdd(new ChatLogLine(
                message, ChatLogLine.LogType.SystemMessages, color));
        }
    }

    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    internal static class SlashCommandPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(TypeText __instance)
        {
            if (__instance == null || __instance.typed == null) return true;
            string text = __instance.typed.text.Trim();
            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !parts[0].Equals("/eatbuffs", StringComparison.OrdinalIgnoreCase))
                return true;

            float threshold = 25f;
            bool force = false;
            if (parts.Length > 2 || (parts.Length == 2 &&
                !(parts[1].Equals("force", StringComparison.OrdinalIgnoreCase) ||
                  (float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                      System.Globalization.CultureInfo.InvariantCulture, out threshold) &&
                   threshold >= 0f && threshold <= 100f))))
            {
                UpdateSocialLog.LogAdd(new ChatLogLine(
                    "Usage: /eatbuffs [0-100|force]", ChatLogLine.LogType.SystemMessages, "yellow"));
                return false;
            }
            if (parts.Length == 2 && parts[1].Equals("force", StringComparison.OrdinalIgnoreCase))
                force = true;
            if (Plugin.Instance != null) Plugin.Instance.RunEatBuffs(threshold, force);
            return false;
        }
    }
}
