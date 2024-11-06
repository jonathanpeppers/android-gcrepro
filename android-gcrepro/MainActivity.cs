using Android.Util;
using Refit;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xamarin.Android.Net;
using Debug = System.Diagnostics.Debug;

namespace android_gcrepro
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var button = RequireViewById<Button>(Resource.Id.startButton);
            button.Click += OnClick;
        }

        async void OnClick(object? sender, EventArgs e)
        {
            var button = sender as Button;
            ArgumentNullException.ThrowIfNull(button);

            var mainThread = TaskScheduler.Current;
            button.Enabled = false;
            try
            {
                while (true)
                {
                    GC.Collect();

                    // Make 1,000 JLOs
                    var jlos = Enumerable.Range(0, 1000)
                        .Select(_ => new Foo())
                        .ToList();

                    // Make a request
                    var request1 = MakeRequest();
                    var request2 = MakeRequest();
                    var request3 = MakeRequest();

                    // Delay a small amount
                    await Task.Delay(100);
                    await Task.WhenAll(request1, request2, request3);

                    // End of loop
                    GC.KeepAlive(jlos);
                    GC.Collect();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);

                new AlertDialog.Builder(this)
                    .SetMessage(exc.Message)!
                    .SetPositiveButton("OK", delegate { })!
                    .Show();
            }
            finally
            {
                button.Enabled = true;
            }
        }

        readonly HttpClient httpClient = new(new AndroidMessageHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                // NOTE: This is insecure!!!
                ServerCertificateCustomValidationCallback = (request, certificate, chain, sslPolicyErrors) => true
            })
        {
            BaseAddress = new Uri("https://httpbin.org"),
        };

        async Task MakeRequest()
        {
            try
            {
                var api = RestService.For<IGzipApi>(httpClient);
                var doc = await api.GetGzip();
                Debug.Assert(doc?.IsGzipped == true);
                Debug.Assert(doc?.Headers?.Count > 0);
            }
            catch (ApiException exc)
            {
                if (exc.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(exc);
                    return;
                }
                throw;
            }
        }
    }

    public interface IGzipApi
    {
        [Get("/gzip")]
        Task<Document> GetGzip();
    }

    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Document))]
    partial class MyJsonContext : JsonSerializerContext
    {
    }

    public class Document
    {
        [JsonPropertyName("gzipped")]
        public bool IsGzipped { get; set; } = false;

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("origin")]
        public string? Origin { get; set; }
    }
    
    class Foo : Java.Lang.Object { }
}