// -----------------------------------------------------------------------
// <copyright file="ArgumentsParser.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using Discord;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.API.Helpers.BlockParser;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using PlayerRoles;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using System.Linq;
using Interactables.Interobjects.DoorUtils;

namespace ThaumielMapEditor.API.Helpers
{
    public class ArgumentsParser
    {
        public static ConcurrentDictionary<string, List<object>> Cache { get; set; } = [];

        public static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static List<object> Load(BlockyPayload payload)
        {
            if (payload.Language != "yaml")
                return [];

            // Cache by the raw source so repeated payload instances (e.g. spawned per schematic) reuse the parse.
            return Cache.GetOrAdd(payload.Code, static code =>
            {
                List<object> parsedBlocks = [];
                List<Dictionary<string, object>> values = Deserializer.Deserialize<List<Dictionary<string, object>>>(code);
                if (values == null)
                    return parsedBlocks;

                foreach (Dictionary<string, object> dict in values)
                {
                    object? result = ParseBlock(dict);

                    if (result is UnknownBlock)
                    {
                        string? blockType = dict.TryGetValue("type", out object? t) ? t?.ToString() : null;

                        if (blockType != null && blockType.StartsWith("enum_"))
                        {
                            result = blockType.EndsWith("_combine")
                                ? new EnumCombineBlock
                                {
                                    InputA = ParseValue(dict.GetValueOrDefault("A")),
                                    InputB = ParseValue(dict.GetValueOrDefault("B"))
                                }
                                : new EnumBlock
                                {
                                    Value = dict.GetValueOrDefault("Value")
                                };
                        }
                    }

                    if (result != null)
                    {
                        parsedBlocks.Add(result);
                    }
                }

                return parsedBlocks;
            });
        }

        private static readonly Dictionary<string, Func<Dictionary<string, object>, object?>> BlockParsers = new()
        {
            ["math_number"] = d => new MathNumberBlock { Num = ParseFloat(d, "NUM") },
            ["text"] = d => new TextBlock { Text = GetString(d, "TEXT") },
            ["logic_boolean"] = d => new LogicBooleanBlock { Value = ParseBool(d, "BOOL") },
            ["math_random_float"] = d => new MathRandomFloatBlock(),
            ["unity_delta"] = d => new UnityDeltaBlock(),
            ["unity_fixed_delta"] = d => new UnityFixedDeltaBlock(),
            ["unity_time"] = d => new UnityTimeBlock(),
            ["unity_vec3_zero"] = d => new UnityVec3ZeroBlock(),
            ["unity_vec3_up"] = d => new UnityVec3UpBlock(),
            ["player_list"] = d => new PlayerListBlock(),
            ["speaker_pause"] = d => new SpeakerPauseBlock(),
            ["speaker_unpause"] = d => new SpeakerUnpauseBlock(),
            ["door_open"] = d => new OpenDoorBlock(),
            ["door_close"] = d => new CloseDoorBlock(),
            ["door_lock"] = d => new LockDoorBlock(),
            ["door_unlock"] = d => new UnlockDoorBlock(),

            ["math_arithmetic"] = d => new MathArithmeticBlock
            {
                OP = GetString(d, "OP"),
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B"))
            },

            ["math_single"] = d => new MathSingleBlock
            {
                OP = GetString(d, "OP"),
                NUM = ParseValue(d.GetValueOrDefault("NUM"))
            },

            ["math_trig"] = d => new MathTrigBlock
            {
                OP = GetString(d, "OP"),
                NUM = ParseValue(d.GetValueOrDefault("NUM"))
            },

            ["math_round"] = d => new MathRoundBlock
            {
                OP = GetString(d, "OP"),
                NUM = ParseValue(d.GetValueOrDefault("NUM"))
            },

            ["math_modulo"] = d => new MathModuloBlock
            {
                DIVIDEND = ParseValue(d.GetValueOrDefault("DIVIDEND")),
                DIVISOR = ParseValue(d.GetValueOrDefault("DIVISOR"))
            },

            ["math_constrain"] = d => new MathConstrainBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                LOW = ParseValue(d.GetValueOrDefault("LOW")),
                HIGH = ParseValue(d.GetValueOrDefault("HIGH"))
            },

            ["text_join"] = d => new TextJoinBlock
            {
                Strings = d.Keys
                    .Where(k => k.StartsWith("ADD"))
                    .OrderBy(k => k)
                    .Select(k => d[k] is Dictionary<string, object> sub ? GetString(sub, "TEXT") : string.Empty)
                    .ToList()
            },

            ["text_length"] = d => new TextLengthBlock { Text = ParseValue(d.GetValueOrDefault("VALUE")) },

            ["controls_repeat_ext"] = d => new RepeatBlock
            {
                Times = ParseValue(d.GetValueOrDefault("TIMES")),
                Stack = ParseStack(d, "DO")
            },

            ["controls_whileUntil"] = d => new WhileUntilBlock
            {
                Mode = GetString(d, "MODE"),
                Condition = ParseBlockBase(d.GetValueOrDefault("BOOL") as Dictionary<string, object>),
                Stack = ParseStack(d, "DO")
            },

            ["controls_for"] = d => new ForLoopBlock
            {
                VarName = GetString(d, "VAR"),
                From = ParseValue(d.GetValueOrDefault("FROM")),
                To = ParseValue(d.GetValueOrDefault("TO")),
                By = ParseValue(d.GetValueOrDefault("BY")),
                Stack = ParseStack(d, "DO")
            },

            ["logic_compare"] = d => new LogicCompareBlock
            {
                OP = GetString(d, "OP"),
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B"))
            },

            ["logic_operation"] = d => new LogicOperationBlock
            {
                OP = GetString(d, "OP"),
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B"))
            },

            ["logic_negate"] = d => new LogicNegateBlock { Bool = ParseValue(d.GetValueOrDefault("BOOL")) },

            ["controls_if"] = d => new ControlsIfBlock
            {
                Condition = ParseBlockBase(d.GetValueOrDefault("IF0") as Dictionary<string, object>),
                IfStack = ParseStack(d, "DO0"),
                ElseStack = ParseStack(d, "ELSE")
            },

            ["timing_wait_for_frames"] = d => new WaitForFrames { WaitTime = (uint)ParseFloat(d, "WaitTime") },
            ["timing_wait_for_seconds"] = d => new WaitForSeconds { WaitTime = ParseFloat(d, "WaitTime") },

            ["timing_wait_until_true"] = d => new WaitUntilBlock
            {
                Condition = ParseCondition(d),
                Stack = ParseStack(d, "DO")
            },

            ["procedures_callnoreturn"] = d => new ProcedureCallNoReturnBlock { Name = GetString(d, "NAME") },

            ["procedures_callreturn"] = d => new ProcedureCallReturnBlock
            {
                Name = GetString(d, "NAME"),
                Args = ParseCallArgs(d)
            },

            ["procedures_defreturn"] = d => new MethodBlock
            {
                Type = "procedures_defreturn",
                Name = d.TryGetValue("NAME", out object? drName) ? drName.ToString() : string.Empty,
                Stack = ParseStack(d, "STACK"),
                Params = d.TryGetValue("PARAMS", out var drp) ? ParseParams(drp) : [],
                Return = d.TryGetValue("RETURN", out object? ret) && ToStringDict(ret) is Dictionary<string, object> returnDict ? ParseBlock(returnDict) : null
            },

            ["procedures_defnoreturn"] = d => new MethodBlock
            {
                Type = "procedures_defnoreturn",
                Name = d.TryGetValue("NAME", out object? name) ? name.ToString() : string.Empty,
                Stack = ParseStack(d, "STACK"),
                Params = d.TryGetValue("PARAMS", out var p) ? ParseParams(p) : []
            },

            ["spawned_event"] = d => ParseEventBlock(d, EventType.OnSpawned, "ignore"),
            ["destroyed_event"] = d => ParseEventBlock(d, EventType.OnDestroyed, "ignore"),
            ["trigger_enter_event"] = d => ParseEventBlock(d, EventType.OnTriggerEntered, "player"),
            ["trigger_exit_event"] = d => ParseEventBlock(d, EventType.OnTriggerExited, "player"),
            ["interaction_event"] = d => ParseEventBlock(d, EventType.OnInteraction, "player"),
            ["interaction_denied_event"] = d => ParseEventBlock(d, EventType.OnInteractionDenied, "player"),

            ["primitive_create"] = d => new PrimitiveCreateBlock
            {
                Name = GetString(d, "name"),
                IsServerSide = ParseBool(d, "isServerSide")
            },

            ["primitive_set_position"] = d => ParsePrimitiveVector(d, "position"),
            ["primitive_set_rotation"] = d => ParsePrimitiveVector(d, "rotation"),
            ["primitive_set_scale"] = d => ParsePrimitiveVector(d, "scale"),

            ["primitive_set_color"] = d => new PrimitiveColorBlock
            {
                R = ParseFloat(d, "r", 1f),
                G = ParseFloat(d, "g", 1f),
                B = ParseFloat(d, "b", 1f),
                A = ParseFloat(d, "a", 1f)
            },

            ["primitive_set_type"] = d => new PrimitiveStateBlock { SettingType = "type", StringValue = GetString(d, "primitiveType") },
            ["primitive_set_flags"] = d => new PrimitiveStateBlock { SettingType = "flags", StringValue = GetString(d, "primitiveFlags") },
            ["get_primitive_property"] = d => new PrimitiveStateBlock { SettingType = "property", StringValue = GetString(d, "Property") },
            ["primitive_set_static"] = d => new PrimitiveStateBlock { SettingType = "static", BoolValue = ParseBool(d, "isStatic") },
            ["primitive_set_smoothing"] = d => new PrimitiveStateBlock { SettingType = "smoothing", ByteValue = (byte)ParseFloat(d, "movementSmoothing") },

            ["logger_log"] = d => new LoggingBlock
            {
                Level = GetLogLevel(GetString(d, "LEVEL")),
                Text = ParseValue(d.GetValueOrDefault("M"))
            },

            ["variables_set"] = d => new VariableBlock
            {
                Name = d.TryGetValue("VAR", out object? vn) ? vn.ToString() : string.Empty,
                Value = ParseValue(d.GetValueOrDefault("VALUE"))
            },

            ["variables_get"] = d => new GetVariableBlock
            {
                Name = d.TryGetValue("VAR", out object? gvn) ? gvn.ToString() : string.Empty
            },

            ["get_player_property"] = d => new PlayerGetPropertyBlock
            {
                Property = GetString(d, "Property")
            },

            ["set_player_gravity"] = d => new PlayerSetGravityBlock
            {
                X = ParseFloat(d, "x"),
                Y = ParseFloat(d, "y", -19.86f),
                Z = ParseFloat(d, "z")
            },

            ["get_player_by_id"] = d => new PlayerGetByIdBlock
            {
                PlayerId = (int)ParseFloat(d, "Player Id")
            },
            ["get_player_by_userid"] = d => new PlayerGetByUserIdBlock
            {
                UserId = GetString(d, "Player User Id")
            },

            ["set_player_role"] = d => new PlayerSetRoleBlock
            {
                Role = Enum.TryParse(GetString(d, "New Role"), out RoleTypeId role) ? role : RoleTypeId.None,
                KeepPosition = ParseBool(d, "Keep Position")
            },

            ["give_player_item"] = d => new PlayerGiveItemBlock
            {
                Item = Enum.TryParse(GetString(d, "New Item"), out ItemType item) ? item : ItemType.None,
                Amount = (int)ParseFloat(d, "Amount", 1f),
                DropIfFull = ParseBool(d, "Drop if full")
            },

            ["remove_player_item"] = d => new PlayerRemoveItemBlock
            {
                Item = Enum.TryParse(GetString(d, "Removed Item"), out ItemType ri) ? ri : ItemType.None
            },

            ["set_player_health"] = d => new PlayerSetHealthBlock 
            { 
                HealthType = "health", 
                Value = ParseFloat(d, "Health") 
            },
            ["set_player_max_health"] = d => new PlayerSetHealthBlock
            {
                HealthType = "max_health",
                Value = ParseFloat(d, "Max Health")
            },
            ["set_player_artificial_health"] = d => new PlayerSetHealthBlock { HealthType = "artificial_health", Value = ParseFloat(d, "Artificial Health") },
            ["set_player_max_artificial_health"] = d => new PlayerSetHealthBlock { HealthType = "max_artificial_health", Value = ParseFloat(d, "Max Artificial Health") },
            ["set_player_hume_shield"] = d => new PlayerSetHealthBlock { HealthType = "hume_shield", Value = ParseFloat(d, "Hume shield") },
            ["set_player_max_hume_shield"] = d => new PlayerSetHealthBlock { HealthType = "max_hume_shield", Value = ParseFloat(d, "Max Hume Shield") },
            ["set_player_hume_shield_regen_rate"] = d => new PlayerSetHealthBlock { HealthType = "hume_shield_regen_rate", Value = ParseFloat(d, "Hume Shield Regen Rate") },
            ["set_player_hume_shield_regen_cooldown"] = d => new PlayerSetHealthBlock { HealthType = "hume_shield_regen_cooldown", Value = ParseFloat(d, "Hume Shield Regen Cooldown") },

            ["set_player_group_name"] = d => new PlayerSetGroupBlock { GroupType = "name", Value = GetString(d, "Group Name") },
            ["set_player_group_color"] = d => new PlayerSetGroupBlock { GroupType = "color", Value = GetString(d, "Group Color") },

            ["send_player_broadcast"] = d => new PlayerSendBroadcastBlock
            {
                Message = GetString(d, "Broadcast Message"),
                Duration = (ushort)ParseFloat(d, "Duration", 5f)
            },

            ["send_player_hint"] = d => new PlayerSendHintBlock
            {
                Message = GetString(d, "Hint Message"),
                Duration = (ushort)ParseFloat(d, "Duration", 5f)
            },

            ["set_player_scale"] = d => new PlayerSetScaleBlock
            {
                X = ParseFloat(d, "x"),
                Y = ParseFloat(d, "y"),
                Z = ParseFloat(d, "z")
            },

            ["texttoy_create"] = d => new TextToyCreateBlock { Name = GetString(d, "name") },
            ["texttoy_set_text"] = d => new TextToySetTextBlock { Text = GetString(d, "text") },
            ["get_texttoy_property"] = d => new TextToyGetPropertyBlock { Property = GetString(d, "Property") },

            ["texttoy_set_display_size"] = d => new TextToySetDisplaySizeBlock
            {
                X = ParseFloat(d, "x", 1f),
                Y = ParseFloat(d, "y", 1f)
            },

            ["waypoint_create"] = d => new WaypointCreateBlock { Name = GetString(d, "name") },
            ["get_waypoint_property"] = d => new WaypointGetPropertyBlock { Property = GetString(d, "Property") },
            ["waypoint_set_visualize_bounds"] = d => new WaypointSetVisualizeBoundsBlock { VisualizeBounds = ParseBool(d, "visualizeBounds") },
            ["waypoint_set_priority"] = d => new WaypointSetPriorityBlock { Priority = ParseFloat(d, "priority") },

            ["waypoint_set_bounds_size"] = d => new WaypointSetBoundsSizeBlock
            {
                X = ParseFloat(d, "x", 1f),
                Y = ParseFloat(d, "y", 1f),
                Z = ParseFloat(d, "z", 1f)
            },

            ["speaker_create"] = d => new SpeakerCreateBlock { Name = GetString(d, "name") },
            ["get_speaker_property"] = d => new SpeakerGetPropertyBlock { Property = GetString(d, "Property") },
            ["speaker_set_volume"] = d => new SpeakerSetVolumeBlock { Volume = ParseFloat(d, "volume", 100f) },
            ["speaker_set_is_spatial"] = d => new SpeakerSetIsSpatialBlock { IsSpatial = ParseBool(d, "isSpatial") },
            ["speaker_set_min_distance"] = d => new SpeakerSetMinDistanceBlock { MinDistance = ParseFloat(d, "minDistance", 1f) },
            ["speaker_set_max_distance"] = d => new SpeakerSetMaxDistanceBlock { MaxDistance = ParseFloat(d, "maxDistance", 10f) },
            ["speaker_set_loop"] = d => new SpeakerSetLoopBlock { Loop = ParseBool(d, "loop") },
            ["speaker_set_path"] = d => new SpeakerSetPathBlock { Path = GetString(d, "path") },

            ["speaker_play"] = d => new SpeakerPlayBlock { FilePath = GetString(d, "filepath") },

            ["door_find_by_name"] = d => new FindDoorByNameBlock { Name = GetString(d, "name") },

            ["door_create"] = d => new CreateDoorBlock
            {
                DoorType = Enum.TryParse(GetString(d, "doorType"), out DoorType dt) ? dt : default,
                DoorPermissionFlags = Enum.TryParse(GetString(d, "doorPermissionFlags"), out DoorPermissionFlags dp) ? dp : default,
                IsOpen = ParseBool(d, "isOpen"),
                IsLocked = ParseBool(d, "isLocked"),
                RequireAllPermissions = ParseBool(d, "requireAllPermissions"),
                Bypass2176 = ParseBool(d, "bypass2176")
            },

            ["door_set_permissions"] = d => new SetDoorPermsBlock
            {
                Perms = Enum.TryParse(GetString(d, "perms"), out DoorPermissionFlags perms) ? perms : default
            },

            ["get_door_property"] = d => new DoorGetPropertyBlock
            {
                Property = GetString(d, "Property")
            },

            ["run_method"] = d => new RunMethodBlock
            {
                FullMethodName = GetString(d, "Full Method Name (namespace + method)"),
                Args = ParseMethodArgs(d, "Argument", 4)
            },

            ["run_method_instance"] = d => new RunMethodInstanceBlock
            {
                Instance = ParseValue(d.GetValueOrDefault("Instance")),
                MethodName = GetString(d, "Full Method Name (namespace + method)"),
                Args = ParseMethodArgs(d, "Argument", 4)
            },

            ["play_animation"] = d => new PlayAnimationBlock
            {
                AnimationName = GetString(d, "animationName")
            },

            ["play_audio"] = d => new PlayAudioBlock
            {
                Path = GetString(d, "path"),
                Volume = ParseFloat(d, "volume", 1f),
                MinDistance = ParseFloat(d, "minDistance", 1f),
                MaxDistance = ParseFloat(d, "maxDistance", 20f),
                IsSpatial = ParseBool(d, "isSpatial")
            },

            ["send_cassie"] = d => new SendCassieBlock
            {
                Message = GetString(d, "message"),
                CustomSubtitles = GetString(d, "customSubtitles"),
                PlayBackground = ParseBool(d, "playBackground"),
                Priority = ParseFloat(d, "priority"),
                GlitchScale = ParseFloat(d, "glitchScale")
            },

            ["run_command"] = d => new RunCommandBlock
            {
                CommandType = GetString(d, "commandType"),
                Command = GetString(d, "command")
            },

            ["give_effect"] = d => new GiveEffectBlock
            {
                Effect = Enum.TryParse(GetString(d, "effect"), out EffectType et) ? et : default,
                Intensity = (byte)ParseFloat(d, "intensity", 1f),
                Duration = ParseFloat(d, "duration", 5f)
            },

            ["remove_effect"] = d => new RemoveEffectBlock
            {
                Effect = Enum.TryParse(GetString(d, "effect"), out EffectType ret) ? ret : default,
                Intensity = (byte)ParseFloat(d, "intensity", 1f)
            },

            ["give_item"] = d => new ActionGiveItemBlock
            {
                Item = Enum.TryParse(GetString(d, "item"), out ItemType git) ? git : ItemType.None,
                Count = (int)ParseFloat(d, "count", 1f)
            },

            ["remove_item"] = d => new ActionRemoveItemBlock
            {
                Item = Enum.TryParse(GetString(d, "item"), out ItemType rit) ? rit : ItemType.None,
                Count = (int)ParseFloat(d, "count", 1f)
            },

            ["warhead"] = d => new WarheadBlock
            {
                Action = Enum.TryParse(GetString(d, "action"), out WarheadAction wa) ? wa : default,
                SuppressSubtitles = ParseBool(d, "suppressSubtitles")
            },

            ["unity_get_pos"] = d => new UnityGetPosBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!
            },

            ["unity_set_pos"] = d => new UnitySetPosBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!,
                POS = ParseVector3(d.GetValueOrDefault("POS"))!
            },

            ["unity_translate"] = d => new UnityTranslateBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!,
                V = ParseVector3(d.GetValueOrDefault("V"))
            },

            ["unity_rotate"] = d => new UnityRotateBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!,
                V = ParseVector3(d.GetValueOrDefault("V"))
            },

            ["unity_set_scale"] = d => new UnitySetScaleBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!,
                S = ParseVector3(d.GetValueOrDefault("S"))
            },

            ["unity_look_at"] = d => new UnityLookAtBlock
            {
                OBJ = ParseValue(d.GetValueOrDefault("OBJ"))!,
                T = ParseValue(d.GetValueOrDefault("T"))!
            },

            ["unity_add_force"] = d => new UnityAddForceBlock
            {
                RB = ParseValue(d.GetValueOrDefault("RB"))!,
                F = ParseValue(d.GetValueOrDefault("F"))
            },

            ["unity_add_torque"] = d => new UnityAddTorqueBlock
            {
                RB = ParseValue(d.GetValueOrDefault("RB"))!,
                T = ParseValue(d.GetValueOrDefault("T"))
            },

            ["unity_set_vel"] = d => new UnitySetVelBlock
            {
                RB = ParseValue(d.GetValueOrDefault("RB"))!,
                V = ParseValue(d.GetValueOrDefault("V"))
            },

            ["unity_raycast"] = d => new UnityRaycastBlock
            {
                O = ParseValue(d.GetValueOrDefault("O")),
                D = ParseValue(d.GetValueOrDefault("D"))
            },

            ["unity_wait"] = d => new UnityWaitBlock { T = ParseValue(d.GetValueOrDefault("T")) },

            ["unity_vec3"] = d => new UnityVec3Block
            {
                X = ParseValue(d.GetValueOrDefault("X")),
                Y = ParseValue(d.GetValueOrDefault("Y")),
                Z = ParseValue(d.GetValueOrDefault("Z"))
            },

            ["unity_distance"] = d => new UnityDistanceBlock
            {
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B"))
            },

            ["unity_lerp"] = d => new UnityLerpBlock
            {
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B")),
                T = ParseValue(d.GetValueOrDefault("T"))
            },

            ["unity_normalize"] = d => new UnityNormalizeBlock { V = ParseValue(d.GetValueOrDefault("V")) },

            ["list_create"] = d => new ListCreateBlock(),

            ["list_add"] = d => new ListAddBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST")),
                Item = ParseValue(d.GetValueOrDefault("ITEM"))
            },

            ["list_remove"] = d => new ListRemoveBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST")),
                Item = ParseValue(d.GetValueOrDefault("ITEM"))
            },

            ["list_remove_at"] = d => new ListRemoveBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST")),
                Item = ParseFloat(d, "INDEX")
            },

            ["list_get"] = d => new ListGetBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST")),
                Index = (int)ParseFloat(d, "INDEX")
            },

            ["list_count"] = d => new ListCountBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST"))
            },

            ["list_contains"] = d => new ListContainsBlock
            {
                List = ParseValue(d.GetValueOrDefault("LIST")),
                Item = ParseValue(d.GetValueOrDefault("ITEM"))
            },

            ["list_clear"] = d =>
            {
                List<object>? list = ParseValue(d.GetValueOrDefault("LIST")) as List<object>;
                list?.Clear();
                return null;
            },

            ["foreach_loop"] = d => new ForeachBlock
            {
                VarName = GetString(d, "VAR"),
                ListInput = ParseValue(d.GetValueOrDefault("LIST")), 
                Stack = ParseStack(d, "DO")
            },

            ["list_first"] = d => new ListFirstBlock { List = ParseValue(d.GetValueOrDefault("LIST")) },
            ["list_last"] = d => new ListLastBlock { List = ParseValue(d.GetValueOrDefault("LIST")) },

            ["get_player_by_collider"] = d => new PlayerGetByColliderBlock(),

            ["string_contains"] = d => new StringContainsBlock { STR = ParseValue(d.GetValueOrDefault("STR")), VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["string_starts_with"] = d => new StringStartsWithBlock { STR = ParseValue(d.GetValueOrDefault("STR")), VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["string_ends_with"] = d => new StringEndsWithBlock { STR = ParseValue(d.GetValueOrDefault("STR")), VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["string_to_upper"] = d => new StringToUpperBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["string_to_lower"] = d => new StringToLowerBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["string_trim"] = d => new StringTrimBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["string_substring"] = d => new StringSubstringBlock
            {
                STR = ParseValue(d.GetValueOrDefault("STR")),
                START = ParseValue(d.GetValueOrDefault("START")),
                LENGTH = ParseValue(d.GetValueOrDefault("LENGTH"))
            },
            ["string_replace"] = d => new StringReplaceBlock
            {
                STR = ParseValue(d.GetValueOrDefault("STR")),
                OLD = ParseValue(d.GetValueOrDefault("OLD")),
                NEW = ParseValue(d.GetValueOrDefault("NEW"))
            },
            ["string_index_of"] = d => new StringIndexOfBlock { STR = ParseValue(d.GetValueOrDefault("STR")), VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["string_char_at"] = d => new StringCharAtBlock { STR = ParseValue(d.GetValueOrDefault("STR")), INDEX = ParseValue(d.GetValueOrDefault("INDEX")) },
            ["string_format"] = d => new StringFormatBlock
            {
                FMT = ParseValue(d.GetValueOrDefault("FMT")),
                ARG1 = ParseValue(d.GetValueOrDefault("ARG1")),
                ARG2 = ParseValue(d.GetValueOrDefault("ARG2")),
                ARG3 = ParseValue(d.GetValueOrDefault("ARG3"))
            },
            ["string_is_null_or_empty"] = d => new StringIsNullOrEmptyBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["string_is_null_or_whitespace"] = d => new StringIsNullOrWhitespaceBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["string_pad_left"] = d => new StringPadLeftBlock { STR = ParseValue(d.GetValueOrDefault("STR")), TOTAL = ParseValue(d.GetValueOrDefault("TOTAL")) },
            ["string_pad_right"] = d => new StringPadRightBlock { STR = ParseValue(d.GetValueOrDefault("STR")), TOTAL = ParseValue(d.GetValueOrDefault("TOTAL")) },
            ["string_join"] = d => new StringJoinBlock { SEP = ParseValue(d.GetValueOrDefault("SEP")), LIST = ParseValue(d.GetValueOrDefault("LIST")) },
            ["string_split"] = d => new StringSplitBlock { STR = ParseValue(d.GetValueOrDefault("STR")), SEP = ParseValue(d.GetValueOrDefault("SEP")) },

            ["dict_create"] = d => new DictCreateBlock(),
            ["dict_add"] = d => new DictAddBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                KEY = ParseValue(d.GetValueOrDefault("KEY")),
                VALUE = ParseValue(d.GetValueOrDefault("VALUE"))
            },
            ["dict_remove"] = d => new DictRemoveBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                KEY = ParseValue(d.GetValueOrDefault("KEY"))
            },
            ["dict_contains_key"] = d => new DictContainsKeyBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                KEY = ParseValue(d.GetValueOrDefault("KEY"))
            },
            ["dict_contains_value"] = d => new DictContainsValueBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                VALUE = ParseValue(d.GetValueOrDefault("VALUE"))
            },
            ["dict_get"] = d => new DictGetBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                KEY = ParseValue(d.GetValueOrDefault("KEY"))
            },
            ["dict_set"] = d => new DictSetBlock
            {
                DICT = ParseValue(d.GetValueOrDefault("DICT")),
                KEY = ParseValue(d.GetValueOrDefault("KEY")),
                VALUE = ParseValue(d.GetValueOrDefault("VALUE"))
            },
            ["dict_count"] = d => new DictCountBlock { DICT = ParseValue(d.GetValueOrDefault("DICT")) },
            ["dict_keys"] = d => new DictKeysBlock { DICT = ParseValue(d.GetValueOrDefault("DICT")) },
            ["dict_values"] = d => new DictValuesBlock { DICT = ParseValue(d.GetValueOrDefault("DICT")) },
            ["dict_clear"] = d => new DictClearBlock { DICT = ParseValue(d.GetValueOrDefault("DICT")) },

            ["hashset_create"] = d => new HashSetCreateBlock(),
            ["hashset_add"] = d => new HashSetAddBlock
            {
                SET = ParseValue(d.GetValueOrDefault("SET")),
                ITEM = ParseValue(d.GetValueOrDefault("ITEM"))
            },
            ["hashset_remove"] = d => new HashSetRemoveBlock
            {
                SET = ParseValue(d.GetValueOrDefault("SET")),
                ITEM = ParseValue(d.GetValueOrDefault("ITEM"))
            },
            ["hashset_contains"] = d => new HashSetContainsBlock
            {
                SET = ParseValue(d.GetValueOrDefault("SET")),
                ITEM = ParseValue(d.GetValueOrDefault("ITEM"))
            },
            ["hashset_count"] = d => new HashSetCountBlock { SET = ParseValue(d.GetValueOrDefault("SET")) },
            ["hashset_clear"] = d => new HashSetClearBlock { SET = ParseValue(d.GetValueOrDefault("SET")) },
            ["hashset_union_with"] = d => new HashSetUnionWithBlock
            {
                SET = ParseValue(d.GetValueOrDefault("SET")),
                OTHER = ParseValue(d.GetValueOrDefault("OTHER"))
            },
            ["hashset_intersect_with"] = d => new HashSetIntersectWithBlock
            {
                SET = ParseValue(d.GetValueOrDefault("SET")),
                OTHER = ParseValue(d.GetValueOrDefault("OTHER"))
            },

            ["stack_create"] = d => new StackCreateBlock(),
            ["stack_push"] = d => new StackPushBlock
            {
                STACK = ParseValue(d.GetValueOrDefault("STACK")),
                ITEM = ParseValue(d.GetValueOrDefault("ITEM"))
            },
            ["stack_pop"] = d => new StackPopBlock { STACK = ParseValue(d.GetValueOrDefault("STACK")) },
            ["stack_peek"] = d => new StackPeekBlock { STACK = ParseValue(d.GetValueOrDefault("STACK")) },
            ["stack_count"] = d => new StackCountBlock { STACK = ParseValue(d.GetValueOrDefault("STACK")) },

            ["queue_create"] = d => new QueueCreateBlock(),
            ["queue_enqueue"] = d => new QueueEnqueueBlock
            {
                QUEUE = ParseValue(d.GetValueOrDefault("QUEUE")),
                ITEM = ParseValue(d.GetValueOrDefault("ITEM"))
            },
            ["queue_dequeue"] = d => new QueueDequeueBlock { QUEUE = ParseValue(d.GetValueOrDefault("QUEUE")) },
            ["queue_peek"] = d => new QueuePeekBlock { QUEUE = ParseValue(d.GetValueOrDefault("QUEUE")) },
            ["queue_count"] = d => new QueueCountBlock { QUEUE = ParseValue(d.GetValueOrDefault("QUEUE")) },

            ["array_create"] = d => new ArrayCreateBlock { SIZE = ParseValue(d.GetValueOrDefault("SIZE")) },
            ["array_length"] = d => new ArrayLengthBlock { ARR = ParseValue(d.GetValueOrDefault("ARR")) },
            ["array_get"] = d => new ArrayGetBlock
            {
                ARR = ParseValue(d.GetValueOrDefault("ARR")),
                INDEX = ParseValue(d.GetValueOrDefault("INDEX"))
            },
            ["array_set"] = d => new ArraySetBlock
            {
                ARR = ParseValue(d.GetValueOrDefault("ARR")),
                INDEX = ParseValue(d.GetValueOrDefault("INDEX")),
                VALUE = ParseValue(d.GetValueOrDefault("VALUE"))
            },

            ["datetime_now"] = d => new DateTimeNowBlock(),
            ["datetime_utc_now"] = d => new DateTimeUtcNowBlock(),
            ["datetime_part"] = d => new DateTimePartBlock
            {
                DT = ParseValue(d.GetValueOrDefault("DT")),
                PART = GetString(d, "PART")
            },
            ["datetime_add"] = d => new DateTimeAddBlock
            {
                DT = ParseValue(d.GetValueOrDefault("DT")),
                UNIT = GetString(d, "UNIT"),
                VALUE = ParseValue(d.GetValueOrDefault("VALUE"))
            },
            ["datetime_subtract"] = d => new DateTimeSubtractBlock
            {
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B"))
            },
            ["datetime_to_string"] = d => new DateTimeToStringBlock { DT = ParseValue(d.GetValueOrDefault("DT")) },
            ["datetime_to_string_format"] = d => new DateTimeToStringFormatBlock
            {
                DT = ParseValue(d.GetValueOrDefault("DT")),
                FMT = ParseValue(d.GetValueOrDefault("FMT"))
            },
            ["datetime_parse"] = d => new DateTimeParseBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["datetime_ticks"] = d => new DateTimeTicksBlock { DT = ParseValue(d.GetValueOrDefault("DT")) },
            ["timespan_from"] = d => new TimeSpanFromBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                UNIT = GetString(d, "UNIT")
            },
            ["timespan_part"] = d => new TimeSpanPartBlock
            {
                TS = ParseValue(d.GetValueOrDefault("TS")),
                PART = GetString(d, "PART")
            },
            ["timespan_zero"] = d => new TimeSpanZeroBlock(),

            ["convert_to_string"] = d => new ConvertToStringBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["convert_to_int"] = d => new ConvertToIntBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["convert_to_float"] = d => new ConvertToFloatBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["convert_to_double"] = d => new ConvertToDoubleBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["convert_to_bool"] = d => new ConvertToBoolBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["convert_parse_int"] = d => new ConvertParseIntBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["convert_parse_float"] = d => new ConvertParseFloatBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["convert_parse_double"] = d => new ConvertParseDoubleBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["convert_parse_bool"] = d => new ConvertParseBoolBlock { STR = ParseValue(d.GetValueOrDefault("STR")) },
            ["convert_to_string_format"] = d => new ConvertToStringFormatBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                FMT = ParseValue(d.GetValueOrDefault("FMT"))
            },

            ["math_min"] = d => new MathMinBlock { A = ParseValue(d.GetValueOrDefault("A")), B = ParseValue(d.GetValueOrDefault("B")) },
            ["math_max"] = d => new MathMaxBlock { A = ParseValue(d.GetValueOrDefault("A")), B = ParseValue(d.GetValueOrDefault("B")) },
            ["math_clamp01"] = d => new MathClamp01Block { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_lerp_float"] = d => new MathLerpFloatBlock
            {
                A = ParseValue(d.GetValueOrDefault("A")),
                B = ParseValue(d.GetValueOrDefault("B")),
                T = ParseValue(d.GetValueOrDefault("T"))
            },
            ["math_move_towards"] = d => new MathMoveTowardsBlock
            {
                CURRENT = ParseValue(d.GetValueOrDefault("CURRENT")),
                TARGET = ParseValue(d.GetValueOrDefault("TARGET")),
                MAXDELTA = ParseValue(d.GetValueOrDefault("MAXDELTA"))
            },
            ["math_random_int_range"] = d => new MathRandomIntRangeBlock
            {
                MIN = ParseValue(d.GetValueOrDefault("MIN")),
                MAX = ParseValue(d.GetValueOrDefault("MAX"))
            },
            ["math_random_float_range"] = d => new MathRandomFloatRangeBlock
            {
                MIN = ParseValue(d.GetValueOrDefault("MIN")),
                MAX = ParseValue(d.GetValueOrDefault("MAX"))
            },
            ["math_sign"] = d => new MathSignBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_floor_to_int"] = d => new MathFloorToIntBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_ceil_to_int"] = d => new MathCeilToIntBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_round_to_int"] = d => new MathRoundToIntBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_round_digits"] = d => new MathRoundDigitsBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                DIGITS = ParseValue(d.GetValueOrDefault("DIGITS"))
            },
            ["math_pingpong"] = d => new MathPingPongBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                LENGTH = ParseValue(d.GetValueOrDefault("LENGTH"))
            },
            ["math_repeat"] = d => new MathRepeatBlock
            {
                VALUE = ParseValue(d.GetValueOrDefault("VALUE")),
                LENGTH = ParseValue(d.GetValueOrDefault("LENGTH"))
            },
            ["math_pi"] = d => new MathPiBlock(),
            ["math_deg2rad"] = d => new MathDeg2RadBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_rad2deg"] = d => new MathRad2DegBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_is_nan"] = d => new MathIsNaNBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
            ["math_is_infinity"] = d => new MathIsInfinityBlock { VALUE = ParseValue(d.GetValueOrDefault("VALUE")) },
        };

        internal static object? ParseBlock(Dictionary<string, object> dict)
        {
            if (!dict.TryGetValue("type", out object? typeObj))
                return null;

            string type = typeObj.ToString();

            if (BlockParsers.TryGetValue(type, out var parser))
                return parser(dict);

            return new UnknownBlock { Raw = dict };
        }

        private static EventBlock ParseEventBlock(Dictionary<string, object> d, EventType eventType, string paramName)
        {
            return new EventBlock
            {
                EventType = eventType,
                ParamName = paramName,
                Stack = ParseStack(d, "DO")
            };
        }

        private static PrimitiveVectorBlock ParsePrimitiveVector(Dictionary<string, object> d, string targetProperty)
        {
            return new PrimitiveVectorBlock
            {
                TargetProperty = targetProperty,
                X = ParseFloat(d, "x"),
                Y = ParseFloat(d, "y"),
                Z = ParseFloat(d, "z")
            };
        }

        private static List<object?> ParseStack(Dictionary<string, object> d, string key)
        {
            if (!d.TryGetValue(key, out object? raw))
                return [];

            List<Dictionary<string, object>> statements = ParseStatementList(raw);
            List<object?> stack = new(statements.Count);

            foreach (Dictionary<string, object> statement in statements)
            {
                object? parsed = ParseBlock(statement);
                if (parsed != null)
                    stack.Add(parsed);
            }

            return stack;
        }

        private static object?[] ParseMethodArgs(Dictionary<string, object> d, string prefix, int count)
        {
            object?[] args = new object?[count];

            for (int i = 0; i < count; i++)
            {
                args[i] = d.TryGetValue($"{prefix} {i + 1}", out object? arg) ? arg : null;
            }

            return args;
        }

        private static BlockBase? ParseBlockBase(Dictionary<string, object>? dict)
        {
            if (dict == null)
            {
                LogManager.Warn("Dictionary was null. Returning.");
                return null;
            }

            object? parsed = ParseBlock(dict);

            if (parsed is not BlockBase block)
                return null;

            return block;
        }

        private static BlockBase? ParseCondition(Dictionary<string, object> dict)
        {
            if (ToStringDict(dict.GetValueOrDefault("BOOL")) is Dictionary<string, object> boolDict)
                return ParseBlockBase(boolDict);

            if (ToStringDict(dict.GetValueOrDefault("CONDITION")) is Dictionary<string, object> condDict)
                return ParseBlockBase(condDict);

            if (ToStringDict(dict.GetValueOrDefault("IF0")) is Dictionary<string, object> ifDict)
                return ParseBlockBase(ifDict);

            if (ToStringDict(dict.GetValueOrDefault("WaitingInput")) is Dictionary<string, object> waitDict)
                return ParseBlockBase(waitDict);

            return null;
        }

        private static Dictionary<string, object?> ParseCallArgs(Dictionary<string, object> dict)
        {
            Dictionary<string, object?> args = [];
            int i = 0;

            while (dict.TryGetValue($"ARGNAME{i}", out object? argName) && dict.TryGetValue($"ARG{i}", out object? argVal))
            {
                args[argName.ToString()!] = ToStringDict(argVal) is Dictionary<string, object> d ? ParseBlock(d) : argVal;
                i++;
            }

            return args;
        }

        private static Dictionary<string, object>? ToStringDict(object? obj)
        {
            if (obj is Dictionary<string, object> stringDict)
                return stringDict;

            if (obj is Dictionary<object, object> objectDict)
            {
                Dictionary<string, object> newDict = [];

                foreach (KeyValuePair<object, object> kvp in objectDict)
                {
                    newDict[kvp.Key.ToString()] = kvp.Value;
                }

                return newDict;
            }

            return null;
        }

        private static Vector3 ParseVector3(object? obj)
        {
            try
            {
                if (obj is Dictionary<string, object> dict)
                {
                    return new Vector3(
                        ParseFloat(dict, "X"),
                        ParseFloat(dict, "Y"),
                        ParseFloat(dict, "Z")
                    );
                }

                if (obj is Vector3 v)
                    return v;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Exception while parsing Vector3 {ex}");
            }

            return Vector3.zero;
        }

        private static List<string> ParseParams(object obj)
        {
            if (obj is not List<object> paramList)
                return [];

            List<string> list = new(paramList.Count);

            foreach (object item in paramList)
                list.Add(item.ToString());

            return list;
        }

        private static object? ParseValue(object? value)
        {
            if (ToStringDict(value) is Dictionary<string, object> dict)
                return ParseBlock(dict);

            return value;
        }

        private static float ParseFloat(Dictionary<string, object> dict, string key, float defaultVal = 0f)
        {
            if (!dict.TryGetValue(key, out object? val))
                return defaultVal;

            return val switch
            {
                float f => f,
                double d => (float)d,
                decimal m => (float)m,
                int i => i,
                long l => l,
                short s => s,
                byte b => b,
                uint ui => ui,
                ulong ul => ul,
                ushort us => us,
                sbyte sb => sb,
                string str when float.TryParse(str, out float result) => result,
                _ => defaultVal
            };
        }

        private static bool ParseBool(Dictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out object? val))
                return false;

            return val switch
            {
                bool b => b,
                string str when bool.TryParse(str, out bool result) => result,
                _ => false
            };
        }

        private static string GetString(Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var val) ? val as string ?? val?.ToString() ?? string.Empty : string.Empty;

        private static List<Dictionary<string, object>> ParseStatementList(object stmtObj)
        {
            List<Dictionary<string, object>> list = [];

            if (stmtObj is List<object> objList)
            {
                foreach (object item in objList)
                {
                    if (ToStringDict(item) is Dictionary<string, object> dictObj)
                        list.Add(dictObj);
                }
            }
            return list;
        }

        private static LogLevel GetLogLevel(string level)
        {
            return level switch
            {
                "info" => LogLevel.Info,
                "debug" => LogLevel.Debug,
                "warn" => LogLevel.Warn,
                "error" => LogLevel.Error,
                _ => LogLevel.Info
            };
        }
    }
}