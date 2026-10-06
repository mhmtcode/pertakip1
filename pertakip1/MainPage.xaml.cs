using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Graphics.Platform;


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
            if (_pin.Length == 0)
                _ = EnsureCameraAsync();   // PIN yazılırken kamera ısınır
            if (_pin.Length >= MaxPinLength) return;

            

            var button = (Button)sender;
            _pin += button.Text;
            UpdatePinDisplay();

            if (_pin.Length == MaxPinLength)
                CheckPin();
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
            // PinDisplay.Text = new string('•', _pin.Length);
            for (int i = 0; i < PinDots.Children.Count; i++)
            {
                var dot = (Microsoft.Maui.Controls.Shapes.Ellipse)PinDots.Children[i];
                dot.Fill = i < _pin.Length ? Colors.MidnightBlue : Colors.Transparent;
            }
        }

        private async void CheckPin()
        {
            //bool isCorrect = _pin == "12345"; // örnek şifre kontrolü

            //await ShowResultPopupAsync(isCorrect);

            //_pin = "";
            //UpdatePinDisplay();
            var pin = _pin;
            _pin = "";
            UpdatePinDisplay();

            bool isCorrect = pin == "12345";
            string? photoPath = await TakePhotoAsync();
            try { Cam.StopCameraPreview(); } catch { }
            // TODO: SQLite kaydına photoPath ekle

//#if DEBUG
            if (photoPath != null)
            {
                DebugPhoto.Source = ImageSource.FromFile(photoPath);
                DebugPhoto.IsVisible = true;
            }
//#endif

            await ShowResultPopupAsync(isCorrect);
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
            try { Cam.StopCameraPreview(); } catch { }
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
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged; // OnDisappearing'de çıkarıyorsun, burada geri ekle
            try { await Permissions.RequestAsync<Permissions.Camera>(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("IZIN HATA: " + ex.Message); }
        }
        private async Task<string?> TakePhotoAsync()
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(2000);
                    await using var stream = await Cam.CaptureImage(cts.Token);
                    using var img = PlatformImage.FromStream(stream);
                    using var small = img.Downsize(480, true);

                    var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{Guid.NewGuid()}.jpg");
                    await File.WriteAllBytesAsync(path, small.AsBytes(ImageFormat.Jpeg, 0.7f));
                    return path;
                }
                catch { await Task.Delay(700); }
            }
            return null; // foto olmasa da giriş engellenmesin
        }

        private async Task EnsureCameraAsync()
        {
            try
            {
                for (int i = 0; i < 20 && Cam.Handler == null; i++)
                    await Task.Delay(100);
                if (Cam.Handler == null) return;

                var cams = await Cam.GetAvailableCameras(CancellationToken.None);
                if (cams.Count == 0) return;

                Cam.SelectedCamera = cams.FirstOrDefault(c => c.Position == CameraPosition.Front) ?? cams.First();
                await Cam.StartCameraPreview(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("KAMERA HATA: " + ex.Message);
            }
        }
    }
}
