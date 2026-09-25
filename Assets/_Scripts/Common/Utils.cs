using System.Collections;
using UnityEngine;

public enum CompareRes
{
    Less, 
    Equal,
    Greater,

    End
}


public class Utils
{
    public static CompareRes Compare<T>(T left, T right)
    {
        int res = Comparer.Default.Compare(left, right);

        if (res < 0)
            return CompareRes.Less;
        if (res > 0)
            return CompareRes.Greater;
        return CompareRes.Equal;
    }
}
