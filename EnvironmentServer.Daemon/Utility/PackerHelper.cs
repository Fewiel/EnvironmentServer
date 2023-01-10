using System.IO;

namespace EnvironmentServer.Daemon.Utility;

public class PackerHelper
{
    public static void DeleteCache(string username, string environmentInternalName)
    {
        if (!Directory.Exists($"/home/{username}/files/{environmentInternalName}/var/cache"))
            return;

        foreach (var f in Directory.GetDirectories(
            $"/home/{username}/files/{environmentInternalName}/var/cache"))
        {
            if (f.Contains("prod"))
                Directory.Delete(f, true);
        }
    }

    public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] repl)
    {
        if (repl == null) return src;
        int index = FindBytes(src, search);
        if (index < 0) return src;
        byte[] dst = new byte[src.Length - search.Length + repl.Length];
        System.Buffer.BlockCopy(src, 0, dst, 0, index);
        System.Buffer.BlockCopy(repl, 0, dst, index, repl.Length);
        System.Buffer.BlockCopy(src, index + search.Length, dst, index + repl.Length, src.Length - (index + search.Length));
        return dst;
    }

    public static byte[] ReplaceBytesAll(byte[] src, byte[] search, byte[] repl)
    {
        if (repl == null) return src;
        int index = FindBytes(src, search);
        if (index < 0) return src;
        byte[] dst;
        do
        {
            dst = new byte[src.Length - search.Length + repl.Length];
            System.Buffer.BlockCopy(src, 0, dst, 0, index);
            System.Buffer.BlockCopy(repl, 0, dst, index, repl.Length);
            System.Buffer.BlockCopy(src, index + search.Length, dst, index + repl.Length, src.Length - (index + search.Length));
            src = dst;
            index = FindBytes(src, search);
        }
        while (index >= 0);
        return dst;
    }

    public static int FindBytes(byte[] src, byte[] find)
    {
        if (src == null || find == null || src.Length == 0 || find.Length == 0 || find.Length > src.Length) return -1;
        for (int i = 0; i < src.Length - find.Length + 1; i++)
        {
            if (src[i] == find[0])
            {
                for (int m = 1; m < find.Length; m++)
                {
                    if (src[i + m] != find[m]) break;
                    if (m == find.Length - 1) return i;
                }
            }
        }
        return -1;
    }
}