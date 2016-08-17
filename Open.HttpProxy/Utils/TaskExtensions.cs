using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Open.HttpProxy.Utils
{
	public static class TaskExtensions
	{
		public static ConfiguredTaskAwaitable<TResult> WithoutCapturingContext<TResult>(this Task<TResult> task)
		{
			return task.ConfigureAwait(continueOnCapturedContext: false);
		}

		public static ConfiguredTaskAwaitable WithoutCapturingContext(this Task task)
		{
			return task.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
