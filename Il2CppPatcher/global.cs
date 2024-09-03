using KeystoneNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using IL2CPP_EASY_PATCHER;

public class global
{
    public static string selectedLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

    public static Dictionary<string, string> implementedLanguages = new Dictionary<string, string>();
    public static Lang defaultLang = new Lang();
    public static Lang lang = new Lang();

    public static Dictionary<string, string[]> returnTypes = new Dictionary<string, string[]>();
    public static LoadedFile loadedFile = new LoadedFile();
    public static Keystone keystone;

    public static PatchData patchData = new PatchData();
    public static Settings settings = new Settings();

    public static MainForm mainfrm = null;

    public static string configPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\{Application.CompanyName}\{Application.ProductName}\config.json";
    public static string configPathTmp = $@"{Path.GetTempPath()}\{Application.ProductName}\config.json";
    public static string myGithub = "https://github.com/Colmines92";
}