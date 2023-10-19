using TinkState;
using TMPro;
using UnityEngine;

public class PoolAndBinding_Item : MonoBehaviour
{
	[SerializeField] TMP_Text label;

	public void Init(Observable<string> model)
	{
		gameObject.RunOnActive(() =>
		{
			return model.Bind(text =>
			{
				Debug.Log($"Setting label for {gameObject.name} to: {text}", gameObject);
				label.text = text;
			});
		});
	}

	public void OnPoolGet()
	{
		gameObject.SetActive(true);
	}

	public void OnPoolRelease()
	{
		gameObject.ClearOnActiveRuns();
		gameObject.SetActive(false);
	}
}
