using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetworkFramework;
using UITools.Events;
using GeneralUtils.Extensions;
using JsonDotNet.Extensions;
using NetworkUIHelpers.Images;
using NetworkUIHelpers.SVG;
using SubscriptionBundlePopups.Models;
using SubscriptionBundlePopups.Payloads;
using SubscriptionBundlePopups.Runtime.Helpers;
using UITools.Interfaces;
using UnibusEvent;
using UnityEngine;
using UnityEngine.UI;

namespace SubscriptionBundlePopups.Views.Cell
{
    public class SubscriptionInfoCell : MonoBehaviour, IDataBinder
    {
        [SerializeField] private RectTransform _leftAnchor;
        [SerializeField] private RectTransform _rightAnchor;
        [SerializeField] private RectTransform _middleAnchor;
        [SerializeField] private RectTransform _bgAnchor;
        [SerializeField] private Material _vectorMaterial;
        [SerializeField] private VerticalLayoutGroup _midAnchorLayout;
        
        private const string MidPaddingTopKey = "sub_info_midPadding_top";
        private const string MidSpacingKey = "sub_info_spacing";

        public GameObject GameObject => gameObject;
        
        public void BindData(DataBindPayload payload)
        {
            throw new NotImplementedException();
        }

        public async Task BindDataAsync(DataBindPayload payload)
        {
            SetLayoutParams(payload);

            var models = GetModels(payload);

            var urls = models.Select(model => model.image_url);

            var downloadQueue = new DownloadQueue().Enqueue(urls);
            await downloadQueue.Load();

            foreach (var model in models)
            {
                await BuildContent(model);
            }
        }

        private static IEnumerable<SubscriptionInfoCellModel> GetModels(DataBindPayload payload)
        {
            var models = payload.Data.Where(kvp => kvp.Key.StartsWith(SubscriptionPopupHelper.ContentKey));
            
            foreach (var kvp in models)
            {
                if (kvp.Value == null)
                {
                    Debug.Log($"Cell model value: {kvp.Key} is null!");
                    continue;
                }
            
                var cellModel = (kvp.Value as string).DeserializeJson<SubscriptionInfoCellModel>();
                yield return cellModel;
            }
        }

        private void SetLayoutParams(DataBindPayload payload)
        {
            _midAnchorLayout.spacing = (payload.Data.GetValue(MidSpacingKey, _midAnchorLayout.spacing.ToString()) as string).ToFloatSafe(_midAnchorLayout.spacing);

            _midAnchorLayout.padding.top =
                (payload.Data.GetValue(MidPaddingTopKey, _midAnchorLayout.padding.top.ToString()) as string).ToIntSafe(_midAnchorLayout.padding.top);
        }
        
        private async Task BuildContent(SubscriptionInfoCellModel model)
        {
            if (model.image_url.IsNullOrWhitespace())
            {
                Debug.LogError("Missing image url!");
                return;
            }
            
            var rectTransform =
                new GameObject($"Slider_content_{model.anchor_type}", typeof(RectTransform)).transform as RectTransform;
            
            rectTransform.gameObject.SetActive(false);
            
            rectTransform.SetParent(GetAnchorAccordingConfig(model.anchor_type), false);
            
            var sprite = model.svg ? await SetSvg(rectTransform, model.image_url) : await SetImage(rectTransform, model.image_url);

            if (!model.button_url.IsNullOrWhitespace())
            {
                SetButton(rectTransform.gameObject,() => OnClick(model.button_url)); 
            }
            
            var scaleFactor = model.scale_factor <= 0 ? 1 : model.scale_factor;
            rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) / scaleFactor;
            rectTransform.pivot = GetPivotAccordingAnchorType(model.anchor_type);
            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.gameObject.SetActive(true);
        }

        private async Task<Sprite> SetSvg(RectTransform rectTransform, string url)
        {
            var svgComponent = rectTransform.gameObject.AddComponent<SVGImage>();
            svgComponent.preserveAspect = true;
            svgComponent.material = _vectorMaterial;
            var svgImportConfig = SVGImportConfig.GetDefault();
            svgImportConfig.PreserveViewport = true;
            await svgComponent.SetImage(url, svgImportConfig);
            
            return svgComponent.sprite;
        }

        private async Task<Sprite> SetImage(RectTransform rectTransform, string url)
        {
            var imageComponent = rectTransform.gameObject.AddComponent<Image>();
            imageComponent.preserveAspect = true;
            await imageComponent.SetImage(url);

            return imageComponent.sprite;
        }
        
        private void SetButton(GameObject go, Action onClick)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Graphic>();
            button.onClick.AddListener(onClick.Invoke); 
        }
        
        private void OnClick(string url)
        {
            Debug.Log($"Subscription Clicked. Link: {url}");
            Unibus.Dispatch(new SubscriptionInfoClickedPayload(url));
        }

        private RectTransform GetAnchorAccordingConfig(SubscriptionInfoCellModel.AnchorType param)
        {
            switch (param)
            {
                case SubscriptionInfoCellModel.AnchorType.Left: return _leftAnchor;
                case SubscriptionInfoCellModel.AnchorType.Middle: return _middleAnchor;
                case SubscriptionInfoCellModel.AnchorType.Right: return _rightAnchor;
                default: return _bgAnchor;
            }
        }

        private Vector2 GetPivotAccordingAnchorType(SubscriptionInfoCellModel.AnchorType type)
        {
            switch (type)
            {
                case SubscriptionInfoCellModel.AnchorType.Left: return new Vector2(0, 1);
                case SubscriptionInfoCellModel.AnchorType.Right: return new Vector2(1, 0);
                default: return Vector2.one * 0.5f;
            }
        }
    }
}