using TMPro;
using UnityEngine;

public class UISampleItem : MonoBehaviour
{
	[SerializeField] TMP_Text text;

    public void Init(string data)
    {
		text.text = data;
    }
}
