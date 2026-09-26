using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace StardropiumChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        /// <summary>
        /// 英文 -> 中文 替换表。
        /// 只放 GMCM 可见文本，不要放配置键名。
        /// </summary>
        private static readonly Dictionary<string, string> Translations = new()
        {
            // ===== 主分类 =====
            ["Base Game & Engine Optimizations"] = "基础游戏与引擎优化",
            ["Third-Party Mod Optimizations"] = "第三方模组优化",
            ["Profiling & Diagnostics"] = "分析与诊断",
            ["Quick Actions & Tools"] = "快捷操作与工具",
            ["Performance Presets"] = "性能预设",
            ["Configuration Categories"] = "配置分类",

            // ===== 预设与操作 =====
            ["Apply 'Maximum Performance' Preset"] = "应用“最高性能”预设",
            ["Apply 'Handheld / Steam Deck' Preset"] = "应用“掌机 / Steam Deck”预设",
            ["Restore 'Recommended Safe Defaults' Preset"] = "恢复“推荐安全默认值”预设",
            ["Purge Memory & Compact Heap"] = "清除内存并压缩堆",
            ["Dump Live Telemetry Audit"] = "导出实时遥测审计",
            ["Reset Telemetry Counters"] = "重置遥测计数器",
            ["Return to Main Menu"] = "返回主菜单",

            // ===== 模块标题 =====
            ["Lighting System Optimization"] = "光照系统优化",
            ["Furniture Frustum Culling"] = "家具视锥剔除",
            ["Animal Frustum Culling"] = "动物视锥剔除",
            ["Vegetation & Crop Frustum Culling"] = "植被与作物视锥剔除",
            ["Co-op Network Stability Optimizer"] = "联机网络稳定性优化",
            ["TMX Map Tile String Optimizer"] = "TMX 地图地块字符串优化",
            ["Item Query & ALL_ITEMS Fast-Resolver"] = "物品查询与 ALL_ITEMS 快速解析",
            ["Universal Memory Optimizer"] = "通用内存优化",
            ["Live Profiling & Behavior Diagnostics"] = "实时分析与行为诊断",
            ["Initial Load & Asset Caching"] = "初始加载与资源缓存",
            ["Map Load Acceleration"] = "地图加载加速",
            ["Memory Optimization"] = "内存优化",
            ["Save Game Serialization"] = "存档序列化",
            ["NPC Schedule & Pathfinding Optimizer"] = "NPC 日程与寻路优化",
            ["SMAPI Fast Render Check"] = "SMAPI 快速渲染检查",
            ["SpaceCore Save Serialization Fast-Path"] = "SpaceCore 存档序列化快速路径",
            ["Spatial Collision Fast-Reject"] = "空间碰撞快速拒绝",
            ["String & Argument Optimization"] = "字符串与参数优化",
            ["10-Minute Clock Smoothing"] = "10 分钟时钟平滑",
            ["Fast Water Tile Lookups"] = "快速水地块查找",
            ["UI Info Suite 2 Buff Reflection Fast-Path"] = "UI Info Suite 2 Buff 反射快速路径",
            ["Alternative Textures Throttling"] = "Alternative Textures 节流",
            ["Cloudy Skies Indoor Culling"] = "Cloudy Skies 室内剔除",
            ["DaLion Perks & Hopper Culling"] = "DaLion 特长与料斗剔除",
            ["Content Patcher Engine Optimizer"] = "Content Patcher 引擎优化",
            ["Custom Companions World-Scan Optimizer"] = "Custom Companions 世界扫描优化",
            ["Dynamic Reflections Optimization"] = "Dynamic Reflections 优化",
            ["Help Wanted Performance Suite"] = "Help Wanted 性能套件",
            ["Movement Overhaul Off-Screen Culling"] = "Movement Overhaul 屏幕外剔除",
            ["NPC Map Locations Optimization"] = "NPC Map Locations 优化",
            ["NPC Viewport Frustum Throttling"] = "NPC 视口视锥节流",
            ["Remote Empty Location Culling"] = "远程空地点剔除",
            ["Save Stream Optimization"] = "存档流优化",
            ["SMAPI Reflection Optimization"] = "SMAPI 反射优化",
            ["Co-op Socket Buffer Expander"] = "联机套接字缓冲区扩展",
            ["Low-Latency GC Scheduler"] = "低延迟 GC 调度器",
            ["Camera Subpixel Smoothing"] = "相机子像素平滑",
            ["Universal Memory Reduction"] = "通用内存减少",
            ["Fast Map Water Initialization"] = "快速地图水域初始化",
            ["Fast Zero-Allocation ArgUtility"] = "快速零分配 ArgUtility",
            ["Zero-Allocation Time Memoization"] = "零分配时间记忆化",
            ["Cull Empty Map Layers"] = "剔除空地图图层",
            ["Optimize TMX Tile Properties"] = "优化 TMX 地块属性",
            ["Optimize NPC Schedule Pathfinding"] = "优化 NPC 日程寻路",
            ["Optimize Item Queries & ALL_ITEMS"] = "优化物品查询与 ALL_ITEMS",

            // ===== 布尔选项 / 描述标题 =====
            ["Cull Off-Screen Lights"] = "剔除屏幕外光源",
            ["Cull Off-Screen Furniture"] = "剔除屏幕外家具",
            ["Cull Off-Screen Animals"] = "剔除屏幕外动物",
            ["Cull Off-Screen Vegetation & Crops"] = "剔除屏幕外植被与作物",
            ["Throttle Entity Texture Updates"] = "节流实体纹理更新",
            ["Enable Spatial Fast-Reject"] = "启用空间快速拒绝",
            ["Enable Raw Image Cache"] = "启用原始图像缓存",
            ["Enable Core Asset Pre-Warming"] = "启用核心资源预热",
            ["Optimize Item Queries & ALL_ITEMS"] = "优化物品查询与 ALL_ITEMS",
            ["Manage SinZ Cache"] = "管理 SinZ 缓存",
            ["Purge Textures on New Day"] = "新的一天清除纹理",
            ["Auto-Trim Working Set on New Day"] = "新的一天自动修剪工作集",
            ["Trim External Image Cache"] = "修剪外部图像缓存",
            ["Trim on Warp"] = "传送时修剪",
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? stardropium = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    a.GetName().Name?.Contains("Stardropium", StringComparison.OrdinalIgnoreCase) == true);

            if (stardropium == null)
            {
                Monitor.Log("未找到 Stardropium 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(
                nameof(Transpiler),
                BindingFlags.Static | BindingFlags.NonPublic
            )!;

            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in stardropium.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method))
                        continue;

                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                        Monitor.Log($"已修补 {type.FullName}.{method.Name}", LogLevel.Trace);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"修补 {type.FullName}.{method.Name} 失败：{ex.Message}", LogLevel.Warn);
                    }
                }
            }

            Monitor.Log($"Stardropium 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;

            // 只处理配置注册相关方法，以及编译器生成的 lambda。
            if (!name.Contains("RegisterConfig", StringComparison.Ordinal) &&
                !name.Contains("OnGameLaunched", StringComparison.Ordinal) &&
                !name.Contains("AddBoolOption", StringComparison.Ordinal) &&
                !name.Contains("AddSectionTitle", StringComparison.Ordinal) &&
                !name.Contains("AddParagraph", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return method.GetMethodBody() != null;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string original &&
                    Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }

                yield return instruction;
            }
        }
    }
}
