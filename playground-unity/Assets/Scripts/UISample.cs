using System;
using System.Collections.Generic;
using System.Linq;
using TinkState;
using TMPro;
using UnityEngine;

public class UISample : MonoBehaviour
{
	[SerializeField] TMP_InputField searchInput;
	[SerializeField] TMP_InputField addInput;
	[SerializeField] RectTransform list;
	[SerializeField] GameObject itemPrefab;
	[SerializeField] PaginationView pagination;

	void Start()
	{
		// supposed to be called from whoever creates the UI, but since this is an example we do this here
		Init(new UISampleModel());
	}

	public void Init(UISampleModel model)
	{
		addInput.onSubmit.AddListener(name => {
			model.AddItem(addInput.text);
			addInput.text = "";
		});
		model.SearchString.Bind(searchInput.SetTextWithoutNotify);
		searchInput.onSubmit.AddListener(model.Search);
		searchInput.onDeselect.AddListener(_ => searchInput.SetTextWithoutNotify(model.SearchString.Value));
		model.DisplayedItems.Bind(DisplayItems);
		pagination.Init(model.Pagination);
	}

	void DisplayItems(IEnumerable<string> items)
	{
		// obviously this should recycle renderers, but for the sake of simplicity let's not
		while (list.childCount > 0) DestroyImmediate(list.GetChild(0).gameObject);

		foreach (var item in items)
		{
			var go = Instantiate(itemPrefab);
			go.transform.SetParent(list);
			go.SetActive(true);
			go.GetComponent<UISampleItem>().Init(item);
		}
	}
}

public class UISampleModel
{
	public Observable<string> SearchString => searchString;
	public readonly Observable<IEnumerable<string>> DisplayedItems;
	public readonly PaginationModel Pagination;

	readonly State<string> searchString = Observable.State("");
	readonly ObservableList<string> items = Observable.List(new[]
	{
		"John Doe",
		"Jane Doe",
		"Alexey",
		"Alexander",
		"Vladimir",
		"Vladislav",
		"Oleg",
		"Denis",
		"Sergey",
		"Leonid",
		"Luca"
	});

	public UISampleModel()
	{
		var filteredItems = Observable.Auto(() =>
		{
			var searchValue = searchString.Value.ToLower();
			var filteredItems = items.Where(name => name.ToLower().Contains(searchValue)).ToList();
			filteredItems.Sort();
			return filteredItems;
		});

		const int itemsPerPage = 3;

		var numPages = Observable.Auto(() => Math.Max(1, (int)Math.Ceiling(filteredItems.Value.Count / (float)itemsPerPage)));

		Pagination = new PaginationModel(numPages);

		DisplayedItems = Observable.Auto(() => filteredItems.Value.Skip(Pagination.CurrentPageIndex.Value * itemsPerPage).Take(itemsPerPage));
	}

	public void AddItem(string name)
	{
		name = name.Trim();
		if (name == "") return;
		items.Add(name);
	}

	public void Search(string str)
	{
		searchString.Value = str.Trim();
		Pagination.GoToIndex(0);
	}
}
