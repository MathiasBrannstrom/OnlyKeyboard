using System.Runtime.InteropServices;

namespace HuntAndPeck.NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }
}
