using System;
using System.Linq.Expressions;

namespace TinkState.Model
{
	public interface Model { }

	interface ModelInternal
	{
		Observable<T> GetObservable<T>(string field);
	}

	public static class ModelExtensions
	{
		// TODO: check if aggressive inlining helps with avoiding allocations and type casts in the release-mode code

		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Observable<T> GetObservable<M, T>(this M model, Expression<Func<M, T>> expr) where M : Model
		{
			var memberExpr = expr.Body as MemberExpression;
			if (memberExpr == null) throw new Exception("Member expression is expected");
			if (!(memberExpr.Expression is ParameterExpression)) throw new Exception("Expression in form of `v => v.Field` expected");
			var fieldName = memberExpr.Member.Name;
			return GetObservable<T>(model, fieldName);
		}

		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Observable<T> GetObservable<T>(this Model model, string field)
		{
			return ((ModelInternal)model).GetObservable<T>(field);
		}
	}
}
