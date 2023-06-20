using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ByteHelper
{
    public static byte[] Combine(byte[] first, byte second)
    {
        return first.Concat(new byte[1] { second }).ToArray();
    }
    public static byte[] Combine(byte[] first, byte[] second)
    {
        return first.Concat(second).ToArray();
    }
    public static byte[] Combine(byte first, byte[] second)
    {
        return (new byte[1] { first }).Concat(second).ToArray();
    }
}
