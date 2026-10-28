using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HttpUtils
{

    public delegate void OnHttpCompleted(string err, object body);

    // http://bycwedu.com:6080/test?uname=blake&upwd=123456
    public static void Get(string url, string param, OnHttpCompleted OnCompleted)
    {
        string urlPath = url;
        if (param != null) {
            urlPath = url + "?" + param;
        }

        UnityWebRequest wq = UnityWebRequest.Get(urlPath);
        wq.SendWebRequest().completed += (AsyncOperation opt) => {
            if (wq.error != null) {
                if (OnCompleted != null) {
                    OnCompleted(wq.error, null);
                }
            }
            else {
                if (OnCompleted != null) {
                    if (wq.downloadHandler.text != null) {
                        OnCompleted(null, wq.downloadHandler.text);
                    }
                    else {
                        OnCompleted(null, wq.downloadHandler.data);
                    }
                }
            }
            wq.Dispose();
        };
    }

    public static void Post(string url, string param, string jsonBody, OnHttpCompleted OnCompleted) {
        string urlPath = url;
        if (param != null) {
            urlPath = url + "?" + param;
        }

        UnityWebRequest wq = UnityWebRequest.Post(urlPath, jsonBody, "application/json");
        wq.SendWebRequest().completed += (AsyncOperation opt) => {
            if (wq.error != null) {
                if (OnCompleted != null) {
                    OnCompleted(wq.error, null);
                }
            }
            else {
                if (OnCompleted != null) {
                    if (wq.downloadHandler.text != null) {
                        OnCompleted(null, wq.downloadHandler.text);
                    }
                    else
                    {
                        OnCompleted(null, wq.downloadHandler.data);
                    }
                }
            }
            wq.Dispose();
        };
    }
}
