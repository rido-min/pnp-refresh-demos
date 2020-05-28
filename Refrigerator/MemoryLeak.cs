using System;
using System.Collections.Generic;
using System.Text;

namespace Refrigerator
{
    static internal class MemoryLeak
    {
        static List<byte[]> memory = new List<byte[]>();
        static internal void FillMemory()
        {
            for (int i = 0; i < 100; i++)
            {
                memory.Add(new byte[1024 * 1024]);
            }
        }

        static internal void FreeMemory()
        {
            memory.Clear();
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForFullGCComplete(2000);
        }
    }
}
