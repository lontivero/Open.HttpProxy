using System;
using System.Diagnostics;

namespace Open.HttpProxy
{
	public class TraceScope : IDisposable
	{
		private readonly Guid _oldActivityId;
		private readonly TraceSource _ts;
		private readonly string _activityName;

		public TraceScope(TraceSource ts, string activityName)
		{
			_ts = ts;
			_oldActivityId = Trace.CorrelationManager.ActivityId;
			_activityName = activityName;

			var newActivityId = Guid.NewGuid();

			if (_oldActivityId != Guid.Empty)
			{
				ts.TraceTransfer(0, $"New activity... {activityName}", newActivityId);
			}
			Trace.CorrelationManager.ActivityId = newActivityId;
//			ts.TraceEvent(TraceEventType.Start, 0, activityName);
		}
		public void Dispose()
		{
			if (_oldActivityId != Guid.Empty)
			{
				_ts.TraceTransfer(0, "Transferring back...", _oldActivityId);
			}
			_ts.TraceEvent(TraceEventType.Stop, 0, _activityName);
//			Trace.CorrelationManager.ActivityId = _oldActivityId;
		}
	}
}
