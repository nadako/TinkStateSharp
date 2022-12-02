using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SampleItemList : MonoBehaviour
{
	[SerializeField] GameObject itemPrefab;

	ObjectPool<SampleItem> itemViewPool;
	readonly List<SampleItem> currentItems = new List<SampleItem>();

	public void DisplayItems(IEnumerable<string> items)
	{
		itemViewPool ??= CreateItemViewPool();

		foreach (var itemView in currentItems)
		{
			itemViewPool.Release(itemView);
		}

		currentItems.Clear();

		foreach (var item in items)
		{
			var itemView = itemViewPool.Get();
			itemView.Init(item);
			currentItems.Add(itemView);
		}
	}

	ObjectPool<SampleItem> CreateItemViewPool()
	{
		return new ObjectPool<SampleItem>(
			createFunc: () =>
			{
				var go = Instantiate(itemPrefab, transform);
				go.SetActive(true);
				return go.GetComponent<SampleItem>();
			},
			actionOnGet: item => item.gameObject.SetActive(true),
			actionOnRelease: item => item.gameObject.SetActive(false),
			actionOnDestroy: item => Destroy(item.gameObject)
		);
	}
}