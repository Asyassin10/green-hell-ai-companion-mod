using System;
using System.Collections;
using UnityEngine.Networking;

namespace GreenHellAI
{
    internal sealed class BackendClient
    {
        private readonly string _url;
        private readonly float _timeout;
        internal BackendClient(string url, float timeout) { _url = url.TrimEnd('/'); _timeout = timeout; }

        internal IEnumerator Ask(string message, Snapshot snapshot, Action<string, string> completed)
        {
            string json = RequestJson(message, snapshot);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest request = new UnityWebRequest(_url + "/v1/chat", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)Math.Max(1, _timeout);
            yield return request.SendWebRequest();
            string answer = null;
            string error = request.error;
            if (String.IsNullOrEmpty(error) && request.responseCode >= 200 && request.responseCode < 300)
            {
                answer = ExtractString(request.downloadHandler.text, "answer");
                if (String.IsNullOrEmpty(answer)) error = "Backend returned no answer.";
            }
            else if (String.IsNullOrEmpty(error)) error = "Backend HTTP " + request.responseCode;
            request.Dispose();
            if (completed != null) completed(answer, error);
        }

        private static string RequestJson(string message, Snapshot sample)
        {
            string state = "{}";
            if (sample != null)
                state = "{\"health\":" + sample.Health.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"max_health\":" + sample.MaxHealth.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"energy\":" + sample.Energy.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"max_energy\":" + sample.MaxEnergy.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"stamina\":" + sample.Stamina.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"max_stamina\":" + sample.MaxStamina.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"scene\":\"" + Escape(sample.SceneName) + "\"}";
            return "{\"message\":\"" + Escape(message) + "\",\"state\":" + state + ",\"history\":[]}";
        }

        private static string Escape(string value)
        {
            if (value == null) return String.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string ExtractString(string json, string property)
        {
            string marker = "\"" + property + "\":";
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;
            start += marker.Length;
            while (start < json.Length && Char.IsWhiteSpace(json[start])) start++;
            if (start >= json.Length || json[start] != '\"') return null;
            start++;
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            bool escaped = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (escaped)
                {
                    escaped = false;
                    if (c == 'n') result.Append('\n'); else if (c == 'r') result.Append('\r');
                    else if (c == 't') result.Append('\t'); else result.Append(c);
                }
                else if (c == '\\') escaped = true;
                else if (c == '"') return result.ToString();
                else result.Append(c);
            }
            return null;
        }
    }
}
