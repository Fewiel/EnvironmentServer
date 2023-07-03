using System;
using System.Collections.Generic;
using System.Text;

namespace EnvironmentServer.Daemon.Utility;

public class IniFile
{
	private readonly Dictionary<string, string> Data = new();

	public IniFile(string content)
	{
		ReadContent(content);
	}

	private void ReadContent(string content)
	{
		var lines = content.Split(Environment.NewLine);

		foreach (var line in lines)
		{
			var l = line.Trim();
			if (string.IsNullOrEmpty(l))
				continue;

			if (l[0] == '#' || l[0] == ';')
				continue;

			if (!l.Contains('='))
				continue;

			ReadLine(l);
		}
	}

	private void ReadLine(string line)
	{
		var lineIndex = line.IndexOf('=');
		Data.Add(line[..lineIndex].Trim('"'), line[(lineIndex + 1)..].Trim('"'));
	}

	public bool TryGetValue(string key, out string value) => Data.TryGetValue(key, out value);

	public void SetValue(string key, string value) => Data[key] = value;

	public string Write()
	{
		var sb = new StringBuilder();

		foreach (var d in Data)
		{
            sb.Append(d.Key).Append('=').AppendLine(d.Value);
		}

		return sb.ToString();
	}
}