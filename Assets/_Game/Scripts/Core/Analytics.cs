using System.Collections.Generic;
using UnityEngine;

public static class Analytics
{
    public static void Track(string name, Dictionary<string, object> props = null)
    {
        string details = props == null ? "" : string.Join(", ", props);
        //Debug.Log($"[analytics] {name} {details}");
    }
}