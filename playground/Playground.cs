using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TinkState;

class Playground
{
	class MutableObject
	{
		public string Field = "initial";
	}

    static void Main()
    {
	    var mutableObject = new MutableObject();

	    var mutableObjectSource = Observable.Manual(mutableObject);

	    void Mutate(string newFieldValue)
	    {
		    mutableObject.Field = newFieldValue;
		    mutableObjectSource.Invalidate();
	    }

	    var mutableObjectObservable = mutableObjectSource.Observe();
	    mutableObjectObservable.Bind(o => Console.WriteLine(o.Field));

	    var auto = Observable.Auto(() => mutableObjectObservable.Value.Field.ToUpperInvariant());
	    auto.Bind(v => Console.WriteLine(v));

	    Mutate("Hello");
    }
}
