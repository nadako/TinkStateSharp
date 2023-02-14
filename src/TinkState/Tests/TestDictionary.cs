using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestDictionary : BaseTest
	{
		[Test]
		public void TestIsReadonly()
		{
			var dict = Observable.Dictionary<int, string>();
			Assert.That(dict.IsReadOnly, Is.False);
		}

		[Test]
		public void TestGetSetItem()
		{
			var dict = Observable.Dictionary<string, int>();
			dict["foo"] = 15;
			Helper(() => dict["foo"], 15, expect =>
			{
				expect(42, () => dict["foo"] = 42);
			});
		}

		[Test]
		public void TestCount()
		{
			var dict = Observable.Dictionary<string, int>();
			dict["foo"] = 15;
			Helper(() => dict.Count, 1, expect =>
			{
				expect(2, () => dict.Add("bar", 2));
				expect(1, () => dict.Remove("foo"));
				expect(0, () => dict.Clear());
			});
		}

		[Test]
		public void TestTryGetValue()
		{
			var dict = Observable.Dictionary<string, int>();
			Helper(() => dict.TryGetValue("foo", out var result) ? result : -1, -1, expect =>
			{
				expect(42, () => dict["foo"] = 42);
				expect(-1, () => dict.Clear());
				expect(10, () => dict.Add("foo", 10));
				expect(-1, () => dict.Remove("foo"));
			});
		}

		[Test]
		public void TestContainsKey()
		{
			var dict = Observable.Dictionary<string, int>();
			Helper(() => dict.ContainsKey("foo"), false, expect =>
			{
				expect(true, () => dict["foo"] = 42);
				expect(false, () => dict.Clear());
				expect(true, () => dict.Add("foo", 10));
				expect(false, () => dict.Remove("foo"));
			});
		}

		[Test]
		public void TestContains()
		{
			var dict = Observable.Dictionary<string, int>();
			Helper(() => dict.Contains(KeyValuePair.Create("foo", 42)), false, expect =>
			{
				expect(true, () => dict["foo"] = 42);
				expect(false, () => dict.Clear());
				expect(true, () => dict.Add("foo", 42));
				expect(false, () => dict.Remove("foo"));
			});
		}

		[Test]
		public void TestRemove()
		{
			var dict = Observable.Dictionary<string, int>();

			dict["foo"] = 42;
			dict.Remove(KeyValuePair.Create("foo", 43));
			Assert.That(dict.ContainsKey("foo"), Is.True);
			dict.Clear();

			Helper(() => dict.ContainsKey("foo"), false, expect =>
			{
				expect(true, () => dict["foo"] = 42);
				expect(false, () => dict.Remove(KeyValuePair.Create("foo", 42)));
				expect(true, () => dict.Add("foo", 42));
				expect(false, () => dict.Remove("foo"));
			});
		}

		[Test]
		public void TestKeys()
		{
			var dict = Observable.Dictionary<string, int>();
			Helper(() => string.Join(",", dict.Keys), "", expect =>
			{
				expect("foo", () => dict["foo"] = 42);
				expect("", () => dict.Clear());
				expect("foo", () => dict.Add("foo", 10));
				expect("foo,baz", () => dict.Add(KeyValuePair.Create("baz", 42)));
				expect("foo,baz,bar", () => dict["bar"] = 1);
				expect("baz,bar", () => dict.Remove("foo"));
			});
		}

		[Test]
		public void TestValues()
		{
			var dict = Observable.Dictionary<int, string>();
			Helper(() => string.Join(",", dict.Values), "", expect =>
			{
				expect("foo", () => dict[1] = "foo");
				expect("", () => dict.Clear());
				expect("foo", () => dict.Add(1, "foo"));
				expect("foo,bar", () => dict[2] = "bar");
				expect("bar", () => dict.Remove(1));
			});
		}

		[Test]
		public void TestEnumeration()
		{
			var dict = Observable.Dictionary<int, string>();
			Helper(() => string.Join(",", dict.Select(kvp => kvp.Key + "-" + kvp.Value)), "", expect =>
			{
				expect("1-foo", () => dict[1] = "foo");
				expect("", () => dict.Clear());
				expect("1-foo", () => dict.Add(1, "foo"));
				expect("1-foo,2-bar", () => dict[2] = "bar");
				expect("2-bar", () => dict.Remove(1));
			});

			dict = Observable.Dictionary<int, string>();
			Helper(() =>
			{
				var list = new List<string>();
				foreach (var obj in (IEnumerable)dict)
				{
					var kvp = (KeyValuePair<int, string>)obj;
					list.Add(kvp.Key + "-" + kvp.Value);
				}
				return string.Join(",", list);
			}, "", expect =>
			{
				expect("1-foo", () => dict[1] = "foo");
				expect("", () => dict.Clear());
				expect("1-foo", () => dict.Add(1, "foo"));
				expect("1-foo,2-bar", () => dict[2] = "bar");
				expect("2-bar", () => dict.Remove(1));
			});
		}

		[Test]
		public void TestCopyTo()
		{
			var dict = Observable.Dictionary<int, string>();
			Helper(() =>
			{
				var array = new KeyValuePair<int, string>[dict.Count];
				dict.CopyTo(array, 0);
				return string.Join(",", array.Select(kvp => kvp.Key + "-" + kvp.Value));
			}, "", expect =>
			{
				expect("1-foo", () => dict[1] = "foo");
				expect("", () => dict.Clear());
				expect("1-foo", () => dict.Add(1, "foo"));
				expect("1-foo,2-bar", () => dict[2] = "bar");
				expect("2-bar", () => dict.Remove(1));
			});
		}

		[Test]
		public void TestObserve()
		{
			var dict = Observable.Dictionary<int, string>();
			dict[1] = "a";
			dict[2] = "b";

			string s(IReadOnlyDictionary<int, string> d)
			{
				var items = d.OrderBy(kvp => kvp.Key).Select(p => p.Key + "-" + p.Value);
				return string.Join(",", items);
			}

			var observable = dict.Observe();
			Assert.That(s(observable.Value), Is.EqualTo("1-a,2-b"));

			var bindingCalls = 0;
			var expectedValue = "1-a,2-b";
			var binding = observable.Bind(d =>
			{
				bindingCalls++;
				Assert.That(s(d), Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			expectedValue = "1-a,2-b,3-c";
			dict[3] = "c";
			Assert.That(bindingCalls, Is.EqualTo(2));

			binding.Dispose();
			dict[0] = "!";
			Assert.That(bindingCalls, Is.EqualTo(2));

			var mapped = observable.Map(s);
			Assert.That(mapped.Value, Is.EqualTo("0-!,1-a,2-b,3-c"));

			var auto = Observable.Auto(() => s(observable.Value));
			Assert.That(auto.Value, Is.EqualTo("0-!,1-a,2-b,3-c"));
			dict.Remove(2);
			Assert.That(auto.Value, Is.EqualTo("0-!,1-a,3-c"));
		}

		void Helper<R>(Func<R> compute, R initialExpectedValue, Action<Action<R, Action>> tests)
		{
			var auto = Observable.Auto(compute);
			R expectedValue = initialExpectedValue;
			var bindingCalled = false;
			var binding = auto.Bind(value =>
			{
				bindingCalled = true;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalled, Is.True);

			void Expect(R newExpectedValue, Action f)
			{
				bindingCalled = false;
				expectedValue = newExpectedValue;
				f();
				Assert.That(bindingCalled, Is.True);
			};

			tests(Expect);
			binding.Dispose();
		}
	}
}
