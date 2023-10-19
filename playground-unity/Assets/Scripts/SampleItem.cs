using TMPro;
using UnityEngine;

public class SampleItem : MonoBehaviour
{
	[SerializeField] TMP_Text text;

	public void Init(string data)
	{
		text.text = data;
	}
}
