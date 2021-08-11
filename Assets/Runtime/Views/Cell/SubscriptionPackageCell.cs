using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeneralUtils.Extensions;
using InceptionPlugins.Pluggables.Purchase;
using JsonDotNet.Extensions;
using NetworkUIHelpers.Images;
using NetworkUIHelpers.SVG;
using PhoenixServices.Models;
using SubscriptionBundlePopups.Runtime.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace SubscriptionBundlePopups.Views.Cell
{
    public class SubscriptionPackageCell : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _secondPriceText;
        [SerializeField] private SVGImage _backgroundImage;
        [SerializeField] private SVGImage _icon;
        [SerializeField] private Image _logos;

        [Space] [Header("Button")] [SerializeField]
        private Button _button;

        [SerializeField] private SVGImage _buttonSvgImage;

        private const string Price = "price";

        // strings
        private const string ProductTitle = "sub_prod_title";
        private const string PriceLabelTop = "price_label_top";
        private const string PriceLabelBottom = "price_label_bottom";
        private const string ButtonPositionKey = "button_pos";
        private const string ProductIconPos = "prod_icon_pos";
        private const string LogosImageKey = "logos_image_key";


        public async Task Init(ProductModel model, IProductMetadata metadata, Action<ProductModel> onBuyClicked)
        {
            await SetContent(model, onBuyClicked);

            SetLabels(model, metadata);
        }

        private async Task SetContent(ProductModel model, Action<ProductModel> onBuyClick)
        {
            var svgConfig = SVGImportConfig.GetDefault();
            svgConfig.PreserveViewport = true;

            await SetBackground(svgConfig, model);
            _button.onClick.AddListener(() => onBuyClick(model));

            await SetButton(svgConfig, model);

            await SetProductIcon(model);

            await SetLogosImage(model);
        }

        private async Task SetBackground(SVGImportConfig config, ProductModel model)
        {
            var holderImageUrl = model.images.GetValue(SubscriptionPopupHelper.ProductHolderImageKey);
            if (holderImageUrl.IsNullOrWhitespace()) return;

            if (_backgroundImage == null) return;

            await _backgroundImage.SetImage(holderImageUrl, config);
            var rect = _backgroundImage.sprite.rect;
            _backgroundImage.rectTransform.sizeDelta = rect.size;
        }

        private async Task SetButton(SVGImportConfig config, ProductModel model)
        {
            var buttonUrl = model.images.GetValue(SubscriptionPopupHelper.ProductButtonImageKey);

            if (buttonUrl.IsNullOrWhitespace()) return;

            await _buttonSvgImage.SetImage(buttonUrl, config);
            var rect = _buttonSvgImage.sprite.rect;
            _buttonSvgImage.rectTransform.sizeDelta = rect.size;

            var buttonPositionJson = model.strings.GetValue(ButtonPositionKey);

            if (buttonPositionJson.IsNullOrEmpty()) return;

            try
            {
                _backgroundImage.rectTransform.anchoredPosition = buttonPositionJson.DeserializeJson<Vector2>();
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occured while trying to parse button position: {e.Message}");
            }
        }

        private async Task SetProductIcon(ProductModel model)
        {
            var iconUrl = model.images.GetValue(SubscriptionPopupHelper.ProductIcon);

            if (iconUrl.IsNullOrWhitespace()) return;

            await _icon.SetImage(iconUrl);
            var rect = _icon.sprite.rect;
            _icon.rectTransform.sizeDelta = rect.size;

            var productIconPosJson = model.strings.GetValue(ProductIconPos);

            if (productIconPosJson.IsNullOrEmpty()) return;

            try
            {
                _icon.rectTransform.anchoredPosition = productIconPosJson.DeserializeJson<Vector2>();
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occured while trying to parse product icon position {e.Message}");
            }

            _icon.gameObject.SetActive(true);
        }

        private void SetLabels(ProductModel model, IProductMetadata metadata)
        {
            var title = GetPriceLabelByKeyAndMetadata(ProductTitle, model);
            _titleText.text = GetPriceLabel(title, metadata);

            var topText = GetPriceLabelByKeyAndMetadata(PriceLabelTop, model);
            var bottomText = GetPriceLabelByKeyAndMetadata(PriceLabelBottom, model);

            _priceText.text = GetPriceLabel(topText, metadata);
            _priceText.enabled = !_priceText.text.IsNullOrWhitespace();

            if (_secondPriceText == null) return;

            _secondPriceText.text = GetPriceLabel(bottomText, metadata);
            _secondPriceText.gameObject.SetActive(!_secondPriceText.text.IsNullOrWhitespace());
        }

        private async Task SetLogosImage(ProductModel model)
        {
            if (_logos == null)
            {
                return;
            }

            var logosUrl = model.images.GetValue(LogosImageKey);

            if (logosUrl.IsNullOrWhitespace())
            {
                return;
            }

            await _logos.SetImage(logosUrl);
            _logos.SetNativeSize();
        }


        private string GetPriceLabelByKeyAndMetadata(string key, ProductModel model) =>
            model.strings.GetValue(key, string.Empty);

        private string GetPriceLabel(string label, IProductMetadata metadata)
        {
            // if no label, return empty
            if (label.IsNullOrEmpty()) return string.Empty;

            var dynamicLabels = Regex.Matches(label, @"(?<=\{)[^}]*(?=\})");

            foreach (Match dynamicLabel in dynamicLabels)
            {
                if (!dynamicLabel.Value.Contains(Price)) return metadata.LocalizedPriceString;

                // if no math needed
                if (dynamicLabel.Length == Price.Length)
                {
                    label = label.Replace("{price}", metadata.LocalizedPriceString);
                }
                else
                {
                    // figure the math
                    var newPrice = metadata.LocalizedPrice;
                    var symbol = dynamicLabel.Value[Price.Length];
                    var numberString = dynamicLabel.Value.Remove(0, Price.Length + 1);
                    var number = int.Parse(numberString);
                    switch (symbol)
                    {
                        case '+':
                            newPrice = metadata.LocalizedPrice + number;
                            break;
                        case '-':
                            newPrice = metadata.LocalizedPrice - number;
                            break;
                        case '*':
                            newPrice = metadata.LocalizedPrice * number;
                            break;
                        case '/':
                            newPrice = metadata.LocalizedPrice / number;
                            break;
                    }

                    // construct new label
                    var start = label.IndexOf('{');
                    var end = label.IndexOf('}', start);
                    var count = end - start;
                    label = label.Remove(start, count + 1);
                    label = label.Insert(start, newPrice.FormatCurrency(metadata.IsoCurrencyCode));
                }
            }

            return label;
        }
    }
}