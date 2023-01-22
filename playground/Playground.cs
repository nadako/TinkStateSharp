using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TinkState;

class Playground
{
    static void Main()
    {
	    var dict = Observable.Dictionary<string, int>();
	    dict.Changes.Bind(change =>
	    {
		    Debug.WriteLine($"Got change: {change.Kind} {change.OldValue} {change.NewValue}");
	    });

	    dict["hi"] = 32;
	    dict["hi"] = 42;
	    dict.Remove("hi");
    }
}
