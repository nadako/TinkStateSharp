using UnityEngine;
using UnityEngine.UI;

public class UISample : MonoBehaviour
{
	[SerializeField] GameObject windowPrefab;
	[SerializeField] Button showButton;

	void Start()
	{
		// we create model once and reuse it every time we create a window, so the state persists between instantiations
		var model = new SampleWindowModel();
		showButton.onClick.AddListener(() =>
		{
			var window = Instantiate(windowPrefab, transform).GetComponent<SampleWindow>();
			window.Init(model);
		});
	}
}
