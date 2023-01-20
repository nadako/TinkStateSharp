using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace TinkState.Model
{
	/// <summary>
	/// Enable support for the <see cref="ObservableAttribute">[Observable]</see> attribute on properties of the implementing class.
	/// </summary>
	public interface Model { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface ModelInternal
	{
		Observable<T> GetObservable<T>(string field);
	}

	/// <summary>
	/// Extension methods for implementors of the <see cref="Model"/> interface.
	/// </summary>
	public static class ModelExtensions
	{
		/// <summary>
		/// Get the underlying backing <see cref="Observable{T}"/> object for given property with the <see cref="ObservableAttribute">[Observable]</see> attribute.
		/// </summary>
		/// <param name="model">Model class instance</param>
		/// <param name="expr">Lambda expression in form of <c>_ => c.Property</c></param>
		/// <typeparam name="M">Type of the model to retrieve an observable from.</typeparam>
		/// <typeparam name="T">Type of the property to retrieve an observable for.</typeparam>
		/// <returns>Backing <see cref="Observable{T}"/> object for given property.</returns>
		public static Observable<T> GetObservable<M, T>(this M model, Expression<Func<M, T>> expr) where M : Model
		{
			var memberExpr = expr.Body as MemberExpression;
			if (memberExpr == null) throw new Exception("Member expression is expected");
			if (!(memberExpr.Expression is ParameterExpression)) throw new Exception("Expression in form of `v => v.Field` expected");
			var fieldName = memberExpr.Member.Name;
			return GetObservable<T>(model, fieldName);
		}

		/// <summary>
		/// Get the underlying backing <see cref="Observable{T}"/> object for given property with the <see cref="ObservableAttribute">[Observable]</see> attribute.
		/// </summary>
		/// <remarks>
		/// This method is not type safe, prefer <see cref="GetObservable{M,T}"/> instead.
		/// </remarks>
		/// <param name="model">Model class instance</param>
		/// <param name="field">Name of the string</param>
		/// <typeparam name="T">Type of the property to retrieve an observable for.</typeparam>
		/// <returns>Backing <see cref="Observable{T}"/> object for given property.</returns>
		public static Observable<T> GetObservable<T>(this Model model, string field)
		{
			return ((ModelInternal)model).GetObservable<T>(field);
		}
	}
}
