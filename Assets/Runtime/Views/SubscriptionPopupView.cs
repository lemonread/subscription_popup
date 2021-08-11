using GeneralUtils.Extensions;
using InceptionPlugins.Pluggables.Purchase;
using NetworkUIHelpers.Images;
using PhoenixServices.Models;
using SubscriptionBundlePopups.Payloads;
using SubscriptionBundlePopups.Views.Cell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UITools.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace SubscriptionBundlePopups.Views
{
    [Serializable]
    public class SubscriptionPopupView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _termsText;
        [SerializeField] private Image _wallOfText;
        [SerializeField] private Image _backgroundContentImage;
        [SerializeField] private RectTransform _contentRect;

        [SerializeField] private VerticalLayoutGroup _verticalGroup;
        [SerializeField] private TextMeshProUGUI _tryBookText;
        [SerializeField] private Transform _tryBookAnchor;
        [SerializeField] private HorizontalLayoutGroup _productsHorizontalLayout;
        [SerializeField] private SubscriptionPackageCell[] _subscriptionPackages;
        [SerializeField] private Image _logosImage;

        [Header("Slider")] [SerializeField] private PageView _pageView;

        [SerializeField] private SubscriptionInfoCell _pagingPrefab;

        [Space] [Header("Footer")] [SerializeField]
        private TextMeshProUGUI _termsAndCondition;

        [SerializeField] private TextMeshProUGUI _privacyPolicy;
        [SerializeField] private TextMeshProUGUI _details;
        [SerializeField] private TextMeshProUGUI _restorePurchase;
        [SerializeField] private Graphic _upArrow;
        [SerializeField] private Graphic _downArrow;
        [SerializeField] private Image _footerBackground;

        public GameObject Root => _root;

        public void SetUIElementsData(SubscriptionUiConfigPayload payload)
        {
            SetVerticalGroupParams(payload);
            SetTryBookParams(payload);
            SetProductHorizontalLayoutParams(payload);
            SetTermsText(payload);
            SetPageViewParams(payload);

            SetColorToGraphics(payload.FooterElementsColor,
                _termsAndCondition,
                _privacyPolicy,
                _details,
                _restorePurchase, _upArrow,
                _downArrow);
        }

        private void SetTermsText(SubscriptionUiConfigPayload payload)
        {
            if (_termsText == null) return;
            _termsText.text = payload.TermText;
        }

        private void SetPageViewParams(SubscriptionUiConfigPayload payload)
        {
            if (_pageView == null) return;
            _pageView.AutoScrollInterval = payload.ScrollInterval;
            _pageView.AutoScrollAnimDuration = payload.ScrollAnimationTime;
        }

        private void SetTryBookParams(SubscriptionUiConfigPayload payload)
        {
            if (_tryBookText == null) return;
            _tryBookText.color = payload.TryButtonColor;
            _tryBookAnchor.SetSiblingIndex(payload.TryButtonHierarchyIndex);
        }

        private void SetProductHorizontalLayoutParams(SubscriptionUiConfigPayload payload)
        {
            if (_productsHorizontalLayout == null) return;
            _productsHorizontalLayout.spacing = payload.ProductHorizontalSpacing;
        }

        private void SetVerticalGroupParams(SubscriptionUiConfigPayload payload)
        {
            if (_verticalGroup == null) return;

            _verticalGroup.spacing = payload.VerticalSpacing;
            _verticalGroup.padding.top = payload.VerticalLayoutTopPadding;
        }

        public void FitRatio(Canvas canvas)
        {
            var canvasRect = (canvas.transform as RectTransform).rect;
            _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x,
                canvasRect.height + _wallOfText.rectTransform.rect.height);
        }

        private void SetColorToGraphics(Color color, params Graphic[] graphics)
        {
            foreach (var graphic in graphics.FilterNulls())
            {
                graphic.color = color;
            }
        }

        // TODO: Need refactor PageView class to Init with custom payload class and not complex dictionaries
        public Task InitPageView(params Dictionary<string, string>[] dataDicts) => _pageView == null
            ? Task.CompletedTask
            : _pageView.InitPagingContent(_pagingPrefab, dataDicts);

        public async Task InitSubscriptionOptions(
            List<(ProductModel productModel, IProductMetadata productMetadata)> data, 
            Action<ProductModel> onBuyClicked)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < data.Count; i++)
            {
                // turn off product view if we have too much
                if (i >= data.Count)
                {
                    _subscriptionPackages[i].gameObject.SetActive(false);
                    continue;
                }

                tasks.Add(_subscriptionPackages[i].Init(data[i].productModel, data[i].productMetadata, onBuyClicked));
            }

            await Task.WhenAll(tasks);
        }

        public async Task InitImages(string backgroundImageUrl, string footerBackgroundUrl, string logosUrl)
        {
            var tasks = new List<Task>();
            if (!footerBackgroundUrl.IsNullOrWhitespace())
            {
                tasks.Add(_footerBackground.SetImage(footerBackgroundUrl));
            }

            if (!backgroundImageUrl.IsNullOrWhitespace())
            {
                tasks.Add(_backgroundContentImage.SetImage(backgroundImageUrl));
            }

            if (!logosUrl.IsNullOrWhitespace())
            {
                tasks.Add(_logosImage.SetImage(logosUrl));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        public async Task SetWallOfTextImage(string imageUrl)
        {
            _wallOfText.preserveAspect = true;
            await _wallOfText.SetImage(imageUrl);
        }
    }
}