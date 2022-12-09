using TinkState.Model;
using TMPro;
using UnityEngine;

class Player : Model
{
	[Observable] public string Name { get; set; }
}

class GreetingViewModel : Model
{
	[Observable] public Player Player { get; set; }

	[Observable] public string Greeting => $"Hello, {Player.Name}!";
}

public class HelloWorld : MonoBehaviour
{
	[SerializeField] TMP_InputField nameInput;
	[SerializeField] TMP_Text greetingLabel;

	void Start()
	{
		var player = new Player {Name = "Dan"};
		var greetingViewModel = new GreetingViewModel {Player = player};

		var greeting = greetingViewModel.GetObservable(m => m.Greeting);
		var name = player.GetObservable(p => p.Name);

		name.Bind(nameInput.SetTextWithoutNotify);
		nameInput.onValueChanged.AddListener(newValue => player.Name = newValue);

		greeting.Bind(text => greetingLabel.text = text);
	}
}