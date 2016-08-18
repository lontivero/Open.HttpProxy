using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{
	public class ProcessStep
	{
		public ProcessStep(Enum state, Enum evnt, Func<Task> action)
		{
			State = state;
			Action = action;
			Event = evnt;
		}

		public Enum Event { get; }
		public Enum State { get; }
		public Func<Task> Action { get; }
	}

	public class CommandConfig
	{
		private readonly Enum _command;
		private readonly ProcessStateConfig _stateConfig;

		public CommandConfig(Enum command, ProcessStateConfig stateConfig)
		{
			_command = command;
			_stateConfig = stateConfig;
		}

		public ProcessStateConfig Then(Enum newState, string trace = null)
		{
			_stateConfig.Add(_command, newState, trace);
			return _stateConfig;
		}
	}

	public class ProcessStateConfig
	{
		private readonly Enum _state;
		private readonly StateMachine _context;

		public ProcessStateConfig(Enum state, StateMachine stateMachine)
		{
			_state = state;
			_context = stateMachine;
		}

		public CommandConfig If(Enum command)
		{
			return new CommandConfig(command, this);
		}

		public void Add(Enum command, Enum newState, string trace=null )
		{
			if (!_context.States.ContainsKey(_state))
			{
				_context.States.Add(_state, new Dictionary<Enum, Enum>());
			}
			_context.States[_state][command] = newState;
			_context.Traces[newState] = trace;
		}
	}

	public class StateMachine
	{
		private readonly Dictionary<Enum, Dictionary<Enum, Enum>> _states;
		private Enum _currentState;
		private readonly Enum _finalState;
		private readonly Enum _initialEvent;
		private readonly Dictionary<Enum, Func<Session, Task<Enum>>> _actionTable;
		private readonly Dictionary<Enum, string> _traces;

		internal Dictionary<Enum, string> Traces => _traces;
		internal Dictionary<Enum, Dictionary<Enum, Enum>> States => _states;

		public StateMachine(
			Enum initialState, 
			Enum finalState, 
			Enum initialEvent,
			Dictionary<Enum, Func<Session, Task<Enum>>> actionTable, 
			Dictionary<Enum, string> traces)
		{
			_finalState = finalState;
			_actionTable = actionTable;
			_initialEvent = initialEvent;
			_traces = traces;
			_currentState = initialState;
			_states = new Dictionary<Enum, Dictionary<Enum, Enum>>();
			_traces = new Dictionary<Enum, string>();
		}

		public async Task RunAsync(Session session)
		{
			using (new TraceScope(HttpProxy.Trace, $"Procession session {session.Id}"))
			{
				var evnt = _initialEvent;
				do
				{
					try
					{
						var nextState = _states[_currentState][evnt];
						var action = _actionTable[nextState];
						var trace = _traces[nextState];
						
						using (new TraceScope(HttpProxy.Trace, trace))
						{
							evnt = await action(session).WithoutCapturingContext();
						}
						_currentState = nextState;
					}
					catch (KeyNotFoundException e)
					{
						Console.WriteLine($"Key: {evnt} not found");
						throw;
					}
				} while (!Equals(_currentState, _finalState));
			}
		}

		public ProcessStateConfig OnState(Enum state)
		{
			return new ProcessStateConfig(state, this);
		}
	}
}
