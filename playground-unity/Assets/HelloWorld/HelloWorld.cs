using TinkState;
using TMPro;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
	[SerializeField] TMP_InputField nameInput;
	[SerializeField] TMP_Text greetingLabel;

	// define piece of mutable observable state
	[SerializeField] SerializableState<string> name = new("World");

	void Start()
	{
		// bind the state two-ways to an input field
		name.Bind(nameInput.SetTextWithoutNotify);
		nameInput.onValueChanged.AddListener(newValue => name.Value = newValue);

		// derive automatically updated observable value from it
		var greeting = Observable.Auto(() => $"Hello, {name.Value}!");

		// bind the auto-observable to a text field
		greeting.Bind(text => greetingLabel.text = text);
	}
}
