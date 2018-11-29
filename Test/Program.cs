using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DplayxDll;
using System.Runtime.InteropServices;

namespace Test
{
  class Program
  {
    static void Main(string[] args)
    {
      var pdp= Marshal.AllocHGlobal(4);
      var ret= Dplayx.DirectPlayCreate(IntPtr.Zero, pdp, IntPtr.Zero);
      var dp = Marshal.ReadIntPtr(pdp);
      var vtbl = Marshal.ReadIntPtr(dp);
      Console.WriteLine($"{dp} {vtbl}");
      Console.ReadLine();
    }
  }
}
