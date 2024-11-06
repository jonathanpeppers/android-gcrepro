# android-gcrepro

A repro for an Android "GC bridge" issue

`https://httpbin.org/gzip` looks something like this when uncompressed:

```json
{
  "gzipped": true, 
  "headers": {
    "Accept-Encoding": "gzip", 
    "Host": "httpbin.org", 
    "User-Agent": "Dalvik/2.1.0 (Linux; U; Android 14; Android SDK built for x86_64 Build/UE1A.230829.036.A1)", 
    "X-Amzn-Trace-Id": "Root=1-672bbe71-2517565400f0bdd66faca864"
  }, 
  "method": "GET", 
  "origin": "75.49.197.187"
}
```
