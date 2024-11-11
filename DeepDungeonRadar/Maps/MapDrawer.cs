using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Misc;
using DeepDungeonRadar.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ImGuiNET;
using static DeepDungeonRadar.util.DeepDungeonUtil;
namespace DeepDungeonRadar.Maps;

public class MapDrawer()
{
    private const float halfCorridorWidth = 5f;
    private const float thickness = 1.5f;
    private (List<Vector2?>, (int, float)) CurrentMap = default;
    private Dictionary<int, Vector2[]> RoomVerticesCache = [];
    private List<Vector2?> Rooms => CurrentMap.Item1;
    private int Shape => CurrentMap.Item2.Item1;
    private float Width => CurrentMap.Item2.Item2;

    private Dictionary<Vector3, bool> WallStatus = [];
    private Dictionary<int, Dictionary<Direction, bool>> RoomDirectionStatus = [];
    private Dictionary<(int Room1, int Room2), (bool Horizontal, bool Blocked)> CorridorStatus = [];
    private Dictionary<(int Room1, int Room2), Vector2[]> CorridorVerticesCache = [];
    private HashSet<int> ActiveRooms = [];


    public unsafe void DebugDraw()
    {
        var sceneWrapper = Framework.Instance()->BGCollisionModule->SceneManager->FirstScene;
        if (sceneWrapper == null) return;
        var scene = sceneWrapper->Scene;
        if (scene == null) return;

        foreach (var coll in scene->Colliders)
        {
            if (coll != null && coll->GetColliderType() == ColliderType.Box && (0x7400 & (coll->ObjectMaterialValue ^ 0x7400)) == 0)
            {
                var box = (ColliderBox*)coll;
                Vector4 color = (box->VisibilityFlags & 1) != 0 ? new(1, 0, 0, 0.7f) : new(0, 1, 0, 0.7f);
                var pos = new Vector3(box->World.Row3.X, MeWorldPos.Y + 0.1f, box->World.Row3.Z);
                if (Vector3.Distance(pos, MeWorldPos) < 50f)
                    if (Service.GameGui.WorldToScreen(pos, out var screenPos))
                        ImGui.GetBackgroundDrawList().AddCircleFilled(screenPos, 10f, ImGui.ColorConvertFloat4ToU32(color));
            }
        }
    }

    public void Draw(ImDrawListPtr windowDrawList, Vector2 pivotWorldPos, Vector2 pivotWindowPos, float zoom, float rotation, uint activefgcolor, uint activebgcolor, uint inactivefgcolor, uint inactivebgcolor)
    {
        if (Rooms == null)
            return;
        var drawStraightCorridor = Service.Config.RadarDrawStraightCorridor;
        // 1. draw corridors
        var CorridorVerticesOnRadar = CorridorVerticesCache.ToDictionary(p => p.Key, p => p.Value.Select(vertex => vertex.ToRadarWindowPos(pivotWorldPos, pivotWindowPos, zoom, rotation)).ToArray());
        foreach (var (corridor, (_, blocked)) in CorridorStatus)
        {
            if (!CorridorVerticesOnRadar.TryGetValue(corridor, out var vertices))
                continue;
            var fgcolor = blocked ? inactivefgcolor : activefgcolor;
            var bgcolor = blocked ? inactivebgcolor : activebgcolor;
            if (vertices.Length == 4)
            {
                windowDrawList.AddConvexPolyFilled(ref vertices[0], 4, bgcolor);
            }
            else if (drawStraightCorridor)
            {
                vertices = [vertices[0], vertices[vertices.Length / 2 - 1], vertices[vertices.Length / 2], vertices[^1]];
                windowDrawList.AddConvexPolyFilled(ref vertices[0], 4, bgcolor);
            }
            else
            {
                Vector2[][] polys = [[vertices[0], vertices[1], vertices[6], vertices[7]],
                                 [vertices[1], vertices[2], vertices[5], vertices[6]],
                                 [vertices[2], vertices[3], vertices[4], vertices[5]]];
                foreach (var poly in polys)
                    windowDrawList.AddConvexPolyFilled(ref poly[0], 4, bgcolor);
            }
            // 只画长边不画短边，短边在画房间时补上
            windowDrawList.AddPolyline(ref vertices[0], vertices.Length / 2, fgcolor, ImDrawFlags.None, thickness);
            windowDrawList.AddPolyline(ref vertices[vertices.Length / 2], vertices.Length / 2, fgcolor, ImDrawFlags.None, thickness);
        }

        // 2. draw rooms
        var RoomVerticesOnRadar = RoomVerticesCache.ToDictionary(p => p.Key, p => p.Value.Select(vertex => vertex.ToRadarWindowPos(pivotWorldPos, pivotWindowPos, zoom, rotation)).ToArray());
        foreach (var (room, vertices) in RoomVerticesOnRadar)
        {
            var active = ActiveRooms.Contains(room);
            var fgcolor = active ? activefgcolor : inactivefgcolor;
            var bgcolor = active ? activebgcolor : inactivebgcolor;
            var segmentLength = vertices.Length / 4;

            // draw bg
            windowDrawList.AddConvexPolyFilled(ref vertices[0], vertices.Length, bgcolor);

            // draw border
            for (var i = 0; i < 4; i++)
                for (var j = 0; j < segmentLength - 1; j++)
                    windowDrawList.AddLine(vertices[i * segmentLength + j], vertices[i * segmentLength + j + 1], fgcolor, thickness);
            
            // draw entry
            if (!RoomDirectionStatus.TryGetValue(room, out var status))
                continue;
            bool[] flags = [true, true, true, true]; // clockwise, start from north
            foreach (var (direction, blocked) in status)
            {
                switch (direction)
                {
                    case Direction.正北: flags[0] = blocked; break;
                    case Direction.正东: flags[1] = blocked; break;
                    case Direction.正南: flags[2] = blocked; break;
                    case Direction.正西: flags[3] = blocked; break;
                }
            }
            for (var i = 0; i < flags.Length; i++)
            {
                if (flags[i])
                    windowDrawList.AddLine(vertices[(i * segmentLength - 1 + vertices.Length) % vertices.Length], vertices[i * segmentLength], fgcolor, thickness);
            }
        }
    }

    public void ClearMapData()
    {
        CurrentMap = default;
        RoomVerticesCache.Clear();
        CorridorVerticesCache.Clear();
    }

    public void ClearFloorDetailData()
    {
        WallStatus.Clear();
        RoomDirectionStatus.Clear();
        CorridorStatus.Clear();
        ActiveRooms.Clear();
    }

    public unsafe void Update()
    {
        ClearFloorDetailData();
        if (!LoadMap()) return;
        if (RoomVerticesCache.Count == 0)
            CalcRoomVertices();
        UpdateWallStatus();
        UpdateRoomDirectionStatus();
        UpdateCorridorStatus();
        if (CorridorVerticesCache.Count == 0)
            CalcCorridorVertices();
        UpdateActiveRooms();
    }

    private bool Cheated = false;
    public unsafe void ResetColliderBox()
    {
        if (!Cheated) return;
        var sceneWrapper = Framework.Instance()->BGCollisionModule->SceneManager->FirstScene;
        if (sceneWrapper == null) return;
        var scene = sceneWrapper->Scene;
        if (scene == null) return;
        foreach (var coll in scene->Colliders)
        {
            if (coll->GetColliderType() == ColliderType.Box && (0x7400 & (coll->ObjectMaterialValue ^ 0x7400)) == 0)
            {
                if (WallStatus.TryGetValue(((ColliderBox*)coll)->World.Row3, out var originStatus))
                {
                    var currentStatus = (coll->VisibilityFlags & 1) == 1;
                    if (currentStatus != originStatus)
                        coll->VisibilityFlags ^= 1;
                }
            }
        }
        Cheated = false;
    }

    public unsafe void Cheat()
    {
        if (!Service.Condition[ConditionFlag.InDeepDungeon]) return;
        var sceneWrapper = Framework.Instance()->BGCollisionModule->SceneManager->FirstScene;
        if (sceneWrapper == null) return;
        var scene = sceneWrapper->Scene;
        if (scene == null) return;
        foreach (var coll in scene->Colliders)
        {
            if (coll->GetColliderType() == ColliderType.Box && (0x7400 & (coll->ObjectMaterialValue ^ 0x7400)) == 0)
            {
                if ((coll->VisibilityFlags & 1) == 1)
                    coll->VisibilityFlags ^= 1;
            }
        }
        Service.ChatGui.Print("你现在可以穿过部分墙壁了~(仅限本层)");
        Cheated = true;
    }

    private bool LoadMap(bool force = false)
    {
        if (!force && Rooms != null || MapData.TryGetMapData(MapId, out CurrentMap))
        {
            return true;
        }
        else
        {
            Service.Log.Warning($"map not found, territory id: {MapId}");
            return false;
        }
    }

    private void CalcRoomVertices()
    {
        var halfRoomWidth = Width / 2;
        // clockwise
        List<Vector2> vertices = [
            new(halfCorridorWidth, -halfRoomWidth),
            new(halfRoomWidth, -halfCorridorWidth),
            new(halfRoomWidth, halfCorridorWidth),
            new(halfCorridorWidth, halfRoomWidth),
            new(-halfCorridorWidth, halfRoomWidth),
            new(-halfRoomWidth, halfCorridorWidth),
            new(-halfRoomWidth, -halfCorridorWidth),
            new(-halfCorridorWidth, -halfRoomWidth)
        ];
        if (Shape == 0)
        {
            vertices.Insert(1, new(halfRoomWidth, -halfRoomWidth));
            vertices.Insert(4, new(halfRoomWidth, halfRoomWidth));
            vertices.Insert(7, new(-halfRoomWidth, halfRoomWidth));
            vertices.Insert(10, new(-halfRoomWidth, -halfRoomWidth));
        }
        for (var i = 0; i < Rooms.Count; i++)
        {
            if (Rooms[i] == null) continue;
            var center = Rooms[i].Value;
            RoomVerticesCache.TryAdd(i, vertices.Select(p => p + center).ToArray());
        }
    }

    private void CalcCorridorVertices()
    {
        var halfRoomWidth = Width / 2;
        foreach (var ((room1, room2), (horizontal, _)) in CorridorStatus)
        {
            if (Rooms[room1] == null || Rooms[room2] == null) continue;
            Vector2 offset = horizontal ? new(halfRoomWidth, 0) : new(0, halfRoomWidth);
            var start = Rooms[room1].Value + offset;
            var end = Rooms[room2].Value - offset;
            CorridorVerticesCache.TryAdd((room1, room2), Utils.GenerateBentLineWithWidth(start, end, halfCorridorWidth, horizontal));
        }
    }
    private unsafe void UpdateWallStatus()
    {
        var scene = Framework.Instance()->BGCollisionModule->SceneManager->FirstScene->Scene;
        if (scene == null) return;
        var walls = new List<(Vector3, bool)>();

        // real walls
        foreach (var coll in scene->Colliders)
        {
            if (coll != null && coll->GetColliderType() == ColliderType.Box && (0x7400 & (coll->ObjectMaterialValue ^ 0x7400)) == 0)
            {
                // true = blocked, false = passable
                walls.Add((((ColliderBox*)coll)->World.Row3, (coll->VisibilityFlags & 1) != 0));
            }
        }

        // virtual walls on leaf rooms
        WallStatus = walls.Concat(MapData.LeafRooms.Select(i =>
        {
            if (MapData.IsLeaf(i, Direction.正北)) return (Rooms[i].Value.ToVector3() + new Vector3(0, 0, Width / 2), false);
            if (MapData.IsLeaf(i, Direction.正东)) return (Rooms[i].Value.ToVector3() + new Vector3(-Width / 2, 0, 0), false);
            if (MapData.IsLeaf(i, Direction.正南)) return (Rooms[i].Value.ToVector3() + new Vector3(0, 0, -Width / 2), false);
            if (MapData.IsLeaf(i, Direction.正西)) return (Rooms[i].Value.ToVector3() + new Vector3(Width / 2, 0, 0), false);
            return (default, default); // default Vector3 已保证不是有效值
        }).Where(i => i != default)).GroupBy(x => x.Item1).ToDictionary(g => g.Key, g => g.First().Item2);
    }

    private void UpdateRoomDirectionStatus()
    {
        RoomDirectionStatus = WallStatus.Select(CheckWall)
                        .Where(x => x.room != -1 && (x.direction == Direction.正北 || x.direction == Direction.正东 || x.direction == Direction.正南 || x.direction == Direction.正西))
                        .GroupBy(x => x.room)
                        .ToDictionary(g => g.Key,
                                      g => g.DistinctBy(g => g.direction)
                                            .ToDictionary(g => g.direction, g => g.blocked));
    }
    
    private void UpdateCorridorStatus()
    {
        foreach (var (room1, room1Status) in RoomDirectionStatus)
        {
            foreach (var (direction1, blocked1) in room1Status.Where(p => p.Key == Direction.正东 || p.Key == Direction.正南))
            {
                var room2 = MapData.GetAdjacentRoom(room1, direction1);
                if (room2 == -1 || Rooms[room2] == null || !RoomDirectionStatus.TryGetValue(room2, out var room2Status))
                    continue;
                var direction2 = direction1.Reverse();
                if (!room2Status.TryGetValue(direction2, out var blocked2))
                    continue;
                CorridorStatus.TryAdd((room1, room2), (direction1 == Direction.正东, blocked1 || blocked2));
            }
        }
    }

    private void UpdateActiveRooms()
    {
        foreach(var (room1, room2) in CorridorStatus.Where(s => !s.Value.Blocked).Select(s => s.Key))
        {
            ActiveRooms.Add(room1);
            ActiveRooms.Add(room2);
        }
    }

    private (int room, Direction direction, bool blocked) CheckWall(KeyValuePair<Vector3, bool> wall)
    {
        var minDistance = Width;
        var index = -1;
        var direction = Direction.None;

        for (var i = 0; i < Rooms.Count; i++)
        {
            if (Rooms[i] == null) continue;
            var distance = Vector2.Distance(Rooms[i].Value, wall.Key.ToVector2());
            if (distance < minDistance)
            {
                minDistance = distance;
                index = i;
                direction = Utils.GetDirection(Rooms[i].Value, wall.Key);
            }
        }
        return (index, direction, wall.Value);
    }
}