using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneralUtils.Extensions;
using JsonDotNet.Extensions;
using NetworkFramework;
using PhoenixServices.Models;
using SubscriptionBundlePopups.Models;

namespace SubscriptionBundlePopups.Runtime.Helpers
{
    public static class SubscriptionPopupHelper
    {
        // General
        public const string BackgroundImageKey = "sub_background_png";
        public const string FooterBackgroundImageKey = "footer_background_png";
        public const string WallOfText = "sub_text_wall";
        public const string LogosImageKey = "logos";

        // Product
        public const string ProductHolderImageKey = "sub_main_image_svg";
        public const string ProductButtonImageKey = "sub_btn_image_svg";
        public const string ProductIcon = "sub_prod_icon_svg";

        // Info
        public const string ContentKey = "sub_info_content";

        public static async Task CacheAssets(PromotionModel promotion)
        {
            var tasks = new List<Task>();

            AddGeneralAssets(promotion, tasks);
            AddProductsAssets(promotion, tasks);
            AddInfoAssets(promotion, tasks);

            await Task.WhenAll(tasks);
        }

        private static void AddGeneralAssets(PromotionModel promotion, List<Task> tasks)
        {
            tasks.Add(DownloadAsset(promotion.images, BackgroundImageKey));
            tasks.Add(DownloadAsset(promotion.images, FooterBackgroundImageKey));
            tasks.Add(DownloadAsset(promotion.images, WallOfText));
        }

        private static void AddProductsAssets(PromotionModel promotion, List<Task> tasks)
        {
            var products = promotion.goods.Select(good => good.GetProduct());

            foreach (var productModel in products)
            {
                tasks.Add(DownloadAsset(productModel.images, ProductHolderImageKey));
                tasks.Add(DownloadAsset(productModel.images, ProductButtonImageKey));
                tasks.Add(DownloadAsset(productModel.images, ProductIcon));
            }
        }

        private static void AddInfoAssets(PromotionModel productModel, List<Task> tasks)
        {
            var models = new List<SubscriptionInfoCellModel>();
            foreach (var json in productModel.strings)
            {
                if (!json.Key.StartsWith(ContentKey)) continue;

                SubscriptionInfoCellModel model = null;
                try
                {
                    model = json.Value.DeserializeJson<SubscriptionInfoCellModel>();
                }
                catch(System.Exception e)
                {
                    UnityEngine.Debug.LogError($"Error when trying to parse value of `{json.Key}`\nError: {e.Message}");
                }
                if (model == null) continue;
                models.Add(model);
            }

            tasks.AddRange(models.Select(subscriptionInfoCellModel => Network.Get(subscriptionInfoCellModel.image_url)));
        }

        private static async Task DownloadAsset(IDictionary<string, string> dict, string key)
        {
            var url = dict.GetValue(key);
            if (url.IsNullOrWhitespace()) return;
            await Network.Get(url,CachePolicy.ReturnCacheDataElseLoad);
        }
    }
}