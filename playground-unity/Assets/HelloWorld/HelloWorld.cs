using TinkState;
using TinkState.Model;
using TMPro;
using UnityEngine;

[Model]
class Player
{
	[Observable] public string Name { get; set; }
}

[Model]
class GreetingViewModel
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

		var greeting = Observable.Auto(() => greetingViewModel.Greeting);
		var name = Observable.Auto(() => player.Name);

		name.Bind(nameInput.SetTextWithoutNotify);
		nameInput.onValueChanged.AddListener(newValue => player.Name = newValue);

		greeting.Bind(text => greetingLabel.text = text);
	}
}