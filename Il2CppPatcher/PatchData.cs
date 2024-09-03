using Newtonsoft.Json;
using System.Collections.Generic;

public class PatchData
{
    [JsonIgnore]
    public ulong baseAddress = 0;
    public string BaseAddress { get; set; } = "0x00000000";
    public int Architecture { get; set; } = 0;
    public Patch[] Patches { get; set; } = new Patch[0];
    public Dictionary<string, string> Constants { get; set; } = new Dictionary<string, string>();
}

public class Patch
{
    public bool Enabled = false;
    public string Name = "";
    public string Address = "";
    public string Value = "";
}