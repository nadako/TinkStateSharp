using System;
using System.Collections.Generic;
using TinkState;
using UnityEngine;

namespace TinkState
{
	[Serializable]
	public class SerializableState<T> : State<T>, ISerializationCallbackReceiver
	{
		State<T> state;
		[SerializeField] T value;

		public SerializableState(T initialValue)
		{
			state = Observable.State(initialValue);
			value = initialValue;
		}

		public T Value
		{
			get => state.Value;
			set
			{
				state.Value = value;
				this.value = value;
			}
		}

		public override string ToString() => state.ToString();

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null) =>
			state.Bind(callback, comparer, scheduler);

		public Observable<TOut> Map<TOut>(Func<T, TOut> transform, IEqualityComparer<TOut> comparer = null) => state.Map(transform, comparer);

		void ISerializationCallbackReceiver.OnBeforeSerialize() {}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			state ??= Observable.State(value);
			Value = value;
		}
	}
}
