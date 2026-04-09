using System.IO;

namespace Doppler.UpdateCredtiCardAccount.Job;

internal static class CycleHelper
{
    public static int IncrementCounter(string counterFilePath)
    {
        var directory = Path.GetDirectoryName(counterFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        int count = 0;
        if (File.Exists(counterFilePath) &&
            int.TryParse(File.ReadAllText(counterFilePath).Trim(), out var current))
        {
            count = current;
        }

        count++;
        File.WriteAllText(counterFilePath, count.ToString());
        return count;
    }

    public static void ResetCounter(string counterFilePath)
    {
        var directory = Path.GetDirectoryName(counterFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(counterFilePath, "0");
    }
}
