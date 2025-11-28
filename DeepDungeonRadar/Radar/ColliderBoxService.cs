using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using static DeepDungeonRadar.Radar.DeepDungeonService;
namespace DeepDungeonRadar.Radar;

public class ColliderBoxService(DeepDungeonService dds)
{
    public readonly Dictionary<Vector3, (bool Vertical, bool Blocked)> ColliderBoxes = [];
    private readonly DeepDungeonService deepDungeonService = dds;
    private bool Cheated = false;
    private bool ReadyToCheat = false;
    public unsafe void Draw()
    {
        if (!deepDungeonService.InDeepDungeon || deepDungeonService.FloorTransfer || !deepDungeonService.HasRadar) return;
        try
        {
            var sceneWrapper = Framework.Instance()->BGCollisionModule->SceneManager->FirstScene;
            if (sceneWrapper == null) return;
            var scene = sceneWrapper->Scene;
            if (scene == null) return;
            foreach (var coll in scene->Colliders)
            {
                if (!FilterColliderBox(coll))
                    continue;
                var box = (ColliderBox*)coll;
                Vector4 color = (box->VisibilityFlags & 1) != 0 ? new(1, 0, 0, 0.7f) : new(0, 1, 0, 0.7f);
                var pos = new Vector3(box->World.Row3.X, MeWorldPos.Y + 0.1f, box->World.Row3.Z);
                if (pos.Distance2D(MeWorldPos) < 50f)
                {
                    Svc.GameGui.WorldToScreen(pos, out var screenPos);
                    {
                        ImGui.GetBackgroundDrawList().AddCircleFilled(screenPos, 10f, ImGui.ColorConvertFloat4ToU32(color));

                    }

                }
            }
        }
        catch (NullReferenceException)
        {

        }
    }

    public unsafe void Update()
    {
        try
        {
            foreach (var coll in Framework.Instance()->BGCollisionModule->SceneManager->FirstScene->Scene->Colliders)
            {
                if (!FilterColliderBox(coll))
                    continue;
                ColliderBoxes[((ColliderBox*)coll)->World.Row3] = (MathF.Abs(((ColliderBox*)coll)->Rotation.Y) > 1.5f, (coll->VisibilityFlags & 1) == 1);
            }
            ReadyToCheat = true;
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"ColliderBox Update exception: {ex}");
        }

        if (ColliderBoxes.Count == 0)
        {
            Svc.Log.Warning("ColliderBox Update: no ColliderBox !");
        }
    }

    public unsafe void Reset()
    {
        if (Cheated)
        {
            if (deepDungeonService.InDeepDungeon && deepDungeonService.HasRadar)
            {
                try
                {
                    foreach (var coll in Framework.Instance()->BGCollisionModule->SceneManager->FirstScene->Scene->Colliders)
                    {
                        if (!FilterColliderBox(coll) || !ColliderBoxes.TryGetValue(((ColliderBox*)coll)->World.Row3, out var originStatus))
                            continue;
                        var currentBlocked = (coll->VisibilityFlags & 1) == 1;
                        if (!currentBlocked && originStatus.Blocked)
                            coll->VisibilityFlags ^= 1;
                    }
                }
                catch { }
            }
            Cheated = false;
        }
        ColliderBoxes.Clear();
        ReadyToCheat = false;
    }

    public unsafe void Cheat()
    {
        if (!deepDungeonService.InDeepDungeon || !ReadyToCheat)
            return;
        try
        {
            foreach (var coll in Framework.Instance()->BGCollisionModule->SceneManager->FirstScene->Scene->Colliders)
            {
                if (FilterColliderBox(coll))
                {
                    if ((coll->VisibilityFlags & 1) == 1)
                        coll->VisibilityFlags ^= 1;
                }
            }
            Plugin.PrintChatMessage("你现在可以穿过部分墙壁了~(仅限本层)");
            Cheated = true;
        }
        catch { }
    }

    private static readonly ulong[] BoxIds = [0x2404, 0x5400, 0x5404, 0x7400, 0x7404, 0x7484];

    private unsafe static bool FilterColliderBox(Collider* coll)
        => coll != null && coll->GetColliderType() == ColliderType.Box 
        && ((coll->ObjectMaterialValue & 0x404) == 0x404 || (coll->ObjectMaterialValue & 0x5400) == 0x5400);
}
