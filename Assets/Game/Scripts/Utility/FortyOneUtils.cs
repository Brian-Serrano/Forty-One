using UnityEngine;

public static class FortyOneUtils
{
    public static string Truncate(this string value, int length)
    {
        if (value.Length > length)
        {
            return value[..length];
        }

        return value;
    }
}
