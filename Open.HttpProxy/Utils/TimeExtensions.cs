using System;

namespace Open.HttpProxy.Utils
{
	static class TimeExtensions
	{
		internal static TimeSpan Seconds(this int seconds)
		{
			return TimeSpan.FromSeconds(seconds);
		}

		internal static TimeSpan Milliseconds(this int milliseconds)
		{
			return TimeSpan.FromMilliseconds(milliseconds);
		}
	}
}
