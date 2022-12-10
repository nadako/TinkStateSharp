using System;
using TinkState.Model;

class MyModel : Model
{
	[Observable] public string Name { get; set; }
}

class Playground
{
    static void Main()
    {
	    var model = new MyModel {Name = "Dan"};
	    var obs = model.GetObservable(_ => _.Name);
	    obs.Bind(name => Console.WriteLine("Name is: " + name));
    }
}
