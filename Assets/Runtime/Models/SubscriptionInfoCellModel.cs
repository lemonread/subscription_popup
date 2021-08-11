using System;

namespace SubscriptionBundlePopups.Models
{
    [Serializable]
    public class SubscriptionInfoCellModel
    {
        public enum AnchorType
        {
            Bg,
            Middle,
            Left,
            Right
        }

        public AnchorType anchor_type;
        public bool svg;
        public string image_url;
        public string button_url;
        public float scale_factor;
    }
}
