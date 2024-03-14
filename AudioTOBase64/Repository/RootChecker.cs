using System;
using System.IO;

public class RootChecker
{
    public static bool IsDeviceRooted()
    {
        // Check for the existence of known root files or directories
        string[] rootIndicators = { "/system/bin/su", "/system/xbin/su", "/sbin/su", "/system/app/Superuser.apk", "/system/app/SuperSU.apk" };

        foreach (string path in rootIndicators)
        {
            if (File.Exists(path))
            {
                return true; // Root indicator found
            }
        }

        // Check for system properties associated with rooted devices
        string[] rootProperties = { "ro.build.tags", "ro.debuggable", "ro.secure", "ro.build.su" };

        foreach (string property in rootProperties)
        {
            string value = GetSystemProperty(property);
            if (!string.IsNullOrEmpty(value) && value.Trim().ToLower() == "1")
            {
                return true; // Root property found
            }
        }

        return false; // No root indicators found
    }

    private static string GetSystemProperty(string property)
    {
        try
        {
            string buildPropPath = "/system/build.prop";
            if (File.Exists(buildPropPath))
            {
                string[] lines = File.ReadAllLines(buildPropPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith(property))
                    {
                        return line.Split('=')[1].Trim();
                    }
                }
            }
        }
        catch (Exception)
        {
            // Error reading build.prop file
        }

        return null;
    }
}
