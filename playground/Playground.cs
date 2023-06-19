using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TinkState;
using TinkState.Internal;

class MutableObject
{
	public string Field = "initial";
}

class Playground
{
    static void Main()
    {

	    var mutableObject = new MutableObject();
	    var state = Observable.State(mutableObject, NeverEqualityComparer<MutableObject>.Instance);

	    void Mutate(string newValue)
	    {
		    mutableObject.Field = newValue;
		    state.ForceInvalidate();
	    }

	    var auto = Observable.Auto(() => state.Value.Field.ToUpperInvariant());

	    state.Bind(o => Console.WriteLine("Field value: " + o.Field));
	    auto.Bind(value => Console.WriteLine("Field value: " + value));

	    Mutate("hello");

        // Process.GetCurrentProcess().WaitForExit();
    }
}
