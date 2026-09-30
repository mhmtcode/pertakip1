namespace pertakip1
{
    public partial class MainPage : ContentPage
    {
        private string _pin = "";
        private const int MaxPinLength = 5; // istediğin uzunluğa göre değiştir

        private bool isInternetConnected;
        public MainPage()
        {
            InitializeComponent();
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
            UpdateConnectionStatus(Connectivity.Current.NetworkAccess);
        }
        private void OnNumberClicked(object sender, EventArgs e)
        {
            if (_pin.Length >= MaxPinLength) return;

            var button = (Button)sender;
            _pin += button.Text;
            UpdatePinDisplay();

            if (_pin.Length == MaxPinLength)
            {
                // Şifre tamamlandı, kontrol et
                CheckPin();
            }
        }

        private void OnBackspaceClicked(object sender, EventArgs e)
        {
            if (_pin.Length > 0)
            {
                _pin = _pin.Substring(0, _pin.Length - 1);
                UpdatePinDisplay();
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            _pin = "";
            UpdatePinDisplay();
        }

        private void UpdatePinDisplay()
        {
            // Girilen rakamları gizlemek için • kullanıyoruz
            PinDisplay.Text = new string('•', _pin.Length);
        }

        private async void CheckPin()
        {
            bool isCorrect = _pin == "12345"; // örnek şifre kontrolü

            await ShowResultPopupAsync(isCorrect);

            _pin = "";
            UpdatePinDisplay();
        }
        private async Task ShowResultPopupAsync(bool isCorrect)
        {
            // Renk ve içerik ayarla
            ResultFrame.BackgroundColor = isCorrect ? Colors.MediumSeaGreen : Colors.IndianRed;
            ResultText.Text = isCorrect ? "Passwort richtig" : "Passwort falsch";
            ResultStatus.Text= isCorrect ? "Giris Yaptiniz" : "Cikis Yaptiniz";
            // Görünür yap, hafif animasyonla büyüterek göster
            ResultOverlay.Opacity = 0;
            ResultOverlay.IsVisible = true;
            ResultFrame.Scale = 0.7;

            await Task.WhenAll(
                ResultOverlay.FadeTo(1, 200),
                ResultFrame.ScaleTo(1, 200, Easing.SpringOut)
            );

            // Belirli süre ekranda kalsın
            await Task.Delay(1200);

            // Kaybolurken de animasyonlu kapansın
            await Task.WhenAll(
                ResultOverlay.FadeTo(0, 200),
                ResultFrame.ScaleTo(0.7, 200, Easing.SpringIn)
            );

            ResultOverlay.IsVisible = false;

            // Başarılıysa sonraki sayfaya geç
            if (isCorrect)
            {
                // await Shell.Current.GoToAsync("//NextPage");
            }
        }


        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            UpdateConnectionStatus(e.NetworkAccess);
        }

        private void UpdateConnectionStatus(NetworkAccess access)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                isInternetConnected = access == NetworkAccess.Internet;

                internetControlLabel.Text = isInternetConnected ? "Connection" : "Keine Connection";
                internetControlLabel.TextColor = isInternetConnected ? Colors.Green : Colors.Red;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width <= 0 || height <= 0) return;

            double baseSize = Math.Min(width, height);
            double keypadSize = baseSize * 0.45; // ekranın %55'i kadar keypad

            KeypadContainer.WidthRequest = keypadSize;
            KeypadContainer.HeightRequest = keypadSize;
        }
    }
}
