/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A QR code with its caption and link actions: the code on the left, then a title, the
    /// link as text, and Copy link and Share.
    /// </summary>
    /// <remarks>
    /// <para>The default <c>title-text</c> is "Visitor code". An empty <c>caption</c> shows the link without its scheme
    /// (see <see cref="DisplayText"/>). The default <c>copy-text</c> is "Copy link". An empty <c>copy-text</c> or <c>share-text</c> hides that action.
    /// The default <c>qr-size</c> is 96. <c>card</c> draws the card and defaults to true.
    /// <see cref="CopyRequested"/> and <see cref="ShareRequested"/> pass the encoded text. The app copies or shares it.</para>
    /// </remarks>
    [UxmlElement]
    public partial class QRCodeCard : VisualElement
    {
        public const string RootClass = "qrCodeCard";
        public const string CardClass = "qrCodeCard--card";
        public const string CodeClass = "qrCodeCard__code";
        public const string TextsClass = "qrCodeCard__texts";
        public const string TitleClass = "qrCodeCard__title";
        public const string CaptionClass = "qrCodeCard__caption";
        public const string LinksClass = "qrCodeCard__links";
        public const string LinkClass = "qrCodeCard__link";

        private readonly QRCodeView code;
        private readonly Label title;
        private readonly Label caption;
        private readonly Label copy;
        private readonly Label share;
        private string captionText;

        public event Action<string> CopyRequested;

        public event Action<string> ShareRequested;

        public QRCodeView Code => code;

        [UxmlAttribute("text")]
        public string Text
        {
            get => code.Text;
            set
            {
                code.Text = value;
                UpdateCaption();
            }
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("caption")]
        public string Caption
        {
            get => captionText;
            set
            {
                captionText = value;
                UpdateCaption();
            }
        }

        [UxmlAttribute("copy-text")]
        public string CopyText
        {
            get => copy.text;
            set => copy.SetTextOrHide(value);
        }

        [UxmlAttribute("share-text")]
        public string ShareText
        {
            get => share.text;
            set => share.SetTextOrHide(value);
        }

        [UxmlAttribute("qr-size")]
        public float QrSize
        {
            get => code.Size;
            set => code.Size = Mathf.Max(48f, value);
        }

        [UxmlAttribute("card")]
        public bool Card
        {
            get => ClassListContains(CardClass);
            set => EnableInClassList(CardClass, value);
        }

        public QRCodeCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            code = new QRCodeView();
            code.AddToClassList(CodeClass);
            Add(code);

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass);
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            caption = UIToolkitExtensions.CreateVisualElement<Label>(texts, CaptionClass);
            var links = UIToolkitExtensions.CreateVisualElement(texts, LinksClass);
            copy = CreateLink(links, () => CopyRequested?.Invoke(code.Text));
            share = CreateLink(links, () => ShareRequested?.Invoke(code.Text));

            Card = true;
            TitleText = "Visitor code";
            CopyText = "Copy link";
            ShareText = null;
            QrSize = 96f;
        }

        public QRCodeCard(string text)
            : this()
        {
            Text = text;
        }

        /// <summary>
        /// The link as people read it. "https://example.com/go/x/" becomes "example.com/go/x".
        /// </summary>
        public static string DisplayText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var shown = text;
            var scheme = shown.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0)
            {
                shown = shown.Substring(scheme + 3);
            }

            return shown.TrimEnd('/');
        }

        private static Label CreateLink(VisualElement parent, Action action)
        {
            var link = UIToolkitExtensions.CreateVisualElement<Label>(parent, LinkClass);
            link.focusable = true;
            link.tabIndex = 0;
            link.RegisterActivate(action);
            return link;
        }


        private void UpdateCaption()
        {
            caption.text = string.IsNullOrEmpty(captionText) ? DisplayText(code.Text) : captionText;
        }
    }
}
