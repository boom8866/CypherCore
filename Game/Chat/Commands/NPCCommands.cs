﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    [CommandGroup("npc", RBACPermissions.CommandNpc)]
    class NPCCommands
    {
        [Command("info", RBACPermissions.CommandNpcInfo)]
        static bool Info(StringArguments args, CommandHandler handler)
        {
            Creature target = handler.getSelectedCreature();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            CreatureTemplate cInfo = target.GetCreatureTemplate();

            uint faction = target.getFaction();
            ulong npcflags = target.GetUInt64Value(UnitFields.NpcFlags);
            uint mechanicImmuneMask = cInfo.MechanicImmuneMask;
            uint displayid = target.GetDisplayId();
            uint nativeid = target.GetNativeDisplayId();
            uint Entry = target.GetEntry();

            long curRespawnDelay = target.GetRespawnTimeEx() - Time.UnixTime;
            if (curRespawnDelay < 0)
                curRespawnDelay = 0;
            string curRespawnDelayStr = Time.secsToTimeString((ulong)curRespawnDelay, true);
            string defRespawnDelayStr = Time.secsToTimeString(target.GetRespawnDelay(), true);

            handler.SendSysMessage(CypherStrings.NpcinfoChar, target.GetSpawnId(), target.GetGUID().ToString(), faction, npcflags, Entry, displayid, nativeid);
            handler.SendSysMessage(CypherStrings.NpcinfoLevel, target.getLevel());
            handler.SendSysMessage(CypherStrings.NpcinfoEquipment, target.GetCurrentEquipmentId(), target.GetOriginalEquipmentId());
            handler.SendSysMessage(CypherStrings.NpcinfoHealth, target.GetCreateHealth(), target.GetMaxHealth(), target.GetHealth());
            handler.SendSysMessage(CypherStrings.NpcinfoInhabitType, cInfo.InhabitType);

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags, target.GetUInt32Value(UnitFields.Flags));
            foreach (uint value in Enum.GetValues(typeof(UnitFlags)))
                if (target.GetUInt32Value(UnitFields.Flags).HasAnyFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", (UnitFlags)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags2, target.GetUInt32Value(UnitFields.Flags2));
            foreach (uint value in Enum.GetValues(typeof(UnitFlags2)))
                if (target.GetUInt32Value(UnitFields.Flags2).HasAnyFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", (UnitFlags2)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags3, target.GetUInt32Value(UnitFields.Flags3));
            foreach (uint value in Enum.GetValues(typeof(UnitFlags3)))
                if (target.GetUInt32Value(UnitFields.Flags3).HasAnyFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", (UnitFlags3)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoDynamicFlags, target.GetUInt32Value(ObjectFields.DynamicFlags));
            handler.SendSysMessage(CypherStrings.CommandRawpawntimes, defRespawnDelayStr, curRespawnDelayStr);
            handler.SendSysMessage(CypherStrings.NpcinfoLoot, cInfo.LootId, cInfo.PickPocketId, cInfo.SkinLootId);
            handler.SendSysMessage(CypherStrings.NpcinfoDungeonId, target.GetInstanceId());

            CreatureData data = Global.ObjectMgr.GetCreatureData(target.GetSpawnId());
            if (data != null)
            {
                handler.SendSysMessage(CypherStrings.NpcinfoPhases, data.phaseid, data.phaseGroup);
                if (data.phaseGroup != 0)
                {
                    var _phases = target.GetPhases();
                    if (!_phases.Empty())
                    {
                        handler.SendSysMessage(CypherStrings.NpcinfoPhaseIds);
                        foreach (uint phaseId in _phases)
                            handler.SendSysMessage("{0}", phaseId);
                    }
                }
            }

            handler.SendSysMessage(CypherStrings.NpcinfoArmor, target.GetArmor());
            handler.SendSysMessage(CypherStrings.NpcinfoPosition, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ());
            handler.SendSysMessage(CypherStrings.NpcinfoAiinfo, target.GetAIName(), target.GetScriptName());
            handler.SendSysMessage(CypherStrings.NpcinfoFlagsExtra, cInfo.FlagsExtra);
            foreach (uint value in Enum.GetValues(typeof(CreatureFlagsExtra)))
                if (cInfo.FlagsExtra.HasAnyFlag((CreatureFlagsExtra)value))
                    handler.SendSysMessage("{0} (0x{1:X})", (CreatureFlagsExtra)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoNpcFlags, npcflags);
            foreach (ulong value in Enum.GetValues(typeof(NPCFlags)))
                if (npcflags.HasAnyFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", (NPCFlags)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoMechanicImmune, mechanicImmuneMask);
            foreach (int value in Enum.GetValues(typeof(Mechanics)))
                if (Convert.ToBoolean(mechanicImmuneMask & (1 << (value - 1))))
                    handler.SendSysMessage("{0} (0x{1:X})", (Mechanics)value, value);

            return true;
        }

        [Command("move", RBACPermissions.CommandNpcMove)]
        static bool Move(StringArguments args, CommandHandler handler)
        {
            ulong lowguid = 0;

            Creature creature = handler.getSelectedCreature();
            if (!creature)
            {
                // number or [name] Shift-click form |color|Hcreature:creature_guid|h[name]|h|r
                string cId = handler.extractKeyFromLink(args, "Hcreature");
                if (string.IsNullOrEmpty(cId))
                    return false;

                lowguid = uint.Parse(cId);

                // Attempting creature load from DB data
                CreatureData data = Global.ObjectMgr.GetCreatureData(lowguid);
                if (data == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);
                    return false;
                }

                uint map_id = data.mapid;
                if (handler.GetSession().GetPlayer().GetMapId() != map_id)
                {
                    handler.SendSysMessage(CypherStrings.CommandCreatureatsamemap, lowguid);
                    return false;
                }

            }
            else
            {
                lowguid = creature.GetSpawnId();
            }

            float x = handler.GetPlayer().GetPositionX();
            float y = handler.GetPlayer().GetPositionY();
            float z = handler.GetPlayer().GetPositionZ();
            float o = handler.GetPlayer().GetOrientation();

            if (creature)
            {
                CreatureData data = Global.ObjectMgr.GetCreatureData(creature.GetSpawnId());
                if (data != null)
                {
                    data.posX = x;
                    data.posY = y;
                    data.posZ = z;
                    data.orientation = o;
                }
                creature.SetPosition(x, y, z, o);
                creature.GetMotionMaster().Initialize();
                if (creature.IsAlive())                            // dead creature will reset movement generator at respawn
                {
                    creature.setDeathState(DeathState.JustDied);
                    creature.Respawn();
                }
            }

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_POSITION);

            stmt.AddValue(0, x);
            stmt.AddValue(1, y);
            stmt.AddValue(2, z);
            stmt.AddValue(3, o);
            stmt.AddValue(4, lowguid);

            DB.World.Execute(stmt);

            handler.SendSysMessage(CypherStrings.CommandCreaturemoved);
            return true;
        }

        [Command("near", RBACPermissions.CommandNpcNear)]
        static bool Near(StringArguments args, CommandHandler handler)
        {
            float distance = args.Empty() ? 10.0f : args.NextSingle();
            uint count = 0;

            Player player = handler.GetPlayer();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_CREATURE_NEAREST);
            stmt.AddValue(0, player.GetPositionX());
            stmt.AddValue(1, player.GetPositionY());
            stmt.AddValue(2, player.GetPositionZ());
            stmt.AddValue(3, player.GetMapId());
            stmt.AddValue(4, player.GetPositionX());
            stmt.AddValue(5, player.GetPositionY());
            stmt.AddValue(6, player.GetPositionZ());
            stmt.AddValue(7, distance * distance);
            SQLResult result = DB.World.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    ulong guid = result.Read<ulong>(0);
                    uint entry = result.Read<uint>(1);
                    float x = result.Read<float>(2);
                    float y = result.Read<float>(3);
                    float z = result.Read<float>(4);
                    ushort mapId = result.Read<ushort>(5);

                    CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
                    if (creatureTemplate == null)
                        continue;

                    handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, creatureTemplate.Name, x, y, z, mapId);

                    ++count;
                }
                while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandNearNpcMessage, distance, count);

            return true;
        }

        [Command("playemote", RBACPermissions.CommandNpcPlayemote)]
        static bool PlayEmote(StringArguments args, CommandHandler handler)
        {
            uint emote = args.NextUInt32();

            Creature target = handler.getSelectedCreature();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            target.SetUInt32Value(UnitFields.NpcEmotestate, emote);

            return true;
        }

        [Command("textemote", RBACPermissions.CommandNpcTextemote)]
        static bool TextEmote(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Creature creature = handler.getSelectedCreature();

            if (!creature)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            creature.TextEmote(args.NextString());

            return true;
        }

        [Command("say", RBACPermissions.CommandNpcSay)]
        static bool Say(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Creature creature = handler.getSelectedCreature();
            if (!creature)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }
            string text = args.NextString();
            creature.Say(text, Language.Universal);

            // make some emotes
            switch (text.LastOrDefault())
            {
                case '?':
                    creature.HandleEmoteCommand(Emote.OneshotQuestion);
                    break;
                case '!':
                    creature.HandleEmoteCommand(Emote.OneshotExclamation);
                    break;
                default:
                    creature.HandleEmoteCommand(Emote.OneshotTalk);
                    break;
            }

            return true;
        }

        [Command("whisper", RBACPermissions.CommandNpcWhisper)]
        static bool Whisper(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string receiver_str = args.NextString();
            string text = args.NextString();

            if (string.IsNullOrEmpty(receiver_str) || string.IsNullOrEmpty(text))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            Creature creature = handler.getSelectedCreature();
            if (!creature)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            ObjectGuid receiver_guid = ObjectGuid.Create(HighGuid.Player, ulong.Parse(receiver_str));

            // check online security
            Player receiver = Global.ObjAccessor.FindPlayer(receiver_guid);
            if (handler.HasLowerSecurity(receiver, ObjectGuid.Empty))
                return false;

            creature.Whisper(text, Language.Universal, receiver);
            return true;
        }

        [Command("yell", RBACPermissions.CommandNpcYell)]
        static bool Yell(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Creature creature = handler.getSelectedCreature();
            if (!creature)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            creature.Yell(args.NextString(), Language.Universal);

            // make an emote
            creature.HandleEmoteCommand(Emote.OneshotShout);

            return true;
        }

        [Command("tame", RBACPermissions.CommandNpcTame)]
        static bool Tame(StringArguments args, CommandHandler handler)
        {
            Creature creatureTarget = handler.getSelectedCreature();
            if (!creatureTarget || creatureTarget.IsPet())
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            Player player = handler.GetSession().GetPlayer();

            if (!player.GetPetGUID().IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.YouAlreadyHavePet);
                return false;
            }

            CreatureTemplate cInfo = creatureTarget.GetCreatureTemplate();

            if (!cInfo.IsTameable(player.CanTameExoticPets()))
            {
                handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);
                return false;
            }

            // Everything looks OK, create new pet
            Pet pet = player.CreateTamedPetFrom(creatureTarget);
            if (!pet)
            {
                handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);
                return false;
            }

            // place pet before player
            float x, y, z;
            player.GetClosePoint(out x, out y, out z, creatureTarget.GetObjectSize(), SharedConst.ContactDistance);
            pet.Relocate(x, y, z, MathFunctions.PI - player.GetOrientation());

            // set pet to defensive mode by default (some classes can't control controlled pets in fact).
            pet.SetReactState(ReactStates.Defensive);

            // calculate proper level
            uint level = (creatureTarget.getLevel() < (player.getLevel() - 5)) ? (player.getLevel() - 5) : creatureTarget.getLevel();

            // prepare visual effect for levelup
            pet.SetUInt32Value(UnitFields.Level, level - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetUInt32Value(UnitFields.Level, level);

            // caster have pet now
            player.SetMinion(pet, true);

            pet.SavePetToDB(PetSaveMode.AsCurrent);
            player.PetSpellInitialize();

            return true;
        }

        [Command("evade", RBACPermissions.CommandNpcEvade)]
        static bool HandleNpcEvadeCommand(StringArguments args, CommandHandler handler)
        {
            Creature creatureTarget = handler.getSelectedCreature();
            if (!creatureTarget || creatureTarget.IsPet())
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (!creatureTarget.IsAIEnabled)
            {
                handler.SendSysMessage(CypherStrings.CreatureNotAiEnabled);
                return false;
            }

            string type_str = args.NextString();
            string force_str = args.NextString();

            EvadeReason why = EvadeReason.Other;
            bool force = false;
            if (!type_str.IsEmpty())
            {
                if (type_str.Equals("NO_HOSTILES") || type_str.Equals("EVADE_REASON_NO_HOSTILES"))
                    why = EvadeReason.NoHostiles;
                else if (type_str.Equals("BOUNDARY") || type_str.Equals("EVADE_REASON_BOUNDARY"))
                    why = EvadeReason.Boundary;
                else if (type_str.Equals("SEQUENCE_BREAK") || type_str.Equals("EVADE_REASON_SEQUENCE_BREAK"))
                    why = EvadeReason.SequenceBreak;
                else if (type_str.Equals("FORCE"))
                    force = true;

                if (!force && !force_str.IsEmpty())
                    if (force_str.Equals("FORCE"))
                        force = true;
            }

            if (force)
                creatureTarget.ClearUnitState(UnitState.Evade);
            creatureTarget.GetAI().EnterEvadeMode(why);

            return true;
        }

        [CommandGroup("follow", RBACPermissions.CommandNpcFollow)]
        class FollowCommands
        {
            [Command("", RBACPermissions.CommandNpcFollow)]
            static bool HandleNpcFollowCommand(StringArguments args, CommandHandler handler)
            {
                Player player = handler.GetSession().GetPlayer();
                Creature creature = handler.getSelectedCreature();

                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                // Follow player - Using pet's default dist and angle
                creature.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, creature.GetFollowAngle());

                handler.SendSysMessage(CypherStrings.CreatureFollowYouNow, creature.GetName());
                return true;
            }

            [Command("stop", RBACPermissions.CommandNpcFollowStop)]
            static bool HandleNpcUnFollowCommand(StringArguments args, CommandHandler handler)
            {
                Player player = handler.GetPlayer();
                Creature creature = handler.getSelectedCreature();

                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                if (/*creature.GetMotionMaster().empty() ||*/
                    creature.GetMotionMaster().GetCurrentMovementGeneratorType() != MovementGeneratorType.Follow)
                {
                    handler.SendSysMessage(CypherStrings.CreatureNotFollowYou, creature.GetName());
                    return false;
                }

                FollowMovementGenerator<Creature> mgen = (FollowMovementGenerator<Creature>)creature.GetMotionMaster().top();

                if (mgen.target != player)
                {
                    handler.SendSysMessage(CypherStrings.CreatureNotFollowYou, creature.GetName());
                    return false;
                }

                // reset movement
                creature.GetMotionMaster().MovementExpired(true);

                handler.SendSysMessage(CypherStrings.CreatureNotFollowYouNow, creature.GetName());
                return true;
            }
        }

        [CommandGroup("delete", RBACPermissions.CommandNpcDelete)]
        class DeleteCommands
        {
            [Command("delete", RBACPermissions.CommandNpcDelete)]
            static bool HandleNpcDeleteCommand(StringArguments args, CommandHandler handler)
            {
                Creature unit = null;

                if (!args.Empty())
                {
                    // number or [name] Shift-click form |color|Hcreature:creature_guid|h[name]|h|r
                    string cId = handler.extractKeyFromLink(args, "Hcreature");
                    if (string.IsNullOrEmpty(cId))
                        return false;

                    ulong lowguid = ulong.Parse(cId);
                    if (lowguid == 0)
                        return false;

                    unit = handler.GetCreatureFromPlayerMapByDbGuid(lowguid);
                }
                else
                    unit = handler.getSelectedCreature();

                if (!unit || unit.IsPet() || unit.IsTotem())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                // Delete the creature
                unit.CombatStop();
                unit.DeleteFromDB();
                unit.AddObjectToRemoveList();

                handler.SendSysMessage(CypherStrings.CommandDelcreatmessage);

                return true;
            }

            [Command("item", RBACPermissions.CommandNpcDeleteItem)]
            static bool HandleNpcDeleteVendorItemCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                Creature vendor = handler.getSelectedCreature();
                if (!vendor || !vendor.IsVendor())
                {
                    handler.SendSysMessage(CypherStrings.CommandVendorselection);
                    return false;
                }

                string pitem = handler.extractKeyFromLink(args, "Hitem");
                if (string.IsNullOrEmpty(pitem))
                {
                    handler.SendSysMessage(CypherStrings.CommandNeeditemsend);
                    return false;
                }
                uint itemId = uint.Parse(pitem);

                ItemVendorType type = ItemVendorType.Item; // FIXME: make type (1 item, 2 currency) an argument

                if (!Global.ObjectMgr.RemoveVendorItem(vendor.GetEntry(), itemId, type))
                {
                    handler.SendSysMessage(CypherStrings.ItemNotInList, itemId);
                    return false;
                }

                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

                handler.SendSysMessage(CypherStrings.ItemDeletedFromList, itemId, itemTemplate.GetName());
                return true;
            }
        }

        [CommandGroup("set", RBACPermissions.CommandNpcSet, true)]
        class SetCommands
        {
            [Command("allowmove", RBACPermissions.CommandNpcSetAllowmove)]
            static bool HandleNpcSetAllowMovementCommand(StringArguments args, CommandHandler handler)
            {
                /*
                if (Global.WorldMgr.getAllowMovement())
                {
                    Global.WorldMgr.SetAllowMovement(false);
                    handler.SendSysMessage(LANG_CREATURE_MOVE_DISABLED);
                }
                else
                {
                    Global.WorldMgr.SetAllowMovement(true);
                    handler.SendSysMessage(LANG_CREATURE_MOVE_ENABLED);
                }
                */
                return true;
            }

            [Command("entry", RBACPermissions.CommandNpcSetEntry)]
            static bool SetEntry(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint newEntryNum = args.NextUInt32();
                if (newEntryNum == 0)
                    return false;

                Unit unit = handler.getSelectedUnit();
                if (!unit || !unit.IsTypeId(TypeId.Unit))
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }
                Creature creature = unit.ToCreature();
                if (creature.UpdateEntry(newEntryNum))
                    handler.SendSysMessage(CypherStrings.Done);
                else
                    handler.SendSysMessage(CypherStrings.Error);
                return true;

            }

            [Command("level", RBACPermissions.CommandNpcSetLevel)]
            static bool SetLevel(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                byte lvl = args.NextByte();
                if (lvl < 1 || lvl > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel) + 3)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                Creature creature = handler.getSelectedCreature();
                if (!creature || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetMaxHealth((uint)(100 + 30 * lvl));
                creature.SetHealth((uint)(100 + 30 * lvl));
                creature.SetLevel(lvl);
                creature.SaveToDB();

                return true;
            }

            [Command("flag", RBACPermissions.CommandNpcSetFlag)]
            static bool SetFlag(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                ulong npcFlags = args.NextUInt64();

                Creature creature = handler.getSelectedCreature();
                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetUInt64Value(UnitFields.NpcFlags, npcFlags);

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_NPCFLAG);
                stmt.AddValue(0, npcFlags);
                stmt.AddValue(1, creature.GetEntry());
                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.ValueSavedRejoin);

                return true;
            }

            [Command("factionid", RBACPermissions.CommandNpcSetFactionid)]
            static bool SetFaction(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint factionId = args.NextUInt32();

                if (!CliDB.FactionTemplateStorage.ContainsKey(factionId))
                {
                    handler.SendSysMessage(CypherStrings.WrongFaction, factionId);
                    return false;
                }

                Creature creature = handler.getSelectedCreature();

                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetFaction(factionId);

                // Faction is set in creature_template - not inside creature

                // Update in memory..
                CreatureTemplate cinfo = creature.GetCreatureTemplate();
                if (cinfo != null)
                    cinfo.Faction = factionId;

                // ..and DB
                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_FACTION);

                stmt.AddValue(0, factionId);
                stmt.AddValue(1, factionId);
                stmt.AddValue(2, creature.GetEntry());

                DB.World.Execute(stmt);

                return true;
            }

            [Command("data", RBACPermissions.CommandNpcSetData)]
            static bool SetData(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string arg1 = args.NextString();
                string arg2 = args.NextString();

                if (string.IsNullOrEmpty(arg1) || string.IsNullOrEmpty(arg2))
                    return false;

                uint data_1 = uint.Parse(arg1);
                uint data_2 = uint.Parse(arg2);

                if (data_1 == 0 || data_2 == 0)
                    return false;

                Creature creature = handler.getSelectedCreature();

                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.GetAI().SetData(data_1, data_2);
                string AIorScript = creature.GetAIName() != "" ? "AI type: " + creature.GetAIName() : (creature.GetScriptName() != "" ? "Script Name: " + creature.GetScriptName() : "No AI or Script Name Set");
                handler.SendSysMessage(CypherStrings.NpcSetdata, creature.GetGUID(), creature.GetEntry(), creature.GetName(), data_1, data_2, AIorScript);
                return true;
            }

            [Command("model", RBACPermissions.CommandNpcSetModel)]
            static bool SetModel(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint displayId = args.NextUInt32();

                Creature creature = handler.getSelectedCreature();

                if (!creature || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetDisplayId(displayId);
                creature.SetNativeDisplayId(displayId);

                creature.SaveToDB();

                return true;
            }

            [Command("movetype", RBACPermissions.CommandNpcSetMovetype)]
            static bool SetMoveType(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                // 3 arguments:
                // GUID (optional - you can also select the creature)
                // stay|random|way (determines the kind of movement)
                // NODEL (optional - tells the system NOT to delete any waypoints)
                //        this is very handy if you want to do waypoints, that are
                //        later switched on/off according to special events (like escort
                //        quests, etc)
                string guid_str = args.NextString();
                string type = args.NextString();
                string dontdel_str = args.NextString();

                bool doNotDelete = false;

                if (string.IsNullOrEmpty(guid_str))
                    return false;

                ulong lowguid = 0;
                Creature creature = null;

                if (!string.IsNullOrEmpty(dontdel_str))
                {
                    Log.outDebug(LogFilter.Misc, "DEBUG: All 3 params are set");

                    // All 3 params are set
                    // GUID
                    // type
                    // doNotDEL
                    if (dontdel_str == "NODEL")
                        doNotDelete = true;
                }
                else
                {
                    // Only 2 params - but maybe NODEL is set
                    if (!string.IsNullOrEmpty(type))
                    {
                        Log.outDebug(LogFilter.Server, "DEBUG: Only 2 params ");
                        if (type == "NODEL")
                        {
                            doNotDelete = true;
                            type = null;
                        }
                    }
                }

                if (string.IsNullOrEmpty(type))                                           // case .setmovetype $move_type (with selected creature)
                {
                    type = guid_str;
                    creature = handler.getSelectedCreature();
                    if (!creature || creature.IsPet())
                        return false;
                    lowguid = creature.GetSpawnId();
                }
                else                                                    // case .setmovetype #creature_guid $move_type (with selected creature)
                {
                    lowguid = uint.Parse(guid_str);

                    if (lowguid != 0)
                        creature = handler.GetCreatureFromPlayerMapByDbGuid(lowguid);

                    // attempt check creature existence by DB data
                    if (!creature)
                    {
                        CreatureData data = Global.ObjectMgr.GetCreatureData(lowguid);
                        if (data == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);
                            return false;
                        }
                    }
                    else
                    {
                        lowguid = creature.GetSpawnId();
                    }
                }

                // now lowguid is low guid really existed creature
                // and creature point (maybe) to this creature or NULL

                MovementGeneratorType move_type;

                if (type == "stay")
                    move_type = MovementGeneratorType.Idle;
                else if (type == "random")
                    move_type = MovementGeneratorType.Random;
                else if (type == "way")
                    move_type = MovementGeneratorType.Waypoint;
                else
                    return false;

                if (creature)
                {
                    // update movement type
                    if (doNotDelete == false)
                        creature.LoadPath(0);

                    creature.SetDefaultMovementType(move_type);
                    creature.GetMotionMaster().Initialize();
                    if (creature.IsAlive())                            // dead creature will reset movement generator at respawn
                    {
                        creature.setDeathState(DeathState.JustDied);
                        creature.Respawn();
                    }
                    creature.SaveToDB();
                }
                if (doNotDelete == false)
                {
                    handler.SendSysMessage(CypherStrings.MoveTypeSet, type);
                }
                else
                {
                    handler.SendSysMessage(CypherStrings.MoveTypeSetNodel, type);
                }

                return true;
            }

            [Command("phase", RBACPermissions.CommandNpcSetPhase)]
            static bool HandleNpcSetPhaseCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint phaseId = args.NextUInt32();
                if (!CliDB.PhaseStorage.ContainsKey(phaseId))
                {
                    handler.SendSysMessage(CypherStrings.PhaseNotfound);
                    return false;
                }

                Creature creature = handler.getSelectedCreature();
                if (!creature || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.ClearPhases();
                creature.SetInPhase(phaseId, true, true);
                creature.SetDBPhase((int)phaseId);

                creature.SaveToDB();
                return true;
            }

            [Command("phasegroup", RBACPermissions.CommandNpcSetPhase)]
            static bool HandleNpcSetPhaseGroup(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                int phaseGroupId = args.NextInt32();

                Creature creature = handler.getSelectedCreature();
                if (!creature || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.ClearPhases();

                foreach (uint id in Global.DB2Mgr.GetPhasesForGroup((uint)phaseGroupId))
                    creature.SetInPhase(id, false, true); // don't send update here for multiple phases, only send it once after adding all phases

                creature.UpdateObjectVisibility();
                creature.SetDBPhase(-phaseGroupId);

                creature.SaveToDB();

                return true;
            }

            [Command("spawndist", RBACPermissions.CommandNpcSetSpawndist)]
            static bool SetSpawnDist(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                float option = args.NextSingle();
                if (option < 0.0f)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                MovementGeneratorType mtype = MovementGeneratorType.Idle;
                if (option > 0.0f)
                    mtype = MovementGeneratorType.Random;

                Creature creature = handler.getSelectedCreature();
                ulong guidLow = 0;

                if (creature)
                    guidLow = creature.GetSpawnId();
                else
                    return false;

                creature.SetRespawnRadius(option);
                creature.SetDefaultMovementType(mtype);
                creature.GetMotionMaster().Initialize();
                if (creature.IsAlive())                                // dead creature will reset movement generator at respawn
                {
                    creature.setDeathState(DeathState.JustDied);
                    creature.Respawn();
                }

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_SPAWN_DISTANCE);
                stmt.AddValue(0, option);
                stmt.AddValue(1, mtype);
                stmt.AddValue(2, guidLow);

                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.CommandSpawndist, option);
                return true;
            }

            [Command("spawntime", RBACPermissions.CommandNpcSetSpawntime)]
            static bool SetSpawnTime(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint spawnTime = args.NextUInt32();

                Creature creature = handler.getSelectedCreature();
                if (!creature)
                    return false;

                ulong guidLow = creature.GetSpawnId();

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_SPAWN_TIME_SECS);
                stmt.AddValue(0, spawnTime);
                stmt.AddValue(1, guidLow);
                DB.World.Execute(stmt);

                creature.SetRespawnDelay(spawnTime);
                handler.SendSysMessage(CypherStrings.CommandSpawntime, spawnTime);

                return true;
            }

            [Command("link", RBACPermissions.CommandNpcSetLink)]
            static bool SetLink(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                ulong linkguid = args.NextUInt64();

                Creature creature = handler.getSelectedCreature();
                if (!creature)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                if (creature.GetSpawnId() == 0)
                {
                    handler.SendSysMessage("Selected creature {0} isn't in creature table", creature.GetGUID().ToString());
                    return false;
                }

                if (!Global.ObjectMgr.SetCreatureLinkedRespawn(creature.GetSpawnId(), linkguid))
                {
                    handler.SendSysMessage("Selected creature can't link with guid '{0}'", linkguid);
                    return false;
                }

                handler.SendSysMessage("LinkGUID '{0}' added to creature with DBTableGUID: '{1}'", linkguid, creature.GetSpawnId());
                return true;
            }
        }

        [CommandGroup("add", RBACPermissions.CommandNpcAdd)]
        class AddCommands
        {
            [Command("", RBACPermissions.CommandNpcAdd)]
            static bool Default(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string charID = handler.extractKeyFromLink(args, "Hcreature_entry");
                if (string.IsNullOrEmpty(charID))
                    return false;

                uint id = uint.Parse(charID);
                if (Global.ObjectMgr.GetCreatureTemplate(id) == null)
                    return false;

                Player chr = handler.GetSession().GetPlayer();
                float x = chr.GetPositionX();
                float y = chr.GetPositionY();
                float z = chr.GetPositionZ();
                float o = chr.GetOrientation();
                Map map = chr.GetMap();

                Transport trans = chr.GetTransport();
                if (trans)
                {
                    ulong guid = map.GenerateLowGuid(HighGuid.Creature);
                    CreatureData data = Global.ObjectMgr.NewOrExistCreatureData(guid);
                    data.id = id;
                    data.phaseMask = chr.GetPhaseMask();
                    data.posX = chr.GetTransOffsetX();
                    data.posY = chr.GetTransOffsetY();
                    data.posZ = chr.GetTransOffsetZ();
                    data.orientation = chr.GetTransOffsetO();

                    Creature _creature = trans.CreateNPCPassenger(guid, data);

                    _creature.SaveToDB((uint)trans.GetGoInfo().MoTransport.SpawnMap, (byte)(1 << (int)map.GetSpawnMode()), chr.GetPhaseMask());

                    Global.ObjectMgr.AddCreatureToGrid(guid, data);
                    return true;
                }

                Creature creature = new Creature();
                if (!creature.Create(map.GenerateLowGuid(HighGuid.Creature), map, chr.GetPhaseMask(), id, x, y, z, o))
                    return false;

                creature.SaveToDB(map.GetId(), (byte)(1 << (int)map.GetSpawnMode()), chr.GetPhaseMask());

                ulong db_guid = creature.GetSpawnId();

                // To call _LoadGoods(); _LoadQuests(); CreateTrainerSpells()
                // current "creature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                creature.CleanupsBeforeDelete();
                creature = new Creature();
                if (!creature.LoadCreatureFromDB(db_guid, map))
                    return false;

                Global.ObjectMgr.AddCreatureToGrid(db_guid, Global.ObjectMgr.GetCreatureData(db_guid));
                return true;
            }

            [Command("item", RBACPermissions.CommandNpcAddItem)]
            static bool AddItem(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                byte type = 1; // FIXME: make type (1 item, 2 currency) an argument

                string pitem = handler.extractKeyFromLink(args, "Hitem");
                if (string.IsNullOrEmpty(pitem))
                {
                    handler.SendSysMessage(CypherStrings.CommandNeeditemsend);
                    return false;
                }

                uint itemId = uint.Parse(pitem);
                if (itemId == 0)
                    return false;

                uint maxcount = args.NextUInt32();
                uint incrtime = args.NextUInt32();
                uint extendedcost = args.NextUInt32();

                Creature vendor = handler.getSelectedCreature();
                if (!vendor)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                uint vendor_entry = vendor.GetEntry();

                if (!Global.ObjectMgr.IsVendorItemValid(vendor_entry, itemId, (int)maxcount, incrtime, extendedcost, (ItemVendorType)type, handler.GetSession().GetPlayer()))
                    return false;

                Global.ObjectMgr.AddVendorItem(vendor_entry, itemId, (int)maxcount, incrtime, extendedcost, (ItemVendorType)type);

                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

                handler.SendSysMessage(CypherStrings.ItemAddedToList, itemId, itemTemplate.GetName(), maxcount, incrtime, extendedcost);
                return true;
            }

            [Command("move", RBACPermissions.CommandNpcAddMove)]
            static bool AddMove(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                ulong lowGuid = args.NextUInt64();
                string waitStr = args.NextString();

                // attempt check creature existence by DB data
                CreatureData data = Global.ObjectMgr.GetCreatureData(lowGuid);
                if (data == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowGuid);
                    return false;
                }

                int wait = !string.IsNullOrEmpty(waitStr) ? int.Parse(waitStr) : 0;
                if (wait < 0)
                    wait = 0;

                // Update movement type
                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE);
                stmt.AddValue(0, MovementGeneratorType.Waypoint);
                stmt.AddValue(1, lowGuid);
                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.WaypointAdded);

                return true;
            }

            [Command("formation", RBACPermissions.CommandNpcAddFormation)]
            static bool AddFormation(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                uint leaderGUID = args.NextUInt32();
                Creature creature = handler.getSelectedCreature();

                if (!creature || creature.GetSpawnId() == 0)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                ulong lowguid = creature.GetSpawnId();
                if (creature.GetFormation() != null)
                {
                    handler.SendSysMessage("Selected creature is already member of group {0}", creature.GetFormation().GetId());
                    return false;
                }

                if (lowguid == 0)
                    return false;

                Player chr = handler.GetSession().GetPlayer();
                FormationInfo group_member = new FormationInfo();
                group_member.follow_angle = (creature.GetAngle(chr) - chr.GetOrientation()) * 180 / MathFunctions.PI;
                group_member.follow_dist = (float)Math.Sqrt(Math.Pow(chr.GetPositionX() - creature.GetPositionX(), 2) + Math.Pow(chr.GetPositionY() - creature.GetPositionY(), 2));
                group_member.leaderGUID = leaderGUID;
                group_member.groupAI = 0;

                FormationMgr.CreatureGroupMap[lowguid] = group_member;
                creature.SearchFormation();

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.INS_CREATURE_FORMATION);
                stmt.AddValue(0, leaderGUID);
                stmt.AddValue(1, lowguid);
                stmt.AddValue(2, group_member.follow_dist);
                stmt.AddValue(3, group_member.follow_angle);
                stmt.AddValue(4, group_member.groupAI);

                DB.World.Execute(stmt);

                handler.SendSysMessage("Creature {0} added to formation with leader {1}", lowguid, leaderGUID);

                return true;
            }

            [Command("temp", RBACPermissions.CommandNpcAddTemp)]
            static bool HandleNpcAddTempSpawnCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                bool loot = false;
                string spawntype_str = args.NextString();
                if (spawntype_str.Equals("LOOT"))
                    loot = true;
                else if (spawntype_str.Equals("NOLOOT"))
                    loot = false;

                string charID = handler.extractKeyFromLink(args, "Hcreature_entry");
                if (string.IsNullOrEmpty(charID))
                    return false;

                Player chr = handler.GetSession().GetPlayer();

                uint id = uint.Parse(charID);
                if (id == 0)
                    return false;

                if (Global.ObjectMgr.GetCreatureTemplate(id) == null)
                    return false;

                chr.SummonCreature(id, chr, loot ? TempSummonType.CorpseTimedDespawn : TempSummonType.CorpseDespawn, 30 * Time.InMilliseconds);

                return true;
            }
        }
    }
}
