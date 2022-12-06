using TinkState;
using TMPro;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
	[SerializeField] TMP_InputField nameInput;
	[SerializeField] TMP_Text greetingLabel;

	void Start()
	{
		// define piece of mutable observable state
		var name = Observable.State("World");

		// bind the state two-ways to an input field
		name.Bind(nameInput.SetTextWithoutNotify);
		nameInput.onValueChanged.AddListener(newValue => name.Value = newValue);

		// derive automatically updated observable value from it
		var greeting = Observable.Auto(() => $"Hello, {name.Value}!");

		// bind the auto-observable to a text field
		greeting.Bind(text => greetingLabel.text = text);
	}
}