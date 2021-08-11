using UnityEngine;

namespace SubscriptionBundlePopups.Payloads
{
    // Config ui data except images 
    public class SubscriptionUiConfigPayload
    {
        public int TryButtonHierarchyIndex { get; }
        public int VerticalLayoutTopPadding { get; }
        public float VerticalSpacing { get; }
        public float ProductHorizontalSpacing { get; }
        
        public string TermText { get; }
        public Color TryButtonColor { get; }
        public Color FooterElementsColor { get;}
        public float ScrollAnimationTime { get; }
        public int ScrollInterval { get; }


        public SubscriptionUiConfigPayload(
            int tryButtonHierarchyIndex, 
            float verticalSpacing, 
            float productHorizontalSpacing,
            string termText, 
            Color tryButtonColor, 
            Color footerElementsColor, 
            int verticalLayoutTopPadding, 
            float scrollAnimationTime, 
            int scrollInterval)
        {
            TryButtonHierarchyIndex = tryButtonHierarchyIndex;
            VerticalSpacing = verticalSpacing;
            ProductHorizontalSpacing = productHorizontalSpacing;
            TermText = termText;
            TryButtonColor = tryButtonColor;
            FooterElementsColor = footerElementsColor;
            VerticalLayoutTopPadding = verticalLayoutTopPadding;

            ScrollAnimationTime = scrollAnimationTime;
            ScrollInterval = scrollInterval;
        }
    }
}
