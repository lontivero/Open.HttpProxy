using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	class Logger
	{
		private static TraceSource _trace = new TraceSource("Open.HttpProxy");

		public void Info(string str)
		{
			_trace.TraceEvent(TraceEventType.Information, 0, str);
		}
		public void Warning(string str)
		{
			_trace.TraceEvent(TraceEventType.Warning, 0, str);
		}
		public void Error(string str)
		{
			_trace.TraceEvent(TraceEventType.Error, 0, str);
		}
		public void Verbose(string str)
		{
			_trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}
		public TraceScope Enter(string activityName)
		{
			var traceScope = new TraceScope(this, activityName);
			_trace.TraceEvent(TraceEventType.Start, 0, activityName);
			return traceScope;
		}

		public void Transfer(string message, Guid newActivityId)
		{
			_trace.TraceTransfer(0, message, newActivityId);

		}

		public void Exit(string activityName)
		{
			_trace.TraceEvent(TraceEventType.Stop, 0, activityName);
		}

		public void LogData(TraceEventType type, object message)
		{
			_trace.TraceData(type, 0, message);
		}
	}
}
