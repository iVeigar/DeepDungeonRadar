using System.Runtime.InteropServices;
using Dalamud.Game;

namespace DeepDungeonRadar;
public class PluginAddressResolver : BaseAddressResolver
{
    private nint CamPtr { get; set; }

    public unsafe float HRotation => *(float*)(CamPtr + 304);

    protected override void Setup64Bit(ISigScanner scanner)
    {
        CamPtr = Marshal.ReadIntPtr(scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 45 33 C9 45 33 C0 33 D2 C6 40 09 01", 0));
    }
}
