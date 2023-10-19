using System.Collections.Generic;
using TinkState;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class PoolAndBinding : MonoBehaviour
{
	[SerializeField] PoolAndBinding_Item itemPrefab;
	[SerializeField] Transform itemContainer;
	[SerializeField] Button addButton;
	[SerializeField] Button removeButton;
	[SerializeField] Button increaseButton;

	ObjectPool<PoolAndBinding_Item> itemPool;
	List<PoolAndBinding_Item> items;

	void Awake()
	{
		itemPool = new ObjectPool<PoolAndBinding_Item>(
			() => Instantiate(itemPrefab, itemContainer),
			item => item.OnPoolGet(),
			item => item.OnPoolRelease(),
			item => Destroy(item.gameObject)
		);
		items = new List<PoolAndBinding_Item>();
	}

	void Start()
	{
		var globalInt = Observable.State(1);
		var nextId = 1;

		addButton.onClick.AddListener(() =>
		{
			var itemId = nextId++;
			var model = Observable.Auto(() =>
			{
				Debug.Log($"Computing text for {itemId}");
				return $"{itemId} - {globalInt.Value}";
			});
			var item = itemPool.Get();
			item.name = "item-" + itemId;
			items.Add(item);
			item.Init(model);
		});

		removeButton.onClick.AddListener(() =>
		{
			if (items.Count == 0) return;
			var index = Random.Range(0, items.Count);
			var item = items[index];
			items.RemoveAt(index);
			itemPool.Release(item);
		});

		increaseButton.onClick.AddListener(() => globalInt.Value++);
	}
}
