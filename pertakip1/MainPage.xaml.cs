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

                if (_pin.Length >= MaxPinLength) return;



            var button = (Button)sender;
            _pin += button.Text;
            UpdatePinDisplay();

            if (_pin.Length == MaxPinLength)
            {
                _ = EnsureCameraAsync();   // PIN yazılırken kamera ısınır
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
            for (int i = 0; i < PinDots.Children.Count; i++)
            {
                var dot = (Microsoft.Maui.Controls.Shapes.Ellipse)PinDots.Children[i];
                dot.Fill = i < _pin.Length ? Colors.MidnightBlue : Colors.Transparent;
            }
        }

        private async void CheckPin()
        {
            var pin = _pin;
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

            if (!isCorrect)
            {
                // 1. Sağa sola sallanma animasyonunu çalıştır
                await ShakePinDotsAsync();

            }
            else
            {
                // Doğru şifre girildiğinde yapılacak işlemler (Örn: Sayfa geçişi)
                await ShowResultPopupAsync(isCorrect);
            }


            _pin = "";
            UpdatePinDisplay();
        }
        private async Task ShowResultPopupAsync(bool isCorrect)
        {
            // Renk ve içerik ayarla
            ResultFrame.BackgroundColor = isCorrect ? Colors.MediumSeaGreen : Colors.IndianRed;
            ResultText.Text = isCorrect ? "Passwort richtig" : "Passwort falsch";
            ResultStatus.Text = isCorrect ? "Giris Yaptiniz" : "Cikis Yaptiniz";
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

                internetConnectionLabel.Text = isInternetConnected ? "Connection" : "Keine Connection";
                internetConnectionLabel.TextColor = isInternetConnected ? Colors.Green : Colors.Red;
                connetionStatus.Fill = isInternetConnected ? Colors.Green : Colors.Red;
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

            if (width <= 0 || Height <= 0) return;

            bool compact = Height < 480;                 // 7" tablet
            double title = compact ? 24 : 38;
            double sub = compact ? 14 : 20;


            double scale = Height switch
            {
                < 700 => 0.55,      // 7"
                < 900 => 1.20,     // 10"
                _ => 1.3       // büyük tablet
            };


            // Başlık + alt başlık + halkalar + StackLayout boşlukları (3 x 12)
            double fixedH = title * 1.3 + sub * 1.3 + 38 + 3 * 12;

            //// 4 satır + 3 boşluk (10) + güvenlik payı
            double keyH = (Height - fixedH - 3 * 10 - 16) / 4;
            keyH = Math.Clamp(keyH * scale, 40, 70);
            double keyW = Math.Clamp(keyH * 1.5, 56, 110);
           

            var res = Application.Current!.Resources;
            res["KeyHeight"] = keyH;
            res["KeyWidth"] = keyW;
            res["KeyFontSize"] = keyH * 0.38;
            res["TitleFontSize"] = title;
            res["SubTitleFontSize"] = sub;

            var d = DeviceDisplay.Current.MainDisplayInfo;
            bool landscape = d.Orientation == DisplayOrientation.Landscape;
            RightPanel.IsVisible = landscape;
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

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            // Ayarlar sayfasına geçiş yap
            //Shell.Current.GoToAsync("//SettingsPage");
        }


        private async Task ShakePinDotsAsync()
        {
            uint duration = 40; // Her bir sağa/sola kayma hareketi süresi (milisaniye)

            // Dairelerin tutulduğu HorizontalStackLayout'u sağa-sola kaydırma adımları
            await PinDots.TranslateTo(-15, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(15, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(-12, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(12, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(-8, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(8, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(-4, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(4, 0, duration, Easing.Linear);
            await PinDots.TranslateTo(0, 0, duration, Easing.Linear); // Başlangıç konumuna geri getir
        }

        

    }
}
