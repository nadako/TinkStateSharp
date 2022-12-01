using System;
using TinkState;

class Playground
{
    static void Main()
    {
	    var list = Observable.List(new[] { "Dan", "John", "Alex" });
	    var obs = list.Observe();

	    Console.WriteLine("Value access: " + string.Join(", ", obs.Value));

	    obs.Bind(values =>
	    {
		    Console.WriteLine("Binding: " + string.Join(", ", values));
	    });
	    Console.WriteLine("Adding item");
	    list.Add("Vlad");
    }
}
