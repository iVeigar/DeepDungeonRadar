using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using DeepDungeonRadar.Enums;

namespace DeepDungeonRadar;

public class DeepDungeonObject
{
    public Vector3 Location { get; init; }

    [JsonIgnore]
    internal Vector2 Location2D => new(Location.X, Location.Z);

    public ushort Territory { get; init; }

    [JsonIgnore]
    internal DeepDungeonBg Bg => Util.TerritoryToBg(Territory);

    public DeepDungeonObjectType Type { get; init; }

    public uint Base { get; init; } // DataId

    public uint InstanceId { get; init; } // ObjectId


    public override string ToString() => $"{Type}, {Territory}, {Base}, {InstanceId:X}, {Location}";

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Location = ");
        builder.Append(Location);
        builder.Append(", Territory = ");
        builder.Append(Territory);
        builder.Append(", Base = ");
        builder.Append(Base);
        builder.Append(", InstanceId = ");
        builder.Append(InstanceId);
        builder.Append(", Type = ");
        builder.Append(Type);
        return true;
    }

}

public class DeepDungeonObjectLocationEqualityComparer : IEqualityComparer<DeepDungeonObject>
{
    public bool Equals(DeepDungeonObject? x, DeepDungeonObject? y)
    {
        if (x == y)
        {
            return true;
        }
        if (x == null || y == null)
        {
            return false;
        }
        return x.Location2D.Equals(y.Location2D) && x.Bg == y.Bg && x.Type == y.Type;
    }

    public int GetHashCode(DeepDungeonObject obj)
    {
        return obj.Location2D.GetHashCode() ^ (int)obj.Bg ^ (int)obj.Type;
    }
}