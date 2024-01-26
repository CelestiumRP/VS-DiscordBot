﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DiscordBot.Patches;

public class CharacterSystemPatches {
    protected internal CharacterSystemPatches(Harmony harmony) {
        _ = new OnCharacterSelectionPatch(harmony);
    }

    private class OnCharacterSelectionPatch {
        private static readonly List<string> CharacterSelectCache = new();

        public OnCharacterSelectionPatch(Harmony harmony) {
            harmony.Patch(typeof(CharacterSystem).GetMethod("onCharacterSelection", BindingFlags.Instance | BindingFlags.NonPublic),
                prefix: GetType().GetMethod("Prefix"),
                postfix: GetType().GetMethod("Postfix"));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static void Prefix(IServerPlayer fromPlayer, CharacterSelectionPacket p) {
            // remove from cache to prevent abuse
            CharacterSelectCache.Remove(fromPlayer.PlayerUID);

            bool didSelectBefore = SerializerUtil.Deserialize(fromPlayer.GetModdata("createCharacter"), false);
            if (didSelectBefore && fromPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
                return;
            }

            if (p.DidSelect) {
                CharacterSelectCache.Add(fromPlayer.PlayerUID);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static void Postfix(IServerPlayer fromPlayer) {
            if (CharacterSelectCache.Remove(fromPlayer.PlayerUID)) {
                DiscordBotMod.Bot?.OnCharacterSelection(fromPlayer);
            }
        }
    }
}
