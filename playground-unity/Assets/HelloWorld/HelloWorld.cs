using TinkState;
using TMPro;
using UnityEngine;

// a very simple example for the README
public class HelloWorld : MonoBehaviour
{
	[SerializeField] TMP_InputField nameInput;
	[SerializeField] TMP_Text greetingLabel;

	void Start()
	{
		var name = Observable.State("World");

		var greeting = Observable.Auto(() => $"Hello, {name.Value}!");

		name.Bind(nameInput.SetTextWithoutNotify);
		nameInput.onValueChanged.AddListener(newValue => name.Value = newValue);

		greeting.Bind(text => greetingLabel.text = text);
	}
}
