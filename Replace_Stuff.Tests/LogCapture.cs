using System.Text.RegularExpressions;
using UnityEngine;

namespace Replace_Stuff.Tests;

/// <summary>Records whether an error matching the pattern was logged while this is alive.</summary>
internal sealed class LogCapture : IDisposable
{
	private readonly Regex pattern;

	public bool Seen { get; private set; }

	public LogCapture(string pattern)
	{
		this.pattern = new Regex(Regex.Escape(pattern));
		Application.logMessageReceivedThreaded += OnLog;
	}

	private void OnLog(string message, string stackTrace, LogType type)
	{
		if (type is LogType.Error or LogType.Exception && pattern.IsMatch(message))
			Seen = true;
	}

	public void Dispose() => Application.logMessageReceivedThreaded -= OnLog;
}
