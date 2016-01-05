using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
	internal class LockedValue<T>
	{
		private T _value;

		public LockedValue(T value = default(T))
		{
			_value = value;
		}

		public T Value
		{
			get { lock (this) { return _value; } }
			set { lock (this) { _value = value; } }
		}

		/// <summary>
		/// Sets the value and returns the old value.
		/// </summary>
		/// <param name="val">The value to be assigned.</param>
		/// <returns>The previous value.</returns>
		public T Set(T val)
		{
			lock (this)
			{
				T ret = _value;
				_value = val;
				return ret;
			}
		}
	}
}
