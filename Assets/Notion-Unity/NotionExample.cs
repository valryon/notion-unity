using System.Threading.Tasks;
using UnityEngine;

public class NotionExample : MonoBehaviour
{
    public string databaseID;

    private async void Start()
    {
        var r = await NotionDownloader.GetTableAsync(databaseID);

        for (int i = 0; i < r.Lines.Length; i++)
        {
            var line = r.Lines[i];

            Debug.Log(i + " ------------- ");

            foreach (var cell in line.Cells)
            {
                Debug.Log(cell.Name + " (" + cell.Type + ") =" + cell.Value);
            }
        }
    }
}
