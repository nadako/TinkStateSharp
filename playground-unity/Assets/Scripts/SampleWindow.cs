﻿using System;
using System.Collections.Generic;
using System.Linq;
using TinkState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SampleWindow : MonoBehaviour
{
	[SerializeField] TMP_InputField searchInput;
	[SerializeField] TMP_InputField addInput;
	[SerializeField] SampleItemList list;
	[SerializeField] PaginationView pagination;
	[SerializeField] Button closeButton;

	public void Init(SampleWindowModel model)
	{
		addInput.onSubmit.AddListener(name => {
			model.AddItem(addInput.text);
			addInput.text = "";
		});

		gameObject.DisposeOnDestroy(model.SearchString.Bind(searchInput.SetTextWithoutNotify));
		searchInput.onSubmit.AddListener(model.Search);
		searchInput.onDeselect.AddListener(_ => searchInput.SetTextWithoutNotify(model.SearchString.Value));

		gameObject.DisposeOnDestroy(model.DisplayedItems.Bind(list.DisplayItems));

		pagination.Init(model.Pagination);

		closeButton.onClick.AddListener(() => Destroy(gameObject));

		gameObject.SetActive(true);
	}
}

public class SampleWindowModel
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

	public SampleWindowModel()
	{
		var filteredItems = Observable.Auto(() =>
		{
			var searchValue = searchString.Value.ToLower();
			var filteredItems = items.Where(name => name.ToLower().Contains(searchValue)).ToList();
			filteredItems.Sort();
			return filteredItems;
		});

		const int itemsPerPage = 3;

		var numPages = filteredItems.Map(items => Math.Max(1, (int)Math.Ceiling(items.Count / (float)itemsPerPage)));

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
