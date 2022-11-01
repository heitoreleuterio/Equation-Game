using Android.App;
using Android.Runtime;
using Microsoft.Extensions.Configuration;

namespace Jogo_Algebra;

[Application]
public class MainApplication : MauiApplication {
    public MainApplication(IntPtr handle,JniHandleOwnership ownership)
        : base(handle,ownership) {
    }

    protected override MauiApp CreateMauiApp() {
        MauiApp app = MauiProgram.CreateMauiApp();
        return app;
    }
}
