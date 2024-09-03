using IL2CPP_EASY_PATCHER.Properties;
using Ionic.Zip;
using KeystoneNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class Functions
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FlushFileBuffers(IntPtr hFile);

    public static bool ValidateRow(ref Keystone keystone, DataGridView gridView, int row)
    {
        bool result = false;

        if (row == -1)
            return false;

        if (gridView[3, row].Value != null)
        {
            string strVal = gridView[3, row].Value.ToString().TrimEnd().TrimEnd(';');
            string strValFinal = ReplaceValues(strVal, global.patchData.Constants).ToLowerInvariant();

            string convStr = strValFinal.StartsWith("0x") ? "" : ValidateFloatOrDouble(strValFinal);

            if (convStr != "")
            {
                if (convStr.EndsWith("d") || convStr.EndsWith("f"))
                    gridView[3, row].Value = convStr;
                result = true;
                goto End;
            }
            else
            {
                byte[] asm = keystone.Assemble(strValFinal, global.patchData.baseAddress).Buffer;
                if (asm.Length > 0)
                {
                    result = true;
                    goto End;
                }
            }

            if (strValFinal == "true" || strValFinal == "false" || strValFinal == "t" || strValFinal == "f")
            {
                if (strValFinal == "t")
                    strValFinal = "true";
                else if (strValFinal == "f")
                    strValFinal = "false";

                bool bVal = false;
                bool.TryParse(strValFinal, out bVal);
                gridView[3, row].Value = bVal.ToString();
                result = true;
                goto End;
            }
            else if (strValFinal == "null" || strValFinal == "return")
            {
                gridView[3, row].Value = strValFinal;
                goto End;
            }
            else if (strValFinal.StartsWith("@"))
            {
                result = true;
                goto End;
            }
            else if (strValFinal.Contains(" ") || (strValFinal.StartsWith("{") && strValFinal.EndsWith("}")))
            {
                string tmp = strValFinal.Replace("{", "").Replace("}", "").Trim().Replace(" ", "").ToUpperInvariant();
                if (IsHexadecimal(tmp) != IsHexResult.invalid)
                {
                    gridView[3, row].Value = ConvertToHexString(tmp);
                    result = true;
                }
                goto End;
            }
            else if (IsHexadecimal(strVal) != IsHexResult.invalid)
            {
                gridView[3, row].Value = ValidateInt(strVal);
                result = true;
                goto End;
            }
        }
        else
        {
            if (gridView[2, row].Value != null && gridView[2, row].Value.ToString() != "")
                goto End;
        }

        result = false;
    End:
        return result;
    }

    public static string ValidateFloatOrDouble(string input)
    {
        // Regex pattern to match float or double
        string pattern = @"^[+-]?(\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?[fFdD]?$";
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(input) && (input.Contains(".") || input.EndsWith("f") || input.EndsWith("d")))
        {
            int decimalPlaces = input.Contains('.') ? input.Split('.')[1].Length - ((input.EndsWith("f") || input.EndsWith("d")) ? 1 : 0) : 0;
            string format = "0." + new string('0', decimalPlaces);

            if (input.EndsWith("d", StringComparison.OrdinalIgnoreCase))
            {
                double val = double.Parse(input.Substring(0, input.Length - 1));
                return val.ToString(format, CultureInfo.InvariantCulture) + "d";
            }
            else
            {
                float val = float.Parse(input.Substring(0, input.Length - (input.EndsWith("f", StringComparison.OrdinalIgnoreCase) ? 1 : 0)));
                return val.ToString(format, CultureInfo.InvariantCulture) + "f";
            }
        }
        return "";
    }

    public static string ValidateInt(string strVal, bool forceHex = false, int digits = 2)
    {
        strVal = strVal.ToLowerInvariant();
        long val = 0;

        bool hex = IsHexadecimal(strVal) == IsHexResult.hex;

        if (hex)
        {
            if (!strVal.StartsWith("0x"))
            {
                if (strVal.Length > 8)
                    strVal = strVal.Substring(0, 8);
                strVal = "0x" + strVal;
            }
            else
            {
                if (strVal.Length > 10)
                    strVal = strVal.Substring(0, 10);
            }
        }

        long.TryParse(hex ? strVal.Substring(2) : strVal, hex || forceHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Number, null, out val);

        string prepend = hex || forceHex ? "0x" : "";
        string result = hex || forceHex ? prepend + val.ToString("X" + digits) : val.ToString();
        return result;
    }

    public static KeystoneMode DetectFileArch(string fileName)
    {
        KeystoneMode result = KeystoneMode.KS_MODE_ARM;

        int minimumFileSize = 64;

        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    if (fileInfo.Length < minimumFileSize)
                        goto CheckARM;

                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peHeaderOffset = br.ReadInt32();
                    fs.Seek(peHeaderOffset, SeekOrigin.Begin);
                    uint peHeader = br.ReadUInt32();

                    if (peHeader != 0x00004550)
                        goto CheckARM;

                    ushort machine = br.ReadUInt16();
                    if (machine == 0x8664)
                    {
                        result = KeystoneMode.KS_MODE_64;
                        goto End;
                    }
                    else if (machine == 0x014c)
                    {
                        result = KeystoneMode.KS_MODE_32;
                        goto End;
                    }

                CheckARM:
                    minimumFileSize = 52;
                    if (fileInfo.Length < minimumFileSize)
                        goto End;

                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] e_ident = br.ReadBytes(16);
                    if (e_ident[0] != 0x7F || e_ident[1] != 'E' || e_ident[2] != 'L' || e_ident[3] != 'F')
                        goto End;

                    bool is64Bit = e_ident[4] == 2;

                    fs.Seek(18, SeekOrigin.Begin);
                    machine = br.ReadUInt16();

                    if (machine == 0x28) // ARM
                    {
                        if (is64Bit)
                        {
                            result = KeystoneMode.KS_MODE_V8;
                            goto End;
                        }
                        else goto End;
                    }
                    else if (machine == 0xB7) // AArch64 (ARMv8)
                    {
                        result = KeystoneMode.KS_MODE_V8;
                        goto End;
                    }
                }
            }
        }
        catch { }
    End:
        return result;
    }

    enum IsHexResult
    {
        invalid,
        dec,
        hex
    }

    static IsHexResult IsHexadecimal(string input)
    {
        if (Regex.IsMatch(input, @"^\d+$"))
            return IsHexResult.dec;

        string tmp = input;
        if (input.StartsWith("0x"))
            tmp = input.Substring(2);
        if (Regex.IsMatch(tmp, @"\A\b[0-9a-fA-F]+\b\Z"))
            return IsHexResult.hex;

        return IsHexResult.invalid;
    }

    public static byte HelperBool(string strVal)
    {
        strVal = strVal.ToLowerInvariant();
        return (byte)(strVal == "true" ? 1 : 0);
    }

    public static string ReplaceValues(string strVal, Dictionary<string, string> replacements)
    {
        string pattern = string.Join("|", replacements.Keys.Select(Regex.Escape));
        return replacements.Count > 0
            ? Regex.Replace(strVal, pattern, match => replacements[match.Value])
            : strVal;
    }

    public static byte[] HelperBytes(ref Keystone keystone, string strVal, ulong position)
    {
        byte[] result = new byte[0];
        if (!keystone.Enabled)
            goto End;

        string strValFinal = ReplaceValues(strVal, global.patchData.Constants).ToLowerInvariant();
        bool hex = strValFinal.StartsWith("0x");
        position = global.patchData.baseAddress + position;

        string convStr = strValFinal.StartsWith("0x") ? "" : ValidateFloatOrDouble(strValFinal);

        if (convStr != "")
        {
            if (convStr.EndsWith("d"))
            {
                double dVal = 0.0f;
                double.TryParse(convStr.EndsWith("d") ? convStr.Substring(0, convStr.Length - 1) : convStr, out dVal);

                int val = Convert.ToInt32(dVal);
                result = Assemble(ref keystone, "mov eax, {0}; ret", val.ToString(), position);
            }
            else if (convStr.EndsWith("f"))
            {
                float fVal = 0.0f;
                float.TryParse(convStr.EndsWith("f") ? convStr.Substring(0, convStr.Length - 1) : convStr, out fVal);

                int val = Convert.ToInt32(fVal);
                result = Assemble(ref keystone, "mov eax, {0}; ret", val.ToString(), position);
            }
            goto End;
        }
        else
        {
            KeystoneEncoded asm = keystone.Assemble(strValFinal, position);
            if (asm != null && asm.Buffer.Length > 0)
            {
                result = asm.Buffer;
                goto End;
            }
        }

        if (strValFinal == "true" || strValFinal == "false")
        {
            byte val = Functions.HelperBool(strValFinal);
            result = Assemble(ref keystone, "mov eax, {0}; ret", val.ToString(), position);
        }
        else if (strValFinal == "null" || strValFinal == "return")
        {
            result = Assemble(ref keystone, "ret", "", position);
        }
        else if (strValFinal.Contains(" ") || (strValFinal.StartsWith("{") && strValFinal.EndsWith("}")))
        {
            string tmp = strValFinal.Replace("{", "").Replace("}", "").Trim().Replace(" ", "").ToUpperInvariant();
            if (IsHexadecimal(tmp) != IsHexResult.invalid)
                result = ParseBytes(strValFinal);
            else
                result = new byte[0];
        }
        else if (IsHexadecimal(strValFinal) != IsHexResult.invalid)
        { 
                int val = 0;
                int.TryParse(hex ? strValFinal.Substring(2) : strValFinal, hex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Number, null, out val);
                result = Assemble(ref keystone, "mov eax, {0}; ret", val.ToString(), position);
        }

    End:
        return result;
    }

    public static byte[] Assemble(ref Keystone keystone, string instr, string val, ulong address)
    {
        switch (instr)
        {
            case "mov eax, {0}; ret":
                switch (keystone.Mode)
                {
                    case KeystoneMode.KS_MODE_ARM:
                    case KeystoneMode.KS_MODE_V8:
                        return keystone.Assemble(String.Format("mov r0, {0}; bx lr", val), address).Buffer;

                    case KeystoneMode.KS_MODE_32:
                    case KeystoneMode.KS_MODE_64:
                        return keystone.Assemble(String.Format("mov eax, {0}; ret", val), address).Buffer;

                    default:
                        break;
                }
                break;

            case "ret":
                switch (keystone.Mode)
                {
                    case KeystoneMode.KS_MODE_ARM:
                    case KeystoneMode.KS_MODE_V8:
                        return keystone.Assemble("bx lr", address).Buffer;

                    case KeystoneMode.KS_MODE_32:
                    case KeystoneMode.KS_MODE_64:
                        return keystone.Assemble("ret", address).Buffer;

                    default:
                        break;
                }
                break;

            default:
                break;
        }

        return new byte[0];
    }

    public static long ParseInt(string strVal)
    {
        strVal = strVal.ToLowerInvariant();
        long val = 0;
        bool hex = strVal.StartsWith("0x");
        long.TryParse(hex ? strVal.Substring(2) : strVal, hex ? NumberStyles.HexNumber : NumberStyles.Number, null, out val);
        return val;
    }

    public static byte[] ParseBytes(string input)
    {
        List<byte> result = new List<byte>();

        input = input.Replace("{", "").Replace("}", "").Trim().Replace(" ", "").ToUpperInvariant();

        if (input.Length == 0)
            goto End;

        if (input.Length % 2 != 0)
            input = "0" + input;

        for (int i = 0; i < input.Length; i += 2)
        {
            string newPair = input.Substring(i, 2);

            int val = -1;
            int.TryParse(newPair, NumberStyles.HexNumber, null, out val);

            if (val == -1)
                val = 0;

            result.Add((byte)val);
        }

    End:
        return result.ToArray();
    }

    public static string ConvertToHexString(string inputStr)
    {
        string input = inputStr.Replace("{", "").Replace("}", "").Trim().Replace(" ", "").ToUpperInvariant();
        string output = "";

        if (input.Length == 0)
            goto End;

        if (input.Length % 2 != 0)
            input = "0" + input;

        for (int i = 0; i < input.Length; i += 2)
        {
            if (i > 0)
                output += " ";

            string newPair = input.Substring(i, 2);

            int val = -1;
            int.TryParse(newPair, System.Globalization.NumberStyles.HexNumber, null, out val);

            if (val == -1)
                return inputStr;

            output += val.ToString("X2");
        }

    End:
        return "{ " + output + " }";
    }

    public static void AddEntry(ref CustomDataGridView gridView)
    {
        bool isMain = gridView.Tag != null && gridView.Tag.ToString() == "1";
        bool valid = false;
        if (gridView.Rows.Count == 0)
        { 
            valid = true;
            goto AddRow;
        }

        valid = isMain
            ? Functions.ValidateRow(ref global.keystone, gridView, gridView.RowCount - 1) || gridView.RowCount == 0
            : gridView[0, gridView.RowCount - 1].Value != null && gridView[0, gridView.RowCount - 1].Value.ToString().Trim() != "" && gridView[1, gridView.RowCount - 1].Value != null && gridView[1, gridView.RowCount - 1].Value.ToString().Trim() != "";

    AddRow:
        if (valid)
        {
            gridView.Rows.Add();
            gridView.CurrentCell = gridView[Convert.ToInt32(isMain), gridView.RowCount - 1];
        }
    }

    public static void DeleteEntry(ref CustomDataGridView gridView)
    {
        if (gridView.SelectedCells.Count > 0 && gridView.RowCount > 1)
        {
            bool isMain = gridView.Tag != null && gridView.Tag.ToString() == "1";
            if (!gridView.Rows[gridView.SelectedCells[0].RowIndex].IsNewRow)
                gridView.Rows.RemoveAt(gridView.SelectedCells[0].RowIndex);
            gridView.CurrentCell = gridView[Convert.ToInt32(isMain), gridView.CurrentRow.Index];
        }
        else
            ClearEntries(ref gridView);
    }

    public static void ClearEntries(ref CustomDataGridView gridView)
    {
        gridView.Rows.Clear();
        AddEntry(ref gridView);
    }

    public static bool ExtractKeystone(string path)
    {
        if (File.Exists(Path.Combine(path, "keystone.dll")))
            return true;

        using (ZipFile zip = ZipFile.Read(new MemoryStream(Resources.keystone)))
        {
            try
            {
                foreach (ZipEntry entry in zip)
                    entry.Extract(path, ExtractExistingFileAction.OverwriteSilently);
                return true;
            }
            catch { }
        }
        return false;
    }

    public static bool CreateDirectory(string dest)
    {
        bool result = false;
        if (Directory.Exists(dest))
            return true;
        string[] subfolders = dest.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
        string currentFolder = "";

        foreach (string folder in subfolders)
        {
            currentFolder += folder + Path.DirectorySeparatorChar.ToString();
            if (!Directory.Exists(currentFolder))
            {
                try
                {
                    Directory.CreateDirectory(currentFolder);
                }
                catch { goto exit; }
            }
        }
        result = true;
    exit:
        return result;
    }

    public static bool MakeBackup(string src, string dst, bool ovr = true)
    {
        if (!CreateDirectory(Path.GetDirectoryName(dst)))
            return false;

        if (!ovr)
            if (File.Exists(dst))
                return true;

        bool result = false;
        string tmp = dst + "~";

        CopyFile(src, tmp);
        result = MoveFile(tmp, dst, true);

        return result;
    }

    public static bool CopyFile(string src, string dst)
    {
        if (!CreateDirectory(Path.GetDirectoryName(dst)))
            return false;

        try
        {
            using (FileStream srcFile = new FileStream(src, FileMode.Open, FileAccess.Read))
            using (FileStream dstFile = new FileStream(dst, FileMode.Create, FileAccess.Write))
            {
                srcFile.CopyTo(dstFile);
                dstFile.Flush();
                FlushFileBuffers(dstFile.SafeFileHandle.DangerousGetHandle());
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static bool MoveFile(string src, string dst, bool forceDelete = false)
    {
        if (!CreateDirectory(Path.GetDirectoryName(dst)))
            return false;

        try
        {
            DeleteFile(dst);
            File.Move(src, dst);
        }
        catch
        {
            if (forceDelete)
                DeleteFile(src);
            return false;
        }

        return true;
    }

    public static bool DeleteFile(string src)
    {
        if (!CreateDirectory(Path.GetDirectoryName(src)))
            return false;

        try
        {
            File.Delete(src);
        }
        catch { return false; }
        return true;
    }

    public static string GetTitle(bool showVersion = false, bool showLoadedFile = false, bool showLoadedConfig = false)
    {
        string result = Application.ProductName;

        if (showVersion)
            result += " v" + GetVersion();

        if (showLoadedFile || showLoadedConfig)
        {
            if (global.loadedFile.FileName != "" || global.loadedFile.PatchFileName != "")
            {
                result += " -";

                if (global.loadedFile.FileName != "")
                    result += " " + global.loadedFile.ShortName;

                if (global.loadedFile.PatchFileName != "")
                    result += " (" + global.loadedFile.PatchShortName + ")";
            }
        }

        return result;
    }

    public static string GetVersion()
    {
        string result = "";

        List<string> split = new List<string>();
        split.AddRange(Application.ProductVersion.Split('.'));

        for (int i = split.Count - 1; i > 1; i--)
        {
            if (split[i] == "0")
                split.RemoveAt(i);
        }

        result = split[0];

        for (int i = 1; i < split.Count; i++)
            result += "." + split[i];

        return result;
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}