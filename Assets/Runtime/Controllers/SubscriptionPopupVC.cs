using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asyncoroutine;
using GeneralUtils.Extensions;
using NaughtyAttributes;
using PhoenixServices.Models;
using SubscriptionBundlePopups.Payloads;
using SubscriptionBundlePopups.Runtime.Helpers;
using SubscriptionBundlePopups.Views;
using UITools.Helpers.AspectRatioHelpers;
using UnityEngine;
using UnityEngine.UI;
using InceptionPlugins.Pluggables.Purchase;
using InceptionPlugins.Injection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SubscriptionBundlePopups.Controllers
{
    public class SubscriptionPopupVC : MonoBehaviour
    { 
        [SerializeField] private SVGImage _arrowUp;
        [SerializeField] private SVGImage _arrowDown;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private Button _tryButton;

        [SerializeField] private Button _privacyButton;
        [SerializeField] private Button _detailsButton;
        [SerializeField] private Button _restoreButton;
        [SerializeField] private Button _tosButton;
        [SerializeField] private Button _exitButton;

        [SerializeField] private ScrollRect _scroller;

        [Space] [SerializeField] private SubscriptionPopupView _popupView;

        [SerializeField] private float _autoScrollTime;
        [SerializeField] private float _autoScrollAmount;

        private PromotionModel _promotion;
        private bool _autoScrollGoUp;

        private const string LogPrefix = "SubscriptionPopup:";

        private const string AutoScrollIntervalKey = "scroll_interval";
        private const string ScrollAnimationTimeKey = "scroll_animation";
        private const string VerticalLayoutTopPaddingKey = "vertical_padding";
        private const string TryButtonHierarchyKey = "try_button_index";
        private const string TryButtonColorKey = "try_button_color";
        private const string ViewVerticalSpacingKey = "popup_vertical_spacing";

        private const string FreeBookLink = "sub_free_link";
        private const string FooterColorKey = "footer_color";
        private const string TermsKey = "terms";
        private const string ProductsSpacingKey = "productsSpacing";

        private const int TryButtonHierarchyIndex = 1;
        private const int DefaultAutoScrollInterval = 4;
        private const float DefaultScrollAnimationTime = 0.3f;

        public event Action<ProductModel> OnSelectProduct;
        public event Action HaveAnAccountClicked;
        public event Action TosClicked;
        public event Action PrivacyClicked;
        public event Action RestoreClicked;
        public event Action ExitClicked;
        public event Func<Task> TryBookClicked;

        public Task<ProductModel> TaskResults => CompletionSource.Task;

        public ProductModel Results { get; private set; }

        private TaskCompletionSource<ProductModel> CompletionSource { get; } =
            new TaskCompletionSource<ProductModel>();

        private void OnDestroy()
        {
            CompletionSource?.TrySetResult(Results);

            if (_tryButton != null)
            {
                _tryButton.onClick.RemoveAllListeners();
            }
        }

        public async Task Init(PromotionModel promotion)
        {
            var promoId = promotion?.id ?? "<NULL>";
            Debug.Log($"{LogPrefix} Init (promotion Id: {promoId})");

            if (_tryButton != null)
            {
                _tryButton.onClick.RemoveAllListeners();
                _tryButton.onClick.AddListener(OnTryBookClick);
            }
            
            _promotion = promotion;
            _popupView.Root.SetActive(false);

            _arrowDown.enabled = true;
            _arrowUp.enabled = false;

            _popupView.SetUIElementsData(CreateConfig(promotion));

            AddActionToButton(_privacyButton, OnPrivacyClick);
            AddActionToButton(_detailsButton, OnDetailsClicked);
            AddActionToButton(_restoreButton, OnRestoreClick);
            AddActionToButton(_tosButton, OnToSClick);
            AddActionToButton(_exitButton, OnExitClicked);

            if (_scroller != null)
            {
                _scroller.onValueChanged.RemoveAllListeners();
                _scroll.onValueChanged.AddListener(OnScrollUpdate);                
            }
            
            // get store ids for all products (including refs)
            var storeIds = new List<string>();
            foreach (var good in promotion.goods)
            {
                var product = good.GetProduct();
                if (product == null) continue;
                storeIds.Add(product.store_id);
                if (!product.full_price_reference.IsNullOrEmpty())
                {
                    storeIds.Add(product.full_price_reference);
                }
            }

            var storeHandler = PluginDiContainer.LoadDefaultAsset<IStoreHandlerPlugin>();
            var productMetadatas = await storeHandler.GetProductsData(storeIds);

            // TODO: this is a patch to handle if IAP didnt set up at all. This logic needs better rehaul
            // Other Issues:
            // * the way storeIds+price refs are used in same data structure leads to non-deterministic size)
            // * The loop below could break if some full price refs are missing, or if productMetas received dont match length of "storeIds"
            if (productMetadatas.Length == 0)
            {
                Debug.LogError("Could not show subscription popup, IAP did not find any products!");
                CompletionSource.SetResult(null);
                return;
            }

            // handle model <> metadata
            var purchaseObjs = new List<(ProductModel, IProductMetadata)>();

            for (var i = 0; i < promotion.goods.Length; i++)
            {
                var product = promotion.goods[i].GetProduct();
                var priceMeta = productMetadatas[i];

                // if we have ref prices
                if (productMetadatas.Length == promotion.goods.Length * 2)
                {
                    priceMeta = productMetadatas[i * 2];
                    purchaseObjs.Add((product, priceMeta));
                    continue;
                }

                purchaseObjs.Add((product, priceMeta));
            }

            var tasks = new List<Task>();

            var subscriptionOptionTask = _popupView.InitSubscriptionOptions(purchaseObjs, model =>
            {
                OnSelectProduct?.Invoke(model);
                Results = model;
                CompletionSource.SetResult(model);
            });

            tasks.Add(subscriptionOptionTask);

            var backgroundUrl = promotion.images.GetValue(SubscriptionPopupHelper.BackgroundImageKey);
            var footerBackgroundUrl = promotion.images.GetValue(SubscriptionPopupHelper.FooterBackgroundImageKey);
            var logosUrl = promotion.images.GetValue(SubscriptionPopupHelper.LogosImageKey);
            var initImageTask = _popupView.InitImages(backgroundUrl, footerBackgroundUrl, logosUrl);

            tasks.Add(initImageTask);

            // Log flow before & After downloads cause this has gotten stuck many times when IAP issues occur
            Debug.Log($"{LogPrefix} Init: awaiting downloads");
            await Task.WhenAll(tasks);
            Debug.Log($"{LogPrefix} Init: Downloaded!");

            if (_tryButton != null)
            {
                _tryButton.gameObject.SetActive(_promotion.strings.GetValue(FreeBookLink) != null);    
            }

            // If we not do it we'll see the popup uninitialized for 1 frame.
            await AwaitHelpers.WaitFrames(1);
        }

        private void OnExitClicked() => ExitClicked?.Invoke();

        private static SubscriptionUiConfigPayload CreateConfig(PromotionModel promotion)
        {
            var termText = promotion.strings.GetValue(TermsKey, "");

            var productsSpacing = promotion.strings.GetValue(ProductsSpacingKey, "0").ToFloatSafe();
            var verticalSpacing = promotion.strings.GetValue(ViewVerticalSpacingKey, "0").ToFloatSafe();

            var tryButtonHierarchyIndexStr =
                promotion.strings.GetValue(TryButtonHierarchyKey, TryButtonHierarchyIndex.ToString())
                    .ToIntSafe(TryButtonHierarchyIndex);

            var verticalLayoutTopPadding = promotion.strings.GetValue(VerticalLayoutTopPaddingKey, "0").ToIntSafe();

            var footerColor = promotion.strings.GetValue(FooterColorKey, "#FFFFFF").ToColorFromHtmlSafe();

            var tryButtonColor = promotion.strings.GetValue(TryButtonColorKey, "#FFFFFF").ToColorFromHtmlSafe();

            var scrollAnimationTime =
                promotion.strings.GetValue(ScrollAnimationTimeKey, DefaultScrollAnimationTime.ToString())
                    .ToFloatSafe(DefaultScrollAnimationTime);

            var autoScrollInterval = promotion.strings
                .GetValue(AutoScrollIntervalKey, DefaultAutoScrollInterval.ToString())
                .ToIntSafe(DefaultAutoScrollInterval);

            return new SubscriptionUiConfigPayload(tryButtonHierarchyIndexStr, verticalSpacing, productsSpacing,
                termText, tryButtonColor, footerColor, verticalLayoutTopPadding, scrollAnimationTime,
                autoScrollInterval);
        }

        private void AddActionToButton(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action.Invoke);
        }

        public async void OnTryBookClick()
        {
            if (TryBookClicked == null) return;
            _tryButton.interactable = false;
            await TryBookClicked.Invoke();
            _tryButton.interactable = true;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            FitRatio();

            _popupView.Root.SetActive(true);

            _ = _popupView.InitPageView(_promotion.strings, _promotion.images);

            var wallOfTextUrl = _promotion.images.GetValue(SubscriptionPopupHelper.WallOfText);

            _ = _popupView.SetWallOfTextImage(wallOfTextUrl);
        }

        [Button("Fit popup to ratio")]
        private void FitPopupToRatio()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            _popupView.FitRatio(canvas);
        }

        private void FitRatio()
        {
            FitPopupToRatio();
            var ratioFixers = GetComponentsInChildren<AspectRatioFixerBase>(true);
            ratioFixers.ForEach(fixer=> fixer.Refresh());
        }


        private void OnHaveAnAccountClicked() => HaveAnAccountClicked?.Invoke();

        private void OnToSClick() => TosClicked?.Invoke();

        private void OnPrivacyClick() => PrivacyClicked?.Invoke();

        private void OnRestoreClick() => RestoreClicked?.Invoke();

        private void OnDetailsClicked()
        {
            if (_autoScrollGoUp)
            {
                StartCoroutine(AutoScroll(_scroll.verticalNormalizedPosition + _autoScrollAmount));
            }
            else if (!_autoScrollGoUp)
            {
                StartCoroutine(AutoScroll(_scroll.verticalNormalizedPosition - _autoScrollAmount));
            }
        }

        private void OnScrollUpdate(Vector2 value)
        {
            if (value.y < 0.2)
            {
                _arrowUp.enabled = true;
                _arrowDown.enabled = false;
                _autoScrollGoUp = true;
            }
            else if (value.y > 0.8)
            {
                _arrowDown.enabled = true;
                _arrowUp.enabled = false;
                _autoScrollGoUp = false;
            }
        }
        
        private IEnumerator AutoScroll(float targetPos)
        {
            var elapsedTime = 0f;
            var startingPos = _scroll.verticalNormalizedPosition;
            while (elapsedTime < _autoScrollTime)
            {
                _scroll.verticalNormalizedPosition =
                    Mathf.Lerp(startingPos, targetPos, (elapsedTime / _autoScrollTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

#if UNITY_EDITOR

        private float _currentAspectRatio;

        // For work on popup in editor.
        [Button("Auto fix ratio")]
        private void AutoFixRatio()
        {
            EditorApplication.update -= FixAspectEditor;
            EditorApplication.update += FixAspectEditor;
        }

        private void FixAspectEditor()
        {
            if (this == null)
            {
                EditorApplication.update -= FixAspectEditor;
                return;
            }
            
            if (EditorApplication.isPlaying) return;
            var aspectRatio = Screen.width / Screen.height;

            if (Mathf.Approximately(aspectRatio, _currentAspectRatio)) return;
            _currentAspectRatio = aspectRatio;
            FitPopupToRatio();
        }
#endif
    }
}