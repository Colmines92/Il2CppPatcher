using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Lang
{
    private Dictionary<string, string> dict = new Dictionary<string, string>();
    private bool isDefaultLang = false;

    public Lang(bool _isDefault = false) => isDefaultLang = _isDefault;

    public string GetStr(string key)
    {
        string result = key;
        if (dict.Count == 0)
            goto End;
        if (dict.ContainsKey(key))
            result = dict[key];
        else if (!isDefaultLang)
            result = global.defaultLang.GetStr(key);
    End:
        return result;
    }

    public void AddEntry(string key, string value)
    {
        if (!dict.ContainsKey(key))
            dict.Add(key, value);
        else
            dict[key] = value;
    }
}