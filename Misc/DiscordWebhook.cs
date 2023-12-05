using System.Net;
using System.Threading.Tasks;
using kg.ValheimEnchantmentSystem.UI;

namespace kg.ValheimEnchantmentSystem.Misc;

public static class DiscordWebhook
{
    public static void TrySend(string link, string msg)
    {
        msg = System.Text.RegularExpressions.Regex.Replace(msg, "<.*?>", "**");
        if (!Uri.TryCreate(link, UriKind.Absolute, out _)) return;
        Task.Run(async () =>
        {
            string json = "{\n\"avatar_url\": \"\",\n  \"content\": \"" + $"{msg}" + "\",\n  \"embeds\": [],\n  \"components\": []\n}";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(link);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                await streamWriter.WriteAsync(json);
            }
            await httpWebRequest.GetResponseAsync();
        });
    }
}