using System;
using System.Diagnostics;

namespace Open.HttpProxy
{
	class TraceScope : IDisposable
	{
		private readonly Guid _oldActivityId;
		private readonly Logger _logger;
		private readonly string _activityName;

		public TraceScope(Logger logger, string activityName)
		{
			_logger = logger;
			_oldActivityId = Trace.CorrelationManager.ActivityId;
			_activityName = activityName;

			var newActivityId = Guid.NewGuid();

			if (_oldActivityId != Guid.Empty)
			{
				_logger.Transfer($"New activity... {activityName}", newActivityId);
			}
			Trace.CorrelationManager.ActivityId = newActivityId;
		}
		public void Dispose()
		{
			if (_oldActivityId != Guid.Empty)
			{
				_logger.Transfer($"back to {_activityName}", _oldActivityId);
			}
			_logger.Exit(_activityName);
			Trace.CorrelationManager.ActivityId = _oldActivityId;
		}
	}
}
