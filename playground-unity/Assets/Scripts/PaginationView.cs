using System;
using TinkState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaginationView : MonoBehaviour
{
	[SerializeField] TMP_Text label;
	[SerializeField] Button previousButton;
	[SerializeField] Button nextButton;

	public void Init(PaginationModel model)
    {
		var pageIndicatorText = Observable.Auto(() => (model.CurrentPageIndex.Value + 1) + " / " + model.NumPages.Value);
		this.DisposeOnDestroy(pageIndicatorText.Bind(text => label.text = text));

		previousButton.onClick.AddListener(model.GoToPrevious);
		this.DisposeOnDestroy(model.HasPrevious.Bind(value => previousButton.interactable = value));

		nextButton.onClick.AddListener(model.GoToNext);
		this.DisposeOnDestroy(model.HasNext.Bind(value => nextButton.interactable = value));
	}
}

public class PaginationModel
{
	public Observable<int> CurrentPageIndex => currentPageIndex;
	public readonly Observable<int> NumPages;
	public readonly Observable<bool> HasNext;
	public readonly Observable<bool> HasPrevious;

	readonly State<int> currentPageIndex;

	public PaginationModel(Observable<int> numPages)
	{
		currentPageIndex = Observable.State(0);
		NumPages = numPages;
		HasPrevious = Observable.Auto(() => currentPageIndex.Value > 0);
		HasNext = Observable.Auto(() => currentPageIndex.Value < numPages.Value - 1);
	}

	public void GoToIndex(int pageIndex)
	{
		currentPageIndex.Value = Math.Clamp(pageIndex, 0, NumPages.Value - 1);
	}

	public void GoToPrevious()
	{
		GoToIndex(currentPageIndex.Value - 1);
	}

	public void GoToNext()
	{
		GoToIndex(currentPageIndex.Value + 1);
	}
}