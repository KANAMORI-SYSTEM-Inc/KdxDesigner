using System.Runtime.InteropServices;
using System.Text;
namespace KdxDesigner.Utils.ini
{
    internal class IniHelper
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
           string section, string key, string defaultValue,
           StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(
            string section, string key, string value, string filePath);

        public static string? ReadValue(string section, string key, string path)
        {
            var retVal = new StringBuilder(512);
            GetPrivateProfileString(section, key, "", retVal, 512, path);
            return string.IsNullOrWhiteSpace(retVal.ToString()) ? null : retVal.ToString();
        }

        public static void WriteValue(string section, string key, string value, string path)
        {
            WritePrivateProfileString(section, key, value, path);
        }

    }
}
