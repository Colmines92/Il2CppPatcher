using IL2CPP_EASY_PATCHER.Properties;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Forms;

public class Settings
{
    public bool MakeBackups = true;
    public string[] Recents = new string[0];
    public string Language = "en";

    public static void Save(string path = null)
    {
        global.settings.MakeBackups = global.mainfrm.tsmiMakeBackups.Checked;

        var match = global.mainfrm.tsmiRecents.DropDownItems.Cast<ToolStripItem>().Where(i => i.Name.StartsWith("tsmiRecent")).Select(i => i.ToolTipText);
        global.settings.Recents = match.ToArray();
        global.settings.Language = global.selectedLang;

        string json = JsonConvert.SerializeObject(global.settings, Formatting.Indented);
        string dir = Path.GetDirectoryName(global.configPathTmp);
        if (!Functions.CreateDirectory(dir))
            return;

        if (!Functions.CreateDirectory(Path.GetDirectoryName(global.configPath)))
            return;

        try
        {
            File.WriteAllText(path == null ? global.configPathTmp : path, json);
            File.Delete(global.configPath);
            File.Move(global.configPathTmp, global.configPath);
        }
        catch { }

        try
        {
            Directory.Delete(dir, true);
        }
        catch { }
    }

    public static void Load(string path = null)
    {
        bool valid = File.Exists(global.configPath);
        if (!valid)
        {
            if (File.Exists(global.configPathTmp))
            {
                try
                {
                    string dir = Path.GetDirectoryName(global.configPath);
                    if (Functions.CreateDirectory(dir))
                    {
                        File.Move(global.configPathTmp, global.configPath);
                        valid = true;
                    }
                    else
                        valid = false;
                }
                catch { valid = false; }
                try
                {
                    string dir = Path.GetDirectoryName(global.configPathTmp);
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }
        if (!valid)
            goto end;

        string json = null;

        try
        {
            json = File.ReadAllText(path == null ? global.configPath : path);
            global.settings = JsonConvert.DeserializeObject<Settings>(json);
        }
        catch
        {
            goto end;
        }
    end:
        global.mainfrm.tsmiMakeBackups.Checked = global.settings.MakeBackups;

        string[] reverse = global.settings.Recents.Reverse().ToArray();
        int count = 1;
        foreach (string file in reverse)
        {
            if (count > 10)
                break;

            if (!File.Exists(file))
                continue;

            ToolStripDropDownItem item = new ToolStripMenuItem()
            {
                Name = "tsmiRecent" + (count++),
                Text = Path.GetFileName(Path.GetFileName(file)),
                ToolTipText = file,
            };
            item.Click += global.mainfrm.recentsToolStripMenuItem_Click;
            global.mainfrm.tsmiRecents.DropDownItems.Insert(0, item);
        }
        global.mainfrm.tsmiRecents.Enabled = global.mainfrm.tsmiRecents.DropDownItems.Count > 2;
        global.selectedLang = global.settings.Language;
        global.lang = LoadLanguage(global.selectedLang);
        global.mainfrm.ApplyLanguage();
    }

    public static Lang LoadLanguage(string langName, bool isDefaultLang = false)
    {
        Lang result = new Lang(isDefaultLang);
        if (!ResourceExists(langName))
        {
            if (!isDefaultLang && string.Equals(langName, global.selectedLang, StringComparison.InvariantCultureIgnoreCase) && langName != "en")
                global.selectedLang = "en";
            return result;
        }

        ResourceManager rm = Resources.ResourceManager;
        string[] lines = rm.GetString(langName).Split(new string[] { "\r\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; ++i)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("["))
            {
                string key = line.Trim('[', ']');
                int nextLine = ++i;
                if (lines.Length > nextLine)
                {
                    string value = lines[nextLine].Trim().Replace("\\n", "\n");
                    result.AddEntry(key, value);
                }
            }
        }

        return result;
    }

    public static bool ResourceExists(string resourceName)
    {
        ResourceManager rm = Resources.ResourceManager;
        object resource = rm.GetObject(resourceName);
        return (resource != null);
    }
}