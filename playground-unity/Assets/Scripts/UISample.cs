using UnityEngine;
using UnityEngine.UI;

public class UISample : MonoBehaviour
{
	[SerializeField] GameObject windowPrefab;
	[SerializeField] Button showButton;

	void Start()
	{
		var model = new SampleWindowModel();

		showButton.onClick.AddListener(() =>
		{
			var window = Instantiate(windowPrefab, transform).GetComponent<SampleWindow>();
			window.Init(model);
		});
	}
}
