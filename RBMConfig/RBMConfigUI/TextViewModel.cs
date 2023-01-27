using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RBMConfig.RBMConfigUI
{
    public class TextViewModel : ViewModel
    {
        private string _text;

        public TextObject TextObject { get; set; }

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public TextViewModel(TextObject text)
        {
            TextObject = text;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            TextObject = TextObject;
        }
    }
}
