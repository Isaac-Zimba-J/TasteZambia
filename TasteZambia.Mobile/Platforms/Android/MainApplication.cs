using Android.App;
using Android.Runtime;

namespace TasteZambia.Mobile;

#if DEBUG
// Debug builds talk plain HTTP to the developer's machine. Release builds keep
// Android's default - cleartext blocked - so this cannot ship by accident.
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
