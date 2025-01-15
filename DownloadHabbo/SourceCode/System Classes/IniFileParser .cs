using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApplication
{
    public static class IniFileParser
    {
        public static Dictionary<string, string> Parse(string filePath)
        {
            var config = new Dictionary<string, string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The configuration file '{filePath}' was not found.");
            }

            string[] lines = File.ReadAllLines(filePath);
            string? currentSection = null;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    continue;
                }

                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine.Substring(separatorIndex + 1).Trim();

                    string fullKey = currentSection != null ? $"{currentSection}:{key}" : key;
                    config[fullKey] = value;
                }
            }

            return config;
        }
    }
}