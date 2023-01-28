using TinkState;
using TMPro;
using UnityEngine;

public class PoolAndBinding_Item : MonoBehaviour
{
	[SerializeField] TMP_Text label;

    public void Init(Observable<string> model)
    {
        gameObject.RunWhenEnabled(() =>
        {
	        return model.Bind(text =>
	        {
		        Debug.Log($"Changing value (go={gameObject.name}) to: text", gameObject);
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
	    gameObject.ClearWhenEnabledRuns();
	    gameObject.SetActive(false);
    }
}
