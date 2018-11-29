using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DllExporter;
using System.Runtime.InteropServices;

namespace DplayxDll
{
  public class Dplayx
  {
    [DllExport]
    public static int DirectPlayCreate(IntPtr guid,IntPtr pDP,IntPtr unk)
    {
      var dp = Marshal.AllocHGlobal(IntPtr.Size*2);
      Marshal.WriteIntPtr(pDP, dp);
      var vtbl = dp + IntPtr.Size;
      Marshal.WriteIntPtr(dp, vtbl);
      var directPlay = new DirectPlay();
      vtbl += IntPtr.Size;
      var pF= Marshal.GetFunctionPointerForDelegate(new DirectPlay.QueryInterface(directPlay.QueryInterfaceI));
      Marshal.WriteIntPtr(vtbl, pF);
      return 0;
    }

    [DllExport]
    public static void DirectPlayEnumerateA()
    {

    }

    [DllExport]
    public static void DirectPlayEnumerateW()
    {

    }

    [DllExport]
    public static void DirectPlayLobbyCreateA()
    {

    }
  }

  public class DirectPlay
  {
    public delegate int QueryInterface(IntPtr me, IntPtr ppvObj);

    public delegate int AddRef(IntPtr me);

    public delegate int Release(IntPtr me);

    public int QueryInterfaceI(IntPtr me, IntPtr ppvObj)
    {
      return 0;
    }
  }
}
