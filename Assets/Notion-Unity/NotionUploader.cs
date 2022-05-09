using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;

namespace NotionUnity
{
    /// <summary>
    /// Notion database uploader
    /// </summary>
    public static class NotionUploader
    {
        /// <summary>
        /// Update a notion table 
        /// </summary>
        /// <param name="databaseID"></param>
        public static async Task UpdateTableAsync(string databaseID, string method, string json)
        {
            string route = $"{Notion.API_URL}/databases/{databaseID.Replace("-", "")}";

            Debug.Log("Updating notion database [" + databaseID + "]...");

            await Request(route, method, json);

            Debug.Log("Update completed");
        }

        /// <summary>
        /// Make an API call and parse the JSON result on success
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <param name="json">Start cursor for paginated results</param>
        /// <returns></returns>
        public static async Task Request(string uri, string method, string json)
        {
            Debug.Log("> Notion API call " + method + " " + uri + "\n");

            using (UnityWebRequest webRequest = new UnityWebRequest(uri, method))
            {
                // ⚠ Unity poor implementation of HTTP client forces us to use a HACK to send non-form JSON with a POST verb
                // https://forum.unity.com/threads/posting-json-through-unitywebrequest.476254/#post-4693241
                if (string.IsNullOrEmpty(json) == false)
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                webRequest.downloadHandler = new DownloadHandlerBuffer();

                webRequest.SetRequestHeader("Authorization", $"Bearer {Notion.API_TOKEN}");
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Notion-Version", Notion.API_VERSION);

                var op = webRequest.SendWebRequest();
                await op;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("< Error: " + webRequest.error);
                        Debug.LogError(webRequest.downloadHandler.text);
                        break;
                }

                // Debug
#pragma warning disable CS0162
                if (Notion.DISPLAY_JSON_RESULT) Debug.Log(webRequest.downloadHandler.text);
            }
        }

        /// <summary>
        /// Change a database title
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="title"></param>
        public static async void ChangeDatabaseTitle(string databaseId, string title)
        {
            string data = @"{
                ""title"": [
                {
                    ""text"": {
                        ""content"": """ + title + @"""
                    }
                }
                ]
            }";

            await UpdateTableAsync(databaseId, "PATCH", data);
        }


#region Helpers

        public static string GetTitleBlock(string content)
        {
            int level = 1;
            if (content.StartsWith("####")) level = 4;
            else if (content.StartsWith("###")) level = 3;
            else if (content.StartsWith("##")) level = 2;
            return @"{
                ""object"": ""block"",
                ""type"": ""heading_" + level + @""",
                ""heading_" + level + @""": {
                    ""text"": [{ ""type"": ""text"", ""text"": { ""content"": """ + content.Replace("#", "").Trim() + @""" } }]
                }
            }";
        }

        public static string GetTextBlock(string content)
        {
            return @"{
                ""object"": ""block"",
                ""type"": ""paragraph"",
                ""paragraph"": {
                        ""text"": [{ ""type"": ""text"", ""text"": { ""content"": """ + content + @""" } }]
                    }
            }";
        }

        public static string GetCodeBlock(string content, string language)
        {
            return @"{
                ""object"": ""block"",
                ""type"": ""code"",
                ""code"": {
                        ""language"":""" + language + @""",
                        ""text"": [{ ""type"": ""text"", ""text"": { ""content"": """ + content + @""" } }]
                    }
            }";
        }

        public static string GetBulletListBlock(string title, string childrens)
        {
            return @"{
                ""object"": ""block"",
                ""type"": ""bulleted_list_item"",
                ""bulleted_list_item"": {
                ""text"": [{
                    ""type"": ""text"",
                    ""text"": {
                        ""content"": """ + title + @""",
                        ""link"": null
                    }
                }],
                ""children"":[" + childrens + @"]
            }";
        }

#endregion
    }
}