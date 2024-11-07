using Refit;
using System.Net;
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
            BaseAddress = new Uri("https://raw.githubusercontent.com/"),
        };

        async Task MakeRequest()
        {
            try
            {
                var api = RestService.For<IMyApi>(httpClient);
                var docs = await api.GetDocuments()!;
                Debug.Assert(docs.Length > 0);
                Debug.Assert(!string.IsNullOrEmpty(docs[0].Id));
                Debug.Assert(!string.IsNullOrEmpty(docs[0].Type));
                Debug.Assert(docs[0].IsPublic);
            }
            catch (ApiException exc)
            {
                // Occasionally we get HttpStatusCode.BadGateway
                if (exc.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(exc);
                    return;
                }
                throw;
            }
        }
    }

    public interface IMyApi
    {
        [Get("/json-iterator/test-data/refs/heads/master/large-file.json")]
        Task<Document[]> GetDocuments();
    }

    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Document[]))]
    partial class MyJsonContext : JsonSerializerContext
    {
    }

    public class Document
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("public")]
        public bool IsPublic { get; set; }
    }
    
    class Foo : Java.Lang.Object { }
}