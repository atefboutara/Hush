using UnityEngine;
using System.IO;
using UnityEditor;

public class EOLFixer
{
    private const string WindowsStyle = "\r\n";
    private const string UnixStyle = "\n";
    private const string MacStyle = "\r";

    [MenuItem("Tools/EOL Fixer/Windows")]
    static void FixAsWindows()
    {
        ReadAndReplaceFiles(WindowsStyle);
    }

    [MenuItem("Tools/EOL Fixer/UNIX")]
    static void FixAsUnix()
    {
        ReadAndReplaceFiles(UnixStyle);
    }

    [MenuItem("Tools/EOL Fixer/Mac")]
    static void FixAsMac()
    {
        ReadAndReplaceFiles(MacStyle);
    }

    private static void ReadAndReplaceFiles(string style)
    {
        var path = Application.dataPath;
        var di = new DirectoryInfo(path);
        int count = ReadAndReplaceFiles(style, di);

        Debug.Log("Fixed files: " + count);
    }

    private static int ReadAndReplaceFiles(string style, DirectoryInfo di)
    {
        int count = 0;

        foreach (var file in di.GetFiles())
        {
            if (file.Extension == ".cs" || file.Extension == ".js")
            {
                string text = File.ReadAllText(file.FullName);
                string newText = ReplaceStyles(text.Replace("\r\n", "\n").Replace("\r", "\n"), style);
                if (!text.Equals(newText))
                {
                    File.WriteAllText(file.FullName, newText);
                    count++;
                }
            }
        }

        foreach (var sdi in di.GetDirectories())
        {
            count += ReadAndReplaceFiles(style, sdi);
        }

        return count;
    }

    private static string ReplaceStyles(string line, string style)
    {
        return line.Replace("\n", style);
    }

}
