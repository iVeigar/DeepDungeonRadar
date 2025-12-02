using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Data;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using SkiaSharp;
namespace DeepDungeonRadar.Radar;

public sealed class MapService : IDisposable
{
    public static class PixelType
    {
        public const uint VoidArea = 0;
        public const uint ReachableAreaBorder = 1;
        public const uint ReachableArea = 2;
        public const uint UnreachableAreaBorder = 4;
        public const uint UnreachableArea = 8;
    }

    public sealed record class MapInfo(Vector2 MinBounds, Vector2 MaxBounds, string Filename)
    {
        public Vector2 TopLeft = MinBounds + ExtendedSize * new Vector2(-0.5f, -0.5f);
        public Vector2 BottomRight = MaxBounds + ExtendedSize * new Vector2(0.5f, 0.5f);
        public Vector2 TopRight = new Vector2(MaxBounds.X, MinBounds.Y) + ExtendedSize * new Vector2(0.5f, -0.5f);
        public Vector2 BottomLeft = new Vector2(MinBounds.X, MaxBounds.Y) + ExtendedSize * new Vector2(-0.5f, 0.5f);
        public bool IsHallofFallacies = Filename.EndsWith(".3.bmp");
        public bool Contains(Vector2 p) => p.InRect(TopLeft, BottomRight);
    }

    private readonly DeepDungeonService deepDungeonService;
    private readonly ColliderBoxService colliderBoxService;
    private readonly Dictionary<string, List<MapInfo>> mapEntries = [];
    private readonly List<(MapInfo info, Bmp1pp data)> loadedMaps = [];

    public (MapInfo Info, Bmp1pp BitMap) CurrentMap { get; private set; }
    public List<Vector2> PassagePosition { get; private set; }
    private SKBitmap processedMap;
    private Vector2 spawnPoint;
    public IDalamudTextureWrap ColoredMapTexture { get; private set; }

    // state
    private bool loading;
    private bool processing;
    private bool coloring;

    // config
    public const int ExtendedSize = 2;
    private readonly Configuration config = Plugin.Config;
    private bool drawUnreachable;
    private uint reachableAreaBorderColor;
    private uint reachableAreaBackgroundColor;
    private uint unreachableAreaBorderColor;
    private uint unreachableAreaBackgroundColor;

    public MapService(DeepDungeonService deepDungeonService, ColliderBoxService colliderBoxService)
    {
        this.deepDungeonService = deepDungeonService;
        this.colliderBoxService = colliderBoxService;
        LoadMapEntries();
        RefreshDrawingConfig();
    }

    public (MapInfo entry, Bmp1pp data) Find(Vector2 pos) => loadedMaps.FirstOrDefault(e => e.info.Contains(pos));
    
    public void RegisterEvents()
    {
        deepDungeonService.EnteredNewFloor += OnEnteredNewFloor;
        deepDungeonService.ExitingCurrentFloor += OnExitingCurrentFloor;
    }

    public void UnregisterEvents()
    {
        deepDungeonService.EnteredNewFloor -= OnEnteredNewFloor;
        deepDungeonService.ExitingCurrentFloor -= OnExitingCurrentFloor;
    }

    private void LoadMapEntries()
    {
        try
        {
            using var dbStream = GetEmbeddedResource("maplist.json");
            using var json = Serialization.ReadJson(dbStream);
            foreach (var jentries in json.RootElement.EnumerateObject())
            {
                var entries = mapEntries[jentries.Name] = [];
                foreach (var jentry in jentries.Value.EnumerateArray())
                {
                    entries.Add(new(
                        ReadVector2(jentry, nameof(MapInfo.MinBounds)),
                        ReadVector2(jentry, nameof(MapInfo.MaxBounds)),
                        jentry.GetProperty(nameof(MapInfo.Filename)).GetString() ?? ""
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Failed to load maplist.json", ex);
            mapEntries.Clear();
        }
    }

    private bool DrawingConfigChanged()
    {
        return drawUnreachable != config.RadarDrawUnreachable ||
            reachableAreaBorderColor != config.ReachableAreaBorderColor ||
            reachableAreaBackgroundColor != config.ReachableAreaBackgroundColor ||
            unreachableAreaBorderColor != config.UnreachableAreaBorderColor ||
            unreachableAreaBackgroundColor != config.UnreachableAreaBackgroundColor;
    }
    
    private void RefreshDrawingConfig()
    {
        drawUnreachable = config.RadarDrawUnreachable;
        reachableAreaBorderColor = config.ReachableAreaBorderColor;
        reachableAreaBackgroundColor = config.ReachableAreaBackgroundColor;
        unreachableAreaBorderColor = config.UnreachableAreaBorderColor;
        unreachableAreaBackgroundColor = config.UnreachableAreaBackgroundColor;
    }

    public bool IsMapReadyToDraw()
    {
        if (loading || processing || coloring || CurrentMap == default || processedMap == default || ColoredMapTexture == default)
            return false;

        if (DrawingConfigChanged())
        {
            RefreshDrawingConfig();
            TaskGenColoredMap();
            return false;
        }
        return true;
    }

    public void OnExitingCurrentFloor(bool exitDungeon)
    {
        colliderBoxService.Reset();
        ResetCurrentMap();
        if (exitDungeon)
            loadedMaps.Clear();
    }

    public void OnEnteredNewFloor()
    {
        colliderBoxService.Update();
        spawnPoint = Player.Position.ToVector2();
        TaskLoadMap();
    }

    public void ResetCurrentMap()
    {
        CurrentMap = default;
        processedMap?.Dispose();
        processedMap = null;
        ColoredMapTexture = null;
    }

    private void TaskLoadMap()
    {
        if (!loading)
        {
            loading = true;
            Task.Run(() =>
            {
                try
                {
                    if (LoadCurrentMap())
                        TaskGenProcessedMap();
                }
                finally
                {
                    loading = false;
                }
            });
        }
    }

    private void TaskGenProcessedMap()
    {
        if (!processing)
        {
            processing = true;
            Task.Run(() =>
            {
                try
                {
                    GenProcessedMap();
                    TaskGenColoredMap();
                }
                finally
                {
                    processing = false;
                }
            });
        }
    }

    private void TaskGenColoredMap()
    {
        if (!coloring)
        {
            coloring = true;
            Task.Run(() =>
            {
                try
                {
                    GenColoredMap();
                }
                finally
                {
                    coloring = false;
                }
            });
        }
    }

    private bool LoadCurrentMap()
    {
        if (!deepDungeonService.HasMap)
            return false;
        var bg = Player.Territory.ToBg().ToString();
        if (!TryLoadMaps(bg))
        {
            Svc.Log.Warning($"Map not found for [{bg}]");
            return false;
        }
        CurrentMap = Find(spawnPoint);
        if (CurrentMap == default)
        {
            Svc.Log.Warning($"Map not found for starting position {spawnPoint} on [{bg}]");
            return false;
        }
        Svc.Log.Debug($"Loaded map for [{bg}]");
        return true;
    }

    private bool TryLoadMaps(string bg)
    {
        if (!mapEntries.TryGetValue(bg, out var mapInfoList))
        {
            Svc.Log.Error($"Map [{bg}] not found in mapEntries");
            return false;
        }

        foreach (var info in mapInfoList)
        {
            if (loadedMaps.Any(kvp => kvp.info.Filename == info.Filename))
                continue;
            try
            {
                using var eStream = GetEmbeddedResource(info.Filename);
                Bmp1pp bitmap = new(eStream);
                loadedMaps.Add((info, bitmap));
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to load {info.Filename}: {ex}");
            }
        }
        return true;
    }

    private unsafe void GenProcessedMap()
    {
        Svc.Log.Debug($"Start processing original 1bpp map");
        if (CurrentMap == default)
        {
            Svc.Log.Warning($"Current map is default, cannot process map");
            return;
        }

        // 处理位图数据
        var srcData = CurrentMap.BitMap;
        processedMap?.Dispose();
        processedMap = new(srcData.Width + 2 * ExtendedSize, srcData.Height + 2 * ExtendedSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        var width = processedMap.Width;
        var height = processedMap.Height;

        // 填充透明区域和所有地面和边界
        var pixels = (uint*)processedMap.GetPixels();
        for (var y = ExtendedSize; y < height - ExtendedSize; y++)
        {
            var x = ExtendedSize;
            for (; x < width - ExtendedSize; x++)
            {
                var idx = x + y * width;
                var topNeighbor = idx - width;
                var leftNeighbor = idx - 1;
                if (srcData[x - ExtendedSize, y - ExtendedSize]) // is void area
                {
                    if (pixels[topNeighbor] == PixelType.UnreachableArea || pixels[leftNeighbor] == PixelType.UnreachableArea)
                        pixels[idx] = PixelType.UnreachableAreaBorder;
                }
                else
                {
                    pixels[idx] = PixelType.UnreachableArea;
                    if (pixels[topNeighbor] == PixelType.VoidArea)
                        pixels[topNeighbor] = PixelType.UnreachableAreaBorder;
                    if (pixels[leftNeighbor] == PixelType.VoidArea)
                        pixels[leftNeighbor] = PixelType.UnreachableAreaBorder;
                }
            }
            // 每行倒数第MapManager.ExtendedSize个像素 // 更右面的像素一定都是void area
            var idxLast = x + y * width;
            if (pixels[idxLast - 1] == PixelType.UnreachableArea)
                pixels[idxLast] = PixelType.UnreachableAreaBorder;
        }
        // 计算倒数第MapManager.ExtendedSize行 // 更下面的几行一定都是void area
        for (var x = 0; x < width; x++)
        {
            var idx = x + (height - ExtendedSize) * width;
            var topNeighbor = idx - width;
            if (pixels[topNeighbor] == PixelType.UnreachableArea)
                pixels[idx] = PixelType.UnreachableAreaBorder;
        }
        Svc.Log.Debug($"Step 1 done");

        // 根据ColliderBoxManager中的墙体位置，沿着墙体方向向两侧扩展，直到遇到已经标记为墙体边界的像素为止
        foreach (var (wallPos, (vertical, blocked)) in colliderBoxService.ColliderBoxes)
        {
            if (!blocked || !CurrentMap.Info.Contains(wallPos.ToVector2()))
                continue;
            var wallPixelPos = (wallPos.ToVector2() - CurrentMap.Info.TopLeft) * 2f;
            var wallX = (int)wallPixelPos.X;
            var wallY = (int)wallPixelPos.Y;
            pixels[wallX + wallY * width] = PixelType.UnreachableAreaBorder;
            if (vertical)
            {
                for (var i = 1; wallY - i >= 0; i++)
                {
                    var next = wallX + (wallY - i) * width;
                    if (pixels[next] != PixelType.UnreachableAreaBorder)
                        pixels[next] = PixelType.UnreachableAreaBorder;
                    else
                        break;
                }
                for (var i = 1; wallY + i < height; i++)
                {
                    var next = wallX + (wallY + i) * width;
                    if (pixels[next] != PixelType.UnreachableAreaBorder)
                        pixels[next] = PixelType.UnreachableAreaBorder;
                    else
                        break;
                }
            }
            else
            {
                for (var i = 1; wallX - i >= 0; i++)
                {
                    var next = wallX - i + wallY * width;
                    if (pixels[next] != PixelType.UnreachableAreaBorder)
                        pixels[next] = PixelType.UnreachableAreaBorder;
                    else
                        break;
                }
                for (var i = 1; wallX + i < width; i++)
                {
                    var next = wallX + i + wallY * width;
                    if (pixels[next] != PixelType.UnreachableAreaBorder)
                        pixels[next] = PixelType.UnreachableAreaBorder;
                    else
                        break;
                }
            }
        }
        Svc.Log.Debug($"Step 2 done");

        static bool IsInvalidStartPoint(Vector2 sp, Vector2 topLeft, uint* pixels, int width, int height)
        {
            var startX = (int)((sp.X - topLeft.X) * 2f);
            var startY = (int)((sp.Y - topLeft.Y) * 2f);
            return startX < 0 || startY < 0 || startX >= width || startY >= height || pixels[startX + startY * width] != PixelType.UnreachableArea;
        }

        var startPoint = spawnPoint;
        // 泛洪填充算法，将spawnPoint所在区域填充为通路，遇到边界后把边界改为可达边界
        // 边界检查
        if (IsInvalidStartPoint(startPoint, CurrentMap.Info.TopLeft, pixels, width, height))
        {
            Svc.Log.Warning($"Flood fill starting point {startPoint} is invalid, change to an opened wall..");
            startPoint = colliderBoxService.ColliderBoxes.FirstOrDefault(kv => !kv.Value.Blocked && !IsInvalidStartPoint(kv.Key.ToVector2(), CurrentMap.Info.TopLeft, pixels, width, height)).Key.ToVector2();
            if (startPoint == default)
            {
                Svc.Log.Error($"No opened wall found, cannot perform flood fill");
                return;
            }
        }

        // 非递归的栈/DFS 实现
        var stack = new Stack<(int x, int y)>();
        // 将起点标为通路并入栈
        var startX = (int)((startPoint.X - CurrentMap.Info.TopLeft.X) * 2f);
        var startY = (int)((startPoint.Y - CurrentMap.Info.TopLeft.Y) * 2f);
        pixels[startX + startY * width] = PixelType.ReachableArea;
        stack.Push((startX, startY));

        var neighbors = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            // 四连通邻居
            foreach (var (dx, dy) in neighbors)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    continue;

                var nIdx = nx + ny * width;
                if (pixels[nIdx] == PixelType.UnreachableAreaBorder)
                {
                    // 标记被探测到的墙，但不继续深入
                    pixels[nIdx] = PixelType.ReachableAreaBorder;
                }
                else if (pixels[nIdx] == PixelType.UnreachableArea)
                {
                    // 标记通路并入栈继续扩展
                    pixels[nIdx] = PixelType.ReachableArea;
                    stack.Push((nx, ny));
                }
            }
        }
        Svc.Log.Debug($"Step 3 done");
        Svc.Log.Debug($"Processed map data of {CurrentMap.Info.Filename}");
    }

    private unsafe void GenColoredMap()
    {
        Svc.Log.Debug($"Start coloring map");
        if (processedMap == null)
        {
            Svc.Log.Warning($"Processed map is null, cannot color map");
            return;
        }
        var width = processedMap.Width;
        var height = processedMap.Height;
        var srcPixels = (uint*)processedMap.GetPixels();
        // 配置文件的颜色格式是abgr, 与函数最后的RawImageSpecification.Rgba32(width, height)对应上了，所以不用转换颜色的字节顺序
        using var coloredMap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var dstPixels = (uint*)coloredMap.GetPixels();
        // 填充可达区域和不可达区域背景色
        for (var i = 0; i < width * height; i++)
        {
            if (srcPixels[i] == PixelType.ReachableArea)
                dstPixels[i] = reachableAreaBackgroundColor;
            else if (drawUnreachable && srcPixels[i] == PixelType.UnreachableArea)
                dstPixels[i] = unreachableAreaBackgroundColor;
        }

        // 绘制边界
        using (var canvas = new SKCanvas(coloredMap))
        {
            // 先绘制不可达区域边界的像素，再绘制可达区域边界的像素，以免前者覆盖后者
            if (drawUnreachable)
            {
                // Paint，启用抗锯齿 + MaskFilter 膨胀
                var paintUnreachableAreaBorder = new SKPaint
                {
                    IsAntialias = true,
                    Color = unreachableAreaBorderColor,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Solid, 3)
                };
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (srcPixels[x + y * width] == PixelType.UnreachableAreaBorder)
                        {
                            canvas.DrawCircle(x, y, 1, paintUnreachableAreaBorder);
                        }
                    }
                }
            }
            var paintReachableAreaBorder = new SKPaint
            {
                IsAntialias = true,
                Color = reachableAreaBorderColor,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Solid, 3)
            };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (srcPixels[x + y * width] == PixelType.ReachableAreaBorder)
                    {
                        canvas.DrawCircle(x, y, 1, paintReachableAreaBorder);
                    }
                }
            }
        }
        Svc.Log.Debug($"Coloring finished");

        ColoredMapTexture = Svc.Texture.CreateFromRaw(
            RawImageSpecification.Bgra32(width, height),
            new ReadOnlySpan<byte>((void*)coloredMap.GetPixels(), width * height * 4)
            );
        Svc.Log.Debug($"Built colored map texture for {CurrentMap.Info.Filename}");
    }

    private static Vector2 ReadVector2(JsonElement obj, string tag) => new(obj.GetProperty(tag + "X").GetSingle(), obj.GetProperty(tag + "Z").GetSingle());

    private static Stream GetEmbeddedResource(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream($"DeepDungeonRadar.Radar.Maps.{name}") ?? throw new InvalidDataException($"Missing embedded resource {name}");

    // 主要在Hall of Fallacies中使用，其他深宫地图只有一个传送门，不需要映射到房间索引
    public int PositionToRoomIndex(Vector2 p)
    {
        if (CurrentMap == default || !CurrentMap.Info.Contains(p))
            return 0;
        var relativePos = (p - CurrentMap.Info.MinBounds) / (CurrentMap.Info.MaxBounds - CurrentMap.Info.MinBounds);
        if (CurrentMap.Info.IsHallofFallacies)
        {
            var col = (int)(relativePos.X * 4);
            var row = (int)(relativePos.Y * 3) + 1;
            return row * 5 + col;
        }
        else
        {
            var col = (int)(relativePos.X * 5);
            var row = (int)(relativePos.Y * 5);
            return row * 5 + col;
        }
    }

    // 房间索引转换为房间位置（房间不一定在切分出的格子的中心，这只是个大概位置，用于初步算出传送门的位置）
    public Vector2 RoomIndexToPosition(int index)
    {
        if (CurrentMap == default)
            return Vector2.Zero;
        var iPos = new Vector2(index % 5, index / 5 - (CurrentMap.Info.IsHallofFallacies ? 1 : 0));
        var relativePos = (iPos + new Vector2(0.5f, 0.5f)) / (CurrentMap.Info.IsHallofFallacies ? new Vector2(4, 3) : new Vector2(5, 5));
        return CurrentMap.Info.MinBounds + relativePos * (CurrentMap.Info.MaxBounds - CurrentMap.Info.MinBounds);
    }

    public void Dispose()
    {
        UnregisterEvents();
        OnExitingCurrentFloor(true);
    }
}
