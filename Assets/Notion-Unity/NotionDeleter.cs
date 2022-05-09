using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NotionUnity
{
    /// <summary>
    /// Notion block deleter
    /// </summary>
    public static class NotionDeleter
    {
        public static async Task DeleteBlocks(List<string> blockIds)
        {
            Debug.Log("Deleting " + blockIds.Count + " blocks...");

            foreach (string blockId in blockIds)
            {
                await DeleteBlock(blockId);
            }
            Debug.Log("All Blocks deleted.");
        }

        public static async Task DeleteBlock(string blockId)
        {
            Debug.Log("Deleting block " + blockId);
            string route = $"{Notion.API_URL}/blocks/{blockId}";
            await Request(route);
        }

        /// <summary>
        /// Make an API call and parse the JSON result on success
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <param name="json">Start cursor for paginated results</param>
        /// <returns></returns>
        public static async Task Request(string uri)
        {
            Debug.Log("> Notion API call DELETE " + uri + "\n");

            using (UnityWebRequest webRequest = new UnityWebRequest(uri, "DELETE"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                //'https://api.notion.com/v1/blocks/9bc30ad4-9373-46a5-84ab-0a7845ee52e6' \
                //  -H 'Authorization: Bearer '"$NOTION_API_KEY"'' \
                //  -H 'Notion-Version: 2021-08-16'


                webRequest.SetRequestHeader("Authorization", $"Bearer {Notion.API_TOKEN}");
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
    }
}